/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Capturing;
using Greenshot.Configuration;
using Greenshot.Core;
using Greenshot.Drawing;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.UnmanagedHelpers;

namespace Greenshot.Forms {
	/// <summary>
	/// Description of CaptureForm.
	/// </summary>
	public partial class CaptureForm : Form, ICaptureHost {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(CaptureForm));

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
		private CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private ILanguage lang = Language.GetInstance();

		public CaptureForm() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			// Make sure the form is hidden (might be overdoing it...)
			this.Hide();
		}

		void DoCaptureFeedback() {
			if(conf.PlayCameraSound) {
				SoundHelper.Play();
			}
			if(conf.ShowFlash) {
				FlashlightForm flashlightForm = new FlashlightForm();
				flashlightForm.Bounds = capture.ScreenBounds;
				flashlightForm.FadeIn();
				flashlightForm.FadeOut();
				flashlightForm.Dispose();
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

			// Delay for the Context menu
			System.Threading.Thread.Sleep(conf.CaptureDelay);

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
					CaptureWithFeedback();
					break;
				case CaptureMode.ActiveWindow:
					CaptureActiveWindow();
					finishCapture();
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
					} else if (ClipboardHelper.GetFormats().Contains("HTML Format")) {
						HtmlFragment htmlFragment = HtmlFragment.FromClipboard();
						text = htmlFragment.Fragment;
						clipboardImage = WebsiteImageGenerator.GetImageFromHTML(text);
					} else {
						text = Clipboard.GetText();
						if ((text != null && text.StartsWith("http://"))) {
							clipboardImage = WebsiteImageGenerator.GetImageFromURL(text);
						}
					}
					if (clipboardImage != null) {
						if (capture != null) {
							capture.Image = clipboardImage;
						} else {
							capture = new Capture(clipboardImage);
						}
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
						HandleCapture();
					}
					break;
				case CaptureMode.Region:
					capture = WindowCapture.CaptureScreen(capture);
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

			// Create Image for writing/printing etc and use "using" as all code paths either output the image or copy the image
			using (Image image = surface.GetImageForExport()) {
				// Flag to detect if we need to create a temp file for the email
				// or use the file that was written
				bool fileWritten = false;
				if (captureDestinations.Contains(CaptureDestination.File)) {
					string filename = FilenameHelper.GetFilenameFromPattern(conf.OutputFileFilenamePattern, conf.OutputFileFormat, captureDetails);
					string filepath = FilenameHelper.FillVariables(conf.OutputFilePath);
					fullPath = Path.Combine(filepath,filename);
					
					// Catching any exception to prevent that the user can't write in the directory.
					// This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218, #3004642
					try {
						ImageOutput.Save(image, fullPath, captureDetails);
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
			
			// If the editor is opened, let it Dispose the surface!
			if (captureDestinations.Contains(CaptureDestination.Editor)) {
				ImageEditorForm editor = new ImageEditorForm(surface, outputMade);

				if (fullPath != null) {
					editor.SetImagePath(fullPath);
				}
				editor.Show();
				editor.Activate();
				LOG.Debug("Finished opening Editor");
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
		private void finishCapture() {
			// Get title
			if (selectedCaptureWindow != null) {
				if (capture == null) {
					capture = new Capture();
				}
				capture.CaptureDetails.Title = selectedCaptureWindow.Text;
			}

			if ( (captureMode == CaptureMode.Window || captureMode == CaptureMode.ActiveWindow) && selectedCaptureWindow != null) {
				Image capturedWindowImage = null;
				// What type of capturing? (From Screen or from window)
				if (conf.CaptureCompleteWindow) {
					// "Capture" the windows content
					capturedWindowImage = selectedCaptureWindow.Image;
					if (capturedWindowImage != null) {
						// Fix Cursor location as we don't crop
						capture.MoveMouseLocation(-selectedCaptureWindow.Rectangle.Location.X, -selectedCaptureWindow.Rectangle.Location.Y);
						// Set the image
						capture.Image = capturedWindowImage;
					}
				}
				// If the PrintWindow implementation isn't used or failed we use the image from the screen.
				if (capturedWindowImage == null) {
					// From screen, take the location of the selected window to copy the content
					captureRect = selectedCaptureWindow.Rectangle;
					// Cropping capture to the selected rectangle
					capture.CropWithScreenCoordinates(captureRect);
					// save for re-capturing later and show recapture context menu option
					RuntimeConfig.LastCapturedRegion = captureRect;
				}
				StopCapturing(false);
				HandleCapture();
			} else if (captureRect.Height > 0 && captureRect.Width > 0) {
				// Resizing the captured rectangle (no need to make another capture
				capture.CropWithScreenCoordinates(captureRect);
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
			selectedCaptureWindow = null;
			if (cleanupCapture && capture != null) {
				capture.Dispose();
				capture = null;
			}
			this.Hide();
		}

		#region key handling
		void CaptureFormKeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Escape) {
				StopCapturing(true);
			} else if (e.KeyCode == Keys.M) {
				// Toggle mouse cursor
				capture.CursorVisible = !capture.CursorVisible;
				PictureBoxMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0));
			} else if (e.KeyCode == Keys.PageDown) {
				// Extend the selectable rectangles with the "insides" of the current Window 
				if (captureMode == CaptureMode.Window) {
					WindowDetails currentWindow = FindCurrentWindow();
					if (currentWindow != null && !currentWindow.HasChildren) {
						currentWindow.GetChildren();
					} else {
						LOG.Warn("No window found!!");
					}
				}
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
				PictureBoxMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0));
			} else if (e.KeyCode == Keys.Return && captureMode == CaptureMode.Window) {
				finishCapture();
			}
		}
		#endregion

		private void CaptureActiveWindow() {
			LOG.Debug("CaptureActiveWindow");
			IntPtr hWnd = User32.GetForegroundWindow();
			if (hWnd != null && hWnd != IntPtr.Zero) {
				// Make sure the screen is captured in case of errors or  if we don't do direct window capturing
				capture = WindowCapture.CaptureScreen(capture);
				selectedCaptureWindow = new WindowDetails(hWnd);
				// Content only
				if (conf.CaptureWindowContent) {
					// Print Tree for debugging
					selectedCaptureWindow.PrintTree("");
					WindowDetails contentWindow = selectedCaptureWindow.GetContent();
					if (contentWindow != null) {
						selectedCaptureWindow = contentWindow;
					}
				}
			}
		}

		#region capture with feedback
		private void CaptureWithFeedback() {
			windows.Clear();
			// Start Enumeration of "active" windows
			WindowsEnumerator windowsEnumerator = new WindowsEnumerator();
			windowsEnumerator.GetWindows();
			foreach(WindowDetails window in windowsEnumerator.Items) {
				// Window should be visible and not ourselves
				if (window.Visible && !window.Handle.Equals(this.Handle)) {
					windows.Add(window);
				}
			}

			this.SuspendLayout();
			this.Bounds = capture.ScreenBounds;
			pictureBox.Image = capture.Image;
			this.Visible = true;
			this.ResumeLayout();
			this.Focus();
			this.Show();
		}
		
		/// <summary>
		/// Helper Method for finding the current Window in the available rectangles
		/// </summary>
		/// <returns>WindowDetails</returns>
		private WindowDetails FindCurrentWindow() {
			foreach(WindowDetails window in windows) {
				Rectangle windowRectangle = window.Rectangle;
				if (windowRectangle.Contains(Cursor.Position)) {
					WindowDetails selectedChild = null;
					// Content only
					if (conf.CaptureWindowContent) {
						WindowDetails childWindow = window.GetContent();
						if (childWindow != null && childWindow.Rectangle.Contains(Cursor.Position)) {
							return childWindow;
						}
					}
					// Check if Children need to be parsed (only if "pgdn" was used)
					if (window.HasChildren) {
 						foreach(WindowDetails childWindow in window.Children) {
							windowRectangle = childWindow.Rectangle;
							if (windowRectangle.Contains(Cursor.Position)) {
								if (selectedChild == null) {
									selectedChild = childWindow;
								} else {
									int sizeCurrent = childWindow.Rectangle.Height * childWindow.Rectangle.Width;
									int sizeSelected = selectedChild.Rectangle.Height * selectedChild.Rectangle.Width;
									if (sizeCurrent < sizeSelected) {
										selectedChild = childWindow;
									}
								}
							}
						}
						if (selectedChild != null) {
							return selectedChild;
						}
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
				mX = e.X;
				mY = e.Y;
				mouseDown = true;
				PictureBoxMouseMove(this, e);
			}
		}
		
		void PictureBoxMouseUp(object sender, MouseEventArgs e) {
			if (mouseDown) {
				// If the mouse goes up we set down to false (nice logic!)
				mouseDown = false;
				// Check if anything is selected
				if (captureMode == CaptureMode.Window && selectedCaptureWindow != null) {
					// Go and process the capture
					finishCapture();
				} else if (captureRect.Height > 0 && captureRect.Width > 0) {
					// correct the GUI width to real width if Region mode
					if (captureMode == CaptureMode.Region) {
						captureRect.Width += 1;
						captureRect.Height += 1;
					}
					// Go and process the capture
					finishCapture();
				}
			}
		}

		void PictureBoxMouseMove(object sender, MouseEventArgs e) {
			cursorPos.X = e.X;
			cursorPos.Y = e.Y;

			if (captureMode == CaptureMode.Region && mouseDown) {
				captureRect = GuiRectangle.GetGuiRectangle(e.X + this.Left, e.Y + this.Top, mX - e.X, mY - e.Y);
			}
			
			// Iterate over the found windows and check if the current location is inside a window
			selectedCaptureWindow = FindCurrentWindow();
			if (selectedCaptureWindow != null) {
				if (capture == null) {
					capture = new Capture();
				}
				capture.CaptureDetails.Title = selectedCaptureWindow.Text;
				if (captureMode == CaptureMode.Window) {
					captureRect = selectedCaptureWindow.Rectangle;
				}
			}
		
			pictureBox.Invalidate();
		}
		
		void PictureBoxPaint(object sender, PaintEventArgs e) {
			Graphics graphics = e.Graphics;
			
			if (capture.Cursor != null && capture.CursorVisible) {
				graphics.DrawIcon(capture.Cursor, capture.CursorLocation.X, capture.CursorLocation.Y);
			}

			if (mouseDown || captureMode == CaptureMode.Window) {
				Rectangle screenbounds = capture.ScreenBounds;
				captureRect.Intersect(screenbounds); // crop what is outside the screen
				Rectangle fixedRect = new Rectangle( captureRect.X, captureRect.Y, captureRect.Width, captureRect.Height );
				fixedRect.X += Math.Abs( screenbounds.X );
				fixedRect.Y += Math.Abs( screenbounds.Y );

				graphics.FillRectangle( OverlayBrush, fixedRect );
				graphics.DrawRectangle( OverlayPen, fixedRect );
				
				// rulers
				int dist = 8;

				using (Font rulerFont = new Font(FontFamily.GenericSansSerif, 8)) {
					int hSpace = TextRenderer.MeasureText(captureRect.Width.ToString(), rulerFont).Width + 3;
					int vSpace = TextRenderer.MeasureText(captureRect.Height.ToString(), rulerFont).Height + 3;
					Brush bgBrush = new SolidBrush(Color.FromArgb(200, 217, 240, 227));
					Pen rulerPen = new Pen(Color.SeaGreen);
					
					// horizontal ruler
					if (fixedRect.Width > hSpace + 3) {
						using (GraphicsPath p = Drawing.RoundedRectangle.Create2(
						                fixedRect.X + (fixedRect.Width / 2 - hSpace / 2) + 3, 
						                fixedRect.Y - dist - 7,
						                TextRenderer.MeasureText(captureRect.Width.ToString(), rulerFont).Width - 3,
						                TextRenderer.MeasureText(captureRect.Width.ToString(), rulerFont).Height,
						                3)) {
							graphics.FillPath(bgBrush, p);
							graphics.DrawPath(rulerPen, p);
							graphics.DrawString(captureRect.Width.ToString(), rulerFont, rulerPen.Brush, fixedRect.X + (fixedRect.Width / 2 - hSpace / 2) + 3, fixedRect.Y - dist - 7);
							graphics.DrawLine(rulerPen, fixedRect.X, fixedRect.Y - dist, fixedRect.X + (fixedRect.Width / 2 - hSpace / 2), fixedRect.Y - dist);
							graphics.DrawLine(rulerPen, fixedRect.X + (fixedRect.Width / 2 + hSpace / 2), fixedRect.Y - dist, fixedRect.X + fixedRect.Width, fixedRect.Y - dist);
							graphics.DrawLine(rulerPen, fixedRect.X, fixedRect.Y - dist - 3, fixedRect.X, fixedRect.Y - dist + 3);
							graphics.DrawLine(rulerPen, fixedRect.X + fixedRect.Width, fixedRect.Y - dist - 3, fixedRect.X + fixedRect.Width, fixedRect.Y - dist + 3);
						}
					}
					
					// vertical ruler
					if (fixedRect.Height > vSpace + 3) {
						using (GraphicsPath p = Drawing.RoundedRectangle.Create2(
						                fixedRect.X - (TextRenderer.MeasureText(captureRect.Height.ToString(), rulerFont).Width) + 1, 
						                fixedRect.Y + (fixedRect.Height / 2 - vSpace / 2) + 2,
						                TextRenderer.MeasureText(captureRect.Height.ToString(), rulerFont).Width - 3,
						                TextRenderer.MeasureText(captureRect.Height.ToString(), rulerFont).Height - 1,
						                3)) {
							graphics.FillPath(bgBrush, p);
							graphics.DrawPath(rulerPen, p);
							graphics.DrawString(captureRect.Height.ToString(), rulerFont, rulerPen.Brush, fixedRect.X - (TextRenderer.MeasureText(captureRect.Height.ToString(), rulerFont).Width) + 1, fixedRect.Y + (fixedRect.Height / 2 - vSpace / 2) + 2);
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
							PointF sizeLocation = new PointF( fixedRect.X + ( captureRect.Width / 2) - (TextRenderer.MeasureText(sizeText, sizeFont).Width / 2), fixedRect.Y + (captureRect.Height / 2) - (sizeFont.GetHeight() / 2));
							graphics.DrawString(sizeText, sizeFont, Brushes.LightSeaGreen, sizeLocation);
						}
					}
				}
			} else {
				if (cursorPos.X > 0 || cursorPos.Y > 0) {
					using (Pen pen = new Pen(Color.LightSeaGreen)) {
						pen.DashStyle = DashStyle.Dot;
						Rectangle screenBounds = capture.ScreenBounds;
						graphics.DrawLine(pen, cursorPos.X, screenBounds.Y, cursorPos.X, screenBounds.Height);
						graphics.DrawLine(pen, screenBounds.X, cursorPos.Y, screenBounds.Width, cursorPos.Y);
					}
					
					string xy = cursorPos.X.ToString() + " x " + cursorPos.Y.ToString();
					using (Font f = new Font(FontFamily.GenericSansSerif, 8)) {
						using (GraphicsPath gp = Drawing.RoundedRectangle.Create2(
							cursorPos.X + 5,
							cursorPos.Y + 5,
							TextRenderer.MeasureText(xy, f).Width - 3,
							TextRenderer.MeasureText(xy, f).Height,
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
