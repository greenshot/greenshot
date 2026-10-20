/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// Everything a flow step or destination may ask from the user (roadmap section 4.3), callable from the thread pool.
    /// The interactive implementation shows the UI through the IUiDispatcher and allows one modal prompt at a time;
    /// the headless one (unattended recipes, CLI, tests) returns defaults or throws <see cref="InteractionRequiredException"/>.
    /// </summary>
    public interface IUserInteraction
    {
        /// <summary>
        /// False for unattended runs: dialogs are not possible.
        /// </summary>
        bool IsInteractive { get; }

        /// <summary>
        /// Ask for a file to save to, null when the user declined.
        /// </summary>
        Task<string> PickSaveFileAsync(SaveFileRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Ask for the output settings (e.g. JPEG quality), null when the user declined.
        /// </summary>
        Task<SurfaceOutputSettings> PromptOutputSettingsAsync(SurfaceOutputSettings current, CancellationToken cancellationToken);

        /// <summary>
        /// Let the user pick one of the destinations (the destination picker), null when the user declined.
        /// The capture details decide which (dynamic) destinations are available.
        /// </summary>
        Task<IDestination> PickDestinationAsync(IReadOnlyList<IDestination> choices, ICaptureDetails captureDetails, CancellationToken cancellationToken);

        /// <summary>
        /// Show the dialog for the view model (the view is registered by whoever owns it, see IDialogViewRegistry),
        /// returns default(TResult) when the user declined.
        /// </summary>
        Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel, CancellationToken cancellationToken);

        /// <summary>
        /// Run the work (on the calling pool thread) while a non-blocking progress dialog with a cancel button is shown,
        /// but only if the work takes longer than ~300 ms (no flicker for fast uploads). Replaces PleaseWaitForm / BackgroundForm.
        /// </summary>
        Task<T> RunWithProgressAsync<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> work, CancellationToken cancellationToken);

        /// <summary>
        /// Show a notification (tray / toast), doesn't wait for the user.
        /// </summary>
        Task NotifyAsync(Notification notification);

        /// <summary>
        /// Show a message which the user has to confirm; null in a headless run.
        /// </summary>
        Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Marker for the view model of a dialog with the result type TResult. The view is registered in the IDialogViewRegistry.
    /// </summary>
    public interface IDialogViewModel<TResult>
    {
    }

    /// <summary>
    /// Maps dialog view models to their views; registered by the owner of the view (core, plugins).
    /// </summary>
    public interface IDialogViewRegistry
    {
        /// <summary>
        /// Register the function which shows the view for the view model (runs on the UI thread) and returns its result.
        /// </summary>
        void Register<TViewModel, TResult>(Func<TViewModel, TResult> showDialog) where TViewModel : IDialogViewModel<TResult>;

        /// <summary>
        /// Register an asynchronous view (e.g. a view which awaits network calls itself), started on the UI thread.
        /// </summary>
        void RegisterAsyncView<TViewModel, TResult>(Func<TViewModel, CancellationToken, Task<TResult>> showDialog) where TViewModel : IDialogViewModel<TResult>;
    }

    /// <summary>
    /// A flow needs the user, but it runs unattended.
    /// </summary>
    public class InteractionRequiredException : InvalidOperationException
    {
        public InteractionRequiredException(string what) : base($"'{what}' needs the user, but this runs unattended.")
        {
        }
    }

    /// <summary>
    /// What to ask for in a save file dialog.
    /// </summary>
    public sealed class SaveFileRequest
    {
        public SaveFileRequest(ICaptureDetails captureDetails, string suggestedPath = null, string format = null)
        {
            CaptureDetails = captureDetails;
            SuggestedPath = suggestedPath;
            Format = format;
        }

        public ICaptureDetails CaptureDetails { get; }

        public string SuggestedPath { get; }

        public string Format { get; }
    }

    /// <summary>
    /// Progress of a long running operation, Percentage null means indeterminate.
    /// </summary>
    public sealed class ProgressInfo
    {
        public ProgressInfo(string message, double? percentage = null)
        {
            Message = message;
            Percentage = percentage;
        }

        public string Message { get; }

        public double? Percentage { get; }
    }

    public enum NotificationKind
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// A line of a notification, e.g. what a destination did, with the icon of the destination
    /// </summary>
    public sealed class NotificationLine
    {
        public NotificationLine(string text, string iconPath = null)
        {
            Text = text;
            IconPath = iconPath;
        }

        public string Text { get; }

        /// <summary>
        /// An image file with the icon shown next to the text, or null
        /// </summary>
        public string IconPath { get; }
    }

    /// <summary>
    /// A button of a notification. A click raises <see cref="INotificationService.ButtonActivated"/> with the action and the value,
    /// also after Greenshot exited: they are all a handler gets, so the value must name what the button acts on (e.g. a file)
    /// </summary>
    public sealed class NotificationButton
    {
        public NotificationButton(string text, string action, string value = null)
        {
            Text = text;
            Action = action;
            Value = value;
            Choices = Array.Empty<NotificationChoice>();
        }

        /// <summary>
        /// A button which acts on one of several values: the notification shows a drop-down with the choices, the chosen value is
        /// the <see cref="NotificationButtonEventArgs.Value"/> of the click
        /// </summary>
        public NotificationButton(string text, string action, IReadOnlyList<NotificationChoice> choices)
        {
            Text = text;
            Action = action;
            Choices = choices ?? Array.Empty<NotificationChoice>();
        }

        /// <summary>
        /// The values to choose from, empty for a button with its <see cref="Value"/>
        /// </summary>
        public IReadOnlyList<NotificationChoice> Choices { get; }

        /// <summary>
        /// The text of the button
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// What the button does, e.g. "open", see NotificationButtons in Greenshot
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// What it acts on, e.g. the path of a file, or null (also for a button with <see cref="Choices"/>)
        /// </summary>
        public string Value { get; }
    }

    /// <summary>
    /// One of the values a <see cref="NotificationButton"/> can act on, e.g. a link
    /// </summary>
    public sealed class NotificationChoice
    {
        public NotificationChoice(string text, string value)
        {
            Text = text;
            Value = value;
        }

        /// <summary>
        /// What the drop-down shows, e.g. "Box: https://…"
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The value, e.g. the link
        /// </summary>
        public string Value { get; }
    }

    /// <summary>
    /// A notification for the user, the click action runs on the UI thread.
    /// </summary>
    public sealed class Notification
    {
        public Notification(NotificationKind kind, string message, TimeSpan? timeout = null, Action onClick = null, string captureId = null, string imagePath = null,
            string title = null, IReadOnlyList<NotificationLine> lines = null, IReadOnlyList<NotificationButton> buttons = null)
        {
            Buttons = buttons ?? Array.Empty<NotificationButton>();
            Kind = kind;
            Message = message;
            Timeout = timeout;
            OnClick = onClick;
            CaptureId = captureId;
            ImagePath = imagePath;
            Title = title;
            Lines = lines ?? Array.Empty<NotificationLine>();
        }

        public NotificationKind Kind { get; }

        /// <summary>
        /// The whole text, also of the <see cref="Lines"/>: what an implementation shows which can't show lines with icons
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// A heading above the <see cref="Lines"/>, or null
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The lines with their icons, e.g. one per destination; empty when the notification is just the <see cref="Message"/>
        /// </summary>
        public IReadOnlyList<NotificationLine> Lines { get; }

        /// <summary>
        /// The buttons, only shown by a service which <see cref="INotificationService.ShowsCaptures"/> (the toasts)
        /// </summary>
        public IReadOnlyList<NotificationButton> Buttons { get; }

        public TimeSpan? Timeout { get; }

        public Action OnClick { get; }

        /// <summary>
        /// The id of the capture kept for the notification, which a click offers in the destination picker (also after a restart), or null
        /// </summary>
        public string CaptureId { get; }

        /// <summary>
        /// An image file the notification shows, or null
        /// </summary>
        public string ImagePath { get; }
    }
}
