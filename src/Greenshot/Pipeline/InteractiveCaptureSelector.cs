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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.UI.Capture;
using log4net;

namespace Greenshot.Pipeline
{
    /// <summary>
    /// Shows the CaptureWindow on the UI thread (through the IUiDispatcher) and returns the selection to the flow on the pool.
    /// </summary>
    public class InteractiveCaptureSelector : IInteractiveCaptureSelector, ICaptureWindowPreparer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InteractiveCaptureSelector));
        private readonly IUiDispatcher _ui;
        private int _active;
        // A prepared window which isn't used in this time is closed, the flow didn't come to the selection
        private static readonly TimeSpan PreparedWindowLifetime = TimeSpan.FromSeconds(10);
        // Only accessed on the UI thread
        private CaptureWindow _openWindow;
        private CaptureWindow _preparedWindow;
        private DispatcherTimer _preparedWindowTimer;
        // The windows to snap to of the last PrepareWindow, with the time it was called
        private readonly object _snapWindowsLock = new object();
        private Task<List<WindowDetails>> _snapWindows;
        private DateTime _snapWindowsStarted;

        /// <param name="ui">Dispatcher for the UI thread, default is the registered one</param>
        public InteractiveCaptureSelector(IUiDispatcher ui = null)
        {
            _ui = ui;
        }

        private IUiDispatcher Ui => _ui ?? SimpleServiceProvider.Current.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance;

        public bool IsSelecting => Volatile.Read(ref _active) != 0;

        public void BringToFront()
        {
            Ui.InvokeAsync(() =>
            {
                _openWindow?.Activate();
            }).FireAndLog("Bring the capture window to the front", Log);
        }

        /// <inheritdoc />
        public void PrepareWindow(Task<List<WindowDetails>> snapWindows)
        {
            lock (_snapWindowsLock)
            {
                _snapWindows = snapWindows;
                _snapWindowsStarted = DateTime.UtcNow;
            }
            if (IsSelecting)
            {
                return;
            }
            var screenBounds = DisplayInfo.ScreenBounds;
            Ui.InvokeAsync(() => CreatePreparedWindow(screenBounds)).FireAndLog("Prepare the capture window", Log);
        }

        /// <inheritdoc />
        public bool TryTakeSnapWindows(out Task<List<WindowDetails>> snapWindows)
        {
            lock (_snapWindowsLock)
            {
                snapWindows = DateTime.UtcNow - _snapWindowsStarted < PreparedWindowLifetime ? _snapWindows : null;
                _snapWindows = null;
            }
            return snapWindows != null;
        }

        /// <summary>
        /// Runs on the UI thread: create the capture window without showing it, it is shown when it gets the capture
        /// </summary>
        private void CreatePreparedWindow(NativeRect screenBounds)
        {
            if (_preparedWindow != null || _openWindow != null)
            {
                return;
            }

            var captureWindow = new CaptureWindow(screenBounds);
            SetOwner(captureWindow);
            captureWindow.Prepare();
            _preparedWindow = captureWindow;
            _preparedWindowTimer ??= new DispatcherTimer { Interval = PreparedWindowLifetime };
            _preparedWindowTimer.Tick -= OnPreparedWindowExpired;
            _preparedWindowTimer.Tick += OnPreparedWindowExpired;
            _preparedWindowTimer.Start();
        }

        private void OnPreparedWindowExpired(object sender, EventArgs e)
        {
            _preparedWindowTimer.Stop();
            var unused = TakePreparedWindow();
            if (unused != null)
            {
                Log.Debug("The prepared capture window wasn't used, closing it.");
                unused.Close();
            }
        }

        /// <summary>
        /// The prepared window, if there is one which is still open
        /// </summary>
        private CaptureWindow TakePreparedWindow()
        {
            _preparedWindowTimer?.Stop();
            var preparedWindow = _preparedWindow;
            _preparedWindow = null;
            return preparedWindow is { IsPrepared: true } ? preparedWindow : null;
        }

        private static void SetOwner(CaptureWindow captureWindow)
        {
            if (SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>(isOptional: true) is System.Windows.Forms.IWin32Window mainForm)
            {
                new WindowInteropHelper(captureWindow).Owner = mainForm.Handle;
            }
        }

        public async Task<SelectionResult> SelectAsync(
            ICapture fullscreenCapture,
            IReadOnlyList<WindowDetails> visibleWindows,
            CaptureMode initialMode,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _active, 1, 0) != 0)
            {
                Log.Warn("Interactive selection is already in progress, bringing it to the front instead of opening a second one.");
                BringToFront();
                return null;
            }

            try
            {
                var selection = await Ui.InvokeAsync(() => ShowCaptureWindow(fullscreenCapture, visibleWindows, initialMode, cancellationToken), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return selection;
            }
            finally
            {
                Interlocked.Exchange(ref _active, 0);
            }
        }

        /// <summary>
        /// Runs on the UI thread: shows the capture window modally and returns the selection (null when the user declined)
        /// </summary>
        private SelectionResult ShowCaptureWindow(ICapture fullscreenCapture, IReadOnlyList<WindowDetails> visibleWindows, CaptureMode initialMode, CancellationToken cancellationToken)
        {
            ThreadAssert.IsUi(nameof(InteractiveCaptureSelector));
            if (fullscreenCapture?.CaptureDetails != null)
            {
                fullscreenCapture.CaptureDetails.CaptureMode = initialMode;
            }

            var windows = visibleWindows?.ToList() ?? new List<WindowDetails>();
            var captureWindow = TakePreparedWindow();
            if (captureWindow != null)
            {
                captureWindow.SetCapture(fullscreenCapture, windows);
            }
            else
            {
                captureWindow = new CaptureWindow(fullscreenCapture, windows);
                SetOwner(captureWindow);
            }
            _openWindow = captureWindow;
            // Cancelling the flow closes the window
            using var registration = cancellationToken.Register(() => Ui.InvokeAsync(() =>
            {
                if (_openWindow == captureWindow)
                {
                    captureWindow.Cancel();
                }
            }).FireAndLog("Close the capture window", Log));

            bool? result;
            try
            {
                result = captureWindow.WaitUntilClosed();
            }
            finally
            {
                _openWindow = null;
            }

            if (result != true || cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return new SelectionResult
            {
                SelectedRegion = captureWindow.CaptureRectangle,
                SelectedWindow = captureWindow.SelectedCaptureWindow,
                FinalMode = captureWindow.UsedCaptureMode
            };
        }
    }
}
