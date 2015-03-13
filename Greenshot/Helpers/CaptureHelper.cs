/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Greenshot.Configuration;
using Greenshot.Destinations;
using Greenshot.Drawing;
using Greenshot.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.UnmanagedHelpers;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Greenshot.Helpers {
	/// <summary>
	/// CaptureHelper contains all the capture logic 
	/// </summary>
	public class CaptureHelper : IDisposable {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(CaptureHelper));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		// TODO: when we get the screen capture code working correctly, this needs to be enabled
		//private static ScreenCaptureHelper screenCapture = null;
		private List<WindowDetails> _windows = new List<WindowDetails>();
		private WindowDetails _selectedCaptureWindow;
		private Rectangle _captureRect = Rectangle.Empty;
		private readonly bool _captureMouseCursor;
		private ICapture _capture;
		private CaptureMode _captureMode;
		private ScreenCaptureMode _screenCaptureMode = ScreenCaptureMode.Auto;

		/// <summary>
		/// The public accessible Dispose
		/// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/// <summary>
		/// This Dispose is called from the Dispose and the Destructor.
		/// When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				// Cleanup
			}
			// Unfortunately we can't dispose the capture, this might still be used somewhere else.
			_windows = null;
			_selectedCaptureWindow = null;
			_capture = null;
			// Empty working set after capturing
			if (conf.MinimizeWorkingSetSize) {
				PsAPI.EmptyWorkingSet();
			}
		}
		public static void CaptureClipboard() {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Clipboard)) {
				captureHelper.MakeCapture();
			}
		}
		public static void CaptureRegion(bool captureMouse) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse)) {
				captureHelper.MakeCapture();
			}
		}
		public static void CaptureRegion(bool captureMouse, IDestination destination) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse, destination)) {
				captureHelper.MakeCapture();			
			}
		}
		public static void CaptureRegion(bool captureMouse, Rectangle region) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse)) {
				captureHelper.MakeCapture(region);
			}
		}
		public static void CaptureFullscreen(bool captureMouse, ScreenCaptureMode screenCaptureMode) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.FullScreen, captureMouse)) {
				captureHelper._screenCaptureMode = screenCaptureMode;
				captureHelper.MakeCapture();
			}
		}
		public static void CaptureLastRegion(bool captureMouse) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.LastRegion, captureMouse)) {
				captureHelper.MakeCapture();
			}
		}

		public static void CaptureIE(bool captureMouse, WindowDetails windowToCapture) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.IE, captureMouse)) {
				captureHelper.SelectedCaptureWindow = windowToCapture;
				captureHelper.MakeCapture();
			}
		}

		public static void CaptureWindow(bool captureMouse) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.ActiveWindow, captureMouse)) {
				captureHelper.MakeCapture();
			}
		}

		public static void CaptureWindow(WindowDetails windowToCapture) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.ActiveWindow)) {
				captureHelper.SelectedCaptureWindow = windowToCapture;
				captureHelper.MakeCapture();
			}
		}

		public static void CaptureWindowInteractive(bool captureMouse) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Window)) {
				captureHelper.MakeCapture();
			}
		}

		public static void CaptureFile(string filename) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.File)) {
				captureHelper.MakeCapture(filename);
			}
		}

		public static void CaptureFile(string filename, IDestination destination) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.File)) {
				captureHelper.AddDestination(destination).MakeCapture(filename);
			}
		}

		public static void ImportCapture(ICapture captureToImport) {
			using (CaptureHelper captureHelper = new CaptureHelper(CaptureMode.File)) {
				captureHelper._capture = captureToImport;
				captureHelper.HandleCapture();
			}
		}

		public CaptureHelper AddDestination(IDestination destination) {
			_capture.CaptureDetails.AddDestination(destination);
			return this;
		}

		public CaptureHelper(CaptureMode captureMode) {
			_captureMode = captureMode;
			_capture = new Capture();			
		}

		public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor) : this(captureMode) {
			_captureMouseCursor = captureMouseCursor;
		}

		public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, ScreenCaptureMode screenCaptureMode) : this(captureMode) {
			_captureMouseCursor = captureMouseCursor;
			_screenCaptureMode = screenCaptureMode;
		}

		public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, IDestination destination) : this(captureMode, captureMouseCursor) {
			_capture.CaptureDetails.AddDestination(destination);
		}
		
		public WindowDetails SelectedCaptureWindow {
			get {
				return _selectedCaptureWindow;
			}
			set {
				_selectedCaptureWindow = value;
			}
		}
		
		private void DoCaptureFeedback() {
			if(conf.PlayCameraSound) {
				SoundHelper.Play();
			}
		}

		/// <summary>
		/// Make Capture with file name
		/// </summary>
		/// <param name="filename">filename</param>
		private void MakeCapture(string filename) {
			_capture.CaptureDetails.Filename = filename;
			MakeCapture();
		}

		/// <summary>
		/// Make Capture for region
		/// </summary>
		/// <param name="filename">filename</param>
		private void MakeCapture(Rectangle region) {
			_captureRect = region;
			MakeCapture();
		}


		/// <summary>
		/// Make Capture with specified destinations
		/// </summary>
		private void MakeCapture() {
			Thread retrieveWindowDetailsThread = null;

			// This fixes a problem when a balloon is still visible and a capture needs to be taken
			// forcefully removes the balloon!
			if (!conf.HideTrayicon) {
				MainForm.Instance.NotifyIcon.Visible = false;
				MainForm.Instance.NotifyIcon.Visible = true;
			}
			LOG.Debug(String.Format("Capturing with mode {0} and using Cursor {1}", _captureMode, _captureMouseCursor));
			_capture.CaptureDetails.CaptureMode = _captureMode;

			// Get the windows details in a seperate thread, only for those captures that have a Feedback
			// As currently the "elements" aren't used, we don't need them yet
			switch (_captureMode) {
				case CaptureMode.Region:
					// Check if a region is pre-supplied!
					if (Rectangle.Empty.Equals(_captureRect)) {
						retrieveWindowDetailsThread = PrepareForCaptureWithFeedback();
					}
					break;
				case CaptureMode.Window:
					retrieveWindowDetailsThread = PrepareForCaptureWithFeedback();
					break;
			}

			// Add destinations if no-one passed a handler
			if (_capture.CaptureDetails.CaptureDestinations == null || _capture.CaptureDetails.CaptureDestinations.Count == 0) {
				AddConfiguredDestination();
			}

			// Delay for the Context menu
			if (conf.CaptureDelay > 0) {
				Thread.Sleep(conf.CaptureDelay);
			} else {
				conf.CaptureDelay = 0;
			}

			// Capture Mousecursor if we are not loading from file or clipboard, only show when needed
			if (_captureMode != CaptureMode.File && _captureMode != CaptureMode.Clipboard) {
				_capture = WindowCapture.CaptureCursor(_capture);
				if (_captureMouseCursor) {
					_capture.CursorVisible = conf.CaptureMousepointer;
				} else {
					_capture.CursorVisible = false;					
				}
			}

			switch(_captureMode) {
				case CaptureMode.Window:
					_capture = WindowCapture.CaptureScreen(_capture);
					_capture.CaptureDetails.AddMetaData("source", "Screen");
					SetDPI();
					CaptureWithFeedback();
					break;
				case CaptureMode.ActiveWindow:
					if (CaptureActiveWindow()) {
						// Capture worked, offset mouse according to screen bounds and capture location
						_capture.MoveMouseLocation(_capture.ScreenBounds.Location.X-_capture.Location.X, _capture.ScreenBounds.Location.Y-_capture.Location.Y);
						_capture.CaptureDetails.AddMetaData("source", "Window");
					} else {
						_captureMode = CaptureMode.FullScreen;
						_capture = WindowCapture.CaptureScreen(_capture);
						_capture.CaptureDetails.AddMetaData("source", "Screen");
						_capture.CaptureDetails.Title = "Screen";
					}
					SetDPI();
					HandleCapture();
					break;
				case CaptureMode.IE:
					if (IECaptureHelper.CaptureIE(_capture, SelectedCaptureWindow) != null) {
						_capture.CaptureDetails.AddMetaData("source", "Internet Explorer");
						SetDPI();
						HandleCapture();
					}
					break;
				case CaptureMode.FullScreen:
					// Check how we need to capture the screen
					bool captureTaken = false;
					switch (_screenCaptureMode) {
						case ScreenCaptureMode.Auto:
							Point mouseLocation = User32.GetCursorLocation();
							foreach (Screen screen in Screen.AllScreens) {
								if (screen.Bounds.Contains(mouseLocation)) {
									_capture = WindowCapture.CaptureRectangle(_capture, screen.Bounds);
									captureTaken = true;
									break;
								}
							}
							break;
						case ScreenCaptureMode.Fixed:
							if (conf.ScreenToCapture > 0 && conf.ScreenToCapture <= Screen.AllScreens.Length) {
								_capture = WindowCapture.CaptureRectangle(_capture, Screen.AllScreens[conf.ScreenToCapture].Bounds);
								captureTaken = true;
							}
							break;
						case ScreenCaptureMode.FullScreen:
							// Do nothing, we take the fullscreen capture automatically
							break;
					}
					if (!captureTaken) {
						_capture = WindowCapture.CaptureScreen(_capture);
					}
					SetDPI();
					HandleCapture();
					break;
				case CaptureMode.Clipboard:
					Image clipboardImage = ClipboardHelper.GetImage();
					if (clipboardImage != null) {
						if (_capture != null) {
							_capture.Image = clipboardImage;
						} else {
							_capture = new Capture(clipboardImage);
						}
						_capture.CaptureDetails.Title = "Clipboard";
						_capture.CaptureDetails.AddMetaData("source", "Clipboard");
						// Force Editor, keep picker
						if (_capture.CaptureDetails.HasDestination(PickerDestination.DESIGNATION)) {
							_capture.CaptureDetails.ClearDestinations();
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(PickerDestination.DESIGNATION));
						} else {
							_capture.CaptureDetails.ClearDestinations();
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
						}
						HandleCapture();
					} else {
						MessageBox.Show(Language.GetString("clipboard_noimage"));
					}
					break;
				case CaptureMode.File:
					Image fileImage = null;
					string filename = _capture.CaptureDetails.Filename;

					if (!string.IsNullOrEmpty(filename)) {
						try {
							if (filename.ToLower().EndsWith("." + OutputFormat.greenshot)) {
								ISurface surface = new Surface();
								surface = ImageOutput.LoadGreenshotSurface(filename, surface);
								surface.CaptureDetails = _capture.CaptureDetails;
								DestinationHelper.GetDestination(EditorDestination.DESIGNATION).ExportCapture(true, surface, _capture.CaptureDetails);
								break;
							}
						} catch (Exception e) {
							LOG.Error(e.Message, e);
							MessageBox.Show(Language.GetFormattedString(LangKey.error_openfile, filename));
						}
						try {
							fileImage = ImageHelper.LoadImage(filename);
						} catch (Exception e) {
							LOG.Error(e.Message, e);
							MessageBox.Show(Language.GetFormattedString(LangKey.error_openfile, filename));
						}
					}
					if (fileImage != null) {
						_capture.CaptureDetails.Title = Path.GetFileNameWithoutExtension(filename);
						_capture.CaptureDetails.AddMetaData("file", filename);
						_capture.CaptureDetails.AddMetaData("source", "file");
						if (_capture != null) {
							_capture.Image = fileImage;
						} else {
							_capture = new Capture(fileImage);
						}
						// Force Editor, keep picker, this is currently the only usefull destination
						if (_capture.CaptureDetails.HasDestination(PickerDestination.DESIGNATION)) {
							_capture.CaptureDetails.ClearDestinations();
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(PickerDestination.DESIGNATION));
						} else {
							_capture.CaptureDetails.ClearDestinations();
							_capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
						}
						HandleCapture();
					}
					break;
				case CaptureMode.LastRegion:
					if (!conf.LastCapturedRegion.IsEmpty) {
						_capture = WindowCapture.CaptureRectangle(_capture, conf.LastCapturedRegion);
						// TODO: Reactive / check if the elements code is activated
						//if (windowDetailsThread != null) {
						//	windowDetailsThread.Join();
						//}

						// Set capture title, fixing bug #3569703
						foreach (WindowDetails window in WindowDetails.GetVisibleWindows()) {
							Point estimatedLocation = new Point(conf.LastCapturedRegion.X + (conf.LastCapturedRegion.Width / 2), conf.LastCapturedRegion.Y + (conf.LastCapturedRegion.Height / 2));
							if (window.Contains(estimatedLocation)) {
								_selectedCaptureWindow = window;
								_capture.CaptureDetails.Title = _selectedCaptureWindow.Text;
								break;
							}
						}
						// Move cursor, fixing bug #3569703
						_capture.MoveMouseLocation(_capture.ScreenBounds.Location.X - _capture.Location.X, _capture.ScreenBounds.Location.Y - _capture.Location.Y);
						//capture.MoveElements(capture.ScreenBounds.Location.X - capture.Location.X, capture.ScreenBounds.Location.Y - capture.Location.Y);

						_capture.CaptureDetails.AddMetaData("source", "screen");
						SetDPI();
						HandleCapture();
					}
					break;
				case CaptureMode.Region:
					// Check if a region is pre-supplied!
					if (Rectangle.Empty.Equals(_captureRect)) {
						_capture = WindowCapture.CaptureScreen(_capture);
						_capture.CaptureDetails.AddMetaData("source", "screen");
						SetDPI();
						CaptureWithFeedback();
					} else {
						_capture = WindowCapture.CaptureRectangle(_capture, _captureRect);
						_capture.CaptureDetails.AddMetaData("source", "screen");
						SetDPI();
						HandleCapture();
					}
					break;
				default:
					LOG.Warn("Unknown capture mode: " + _captureMode);
					break;
			}
			// Wait for thread, otherwise we can't dipose the CaptureHelper
			if (retrieveWindowDetailsThread != null) {
				retrieveWindowDetailsThread.Join();
			}
			if (_capture != null) {
				LOG.Debug("Disposing capture");
				_capture.Dispose();
			}
		}
				
		/// <summary>
		/// Pre-Initialization for CaptureWithFeedback, this will get all the windows before we change anything
		/// </summary>
		private Thread PrepareForCaptureWithFeedback() {
			_windows = new List<WindowDetails>();
			
			// If the App Launcher is visisble, no other windows are active
			WindowDetails appLauncherWindow = WindowDetails.GetAppLauncher();
			if (appLauncherWindow != null && appLauncherWindow.Visible) {
				_windows.Add(appLauncherWindow);
				return null;
			}

			Thread getWindowDetailsThread = new Thread(RetrieveWindowDetails);
			getWindowDetailsThread.Name = "Retrieve window details";
			getWindowDetailsThread.IsBackground = true;
			getWindowDetailsThread.Start();
			return getWindowDetailsThread;
		}

		private void RetrieveWindowDetails() {
			LOG.Debug("start RetrieveWindowDetails");
			// Start Enumeration of "active" windows
			List<WindowDetails> allWindows = WindowDetails.GetMetroApps();
			allWindows.AddRange(WindowDetails.GetAllWindows());
			foreach (WindowDetails window in allWindows) {
				// Window should be visible and not ourselves
				if (!window.Visible) {
					continue;
				}

				// Skip empty 
				Rectangle windowRectangle = window.WindowRectangle;
				Size windowSize = windowRectangle.Size;
				if (windowSize.Width == 0 || windowSize.Height == 0) {
					continue;
				}

				// Make sure the details are retrieved once
				window.FreezeDetails();

				// Force children retrieval, sometimes windows close on losing focus and this is solved by caching
				int goLevelDeep = 3;
				if (conf.WindowCaptureAllChildLocations) {
					goLevelDeep = 20;
				}
				window.GetChildren(goLevelDeep);
				lock (_windows) {
					_windows.Add(window);
				}
			}
			LOG.Debug("end RetrieveWindowDetails");
		}

		private void AddConfiguredDestination() {
			foreach(string destinationDesignation in conf.OutputDestinations) {
				IDestination destination = DestinationHelper.GetDestination(destinationDesignation);
				if (destination != null) {
					_capture.CaptureDetails.AddDestination(destination);
				}
			}
		}

		/// <summary>
		/// If a balloon tip is show for a taken capture, this handles the click on it
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OpenCaptureOnClick(object sender, EventArgs e) {
			SurfaceMessageEventArgs eventArgs = MainForm.Instance.NotifyIcon.Tag as SurfaceMessageEventArgs;
			if (eventArgs == null) {
				LOG.Warn("OpenCaptureOnClick called without SurfaceMessageEventArgs");
				RemoveEventHandler(sender, e);
				return;
			}
			ISurface surface = eventArgs.Surface;
			if (surface != null && eventArgs.MessageType == SurfaceMessageTyp.FileSaved) {
				if (!string.IsNullOrEmpty(surface.LastSaveFullPath)) {
					string errorMessage = null;

					try {
						ProcessStartInfo psi = new ProcessStartInfo("explorer.exe");
						psi.Arguments = Path.GetDirectoryName(surface.LastSaveFullPath);
						psi.UseShellExecute = false;
						using (Process p = new Process()) {
							p.StartInfo = psi;
							p.Start();
						}
					} catch (Exception ex) {
						errorMessage = ex.Message;
					}
					// Added fallback for when the explorer can't be found
					if (errorMessage != null) {
						try {
							string windowsPath = Environment.GetEnvironmentVariable("SYSTEMROOT");
							string explorerPath = Path.Combine(windowsPath, "explorer.exe");
							if (File.Exists(explorerPath)) {
								ProcessStartInfo psi = new ProcessStartInfo(explorerPath);
								psi.Arguments = Path.GetDirectoryName(surface.LastSaveFullPath);
								psi.UseShellExecute = false;
								using (Process p = new Process()) {
									p.StartInfo = psi;
									p.Start();
								}
								errorMessage = null;
							}
						} catch {
						}
					}
					if (errorMessage != null) {
						MessageBox.Show(string.Format("{0}\r\nexplorer.exe {1}", errorMessage, surface.LastSaveFullPath), "explorer.exe", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			} else if (surface != null && !string.IsNullOrEmpty(surface.UploadURL)) {
				Process.Start(surface.UploadURL);
			}
			LOG.DebugFormat("Deregistering the BalloonTipClicked");
			RemoveEventHandler(sender, e);
		}

		private void RemoveEventHandler(object sender, EventArgs e) {
			MainForm.Instance.NotifyIcon.BalloonTipClicked -= OpenCaptureOnClick;
			MainForm.Instance.NotifyIcon.BalloonTipClosed -= RemoveEventHandler;
			MainForm.Instance.NotifyIcon.Tag = null;
		}

		/// <summary>
		/// This is the SufraceMessageEvent receiver
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs) {
			if (eventArgs == null || string.IsNullOrEmpty(eventArgs.Message)) {
				return;
			}
			switch (eventArgs.MessageType) {
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


		private void HandleCapture() {
			// Flag to see if the image was "exported" so the FileEditor doesn't
			// ask to save the file as long as nothing is done.
			bool outputMade = false;

			// Make sure the user sees that the capture is made
			if (_capture.CaptureDetails.CaptureMode == CaptureMode.File || _capture.CaptureDetails.CaptureMode == CaptureMode.Clipboard) {
				// Maybe not "made" but the original is still there... somehow
				outputMade = true;
			} else {
				// Make sure the resolution is set correctly!
				if (_capture.CaptureDetails != null && _capture.Image != null) {
					((Bitmap)_capture.Image).SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
				}
				DoCaptureFeedback();
			}

			LOG.Debug("A capture of: " + _capture.CaptureDetails.Title);

			// check if someone has passed a destination
			if (_capture.CaptureDetails.CaptureDestinations == null || _capture.CaptureDetails.CaptureDestinations.Count == 0) {
				AddConfiguredDestination();
			}

			// Create Surface with capture, this way elements can be added automatically (like the mouse cursor)
			Surface surface = new Surface(_capture);
			surface.Modified = !outputMade;

			// Register notify events if this is wanted			
			if (conf.ShowTrayNotification && !conf.HideTrayicon) {
				surface.SurfaceMessage += SurfaceMessageReceived;
				
			}
			// Let the processors do their job
			foreach(IProcessor processor in ProcessorHelper.GetAllProcessors()) {
				if (processor.isActive) {
					LOG.InfoFormat("Calling processor {0}", processor.Description);
					processor.ProcessCapture(surface, _capture.CaptureDetails);
				}
			}
			
			// As the surfaces copies the reference to the image, make sure the image is not being disposed (a trick to save memory)
			_capture.Image = null;

			// Get CaptureDetails as we need it even after the capture is disposed
			ICaptureDetails captureDetails = _capture.CaptureDetails;
			bool canDisposeSurface = true;

			if (captureDetails.HasDestination(PickerDestination.DESIGNATION)) {
				DestinationHelper.ExportCapture(false, PickerDestination.DESIGNATION, surface, captureDetails);
				captureDetails.CaptureDestinations.Clear();
				canDisposeSurface = false;
			}

			// Disable capturing
			_captureMode = CaptureMode.None;
			// Dispose the capture, we don't need it anymore (the surface copied all information and we got the title (if any)).
			_capture.Dispose();
			_capture = null;

			int destinationCount = captureDetails.CaptureDestinations.Count;
			if (destinationCount > 0) {
				// Flag to detect if we need to create a temp file for the email
				// or use the file that was written
				foreach(IDestination destination in captureDetails.CaptureDestinations) {
					if (PickerDestination.DESIGNATION.Equals(destination.Designation)) {
						continue;
					}
					LOG.InfoFormat("Calling destination {0}", destination.Description);

					ExportInformation exportInformation = destination.ExportCapture(false, surface, captureDetails);
					if (EditorDestination.DESIGNATION.Equals(destination.Designation) && exportInformation.ExportMade) {
						canDisposeSurface = false;
					}
				}
			}
			if (canDisposeSurface) {
				surface.Dispose();
			}
		}

		private bool CaptureActiveWindow() {
			bool presupplied = false;
			LOG.Debug("CaptureActiveWindow");
			if (_selectedCaptureWindow != null) {
				LOG.Debug("Using supplied window");
				presupplied = true;
			} else {
				_selectedCaptureWindow = WindowDetails.GetActiveWindow();
				if (_selectedCaptureWindow != null) {
					LOG.DebugFormat("Capturing window: {0} with {1}", _selectedCaptureWindow.Text, _selectedCaptureWindow.WindowRectangle);
				}
			}
			if (_selectedCaptureWindow == null || (!presupplied && _selectedCaptureWindow.Iconic)) {
				LOG.Warn("No window to capture!");
				// Nothing to capture, code up in the stack will capture the full screen
				return false;
			}
			if (!presupplied && _selectedCaptureWindow != null && _selectedCaptureWindow.Iconic) {
				// Restore the window making sure it's visible!
				// This is done mainly for a screen capture, but some applications like Excel and TOAD have weird behaviour!
				_selectedCaptureWindow.Restore();
			}
			_selectedCaptureWindow = SelectCaptureWindow(_selectedCaptureWindow);
			if (_selectedCaptureWindow == null) {
				LOG.Warn("No window to capture, after SelectCaptureWindow!");
				// Nothing to capture, code up in the stack will capture the full screen
				return false;
			}
			// Fix for Bug #3430560 
			conf.LastCapturedRegion = _selectedCaptureWindow.WindowRectangle;
			bool returnValue = CaptureWindow(_selectedCaptureWindow, _capture, conf.WindowCaptureMode) != null;
			return returnValue;
		}

		/// <summary>
		/// Select the window to capture, this has logic which takes care of certain special applications
		/// like TOAD or Excel
		/// </summary>
		/// <param name="windowToCapture">WindowDetails with the target Window</param>
		/// <returns>WindowDetails with the target Window OR a replacement</returns>
		public static WindowDetails SelectCaptureWindow(WindowDetails windowToCapture) {
			Rectangle windowRectangle = windowToCapture.WindowRectangle;
			if (windowRectangle.Width == 0 || windowRectangle.Height == 0) {
				LOG.WarnFormat("Window {0} has nothing to capture, using workaround to find other window of same process.", windowToCapture.Text);
				// Trying workaround, the size 0 arrises with e.g. Toad.exe, has a different Window when minimized
				WindowDetails linkedWindow = WindowDetails.GetLinkedWindow(windowToCapture);
				if (linkedWindow != null) {
					windowRectangle = linkedWindow.WindowRectangle;
					windowToCapture = linkedWindow;
				} else {
					return null;
				}
			}
			return windowToCapture;
		}
		
		/// <summary>
		/// Check if Process uses PresentationFramework.dll -> meaning it uses WPF
		/// </summary>
		/// <param name="process">Proces to check for the presentation framework</param>
		/// <returns>true if the process uses WPF</returns>
		private static bool isWPF(Process process) {
			if (process != null) {
				try {
					foreach (ProcessModule module in process.Modules) {
						if (module.ModuleName.StartsWith("PresentationFramework")) {
							LOG.InfoFormat("Found that Process {0} uses {1}, assuming it's using WPF", process.ProcessName, module.FileName);
							return true;
						}
					}
				} catch (Exception) {
					// Access denied on the modules
					LOG.WarnFormat("No access on the modules from process {0}, assuming WPF is used.", process.ProcessName);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Capture the supplied Window
		/// </summary>
		/// <param name="windowToCapture">Window to capture</param>
		/// <param name="captureForWindow">The capture to store the details</param>
		/// <param name="windowCaptureMode">What WindowCaptureMode to use</param>
		/// <returns></returns>
		public static ICapture CaptureWindow(WindowDetails windowToCapture, ICapture captureForWindow, WindowCaptureMode windowCaptureMode) {
			if (captureForWindow == null) {
				captureForWindow = new Capture();
			}
			Rectangle windowRectangle = windowToCapture.WindowRectangle;

			// When Vista & DWM (Aero) enabled
			bool dwmEnabled = DWM.isDWMEnabled();
			// get process name to be able to exclude certain processes from certain capture modes
			using (Process process = windowToCapture.Process) {
				bool isAutoMode = windowCaptureMode == WindowCaptureMode.Auto;
				// For WindowCaptureMode.Auto we check:
				// 1) Is window IE, use IE Capture
				// 2) Is Windows >= Vista & DWM enabled: use DWM
				// 3) Otherwise use GDI (Screen might be also okay but might lose content)
				if (isAutoMode) {
					if (conf.IECapture && IECaptureHelper.IsIEWindow(windowToCapture)) {
						try {
							ICapture ieCapture = IECaptureHelper.CaptureIE(captureForWindow, windowToCapture);
							if (ieCapture != null) {
								return ieCapture;
							}
						} catch (Exception ex) {
							LOG.WarnFormat("Problem capturing IE, skipping to normal capture. Exception message was: {0}", ex.Message);
						}
					}

					// Take default screen
					windowCaptureMode = WindowCaptureMode.Screen;

					// Change to GDI, if allowed
					if (!windowToCapture.isMetroApp && WindowCapture.IsGdiAllowed(process)) {
						if (!dwmEnabled && isWPF(process)) {
							// do not use GDI, as DWM is not enabled and the application uses PresentationFramework.dll -> isWPF
							LOG.InfoFormat("Not using GDI for windows of process {0}, as the process uses WPF", process.ProcessName);
						} else {
							windowCaptureMode = WindowCaptureMode.GDI;
						}
					}

					// Change to DWM, if enabled and allowed
					if (dwmEnabled) {
						if (windowToCapture.isMetroApp || WindowCapture.IsDwmAllowed(process)) {
							windowCaptureMode = WindowCaptureMode.Aero;
						}
					}
				} else if (windowCaptureMode == WindowCaptureMode.Aero || windowCaptureMode == WindowCaptureMode.AeroTransparent) {
					if (!dwmEnabled || (!windowToCapture.isMetroApp && !WindowCapture.IsDwmAllowed(process))) {
						// Take default screen
						windowCaptureMode = WindowCaptureMode.Screen;
						// Change to GDI, if allowed
						if (WindowCapture.IsGdiAllowed(process)) {
							windowCaptureMode = WindowCaptureMode.GDI;
						}
					}
				} else if (windowCaptureMode == WindowCaptureMode.GDI && !WindowCapture.IsGdiAllowed(process)) {
					// GDI not allowed, take screen
					windowCaptureMode = WindowCaptureMode.Screen;
				}

				LOG.InfoFormat("Capturing window with mode {0}", windowCaptureMode);
				bool captureTaken = false;
				windowRectangle.Intersect(captureForWindow.ScreenBounds);
				// Try to capture
				while (!captureTaken) {
					ICapture tmpCapture = null;
					switch (windowCaptureMode) {
						case WindowCaptureMode.GDI:
							if (WindowCapture.IsGdiAllowed(process)) {
								if (windowToCapture.Iconic) {
									// Restore the window making sure it's visible!
									windowToCapture.Restore();
								} else {
									windowToCapture.ToForeground();
								}
								tmpCapture = windowToCapture.CaptureGDIWindow(captureForWindow);
								if (tmpCapture != null) {
									// check if GDI capture any good, by comparing it with the screen content
									int blackCountGDI = ImageHelper.CountColor(tmpCapture.Image, Color.Black, false);
									int GDIPixels = tmpCapture.Image.Width * tmpCapture.Image.Height;
									int blackPercentageGDI = (blackCountGDI * 100) / GDIPixels;
									if (blackPercentageGDI >= 1) {
										int screenPixels = windowRectangle.Width * windowRectangle.Height;
										using (ICapture screenCapture = new Capture()) {
											screenCapture.CaptureDetails = captureForWindow.CaptureDetails;
											if (WindowCapture.CaptureRectangleFromDesktopScreen(screenCapture, windowRectangle) != null) {
												int blackCountScreen = ImageHelper.CountColor(screenCapture.Image, Color.Black, false);
												int blackPercentageScreen = (blackCountScreen * 100) / screenPixels;
												if (screenPixels == GDIPixels) {
													// "easy compare", both have the same size
													// If GDI has more black, use the screen capture.
													if (blackPercentageGDI > blackPercentageScreen) {
														LOG.Debug("Using screen capture, as GDI had additional black.");
														// changeing the image will automatically dispose the previous
														tmpCapture.Image = screenCapture.Image;
														// Make sure it's not disposed, else the picture is gone!
														screenCapture.NullImage();
													}
												} else if (screenPixels < GDIPixels) {
													// Screen capture is cropped, window is outside of screen
													if (blackPercentageGDI > 50 && blackPercentageGDI > blackPercentageScreen) {
														LOG.Debug("Using screen capture, as GDI had additional black.");
														// changeing the image will automatically dispose the previous
														tmpCapture.Image = screenCapture.Image;
														// Make sure it's not disposed, else the picture is gone!
														screenCapture.NullImage();
													}
												} else {
													// Use the GDI capture by doing nothing
													LOG.Debug("This should not happen, how can there be more screen as GDI pixels?");
												}
											}
										}
									}
								}
							}
							if (tmpCapture != null) {
								captureForWindow = tmpCapture;
								captureTaken = true;
							} else {
								// A problem, try Screen
								windowCaptureMode = WindowCaptureMode.Screen;
							}
							break;
						case WindowCaptureMode.Aero:
						case WindowCaptureMode.AeroTransparent:
							if (windowToCapture.isMetroApp || WindowCapture.IsDwmAllowed(process)) {
								tmpCapture = windowToCapture.CaptureDWMWindow(captureForWindow, windowCaptureMode, isAutoMode);
							}
							if (tmpCapture != null) {
								captureForWindow = tmpCapture;
								captureTaken = true;
							} else {
								// A problem, try GDI
								windowCaptureMode = WindowCaptureMode.GDI;
							}
							break;
						default:
							// Screen capture
							if (windowToCapture.Iconic) {
								// Restore the window making sure it's visible!
								windowToCapture.Restore();
							} else {
								windowToCapture.ToForeground();
							}

							try {
								captureForWindow = WindowCapture.CaptureRectangleFromDesktopScreen(captureForWindow, windowRectangle);
								captureTaken = true;
							} catch (Exception e) {
								LOG.Error("Problem capturing", e);
								return null;
							}
							break;
					}
				}
			}

			if (captureForWindow != null) {
				if (windowToCapture != null) {
					captureForWindow.CaptureDetails.Title = windowToCapture.Text;
				}
			}

			return captureForWindow;
		}

		private void SetDPI() {
			// Workaround for proble with DPI retrieval, the FromHwnd activates the window...
			WindowDetails previouslyActiveWindow = WindowDetails.GetActiveWindow();
			// Workaround for changed DPI settings in Windows 7
			using (Graphics graphics = Graphics.FromHwnd(MainForm.Instance.Handle)) {
				_capture.CaptureDetails.DpiX = graphics.DpiX;
				_capture.CaptureDetails.DpiY = graphics.DpiY;
			}
			if (previouslyActiveWindow != null) {
				// Set previouslyActiveWindow as foreground window
				previouslyActiveWindow.ToForeground();
			}
			if (_capture.CaptureDetails != null && _capture.Image != null) {
				((Bitmap)_capture.Image).SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
			}

		}

		#region capture with feedback
		private void CaptureWithFeedback() {
			// The following, to be precise the HideApp, causes the app to close as described in BUG-1620 
			// Added check for metro (Modern UI) apps, which might be maximized and cover the screen.
			
			//foreach(WindowDetails app in WindowDetails.GetMetroApps()) {
			//	if (app.Maximised) {
			//		app.HideApp();
			//	}
			//}

			using (CaptureForm captureForm = new CaptureForm(_capture, _windows)) {
				DialogResult result = captureForm.ShowDialog();
				if (result == DialogResult.OK) {
					_selectedCaptureWindow = captureForm.SelectedCaptureWindow;
					_captureRect = captureForm.CaptureRectangle;
					// Get title
					if (_selectedCaptureWindow != null) {
						_capture.CaptureDetails.Title = _selectedCaptureWindow.Text;
					}
					
					if (_captureRect.Height > 0 && _captureRect.Width > 0) {
						// Take the captureRect, this already is specified as bitmap coordinates
						_capture.Crop(_captureRect);
						
						// save for re-capturing later and show recapture context menu option
						// Important here is that the location needs to be offsetted back to screen coordinates!
						Rectangle tmpRectangle = _captureRect;
						tmpRectangle.Offset(_capture.ScreenBounds.Location.X, _capture.ScreenBounds.Location.Y);
						conf.LastCapturedRegion = tmpRectangle;
						HandleCapture();
					}
				}
			}
		}
		
		#endregion
	}
}
