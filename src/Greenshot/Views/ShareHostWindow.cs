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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using Greenshot.Destinations;
using Greenshot.Native;
using log4net;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Color = Windows.UI.Color;

namespace Greenshot.Views
{
    /// <summary>
    /// The window the Windows share dialog belongs to. ShowShareUIForWindow needs a real top level window, and when sharing from
    /// the destination picker, a recipe or the command line there is no window of Greenshot, so this is a tiny, nearly invisible one
    /// on the monitor with the mouse cursor: the share dialog opens there.
    /// </summary>
    public sealed class ShareHostWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ShareHostWindow));

        /// <summary>
        /// IID of DataTransferManager, required for the correct COM projection on both Windows 10 and 11
        /// </summary>
        private static readonly Guid DataTransferManagerIid = new Guid("a5caee9b-8708-49d1-8d36-67d25a8da00c");

        private readonly string _filePath;
        private readonly string _title;
        private IDataTransferManagerInterOp _dtmInterop;
        private DataTransferManager _dataTransferManager;
        private DispatcherTimer _watchdogTimer;

        // State tracking, the share events arrive on other threads
        private volatile bool _isShareOpen;
        private volatile bool _targetPicked;
        private bool _isClosed;

        /// <summary>
        /// The name of the app that received the share
        /// </summary>
        public string AppName { get; private set; }

        /// <summary>
        /// The view for a <see cref="ShareRequest"/>: shows the share dialog modally (on the UI thread).
        /// </summary>
        /// <returns>the name of the app that received the share, null when nothing was shared</returns>
        public static string Show(ShareRequest request)
        {
            var window = new ShareHostWindow(request.FilePath, request.Title);
            return window.ShowDialog() == true ? window.AppName ?? "Windows share" : null;
        }

        /// <param name="filePath">The capture, saved as PNG</param>
        /// <param name="title">Title of the share</param>
        public ShareHostWindow(string filePath, string title)
        {
            _filePath = filePath;
            _title = title;

            // Not to be seen: tiny, borderless, nearly transparent, not in the taskbar
            Width = 10;
            Height = 10;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Opacity = 0.01;
            ShowInTaskbar = false;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Title = "Greenshot share";

            SourceInitialized += OnSourceInitialized;
            ContentRendered += OnContentRendered;
            // The window gets the focus back when the share dialog closes, this is how a cancel is detected on Windows before 2004
            Activated += OnActivated;
            Closed += OnClosed;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;

            // In the middle of the monitor with the mouse cursor, in pixels
            var cursor = User32Api.GetCursorLocation();
            var display = DisplayInfo.AllDisplayInfos.FirstOrDefault(d => d.Bounds.Contains(cursor)) ?? DisplayInfo.AllDisplayInfos.FirstOrDefault();
            if (display != null)
            {
                var workArea = display.WorkingArea;
                User32Api.SetWindowPos(handle, IntPtr.Zero, workArea.Left + workArea.Width / 2, workArea.Top + workArea.Height / 2, 0, 0,
                    WindowPos.SWP_NOSIZE | WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);
            }

            try
            {
                InitializeShareManager(handle);
            }
            catch (Exception ex)
            {
                Log.Error("Init of DataTransferManager failed", ex);
            }
        }

        private void InitializeShareManager(IntPtr hwnd)
        {
            _dtmInterop = DataTransferManagerHelper.GetInteropFactory("Windows.ApplicationModel.DataTransfer.DataTransferManager");

            Guid dtmIid = DataTransferManagerIid;
            _dataTransferManager = _dtmInterop.GetForWindow(hwnd, ref dtmIid);
            if (_dataTransferManager == null)
            {
                throw new InvalidOperationException("GetForWindow returned null.");
            }

            // 1. Hook the Data Request (Setup content)
            _dataTransferManager.DataRequested += OnDataRequested;
            // 2. Hook the Result (Know if they picked something)
            _dataTransferManager.TargetApplicationChosen += OnTargetApplicationChosen;
        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            WindowHelper.ToForeground(handle);

            if (_dtmInterop == null)
            {
                CloseWith(false);
                return;
            }

            _isShareOpen = true;
            Log.Debug("Invoking ShowShareUIForWindow");
            try
            {
                _dtmInterop.ShowShareUIForWindow(handle);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to invoke ShowShareUIForWindow", ex);
                CloseWith(false);
                return;
            }

            // Safety watchdog (10 seconds): Greenshot never waits forever when the share flyout is dismissed or fails silently
            _watchdogTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = TimeSpan.FromSeconds(10) };
            _watchdogTimer.Tick += (s, args) =>
            {
                _watchdogTimer?.Stop();
                if (!_targetPicked)
                {
                    Log.Warn("Windows Share UI did not complete or was closed; aborting wait.");
                    CloseWith(false);
                }
            };
            _watchdogTimer.Start();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _isClosed = true;
            _watchdogTimer?.Stop();
            _watchdogTimer = null;
            if (_dataTransferManager != null)
            {
                _dataTransferManager.DataRequested -= OnDataRequested;
                _dataTransferManager.TargetApplicationChosen -= OnTargetApplicationChosen;
            }
        }

        /// <summary>
        /// Close the window with the result, on the UI thread (the share events are raised on other threads), only once.
        /// </summary>
        private void CloseWith(bool shared)
        {
            UiDispatcher.Current.InvokeAsync(() =>
            {
                _watchdogTimer?.Stop();
                if (_isClosed)
                {
                    return;
                }

                _isClosed = true;
                DialogResult = shared;
            }).FireAndLog("Close the share window", Log);
        }

        // --- EVENT 1: PREPARING DATA ---
        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            _targetPicked = false;
            _isShareOpen = true;

            var request = args.Request;
            // The deferral tells Windows the data comes later, it is completed by PrepareDataAsync
            var deferral = request.GetDeferral();
            PrepareDataAsync(request, deferral).FireAndLog("Prepare the share data", Log);
        }

        private async Task PrepareDataAsync(DataRequest request, DataRequestDeferral deferral)
        {
            try
            {
                StorageFile storageFile = await StorageFile.GetFileFromPathAsync(_filePath).AsTask().ConfigureAwait(false);
                Log.Debug("Created StorageFile for the capture");
                var imageRandomAccessStreamReference = RandomAccessStreamReference.CreateFromFile(storageFile);

                var dataPackage = request.Data;
                dataPackage.Properties.Title = _title ?? "Share a screenshot";
                dataPackage.Properties.ApplicationName = "Greenshot";
                dataPackage.Properties.Thumbnail = imageRandomAccessStreamReference;
                dataPackage.Properties.LogoBackgroundColor = Color.FromArgb(0xff, 0x3d, 0x3d, 0x3d);

                // Hook completion and cancellation lifecycle events
                dataPackage.ShareCompleted += (dp, scArgs) =>
                {
                    Log.Debug("DataPackage.ShareCompleted");
                    _targetPicked = true;
                    _isShareOpen = false;
                    CloseWith(true);
                };
                dataPackage.ShareCanceled += (dp, scArgs) =>
                {
                    Log.Debug("DataPackage.ShareCanceled");
                    _isShareOpen = false;
                    CloseWith(false);
                };
                dataPackage.OperationCompleted += (dp, ocArgs) =>
                {
                    Log.DebugFormat("DataPackage.OperationCompleted: {0}", ocArgs.Operation);
                    _targetPicked = true;
                    _isShareOpen = false;
                    CloseWith(true);
                };
                dataPackage.Destroyed += (dp, dArgs) =>
                {
                    Log.Debug("DataPackage.Destroyed");
                    if (!_targetPicked)
                    {
                        CloseWith(false);
                    }
                };

                dataPackage.SetStorageItems([storageFile]);
                dataPackage.SetBitmap(imageRandomAccessStreamReference);
            }
            catch (Exception ex)
            {
                request.FailWithDisplayText("Error: " + ex.Message);
                CloseWith(false);
            }
            finally
            {
                deferral.Complete();
            }
        }

        // --- EVENT 2: TARGET CHOSEN (SUCCESS) ---
        private void OnTargetApplicationChosen(DataTransferManager sender, TargetApplicationChosenEventArgs args)
        {
            // The user picked an app, the share dialog closes right after this
            _targetPicked = true;
            _isShareOpen = false;

            // 'args.ApplicationName' contains the Package Family Name (e.g., Microsoft.Windows.Mail_...)
            AppName = args.ApplicationName;
            CloseWith(true);
        }

        // --- EVENT 3: CANCELLATION DETECTION (HEURISTIC) ---
        private void OnActivated(object sender, EventArgs e)
        {
            // The window got the focus back. If the share was open, but no target was picked, the user clicked away (cancelled).
            if (!_isShareOpen)
            {
                return;
            }

            // A tiny delay: TargetApplicationChosen is raised at ALMOST the same time as Activated
            var timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = TimeSpan.FromMilliseconds(150) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                _isShareOpen = false;
                CloseWith(_targetPicked);
            };
            timer.Start();
        }
    }
}
