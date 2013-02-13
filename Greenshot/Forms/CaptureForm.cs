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
using GreenshotPlugin.Controls;
using System.Security.Permissions;

namespace Greenshot.Forms {
	/// <summary>
	/// The capture form is used to select a part of the capture
	/// </summary>
	public partial class CaptureForm : AnimatingForm {
		private enum FixMode {None, Initiated, Horizontal, Vertical};

		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(CaptureForm));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static Brush GreenOverlayBrush = new SolidBrush(Color.FromArgb(50, Color.MediumSeaGreen));
		private static Brush RedOverlayBrush = new SolidBrush(Color.FromArgb(50, Color.DarkRed));
		private static Pen OverlayPen = new Pen(Color.FromArgb(50, Color.Black));
		private static CaptureForm currentForm = null;
		private static Brush backgroundBrush = null;

		/// <summary>
		/// Initialize the background brush
		/// </summary>
		static CaptureForm() {
			Image backgroundForTransparency = GreenshotPlugin.Core.GreenshotResources.getImage("Checkerboard.Image");
			backgroundBrush = new TextureBrush(backgroundForTransparency, WrapMode.Tile);
		}

		private int mX;
		private int mY;
		private Point mouseMovePos = Point.Empty;
		private Point cursorPos = Point.Empty;
		private CaptureMode captureMode = CaptureMode.None;
		private List<WindowDetails> windows = new List<WindowDetails>();
		private WindowDetails selectedCaptureWindow;
		private bool mouseDown = false;
		private Rectangle captureRect = Rectangle.Empty;
		private ICapture capture = null;
		private Image capturedImage = null;
		private Point previousMousePos = Point.Empty;
		private FixMode fixMode = FixMode.None;
		private RectangleAnimator windowAnimator = null;
		private RectangleAnimator zoomAnimator = null;

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
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
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
		public CaptureForm(ICapture capture, List<WindowDetails> windows) : base() {
			if (currentForm != null) {
				LOG.Debug("Found currentForm, Closing already opened CaptureForm");
				currentForm.Close();
				currentForm = null;
				Application.DoEvents();
			}
			currentForm = this;

			// Enable the AnimatingForm
			EnableAnimation = true;

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
			this.DoubleBuffered = !isTerminalServerSession;
			this.Text = "Greenshot capture form";

			// Make sure we never capture the captureform
			WindowDetails.RegisterIgnoreHandle(this.Handle);
			// Unregister at close
			this.FormClosing += delegate {
				// remove the buffer if it was created inside this form
				if (capturedImage != capture.Image) {
					capturedImage.Dispose();
				}
				LOG.Debug("Closing captureform");
				WindowDetails.UnregisterIgnoreHandle(this.Handle);
			};

			// set cursor location
			cursorPos = WindowCapture.GetCursorLocationRelativeToScreenBounds();

			// Initialize the animations, the window capture zooms out from the cursor to the window under the cursor 
			if (captureMode == CaptureMode.Window) {
				windowAnimator = new RectangleAnimator(new Rectangle(cursorPos, Size.Empty), captureRect, FramesForMillis(700), EasingType.Quintic, EasingMode.EaseOut);
			}

			// Set the zoomer animation
			InitializeZoomer(conf.ZoomerEnabled);

			this.SuspendLayout();
			this.Bounds = capture.ScreenBounds;
			this.ResumeLayout();
			
			// Fix missing focus
			WindowDetails.ToForeground(this.Handle);
			this.TopMost = true;
		}
		
		/// <summary>
		/// Create an animation for the zoomer, depending on if it's active or not.
		/// </summary>
		void InitializeZoomer(bool isOn) {
			if (isOn) {
				// Initialize the zoom with a invalid position
				zoomAnimator = new RectangleAnimator(Rectangle.Empty, new Rectangle(int.MaxValue, int.MaxValue, 0, 0), FramesForMillis(1000), EasingType.Quintic, EasingMode.EaseOut);
				VerifyZoomAnimation(cursorPos, false);
			} else if (zoomAnimator != null) {
				zoomAnimator.ChangeDestination(new Rectangle(Point.Empty, Size.Empty), FramesForMillis(1000));
			}
		}

