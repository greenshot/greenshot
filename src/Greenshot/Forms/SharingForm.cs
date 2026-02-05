/*
* Greenshot - a free and open source screenshot tool
* Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Drawing;
using Greenshot.Native;
using Greenshot.Native.Internal;
using log4net;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Color = Windows.UI.Color;

namespace Greenshot.Forms
{
    /// <summary>
    /// Form that displays the Windows Share UI for sharing captures to other apps
    /// </summary>
    public sealed partial class SharingForm : BaseForm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SharingForm));

        private IDataTransferManagerInterOp _dtmInterop;
        private DataTransferManager _dataTransferManager;

        // State tracking
        private bool _isShareOpen = false;
        private string _appName;
        private bool _targetPicked = false;
        private ISurface _surface;
        private ICaptureDetails _captureDetails;

        public bool TargetPicked { get => _targetPicked; }
        public string AppName { get => _appName; }

        public SharingForm(ISurface surface, ICaptureDetails captureDetails)
        {
            _surface = surface;
            _captureDetails = captureDetails;

            InitializeComponent();
            this.Load += MainForm_Load;

            // Hook into the Form's activation events to detect "Cancellation"
            this.Activated += SharingForm_Activated;
            this.Deactivate += SharingForm_Deactivate;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                InitializeShareManager();

                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init Failed: " + ex.ToString());
            }
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

        // --- EVENT 1: PREPARING DATA ---
        private async void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            // 1. Reset State
            _targetPicked = false;
            _isShareOpen = true;

            var request = args.Request;
            var deferral = request.GetDeferral();

            try
            {
                // --- STEP A: LAZY CLEANUP ---
                // Fire and forget: Try to clean up ANY old share files from previous runs.
                // If Paint still has one open, File.Delete will throw, so we catch and ignore it.
                CleanupOldShareFiles();

                // We use a GUID to ensure this specific share action never conflicts with an open app.
                var shareGuid = Guid.NewGuid();
                var outputSettings = new SurfaceOutputSettings(OutputFormat.png);

                // Capture itself
                string uniqueFileName = $"greenshot_share_{shareGuid}.png";
                string filePath = Path.Combine(Path.GetTempPath(), uniqueFileName);
                ImageIO.Save(_surface, filePath, false, outputSettings, false);
                Log.Debug("Created StorageFile for the capture");
                StorageFile storageFile = await StorageFile.GetFileFromPathAsync(filePath);
                var imageRandomAccessStreamReference = RandomAccessStreamReference.CreateFromFile(storageFile);

                var dataPackage = request.Data;
                dataPackage.Properties.Title = "Share a screenshot";
                dataPackage.Properties.ApplicationName = "Greenshot";
                dataPackage.Properties.Thumbnail = imageRandomAccessStreamReference;
                dataPackage.Properties.LogoBackgroundColor = Color.FromArgb(0xff, 0x3d, 0x3d, 0x3d);

                dataPackage.SetStorageItems([storageFile]);
                dataPackage.SetBitmap(imageRandomAccessStreamReference);

            }
            catch (Exception ex)
            {
                request.FailWithDisplayText("Error: " + ex.Message);
                DialogResult = System.Windows.Forms.DialogResult.Abort;

            }
            finally
            {
                deferral.Complete();
            }
        }

        // --- HELPER: SAFE CLEANUP ---
        private void CleanupOldShareFiles()
        {
            try
            {
                string tempDir = Path.GetTempPath();

                // Find all files matching our pattern "share_*.png"
                string[] oldFiles = Directory.GetFiles(tempDir, "greenshot_share_*.png");

                foreach (string file in oldFiles)
                {
                    try
                    {
                        // Try to delete. If Paint has it locked, this throws IOException.
                        File.Delete(file);
                    }
                    catch
                    {
                        // Intentionally empty. 
                        // If it's locked, we just leave it for the next time/cleanup.
                    }
                }
            }
            catch
            {
                // If Directory.GetFiles fails (rare), just ignore.
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
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        // --- EVENT 3: CANCELLATION DETECTION (HEURISTIC) ---

        private void SharingForm_Deactivate(object sender, EventArgs e)
        {
            // Logic: If the form loses focus, it *might* be because the Share UI popped up.
            // We already set _isShareOpen = true in the Button Click or DataRequested event.
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
                timer.Interval = 100;
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    // Reset state
                    _isShareOpen = false;

                    if (!_targetPicked)
                    {
                        // If we are here, the window is active, share was open, but no target selected.
                        DialogResult = System.Windows.Forms.DialogResult.Abort;
                    }
                    else
                    {
                        DialogResult = System.Windows.Forms.DialogResult.OK;
                    }
                };
                timer.Start();
            } else {
                // Initial showing, set the flag explicitly before showing the UI
                _isShareOpen = true;

                _dtmInterop.ShowShareUIForWindow(this.Handle);
            }
        }
    }
}