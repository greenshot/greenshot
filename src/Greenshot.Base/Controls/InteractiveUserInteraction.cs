/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using log4net;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// <see cref="IUserInteraction"/> with UI: called from the pool, every dialog is shown on the UI thread through the dispatcher.
    /// At most one modal prompt at a time (app-wide): parallel flows queue their prompts instead of stacking modal loops.
    /// </summary>
    public sealed class InteractiveUserInteraction : IUserInteraction, IDialogViewRegistry
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InteractiveUserInteraction));
        private static readonly TimeSpan ProgressDialogDelay = TimeSpan.FromMilliseconds(300);
        private readonly IUiDispatcher _ui;
        private readonly SemaphoreSlim _modal = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>> _views = new ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>>();

        public InteractiveUserInteraction(IUiDispatcher ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public bool IsInteractive => true;

        /// <summary>
        /// Run a modal prompt on the UI thread, one at a time.
        /// </summary>
        private async Task<T> ModalAsync<T>(Func<T> prompt, CancellationToken cancellationToken)
        {
            await _modal.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await _ui.InvokeAsync(prompt, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _modal.Release();
            }
        }

        private async Task<T> ModalTaskAsync<T>(Func<Task<T>> prompt, CancellationToken cancellationToken)
        {
            await _modal.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await _ui.InvokeAsync(prompt, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _modal.Release();
            }
        }

        public Task<string> PickSaveFileAsync(SaveFileRequest request, CancellationToken cancellationToken)
        {
            return ModalAsync(() =>
            {
                var saveImageFileDialog = new SaveImageFileDialog(request?.CaptureDetails);
                return saveImageFileDialog.ShowDialog() ? saveImageFileDialog.FileNameWithExtension : null;
            }, cancellationToken);
        }

        public Task<SurfaceOutputSettings> PromptOutputSettingsAsync(SurfaceOutputSettings current, CancellationToken cancellationToken)
        {
            return ModalAsync(() =>
            {
                var qualityWindow = new QualityWindow(current);
                qualityWindow.ShowDialog();
                return qualityWindow.Settings;
            }, cancellationToken);
        }

        public Task<IDestination> PickDestinationAsync(IReadOnlyList<IDestination> choices, ICaptureDetails captureDetails, CancellationToken cancellationToken)
        {
            return ModalTaskAsync(() => DestinationMenuBuilder.ShowPickerAsync(choices, captureDetails, cancellationToken), cancellationToken);
        }

        public void Register<TViewModel, TResult>(Func<TViewModel, TResult> showDialog) where TViewModel : IDialogViewModel<TResult>
        {
            if (showDialog == null) throw new ArgumentNullException(nameof(showDialog));
            _views[typeof(TViewModel)] = (viewModel, _) => Task.FromResult<object>(showDialog((TViewModel)viewModel));
        }

        public void RegisterAsyncView<TViewModel, TResult>(Func<TViewModel, CancellationToken, Task<TResult>> showDialog) where TViewModel : IDialogViewModel<TResult>
        {
            if (showDialog == null) throw new ArgumentNullException(nameof(showDialog));
            _views[typeof(TViewModel)] = async (viewModel, cancellationToken) => (object)await showDialog((TViewModel)viewModel, cancellationToken).ConfigureAwait(true);
        }

        public async Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel, CancellationToken cancellationToken)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            if (!_views.TryGetValue(viewModel.GetType(), out var showDialog))
            {
                throw new InvalidOperationException($"No view registered for {viewModel.GetType().Name}");
            }

            var result = await ModalTaskAsync(() => showDialog(viewModel, cancellationToken), cancellationToken).ConfigureAwait(false);
            return result is TResult typed ? typed : default;
        }

        public async Task<T> RunWithProgressAsync<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> work, CancellationToken cancellationToken)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var progress = new DialogProgress(_ui);
            // The work runs on the calling (pool) thread
            var workTask = work(progress, cancellation.Token);

            // Only show a dialog when the work takes a while (no flicker for fast uploads)
            if (await Task.WhenAny(workTask, Task.Delay(ProgressDialogDelay, CancellationToken.None)).ConfigureAwait(false) == workTask)
            {
                return await workTask.ConfigureAwait(false);
            }

            ProgressWindow dialog = null;
            try
            {
                dialog = await _ui.InvokeAsync(() =>
                {
                    var progressDialog = new ProgressWindow(title, () =>
                    {
                        try
                        {
                            cancellation.Cancel();
                        }
                        catch (ObjectDisposedException)
                        {
                            // The work ended in the meantime
                        }
                    });
                    progress.Attach(progressDialog);
                    progressDialog.Show();
                    return progressDialog;
                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't show the progress dialog", ex);
            }

            try
            {
                return await workTask.ConfigureAwait(false);
            }
            finally
            {
                if (dialog != null)
                {
                    progress.Detach();
                    _ui.InvokeAsync(() =>
                    {
                        dialog.DetachCancel();
                        dialog.CloseByCode();
                    }, CancellationToken.None).FireAndLog("Close the progress dialog", Log);
                }
            }
        }

        public Task NotifyAsync(Notification notification)
        {
            if (notification == null)
            {
                return Task.CompletedTask;
            }

            var notificationService = SimpleServiceProvider.Current.GetInstance<INotificationService>(isOptional: true);
            if (notificationService == null)
            {
                Log.InfoFormat("Notification ({0}): {1}", notification.Kind, notification.Message);
                return Task.CompletedTask;
            }

            // Notifications are shown on the UI thread, but nobody waits for the user
            return _ui.InvokeAsync(() =>
            {
                switch (notification.Kind)
                {
                    case NotificationKind.Error:
                        notificationService.ShowErrorMessage(notification.Message, notification.Timeout, notification.OnClick);
                        break;
                    case NotificationKind.Warning:
                        notificationService.ShowWarningMessage(notification.Message, notification.Timeout, notification.OnClick);
                        break;
                    default:
                        notificationService.ShowInfoMessage(notification.Message, notification.Timeout, notification.OnClick);
                        break;
                }
            });
        }

        public Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken)
        {
            return ModalAsync<bool?>(() =>
            {
                var buttons = isError
                    ? new[] { Language.GetString("OK") }
                    : new[] { Language.GetString("OK"), Language.GetString("CANCEL") };
                // Flows run without a window of their own, so the box is shown over all windows
                int choice = ThemedMessageBox.ShowChoice(null, title, message, isError ? MessageBoxImage.Error : MessageBoxImage.Question, buttons,
                    defaultIndex: 0, cancelIndex: buttons.Length - 1, onTop: true);
                return choice == 0;
            }, cancellationToken);
        }

        /// <summary>
        /// Progress which reports to the dialog through the dispatcher (not Progress&lt;T&gt;: that captures the creating thread's context).
        /// </summary>
        private sealed class DialogProgress : IProgress<ProgressInfo>
        {
            private readonly IUiDispatcher _ui;
            private ProgressWindow _dialog;
            private ProgressInfo _last;

            public DialogProgress(IUiDispatcher ui)
            {
                _ui = ui;
            }

            public void Attach(ProgressWindow dialog)
            {
                _dialog = dialog;
                if (_last != null)
                {
                    dialog.Report(_last);
                }
            }

            public void Detach()
            {
                _dialog = null;
            }

            public void Report(ProgressInfo value)
            {
                _last = value;
                var dialog = _dialog;
                if (dialog == null)
                {
                    return;
                }

                _ui.InvokeAsync(() => dialog.Report(value)).FireAndLog("Report progress", Log);
            }
        }
    }
}