		#region key handling		
		void CaptureFormKeyUp(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.ShiftKey) {
				fixMode = FixMode.None;
			}
		}

		/// <summary>
		/// Handle the key down event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void CaptureFormKeyDown(object sender, KeyEventArgs e) {
			switch (e.KeyCode) {
				case Keys.Up:
					Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y - 1);
					break;
				case Keys.Down:
					Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y + 1);
					break;
				case Keys.Left:
					Cursor.Position = new Point(Cursor.Position.X - 1, Cursor.Position.Y);
					break;
				case Keys.Right:
					Cursor.Position = new Point(Cursor.Position.X + 1, Cursor.Position.Y);
					break;
				case Keys.ShiftKey:
					// Fixmode
					if (fixMode == FixMode.None) {
						fixMode = FixMode.Initiated;
						return;
					}
					break;
				case Keys.Escape:
					// Cancel
					DialogResult = DialogResult.Cancel;
					break;
				case Keys.M:
					// Toggle mouse cursor
					capture.CursorVisible = !capture.CursorVisible;
					Invalidate();
					break;
				//// TODO: Enable when the screen capture code works reliable
				//case Keys.V:
				//	// Video
				//	if (capture.CaptureDetails.CaptureMode != CaptureMode.Video) {
				//		capture.CaptureDetails.CaptureMode = CaptureMode.Video;
				//	} else {
				//		capture.CaptureDetails.CaptureMode = captureMode;
				//	}
				//	Invalidate();
				//	break;
				case Keys.Z:
					if (captureMode == CaptureMode.Region) {
						// Toggle zoom
						conf.ZoomerEnabled = !conf.ZoomerEnabled;
						InitializeZoomer(conf.ZoomerEnabled);
						Invalidate();
					}
					break;
				case Keys.Space:
					// Toggle capture mode
					switch (captureMode) {
						case CaptureMode.Region:
							// Set the window capture mode
							captureMode = CaptureMode.Window;
							// "Fade out" Zoom
							InitializeZoomer(false);
							// "Fade in" window
							windowAnimator = new RectangleAnimator(new Rectangle(cursorPos, Size.Empty), captureRect, FramesForMillis(700), EasingType.Quintic, EasingMode.EaseOut);
							captureRect = Rectangle.Empty;
							Invalidate();
							break;
						case CaptureMode.Window:
							// Set the region capture mode
							captureMode = CaptureMode.Region;
							// "Fade out" window
							windowAnimator.ChangeDestination(new Rectangle(cursorPos, Size.Empty), FramesForMillis(700));
							// Fade in zoom
							InitializeZoomer(conf.ZoomerEnabled);
							captureRect = Rectangle.Empty;
							Invalidate();
							break;
					}
					selectedCaptureWindow = null;
					OnMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0));
					break;
				case Keys.Return:
					// Confirm
					if (captureMode == CaptureMode.Window) {
						DialogResult = DialogResult.OK;
					}
					break;
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
				Point tmpCursorLocation = WindowCapture.GetCursorLocationRelativeToScreenBounds();
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
			mouseMovePos = WindowCapture.GetLocationRelativeToScreenBounds(mouseMovePos);
		}

		/// <summary>
		/// Helper method to simplify check
		/// </summary>
		/// <param name="animator"></param>
		/// <returns></returns>
		bool isAnimating(IAnimator animator) {
			if (animator == null) {
				return false;
			}
			return animator.hasNext;
		}

		/// <summary>
		/// update the frame, this only invalidates
		/// </summary>
		protected override void Animate() {
			Point lastPos = cursorPos.Clone();
			cursorPos = mouseMovePos.Clone();

			if (selectedCaptureWindow != null && lastPos.Equals(cursorPos) && !isAnimating(zoomAnimator) && !isAnimating(windowAnimator)) {
				return;
			}

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

			Rectangle invalidateRectangle = Rectangle.Empty;
			if (mouseDown && (captureMode != CaptureMode.Window)) {
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
				invalidateRectangle = new Rectangle(x1,y1, x2-x1, y2-y1);
				Invalidate(invalidateRectangle);
			} else if (captureMode != CaptureMode.Window) {
				if (!isTerminalServerSession) {
					Rectangle allScreenBounds = WindowCapture.GetScreenBounds();
					allScreenBounds.Location = WindowCapture.GetLocationRelativeToScreenBounds(allScreenBounds.Location);
					if (verticalMove) {
						// Before
						invalidateRectangle = GuiRectangle.GetGuiRectangle(allScreenBounds.Left, lastPos.Y - 2, this.Width + 2, 45);
						Invalidate(invalidateRectangle);
						// After
						invalidateRectangle = GuiRectangle.GetGuiRectangle(allScreenBounds.Left, cursorPos.Y - 2, this.Width + 2, 45);
						Invalidate(invalidateRectangle);
					}
					if (horizontalMove) {
						// Before
						invalidateRectangle = GuiRectangle.GetGuiRectangle(lastPos.X - 2, allScreenBounds.Top, 75, this.Height + 2);
						Invalidate(invalidateRectangle);
						// After
						invalidateRectangle = GuiRectangle.GetGuiRectangle(cursorPos.X - 2, allScreenBounds.Top, 75, this.Height + 2);
						Invalidate(invalidateRectangle);
					}
				}
			} else {
				if (selectedCaptureWindow != null && !selectedCaptureWindow.Equals(lastWindow)) {
					// Window changes, make new animation from current to target
					windowAnimator.ChangeDestination(captureRect, FramesForMillis(700));
				}
			}
			// always animate the Window area through to the last frame, so we see the fade-in/out untill the end
			// Using a safety "offset" to make sure the text is invalidated too
			const int SAFETY_SIZE = 30;
			// Check if the 
			if (isAnimating(windowAnimator)) {
				invalidateRectangle = windowAnimator.Current;
				invalidateRectangle.Inflate(SAFETY_SIZE, SAFETY_SIZE);
				Invalidate(invalidateRectangle);
				invalidateRectangle = windowAnimator.Next();
				invalidateRectangle.Inflate(SAFETY_SIZE, SAFETY_SIZE);
				Invalidate(invalidateRectangle);
				// Check if this was the last of the windows animations in the normal region capture.
				if (captureMode != CaptureMode.Window && !isAnimating(windowAnimator)) {
					Invalidate();
				}
			}

			if (zoomAnimator != null && (isAnimating(zoomAnimator) || captureMode != CaptureMode.Window)) {
				// Make sure we invalidate the old zoom area
				invalidateRectangle = zoomAnimator.Current;
				invalidateRectangle.Offset(lastPos);
				Invalidate(invalidateRectangle);
				// Only verify if we are really showing the zoom, not the outgoing animation
				if (conf.ZoomerEnabled && captureMode != CaptureMode.Window) {
					VerifyZoomAnimation(cursorPos, false);
				}
				// The following logic is not needed, next always returns the current if there are no frames left
				// but it makes more sense if we want to change something in the logic
				if (isAnimating(zoomAnimator)) {
					invalidateRectangle = zoomAnimator.Next();
				} else {
					invalidateRectangle = zoomAnimator.Current;
				}
				invalidateRectangle.Offset(cursorPos);
				Invalidate(invalidateRectangle);
			}
			// Force update "now"
			Update();
		}

		/// <summary>
		/// This makes sure there is no background painted, as we have complete "paint" control it doesn't make sense to do otherwise.
		/// </summary>
		/// <param name="pevent"></param>
		protected override void OnPaintBackground(PaintEventArgs pevent) {
		}

		/// <summary>
		/// Checks if the Zoom area can move there where it wants to go, change direction if not.
		/// </summary>
		/// <param name="pos">preferred destination location for the zoom area</param>
		/// <param name="allowZoomOverCaptureRect">false to try to find a location which is neither out of screen bounds nor intersects with the selected rectangle</param>
		private void VerifyZoomAnimation(Point pos, bool allowZoomOverCaptureRect) {
			Rectangle screenBounds = Screen.GetBounds(MousePosition);
			// convert to be relative to top left corner of all screen bounds
			screenBounds.Location = WindowCapture.GetLocationRelativeToScreenBounds(screenBounds.Location);
			int relativeZoomSize = Math.Min(screenBounds.Width, screenBounds.Height) / 5;
			// Make sure the final size is a plural of 4, this makes it look better
			relativeZoomSize = relativeZoomSize - (relativeZoomSize % 4);
			Size zoomSize = new Size(relativeZoomSize, relativeZoomSize);
			Point zoomOffset = new Point(20, 20);

			Rectangle targetRectangle = zoomAnimator.Final;
			targetRectangle.Offset(pos);
			if (!screenBounds.Contains(targetRectangle) || (!allowZoomOverCaptureRect && captureRect.IntersectsWith(targetRectangle))) {
				Point destinationLocation = Point.Empty;
				Rectangle tl = new Rectangle(pos.X - (zoomOffset.X + zoomSize.Width), pos.Y - (zoomOffset.Y + zoomSize.Height), zoomSize.Width, zoomSize.Height);
				Rectangle tr = new Rectangle(pos.X + zoomOffset.X, pos.Y - (zoomOffset.Y + zoomSize.Height), zoomSize.Width, zoomSize.Height);
				Rectangle bl = new Rectangle(pos.X - (zoomOffset.X + zoomSize.Width), pos.Y + zoomOffset.Y, zoomSize.Width, zoomSize.Height);
				Rectangle br = new Rectangle(pos.X + zoomOffset.X, pos.Y + zoomOffset.Y, zoomSize.Width, zoomSize.Height);
				if (screenBounds.Contains(br) && (allowZoomOverCaptureRect || !captureRect.IntersectsWith(br))) {
					destinationLocation = new Point(zoomOffset.X, zoomOffset.Y);
				} else if (screenBounds.Contains(bl) && (allowZoomOverCaptureRect || !captureRect.IntersectsWith(bl))) {
					destinationLocation = new Point(-zoomOffset.X - zoomSize.Width, zoomOffset.Y);
				} else if (screenBounds.Contains(tr) && (allowZoomOverCaptureRect || !captureRect.IntersectsWith(tr))) {
					destinationLocation = new Point(zoomOffset.X, -zoomOffset.Y - zoomSize.Width);
				} else if (screenBounds.Contains(tl) && (allowZoomOverCaptureRect || !captureRect.IntersectsWith(tl))) {
					destinationLocation = new Point(-zoomOffset.X - zoomSize.Width, -zoomOffset.Y - zoomSize.Width);
				} 
				if (destinationLocation == Point.Empty && !allowZoomOverCaptureRect) {
					VerifyZoomAnimation(pos, true);
				} else {
					zoomAnimator.ChangeDestination(new Rectangle(destinationLocation, zoomSize));
				}
			}
		}

		/// <summary>
		/// Draw the zoomed area
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="sourceRectangle"></param>
		/// <param name="destinationRectangle"></param>
		private void DrawZoom(Graphics graphics, Rectangle sourceRectangle, Rectangle destinationRectangle) {
			if (capturedImage == null) {
				return;
			}
			
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.CompositingQuality = CompositingQuality.HighSpeed;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			
			using (GraphicsPath path = new GraphicsPath()) {
				path.AddEllipse(destinationRectangle);
				graphics.SetClip(path);
				graphics.FillRectangle(backgroundBrush, destinationRectangle);
				graphics.DrawImage(capturedImage, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
			}

			// Draw the circle around the zoomer
			using (Pen pen = new Pen(Color.White, 2)) {
				graphics.DrawEllipse(pen, destinationRectangle);
			}

			// Make sure we don't have a pixeloffsetmode/smoothingmode when drawing the crosshair
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.PixelOffsetMode = PixelOffsetMode.None;

			// Calculate some values
			int pixelThickness = destinationRectangle.Width / sourceRectangle.Width;
			int halfWidth = destinationRectangle.Width / 2;
			int halfWidthEnd = (destinationRectangle.Width / 2) - (pixelThickness / 2);
			int halfHeight = destinationRectangle.Height / 2;
			int halfHeightEnd = (destinationRectangle.Height / 2) - (pixelThickness / 2);

			int drawAtHeight = destinationRectangle.Y + halfHeight;
			int drawAtWidth = destinationRectangle.X + halfWidth;
			int padding = pixelThickness;

			// Pen to draw
			using (Pen pen = new Pen(Color.Black, pixelThickness)) {
				// Draw the croshair-lines
				// Vertical top to middle
				graphics.DrawLine(pen, drawAtWidth, destinationRectangle.Y + padding, drawAtWidth, destinationRectangle.Y + halfHeightEnd - padding);
				// Vertical middle + 1 to bottom
				graphics.DrawLine(pen, drawAtWidth, destinationRectangle.Y + halfHeightEnd + 2 * padding, drawAtWidth, destinationRectangle.Y + destinationRectangle.Width - padding);
				// Horizontal left to middle
				graphics.DrawLine(pen, destinationRectangle.X + padding, drawAtHeight, destinationRectangle.X + halfWidthEnd - padding, drawAtHeight);
				// Horizontal middle + 1 to right
				graphics.DrawLine(pen, destinationRectangle.X + halfWidthEnd + 2 * padding, drawAtHeight, destinationRectangle.X + destinationRectangle.Width - padding, drawAtHeight);

				// Fix offset for drawing the white rectangle around the crosshair-lines
				drawAtHeight -= (pixelThickness / 2);
				drawAtWidth -= (pixelThickness / 2);
				// Fix off by one error with the DrawRectangle
				pixelThickness -= 1;
				// Change the color and the pen width
				pen.Color = Color.White;
				pen.Width = 1;
				// Vertical top to middle
				graphics.DrawRectangle(pen, drawAtWidth, destinationRectangle.Y + padding, pixelThickness, halfHeightEnd - 2 * padding - 1);
				// Vertical middle + 1 to bottom
				graphics.DrawRectangle(pen, drawAtWidth, destinationRectangle.Y + halfHeightEnd + 2 * padding, pixelThickness, halfHeightEnd - 2 * padding - 1);
				// Horizontal left to middle
				graphics.DrawRectangle(pen, destinationRectangle.X + padding, drawAtHeight, halfWidthEnd - 2 * padding - 1, pixelThickness);
				// Horizontal middle + 1 to right
				graphics.DrawRectangle(pen, destinationRectangle.X + halfWidthEnd + 2 * padding, drawAtHeight, halfWidthEnd - 2 * padding - 1, pixelThickness);
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

			if (mouseDown || captureMode == CaptureMode.Window || isAnimating(windowAnimator)) {
				captureRect.Intersect(new Rectangle(Point.Empty, capture.ScreenBounds.Size)); // crop what is outside the screen
				
				Rectangle fixedRect;
				//if (captureMode == CaptureMode.Window) {
				if (isAnimating(windowAnimator)) {
					// Use the animator
					fixedRect = windowAnimator.Current;
				} else {
					fixedRect = captureRect;
				}

				// TODO: enable when the screen capture code works reliable
				//if (capture.CaptureDetails.CaptureMode == CaptureMode.Video) {
				//	graphics.FillRectangle(RedOverlayBrush, fixedRect);
				//} else {
					graphics.FillRectangle(GreenOverlayBrush, fixedRect);
				//}
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
				if (!isTerminalServerSession) {
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

			// Zoom
			if (zoomAnimator != null && (isAnimating(zoomAnimator) || captureMode != CaptureMode.Window)) {
				const int zoomSourceWidth = 25;
				const int zoomSourceHeight = 25;
				
				Rectangle sourceRectangle = new Rectangle(cursorPos.X - (zoomSourceWidth / 2), cursorPos.Y - (zoomSourceHeight / 2), zoomSourceWidth, zoomSourceHeight);
				
				Rectangle destinationRectangle = zoomAnimator.Current;
				destinationRectangle.Offset(cursorPos);
				DrawZoom(graphics, sourceRectangle, destinationRectangle);
			}
		}
		#endregion
	}
}
