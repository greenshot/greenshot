/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Forms;

namespace Greenshot.Helpers
{
    /// <summary>
    /// CaptureHelper contains all the capture logic
    /// </summary>
    public class CaptureHelper : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CaptureHelper));

        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
        
        private List<WindowDetails> _windows = new();
        private WindowDetails _selectedCaptureWindow;
        private NativeRect _captureRect = NativeRect.Empty;
        private readonly bool _captureMouseCursor;
        private ICapture _capture;
        private CaptureMode _captureMode;
        private ScreenCaptureMode _screenCaptureMode = ScreenCaptureMode.Auto;

        /// <summary>
        /// The public accessible Dispose
        /// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)

        /// <summary>
        /// This Dispose is called from the Dispose and the Destructor.
        /// When disposing==true all non-managed resources should be freed too!
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup
            }

            // Unfortunately we can't dispose the capture, this might still be used somewhere else.
            _windows = null;
            _selectedCaptureWindow = null;
            _capture = null;
            // Empty working set after capturing
            if (CoreConfig.MinimizeWorkingSetSize)
            {
                PsApi.EmptyWorkingSet();
            }
        }

        public static void CaptureClipboard(IDestination destination = null)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Clipboard);
            if (destination != null)
            {
                captureHelper.AddDestination(destination);
            }

            captureHelper.MakeCapture();
        }

        public static void CaptureRegion(bool captureMouse)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse);
            captureHelper.MakeCapture();
        }

        public static void CaptureRegion(bool captureMouse, IDestination destination)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse, destination);
            captureHelper.MakeCapture();
        }

        public static void CaptureRegion(bool captureMouse, NativeRect region)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse);
            captureHelper.MakeCapture(region);
        }

        public static void CaptureFullscreen(bool captureMouse, ScreenCaptureMode screenCaptureMode)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.FullScreen, captureMouse)
            {
                _screenCaptureMode = screenCaptureMode
            };
            captureHelper.MakeCapture();
        }

        public static void CaptureLastRegion(bool captureMouse)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.LastRegion, captureMouse);
            captureHelper.MakeCapture();
        }

        public static void CaptureIe(bool captureMouse, WindowDetails windowToCapture)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.IE, captureMouse)
            {
                SelectedCaptureWindow = windowToCapture
            };
            captureHelper.MakeCapture();
        }

        public static void CaptureWindow(bool captureMouse)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.ActiveWindow, captureMouse);
            captureHelper.MakeCapture();
        }

        public static void CaptureWindow(WindowDetails windowToCapture)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.ActiveWindow)
            {
                SelectedCaptureWindow = windowToCapture
            };
            captureHelper.MakeCapture();
        }

        public static void CaptureWindowInteractive(bool captureMouse)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Window, captureMouse);
            captureHelper.MakeCapture();
        }

        public static void CaptureFile(string filename, IDestination destination = null)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.File);

            if (destination != null)
            {
                captureHelper.AddDestination(destination);
            }

            captureHelper.MakeCapture(filename);
        }

        public static void ImportCapture(ICapture captureToImport)
        {
            using CaptureHelper captureHelper = new CaptureHelper(CaptureMode.File)
            {
                _capture = captureToImport
            };
            captureHelper.HandleCapture();
        }

        public CaptureHelper AddDestination(IDestination destination)
        {
            _capture.CaptureDetails.AddDestination(destination);
            return this;
        }

        public CaptureHelper(CaptureMode captureMode)
        {
            _captureMode = captureMode;
            _capture = new Capture();
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
            _capture.CaptureDetails.AddDestination(destination);
        }

        public WindowDetails SelectedCaptureWindow
        {
            get => _selectedCaptureWindow;
            set => _selectedCaptureWindow = value;
        }

        private void DoCaptureFeedback()
        {
            if (CoreConfig.PlayCameraSound)
            {
                SoundHelper.Play();
            }
        }

        /// <summary>
        /// Make Capture with file name
        /// </summary>
        /// <param name="filename">filename</param>
        private void MakeCapture(string filename)
        {
            _capture.CaptureDetails.Filename = filename;
            MakeCapture();
        }

        /// <summary>
        /// Make Capture for region
        /// </summary>
        /// <param name="region">NativeRect</param>
        private void MakeCapture(NativeRect region)
        {
            _captureRect = region;
            MakeCapture();
        }


        /// <summary>
        /// Make Capture with specified destinations
        /// </summary>
        private void MakeCapture()
        {
            Thread retrieveWindowDetailsThread = null;

            // This fixes a problem when a balloon is still visible and a capture needs to be taken
            // forcefully removes the balloon!
            if (!CoreConfig.HideTrayicon)
            {
                var notifyIcon = SimpleServiceProvider.Current.GetInstance<NotifyIcon>();
                notifyIcon.Visible = false;
                notifyIcon.Visible = true;
            }

            Log.Debug($"Capturing with mode {_captureMode} and using Cursor {_captureMouseCursor}");
            _capture.CaptureDetails.CaptureMode = _captureMode;

            // Get the windows details in a separate thread, only for those captures that have a Feedback
            // As currently the "elements" aren't used, we don't need them yet
            switch (_captureMode)
            {
                case CaptureMode.Region:
                    // Check if a region is pre-supplied!
                    if (_captureRect.IsEmpty)
                    {
                        retrieveWindowDetailsThread = PrepareForCaptureWithFeedback();
                    }

                    break;
                case CaptureMode.Window:
                    retrieveWindowDetailsThread = PrepareForCaptureWithFeedback();
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

            // Capture Mouse cursor if we are not loading from file or clipboard, only show when needed
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
                    bool captureTaken = false;
                    switch (_screenCaptureMode)
                    {
                        case ScreenCaptureMode.Auto:
                            NativePoint mouseLocation = User32Api.GetCursorLocation();
                            foreach (Screen screen in Screen.AllScreens)
                            {
                                if (screen.Bounds.Contains(mouseLocation))
                                {
                                    _capture = WindowCapture.CaptureRectangle(_capture, screen.Bounds);
                                    captureTaken = true;
                                    // As the screen shot might be on a different monitor we need to correct the mouse location
                                    var correctedCursorLocation = _capture.CursorLocation.Offset(-screen.Bounds.Location.X, -screen.Bounds.Location.Y);
                                    _capture.CursorLocation = correctedCursorLocation;
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
                    // TODO: Fix getting image vs. drawablecontainer
                    Image clipboardImage = ClipboardHelper.GetImage();
                    if (clipboardImage != null)
                    {
                        if (_capture != null)
                        {
                            _capture.Image = clipboardImage;
                        }
                        else
                        {
                            _capture = new Capture(clipboardImage);
                        }

                        _capture.CaptureDetails.Title = "Clipboard";
                        _capture.CaptureDetails.AddMetaData("source", "Clipboard");
                        // Force Editor, keep picker
                        if (_capture.CaptureDetails.HasDestination(nameof(WellKnownDestinations.Picker)))
                        {
                            _capture.CaptureDetails.ClearDestinations();
                            _capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
                            _capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(nameof(WellKnownDestinations.Picker)));
                        }
                        else
                        {
                            _capture.CaptureDetails.ClearDestinations();
                            _capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
                        }

                        HandleCapture();
                    }
                    else
                    {
                        MessageBox.Show(Language.GetString("clipboard_noimage"));
                    }

                    break;
                case CaptureMode.File:
                    Image fileImage = null;
                    string filename = _capture.CaptureDetails.Filename;

                    if (!string.IsNullOrEmpty(filename))
                    {
                        // TODO: Fix that the Greenshot format needs a separate code path
                        try
                        {
                            if (filename.ToLower().EndsWith("." + OutputFormat.greenshot))
                            {
                                ISurface surface = new Surface();
                                surface = ImageIO.LoadGreenshotSurface(filename, surface);
                                surface.CaptureDetails = _capture.CaptureDetails;
                                DestinationHelper.GetDestination(EditorDestination.DESIGNATION).ExportCapture(true, surface, _capture.CaptureDetails);
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.Message, e);
                            MessageBox.Show(Language.GetFormattedString(LangKey.error_openfile, filename));
                        }

                        // TODO: Remove Image loading for here
                        try
                        {
                            fileImage = ImageIO.LoadImage(filename);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.Message, e);
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
                            _capture.Image = fileImage;
                        }
                        else
                        {
                            _capture = new Capture(fileImage);
                        }

                        // Force Editor, keep picker, this is currently the only usefull destination
                        if (_capture.CaptureDetails.HasDestination(nameof(WellKnownDestinations.Picker)))
                        {
                            _capture.CaptureDetails.ClearDestinations();
                            _capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
                            _capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(nameof(WellKnownDestinations.Picker)));
                        }
                        else
                        {
                            _capture.CaptureDetails.ClearDestinations();
                            _capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
                        }

                        HandleCapture();
                    }

                    break;
                case CaptureMode.LastRegion:
                    if (!CoreConfig.LastCapturedRegion.IsEmpty)
                    {
                        _capture = WindowCapture.CaptureRectangle(_capture, CoreConfig.LastCapturedRegion);
                        // TODO: Reactive / check if the elements code is activated
                        //if (windowDetailsThread != null) {
                        //    windowDetailsThread.Join();
                        //}

                        // Set capture title, fixing bug #3569703
                        foreach (WindowDetails window in WindowDetails.GetVisibleWindows())
                        {
                            NativePoint estimatedLocation = new NativePoint(CoreConfig.LastCapturedRegion.X + CoreConfig.LastCapturedRegion.Width / 2,
                                CoreConfig.LastCapturedRegion.Y + CoreConfig.LastCapturedRegion.Height / 2);
                            if (!window.Contains(estimatedLocation)) continue;
                            _selectedCaptureWindow = window;
                            _capture.CaptureDetails.Title = _selectedCaptureWindow.Text;
                            break;
                        }

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
                    if (_captureRect.IsEmpty)
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
                    Log.Warn("Unknown capture mode: " + _captureMode);
                    break;
            }

            // Wait for thread, otherwise we can't dispose the CaptureHelper
            retrieveWindowDetailsThread?.Join();
            if (_capture != null)
            {
                Log.Debug("Disposing capture");
                _capture.Dispose();
            }
        }

        /// <summary>
        /// Pre-Initialization for CaptureWithFeedback, this will get all the windows before we change anything
        /// </summary>
        private Thread PrepareForCaptureWithFeedback()
        {
            _windows = new List<WindowDetails>();

            Thread getWindowDetailsThread = new Thread(RetrieveWindowDetails)
            {
                Name = "Retrieve window details",
                IsBackground = true
            };
            getWindowDetailsThread.Start();
            return getWindowDetailsThread;
        }

        private void RetrieveWindowDetails()
        {
            Log.Debug("start RetrieveWindowDetails");
            // Start Enumeration of "active" windows
            foreach (var window in WindowDetails.GetVisibleWindows())
            {
                // Make sure the details are retrieved once
                window.FreezeDetails();

                // Force children retrieval, sometimes windows close on losing focus and this is solved by caching
                int goLevelDeep = 3;
                if (CoreConfig.WindowCaptureAllChildLocations)
                {
                    goLevelDeep = 20;
                }

                window.GetChildren(goLevelDeep);
                lock (_windows)
                {
                    _windows.Add(window);
                }
            }

            Log.Debug("end RetrieveWindowDetails");
        }

        private void AddConfiguredDestination()
        {
            foreach (string destinationDesignation in CoreConfig.OutputDestinations)
            {
                IDestination destination = DestinationHelper.GetDestination(destinationDesignation);
                if (destination != null)
                {
                    _capture.CaptureDetails.AddDestination(destination);
                }
            }
        }

        /// <summary>
        /// If a balloon tip is show for a taken capture, this handles the click on it
        /// </summary>
        /// <param name="eventArgs">SurfaceMessageEventArgs</param>
        private void OpenCaptureOnClick(SurfaceMessageEventArgs eventArgs)
        {
            ISurface surface = eventArgs.Surface;
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

            Log.DebugFormat("Deregistering the BalloonTipClicked");
        }

        /// <summary>
        /// This is the SurfaceMessageEvent receiver
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="eventArgs">SurfaceMessageEventArgs</param>
        private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs?.Message))
            {
                return;
            }

            var notifyIconClassicMessageHandler = SimpleServiceProvider.Current.GetInstance<INotificationService>();
            switch (eventArgs.MessageType)
            {
                case SurfaceMessageTyp.Error:
                    notifyIconClassicMessageHandler.ShowErrorMessage(eventArgs.Message, TimeSpan.FromHours(1));
                    break;
                case SurfaceMessageTyp.Info:
                    notifyIconClassicMessageHandler.ShowInfoMessage(eventArgs.Message, TimeSpan.FromHours(1), () => { Log.Info("Clicked!"); });
                    break;
                case SurfaceMessageTyp.FileSaved:
                case SurfaceMessageTyp.UploadedUri:
                    // Show a balloon and register an event handler to open the "capture" for if someone clicks the balloon.
                    notifyIconClassicMessageHandler.ShowInfoMessage(eventArgs.Message, TimeSpan.FromHours(1), () => OpenCaptureOnClick(eventArgs));
                    break;
            }
        }

        private void HandleCapture()
        {
            // Flag to see if the image was "exported" so the FileEditor doesn't
            // ask to save the file as long as nothing is done.
            bool outputMade = false;

            if (_capture.CaptureDetails.CaptureMode == CaptureMode.Text)
            {
                var selectionRectangle = new NativeRect(NativePoint.Empty, _capture.Image.Size);
                var ocrInfo = _capture.CaptureDetails.OcrInformation;
                if (ocrInfo != null)
                {
                    var textResult = new StringBuilder();
                    foreach (var line in ocrInfo.Lines)
                    {
                        var lineBounds = line.CalculatedBounds;
                        if (lineBounds.IsEmpty) continue;
                        // Highlight the text which is selected
                        if (!lineBounds.IntersectsWith(selectionRectangle)) continue;

                        for (var index = 0; index < line.Words.Length; index++)
                        {
                            var word = line.Words[index];
                            if (!word.Bounds.IntersectsWith(selectionRectangle)) continue;
                            textResult.Append(word.Text);

                            if (index + 1 < line.Words.Length && word.Text.Length > 1)
                            {
                                textResult.Append(' ');
                            }
                        }

                        textResult.AppendLine();
                    }

                    if (textResult.Length > 0)
                    {
                        Clipboard.SetText(textResult.ToString());
                    }
                }

                // Disable capturing
                _captureMode = CaptureMode.None;
                // Dispose the capture, we don't need it anymore (the surface copied all information and we got the title (if any)).
                _capture.Dispose();
                _capture = null;
                return;
            }

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
                    ((Bitmap) _capture.Image)?.SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
                }

                DoCaptureFeedback();
            }

            Log.Debug("A capture of: " + _capture.CaptureDetails.Title);

            // check if someone has passed a destination
            if (_capture.CaptureDetails.CaptureDestinations == null || _capture.CaptureDetails.CaptureDestinations.Count == 0)
            {
                AddConfiguredDestination();
            }

            // Create Surface with capture, this way elements can be added automatically (like the mouse cursor)
            Surface surface = new Surface(_capture)
            {
                Modified = !outputMade
            };

            // Register notify events if this is wanted
            if (CoreConfig.ShowTrayNotification && !CoreConfig.HideTrayicon)
            {
                surface.SurfaceMessage += SurfaceMessageReceived;
            }

            // Let the processors do their job
            foreach (var processor in SimpleServiceProvider.Current.GetAllInstances<IProcessor>())
            {
                if (!processor.isActive) continue;
                Log.InfoFormat("Calling processor {0}", processor.Description);
                processor.ProcessCapture(surface, _capture.CaptureDetails);
            }

            // As the surfaces copies the reference to the image, make sure the image is not being disposed (a trick to save memory)
            _capture.Image = null;

            // Get CaptureDetails as we need it even after the capture is disposed
            ICaptureDetails captureDetails = _capture.CaptureDetails;
            bool canDisposeSurface = true;

            if (captureDetails.HasDestination(nameof(WellKnownDestinations.Picker)))
            {
                DestinationHelper.ExportCapture(false, nameof(WellKnownDestinations.Picker), surface, captureDetails);
                captureDetails.CaptureDestinations.Clear();
                canDisposeSurface = false;
            }

            // Disable capturing
            _captureMode = CaptureMode.None;
            // Dispose the capture, we don't need it anymore (the surface copied all information and we got the title (if any)).
            _capture.Dispose();
            _capture = null;

            int destinationCount = captureDetails.CaptureDestinations.Count;
            if (destinationCount > 0)
            {
                // Flag to detect if we need to create a temp file for the email
                // or use the file that was written
                foreach (IDestination destination in captureDetails.CaptureDestinations)
                {
                    if (nameof(WellKnownDestinations.Picker).Equals(destination.Designation))
                    {
                        continue;
                    }

                    Log.InfoFormat("Calling destination {0}", destination.Description);

                    ExportInformation exportInformation = destination.ExportCapture(false, surface, captureDetails);
                    if (EditorDestination.DESIGNATION.Equals(destination.Designation) && exportInformation.ExportMade)
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
            bool presupplied = false;
            Log.Debug("CaptureActiveWindow");
            if (_selectedCaptureWindow != null)
            {
                Log.Debug("Using supplied window");
                presupplied = true;
            }
            else
            {
                _selectedCaptureWindow = WindowDetails.GetActiveWindow();
                if (_selectedCaptureWindow != null)
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.DebugFormat("Capturing window: {0} with {1}", _selectedCaptureWindow.Text, _selectedCaptureWindow.WindowRectangle);
                    }
                }
            }

            if (_selectedCaptureWindow == null || (!presupplied && _selectedCaptureWindow.Iconic))
            {
                Log.Warn("No window to capture!");
                // Nothing to capture, code up in the stack will capture the full screen
                return false;
            }

            if (!presupplied && _selectedCaptureWindow != null && _selectedCaptureWindow.Iconic)
            {
                // Restore the window making sure it's visible!
                // This is done mainly for a screen capture, but some applications like Excel and TOAD have weird behaviour!
                _selectedCaptureWindow.Restore();
            }

            _selectedCaptureWindow = SelectCaptureWindow(_selectedCaptureWindow);
            if (_selectedCaptureWindow == null)
            {
                Log.Warn("No window to capture, after SelectCaptureWindow!");
                // Nothing to capture, code up in the stack will capture the full screen
                return false;
            }

            // Fix for Bug #3430560
            CoreConfig.LastCapturedRegion = _selectedCaptureWindow.WindowRectangle;
            bool returnValue = CaptureWindow(_selectedCaptureWindow, _capture, CoreConfig.WindowCaptureMode) != null;
            return returnValue;
        }

        /// <summary>
        /// Select the window to capture, this has logic which takes care of certain special applications
        /// like TOAD or Excel
        /// </summary>
        /// <param name="windowToCapture">WindowDetails with the target Window</param>
        /// <returns>WindowDetails with the target Window OR a replacement</returns>
        public static WindowDetails SelectCaptureWindow(WindowDetails windowToCapture)
        {
            NativeRect windowRectangle = windowToCapture.WindowRectangle;
            if (windowRectangle.Width == 0 || windowRectangle.Height == 0)
            {
                Log.WarnFormat("Window {0} has nothing to capture, using workaround to find other window of same process.", windowToCapture.Text);
                // Trying workaround, the size 0 arises with e.g. Toad.exe, has a different Window when minimized
                WindowDetails linkedWindow = WindowDetails.GetLinkedWindow(windowToCapture);
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
        /// Check if Process uses PresentationFramework.dll -> meaning it uses WPF
        /// </summary>
        /// <param name="process">Process to check for the presentation framework</param>
        /// <returns>true if the process uses WPF</returns>
        private static bool IsWpf(Process process)
        {
            if (process == null)
            {
                return false;
            }
            try
            {
                foreach (ProcessModule module in process.Modules)
                {
                    if (!module.ModuleName.StartsWith("PresentationFramework"))
                    {
                        continue;
                    }
                    Log.InfoFormat("Found that Process {0} uses {1}, assuming it's using WPF", process.ProcessName, module.FileName);
                    return true;
                }
            }
            catch (Exception)
            {
                // Access denied on the modules
                Log.WarnFormat("No access on the modules from process {0}, assuming WPF is used.", process.ProcessName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Capture the supplied Window
        /// </summary>
        /// <param name="windowToCapture">Window to capture</param>
        /// <param name="captureForWindow">The capture to store the details</param>
        /// <param name="windowCaptureMode">What WindowCaptureMode to use</param>
        /// <returns>ICapture</returns>
        public static ICapture CaptureWindow(WindowDetails windowToCapture, ICapture captureForWindow, WindowCaptureMode windowCaptureMode)
        {
            if (captureForWindow == null)
            {
                captureForWindow = new Capture();
            }

            NativeRect windowRectangle = windowToCapture.WindowRectangle;

            // When Vista & DWM (Aero) enabled
            bool dwmEnabled = DwmApi.IsDwmEnabled;
            // get process name to be able to exclude certain processes from certain capture modes
            using (Process process = windowToCapture.Process)
            {
                bool isAutoMode = windowCaptureMode == WindowCaptureMode.Auto;
                // For WindowCaptureMode.Auto we check:
                // 1) Is window IE, use IE Capture
                // 2) Is Windows >= Vista & DWM enabled: use DWM
                // 3) Otherwise use GDI (Screen might be also okay but might lose content)
                if (isAutoMode)
                {
                    if (CoreConfig.IECapture && IeCaptureHelper.IsIeWindow(windowToCapture))
                    {
                        try
                        {
                            ICapture ieCapture = IeCaptureHelper.CaptureIe(captureForWindow, windowToCapture);
                            if (ieCapture != null)
                            {
                                return ieCapture;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WarnFormat("Problem capturing IE, skipping to normal capture. Exception message was: {0}", ex.Message);
                        }
                    }

                    // Take default screen
                    windowCaptureMode = WindowCaptureMode.Screen;

                    // In https://github.com/greenshot/greenshot/issues/373 it was shown that PrintWindow (GDI) works great with Windows 11
                    // In https://github.com/greenshot/greenshot/issues/658 is was made clear it DOESN'T!
                    // Change to GDI, if allowed
                    if (WindowCapture.IsGdiAllowed(process))
                    {
                        if (!dwmEnabled && IsWpf(process))
                        {
                            // do not use GDI, as DWM is not enabled and the application uses PresentationFramework.dll -> isWPF
                            Log.InfoFormat("Not using GDI for windows of process {0}, as the process uses WPF", process.ProcessName);
                        }
                        else
                        {
                            windowCaptureMode = WindowCaptureMode.GDI;
                        }
                    }

                    // Change to DWM, if enabled and allowed
                    if (dwmEnabled)
                    {
                        if (WindowCapture.IsDwmAllowed(process))
                        {
                            windowCaptureMode = WindowCaptureMode.Aero;
                        }
                    }
                }
                else if (windowCaptureMode == WindowCaptureMode.Aero || windowCaptureMode == WindowCaptureMode.AeroTransparent)
                {
                    if (!dwmEnabled || !WindowCapture.IsDwmAllowed(process))
                    {
                        // Take default screen
                        windowCaptureMode = WindowCaptureMode.Screen;
                        // Change to GDI, if allowed
                        if (WindowCapture.IsGdiAllowed(process))
                        {
                            windowCaptureMode = WindowCaptureMode.GDI;
                        }
                    }
                }
                else if (windowCaptureMode == WindowCaptureMode.GDI && !WindowCapture.IsGdiAllowed(process))
                {
                    // GDI not allowed, take screen
                    windowCaptureMode = WindowCaptureMode.Screen;
                }

                Log.InfoFormat("Capturing window with mode {0}", windowCaptureMode);
                bool captureTaken = false;
                windowRectangle = windowRectangle.Intersect(captureForWindow.ScreenBounds);
                // Try to capture
                while (!captureTaken)
                {
                    ICapture tmpCapture = null;
                    switch (windowCaptureMode)
                    {
                        case WindowCaptureMode.GDI:
                            if (WindowCapture.IsGdiAllowed(process))
                            {
                                if (windowToCapture.Iconic)
                                {
                                    // Restore the window making sure it's visible!
                                    windowToCapture.Restore();
                                }
                                else
                                {
                                    windowToCapture.ToForeground();
                                }

                                tmpCapture = windowToCapture.CaptureGdiWindow(captureForWindow);
                                if (tmpCapture != null)
                                {
                                    // check if GDI capture any good, by comparing it with the screen content
                                    int blackCountGdi = ImageHelper.CountColor(tmpCapture.Image, Color.Black, false);
                                    int gdiPixels = tmpCapture.Image.Width * tmpCapture.Image.Height;
                                    int blackPercentageGdi = blackCountGdi * 100 / gdiPixels;
                                    if (blackPercentageGdi >= 1)
                                    {
                                        int screenPixels = windowRectangle.Width * windowRectangle.Height;
                                        using ICapture screenCapture = new Capture
                                        {
                                            CaptureDetails = captureForWindow.CaptureDetails
                                        };
                                        if (WindowCapture.CaptureRectangleFromDesktopScreen(screenCapture, windowRectangle) != null)
                                        {
                                            int blackCountScreen = ImageHelper.CountColor(screenCapture.Image, Color.Black, false);
                                            int blackPercentageScreen = blackCountScreen * 100 / screenPixels;
                                            if (screenPixels == gdiPixels)
                                            {
                                                // "easy compare", both have the same size
                                                // If GDI has more black, use the screen capture.
                                                if (blackPercentageGdi > blackPercentageScreen)
                                                {
                                                    Log.Debug("Using screen capture, as GDI had additional black.");
                                                    // changeing the image will automatically dispose the previous
                                                    tmpCapture.Image = screenCapture.Image;
                                                    // Make sure it's not disposed, else the picture is gone!
                                                    screenCapture.NullImage();
                                                }
                                            }
                                            else if (screenPixels < gdiPixels)
                                            {
                                                // Screen capture is cropped, window is outside of screen
                                                if (blackPercentageGdi > 50 && blackPercentageGdi > blackPercentageScreen)
                                                {
                                                    Log.Debug("Using screen capture, as GDI had additional black.");
                                                    // changeing the image will automatically dispose the previous
                                                    tmpCapture.Image = screenCapture.Image;
                                                    // Make sure it's not disposed, else the picture is gone!
                                                    screenCapture.NullImage();
                                                }
                                            }
                                            else
                                            {
                                                // Use the GDI capture by doing nothing
                                                Log.Debug("This should not happen, how can there be more screen as GDI pixels?");
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
                                windowCaptureMode = WindowCaptureMode.Screen;
                            }

                            break;
                        case WindowCaptureMode.Aero:
                        case WindowCaptureMode.AeroTransparent:
                            if (WindowCapture.IsDwmAllowed(process))
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
                                windowCaptureMode = WindowCaptureMode.GDI;
                            }

                            break;
                        default:
                            // Screen capture
                            if (windowToCapture.Iconic)
                            {
                                // Restore the window making sure it's visible!
                                windowToCapture.Restore();
                            }
                            else
                            {
                                windowToCapture.ToForeground();
                            }

                            try
                            {
                                captureForWindow = WindowCapture.CaptureRectangleFromDesktopScreen(captureForWindow, windowRectangle);
                                captureTaken = true;
                            }
                            catch (Exception e)
                            {
                                Log.Error("Problem capturing", e);
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
            // Workaround for problem with DPI retrieval, the FromHwnd activates the window...
            WindowDetails previouslyActiveWindow = WindowDetails.GetActiveWindow();
            // Workaround for changed DPI settings in Windows 7
            var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>();
            using (Graphics graphics = Graphics.FromHwnd(mainForm.Handle))
            {
                _capture.CaptureDetails.DpiX = graphics.DpiX;
                _capture.CaptureDetails.DpiY = graphics.DpiY;
            }

            // Set previouslyActiveWindow as foreground window
            previouslyActiveWindow?.ToForeground();
            if (_capture.CaptureDetails != null)
            {
                ((Bitmap) _capture.Image)?.SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
            }
        }

        private void CaptureWithFeedback()
        {
            using CaptureForm captureForm = new CaptureForm(_capture, _windows);
            // Make sure the form is hidden after showing, even if an exception occurs, so all errors will be shown
            DialogResult result;
            try
            {
                var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>();
                result = captureForm.ShowDialog(mainForm);
            }
            finally
            {
                captureForm.Hide();
            }

            if (result != DialogResult.OK) return;

            _selectedCaptureWindow = captureForm.SelectedCaptureWindow;
            _captureRect = captureForm.CaptureRectangle;
            // Get title
            if (_selectedCaptureWindow != null)
            {
                _capture.CaptureDetails.Title = _selectedCaptureWindow.Text;
            }

            if (_captureRect.Height > 0 && _captureRect.Width > 0)
            {
                // Take the captureRect, this already is specified as bitmap coordinates
                _capture.Crop(_captureRect);

                // save for re-capturing later and show recapture context menu option
                // Important here is that the location needs to be offsetted back to screen coordinates!
                NativeRect tmpRectangle = _captureRect.Offset(_capture.ScreenBounds.Location.X, _capture.ScreenBounds.Location.Y);
                CoreConfig.LastCapturedRegion = tmpRectangle;
            }

            HandleCapture();
        }
    }
}