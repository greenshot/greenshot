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
using System.Drawing.Imaging;
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
	/// The capture form is used to select a part of the capture
	/// </summary>
	public partial class CaptureForm : Form {
		private enum FixMode {None, Initiated, Horizontal, Vertical};

		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(CaptureForm));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static Brush GreenOverlayBrush = new SolidBrush(Color.FromArgb(50, Color.MediumSeaGreen));
		private static Brush RedOverlayBrush = new SolidBrush(Color.FromArgb(50, Color.DarkRed));
		private static Pen OverlayPen = new Pen(Color.FromArgb(50, Color.Black));
		private static CaptureForm currentForm = null;
		private static Brush backgroundBrush = null;

		static CaptureForm() {
			Image backgroundForTransparency = GreenshotPlugin.Core.GreenshotResources.getImage("Checkerboard.Image");
			backgroundBrush = new TextureBrush(backgroundForTransparency, WrapMode.Tile);
		}

		private int mX;
		private int mY;
		private Point mouseMovePos = Point.Empty;
		private Point cursorPos = Point.Empty;
		private Point cursorPosOnBitmap = Point.Empty;
		private CaptureMode captureMode = CaptureMode.None;
		private List<WindowDetails> windows = new List<WindowDetails>();
		private WindowDetails selectedCaptureWindow;
		private bool mouseDown = false;
		private Rectangle captureRect = Rectangle.Empty;
		private ICapture capture = null;
		private Image capturedImage = null;
		private Timer timer = null;
		private bool isZooming = true;
		private Point previousMousePos = Point.Empty;
		private FixMode fixMode = FixMode.None;
		private RectangleAnimator windowAnimator = new RectangleAnimator(Rectangle.Empty, Rectangle.Empty, 0);
		private SizeAnimator zoomAnimator;

		/// <summary>
		/// Property to access the selected capture rectangle
		/// </summary>
		public Rectangle CaptureRectangle {
			get {
				return captureRect;
			}
		}

		/// <summary>
		/// Property to access the used capture mode
		/// </summary>
		public CaptureMode UsedCaptureMode {
			get {
				return captureMode;
			}
		}

		/// <summary>
		/// Get the selected window
		/// </summary>
		public WindowDetails SelectedCaptureWindow {
			get {
				return selectedCaptureWindow;
			}
		}

		/// <summary>
		/// This should prevent childs to draw backgrounds
		/// </summary>
		protected override CreateParams CreateParams {
			get {
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000;
				return cp;
			}
		}

		/// <summary>
		/// This creates the capture form
		/// </summary>
		/// <param name="capture"></param>
		/// <param name="windows"></param>
		public CaptureForm(ICapture capture, List<WindowDetails> windows) {
			if (currentForm != null) {
				LOG.Debug("Found currentForm, Closing already opened CaptureForm");
				currentForm.Close();
				currentForm = null;
				Application.DoEvents();
			}
			currentForm = this;
			
			// comment this out if the timer should not be used
			timer = new Timer();
			
			// Using 32bppPArgb speeds up the drawing.
			//capturedImage = ImageHelper.Clone(capture.Image, PixelFormat.Format32bppPArgb);
			// comment the clone, uncomment the assignment and the original bitmap is used.
			capturedImage = capture.Image;

			// clean up
			this.FormClosed += delegate {
				currentForm = null;
				LOG.Debug("Remove CaptureForm from currentForm");
			};

			this.capture = capture;
			this.windows = windows;
			this.captureMode = capture.CaptureDetails.CaptureMode;

			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			// Only double-buffer when we are not in a TerminalServerSession
			this.DoubleBuffered = !System.Windows.Forms.SystemInformation.TerminalServerSession;
			this.Text = "Greenshot capture form";

			// Make sure we never capture the captureform
			WindowDetails.RegisterIgnoreHandle(this.Handle);
			// Unregister at close
			this.FormClosing += delegate {
				if (timer != null) {
					timer.Stop();
				}
				// remove the buffer if it was created inside this form
				if (capturedImage != capture.Image) {
					capturedImage.Dispose();
				}
				LOG.Debug("Closing captureform");
				WindowDetails.UnregisterIgnoreHandle(this.Handle);
			};

			// set cursor location
			cursorPos = WindowCapture.GetCursorLocation();
			cursorPosOnBitmap = GetAbsoluteLocation(cursorPos);

			this.SuspendLayout();
			this.Bounds = capture.ScreenBounds;
			this.ResumeLayout();
			
			// Fix missing focus
			WindowDetails.ToForeground(this.Handle);
			this.TopMost = true;
			
			zoomAnimator = new SizeAnimator(Size.Empty, new Size(200, 200), 10);
			if (timer != null) {
				timer.Interval = 30;
				timer.Tick += new EventHandler(timer_Tick);
				timer.Start();
			}
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
				Invalidate();
			} else if (e.KeyCode == Keys.V) {
				if (capture.CaptureDetails.CaptureMode != CaptureMode.Video) {
					capture.CaptureDetails.CaptureMode = CaptureMode.Video;
				} else {
					capture.CaptureDetails.CaptureMode = captureMode;
				}
				Invalidate();
			} else if (e.KeyCode == Keys.Z) {
				// Toggle zoom
				isZooming = !isZooming;
			} else if (e.KeyCode == Keys.Space) {
				switch (captureMode) {
					case CaptureMode.Region:
						captureMode = CaptureMode.Window;
						break;
					case CaptureMode.Window:
						captureMode = CaptureMode.Region;
						break;
				}
				Invalidate();
				selectedCaptureWindow = null;
				OnMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0));
			} else if (e.KeyCode == Keys.Return && captureMode == CaptureMode.Window) {
				DialogResult = DialogResult.OK;
			}
		}
		#endregion

		#region events
		/// <summary>
		/// The mousedown handler of the capture form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnMouseDown(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				Point tmpCursorLocation = WindowCapture.GetCursorLocation();
				// As the cursorPos is not in Bitmap coordinates, we need to correct.
				tmpCursorLocation = GetAbsoluteLocation(tmpCursorLocation);
				mX = tmpCursorLocation.X;
				mY = tmpCursorLocation.Y;
				mouseDown = true;
				OnMouseMove(this, e);
				Invalidate();
			}
		}
		
		/// <summary>
		/// The mouse up handler of the capture form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnMouseUp(object sender, MouseEventArgs e) {
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
					Invalidate();
				}
			}
		}
		
		/// <summary>
		/// This method is used to "fix" the mouse coordinates when keeping shift/ctrl pressed
		/// </summary>
		/// <param name="currentMouse"></param>
		/// <returns></returns>
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

		/// <summary>
		/// The mouse move handler of the capture form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnMouseMove(object sender, MouseEventArgs e) {
			// Make sure the mouse coordinates are fixed, when pressing shift
			mouseMovePos = FixMouseCoordinates(WindowCapture.GetCursorLocation());
			// If the timer is used, the timer_Tick does the following.
			// If the timer is not used, we need to call the update ourselves
			if (timer == null) {
				updateFrame();
			}
		}

		/// <summary>
		/// The tick handler of the capture form, this initiates the frame drawing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void timer_Tick(object sender, EventArgs e) {
			updateFrame();
		}

		/// <summary>
		/// update the frame, this only invalidates
		/// </summary>
		void updateFrame() {
			Point lastPos = cursorPos.Clone();
			cursorPos = mouseMovePos.Clone();
			if (selectedCaptureWindow != null && lastPos.Equals(cursorPos) && !zoomAnimator.hasNext && !windowAnimator.hasNext) {
				return;
			}

			// As the cursorPos is not in Bitmap coordinates, we need to correct.
			cursorPosOnBitmap = GetAbsoluteLocation(cursorPos);
			Point lastPosOnBitmap = GetAbsoluteLocation(lastPos);

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
				captureRect = GuiRectangle.GetGuiRectangle(cursorPosOnBitmap.X, cursorPosOnBitmap.Y, mX - cursorPosOnBitmap.X, mY - cursorPosOnBitmap.Y);
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
				int x1 = Math.Min(mX, lastPosOnBitmap.X);
				int x2 = Math.Max(mX, lastPosOnBitmap.X);
				int y1 = Math.Min(mY, lastPosOnBitmap.Y);
				int y2 = Math.Max(mY, lastPosOnBitmap.Y);
				x1= Math.Min(x1, cursorPosOnBitmap.X);
				x2= Math.Max(x2, cursorPosOnBitmap.X);
				y1= Math.Min(y1, cursorPosOnBitmap.Y);
				y2= Math.Max(y2, cursorPosOnBitmap.Y);

				// Safety correction
				x2 += 2;
				y2 += 2;

				// Here we correct for text-size
				
				// Calculate the size
				int textForWidth = Math.Max(Math.Abs(mX - cursorPosOnBitmap.X), Math.Abs(mX - lastPosOnBitmap.X));
				int textForHeight = Math.Max(Math.Abs(mY - cursorPosOnBitmap.Y), Math.Abs(mY - lastPosOnBitmap.Y));

				using (Font rulerFont = new Font(FontFamily.GenericSansSerif, 8)) {
					Size measureWidth = TextRenderer.MeasureText(textForWidth.ToString(), rulerFont);
					x1 -= measureWidth.Width + 15;

					Size measureHeight = TextRenderer.MeasureText(textForHeight.ToString(), rulerFont);
					y1 -= measureWidth.Height + 10;
				}
				Rectangle invalidateRectangle = new Rectangle(x1,y1, x2-x1, y2-y1);
				Invalidate(invalidateRectangle);
			} else {
				if (captureMode == CaptureMode.Window) {
					// Using a 50 Pixel offset to the left, top, to make sure the text is invalidated too
					const int SAFETY_SIZE = 25;
					if (windowAnimator.hasNext) {
						Rectangle invalidateRectangle = windowAnimator.Current;
						invalidateRectangle.Inflate(SAFETY_SIZE, SAFETY_SIZE);
						Invalidate(invalidateRectangle);
						invalidateRectangle = windowAnimator.Next();
						invalidateRectangle.Inflate(SAFETY_SIZE, SAFETY_SIZE);
						Invalidate(invalidateRectangle);
					}
					if (selectedCaptureWindow != null && !selectedCaptureWindow.Equals(lastWindow)) {
						windowAnimator = new RectangleAnimator(lastCaptureRect,captureRect, 5);
						Rectangle invalidateRectangle = new Rectangle(lastCaptureRect.Location, lastCaptureRect.Size);
						invalidateRectangle.Inflate(SAFETY_SIZE, SAFETY_SIZE);
						Invalidate(invalidateRectangle);
						invalidateRectangle = new Rectangle(captureRect.Location, captureRect.Size);
						invalidateRectangle.Inflate(SAFETY_SIZE, SAFETY_SIZE);
						Invalidate(invalidateRectangle);
					}
				} else {
					if (!conf.OptimizeForRDP) {
						if (verticalMove) {
							Rectangle before = GuiRectangle.GetGuiRectangle(WindowCapture.GetScreenBounds().Left, lastPos.Y - 2, this.Width+2, 45);
							Rectangle after = GuiRectangle.GetGuiRectangle(WindowCapture.GetScreenBounds().Left, cursorPos.Y - 2, this.Width+2, 45);
							Invalidate(before);
							Invalidate(after);
						}
						if (horizontalMove) {
							Rectangle before = GuiRectangle.GetGuiRectangle(lastPos.X - 2, WindowCapture.GetScreenBounds().Top, 75, this.Height+2);
							Rectangle after = GuiRectangle.GetGuiRectangle(cursorPos.X -2, WindowCapture.GetScreenBounds().Top, 75, this.Height+2);
							Invalidate(before);
							Invalidate(after);
						}
					}
				}
			}
			if (isZooming && captureMode != CaptureMode.Window) {
				Invalidate(ZoomArea(lastPos, zoomAnimator.Current));
				Invalidate(ZoomArea(cursorPos, zoomAnimator.Next()));

				// TODO: Move this to the Animator, but we need to check how to make sure we have an exact result.
				//if (zoomSize.Width < 200) {
				//	zoomSize.Width += (220-zoomSize.Width)/5;
				//	zoomSize.Height += (220-zoomSize.Height)/5;
				//}
			}
			// Force update "now"
			Update();
		}
		
		/// <summary>
		/// Get the absolute location for the supplied screen location
		/// </summary>
		/// <param name="screenLocation"></param>
		/// <returns>Absolute location</returns>
		private Point GetAbsoluteLocation(Point screenLocation) {
			Point ret = screenLocation.Clone();
			ret.Offset(-capture.ScreenBounds.X, -capture.ScreenBounds.Y);
			return ret;
		}

		/// <summary>
		/// This makes sure there is no background painted, as we have complete "paint" control it doesn't make sense to do otherwise.
		/// </summary>
		/// <param name="pevent"></param>
		protected override void OnPaintBackground(PaintEventArgs pevent) {
		}

		/// <summary>
		/// Get the area of where the zoom can be drawn
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		private Rectangle ZoomArea(Point pos, Size size) {
			Rectangle ret;
			const int distanceX = 20;
			const int distanceY = 20;
			Rectangle tl = new Rectangle(pos.X - (distanceX + size.Width), pos.Y - (distanceY + size.Height), size.Width, size.Height);
			Rectangle tr = new Rectangle(pos.X + distanceX, pos.Y - (distanceY + size.Height), size.Width, size.Height);
			Rectangle bl = new Rectangle(pos.X - (distanceX + size.Width), pos.Y + distanceY, size.Width, size.Height);
			Rectangle br = new Rectangle(pos.X + distanceX, pos.Y + distanceY, size.Width, size.Height);
			Rectangle screenBounds = Screen.GetBounds(pos);
			if (screenBounds.Contains(br)) {
				ret = br;
			} else if (screenBounds.Contains(bl)) {
				ret = bl;
			} else if (screenBounds.Contains(tr)) {
				ret = tr;
			} else {
				ret = tl;
			}
			ret.Offset(-capture.ScreenBounds.Location.X, -capture.ScreenBounds.Location.Y);
			return ret;
		}

		/// <summary>
		/// Draw the zoomed area
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="sourceRectangle"></param>
		/// <param name="destinationRectangle"></param>
		private void DrawZoom(Graphics graphics, Rectangle sourceRectangle, Rectangle destinationRectangle) {
			if (capturedImage == null || !isZooming) {
				return;
			}
			
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.CompositingQuality = CompositingQuality.HighSpeed;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			
			using (GraphicsPath path = new GraphicsPath()) {
				path.AddEllipse(destinationRectangle);
				using (Region clipRegion = new Region(path)) {
					graphics.Clip = clipRegion;
					graphics.FillRectangle(backgroundBrush, destinationRectangle);
					graphics.DrawImage(capturedImage, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
				}
			}

			int pixelThickness = destinationRectangle.Width / sourceRectangle.Width;
			using (Pen pen = new Pen(Color.White, 1)) {
				using (Brush brush = new SolidBrush(Color.Black)) {
					int halfWidth = destinationRectangle.Width / 2;
					int halfWidthEnd = (destinationRectangle.Width / 2) - (pixelThickness / 2);
					int halfHeight = destinationRectangle.Height / 2;
					int halfHeightEnd = (destinationRectangle.Height / 2) - (pixelThickness / 2);

					int drawAtHeight = destinationRectangle.Y + halfHeight - (pixelThickness / 2);
					int drawAtWidth = destinationRectangle.X + halfWidth - (pixelThickness / 2);
					int padding = pixelThickness;

					// Vertical top to middle
					graphics.FillRectangle(brush, drawAtWidth, destinationRectangle.Y + padding, pixelThickness, halfHeightEnd - 2 * padding);
					graphics.DrawRectangle(pen, drawAtWidth, destinationRectangle.Y + padding, pixelThickness, halfHeightEnd - 2 * padding);
					// Vertical middle + 1 to bottom
					graphics.FillRectangle(brush, drawAtWidth, destinationRectangle.Y + halfHeightEnd + pixelThickness + padding, pixelThickness, halfHeightEnd - 2 * padding);
					graphics.DrawRectangle(pen, drawAtWidth, destinationRectangle.Y + halfHeightEnd + pixelThickness + padding, pixelThickness, halfHeightEnd - 2 * padding);
					// Horizontal left to middle
					graphics.FillRectangle(brush, destinationRectangle.X + padding, drawAtHeight, halfWidthEnd - 2 * padding, pixelThickness);
					graphics.DrawRectangle(pen, destinationRectangle.X + padding, drawAtHeight, halfWidthEnd - 2 * padding, pixelThickness);
					// Horizontal middle + 1 to right
					graphics.FillRectangle(brush, destinationRectangle.X + halfWidthEnd + pixelThickness + padding, drawAtHeight, halfWidthEnd - 2 * padding, pixelThickness);
					graphics.DrawRectangle(pen, destinationRectangle.X + halfWidthEnd + pixelThickness + padding, drawAtHeight, halfWidthEnd - 2 * padding, pixelThickness);

					pen.Width = 2;
					graphics.DrawEllipse(pen, destinationRectangle);
				}
			}
		}

		/// <summary>
		/// Paint the actual visible parts
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnPaint(object sender, PaintEventArgs e) {
			Graphics graphics = e.Graphics;
			Rectangle clipRectangle = e.ClipRectangle;
			//graphics.BitBlt((Bitmap)buffer, Point.Empty);
			graphics.DrawImageUnscaled(capturedImage, Point.Empty);
			// Only draw Cursor if it's (partly) visible
			if (capture.Cursor != null && capture.CursorVisible && clipRectangle.IntersectsWith(new Rectangle(capture.CursorLocation, capture.Cursor.Size))) {
				graphics.DrawIcon(capture.Cursor, capture.CursorLocation.X, capture.CursorLocation.Y);
			}

			if (mouseDown || captureMode == CaptureMode.Window) {
				captureRect.Intersect(new Rectangle(Point.Empty, capture.ScreenBounds.Size)); // crop what is outside the screen
				
				Rectangle fixedRect;
				if (captureMode == CaptureMode.Window) {
					// Use the animator
					fixedRect = windowAnimator.Current;
				} else {
					fixedRect = captureRect;					
				}

				if (capture.CaptureDetails.CaptureMode == CaptureMode.Video) {
					graphics.FillRectangle(RedOverlayBrush, fixedRect);
				} else {
					graphics.FillRectangle(GreenOverlayBrush, fixedRect);
				}
				graphics.DrawRectangle(OverlayPen, fixedRect);
				
				// rulers
				int dist = 8;
				
				string captureWidth;
				string captureHeight;
				// The following fixes the very old incorrect size information bug
				if (captureMode == CaptureMode.Window) {
					captureWidth = captureRect.Width.ToString();
					captureHeight = captureRect.Height.ToString();
				} else {
					captureWidth = (captureRect.Width + 1).ToString();
					captureHeight = (captureRect.Height + 1).ToString();
				}
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
						graphics.DrawLine(pen, cursorPosOnBitmap.X, screenBounds.Y, cursorPosOnBitmap.X, screenBounds.Height);
						graphics.DrawLine(pen, screenBounds.X, cursorPosOnBitmap.Y, screenBounds.Width, cursorPosOnBitmap.Y);
					}

					string xy = cursorPosOnBitmap.X + " x " + cursorPosOnBitmap.Y;
					using (Font f = new Font(FontFamily.GenericSansSerif, 8)) {
						Size xySize = TextRenderer.MeasureText(xy, f);
						using (GraphicsPath gp = Drawing.RoundedRectangle.Create2(cursorPosOnBitmap.X + 5, cursorPosOnBitmap.Y + 5, xySize.Width - 3, xySize.Height, 3)) {
							using (Brush bgBrush = new SolidBrush(Color.FromArgb(200, 217, 240, 227))) {
								graphics.FillPath(bgBrush, gp);
							}
							using (Pen pen = new Pen(Color.SeaGreen)) {
								graphics.DrawPath(pen, gp);
								Point coordinatePosition = new Point(cursorPosOnBitmap.X + 5, cursorPosOnBitmap.Y + 5);
								graphics.DrawString(xy, f, pen.Brush, coordinatePosition);
							}
						}
					}
				}
			}

			if (captureMode != CaptureMode.Window) {
				const int zoomSourceWidth = 25;
				const int zoomSourceHeight = 25;
				
				Rectangle sourceRectangle = new Rectangle(cursorPosOnBitmap.X - (zoomSourceWidth / 2), cursorPosOnBitmap.Y - (zoomSourceHeight / 2), zoomSourceWidth, zoomSourceHeight);
				DrawZoom(graphics, sourceRectangle, ZoomArea(cursorPos, zoomAnimator.Current));

			}
		}
		#endregion
	}
}
