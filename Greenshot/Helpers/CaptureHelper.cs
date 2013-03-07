/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Configuration;
using Greenshot.Destinations;
using Greenshot.Drawing;
using Greenshot.Forms;
using Greenshot.Helpers;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.UnmanagedHelpers;
using Greenshot.IniFile;
using Greenshot.Interop;

namespace Greenshot.Helpers {
	/// <summary>
	/// CaptureHelper contains all the capture logic 
	/// </summary>
	public class CaptureHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(CaptureHelper));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		// TODO: when we get the screen capture code working correctly, this needs to be enabled
		//private static ScreenCaptureHelper screenCapture = null;
		private List<WindowDetails> windows = new List<WindowDetails>();
		private WindowDetails selectedCaptureWindow = null;
		private Rectangle captureRect = Rectangle.Empty;
		private bool captureMouseCursor = false;
		private ICapture capture = null;
		private CaptureMode captureMode;
		private ScreenCaptureMode screenCaptureMode = ScreenCaptureMode.Auto;
		private Thread windowDetailsThread = null;

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
			windows = null;
			selectedCaptureWindow = null;
			windowDetailsThread = null;
			capture = null;
		}
		public static void CaptureClipboard() {
			new CaptureHelper(CaptureMode.Clipboard).MakeCapture();
		}
		public static void CaptureRegion(bool captureMouse) {
			new CaptureHelper(CaptureMode.Region, captureMouse).MakeCapture();
		}
		public static void CaptureRegion(bool captureMouse, IDestination destination) {
			CaptureHelper captureHelper = new CaptureHelper(CaptureMode.Region, captureMouse, destination);
			captureHelper.MakeCapture();
		}
		public static void CaptureRegion(bool captureMouse, Rectangle region) {
			new CaptureHelper(CaptureMode.Region, captureMouse).MakeCapture(region);
		}
		public static void CaptureFullscreen(bool captureMouse, ScreenCaptureMode screenCaptureMode) {
			CaptureHelper captureHelper = new CaptureHelper(CaptureMode.FullScreen, captureMouse);
			captureHelper.screenCaptureMode = screenCaptureMode;
			captureHelper.MakeCapture();
		}
		public static void CaptureLastRegion(bool captureMouse) {
			new CaptureHelper(CaptureMode.LastRegion, captureMouse).MakeCapture();
		}

		public static void CaptureIE(bool captureMouse, WindowDetails windowToCapture) {
			CaptureHelper captureHelper = new CaptureHelper(CaptureMode.IE, captureMouse);
			captureHelper.SelectedCaptureWindow = windowToCapture;
			captureHelper.MakeCapture();
		}

		public static void CaptureWindow(bool captureMouse) {
			new CaptureHelper(CaptureMode.ActiveWindow, captureMouse).MakeCapture();
		}

		public static void CaptureWindow(WindowDetails windowToCapture) {
			CaptureHelper captureHelper = new CaptureHelper(CaptureMode.ActiveWindow);
			captureHelper.SelectedCaptureWindow = windowToCapture;
			captureHelper.MakeCapture();
		}

		public static void CaptureWindowInteractive(bool captureMouse) {
			new CaptureHelper(CaptureMode.Window, captureMouse).MakeCapture();
		}

		public static void CaptureFile(string filename) {
			new CaptureHelper(CaptureMode.File).MakeCapture(filename);
		}

		public static void CaptureFile(string filename, IDestination destination) {
			new CaptureHelper(CaptureMode.File).AddDestination(destination).MakeCapture(filename);
		}

		public static void ImportCapture(ICapture captureToImport) {
			CaptureHelper captureHelper = new CaptureHelper(CaptureMode.File);
			captureHelper.capture = captureToImport;
			captureHelper.HandleCapture();
		}

		public CaptureHelper AddDestination(IDestination destination) {
			capture.CaptureDetails.AddDestination(destination);
			return this;
		}

		public CaptureHelper(CaptureMode captureMode) {
			this.captureMode = captureMode;
			capture = new Capture();			
		}

		public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor) : this(captureMode) {
			this.captureMouseCursor = captureMouseCursor;
		}

		public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, ScreenCaptureMode screenCaptureMode) : this(captureMode) {
			this.captureMouseCursor = captureMouseCursor;
			this.screenCaptureMode = screenCaptureMode;
		}

		public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, IDestination destination) : this(captureMode, captureMouseCursor) {
			capture.CaptureDetails.AddDestination(destination);
		}
		
		public WindowDetails SelectedCaptureWindow {
			get {
				return selectedCaptureWindow;
			}
			set {
				selectedCaptureWindow = value;
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
			capture.CaptureDetails.Filename = filename;
			MakeCapture();
		}

		/// <summary>
		/// Make Capture for region
		/// </summary>
		/// <param name="filename">filename</param>
		private void MakeCapture(Rectangle region) {
			captureRect = region;
			MakeCapture();
		}


		/// <summary>
		/// Make Capture with specified destinations
		/// </summary>
		private void MakeCapture() {
			// Experimental code
			// TODO: when we get the screen capture code working correctly, this needs to be enabled
			//if (screenCapture != null) {
			//	screenCapture.Stop();
			//	screenCapture = null;
			//	return;
			//}
			// This fixes a problem when a balloon is still visible and a capture needs to be taken
			// forcefully removes the balloon!
			if (!conf.HideTrayicon) {
				MainForm.Instance.NotifyIcon.Visible = false;
				MainForm.Instance.NotifyIcon.Visible = true;
			}
			LOG.Debug(String.Format("Capturing with mode {0} and using Cursor {1}", captureMode, captureMouseCursor));
			capture.CaptureDetails.CaptureMode = captureMode;

			// Get the windows details in a seperate thread, only for those captures that have a Feedback
			// As currently the "elements" aren't used, we don't need them yet
			switch (captureMode) {
				case CaptureMode.Region:
					// Check if a region is pre-supplied!
					if (Rectangle.Empty.Equals(captureRect)) {
						windowDetailsThread = PrepareForCaptureWithFeedback();
					}
					break;
				case CaptureMode.Window:
					windowDetailsThread = PrepareForCaptureWithFeedback();
					break;
			}

			// Add destinations if no-one passed a handler
			if (capture.CaptureDetails.CaptureDestinations == null || capture.CaptureDetails.CaptureDestinations.Count == 0) {
				AddConfiguredDestination();
			}

			// Workaround for proble with DPI retrieval, the FromHwnd activates the window...
			WindowDetails previouslyActiveWindow = WindowDetails.GetActiveWindow();
			// Workaround for changed DPI settings in Windows 7
			using (Graphics graphics = Graphics.FromHwnd(MainForm.Instance.Handle)) {
				capture.CaptureDetails.DpiX = graphics.DpiX;
				capture.CaptureDetails.DpiY = graphics.DpiY;
			}
			if (previouslyActiveWindow != null) {
				// Set previouslyActiveWindow as foreground window
				previouslyActiveWindow.ToForeground();
			}

			// Delay for the Context menu
			if (conf.CaptureDelay > 0) {
				System.Threading.Thread.Sleep(conf.CaptureDelay);
			} else {
				conf.CaptureDelay = 0;
			}

			// Capture Mousecursor if we are not loading from file or clipboard, only show when needed
			if (captureMode != CaptureMode.File && captureMode != CaptureMode.Clipboard) {
				capture = WindowCapture.CaptureCursor(capture);
				if (captureMouseCursor) {
					capture.CursorVisible = conf.CaptureMousepointer;
				} else {
					capture.CursorVisible = false;					
				}
			}

			switch(captureMode) {
				case CaptureMode.Window:
					capture = WindowCapture.CaptureScreen(capture);
					capture.CaptureDetails.AddMetaData("source", "Screen");
					CaptureWithFeedback();
					break;
				case CaptureMode.ActiveWindow:
					if (CaptureActiveWindow()) {
						// TODO: Reactive / check if the elements code is activated
						//if (windowDetailsThread != null) {
						//	windowDetailsThread.Join();
						//}
						//capture.MoveElements(capture.ScreenBounds.Location.X-capture.Location.X, capture.ScreenBounds.Location.Y-capture.Location.Y);

						// Capture worked, offset mouse according to screen bounds and capture location
						capture.MoveMouseLocation(capture.ScreenBounds.Location.X-capture.Location.X, capture.ScreenBounds.Location.Y-capture.Location.Y);
						capture.CaptureDetails.AddMetaData("source", "Window");
					} else {
						captureMode = CaptureMode.FullScreen;
						capture = WindowCapture.CaptureScreen(capture);
						capture.CaptureDetails.AddMetaData("source", "Screen");
						capture.CaptureDetails.Title = "Screen";
					}
					HandleCapture();
					break;
				case CaptureMode.IE:
					if (IECaptureHelper.CaptureIE(capture, SelectedCaptureWindow) != null) {
						capture.CaptureDetails.AddMetaData("source", "Internet Explorer");
						HandleCapture();
					}
					break;
				case CaptureMode.FullScreen:
					// Check how we need to capture the screen
					bool captureTaken = false;
					switch (screenCaptureMode) {
						case ScreenCaptureMode.Auto:
							Point mouseLocation = WindowCapture.GetCursorLocation();
							foreach (Screen screen in Screen.AllScreens) {
								if (screen.Bounds.Contains(mouseLocation)) {
									capture = WindowCapture.CaptureRectangle(capture, screen.Bounds);
									captureTaken = true;
									break;
								}
							}
							break;
						case ScreenCaptureMode.Fixed:
							if (conf.ScreenToCapture > 0 && conf.ScreenToCapture <= Screen.AllScreens.Length) {
								capture = WindowCapture.CaptureRectangle(capture, Screen.AllScreens[conf.ScreenToCapture].Bounds);
								captureTaken = true;
							}
							break;
						case ScreenCaptureMode.FullScreen:
							// Do nothing, we take the fullscreen capture automatically
							break;
					}
					if (!captureTaken) {
						capture = WindowCapture.CaptureScreen(capture);
					}
					HandleCapture();
					break;
				case CaptureMode.Clipboard:
					Image clipboardImage = ClipboardHelper.GetImage();
					if (clipboardImage != null) {
						if (capture != null) {
							capture.Image = clipboardImage;
						} else {
							capture = new Capture(clipboardImage);
						}
						capture.CaptureDetails.Title = "Clipboard";
						capture.CaptureDetails.AddMetaData("source", "Clipboard");
						// Force Editor, keep picker
						if (capture.CaptureDetails.HasDestination(Destinations.PickerDestination.DESIGNATION)) {
							capture.CaptureDetails.ClearDestinations();
							capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(Destinations.EditorDestination.DESIGNATION));
							capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(Destinations.PickerDestination.DESIGNATION));
						} else {
							capture.CaptureDetails.ClearDestinations();
							capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(Destinations.EditorDestination.DESIGNATION));
						}
						HandleCapture();
					} else {
						MessageBox.Show(Language.GetString("clipboard_noimage"));
					}
					break;
				case CaptureMode.File:
					Image fileImage = null;
					string filename = capture.CaptureDetails.Filename;

					if (!string.IsNullOrEmpty(filename)) {
						try {
							if (filename.ToLower().EndsWith("." + OutputFormat.greenshot)) {
								ISurface surface = new Surface();
								surface = ImageOutput.LoadGreenshotSurface(filename, surface);
								surface.CaptureDetails = capture.CaptureDetails;
								DestinationHelper.GetDestination(EditorDestination.DESIGNATION).ExportCapture(true, surface, capture.CaptureDetails);
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
						capture.CaptureDetails.Title = Path.GetFileNameWithoutExtension(filename);
						capture.CaptureDetails.AddMetaData("file", filename);
						capture.CaptureDetails.AddMetaData("source", "file");
						if (capture != null) {
							capture.Image = fileImage;
						} else {
							capture = new Capture(fileImage);
						}
						// Force Editor, keep picker, this is currently the only usefull destination
						if (capture.CaptureDetails.HasDestination(PickerDestination.DESIGNATION)) {
							capture.CaptureDetails.ClearDestinations();
							capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
							capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(PickerDestination.DESIGNATION));
						} else {
							capture.CaptureDetails.ClearDestinations();
							capture.CaptureDetails.AddDestination(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
						}
						HandleCapture();
					}
					break;
				case CaptureMode.LastRegion:
					if (!RuntimeConfig.LastCapturedRegion.IsEmpty) {
						capture = WindowCapture.CaptureRectangle(capture, RuntimeConfig.LastCapturedRegion);
						// TODO: Reactive / check if the elements code is activated
						//if (windowDetailsThread != null) {
						//	windowDetailsThread.Join();
						//}

						// Set capture title, fixing bug #3569703
						foreach (WindowDetails window in WindowDetails.GetVisibleWindows()) {
							Point estimatedLocation = new Point(RuntimeConfig.LastCapturedRegion.X + (RuntimeConfig.LastCapturedRegion.Width / 2), RuntimeConfig.LastCapturedRegion.Y + (RuntimeConfig.LastCapturedRegion.Height / 2));
							if (window.Contains(estimatedLocation)) {
								selectedCaptureWindow = window;
								capture.CaptureDetails.Title = selectedCaptureWindow.Text;
								break;
							}
						}
						// Move cursor, fixing bug #3569703
						capture.MoveMouseLocation(capture.ScreenBounds.Location.X - capture.Location.X, capture.ScreenBounds.Location.Y - capture.Location.Y);
						//capture.MoveElements(capture.ScreenBounds.Location.X - capture.Location.X, capture.ScreenBounds.Location.Y - capture.Location.Y);

						capture.CaptureDetails.AddMetaData("source", "screen");
						HandleCapture();
					}
					break;
				case CaptureMode.Region:
					// Check if a region is pre-supplied!
					if (Rectangle.Empty.Equals(captureRect)) {
						capture = WindowCapture.CaptureScreen(capture);
						capture.CaptureDetails.AddMetaData("source", "screen");
						CaptureWithFeedback();
					} else {
						capture = WindowCapture.CaptureRectangle(capture, captureRect);
						capture.CaptureDetails.AddMetaData("source", "screen");
						HandleCapture();
					}
					break;
				default:
					LOG.Warn("Unknown capture mode: " + captureMode);
					break;
			}
			// TODO: Reactive / check if the elements code is activated
			//if (windowDetailsThread != null) {
			//	windowDetailsThread.Join();
			//}
			if (capture != null) {
				LOG.Debug("Disposing capture");
				capture.Dispose();
			}
		}
				
		/// <summary>
		/// Pre-Initialization for CaptureWithFeedback, this will get all the windows before we change anything
		/// </summary>
		private Thread PrepareForCaptureWithFeedback() {
			windows = new List<WindowDetails>();
			
			// If the App Launcher is visisble, no other windows are active
			WindowDetails appLauncherWindow = WindowDetails.GetAppLauncher();
			if (appLauncherWindow != null && appLauncherWindow.Visible) {
				windows.Add(appLauncherWindow);
				return null;
			}
			
			Thread getWindowDetailsThread = new Thread (delegate() {
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
					if (windowSize.Width == 0 ||  windowSize.Height == 0) {
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
					lock (windows) {
						windows.Add(window);
					}

					// TODO: Following code should be enabled & checked if the editor can support "elements"
					//// Get window rectangle as capture Element
					//CaptureElement windowCaptureElement = new CaptureElement(windowRectangle);
					//if (capture == null) {
					//	break;
					//}
					//capture.Elements.Add(windowCaptureElement);
	
					//if (!window.HasParent) {
					//	// Get window client rectangle as capture Element, place all the other "children" in there
					//	Rectangle clientRectangle = window.ClientRectangle;
					//	CaptureElement windowClientCaptureElement = new CaptureElement(clientRectangle);
					//	windowCaptureElement.Children.Add(windowClientCaptureElement);
					//	AddCaptureElementsForWindow(windowClientCaptureElement, window, goLevelDeep);
					//} else {
					//	AddCaptureElementsForWindow(windowCaptureElement, window, goLevelDeep);
					//}
				}
//				lock (windows) {
//					windows = WindowDetails.SortByZOrder(IntPtr.Zero, windows);
//				}
			});
			getWindowDetailsThread.Name = "Retrieve window details";
			getWindowDetailsThread.IsBackground = true;
			getWindowDetailsThread.Start();
			return getWindowDetailsThread;
		}
		
		// Code used to get the capture elements, which is not active yet
		//private void AddCaptureElementsForWindow(ICaptureElement parentElement, WindowDetails parentWindow, int level) {
		//    foreach(WindowDetails childWindow in parentWindow.Children) {
		//        // Make sure the details are retrieved once
		//        childWindow.FreezeDetails();
		//        Rectangle childRectangle = childWindow.WindowRectangle;
		//        Size s1 = childRectangle.Size;
		//        childRectangle.Intersect(parentElement.Bounds);
		//        if (childRectangle.Width > 0 && childRectangle.Height > 0) {
		//            CaptureElement childCaptureElement = new CaptureElement(childRectangle);
		//            parentElement.Children.Add(childCaptureElement);
		//            if (level > 0) {
		//                AddCaptureElementsForWindow(childCaptureElement, childWindow, level -1);
		//            }
		//        }
		//    }
		//}

		private void AddConfiguredDestination() {
			foreach(string destinationDesignation in conf.OutputDestinations) {
				IDestination destination = DestinationHelper.GetDestination(destinationDesignation);
				if (destination != null) {
					capture.CaptureDetails.AddDestination(destination);
				}
			}
		}

		private void HandleCapture() {
			// Flag to see if the image was "exported" so the FileEditor doesn't
			// ask to save the file as long as nothing is done.
			bool outputMade = false;

			// Make sure the user sees that the capture is made
			if (capture.CaptureDetails.CaptureMode == CaptureMode.File || capture.CaptureDetails.CaptureMode == CaptureMode.Clipboard) {
				// Maybe not "made" but the original is still there... somehow
				outputMade = true;
			} else {
				// Make sure the resolution is set correctly!
				if (capture.CaptureDetails != null && capture.Image != null) {
					((Bitmap)capture.Image).SetResolution(capture.CaptureDetails.DpiX, capture.CaptureDetails.DpiY);
				}
				DoCaptureFeedback();
			}

			LOG.Debug("A capture of: " + capture.CaptureDetails.Title);

			// check if someone has passed a destination
			if (capture.CaptureDetails.CaptureDestinations == null || capture.CaptureDetails.CaptureDestinations.Count == 0) {
				AddConfiguredDestination();
			}

			// Create Surface with capture, this way elements can be added automatically (like the mouse cursor)
			Surface surface = new Surface(capture);
			surface.Modified = !outputMade;

			// Register notify events if this is wanted			
			if (conf.ShowTrayNotification && !conf.HideTrayicon) {
				surface.SurfaceMessage += delegate(object source, SurfaceMessageEventArgs eventArgs) {
					if (string.IsNullOrEmpty(eventArgs.Message)) {
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
							EventHandler balloonTipClickedHandler = null;
							EventHandler balloonTipClosedHandler = null;
							balloonTipClosedHandler = delegate(object sender, EventArgs e) {
								LOG.DebugFormat("Deregistering the BalloonTipClosed");
								MainForm.Instance.NotifyIcon.BalloonTipClicked -= balloonTipClickedHandler;
								MainForm.Instance.NotifyIcon.BalloonTipClosed -= balloonTipClosedHandler;
							};

							balloonTipClickedHandler = delegate(object sender, EventArgs e) {
								if (eventArgs.MessageType == SurfaceMessageTyp.FileSaved) {
									if (!string.IsNullOrEmpty(surface.LastSaveFullPath)) {
										ProcessStartInfo psi = new ProcessStartInfo("explorer");
										psi.Arguments = Path.GetDirectoryName(surface.LastSaveFullPath);
										psi.UseShellExecute = false;
										Process p = new Process();
										p.StartInfo = psi;
										p.Start();
									}
								} else {
									if (!string.IsNullOrEmpty(surface.UploadURL)) {
										System.Diagnostics.Process.Start(surface.UploadURL);
									}
								}
								LOG.DebugFormat("Deregistering the BalloonTipClicked");
								MainForm.Instance.NotifyIcon.BalloonTipClicked -= balloonTipClickedHandler;
								MainForm.Instance.NotifyIcon.BalloonTipClosed -= balloonTipClosedHandler;
							};
							MainForm.Instance.NotifyIcon.BalloonTipClicked += balloonTipClickedHandler;
							MainForm.Instance.NotifyIcon.BalloonTipClosed += balloonTipClosedHandler;
							MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", eventArgs.Message, ToolTipIcon.Info);
							break;
					}
				};
				
			}
			// Let the processors do their job
			foreach(IProcessor processor in ProcessorHelper.GetAllProcessors()) {
				if (processor.isActive) {
					LOG.InfoFormat("Calling processor {0}", processor.Description);
					processor.ProcessCapture(surface, capture.CaptureDetails);
				}
			}
			
			// As the surfaces copies the reference to the image, make sure the image is not being disposed (a trick to save memory)
			capture.Image = null;

			// Get CaptureDetails as we need it even after the capture is disposed
			ICaptureDetails captureDetails = capture.CaptureDetails;
			bool canDisposeSurface = true;

			if (captureDetails.HasDestination(Destinations.PickerDestination.DESIGNATION)) {
				DestinationHelper.ExportCapture(false, Destinations.PickerDestination.DESIGNATION, surface, captureDetails);
				captureDetails.CaptureDestinations.Clear();
				canDisposeSurface = false;
			}

			// Disable capturing
			captureMode = CaptureMode.None;
			// Dispose the capture, we don't need it anymore (the surface copied all information and we got the title (if any)).
			capture.Dispose();
			capture = null;

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
					if (Destinations.EditorDestination.DESIGNATION.Equals(destination.Designation) && exportInformation.ExportMade) {
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
			if (selectedCaptureWindow != null) {
				LOG.Debug("Using supplied window");
				presupplied = true;
			} else {
				selectedCaptureWindow = WindowDetails.GetActiveWindow();
				if (selectedCaptureWindow != null) {
					LOG.DebugFormat("Capturing window: {0} with {1}", selectedCaptureWindow.Text, selectedCaptureWindow.WindowRectangle);
				}
			}
			if (selectedCaptureWindow == null || (!presupplied && selectedCaptureWindow.Iconic)) {
				LOG.Warn("No window to capture!");
				// Nothing to capture, code up in the stack will capture the full screen
				return false;
			}
			if (!presupplied && selectedCaptureWindow != null && selectedCaptureWindow.Iconic) {
				// Restore the window making sure it's visible!
				// This is done mainly for a screen capture, but some applications like Excel and TOAD have weird behaviour!
				selectedCaptureWindow.Restore();
			}
			selectedCaptureWindow = SelectCaptureWindow(selectedCaptureWindow);
			if (selectedCaptureWindow == null) {
				LOG.Warn("No window to capture, after SelectCaptureWindow!");
				// Nothing to capture, code up in the stack will capture the full screen
				return false;
			}
			// Fix for Bug #3430560 
			RuntimeConfig.LastCapturedRegion = selectedCaptureWindow.WindowRectangle;
			bool returnValue = CaptureWindow(selectedCaptureWindow, capture, conf.WindowCaptureMode) != null;
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
			Process process = windowToCapture.Process;
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
				if (!windowToCapture.isMetroApp && WindowCapture.isGDIAllowed(process)) {
					if (!dwmEnabled && isWPF(process)) {
						// do not use GDI, as DWM is not enabled and the application uses PresentationFramework.dll -> isWPF
						LOG.InfoFormat("Not using GDI for windows of process {0}, as the process uses WPF", process.ProcessName);
					} else {
						windowCaptureMode = WindowCaptureMode.GDI;
					}
				}

				// Change to DWM, if enabled and allowed
				if (dwmEnabled) {
					if (windowToCapture.isMetroApp || WindowCapture.isDWMAllowed(process)) {
						windowCaptureMode = WindowCaptureMode.Aero;
					}
				}
			} else if (windowCaptureMode == WindowCaptureMode.Aero || windowCaptureMode == WindowCaptureMode.AeroTransparent) {
				if (!dwmEnabled || (!windowToCapture.isMetroApp && !WindowCapture.isDWMAllowed(process))) {
					// Take default screen
					windowCaptureMode = WindowCaptureMode.Screen;
					// Change to GDI, if allowed
					if (WindowCapture.isGDIAllowed(process)) {
						windowCaptureMode = WindowCaptureMode.GDI;
					}
				}
			} else if (windowCaptureMode == WindowCaptureMode.GDI && !WindowCapture.isGDIAllowed(process)) {
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
						if (WindowCapture.isGDIAllowed(process)) {
							if (windowToCapture.Iconic) {
								// Restore the window making sure it's visible!
								windowToCapture.Restore();
							} else {
								windowToCapture.ToForeground();
							}
							tmpCapture = windowToCapture.CaptureGDIWindow(captureForWindow);
							if (tmpCapture != null) {
								// check if GDI capture any good, by comparing it with the screen content
								int blackCountGDI = ImageHelper.CountColor((Bitmap)tmpCapture.Image, Color.Black, false);
								int GDIPixels = tmpCapture.Image.Width * tmpCapture.Image.Height;
								int blackPercentageGDI = (blackCountGDI * 100) / GDIPixels;
								if (blackPercentageGDI >= 1) {
									int screenPixels = windowRectangle.Width * windowRectangle.Height;
									using (ICapture screenCapture = new Capture()) {
										screenCapture.CaptureDetails = captureForWindow.CaptureDetails;
										if (WindowCapture.CaptureRectangle(screenCapture, windowRectangle) != null) {
											int blackCountScreen = ImageHelper.CountColor((Bitmap)screenCapture.Image, Color.Black, false);
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
						if (windowToCapture.isMetroApp || WindowCapture.isDWMAllowed(process)) {
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
							captureForWindow = WindowCapture.CaptureRectangle(captureForWindow, windowRectangle);
							captureTaken = true;
						} catch (Exception e) {
							LOG.Error("Problem capturing", e);
							return null;
						}
						break;
				}
			}

			if (captureForWindow != null) {
				if (windowToCapture != null) {
					captureForWindow.CaptureDetails.Title = windowToCapture.Text;
				}
				((Bitmap)captureForWindow.Image).SetResolution(captureForWindow.CaptureDetails.DpiX, captureForWindow.CaptureDetails.DpiY);
			}

			return captureForWindow;
		}

		#region capture with feedback
		private void CaptureWithFeedback() {
			// Added check for metro (Modern UI) apps, which might be maximized and cover the screen.
			// as they don't want to 
			foreach(WindowDetails app in WindowDetails.GetMetroApps()) {
				if (app.Maximised) {
					app.HideApp();
				}
			}
			using (CaptureForm captureForm = new CaptureForm(capture, windows)) {
				DialogResult result = captureForm.ShowDialog();
				if (result == DialogResult.OK) {
					selectedCaptureWindow = captureForm.SelectedCaptureWindow;
					captureRect = captureForm.CaptureRectangle;
					// Get title
					if (selectedCaptureWindow != null) {
						capture.CaptureDetails.Title = selectedCaptureWindow.Text;
					}
					
					if (captureRect.Height > 0 && captureRect.Width > 0) {
						// TODO: Reactive / check if the elements code is activated
						//if (windowDetailsThread != null) {
						//	windowDetailsThread.Join();
						//}

						// Experimental code for Video capture
						// TODO: when we get the screen capture code working correctly, this needs to be enabled
						//if (capture.CaptureDetails.CaptureMode == CaptureMode.Video) {
						//    if (captureForm.UsedCaptureMode == CaptureMode.Window) {
						//        screenCapture = new ScreenCaptureHelper(selectedCaptureWindow);
						//    } else if (captureForm.UsedCaptureMode == CaptureMode.Region) {
						//        screenCapture = new ScreenCaptureHelper(captureRect);
						//    }
						//    if (screenCapture != null) {
						//        screenCapture.RecordMouse = capture.CursorVisible;
						//        if (screenCapture.Start(25)) {
						//            return;
						//        }
						//        // User clicked cancel or a problem occured
						//        screenCapture.Stop();
						//        screenCapture = null;
						//        return;
						//    }
						//}
						// Take the captureRect, this already is specified as bitmap coordinates
						capture.Crop(captureRect);
						
						// save for re-capturing later and show recapture context menu option
						// Important here is that the location needs to be offsetted back to screen coordinates!
						Rectangle tmpRectangle = captureRect.Clone();
						tmpRectangle.Offset(capture.ScreenBounds.Location.X, capture.ScreenBounds.Location.Y);
						RuntimeConfig.LastCapturedRegion = tmpRectangle;
						HandleCapture();
					}
				}
			}
		}
		
		#endregion
	}
}
