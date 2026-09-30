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
using System.IO;
using System.Windows.Forms;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Native;
using log4net;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Greenshot.Base.Core.FileFormat;
using Color = Windows.UI.Color;
using System.Threading.Tasks;
using Greenshot.Base.Threading;
using Greenshot.Destinations;

namespace Greenshot.Forms
{
    /// <summary>
    /// Form that displays the Windows Share UI for sharing captures to other apps
    /// </summary>
    public sealed partial class SharingForm : GreenshotForm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SharingForm));

        private IDataTransferManagerInterOp _dtmInterop;
        private DataTransferManager _dataTransferManager;

        // State tracking
        private bool _isShareOpen = false;
        private string _appName;
        private bool _targetPicked = false;
        private readonly string _filePath;
        private readonly string _title;
        private System.Windows.Forms.Timer _watchdogTimer;

        public bool TargetPicked { get => _targetPicked; }
        public string AppName { get => _appName; }

        /// <summary>
        /// The view for a <see cref="ShareRequest"/>: shows the share dialog modally (on the UI thread).
        /// </summary>
        /// <returns>the name of the app that received the share, null when nothing was shared</returns>
        public static string Show(ShareRequest request)
        {
            using var sharingForm = new SharingForm(request.FilePath, request.Title);
            var dialogResult = sharingForm.ShowDialog();
            return dialogResult == System.Windows.Forms.DialogResult.OK ? sharingForm.AppName ?? "Windows share" : null;
        }

        public SharingForm() : this(null, null)
        {
        }

        /// <param name="filePath">The capture, saved as PNG</param>
        /// <param name="title">Title of the share</param>
        public SharingForm(string filePath, string title)
        {
            _filePath = filePath;
            _title = title;

            InitializeComponent();

            try
            {
                InitializeShareManager();
            }
            catch (Exception ex)
            {
                Log.Error("Init of DataTransferManager failed", ex);
            }

            // Hook into the Form's activation events to detect "Cancellation"
            this.Activated += SharingForm_Activated;
            this.Deactivate += SharingForm_Deactivate;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            WindowDetails.ToForeground(this.Handle);

            _isShareOpen = true;
            Log.Debug("Invoking ShowShareUIForWindow");
            try
            {
                _dtmInterop?.ShowShareUIForWindow(this.Handle);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to invoke ShowShareUIForWindow", ex);
                DialogResult = System.Windows.Forms.DialogResult.Abort;
                return;
            }

            // Safety watchdog (10 seconds): ensures Greenshot never deadlocks if OS share flyout is dismissed or fails silently
            _watchdogTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            _watchdogTimer.Tick += (s, args) =>
            {
                _watchdogTimer?.Stop();
                _watchdogTimer?.Dispose();
                _watchdogTimer = null;
                if (!_targetPicked && DialogResult == System.Windows.Forms.DialogResult.None)
                {
                    Log.Warn("Windows Share UI did not complete or was closed; aborting wait.");
                    DialogResult = System.Windows.Forms.DialogResult.Abort;
                }
            };
            _watchdogTimer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _watchdogTimer?.Stop();
            _watchdogTimer?.Dispose();
            _watchdogTimer = null;
            base.OnFormClosed(e);
        }

        private void InitializeShareManager()
        {
            _dtmInterop = DataTransferManagerHelper.GetInteropFactory("Windows.ApplicationModel.DataTransfer.DataTransferManager");

            IntPtr hwnd = this.Handle;
            // IID of DataTransferManager - required for correct COM projection on both Windows 10 and 11
            Guid dtmIid = new Guid("a5caee9b-8708-49d1-8d36-67d25a8da00c");

            _dataTransferManager = _dtmInterop.GetForWindow(hwnd, ref dtmIid);

            if (_dataTransferManager == null) throw new Exception("GetForWindow returned null.");

            // 1. Hook the Data Request (Setup content)
            _dataTransferManager.DataRequested += OnDataRequested;

            // 2. Hook the Result (Know if they picked something)
            _dataTransferManager.TargetApplicationChosen += OnTargetApplicationChosen;
        }

        /// <summary>
        /// Close the form with the result, marshaled to the UI thread (the share events are raised on other threads).
        /// </summary>
        private void CloseWith(System.Windows.Forms.DialogResult dialogResult)
        {
            UiDispatcher.Current.InvokeAsync(() =>
            {
                _watchdogTimer?.Stop();
                if (!IsDisposed && DialogResult == System.Windows.Forms.DialogResult.None)
                {
                    DialogResult = dialogResult;
                }
            }).FireAndLog("Close the share form", Log);
        }

        // --- EVENT 1: PREPARING DATA ---
        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            // 1. Reset State
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
                    CloseWith(System.Windows.Forms.DialogResult.OK);
                };
                dataPackage.ShareCanceled += (dp, scArgs) =>
                {
                    Log.Debug("DataPackage.ShareCanceled");
                    _isShareOpen = false;
                    CloseWith(System.Windows.Forms.DialogResult.Abort);
                };
                dataPackage.OperationCompleted += (dp, ocArgs) =>
                {
                    Log.DebugFormat("DataPackage.OperationCompleted: {0}", ocArgs.Operation);
                    _targetPicked = true;
                    _isShareOpen = false;
                    CloseWith(System.Windows.Forms.DialogResult.OK);
                };
                dataPackage.Destroyed += (dp, dArgs) =>
                {
                    Log.Debug("DataPackage.Destroyed");
                    if (!_targetPicked)
                    {
                        CloseWith(System.Windows.Forms.DialogResult.Abort);
                    }
                };

                dataPackage.SetStorageItems([storageFile]);
                dataPackage.SetBitmap(imageRandomAccessStreamReference);

            }
            catch (Exception ex)
            {
                request.FailWithDisplayText("Error: " + ex.Message);
                CloseWith(System.Windows.Forms.DialogResult.Abort);

            }
            finally
            {
                deferral.Complete();
            }
        }

        // --- EVENT 2: TARGET CHOSEN (SUCCESS) ---
        private void OnTargetApplicationChosen(DataTransferManager sender, TargetApplicationChosenEventArgs args)
        {
            // The user picked an app!
            _targetPicked = true;
            _isShareOpen = false; // The UI closes immediately after this

            // 'args.ApplicationName' contains the Package Family Name (e.g., Microsoft.Windows.Mail_...)
            _appName = args.ApplicationName;
            CloseWith(System.Windows.Forms.DialogResult.OK);
        }

        // --- EVENT 3: CANCELLATION DETECTION (HEURISTIC) ---

        private void SharingForm_Deactivate(object sender, EventArgs e)
        {
            // Logic: If the form loses focus, it *might* be because the Share UI popped up.
            // We already set _isShareOpen = true in OnShown or DataRequested event.
        }

        private void SharingForm_Activated(object sender, EventArgs e)
        {
            // Logic: The form got focus back.
            // If the Share was open, but no target was picked, it means the user clicked away (Cancelled).
            if (_isShareOpen)
            {
                // Give a tiny delay because "TargetApplicationChosen" fires ALMOST at the same time as Activated.
                // We want to make sure the other event had a chance to set _targetPicked = true.
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 150;
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    // Reset state
                    _isShareOpen = false;

                    if (!_targetPicked && DialogResult == System.Windows.Forms.DialogResult.None)
                    {
                        _watchdogTimer?.Stop();
                        DialogResult = System.Windows.Forms.DialogResult.Abort;
                    }
                    else if (_targetPicked && DialogResult == System.Windows.Forms.DialogResult.None)
                    {
                        _watchdogTimer?.Stop();
                        DialogResult = System.Windows.Forms.DialogResult.OK;
                    }
                };
                timer.Start();
            }
        }
    }
}