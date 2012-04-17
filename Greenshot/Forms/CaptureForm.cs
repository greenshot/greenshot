/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Drawing;
using Greenshot.Helpers;
using Greenshot.Plugin;
using GreenshotPlugin.UnmanagedHelpers;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace Greenshot.Forms {
	/// <summary>
	/// Description of CaptureForm.
	/// </summary>
	public partial class CaptureForm : Form {
		private enum FixMode {None, Initiated, Horizontal, Vertical};

		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(CaptureForm));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static Brush GreenOverlayBrush = new SolidBrush(Color.FromArgb(50, Color.MediumSeaGreen));
		private static Brush RedOverlayBrush = new SolidBrush(Color.FromArgb(50, Color.DarkRed));
		private static Pen OverlayPen = new Pen(Color.FromArgb(50, Color.Black));
		private static CaptureForm currentForm = null;

		private int mX;
		private int mY;
		private Point cursorPos = Point.Empty;
		private CaptureMode captureMode = CaptureMode.None;
		private List<WindowDetails> windows = new List<WindowDetails>();
		private WindowDetails selectedCaptureWindow;
		private bool mouseDown = false;
		private Rectangle captureRect = Rectangle.Empty;
		private ICapture capture = null;

		private Point previousMousePos = Point.Empty;
		private FixMode fixMode = FixMode.None;

		public Rectangle CaptureRectangle {
			get {
				return captureRect;
			}
		}

		public CaptureMode UsedCaptureMode {
			get {
				return captureMode;
			}
		}

		public WindowDetails SelectedCaptureWindow {
			get {
				return selectedCaptureWindow;
			}
		}

		public CaptureForm(ICapture capture, List<WindowDetails> windows) {
			if (currentForm != null) {
				LOG.Debug("Found currentForm, Closing already opened CaptureForm");
				currentForm.Close();
				currentForm = null;
				Application.DoEvents();
			}
			currentForm = this;

			// clean up
			this.FormClosed += delegate {
				currentForm = null;
				LOG.Debug("Remove CaptureForm from currentForm");
			};

			this.capture = capture;
			this.windows = windows;
			captureMode = capture.CaptureDetails.CaptureMode;

			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Text = "Greenshot capture form";

			// Make sure we never capture the captureform
			WindowDetails.RegisterIgnoreHandle(this.Handle);
			// TODO: Need to call unregister at close

			// set cursor location
			cursorPos = WindowCapture.GetCursorLocation();
			// Offset to screen coordinates
			cursorPos.Offset(-capture.ScreenBounds.X, -capture.ScreenBounds.Y);

			this.SuspendLayout();
			pictureBox.Image = capture.Image;
			this.Bounds = capture.ScreenBounds;
			this.ResumeLayout();
			
			// Fix missing focus
			WindowDetails.ToForeground(this.Handle);
		}

		#region key handling		
		void CaptureFormKeyUp(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.ShiftKey) {
				fixMode = FixMode.None;
			}
		}

		void CaptureFormKeyDown(object sender, KeyEventArgs e) {
			// Check fixmode
			if (e.KeyCode == Keys.ShiftKey) {
				if (fixMode == FixMode.None) {
					fixMode = FixMode.Initiated;
					return;
				}
			}
			if (e.KeyCode == Keys.Escape) {
				DialogResult = DialogResult.Cancel;
			} else if (e.KeyCode == Keys.M) {
				// Toggle mouse cursor
				capture.CursorVisible = !capture.CursorVisible;
				pictureBox.Invalidate();
			} else if (e.KeyCode == Keys.V && conf.isExperimentalFeatureEnabled("Video")) {
				capture.CaptureDetails.CaptureMode = CaptureMode.Video;
				pictureBox.Invalidate();
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
				DialogResult = DialogResult.OK;
			}
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
				// If the mouse goes up we set down to false (nice logic!)
				mouseDown = false;
				// Check if anything is selected
				if (captureMode == CaptureMode.Window && selectedCaptureWindow != null) {
					// Go and process the capture
					DialogResult = DialogResult.OK;
				} else if (captureRect.Height > 0 && captureRect.Width > 0) {
					// correct the GUI width to real width if Region mode
					if (captureMode == CaptureMode.Region) {
						captureRect.Width += 1;
						captureRect.Height += 1;
					}
					// Go and process the capture
					DialogResult = DialogResult.OK;
				} else {
					pictureBox.Invalidate();
				}
			}
		}
		
		private Point FixMouseCoordinates(Point currentMouse) {
			if (fixMode == FixMode.Initiated) {
				if (previousMousePos.X != currentMouse.X) {
					fixMode = FixMode.Vertical;
				} else if (previousMousePos.Y != currentMouse.Y) {
					fixMode = FixMode.Horizontal;
				}
			} else if (fixMode == FixMode.Vertical) {
				currentMouse = new Point(currentMouse.X, previousMousePos.Y);
			} else if (fixMode == FixMode.Horizontal) {
				currentMouse = new Point(previousMousePos.X, currentMouse.Y);
			}
			previousMousePos = currentMouse;
			return currentMouse;
		}

		void PictureBoxMouseMove(object sender, MouseEventArgs e) {
			Point lastPos = new Point(cursorPos.X, cursorPos.Y);
			cursorPos = WindowCapture.GetCursorLocation();
			// Make sure the mouse coordinates are fixed, when pressing shift
			cursorPos = FixMouseCoordinates(cursorPos);
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
			Point cursorPosition = Cursor.Position;
			selectedCaptureWindow = null;
			lock (windows) {
				foreach (WindowDetails window in windows) {
					if (window.Contains(cursorPosition)) {
						// Only go over the children if we are in window mode
						if (CaptureMode.Window == captureMode) {
							selectedCaptureWindow = window.FindChildUnderPoint(cursorPosition);
						} else {
							selectedCaptureWindow = window;
						}
						break;
					}
				}
			}
			if (selectedCaptureWindow != null && !selectedCaptureWindow.Equals(lastWindow)) {
				capture.CaptureDetails.Title = selectedCaptureWindow.Text;
				capture.CaptureDetails.AddMetaData("windowtitle", selectedCaptureWindow.Text);
				if (captureMode == CaptureMode.Window) {
					// Here we want to capture the window which is under the mouse
					captureRect = selectedCaptureWindow.WindowRectangle;
					// As the ClientRectangle is not in Bitmap coordinates, we need to correct.
					captureRect.Offset(-capture.ScreenBounds.Location.X, -capture.ScreenBounds.Location.Y);
				}
			}
			if (mouseDown && (CaptureMode.Window != captureMode)) {
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
					if (selectedCaptureWindow != null && !selectedCaptureWindow.Equals(lastWindow)) {
						// Using a 50 Pixel offset to the left, top, to make sure the text is invalidated too
						const int SAFETY_SIZE = 50;
						Rectangle invalidateRectangle = new Rectangle(lastCaptureRect.Location, lastCaptureRect.Size);
						invalidateRectangle.X -= SAFETY_SIZE/2;
						invalidateRectangle.Y -= SAFETY_SIZE/2;
						invalidateRectangle.Width += SAFETY_SIZE;
						invalidateRectangle.Height += SAFETY_SIZE;
						pictureBox.Invalidate(invalidateRectangle);
						invalidateRectangle = new Rectangle(captureRect.Location, captureRect.Size);
						invalidateRectangle.X -= SAFETY_SIZE/2;
						invalidateRectangle.Y -= SAFETY_SIZE/2;
						invalidateRectangle.Width += SAFETY_SIZE;
						invalidateRectangle.Height += SAFETY_SIZE;
						pictureBox.Invalidate(invalidateRectangle);
					}
				} else {
					if (!conf.OptimizeForRDP) {
						if (verticalMove) {
							Rectangle before = GuiRectangle.GetGuiRectangle(0, lastPos.Y - 2, this.Width+2, 45);
							Rectangle after = GuiRectangle.GetGuiRectangle(0, cursorPos.Y - 2, this.Width+2, 45);
							pictureBox.Invalidate(before);
							pictureBox.Invalidate(after);
						}
						if (horizontalMove) {
							Rectangle before = GuiRectangle.GetGuiRectangle(lastPos.X - 2, 0, 75, this.Height+2);
							Rectangle after = GuiRectangle.GetGuiRectangle(cursorPos.X -2, 0, 75, this.Height+2);
							pictureBox.Invalidate(before);
							pictureBox.Invalidate(after);
						}
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
				if (capture.CaptureDetails.CaptureMode == CaptureMode.Video) {
					graphics.FillRectangle( RedOverlayBrush, fixedRect );
				} else {
					graphics.FillRectangle( GreenOverlayBrush, fixedRect );
				}
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
				if (!conf.OptimizeForRDP) {
					using (Pen pen = new Pen(Color.LightSeaGreen)) {
						pen.DashStyle = DashStyle.Dot;
						Rectangle screenBounds = capture.ScreenBounds;
						graphics.DrawLine(pen, cursorPos.X, screenBounds.Y, cursorPos.X, screenBounds.Height);
						graphics.DrawLine(pen, screenBounds.X, cursorPos.Y, screenBounds.Width, cursorPos.Y);
					}

					string xy = cursorPos.X + " x " + cursorPos.Y;
					using (Font f = new Font(FontFamily.GenericSansSerif, 8)) {
						Size xySize = TextRenderer.MeasureText(xy, f);
						using (GraphicsPath gp = Drawing.RoundedRectangle.Create2(cursorPos.X + 5, cursorPos.Y + 5, xySize.Width - 3, xySize.Height, 3)) {
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
	}
}
