#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CommonServiceLocator;
using Dapplo.Ini;
using Dapplo.Windows.App;
using Dapplo.Windows.Desktop;
using Greenshot.Destinations;
using Greenshot.Forms;
using Dapplo.Log;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Greenshot.Addon.LegacyEditor;
using Greenshot.Addon.LegacyEditor.Drawing;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Components;
using Greenshot.Gfx;
using LangKey = Greenshot.Configuration.LangKey;

#endregion

namespace Greenshot.Helpers
{
    /// <summary>
    ///     CaptureHelper contains all the capture logic
    /// </summary>
    public class CaptureHelper : IDisposable
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly ICoreConfiguration CoreConfig = IniConfig.Current.Get<ICoreConfiguration>();
        private readonly bool _captureMouseCursor;
        private ICapture _capture;
        private CaptureMode _captureMode;
        private NativeRect _captureRect = NativeRect.Empty;
        private ScreenCaptureMode _screenCaptureMode = ScreenCaptureMode.Auto;
        // TODO: when we get the screen capture code working correctly, this needs to be enabled
        //private static ScreenCaptureHelper screenCapture = null;
        private IList<IInteropWindow> _windows = new List<IInteropWindow>();
        private readonly IEnumerable<IDestination> _destinations;
        private readonly IEnumerable<IProcessor> _processors;

        public CaptureHelper(CaptureMode captureMode)
        {
            _captureMode = captureMode;
            _capture = new Capture();
            _destinations = ServiceLocator.Current.GetAllInstances<IDestination>();
            _processors = ServiceLocator.Current.GetAllInstances<IProcessor>();
        }

