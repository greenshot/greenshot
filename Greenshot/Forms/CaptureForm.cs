/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Drawing;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.UnmanagedHelpers;
using GreenshotPlugin.Core;

namespace Greenshot.Forms {
	/// <summary>
	/// Description of CaptureForm.
	/// </summary>
	public partial class CaptureForm : Form, ICaptureHost {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(CaptureForm));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

		private int mX;
		private int mY;
		private Point cursorPos = Point.Empty;
		// TODO: dispose Brush & Pen, not very important as we only instanciate this once
		private Brush OverlayBrush = new SolidBrush(Color.FromArgb(50, Color.MediumSeaGreen));
		private Pen OverlayPen = new Pen(Color.FromArgb(50, Color.Black));
		private CaptureMode captureMode = CaptureMode.None;
		private List<WindowDetails> windows = new List<WindowDetails>();
		private WindowDetails selectedCaptureWindow;
		private bool mouseDown = false;
		private Rectangle captureRect = Rectangle.Empty;
		private ICapture capture = null;
		private ILanguage lang = Language.GetInstance();

		public CaptureForm() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Text = "Greenshot capture form";

			// Make sure the form is hidden (might be overdoing it...)
			this.Hide();
		}

		void DoCaptureFeedback() {
			if(conf.PlayCameraSound) {
				SoundHelper.Play();
			}
		}

		/// <summary>
		/// Make Capture with default destinations
		/// </summary>
		/// <param name="mode">CaptureMode</param>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		public void MakeCapture(CaptureMode mode, bool captureMouseCursor) {
			Capture passingCapture = new Capture();
			MakeCapture(mode, captureMouseCursor, passingCapture);
		}

		/// <summary>
		/// Make capture of window
		/// </summary>
		/// <param name="window">WindowDetails of the window to capture</param>
		public void MakeCapture(WindowDetails window) {
			MakeCapture(window, null);
		}

		/// <summary>
		/// Make capture of window
		/// </summary>
		/// <param name="window">WindowDetails of the window to capture</param>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		public void MakeCapture(WindowDetails window,  CaptureHandler captureHandler) {
			Capture passingCapture = new Capture();
			passingCapture.CaptureDetails.CaptureHandler = captureHandler;
			selectedCaptureWindow = window;
			MakeCapture(CaptureMode.ActiveWindow, false, passingCapture);
		}
		
		/// <summary>
		/// Make Capture with default destinations
		/// </summary>
		/// <param name="mode">CaptureMode</param>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		public void MakeCapture(CaptureMode mode, bool captureMouseCursor, CaptureHandler captureHandler) {
			Capture passingCapture = new Capture();
			passingCapture.CaptureDetails.CaptureHandler = captureHandler;
			MakeCapture(mode, captureMouseCursor, passingCapture);
		}


		/// <summary>
		/// Make Capture with specified destinations
		/// </summary>
		/// <param name="mode">CaptureMode</param>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		/// <param name="captureDestinations">List<CaptureDestination> with destinations</param>
		public void MakeCapture(CaptureMode mode, bool captureMouseCursor, List<CaptureDestination> captureDestinations) {
			Capture passingCapture = new Capture();
			passingCapture.CaptureDetails.CaptureDestinations = captureDestinations;
			MakeCapture(mode, captureMouseCursor, passingCapture);
		}

		/// <summary>
		/// Make Capture with file name
		/// </summary>
		/// <param name="filename">List<CaptureDestination> with destinations</param>
		public void MakeCapture(string filename) {
			Capture passingCapture = new Capture();
			passingCapture.CaptureDetails.Filename = filename;
			MakeCapture(CaptureMode.File, false, passingCapture);
		}

		/// <summary>
		/// Make Capture with specified destinations
		/// </summary>
		/// <param name="mode">CaptureMode</param>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		/// <param name="captureDestinations">List<CaptureDestination> with destinations</param>
		private void MakeCapture(CaptureMode mode, bool captureMouseCursor, ICapture newCapture) {
			if (captureMode != CaptureMode.None) {
				LOG.Warn(String.Format("Capture started while capturing, current mode = {0} new capture was {1}.", captureMode, mode));
				return;
			} else {
				LOG.Debug(String.Format("MakeCapture({0}, {1})", mode, captureMouseCursor));
			}
			captureMode = mode;
			
			// cleanup the previos information if there is still any
			if (capture != null) {
				LOG.Debug("Capture wasn't disposed yet, this would suggest a leak");
				capture.Dispose();
				capture = null;
			}
			// Use the supplied Capture information
			capture = newCapture;
			capture.CaptureDetails.CaptureMode = mode;

			// Workaround for proble with DPI retrieval, the FromHwnd activates the window...
			WindowDetails previouslyActiveWindow = WindowDetails.GetActiveWindow();
			// Workaround for changed DPI settings in Windows 7
			using (Graphics graphics = Graphics.FromHwnd(this.Handle)) {
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

			// Allways capture Mousecursor, only show when needed
			capture = WindowCapture.CaptureCursor(capture);
			capture.CursorVisible = false;
			// Check if needed
			if (captureMouseCursor && mode != CaptureMode.Clipboard && mode != CaptureMode.File) {
				capture.CursorVisible = conf.CaptureMousepointer;
			}

			switch(mode) {
				case CaptureMode.Window:
					capture = WindowCapture.CaptureScreen(capture);
					capture.CaptureDetails.AddMetaData("source", "Screen");
					CaptureWithFeedback();
					break;
				case CaptureMode.ActiveWindow:
					if (CaptureActiveWindow()) {
						// Capture worked, offset mouse according to screen bounds and capture location
						capture.MoveMouseLocation(capture.ScreenBounds.Location.X-capture.Location.X, capture.ScreenBounds.Location.Y-capture.Location.Y);
						capture.CaptureDetails.AddMetaData("source", "Window");
					} else {
						captureMode = CaptureMode.FullScreen;
						capture = WindowCapture.CaptureScreen(capture);
						capture.CaptureDetails.AddMetaData("source", "Screen");
						capture.CaptureDetails.Title = "Screen";
					}
					// Make sure capturing is stopped at any cost
					StopCapturing(false);
					HandleCapture();
					break;
				case CaptureMode.IE:
					if (IECaptureHelper.CaptureIE(capture) != null) {
						capture.CaptureDetails.AddMetaData("source", "Internet Explorer");
						HandleCapture();
					} else {
						StopCapturing(true);
					}
					break;
				case CaptureMode.FullScreen:
					capture = WindowCapture.CaptureScreen(capture);
					HandleCapture();
					break;
				case CaptureMode.Clipboard:
					Image clipboardImage = null;
					string text = "Clipboard";
					if (Clipboard.ContainsImage()) {
						clipboardImage = Clipboard.GetImage();
					}
					if (clipboardImage != null) {
						if (capture != null) {
							capture.Image = clipboardImage;
						} else {
							capture = new Capture(clipboardImage);
						}
						string title = Clipboard.GetText();
						if (title == null || title.Trim().Length == 0) {
							title = "Clipboard";
						}
						capture.CaptureDetails.Title = title;
						capture.CaptureDetails.AddMetaData("source", "Clipboard");
						// Force Editor
						capture.CaptureDetails.AddDestination(CaptureDestination.Editor);
						HandleCapture();
					} else {
						MessageBox.Show("Couldn't create bitmap from : " + text);
					}
					break;
				case CaptureMode.File:
					Bitmap fileBitmap = null;
					try {
						fileBitmap = new Bitmap(capture.CaptureDetails.Filename, true);
					} catch (Exception e) {
						LOG.Error(e.Message, e);
						MessageBox.Show(lang.GetFormattedString(LangKey.error_openfile, capture.CaptureDetails.Filename));
					}
					capture.CaptureDetails.Title = Path.GetFileNameWithoutExtension(capture.CaptureDetails.Filename);
					capture.CaptureDetails.AddMetaData("file", capture.CaptureDetails.Filename);
					capture.CaptureDetails.AddMetaData("source", "file");
					if (fileBitmap != null) {
						if (capture != null) {
							capture.Image = fileBitmap;
						} else {
							capture = new Capture(fileBitmap);
						}
						// Force Editor
						capture.CaptureDetails.AddDestination(CaptureDestination.Editor);
						HandleCapture();
					}
					break;
				case CaptureMode.LastRegion:
					if (!RuntimeConfig.LastCapturedRegion.Equals(Rectangle.Empty)) {
						capture = WindowCapture.CaptureRectangle(capture, RuntimeConfig.LastCapturedRegion);
						capture.CaptureDetails.AddMetaData("source", "screen");
						HandleCapture();
					}
					break;
				case CaptureMode.Region:
					capture = WindowCapture.CaptureScreen(capture);
					capture.CaptureDetails.AddMetaData("source", "screen");
					CaptureWithFeedback();
					break;
				default:
					LOG.Warn("Unknown capture mode: " + mode);
					break;
			}
		}

		private ICapture AddConfiguredDestination(ICapture capture) {
			if (conf.OutputDestinations.Contains(Destination.FileDefault)) {
				capture.CaptureDetails.AddDestination(CaptureDestination.File);
			}

			if (conf.OutputDestinations.Contains(Destination.FileWithDialog)) {
				capture.CaptureDetails.AddDestination(CaptureDestination.FileWithDialog);
			}

			if (conf.OutputDestinations.Contains(Destination.Clipboard)) {
				capture.CaptureDetails.AddDestination(CaptureDestination.Clipboard);
			}

			if (conf.OutputDestinations.Contains(Destination.Printer)) {
				capture.CaptureDetails.AddDestination(CaptureDestination.Printer);
			}

			if (conf.OutputDestinations.Contains(Destination.Editor)) {
				capture.CaptureDetails.AddDestination(CaptureDestination.Editor);
			}

			if (conf.OutputDestinations.Contains(Destination.EMail)) {
				capture.CaptureDetails.AddDestination(CaptureDestination.EMail);
			}
			return capture;
		}

		/// <summary>
		/// Process a bitmap like it was captured
		/// </summary>
		/// <param name="bitmap">The bitmap to process</param>
		public void HandleCapture(Bitmap bitmap) {
			Capture capture = new Capture(bitmap);
			HandleCapture(capture);
		}

		// This is also an ICapture Interface implementation
		public void HandleCapture(Capture capture) {
			this.capture = capture;
			HandleCapture();
		}

		private void HandleCapture() {
			string fullPath = null;
			// Flag to see if the image was "exported" so the FileEditor doesn't
			// ask to save the file as long as nothing is done.
			bool outputMade = false;

			// Make sure the user sees that the capture is made
			if (capture.CaptureDetails.CaptureMode != CaptureMode.File && capture.CaptureDetails.CaptureMode != CaptureMode.Clipboard) {
				DoCaptureFeedback();
			} else {
				// If File || Clipboard
				// Maybe not "made" but the original is still there... somehow
				outputMade = true;
			}

			LOG.Debug("A capture of: " + capture.CaptureDetails.Title);
			
			// Create event OnCaptureTaken for all Plugins
			PluginHelper.instance.CreateCaptureTakenEvent(capture);

			// check if someone has passed a handler
			if (capture.CaptureDetails.CaptureHandler != null) {
				CaptureTakenEventArgs eventArgs = new CaptureTakenEventArgs(capture);
				capture.CaptureDetails.CaptureHandler(this, eventArgs);
			} else if (capture.CaptureDetails.CaptureDestinations == null || capture.CaptureDetails.CaptureDestinations.Count == 0) {
				AddConfiguredDestination(capture);
			}

			// Create Surface with capture, this way elements can be added automatically (like the mouse cursor)
			Surface surface = new Surface(capture);
			
			// As the surfaces copies the reference to the image, make sure the image is not being disposed (a trick to save memory)
			capture.Image = null;

			// Call plugins to do something with the screenshot
			PluginHelper.instance.CreateSurfaceFromCaptureEvent(capture, surface);

			// Disable capturing
			captureMode = CaptureMode.None;

			// Retrieve important information from the Capture object
			ICaptureDetails captureDetails = capture.CaptureDetails;
			List<CaptureDestination> captureDestinations = capture.CaptureDetails.CaptureDestinations;

			// Dispose the capture, we don't need it anymore (the surface copied all information and we got the title (if any)).
			capture.Dispose();
			capture = null;

			// Want to add more stuff to the surface?? DO IT HERE!
			int destinationsCount = captureDestinations.Count;
			if (captureDestinations.Contains(CaptureDestination.Editor)) {
				destinationsCount--;
			}
			if (destinationsCount > 0) {
				// Create Image for writing/printing etc and use "using" as all code paths either output the image or copy the image
				using (Image image = surface.GetImageForExport()) {
					// Flag to detect if we need to create a temp file for the email
					// or use the file that was written
					bool fileWritten = false;
					if (captureDestinations.Contains(CaptureDestination.File)) {
						string pattern = conf.OutputFileFilenamePattern;
						if (pattern == null || string.IsNullOrEmpty(pattern.Trim())) {
							pattern = "greenshot ${capturetime}";
						}
						string filename = FilenameHelper.GetFilenameFromPattern(pattern, conf.OutputFileFormat, captureDetails);
						string filepath = FilenameHelper.FillVariables(conf.OutputFilePath);
						fullPath = Path.Combine(filepath,filename);
						
						// Catching any exception to prevent that the user can't write in the directory.
						// This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218, #3004642
						try {
							ImageOutput.Save(image, fullPath);
							fileWritten = true;
							outputMade = true;
						} catch (Exception e) {
							LOG.Error("Error saving screenshot!", e);
							// Show the problem
							MessageBox.Show(lang.GetString(LangKey.error_save), lang.GetString(LangKey.error));
							// when save failed we present a SaveWithDialog
							fullPath = ImageOutput.SaveWithDialog(image, captureDetails);
							fileWritten = (fullPath != null);
						}
					} 
	
					if (captureDestinations.Contains(CaptureDestination.FileWithDialog)) {
						fullPath = ImageOutput.SaveWithDialog(image, captureDetails);
						fileWritten = (fullPath != null);
						outputMade = outputMade || fileWritten;
					}
	
					if (captureDestinations.Contains(CaptureDestination.Clipboard)) {
						ClipboardHelper.SetClipboardData(image);
						outputMade = true;
					}
	
					if (captureDestinations.Contains(CaptureDestination.Printer)) {
						PrinterSettings printerSettings = new PrintHelper(image, captureDetails).PrintWithDialog();
						outputMade = outputMade || printerSettings != null;
					}
					
					if (captureDestinations.Contains(CaptureDestination.EMail)) {
						if (!fileWritten) {
							MapiMailMessage.SendImage(image, captureDetails);
						} else {
							MapiMailMessage.SendImage(fullPath, captureDetails.Title, false);
						}
						// Don't know how to handle a cancel in the email
						outputMade = true;
					}
				}				
			}
			// Make sure we don't have garbage before opening the screenshot
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			// If the editor is opened, let it Dispose the surface!
			if (captureDestinations.Contains(CaptureDestination.Editor)) {
				try {
					ImageEditorForm editor = new ImageEditorForm(surface, outputMade);
	
					if (fullPath != null) {
						editor.SetImagePath(fullPath);
					}
					editor.Show();
					editor.Activate();
					LOG.Debug("Finished opening Editor");
				} catch (Exception e) {
					// Dispose the surface when an error is caught
					surface.Dispose();
					throw e;
				}
			} else {
				// Dispose the surface, we are done with it!
				surface.Dispose();
			}
							
			// Make CaptureForm invisible
			this.Visible = false;
			// Hiding makes the editor (if any) get focus
			this.Hide();
		}
		
		/**
		 * Finishing the whole Capture with Feedback flow, passing the result on to the HandleCapture
		 */
		private void finishCaptureWithFeedback() {
			// Get title
			if (selectedCaptureWindow != null) {
				if (capture == null) {
					capture = new Capture();
				}
				capture.CaptureDetails.Title = selectedCaptureWindow.Text;
			}

			if (captureRect.Height > 0 && captureRect.Width > 0) {
				// Take the captureRect, this already is specified as bitmap coordinates
				capture.Crop(captureRect);
				// save for re-capturing later and show recapture context menu option
				RuntimeConfig.LastCapturedRegion = captureRect;

				StopCapturing(false);
				HandleCapture();
			}
		}

		/**
		 * Stopping the whole Capture with Feedback flow
		 */
		private void StopCapturing(bool cleanupCapture) {
			mouseDown = false;
			// Disable the capture mode
			captureMode = CaptureMode.None;
			cursorPos.X = 0;
			cursorPos.Y = 0;
			captureRect = Rectangle.Empty;
			if (cleanupCapture && capture != null) {
				capture.Dispose();
				capture = null;
			}
			this.Hide();
			
			// Example of how to capture a scrolling window... somewhat...
//			if (selectedCaptureWindow.hasVScroll()) {
//				using (Image captured = selectedCaptureWindow.PrintWithVScroll()) {
//					Quantizer quantizer = new OctreeQuantizer(255,8);
//					using (Image imageToSave = quantizer.Quantize(captured)) {
//						imageToSave.Save(@"D:\BLA.PNG", ImageFormat.Png);
//					}
//				}
//			}
			selectedCaptureWindow = null;
		}

		#region key handling
		void CaptureFormKeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Escape) {
				StopCapturing(true);
			} else if (e.KeyCode == Keys.M) {
				// Toggle mouse cursor
				capture.CursorVisible = !capture.CursorVisible;
				PictureBoxMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0));
			} else if (e.KeyCode == Keys.Space) {
				switch (captureMode) {
					case CaptureMode.Region:
						captureMode = CaptureMode.Window;
						break;
					case CaptureMode.Window:
						captureMode = CaptureMode.Region;
						break;
				}
				pictureBox.Invalidate();
				selectedCaptureWindow = null;
				PictureBoxMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0));
			} else if (e.KeyCode == Keys.Return && captureMode == CaptureMode.Window) {
				finishCaptureWithFeedback();
			}
		}
		#endregion

		private bool CaptureActiveWindow() {
			bool presupplied = false;
			LOG.Debug("CaptureActiveWindow");
			if (selectedCaptureWindow != null) {
				LOG.Debug("Using supplied window");
				presupplied = true;
			} else {
				selectedCaptureWindow = WindowDetails.GetActiveWindow();
				if (selectedCaptureWindow != null) {
					LOG.DebugFormat("Capturing window: {0} with {1}", selectedCaptureWindow.Text, selectedCaptureWindow.ClientRectangle);
				}
			}
			if (selectedCaptureWindow == null || (!presupplied && selectedCaptureWindow.Iconic)) {
				LOG.Warn("No window to capture!");
				// Nothing to capture, code up in the stack will capture the full screen
				return false;
			}
			if (selectedCaptureWindow.Iconic) {
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
			return CaptureWindow(selectedCaptureWindow, capture, conf.WindowCaptureMode) != null;
		}

		/// <summary>
		/// Select the window to capture, this has logic which takes care of certain special applications
		/// like TOAD or Excel
		/// </summary>
		/// <param name="windowToCapture">WindowDetails with the target Window</param>
		/// <returns>WindowDetails with the target Window OR a replacement</returns>
		public static WindowDetails SelectCaptureWindow(WindowDetails windowToCapture) {
			Rectangle windowRectangle = windowToCapture.ClientRectangle;
			if (windowToCapture.Iconic || windowRectangle.Width == 0 || windowRectangle.Height == 0) {
				LOG.WarnFormat("Window {0} has nothing to capture, using workaround to find other window of same process.", windowToCapture.Text);
				// Trying workaround, the size 0 arrises with e.g. Toad.exe, has a different Window when minimized
				WindowDetails linkedWindow = WindowDetails.GetLinkedWindow(windowToCapture);
				if (linkedWindow != null) {
					windowRectangle = linkedWindow.ClientRectangle;
					windowToCapture = linkedWindow;
				} else {
					return null;
				}
			}
			return windowToCapture;
		}
		
		/// <summary>
		/// Capture the supplied Window
		/// </summary>
		/// <param name="windowToCapture">Window to capture</param>
		/// <param name="captureForWindow">The capture to store the details</param>
		/// <param name="windowCaptureMode">What WindowCaptureMode to use</param>
		/// <returns></returns>
		public static ICapture CaptureWindow(WindowDetails windowToCapture, ICapture captureForWindow, WindowCaptureMode windowCaptureMode) {
			Rectangle windowRectangle = windowToCapture.ClientRectangle;
			if (windowToCapture.Iconic) {
				// Restore the window making sure it's visible!
				windowToCapture.Restore();
			}

			// When Vista & DWM (Aero) enabled
			bool dwmEnabled = DWM.isDWMEnabled();
				
			// For WindowCaptureMode.Auto we check:
			// 1) Is window IE, use IE Capture
			// 2) Is Windows >= Vista & DWM enabled: use DWM
			// 3) Otherwise use GDI (Screen might be also okay but might lose content)
			if (windowCaptureMode == WindowCaptureMode.Auto) {
				if (windowToCapture.ClassName == "IEFrame") {
					ICapture ieCapture = IECaptureHelper.CaptureIE(captureForWindow);
					if (ieCapture != null) {
						return ieCapture;
					}
				}
				
				// Take default GDI
				windowCaptureMode = WindowCaptureMode.GDI;
				// Take DWM if enabled
				if (dwmEnabled) {
					windowCaptureMode = WindowCaptureMode.Aero;
				}
			} else if (windowCaptureMode == WindowCaptureMode.Aero || windowCaptureMode == WindowCaptureMode.AeroTransparent) {
				if (!dwmEnabled) {
					windowCaptureMode = WindowCaptureMode.GDI;
				}
			}
			LOG.DebugFormat("Capturing window with mode {0}", windowCaptureMode);
			switch(windowCaptureMode) {
				case WindowCaptureMode.GDI:
					// GDI
					captureForWindow = windowToCapture.CaptureWindow(captureForWindow);
					break;
				case WindowCaptureMode.Aero:
				case WindowCaptureMode.AeroTransparent:
					// DWM
					captureForWindow = windowToCapture.CaptureDWMWindow(captureForWindow, windowCaptureMode);
					break;
				case WindowCaptureMode.Screen:
				default:
					// Screen capture
					windowRectangle.Intersect(captureForWindow.ScreenBounds);
					try {
						captureForWindow = WindowCapture.CaptureRectangle(captureForWindow, windowRectangle);
					} catch (Exception e) {
						LOG.Error("Problem capturing", e);
						return null;
					}
					break;
			}
			captureForWindow.CaptureDetails.Title = windowToCapture.Text;
			((Bitmap)captureForWindow.Image).SetResolution(captureForWindow.CaptureDetails.DpiX, captureForWindow.CaptureDetails.DpiY);
			return captureForWindow;
		}

		#region capture with feedback
		private void CaptureWithFeedback() {
			windows.Clear();
			// Start Enumeration of "active" windows
			foreach(WindowDetails window in WindowDetails.GetAllWindows()) {
				// Window should be visible and not ourselves
				if (!window.Visible) {
					continue;
				}
				if (window.Handle.Equals(this.Handle)) {
					continue;
				}
				// Skip empty
				Size windowSize = window.ClientRectangle.Size;
				if (windowSize.Width == 0 ||  windowSize.Height == 0) {
					continue;
				}
				windows.Add(window);
			}

			// Reset "previous" cursor location
			cursorPos = WindowCapture.GetCursorLocation();
			// Offset to screen coordinates
			cursorPos.Offset(-capture.ScreenBounds.X, -capture.ScreenBounds.Y);

			this.SuspendLayout();
			pictureBox.Image = capture.Image;
			this.Bounds = capture.ScreenBounds;
			this.ResumeLayout();
			this.Visible = true;
			// Fix missing focus
			WindowDetails.ToForeground(this.Handle);
		}
		
		/// <summary>
		/// Helper Method for finding the current Window in the available rectangles
		/// </summary>
		/// <returns>WindowDetails</returns>
		private WindowDetails FindCurrentWindow() {
			foreach(WindowDetails window in windows) {
				Rectangle windowRectangle = window.ClientRectangle;
				if (windowRectangle.Contains(Cursor.Position)) {
					WindowDetails selectedChild = null;
					Rectangle selectedChildRectangle = Rectangle.Empty;
					// Check if Children need to be parsed (only if "pgdn" was used)
					foreach(WindowDetails childWindow in window.Children) {
						windowRectangle = childWindow.ClientRectangle;
						if (windowRectangle.Contains(Cursor.Position)) {
							if (selectedChild == null) {
								selectedChild = childWindow;
								selectedChildRectangle = selectedChild.ClientRectangle;
							} else {
								Rectangle childRectangle = childWindow.ClientRectangle;
								int sizeCurrent = childRectangle.Height * childRectangle.Width;
								int sizeSelected = selectedChildRectangle.Height * selectedChildRectangle.Width;
								if (sizeCurrent < sizeSelected) {
									selectedChild = childWindow;
								}
							}
						}
					}
					if (selectedChild != null) {
						return selectedChild;
					}
					return window;
				}
			}
			return null;
		}
		#endregion

		#region pictureBox events
		void PictureBoxMouseDown(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				Point tmpCursorLocation = WindowCapture.GetCursorLocation();
				// As the cursorPos is not in Bitmap coordinates, we need to correct.
				tmpCursorLocation.Offset(-capture.ScreenBounds.Location.X, -capture.ScreenBounds.Location.Y);

				mX = tmpCursorLocation.X;
				mY = tmpCursorLocation.Y;
				mouseDown = true;
				PictureBoxMouseMove(this, e);
				pictureBox.Invalidate();
			}
		}
		
		void PictureBoxMouseUp(object sender, MouseEventArgs e) {
			if (mouseDown) {
				pictureBox.Invalidate();
				// If the mouse goes up we set down to false (nice logic!)
				mouseDown = false;
				// Check if anything is selected
				if (captureMode == CaptureMode.Window && selectedCaptureWindow != null) {
					// Go and process the capture
					finishCaptureWithFeedback();
				} else if (captureRect.Height > 0 && captureRect.Width > 0) {
					// correct the GUI width to real width if Region mode
					if (captureMode == CaptureMode.Region) {
						captureRect.Width += 1;
						captureRect.Height += 1;
					}
					// Go and process the capture
					finishCaptureWithFeedback();
				}
			}
		}

		void PictureBoxMouseMove(object sender, MouseEventArgs e) {
			Point lastPos = new Point(cursorPos.X, cursorPos.Y);
			cursorPos = WindowCapture.GetCursorLocation();
			// As the cursorPos is not in Bitmap coordinates, we need to correct.
			cursorPos.Offset(-capture.ScreenBounds.Location.X, -capture.ScreenBounds.Location.Y);
			Rectangle lastCaptureRect = new Rectangle(captureRect.Location, captureRect.Size);
			WindowDetails lastWindow = selectedCaptureWindow;
			bool horizontalMove = false;
			bool verticalMove = false;

			if (lastPos.X != cursorPos.X) {
				horizontalMove = true;
			}
			if (lastPos.Y != cursorPos.Y) {
				verticalMove = true;
			}

			if (captureMode == CaptureMode.Region && mouseDown) {
				captureRect = GuiRectangle.GetGuiRectangle(cursorPos.X, cursorPos.Y, mX - cursorPos.X, mY - cursorPos.Y);
			}
			
			// Iterate over the found windows and check if the current location is inside a window
			selectedCaptureWindow = FindCurrentWindow();
			if (selectedCaptureWindow != null && !selectedCaptureWindow.Equals(lastWindow)) {
				if (capture == null) {
					capture = new Capture();
				}
				capture.CaptureDetails.Title = selectedCaptureWindow.Text;
				capture.CaptureDetails.AddMetaData("windowtitle", selectedCaptureWindow.Text);
				if (captureMode == CaptureMode.Window) {
					// Here we want to capture the window which is under the mouse
					captureRect = selectedCaptureWindow.ClientRectangle;
					// As the ClientRectangle is not in Bitmap coordinates, we need to correct.
					captureRect.Offset(-capture.ScreenBounds.Location.X, -capture.ScreenBounds.Location.Y);
				}
			}
			if (mouseDown) {
				int x1 = Math.Min(mX, lastPos.X);
				int x2 = Math.Max(mX, lastPos.X);
				int y1 = Math.Min(mY, lastPos.Y);
				int y2 = Math.Max(mY, lastPos.Y);
				x1= Math.Min(x1, cursorPos.X);
				x2= Math.Max(x2, cursorPos.X);
				y1= Math.Min(y1, cursorPos.Y);
				y2= Math.Max(y2, cursorPos.Y);

				// Safety correction
				x2 += 2;
				y2 += 2;

				// Here we correct for text-size
				
				// Calculate the size
				int textForWidth = Math.Max(Math.Abs(mX - cursorPos.X), Math.Abs(mX - lastPos.X));
				int textForHeight = Math.Max(Math.Abs(mY - cursorPos.Y), Math.Abs(mY - lastPos.Y));

				using (Font rulerFont = new Font(FontFamily.GenericSansSerif, 8)) {
					Size measureWidth = TextRenderer.MeasureText(textForWidth.ToString(), rulerFont);
					x1 -= measureWidth.Width + 15;

					Size measureHeight = TextRenderer.MeasureText(textForHeight.ToString(), rulerFont);
					y1 -= measureWidth.Height + 10;
				}
				Rectangle invalidateRectangle = new Rectangle(x1,y1, x2-x1, y2-y1);
				pictureBox.Invalidate(invalidateRectangle);
			} else {
				if (captureMode == CaptureMode.Window) {
					if (!selectedCaptureWindow.Equals(lastWindow)) {
						// Using a 50 Pixel offset to the left, top, to make sure the text is invalidated too
						const int SAFETY_SIZE = 50;
						Rectangle invalidateRectangle = new Rectangle(lastCaptureRect.Location, lastCaptureRect.Size);
						invalidateRectangle.X -= SAFETY_SIZE;
						invalidateRectangle.Y -= SAFETY_SIZE;
						invalidateRectangle.Width += SAFETY_SIZE;
						invalidateRectangle.Height += SAFETY_SIZE;
						pictureBox.Invalidate(invalidateRectangle);
						invalidateRectangle = new Rectangle(captureRect.Location, captureRect.Size);
						invalidateRectangle.X -= SAFETY_SIZE;
						invalidateRectangle.Y -= SAFETY_SIZE;
						invalidateRectangle.Width += SAFETY_SIZE;
						invalidateRectangle.Height += SAFETY_SIZE;
						pictureBox.Invalidate(invalidateRectangle);
					}
				} else {
					if (verticalMove) {
						Rectangle before = GuiRectangle.GetGuiRectangle(0, lastPos.Y - 2, this.Width, 45);
						Rectangle after = GuiRectangle.GetGuiRectangle(0, cursorPos.Y - 2, this.Width, 45);
						pictureBox.Invalidate(before);
						pictureBox.Invalidate(after);
					}
					if (horizontalMove) {
						Rectangle before = GuiRectangle.GetGuiRectangle(lastPos.X - 2, 0, 75, this.Height);
						Rectangle after = GuiRectangle.GetGuiRectangle(cursorPos.X -2, 0, 75, this.Height);
						pictureBox.Invalidate(before);
						pictureBox.Invalidate(after);
					}
				}
			}
		}
		
		void PictureBoxPaint(object sender, PaintEventArgs e) {
			Graphics graphics = e.Graphics;
			Rectangle clipRectangle = e.ClipRectangle;
			// Only draw Cursor if it's (partly) visible
			if (capture.Cursor != null && capture.CursorVisible && clipRectangle.IntersectsWith(new Rectangle(capture.CursorLocation, capture.Cursor.Size))) {
				graphics.DrawIcon(capture.Cursor, capture.CursorLocation.X, capture.CursorLocation.Y);
			}

			if (mouseDown || captureMode == CaptureMode.Window) {
				captureRect.Intersect(new Rectangle(Point.Empty, capture.ScreenBounds.Size)); // crop what is outside the screen
				Rectangle fixedRect = new Rectangle( captureRect.X, captureRect.Y, captureRect.Width, captureRect.Height );
				graphics.FillRectangle( OverlayBrush, fixedRect );
				graphics.DrawRectangle( OverlayPen, fixedRect );
				
				// rulers
				int dist = 8;
				
				string captureWidth = (captureRect.Width + 1).ToString();
				string captureHeight = (captureRect.Height + 1).ToString();

				using (Font rulerFont = new Font(FontFamily.GenericSansSerif, 8)) {
					Size measureWidth = TextRenderer.MeasureText(captureWidth, rulerFont);
					Size measureHeight = TextRenderer.MeasureText(captureHeight, rulerFont);
					int hSpace = measureWidth.Width + 3;
					int vSpace = measureHeight.Height + 3;
					Brush bgBrush = new SolidBrush(Color.FromArgb(200, 217, 240, 227));
					Pen rulerPen = new Pen(Color.SeaGreen);
					
					// horizontal ruler
					if (fixedRect.Width > hSpace + 3) {
						using (GraphicsPath p = Drawing.RoundedRectangle.Create2(
						                fixedRect.X + (fixedRect.Width / 2 - hSpace / 2) + 3, 
						                fixedRect.Y - dist - 7,
						                measureWidth.Width - 3,
						                measureWidth.Height,
						                3)) {
							graphics.FillPath(bgBrush, p);
							graphics.DrawPath(rulerPen, p);
							graphics.DrawString(captureWidth, rulerFont, rulerPen.Brush, fixedRect.X + (fixedRect.Width / 2 - hSpace / 2) + 3, fixedRect.Y - dist - 7);
							graphics.DrawLine(rulerPen, fixedRect.X, fixedRect.Y - dist, fixedRect.X + (fixedRect.Width / 2 - hSpace / 2), fixedRect.Y - dist);
							graphics.DrawLine(rulerPen, fixedRect.X + (fixedRect.Width / 2 + hSpace / 2), fixedRect.Y - dist, fixedRect.X + fixedRect.Width, fixedRect.Y - dist);
							graphics.DrawLine(rulerPen, fixedRect.X, fixedRect.Y - dist - 3, fixedRect.X, fixedRect.Y - dist + 3);
							graphics.DrawLine(rulerPen, fixedRect.X + fixedRect.Width, fixedRect.Y - dist - 3, fixedRect.X + fixedRect.Width, fixedRect.Y - dist + 3);
						}
					}
					
					// vertical ruler
					if (fixedRect.Height > vSpace + 3) {
						using (GraphicsPath p = Drawing.RoundedRectangle.Create2(
						                fixedRect.X - measureHeight.Width + 1, 
						                fixedRect.Y + (fixedRect.Height / 2 - vSpace / 2) + 2,
						                measureHeight.Width - 3,
						                measureHeight.Height - 1,
						                3)) {
							graphics.FillPath(bgBrush, p);
							graphics.DrawPath(rulerPen, p);
							graphics.DrawString(captureHeight, rulerFont, rulerPen.Brush, fixedRect.X - measureHeight.Width + 1, fixedRect.Y + (fixedRect.Height / 2 - vSpace / 2) + 2);
							graphics.DrawLine(rulerPen, fixedRect.X - dist, fixedRect.Y, fixedRect.X - dist, fixedRect.Y + (fixedRect.Height / 2 - vSpace / 2));
							graphics.DrawLine(rulerPen, fixedRect.X - dist, fixedRect.Y + (fixedRect.Height / 2 + vSpace / 2), fixedRect.X - dist, fixedRect.Y + fixedRect.Height);
							graphics.DrawLine(rulerPen, fixedRect.X - dist - 3, fixedRect.Y, fixedRect.X - dist + 3, fixedRect.Y);
							graphics.DrawLine(rulerPen, fixedRect.X - dist - 3, fixedRect.Y + fixedRect.Height, fixedRect.X - dist + 3, fixedRect.Y + fixedRect.Height);
						}
					}
					
					rulerPen.Dispose();
					bgBrush.Dispose();
				}
				
				// Display size of selected rectangle
				// Prepare the font and text.
				using (Font sizeFont = new Font( FontFamily.GenericSansSerif, 12 )) {
					// When capturing a Region we need to add 1 to the height/width for correction
					string sizeText = null;
					if (captureMode == CaptureMode.Region) {
							// correct the GUI width to real width for the shown size
							sizeText = (captureRect.Width + 1) + " x " + (captureRect.Height + 1);
					} else {
						sizeText = captureRect.Width + " x " + captureRect.Height;
					}
					
					// Calculate the scaled font size.
					SizeF extent = graphics.MeasureString( sizeText, sizeFont );
					float hRatio = captureRect.Height / (extent.Height * 2);
					float wRatio = captureRect.Width / (extent.Width * 2);
					float ratio = ( hRatio < wRatio ? hRatio : wRatio );
					float newSize = sizeFont.Size * ratio;
					
					if ( newSize >= 4 ) {
						// Only show if 4pt or larger.
						if (newSize > 20) {
							newSize = 20;
						}
						// Draw the size.
						using (Font newSizeFont = new Font(FontFamily.GenericSansSerif, newSize, FontStyle.Bold)) {
							PointF sizeLocation = new PointF( fixedRect.X + ( captureRect.Width / 2) - (extent.Width / 2), fixedRect.Y + (captureRect.Height / 2) - (sizeFont.GetHeight() / 2));
							graphics.DrawString(sizeText, sizeFont, Brushes.LightSeaGreen, sizeLocation);
						}
					}
				}
			} else {
				if (cursorPos.X >= 0 || cursorPos.Y >= 0) {
					using (Pen pen = new Pen(Color.LightSeaGreen)) {
						pen.DashStyle = DashStyle.Dot;
						Rectangle screenBounds = capture.ScreenBounds;
						graphics.DrawLine(pen, cursorPos.X, screenBounds.Y, cursorPos.X, screenBounds.Height);
						graphics.DrawLine(pen, screenBounds.X, cursorPos.Y, screenBounds.Width, cursorPos.Y);
					}
					
					string xy = cursorPos.X + " x " + cursorPos.Y;
					using (Font f = new Font(FontFamily.GenericSansSerif, 8)) {
						Size xySize = TextRenderer.MeasureText(xy, f);
						using (GraphicsPath gp = Drawing.RoundedRectangle.Create2(
							cursorPos.X + 5,
							cursorPos.Y + 5,
							xySize.Width - 3,
							xySize.Height,
							3)) {
							using (Brush bgBrush = new SolidBrush(Color.FromArgb(200, 217, 240, 227))) {
								graphics.FillPath(bgBrush, gp);
							}
							using (Pen pen = new Pen(Color.SeaGreen)) {
								graphics.DrawPath(pen, gp);
								Point coordinatePosition = new Point(cursorPos.X + 5, cursorPos.Y + 5);
								graphics.DrawString(xy, f, pen.Brush, coordinatePosition);
							}
						}
					}
				}
			}
		}
		#endregion
		
		#region Form Events
		private void CaptureFormVisibleChanged( object sender, EventArgs e ) {
 			if ( !this.Visible && pictureBox.Image != null ) {
 				Image img = pictureBox.Image;
 				pictureBox.Image = null;
 				img.Dispose();
 			}
 		}
		#endregion
	}
}