        public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor) : this(captureMode)
        {
            _captureMouseCursor = captureMouseCursor;
        }

        public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, ScreenCaptureMode screenCaptureMode) : this(captureMode)
        {
            _captureMouseCursor = captureMouseCursor;
            _screenCaptureMode = screenCaptureMode;
        }

        public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, IDestination destination) : this(captureMode, captureMouseCursor)
        {
            if (destination != null)
            {
                _capture.CaptureDetails.AddDestination(destination);
            }
        }

        public IInteropWindow SelectedCaptureWindow { get; set; }

        /// <summary>
        ///     The public accessible Dispose
        ///     Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)

        /// <summary>
        ///     This Dispose is called from the Dispose and the Destructor.
        ///     When disposing==true all non-managed resources should be freed too!
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup
            }
            // Unfortunately we can't dispose the capture, this might still be used somewhere else.
            _windows = null;
            SelectedCaptureWindow = null;
            _capture = null;
            // Empty working set after capturing
            if (CoreConfig.MinimizeWorkingSetSize)
            {
                PsApi.EmptyWorkingSet();
            }
        }

        public static void CaptureClipboard(IDestination destination = null)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.Clipboard, false, destination))
            {
                captureHelper.MakeCapture();
            }
        }

        public static void CaptureRegion(bool captureMouse, IDestination destination = null)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse, destination))
            {
                captureHelper.MakeCapture();
            }
        }

        public static void CaptureRegion(bool captureMouse, NativeRect region)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse))
            {
                captureHelper.MakeCapture(region);
            }
        }

        public static void CaptureFullscreen(bool captureMouse, ScreenCaptureMode screenCaptureMode, IDestination destination = null)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.FullScreen, captureMouse, destination))
            {
                captureHelper._screenCaptureMode = screenCaptureMode;
                captureHelper.MakeCapture();
            }
        }

        public static void CaptureLastRegion(bool captureMouse)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.LastRegion, captureMouse))
            {
                captureHelper.MakeCapture();
            }
        }

        public static void CaptureIe(bool captureMouse, IInteropWindow windowToCapture)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.IE, captureMouse))
            {
                captureHelper.SelectedCaptureWindow = windowToCapture;
                captureHelper.MakeCapture();
            }
        }

        public static void CaptureWindow(bool captureMouse, IDestination destination = null)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.ActiveWindow, captureMouse, destination))
            {
                captureHelper.MakeCapture();
            }
        }

        public static void CaptureWindow(IInteropWindow windowToCapture)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.ActiveWindow))
            {
                captureHelper.SelectedCaptureWindow = windowToCapture;
                captureHelper.MakeCapture();
            }
        }

        public static void CaptureWindowInteractive(bool captureMouse)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.Window))
            {
                captureHelper.MakeCapture();
            }
        }

        public static void CaptureFile(string filename)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.File))
            {
                captureHelper.MakeCapture(filename);
            }
        }

        public static void CaptureFile(string filename, IDestination destination)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.File))
            {
                captureHelper.AddDestination(destination).MakeCapture(filename);
            }
        }

        public static void ImportCapture(ICapture captureToImport)
        {
            using (var captureHelper = new CaptureHelper(CaptureMode.File))
            {
                captureHelper._capture = captureToImport;
                captureHelper.HandleCapture();
            }
        }

        public CaptureHelper AddDestination(IDestination destination)
        {
            _capture.CaptureDetails.AddDestination(destination);
            return this;
        }

        private void DoCaptureFeedback()
        {
            if (CoreConfig.PlayCameraSound)
            {
                SoundHelper.Play();
            }
        }

        /// <summary>
        ///     Make Capture with file name
        /// </summary>
        /// <param name="filename">filename</param>
        private void MakeCapture(string filename)
        {
            _capture.CaptureDetails.Filename = filename;
            MakeCapture();
        }

        /// <summary>
        ///     Make Capture for region
        /// </summary>
        /// <param name="region">NativeRect</param>
        private void MakeCapture(NativeRect region)
        {
            _captureRect = region;
            MakeCapture();
        }


        /// <summary>
        ///     Make Capture with specified destinations
        /// </summary>
        private void MakeCapture()
        {
            // This fixes a problem when a balloon is still visible and a capture needs to be taken
            // forcefully removes the balloon!
            if (!CoreConfig.HideTrayicon)
            {
                MainForm.Instance.NotifyIcon.Visible = false;
                MainForm.Instance.NotifyIcon.Visible = true;
            }
            Log.Debug().WriteLine($"Capturing with mode {_captureMode} and using Cursor {_captureMouseCursor}");
            _capture.CaptureDetails.CaptureMode = _captureMode;

            // Get the windows details in a seperate thread, only for those captures that have a Feedback
            // As currently the "elements" aren't used, we don't need them yet
            switch (_captureMode)
            {
                case CaptureMode.Region:
                    // Check if a region is pre-supplied!
                    if (_captureRect.IsEmpty)
                    {
                        PrepareForCaptureWithFeedback();
                    }
                    break;
                case CaptureMode.Window:
                    PrepareForCaptureWithFeedback();
                    break;
            }

            // Add destinations if no-one passed a handler
            if (_capture.CaptureDetails.CaptureDestinations == null || _capture.CaptureDetails.CaptureDestinations.Count == 0)
            {
                AddConfiguredDestination();
            }

            // Delay for the Context menu
            if (CoreConfig.CaptureDelay > 0)
            {
                Thread.Sleep(CoreConfig.CaptureDelay);
            }
            else
            {
                CoreConfig.CaptureDelay = 0;
            }

            // Capture Mousecursor if we are not loading from file or clipboard, only show when needed
            if (_captureMode != CaptureMode.File && _captureMode != CaptureMode.Clipboard)
            {
                _capture = WindowCapture.CaptureCursor(_capture);
                _capture.CursorVisible = _captureMouseCursor && CoreConfig.CaptureMousepointer;
            }

            switch (_captureMode)
            {
                case CaptureMode.Window:
                    _capture = WindowCapture.CaptureScreen(_capture);
                    _capture.CaptureDetails.AddMetaData("source", "Screen");
                    SetDpi();
                    CaptureWithFeedback();
                    break;
                case CaptureMode.ActiveWindow:
                    if (CaptureActiveWindow())
                    {
                        // Capture worked, offset mouse according to screen bounds and capture location
                        _capture.MoveMouseLocation(_capture.ScreenBounds.Location.X - _capture.Location.X, _capture.ScreenBounds.Location.Y - _capture.Location.Y);
                        _capture.CaptureDetails.AddMetaData("source", "Window");
                    }
                    else
                    {
                        _captureMode = CaptureMode.FullScreen;
                        _capture = WindowCapture.CaptureScreen(_capture);
                        _capture.CaptureDetails.AddMetaData("source", "Screen");
                        _capture.CaptureDetails.Title = "Screen";
                    }
                    SetDpi();
                    HandleCapture();
                    break;
                case CaptureMode.IE:
                    if (IeCaptureHelper.CaptureIe(_capture, SelectedCaptureWindow) != null)
                    {
                        _capture.CaptureDetails.AddMetaData("source", "Internet Explorer");
                        SetDpi();
                        HandleCapture();
                    }
                    break;
                case CaptureMode.FullScreen:
                    // Check how we need to capture the screen
                    var captureTaken = false;
                    switch (_screenCaptureMode)
                    {
                        case ScreenCaptureMode.Auto:
                            var mouseLocation = User32Api.GetCursorLocation();
                            foreach (var screen in Screen.AllScreens)
                            {
                                if (screen.Bounds.Contains(mouseLocation))
                                {
                                    _capture = WindowCapture.CaptureRectangle(_capture, screen.Bounds);
                                    captureTaken = true;
                                    break;
                                }
                            }
                            break;
                        case ScreenCaptureMode.Fixed:
                            if (CoreConfig.ScreenToCapture > 0 && CoreConfig.ScreenToCapture <= Screen.AllScreens.Length)
                            {
                                _capture = WindowCapture.CaptureRectangle(_capture, Screen.AllScreens[CoreConfig.ScreenToCapture].Bounds);
                                captureTaken = true;
                            }
                            break;
                        case ScreenCaptureMode.FullScreen:
                            // Do nothing, we take the fullscreen capture automatically
                            break;
                    }
                    if (!captureTaken)
                    {
                        _capture = WindowCapture.CaptureScreen(_capture);
                    }
                    SetDpi();
                    HandleCapture();
                    break;
                case CaptureMode.Clipboard:
                    var clipboardImage = ClipboardHelper.GetBitmap();
                    if (clipboardImage != null)
                    {
                        if (_capture != null)
                        {
                            _capture.Bitmap = clipboardImage;
                        }
                        else
                        {
                            _capture = new Capture(clipboardImage);
                        }
                        _capture.CaptureDetails.Title = "Clipboard";
                        _capture.CaptureDetails.AddMetaData("source", "Clipboard");
                        // Force Editor, keep picker
                        if (_capture.CaptureDetails.HasDestination(typeof(PickerDestination).GetDesignation()))
                        {
                            _capture.CaptureDetails.ClearDestinations();
                            _capture.CaptureDetails.AddDestination(_destinations.Find("Editor"));
                            _capture.CaptureDetails.AddDestination(_destinations.Find(typeof(PickerDestination)));
                        }
                        else
                        {
                            _capture.CaptureDetails.ClearDestinations();
                            _capture.CaptureDetails.AddDestination(_destinations.Find("Editor"));
                        }
                        HandleCapture();
                    }
                    else
                    {
                        MessageBox.Show(Language.GetString("clipboard_noimage"));
                    }
                    break;
                case CaptureMode.File:
                    Bitmap fileImage = null;
                    var filename = _capture.CaptureDetails.Filename;

                    if (!string.IsNullOrEmpty(filename))
                    {
                        try
                        {
                            if (filename.ToLower().EndsWith("." + OutputFormats.greenshot))
                            {
                                ISurface surface = new Surface();
                                surface = ImageOutput.LoadGreenshotSurface(filename, surface);
                                surface.CaptureDetails = _capture.CaptureDetails;
                                _destinations.Find(typeof(EditorDestination))?.ExportCapture(true, surface, _capture.CaptureDetails);
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error().WriteLine(e, e.Message);
                            MessageBox.Show(Language.GetFormattedString(LangKey.error_openfile, filename));
                        }
                        try
                        {
                            fileImage = BitmapHelper.LoadBitmap(filename);
                        }
                        catch (Exception e)
                        {
                            Log.Error().WriteLine(e, e.Message);
                            MessageBox.Show(Language.GetFormattedString(LangKey.error_openfile, filename));
                        }
                    }
                    if (fileImage != null)
                    {
                        _capture.CaptureDetails.Title = Path.GetFileNameWithoutExtension(filename);
                        _capture.CaptureDetails.AddMetaData("file", filename);
                        _capture.CaptureDetails.AddMetaData("source", "file");
                        if (_capture != null)
                        {
                            _capture.Bitmap = fileImage;
                        }
                        else
                        {
                            _capture = new Capture(fileImage);
                        }
                        // Force Editor, keep picker, this is currently the only usefull destination
                        if (_capture.CaptureDetails.HasDestination(typeof(PickerDestination).GetDesignation()))
                        {
                            _capture.CaptureDetails.ClearDestinations();
                            _capture.CaptureDetails.AddDestination(_destinations.Find("Editor"));
                            _capture.CaptureDetails.AddDestination(_destinations.Find(typeof(PickerDestination)));
                        }
                        else
                        {
                            _capture.CaptureDetails.ClearDestinations();
                            _capture.CaptureDetails.AddDestination(_destinations.Find("Editor"));
                        }
                        HandleCapture();
                    }
                    break;
                case CaptureMode.LastRegion:
                    if (!CoreConfig.LastCapturedRegion.IsEmpty)
                    {
                        _capture = WindowCapture.CaptureRectangle(_capture, CoreConfig.LastCapturedRegion);

                        // Move cursor, fixing bug #3569703
                        _capture.MoveMouseLocation(_capture.ScreenBounds.Location.X - _capture.Location.X, _capture.ScreenBounds.Location.Y - _capture.Location.Y);
                        //capture.MoveElements(capture.ScreenBounds.Location.X - capture.Location.X, capture.ScreenBounds.Location.Y - capture.Location.Y);

                        _capture.CaptureDetails.AddMetaData("source", "screen");
                        SetDpi();
                        HandleCapture();
                    }
                    break;
                case CaptureMode.Region:
                    // Check if a region is pre-supplied!
                    if (NativeRect.Empty.Equals(_captureRect))
                    {
                        _capture = WindowCapture.CaptureScreen(_capture);
                        _capture.CaptureDetails.AddMetaData("source", "screen");
                        SetDpi();
                        CaptureWithFeedback();
                    }
                    else
                    {
                        _capture = WindowCapture.CaptureRectangle(_capture, _captureRect);
                        _capture.CaptureDetails.AddMetaData("source", "screen");
                        SetDpi();
                        HandleCapture();
                    }
                    break;
                default:
                    Log.Warn().WriteLine("Unknown capture mode: " + _captureMode);
                    break;
            }
            if (_capture != null)
            {
                Log.Debug().WriteLine("Disposing capture");
                _capture.Dispose();
            }
        }

        /// <summary>
        ///     Pre-Initialization for CaptureWithFeedback, this will get all the windows before we change anything
        /// </summary>
        private void PrepareForCaptureWithFeedback()
        {
            // TODO: Capture all popups, from the initial capture, to make them available like the mouse cursor.
            // Than change focus, to remove the popups, and take the real capture
            _windows = WindowsEnumerator.EnumerateWindows()
                .Concat(AppQuery.WindowsStoreApps)
                .Where(window => User32Api.IsWindow(window.Handle) && (window.IsTopLevel() || window.IsPopup()) && window.Handle != MainForm.Instance.Handle)
                .ToList();

            // If the App Launcher is visisble, on Windows 8 no other windows are active.. for Win 10 this is no longer true
            if (AppQuery.IsLauncherVisible)
            {
                _windows.Add(AppQuery.GetAppLauncher());
            }     
        }

        private void AddConfiguredDestination()
        {
            foreach (var destinationDesignation in CoreConfig.OutputDestinations)
            {
                var destination = _destinations.Find(destinationDesignation);
                if (destination != null)
                {
                    _capture.CaptureDetails.AddDestination(destination);
                }
            }
        }

        /// <summary>
        ///     If a balloon tip is show for a taken capture, this handles the click on it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenCaptureOnClick(object sender, EventArgs e)
        {
            if (!(MainForm.Instance.NotifyIcon.Tag is SurfaceMessageEventArgs eventArgs))
            {
                Log.Warn().WriteLine("OpenCaptureOnClick called without SurfaceMessageEventArgs");
                RemoveEventHandler(sender, e);
                return;
            }
            var surface = eventArgs.Surface;
            if (surface != null)
            {
                switch (eventArgs.MessageType)
                {
                    case SurfaceMessageTyp.FileSaved:
                        ExplorerHelper.OpenInExplorer(surface.LastSaveFullPath);
                        break;
                    case SurfaceMessageTyp.UploadedUri:
                        Process.Start(surface.UploadUrl);
                        break;
                }
            }
            Log.Debug().WriteLine("Deregistering the BalloonTipClicked");
            RemoveEventHandler(sender, e);
        }

        private void RemoveEventHandler(object sender, EventArgs e)
        {
            MainForm.Instance.NotifyIcon.BalloonTipClicked -= OpenCaptureOnClick;
            MainForm.Instance.NotifyIcon.BalloonTipClosed -= RemoveEventHandler;
            MainForm.Instance.NotifyIcon.Tag = null;
        }

        /// <summary>
        ///     This is the SufraceMessageEvent receiver
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs?.Message))
            {
                return;
            }
            if (MainForm.Instance == null)
            {
                return;
            }
            switch (eventArgs.MessageType)
            {
                case SurfaceMessageTyp.Error:
                    MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", eventArgs.Message, ToolTipIcon.Error);
                    break;
                case SurfaceMessageTyp.Info:
                    MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", eventArgs.Message, ToolTipIcon.Info);
                    break;
                case SurfaceMessageTyp.FileSaved:
                case SurfaceMessageTyp.UploadedUri:
                    // Show a balloon and register an event handler to open the "capture" for if someone clicks the balloon.
                    MainForm.Instance.NotifyIcon.BalloonTipClicked += OpenCaptureOnClick;
                    MainForm.Instance.NotifyIcon.BalloonTipClosed += RemoveEventHandler;
                    // Store for later usage
                    MainForm.Instance.NotifyIcon.Tag = eventArgs;
                    MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", eventArgs.Message, ToolTipIcon.Info);
                    break;
            }
        }

        /// <summary>
        /// Process the actuall capture
        /// </summary>
        private void HandleCapture()
        {
            // Flag to see if the image was "exported" so the FileEditor doesn't
            // ask to save the file as long as nothing is done.
            var outputMade = false;

            // Make sure the user sees that the capture is made
            if (_capture.CaptureDetails.CaptureMode == CaptureMode.File || _capture.CaptureDetails.CaptureMode == CaptureMode.Clipboard)
            {
                // Maybe not "made" but the original is still there... somehow
                outputMade = true;
            }
            else
            {
                // Make sure the resolution is set correctly!
                if (_capture.CaptureDetails != null)
                {
                    _capture.Bitmap?.SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
                    // Generate a title
                    if (_capture.CaptureDetails.Title == null)
                    {
                        var windows = InteropWindowQuery.GetTopLevelWindows().OrderByDescending(window => window.Intersection(_captureRect).Pixels()).ToList();
                        var windowWithLargestIntersection = windows.FirstOrDefault();
                        _capture.CaptureDetails.Title = windowWithLargestIntersection?.GetCaption();
                    }
                }


                DoCaptureFeedback();
            }

            Log.Debug().WriteLine("A capture of: " + _capture.CaptureDetails.Title);

            // check if someone has passed a destination
            if (_capture.CaptureDetails.CaptureDestinations == null || _capture.CaptureDetails.CaptureDestinations.Count == 0)
            {
                AddConfiguredDestination();
            }

            // Create Surface with capture, this way elements can be added automatically (like the mouse cursor)
            var surface = new Surface(_capture)
            {
                Modified = !outputMade
            };

            // Register notify events if this is wanted			
            if (CoreConfig.ShowTrayNotification && !CoreConfig.HideTrayicon)
            {
                surface.SurfaceMessage += SurfaceMessageReceived;
            }
            // Let the processors do their job
            foreach (var processor in _processors)
            {
                if (!processor.IsActive)
                {
                    continue;
                }
                Log.Info().WriteLine("Calling processor {0}", processor.Description);
                processor.ProcessCapture(surface, _capture.CaptureDetails);
            }

            // As the surfaces copies the reference to the image, make sure the image is not being disposed (a trick to save memory)
            _capture.Bitmap = null;

            // Get CaptureDetails as we need it even after the capture is disposed
            var captureDetails = _capture.CaptureDetails;
            var canDisposeSurface = true;

            if (captureDetails.HasDestination(typeof(PickerDestination).GetDesignation()))
            {
                _destinations.Find(typeof(PickerDestination))?.ExportCapture(false, surface, captureDetails);
                captureDetails.CaptureDestinations.Clear();
                canDisposeSurface = false;
            }

            // Disable capturing
            _captureMode = CaptureMode.None;
            // Dispose the capture, we don't need it anymore (the surface copied all information and we got the title (if any)).
            _capture.Dispose();
            _capture = null;

            var destinationCount = captureDetails.CaptureDestinations.Count;
            if (destinationCount > 0)
            {
                // Flag to detect if we need to create a temp file for the email
                // or use the file that was written
                foreach (var destination in captureDetails.CaptureDestinations)
                {
                    if (typeof(PickerDestination).GetDesignation().Equals(destination.Designation))
                    {
                        continue;
                    }
                    Log.Info().WriteLine("Calling destination {0}", destination.Description);

                    var exportInformation = destination.ExportCapture(false, surface, captureDetails);
                    if ("Editor".Equals(destination.Designation) && exportInformation.ExportMade)
                    {
                        canDisposeSurface = false;
                    }
                }
            }
            if (canDisposeSurface)
            {
                surface.Dispose();
            }
        }

        private bool CaptureActiveWindow()
        {
            var presupplied = false;
            Log.Debug().WriteLine("CaptureActiveWindow");
            if (SelectedCaptureWindow != null)
            {
                Log.Debug().WriteLine("Using supplied window");
                presupplied = true;
            }
            else
            {
                SelectedCaptureWindow = InteropWindowQuery.GetActiveWindow();
                if (SelectedCaptureWindow != null)
                {
                    if (Log.IsDebugEnabled())
                    {
                        Log.Debug().WriteLine("Capturing window: {0} with {1}", SelectedCaptureWindow.Text, SelectedCaptureWindow.GetInfo().Bounds);
                    }
                }
            }
            if (SelectedCaptureWindow == null || !presupplied && SelectedCaptureWindow.IsMinimized())
            {
                Log.Warn().WriteLine("No window to capture!");
                // Nothing to capture, code up in the stack will capture the full screen
                return false;
            }
            if (!presupplied && SelectedCaptureWindow != null && SelectedCaptureWindow.IsMinimized())
            {
                // Restore the window making sure it's visible!
                // This is done mainly for a screen capture, but some applications like Excel and TOAD have weird behaviour!
                SelectedCaptureWindow.Restore();
            }
            SelectedCaptureWindow = SelectCaptureWindow(SelectedCaptureWindow);
            if (SelectedCaptureWindow == null)
            {
                Log.Warn().WriteLine("No window to capture, after SelectCaptureWindow!");
                // Nothing to capture, code up in the stack will capture the full screen
                return false;
            }
            // Fix for Bug #3430560 
            CoreConfig.LastCapturedRegion = SelectedCaptureWindow.GetInfo().Bounds;
            var returnValue = CaptureWindow(SelectedCaptureWindow, _capture, CoreConfig.WindowCaptureMode) != null;
            return returnValue;
        }

        /// <summary>
        ///     Select the window to capture, this has logic which takes care of certain special applications
        ///     like TOAD or Excel
        /// </summary>
        /// <param name="windowToCapture">WindowDetails with the target Window</param>
        /// <returns>WindowDetails with the target Window OR a replacement</returns>
        public static IInteropWindow SelectCaptureWindow(IInteropWindow windowToCapture)
        {
            NativeRect windowRectangle = windowToCapture.GetInfo().Bounds;
            if (windowRectangle.Width == 0 || windowRectangle.Height == 0)
            {
                Log.Warn().WriteLine("Window {0} has nothing to capture, using workaround to find other window of same process.", windowToCapture.Text);
                // Trying workaround, the size 0 arrises with e.g. Toad.exe, has a different Window when minimized
                var linkedWindow = windowToCapture.GetLinkedWindows().FirstOrDefault();
                if (linkedWindow != null)
                {
                    windowToCapture = linkedWindow;
                }
                else
                {
                    return null;
                }
            }
            return windowToCapture;
        }

        /// <summary>
        ///     Check if Process uses PresentationFramework.dll -> meaning it uses WPF
        /// </summary>
        /// <param name="process">Proces to check for the presentation framework</param>
        /// <returns>true if the process uses WPF</returns>
        private static bool IsWpf(Process process)
        {
            if (process != null)
            {
                try
                {
                    foreach (ProcessModule module in process.Modules)
                    {
                        if (module.ModuleName.StartsWith("PresentationFramework"))
                        {
                            Log.Info().WriteLine("Found that Process {0} uses {1}, assuming it's using WPF", process.ProcessName, module.FileName);
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    // Access denied on the modules
                    Log.Warn().WriteLine("No access on the modules from process {0}, assuming WPF is used.", process.ProcessName);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Capture the supplied Window
        /// </summary>
        /// <param name="windowToCapture">IInteropWindow to capture</param>
        /// <param name="captureForWindow">The capture to store the details</param>
        /// <param name="windowCaptureMode">What WindowCaptureModes to use</param>
        /// <returns>ICapture</returns>
        public static ICapture CaptureWindow(IInteropWindow windowToCapture, ICapture captureForWindow, WindowCaptureModes windowCaptureMode)
        {
            if (captureForWindow == null)
            {
                captureForWindow = new Capture();
            }
            NativeRect windowRectangle = windowToCapture.GetInfo().Bounds;

            // When Vista & DWM (Aero) enabled
            var dwmEnabled = Dwm.IsDwmEnabled;
            // get process name to be able to exclude certain processes from certain capture modes
            using (var process = Process.GetProcessById(windowToCapture.GetProcessId()))
            {
                var isAutoMode = windowCaptureMode == WindowCaptureModes.Auto;
                // For WindowCaptureModes.Auto we check:
                // 1) Is window IE, use IE Capture
                // 2) Is Windows >= Vista & DWM enabled: use DWM
                // 3) Otherwise use GDI (Screen might be also okay but might lose content)
                if (isAutoMode)
                {
                    if (CoreConfig.IECapture && IeCaptureHelper.IsIeWindow(windowToCapture))
                    {
                        try
                        {
                            var ieCapture = IeCaptureHelper.CaptureIe(captureForWindow, windowToCapture);
                            if (ieCapture != null)
                            {
                                return ieCapture;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warn().WriteLine("Problem capturing IE, skipping to normal capture. Exception message was: {0}", ex.Message);
                        }
                    }

                    // Take default screen
                    windowCaptureMode = WindowCaptureModes.Screen;

                    // Change to GDI, if allowed
                    if (!windowToCapture.IsApp() && WindowCapture.IsGdiAllowed(process))
                    {
                        if (!dwmEnabled && IsWpf(process))
                        {
                            // do not use GDI, as DWM is not enabled and the application uses PresentationFramework.dll -> isWPF
                            Log.Info().WriteLine("Not using GDI for windows of process {0}, as the process uses WPF", process.ProcessName);
                        }
                        else
                        {
                            windowCaptureMode = WindowCaptureModes.GDI;
                        }
                    }

                    // Change to DWM, if enabled and allowed
                    if (dwmEnabled)
                    {
                        if (windowToCapture.IsApp() || WindowCapture.IsDwmAllowed(process))
                        {
                            windowCaptureMode = WindowCaptureModes.Aero;
                        }
                    }
                }
                else if (windowCaptureMode == WindowCaptureModes.Aero || windowCaptureMode == WindowCaptureModes.AeroTransparent)
                {
                    if (!dwmEnabled || !windowToCapture.IsApp() && !WindowCapture.IsDwmAllowed(process))
                    {
                        // Take default screen
                        windowCaptureMode = WindowCaptureModes.Screen;
                        // Change to GDI, if allowed
                        if (WindowCapture.IsGdiAllowed(process))
                        {
                            windowCaptureMode = WindowCaptureModes.GDI;
                        }
                    }
                }
                else if (windowCaptureMode == WindowCaptureModes.GDI && !WindowCapture.IsGdiAllowed(process))
                {
                    // GDI not allowed, take screen
                    windowCaptureMode = WindowCaptureModes.Screen;
                }

                Log.Info().WriteLine("Capturing window with mode {0}", windowCaptureMode);
                var captureTaken = false;
                windowRectangle = windowRectangle.Intersect(captureForWindow.ScreenBounds);
                // Try to capture
                while (!captureTaken)
                {
                    ICapture tmpCapture = null;
                    switch (windowCaptureMode)
                    {
                        case WindowCaptureModes.GDI:
                            if (WindowCapture.IsGdiAllowed(process))
                            {
                                if (windowToCapture.IsMinimized())
                                {
                                    // Restore the window making sure it's visible!
                                    windowToCapture.Restore();
                                }
                                else
                                {
                                    // TODO: Await
                                    windowToCapture.ToForegroundAsync(false);
                                }
                                tmpCapture = windowToCapture.CaptureGdiWindow(captureForWindow);
                                if (tmpCapture != null)
                                {
                                    // check if GDI capture any good, by comparing it with the screen content
                                    var blackCountGdi = tmpCapture.Bitmap.CountColor(Color.Black, false);
                                    var gdiPixels = tmpCapture.Bitmap.Width * tmpCapture.Bitmap.Height;
                                    var blackPercentageGdi = blackCountGdi * 100 / gdiPixels;
                                    if (blackPercentageGdi >= 1)
                                    {
                                        var screenPixels = windowRectangle.Width * windowRectangle.Height;
                                        using (ICapture screenCapture = new Capture())
                                        {
                                            screenCapture.CaptureDetails = captureForWindow.CaptureDetails;
                                            if (WindowCapture.CaptureRectangleFromDesktopScreen(screenCapture, windowRectangle) != null)
                                            {
                                                var blackCountScreen = screenCapture.Bitmap.CountColor(Color.Black, false);
                                                var blackPercentageScreen = blackCountScreen * 100 / screenPixels;
                                                if (screenPixels == gdiPixels)
                                                {
                                                    // "easy compare", both have the same size
                                                    // If GDI has more black, use the screen capture.
                                                    if (blackPercentageGdi > blackPercentageScreen)
                                                    {
                                                        Log.Debug().WriteLine("Using screen capture, as GDI had additional black.");
                                                        // changeing the image will automatically dispose the previous
                                                        tmpCapture.Bitmap = screenCapture.Bitmap;
                                                        // Make sure it's not disposed, else the picture is gone!
                                                        screenCapture.NullBitmap();
                                                    }
                                                }
                                                else if (screenPixels < gdiPixels)
                                                {
                                                    // Screen capture is cropped, window is outside of screen
                                                    if (blackPercentageGdi > 50 && blackPercentageGdi > blackPercentageScreen)
                                                    {
                                                        Log.Debug().WriteLine("Using screen capture, as GDI had additional black.");
                                                        // changeing the image will automatically dispose the previous
                                                        tmpCapture.Bitmap = screenCapture.Bitmap;
                                                        // Make sure it's not disposed, else the picture is gone!
                                                        screenCapture.NullBitmap();
                                                    }
                                                }
                                                else
                                                {
                                                    // Use the GDI capture by doing nothing
                                                    Log.Debug().WriteLine("This should not happen, how can there be more screen as GDI pixels?");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (tmpCapture != null)
                            {
                                captureForWindow = tmpCapture;
                                captureTaken = true;
                            }
                            else
                            {
                                // A problem, try Screen
                                windowCaptureMode = WindowCaptureModes.Screen;
                            }
                            break;
                        case WindowCaptureModes.Aero:
                        case WindowCaptureModes.AeroTransparent:
                            if (windowToCapture.IsApp() || WindowCapture.IsDwmAllowed(process))
                            {
                                tmpCapture = windowToCapture.CaptureDwmWindow(captureForWindow, windowCaptureMode, isAutoMode);
                            }
                            if (tmpCapture != null)
                            {
                                captureForWindow = tmpCapture;
                                captureTaken = true;
                            }
                            else
                            {
                                // A problem, try GDI
                                windowCaptureMode = WindowCaptureModes.GDI;
                            }
                            break;
                        default:
                            // Screen capture
                            if (windowToCapture.IsMinimized())
                            {
                                // Restore the window making sure it's visible!
                                windowToCapture.Restore();
                            }
                            else
                            {
                                // TODO: Await
                                windowToCapture.ToForegroundAsync();
                            }

                            try
                            {
                                captureForWindow = WindowCapture.CaptureRectangleFromDesktopScreen(captureForWindow, windowRectangle);
                                captureTaken = true;
                            }
                            catch (Exception e)
                            {
                                Log.Error().WriteLine(e, "Problem capturing");
                                return null;
                            }
                            break;
                    }
                }
            }

            if (captureForWindow != null)
            {
                captureForWindow.CaptureDetails.Title = windowToCapture.Text;
            }

            return captureForWindow;
        }

        private void SetDpi()
        {
            // Workaround for proble with DPI retrieval, the FromHwnd activates the window...
            var previouslyActiveWindow = InteropWindowQuery.GetActiveWindow();
            // Workaround for changed DPI settings in Windows 7
            using (var graphics = Graphics.FromHwnd(MainForm.Instance.Handle))
            {
                _capture.CaptureDetails.DpiX = graphics.DpiX;
                _capture.CaptureDetails.DpiY = graphics.DpiY;
            }
            // Set previouslyActiveWindow as foreground window
            previouslyActiveWindow?.ToForegroundAsync(false);
            if (_capture.CaptureDetails != null)
            {
                _capture.Bitmap?.SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
            }
        }

        #region capture with feedback

        private void CaptureWithFeedback()
        {
            // The following, to be precise the HideApp, causes the app to close as described in BUG-1620 
            // Added check for metro (Modern UI) apps, which might be maximized and cover the screen.

            //foreach(WindowDetails app in WindowDetails.GetMetroApps()) {
            //	if (app.Maximised) {
            //		app.HideApp();
            //	}
            //}

            using (var captureForm = new CaptureForm(_capture, _windows))
            {
                // Make sure the form is hidden after showing, even if an exception occurs, so all errors will be shown
                DialogResult result;
                try
                {
                    result = captureForm.ShowDialog(MainForm.Instance);
                }
                finally
                {
                    captureForm.Hide();
                    // Make sure it's gone
                    Application.DoEvents();
                }
                if (result != DialogResult.OK)
                {
                    return;
                }
                SelectedCaptureWindow = captureForm.SelectedCaptureWindow;
                _captureRect = captureForm.CaptureRectangle;
                // Get title
                if (SelectedCaptureWindow != null)
                {
                    _capture.CaptureDetails.Title = SelectedCaptureWindow.Text;
                }

                // Scroll test:
                var windowScroller = captureForm.WindowScroller;
                if (windowScroller != null)
                {
                    var scrollingCapture = new ScrollingCapture(windowScroller);

                    var resultImage = scrollingCapture.Capture();
                    if (resultImage != null)
                    {
                        _capture = new Capture(resultImage)
                        {
                            CaptureDetails = _capture.CaptureDetails
                        };
                        HandleCapture();
                        return;
                    }
                }

                if (_captureRect.Height * _captureRect.Width <= 0)
                {
                    return;
                }
                // Take the captureRect, this already is specified as bitmap coordinates
                _capture.Crop(_captureRect);

                // save for re-capturing later and show recapture context menu option
                // Important here is that the location needs to be offsetted back to screen coordinates!
                var tmpRectangle = _captureRect.Offset(_capture.ScreenBounds.Location.X, _capture.ScreenBounds.Location.Y);
                CoreConfig.LastCapturedRegion = tmpRectangle;
                HandleCapture();
            }
        }

        #endregion
    }
}