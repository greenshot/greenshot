//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Native;
using Greenshot.Core;
using Greenshot.Core.Configuration;
using Greenshot.Core.Enumerations;
using Greenshot.Core.Extensions;
using Greenshot.Core.Interfaces;
using Greenshot.Legacy;
using Greenshot.Legacy.Controls;

#endregion

namespace Greenshot.CaptureCore.Forms
{
	/// <summary>
	///     The capture form is used to select a part of the capture
	/// </summary>
	public sealed partial class CropForm : AnimatingForm
	{
		private static readonly LogSource Log = new LogSource();
		private static CropForm _currentForm;

		private readonly Brush _areaOverlayBrush;
		private readonly Pen _areaOverlayPen;
		private readonly Brush _backgroundBrush;
		private readonly ICapture _capture;

		private readonly ICropConfiguration _cropConfiguration;
		private readonly Task<IList<WindowDetails>> _retrieveWindowsTask;
		private Rectangle _captureRect = Rectangle.Empty;
		private Point _cursorPos;
		private FixMode _fixMode = FixMode.None;
		private bool _isCtrlPressed;
		private bool _mouseDown;
		private Point _mouseMovePos;
		private int _mX;
		private int _mY;
		private Point _previousMousePos = Point.Empty;
		private RectangleAnimator _windowAnimator;
		private IList<WindowDetails> _windows;
		private RectangleAnimator _zoomAnimator;

		/// <summary>
		///     This creates the capture form
		/// </summary>
		/// <param name="capture"></param>
		/// <param name="retrieveWindowsTask"></param>
		/// <param name="cropConfiguration">ICropConfiguration with the configuration for the cropper</param>
		public CropForm(ICapture capture, Task<IList<WindowDetails>> retrieveWindowsTask, ICropConfiguration cropConfiguration)
		{
			_cropConfiguration = cropConfiguration;
			Image backgroundForTransparency = GreenshotResources.GetImage("Checkerboard.Image");
			_backgroundBrush = new TextureBrush(backgroundForTransparency, WrapMode.Tile);
			_areaOverlayBrush = new SolidBrush(cropConfiguration.CropAreaColor);
			_areaOverlayPen = new Pen(Color.FromArgb(50, Color.Black));

			if (_currentForm != null)
			{
				Log.Warn().WriteLine("Found currentForm, Closing already opened CaptureForm");
				_currentForm.Close();
				_currentForm = null;
			}
			_currentForm = this;

			// Assign windows retrieval outside UI
			if (retrieveWindowsTask != null)
			{
				Task.Run(async () =>
				{
					_windows = await _retrieveWindowsTask.ConfigureAwait(true);
					Log.Info().WriteLine("Found {0} windows", _windows.Count);
				});
			}

			// Enable the AnimatingForm
			EnableAnimation = true;

			// clean up
			FormClosed += ClosedHandler;

			_capture = capture;
			_retrieveWindowsTask = retrieveWindowsTask;
			UsedCaptureMode = capture.CaptureDetails.CaptureMode;

			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			// Only double-buffer when we are not in a TerminalServerSession
			DoubleBuffered = !IsTerminalServerSession;
			Text = "Greenshot capture form";

			// Set the cursor positions, so the zoomer doesn't arrive at a weird place
			_cursorPos = _mouseMovePos = ScreenHelper.GetCursorLocationRelativeToScreenBounds();

			// Make sure we never capture the captureform
			WindowDetails.RegisterIgnoreHandle(Handle);
			// Unregister at close
			FormClosing += ClosingHandler;

			// Initialize the animations, the window capture zooms out from the cursor to the window under the cursor 
			if (UsedCaptureMode == CaptureModes.Window)
			{
				_windowAnimator = new RectangleAnimator(new Rectangle(_cursorPos, Size.Empty), _captureRect, FramesForMillis(700), EasingType.Quintic, EasingMode.EaseOut);
			}

			SuspendLayout();
			Bounds = capture.ScreenBounds;
			ResumeLayout();

			// Fix missing focus
			ToFront = true;
			TopMost = true;
			// Set the zoomer animation
			InitializeZoomer(_cropConfiguration.ZoomerEnabled);
		}

		/// <summary>
		///     Property to access the selected capture rectangle
		/// </summary>
		public Rectangle CaptureRectangle => _captureRect;

		/// <summary>
		///     Get the selected window
		/// </summary>
		public WindowDetails SelectedCaptureWindow { get; private set; }

		/// <summary>
		///     Property to access the used capture mode
		/// </summary>
		public CaptureModes UsedCaptureMode { get; private set; }

		/// <summary>
		///     This should prevent childs to draw backgrounds
		/// </summary>
		protected override CreateParams CreateParams
		{
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			get
			{
				var createParams = base.CreateParams;
				createParams.ExStyle |= 0x02000000;
				return createParams;
			}
		}

		private void ClosedHandler(object sender, EventArgs e)
		{
			_currentForm = null;
			Log.Debug().WriteLine("Remove CaptureForm from currentForm");
			if (SelectedCaptureWindow == null)
			{
				return;
			}
			Log.Debug().WriteLine("Selected window: {0}", SelectedCaptureWindow);
			_capture.CaptureDetails.Title = SelectedCaptureWindow.Text;
			_capture.CaptureDetails.AddMetaData("windowtitle", SelectedCaptureWindow.Text);
			if (UsedCaptureMode != CaptureModes.Window)
			{
				return;
			}
			// Here we want to capture the window which is under the mouse
			_captureRect = SelectedCaptureWindow.WindowRectangle;
			// As the ClientRectangle is not in Bitmap coordinates, we need to correct.
			_captureRect.Offset(-_capture.ScreenBounds.Location.X, -_capture.ScreenBounds.Location.Y);
		}

		private void ClosingHandler(object sender, EventArgs e)
		{
			Log.Debug().WriteLine("Closing captureform");
			WindowDetails.UnregisterIgnoreHandle(Handle);
		}

		/// <summary>
		///     Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				components?.Dispose();
				_areaOverlayBrush?.Dispose();
				_areaOverlayPen?.Dispose();
				_backgroundBrush?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		///     Create an animation for the zoomer, depending on if it's active or not.
		/// </summary>
		private void InitializeZoomer(bool isOn)
		{
			if (isOn)
			{
				// Initialize the zoom with a invalid position
				_zoomAnimator = new RectangleAnimator(Rectangle.Empty, new Rectangle(int.MaxValue, int.MaxValue, 0, 0), FramesForMillis(1000), EasingType.Quintic, EasingMode.EaseOut);
				VerifyZoomAnimation(_cursorPos, false);
			}
			else
			{
				_zoomAnimator?.ChangeDestination(new Rectangle(Point.Empty, Size.Empty), FramesForMillis(1000));
			}
		}

		private enum FixMode
		{
			None,
			Initiated,
			Horizontal,
			Vertical
		}

		#region key handling		

		private void CaptureFormKeyUp(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.ShiftKey:
					_fixMode = FixMode.None;
					break;
				case Keys.ControlKey:
					_isCtrlPressed = false;
					break;
			}
		}

		/// <summary>
		///     Handle the key down event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CaptureFormKeyDown(object sender, KeyEventArgs e)
		{
			int step = _isCtrlPressed ? 10 : 1;

			switch (e.KeyCode)
			{
				case Keys.Up:
					Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y - step);
					break;
				case Keys.Down:
					Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y + step);
					break;
				case Keys.Left:
					Cursor.Position = new Point(Cursor.Position.X - step, Cursor.Position.Y);
					break;
				case Keys.Right:
					Cursor.Position = new Point(Cursor.Position.X + step, Cursor.Position.Y);
					break;
				case Keys.ShiftKey:
					// Fixmode
					if (_fixMode == FixMode.None)
					{
						_fixMode = FixMode.Initiated;
					}
					break;
				case Keys.ControlKey:
					_isCtrlPressed = true;
					break;
				case Keys.Escape:
					// Cancel
					DialogResult = DialogResult.Cancel;
					Close();
					break;
				case Keys.M:
					// Toggle mouse cursor
					_capture.CursorVisible = !_capture.CursorVisible;
					Invalidate();
					break;
				//// TODO: Enable when the screen capture code works reliable
				//case Keys.V:
				//	// Video
				//	if (capture.CaptureDetails.CaptureModes != CaptureModes.Video) {
				//		capture.CaptureDetails.CaptureModes = CaptureModes.Video;
				//	} else {
				//		capture.CaptureDetails.CaptureModes = captureMode;
				//	}
				//	Invalidate();
				//	break;
				case Keys.Z:
					if (UsedCaptureMode == CaptureModes.Region)
					{
						// Toggle zoom
						_cropConfiguration.ZoomerEnabled = !_cropConfiguration.ZoomerEnabled;
						InitializeZoomer(_cropConfiguration.ZoomerEnabled);
						Invalidate();
					}
					break;
				case Keys.Space:
					// Toggle capture mode
					  switch (UsedCaptureMode) {
						case CaptureModes.Region:
							// Set the window capture mode
							UsedCaptureMode = CaptureModes.Window;
							// "Fade out" Zoom
							InitializeZoomer(false);
							// "Fade in" window
							_windowAnimator = new RectangleAnimator(new Rectangle(_cursorPos, Size.Empty), _captureRect, FramesForMillis(700), EasingType.Quintic, EasingMode.EaseOut);
							_captureRect = Rectangle.Empty;
							Invalidate();
							break;
						case CaptureModes.Window:
							// Set the region capture mode
							UsedCaptureMode = CaptureModes.Region;
							// "Fade out" window
							_windowAnimator.ChangeDestination(new Rectangle(_cursorPos, Size.Empty), FramesForMillis(700));
							// Fade in zoom
							InitializeZoomer(_cropConfiguration.ZoomerEnabled);
							_captureRect = Rectangle.Empty;
							Invalidate();
							break;
					}
					SelectedCaptureWindow = null;
					OnMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0));
					break;
				case Keys.Return:
					// Confirm
					if (UsedCaptureMode == CaptureModes.Window)
					{
						DialogResult = DialogResult.OK;
						Close();
					}
					else if (!_mouseDown)
					{
						HandleMouseDown();
					}
					else if (_mouseDown)
					{
						HandleMouseUp();
					}
					break;
			}
		}

		#endregion

		#region events

		/// <summary>
		///     The mousedown handler of the capture form
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="e">MouseEventArgs</param>
		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				HandleMouseDown();
			}
		}

		private void HandleMouseDown()
		{
			Point tmpCursorLocation = ScreenHelper.GetCursorLocationRelativeToScreenBounds();
			_mX = tmpCursorLocation.X;
			_mY = tmpCursorLocation.Y;
			_mouseDown = true;
			OnMouseMove(this, null);
			Invalidate();
		}

		private void HandleMouseUp()
		{
			// If the mouse goes up we set down to false (nice logic!)
			_mouseDown = false;
			// Check if anything is selected
			if ((UsedCaptureMode == CaptureModes.Window) && (SelectedCaptureWindow != null))
			{
				// Go and process the capture
				DialogResult = DialogResult.OK;
				Close();
			}
			else if ((_captureRect.Height > 0) && (_captureRect.Width > 0))
			{
				// correct the GUI width to real width if Region mode
				if (UsedCaptureMode == CaptureModes.Region)
				{
					_captureRect.Width += 1;
					_captureRect.Height += 1;
				}
				// Go and process the capture
				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				Invalidate();
			}
		}

		/// <summary>
		///     The mouse up handler of the capture form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (_mouseDown)
			{
				HandleMouseUp();
			}
		}

		/// <summary>
		///     This method is used to "fix" the mouse coordinates when keeping shift/ctrl pressed
		/// </summary>
		/// <param name="currentMousePoint"></param>
		/// <returns></returns>
		private Point FixMouseCoordinates(System.Windows.Point currentMousePoint)
		{
			var currentMouse = new Point((int) currentMousePoint.X, (int) currentMousePoint.Y);
			if (_fixMode == FixMode.Initiated)
			{
				if (_previousMousePos.X != currentMouse.X)
				{
					_fixMode = FixMode.Vertical;
				}
				else if (_previousMousePos.Y != currentMouse.Y)
				{
					_fixMode = FixMode.Horizontal;
				}
			}
			else if (_fixMode == FixMode.Vertical)
			{
				currentMouse = new Point(currentMouse.X, _previousMousePos.Y);
			}
			else if (_fixMode == FixMode.Horizontal)
			{
				currentMouse = new Point(_previousMousePos.X, currentMouse.Y);
			}
			_previousMousePos = currentMouse;
			return currentMouse;
		}

		/// <summary>
		///     The mouse move handler of the capture form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			// Make sure the mouse coordinates are fixed, when pressing shift
			_mouseMovePos = FixMouseCoordinates(User32.GetCursorLocation());
			_mouseMovePos = ScreenHelper.GetLocationRelativeToScreenBounds(_mouseMovePos);
		}

		/// <summary>
		///     Helper method to simplify check
		/// </summary>
		/// <param name="animator"></param>
		/// <returns></returns>
		private bool IsAnimating(IAnimator animator)
		{
			if (animator == null)
			{
				return false;
			}
			return animator.hasNext;
		}

		/// <summary>
		///     update the frame, this only invalidates
		/// </summary>
		protected override Task Animate(CancellationToken token = default(CancellationToken))
		{
			Point lastPos = _cursorPos;
			_cursorPos = _mouseMovePos;

			if ((SelectedCaptureWindow != null) && lastPos.Equals(_cursorPos) && !IsAnimating(_zoomAnimator) && !IsAnimating(_windowAnimator))
			{
				return Task.FromResult(true);
			}

			WindowDetails lastWindow = SelectedCaptureWindow;
			bool horizontalMove = false;
			bool verticalMove = false;

			if (lastPos.X != _cursorPos.X)
			{
				horizontalMove = true;
			}
			if (lastPos.Y != _cursorPos.Y)
			{
				verticalMove = true;
			}

			if ((UsedCaptureMode == CaptureModes.Region) && _mouseDown)
			{
				_captureRect = new Rectangle(_cursorPos.X, _cursorPos.Y, _mX - _cursorPos.X, _mY - _cursorPos.Y).MakeGuiRectangle();
			}

			// Iterate over the found windows and check if the current location is inside a window
			var cursorPosition = Cursor.Position;

			if ((_windows != null) && (_windows.Count > 0))
			{
				SelectedCaptureWindow = null;
				foreach (var window in _windows)
				{
					if (!window.Contains(cursorPosition))
					{
						continue;
					}
					// Only go over the children if we are in window mode
					SelectedCaptureWindow = CaptureModes.Window == UsedCaptureMode ? window.FindChildUnderPoint(cursorPosition) : window;
					break;
				}
			}


			Rectangle invalidateRectangle;
			if (_mouseDown && (UsedCaptureMode != CaptureModes.Window))
			{
				int x1 = Math.Min(_mX, lastPos.X);
				int x2 = Math.Max(_mX, lastPos.X);
				int y1 = Math.Min(_mY, lastPos.Y);
				int y2 = Math.Max(_mY, lastPos.Y);
				x1 = Math.Min(x1, _cursorPos.X);
				x2 = Math.Max(x2, _cursorPos.X);
				y1 = Math.Min(y1, _cursorPos.Y);
				y2 = Math.Max(y2, _cursorPos.Y);

				// Safety correction
				x2 += 2;
				y2 += 2;

				// Here we correct for text-size

				// Calculate the size
				int textForWidth = Math.Max(Math.Abs(_mX - _cursorPos.X), Math.Abs(_mX - lastPos.X));
				int textForHeight = Math.Max(Math.Abs(_mY - _cursorPos.Y), Math.Abs(_mY - lastPos.Y));

				using (Font rulerFont = new Font(FontFamily.GenericSansSerif, 8))
				{
					Size measureWidth = TextRenderer.MeasureText(textForWidth.ToString(CultureInfo.InvariantCulture), rulerFont);
					x1 -= measureWidth.Width + 15;

					Size measureHeight = TextRenderer.MeasureText(textForHeight.ToString(CultureInfo.InvariantCulture), rulerFont);
					y1 -= measureHeight.Height + 10;
				}
				invalidateRectangle = new Rectangle(x1, y1, x2 - x1, y2 - y1);
				Invalidate(invalidateRectangle);
			}
			else if (UsedCaptureMode != CaptureModes.Window)
			{
				if (!IsTerminalServerSession)
				{
					Rectangle allScreenBounds = ScreenHelper.GetScreenBounds();
					allScreenBounds.Location = ScreenHelper.GetLocationRelativeToScreenBounds(allScreenBounds.Location);
					if (verticalMove)
					{
						// Before
						invalidateRectangle = new Rectangle(allScreenBounds.Left, lastPos.Y - 2, Width + 2, 45).MakeGuiRectangle();
						Invalidate(invalidateRectangle);
						// After
						invalidateRectangle = new Rectangle(allScreenBounds.Left, _cursorPos.Y - 2, Width + 2, 45).MakeGuiRectangle();
						Invalidate(invalidateRectangle);
					}
					if (horizontalMove)
					{
						// Before
						invalidateRectangle = new Rectangle(lastPos.X - 2, allScreenBounds.Top, 75, Height + 2).MakeGuiRectangle();
						Invalidate(invalidateRectangle);
						// After
						invalidateRectangle = new Rectangle(_cursorPos.X - 2, allScreenBounds.Top, 75, Height + 2).MakeGuiRectangle();
						Invalidate(invalidateRectangle);
					}
				}
			}
			else if ((SelectedCaptureWindow != null) && !SelectedCaptureWindow.Equals(lastWindow))
			{
				// Window changes, make new animation from current to target
				_windowAnimator.ChangeDestination(_captureRect, FramesForMillis(700));
			}

			// always animate the Window area through to the last frame, so we see the fade-in/out untill the end
			// Using a safety "offset" to make sure the text is invalidated too
			const int safetySize = 30;

			// Check if we are animating 
			if (IsAnimating(_windowAnimator))
			{
				invalidateRectangle = _windowAnimator.Current;
				invalidateRectangle.Inflate(safetySize, safetySize);
				Invalidate(invalidateRectangle);
				invalidateRectangle = _windowAnimator.Next();
				invalidateRectangle.Inflate(safetySize, safetySize);
				Invalidate(invalidateRectangle);
				// Check if this was the last of the windows animations in the normal region capture.
				if ((UsedCaptureMode != CaptureModes.Window) && !IsAnimating(_windowAnimator))
				{
					Invalidate();
				}
			}

			if ((_zoomAnimator != null) && (IsAnimating(_zoomAnimator) || (UsedCaptureMode != CaptureModes.Window)))
			{
				// Make sure we invalidate the old zoom area
				invalidateRectangle = _zoomAnimator.Current;
				invalidateRectangle.Offset(lastPos);
				Invalidate(invalidateRectangle);
				// Only verify if we are really showing the zoom, not the outgoing animation
				if (_cropConfiguration.ZoomerEnabled && (UsedCaptureMode != CaptureModes.Window))
				{
					VerifyZoomAnimation(_cursorPos, false);
				}
				// The following logic is not needed, next always returns the current if there are no frames left
				// but it makes more sense if we want to change something in the logic
				invalidateRectangle = IsAnimating(_zoomAnimator) ? _zoomAnimator.Next() : _zoomAnimator.Current;
				invalidateRectangle.Offset(_cursorPos);
				Invalidate(invalidateRectangle);
			}
			// Force update "now"
			Update();
			return Task.FromResult(true);
		}

		/// <summary>
		///     This makes sure there is no background painted, as we have complete "paint" control it doesn't make sense to do
		///     otherwise.
		/// </summary>
		/// <param name="pevent"></param>
		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
		}

		/// <summary>
		///     Checks if the Zoom area can move there where it wants to go, change direction if not.
		/// </summary>
		/// <param name="pos">preferred destination location for the zoom area</param>
		/// <param name="allowZoomOverCaptureRect">
		///     false to try to find a location which is neither out of screen bounds nor
		///     intersects with the selected rectangle
		/// </param>
		private void VerifyZoomAnimation(Point pos, bool allowZoomOverCaptureRect)
		{
			Rectangle screenBounds = DisplayInfo.GetBounds(MousePosition);
			// convert to be relative to top left corner of all screen bounds
			screenBounds.Location = ScreenHelper.GetLocationRelativeToScreenBounds(screenBounds.Location);
			int relativeZoomSize = Math.Min(screenBounds.Width, screenBounds.Height)/5;
			// Make sure the final size is a plural of 4, this makes it look better
			relativeZoomSize = relativeZoomSize - relativeZoomSize%4;
			Size zoomSize = new Size(relativeZoomSize, relativeZoomSize);
			Point zoomOffset = new Point(20, 20);

			Rectangle targetRectangle = _zoomAnimator.Final;
			targetRectangle.Offset(pos);
			if (!screenBounds.Contains(targetRectangle) || (!allowZoomOverCaptureRect && _captureRect.IntersectsWith(targetRectangle)))
			{
				Point destinationLocation = Point.Empty;
				Rectangle tl = new Rectangle(pos.X - (zoomOffset.X + zoomSize.Width), pos.Y - (zoomOffset.Y + zoomSize.Height), zoomSize.Width, zoomSize.Height);
				Rectangle tr = new Rectangle(pos.X + zoomOffset.X, pos.Y - (zoomOffset.Y + zoomSize.Height), zoomSize.Width, zoomSize.Height);
				Rectangle bl = new Rectangle(pos.X - (zoomOffset.X + zoomSize.Width), pos.Y + zoomOffset.Y, zoomSize.Width, zoomSize.Height);
				Rectangle br = new Rectangle(pos.X + zoomOffset.X, pos.Y + zoomOffset.Y, zoomSize.Width, zoomSize.Height);
				if (screenBounds.Contains(br) && (allowZoomOverCaptureRect || !_captureRect.IntersectsWith(br)))
				{
					destinationLocation = new Point(zoomOffset.X, zoomOffset.Y);
				}
				else if (screenBounds.Contains(bl) && (allowZoomOverCaptureRect || !_captureRect.IntersectsWith(bl)))
				{
					destinationLocation = new Point(-zoomOffset.X - zoomSize.Width, zoomOffset.Y);
				}
				else if (screenBounds.Contains(tr) && (allowZoomOverCaptureRect || !_captureRect.IntersectsWith(tr)))
				{
					destinationLocation = new Point(zoomOffset.X, -zoomOffset.Y - zoomSize.Width);
				}
				else if (screenBounds.Contains(tl) && (allowZoomOverCaptureRect || !_captureRect.IntersectsWith(tl)))
				{
					destinationLocation = new Point(-zoomOffset.X - zoomSize.Width, -zoomOffset.Y - zoomSize.Width);
				}
				if ((destinationLocation == Point.Empty) && !allowZoomOverCaptureRect)
				{
					VerifyZoomAnimation(pos, true);
				}
				else
				{
					_zoomAnimator.ChangeDestination(new Rectangle(destinationLocation, zoomSize));
				}
			}
		}

		/// <summary>
		///     Draw the zoomed area
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="sourceRectangle"></param>
		/// <param name="destinationRectangle"></param>
		private void DrawZoom(Graphics graphics, Rectangle sourceRectangle, Rectangle destinationRectangle)
		{
			if (_capture.Image == null)
			{
				return;
			}
			ImageAttributes attributes;

			if (_cropConfiguration.ZoomerOpacity < 1)
			{
				//create a color matrix object to change the opacy
				var opacyMatrix = new ColorMatrix
				{
					Matrix33 = _cropConfiguration.ZoomerOpacity
				};
				attributes = new ImageAttributes();
				attributes.SetColorMatrix(opacyMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
			}
			else
			{
				attributes = null;
			}

			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.CompositingQuality = CompositingQuality.HighSpeed;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			using (GraphicsPath path = new GraphicsPath())
			{
				path.AddEllipse(destinationRectangle);
				graphics.SetClip(path);
				if (!(_cropConfiguration.ZoomerOpacity < 1))
				{
					graphics.FillRectangle(_backgroundBrush, destinationRectangle);
					graphics.DrawImage(_capture.Image, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
				}
				else
				{
					graphics.DrawImage(_capture.Image, destinationRectangle, sourceRectangle.X, sourceRectangle.Y, sourceRectangle.Width, sourceRectangle.Height, GraphicsUnit.Pixel, attributes);
				}
			}
			int alpha = (int) (255*_cropConfiguration.ZoomerOpacity);
			Color opacyWhite = Color.FromArgb(alpha, 255, 255, 255);
			Color opacyBlack = Color.FromArgb(alpha, 0, 0, 0);

			// Draw the circle around the zoomer
			using (Pen pen = new Pen(opacyWhite, 2))
			{
				graphics.DrawEllipse(pen, destinationRectangle);
			}

			// Make sure we don't have a pixeloffsetmode/smoothingmode when drawing the crosshair
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.PixelOffsetMode = PixelOffsetMode.None;

			// Calculate some values
			int pixelThickness = destinationRectangle.Width/sourceRectangle.Width;
			int halfWidth = destinationRectangle.Width/2;
			int halfWidthEnd = destinationRectangle.Width/2 - pixelThickness/2;
			int halfHeight = destinationRectangle.Height/2;
			int halfHeightEnd = destinationRectangle.Height/2 - pixelThickness/2;

			int drawAtHeight = destinationRectangle.Y + halfHeight;
			int drawAtWidth = destinationRectangle.X + halfWidth;
			int padding = pixelThickness;

			// Pen to draw
			using (Pen pen = new Pen(opacyBlack, pixelThickness))
			{
				// Draw the croshair-lines
				// Vertical top to middle
				graphics.DrawLine(pen, drawAtWidth, destinationRectangle.Y + padding, drawAtWidth, destinationRectangle.Y + halfHeightEnd - padding);
				// Vertical middle + 1 to bottom
				graphics.DrawLine(pen, drawAtWidth, destinationRectangle.Y + halfHeightEnd + 2*padding, drawAtWidth, destinationRectangle.Y + destinationRectangle.Width - padding);
				// Horizontal left to middle
				graphics.DrawLine(pen, destinationRectangle.X + padding, drawAtHeight, destinationRectangle.X + halfWidthEnd - padding, drawAtHeight);
				// Horizontal middle + 1 to right
				graphics.DrawLine(pen, destinationRectangle.X + halfWidthEnd + 2*padding, drawAtHeight, destinationRectangle.X + destinationRectangle.Width - padding, drawAtHeight);

				// Fix offset for drawing the white rectangle around the crosshair-lines
				drawAtHeight -= pixelThickness/2;
				drawAtWidth -= pixelThickness/2;
				// Fix off by one error with the DrawRectangle
				pixelThickness -= 1;
				// Change the color and the pen width
				pen.Color = opacyWhite;
				pen.Width = 1;
				// Vertical top to middle
				graphics.DrawRectangle(pen, drawAtWidth, destinationRectangle.Y + padding, pixelThickness, halfHeightEnd - 2*padding - 1);
				// Vertical middle + 1 to bottom
				graphics.DrawRectangle(pen, drawAtWidth, destinationRectangle.Y + halfHeightEnd + 2*padding, pixelThickness, halfHeightEnd - 2*padding - 1);
				// Horizontal left to middle
				graphics.DrawRectangle(pen, destinationRectangle.X + padding, drawAtHeight, halfWidthEnd - 2*padding - 1, pixelThickness);
				// Horizontal middle + 1 to right
				graphics.DrawRectangle(pen, destinationRectangle.X + halfWidthEnd + 2*padding, drawAtHeight, halfWidthEnd - 2*padding - 1, pixelThickness);
			}
			attributes?.Dispose();
		}

		/// <summary>
		///     Paint the actual visible parts
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnPaint(object sender, PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			Rectangle clipRectangle = e.ClipRectangle;
			//graphics.BitBlt((Bitmap)buffer, Point.Empty);
			graphics.DrawImageUnscaled(_capture.Image, Point.Empty);
			// Only draw Cursor if it's (partly) visible
			if (_capture.Cursor != null)
			{
				Rectangle cursorBounds = new Rectangle(_capture.CursorLocation, _capture.Cursor.Size);
				if (_capture.CursorVisible && clipRectangle.IntersectsWith(cursorBounds))
				{
					_capture.Cursor.DrawStretched(graphics, cursorBounds);
				}
			}

			if (_mouseDown || (UsedCaptureMode == CaptureModes.Window) || IsAnimating(_windowAnimator))
			{
				_captureRect.Intersect(new Rectangle(Point.Empty, _capture.ScreenBounds.Size)); // crop what is outside the screen

				//if (captureMode == CaptureModes.Window) {
				var fixedRect = IsAnimating(_windowAnimator) ? _windowAnimator.Current : _captureRect;

				// TODO: enable when the video screen capture code works reliable
				//if (capture.CaptureDetails.CaptureModes == CaptureModes.Video) {
				//	graphics.FillRectangle(RedOverlayBrush, fixedRect);
				//} else {
				graphics.FillRectangle(_areaOverlayBrush, fixedRect);
				//}
				graphics.DrawRectangle(_areaOverlayPen, fixedRect);

				// rulers
				const int dist = 8;

				string captureWidth;
				string captureHeight;
				// The following fixes the very old incorrect size information bug
				if (UsedCaptureMode == CaptureModes.Window)
				{
					captureWidth = _captureRect.Width.ToString(CultureInfo.InvariantCulture);
					captureHeight = _captureRect.Height.ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					captureWidth = (_captureRect.Width + 1).ToString(CultureInfo.InvariantCulture);
					captureHeight = (_captureRect.Height + 1).ToString(CultureInfo.InvariantCulture);
				}
				using (Font rulerFont = new Font(FontFamily.GenericSansSerif, 8))
				{
					Size measureWidth = TextRenderer.MeasureText(captureWidth, rulerFont);
					Size measureHeight = TextRenderer.MeasureText(captureHeight, rulerFont);
					int hSpace = measureWidth.Width + 3;
					int vSpace = measureHeight.Height + 3;
					using (Brush bgBrush = new SolidBrush(_cropConfiguration.CropAreaColor))
					using (Pen rulerPen = new Pen(_cropConfiguration.CropRulerLinesColor))
					{
						// horizontal ruler
						if (fixedRect.Width > hSpace + 3)
						{
							using (GraphicsPath p = RoundedRectangle.Create2(fixedRect.X + (fixedRect.Width/2 - hSpace/2) + 3, fixedRect.Y - dist - 7, measureWidth.Width - 3, measureWidth.Height, 3))
							{
								graphics.FillPath(bgBrush, p);
								graphics.DrawPath(rulerPen, p);
								graphics.DrawString(captureWidth, rulerFont, rulerPen.Brush, fixedRect.X + (fixedRect.Width/2 - hSpace/2) + 3, fixedRect.Y - dist - 7);
								graphics.DrawLine(rulerPen, fixedRect.X, fixedRect.Y - dist, fixedRect.X + (fixedRect.Width/2 - hSpace/2), fixedRect.Y - dist);
								graphics.DrawLine(rulerPen, fixedRect.X + fixedRect.Width/2 + hSpace/2, fixedRect.Y - dist, fixedRect.X + fixedRect.Width, fixedRect.Y - dist);
								graphics.DrawLine(rulerPen, fixedRect.X, fixedRect.Y - dist - 3, fixedRect.X, fixedRect.Y - dist + 3);
								graphics.DrawLine(rulerPen, fixedRect.X + fixedRect.Width, fixedRect.Y - dist - 3, fixedRect.X + fixedRect.Width, fixedRect.Y - dist + 3);
							}
						}

						// vertical ruler
						if (fixedRect.Height > vSpace + 3)
						{
							using (GraphicsPath p = RoundedRectangle.Create2(fixedRect.X - measureHeight.Width + 1, fixedRect.Y + (fixedRect.Height/2 - vSpace/2) + 2, measureHeight.Width - 3, measureHeight.Height - 1, 3))
							{
								graphics.FillPath(bgBrush, p);
								graphics.DrawPath(rulerPen, p);
								graphics.DrawString(captureHeight, rulerFont, rulerPen.Brush, fixedRect.X - measureHeight.Width + 1, fixedRect.Y + (fixedRect.Height/2 - vSpace/2) + 2);
								graphics.DrawLine(rulerPen, fixedRect.X - dist, fixedRect.Y, fixedRect.X - dist, fixedRect.Y + (fixedRect.Height/2 - vSpace/2));
								graphics.DrawLine(rulerPen, fixedRect.X - dist, fixedRect.Y + fixedRect.Height/2 + vSpace/2, fixedRect.X - dist, fixedRect.Y + fixedRect.Height);
								graphics.DrawLine(rulerPen, fixedRect.X - dist - 3, fixedRect.Y, fixedRect.X - dist + 3, fixedRect.Y);
								graphics.DrawLine(rulerPen, fixedRect.X - dist - 3, fixedRect.Y + fixedRect.Height, fixedRect.X - dist + 3, fixedRect.Y + fixedRect.Height);
							}
						}
					}
				}

				// Display size of selected rectangle
				// Prepare the font and text.
				using (Font sizeFont = new Font(FontFamily.GenericSansSerif, 12))
				{
					// When capturing a Region we need to add 1 to the height/width for correction
					string sizeText;
					if (UsedCaptureMode == CaptureModes.Region)
					{
						// correct the GUI width to real width for the shown size
						sizeText = _captureRect.Width + 1 + " x " + (_captureRect.Height + 1);
					}
					else
					{
						sizeText = _captureRect.Width + " x " + _captureRect.Height;
					}

					// Calculate the scaled font size.
					SizeF extent = graphics.MeasureString(sizeText, sizeFont);
					float hRatio = _captureRect.Height/(extent.Height*2);
					float wRatio = _captureRect.Width/(extent.Width*2);
					float ratio = hRatio < wRatio ? hRatio : wRatio;
					float newSize = sizeFont.Size*ratio;

					if (newSize >= 4)
					{
						// Only show if 4pt or larger.
						if (newSize > 20)
						{
							newSize = 20;
						}
						// Draw the size.
						using (var brush = new SolidBrush(_cropConfiguration.CropRulerAreaColor))
						using (Font newSizeFont = new Font(FontFamily.GenericSansSerif, newSize, FontStyle.Bold))
						{
							PointF sizeLocation = new PointF(fixedRect.X + _captureRect.Width/2 - extent.Width/2, fixedRect.Y + _captureRect.Height/2 - newSizeFont.GetHeight()/2);
							graphics.DrawString(sizeText, newSizeFont, brush, sizeLocation);
						}
					}
				}
			}
			else
			{
				if (!IsTerminalServerSession)
				{
					using (Pen pen = new Pen(_cropConfiguration.CropRulerAreaColor))
					{
						pen.DashStyle = DashStyle.Dot;
						Rectangle screenBounds = _capture.ScreenBounds;
						graphics.DrawLine(pen, _cursorPos.X, screenBounds.Y, _cursorPos.X, screenBounds.Height);
						graphics.DrawLine(pen, screenBounds.X, _cursorPos.Y, screenBounds.Width, _cursorPos.Y);
					}

					string xy = _cursorPos.X + " x " + _cursorPos.Y;
					using (Font f = new Font(FontFamily.GenericSansSerif, 8))
					{
						Size xySize = TextRenderer.MeasureText(xy, f);
						using (GraphicsPath gp = RoundedRectangle.Create2(_cursorPos.X + 5, _cursorPos.Y + 5, xySize.Width - 3, xySize.Height, 3))
						{
							using (Brush bgBrush = new SolidBrush(_cropConfiguration.CropAreaLinesColor))
							{
								graphics.FillPath(bgBrush, gp);
							}
							using (Pen pen = new Pen(_cropConfiguration.CropRulerLinesColor))
							{
								graphics.DrawPath(pen, gp);
								Point coordinatePosition = new Point(_cursorPos.X + 5, _cursorPos.Y + 5);
								graphics.DrawString(xy, f, pen.Brush, coordinatePosition);
							}
						}
					}
				}
			}

			// Zoom
			if ((_zoomAnimator != null) && (IsAnimating(_zoomAnimator) || (UsedCaptureMode != CaptureModes.Window)))
			{
				const int zoomSourceWidth = 25;
				const int zoomSourceHeight = 25;

				Rectangle sourceRectangle = new Rectangle(_cursorPos.X - zoomSourceWidth/2, _cursorPos.Y - zoomSourceHeight/2, zoomSourceWidth, zoomSourceHeight);

				Rectangle destinationRectangle = _zoomAnimator.Current;
				destinationRectangle.Offset(_cursorPos);
				DrawZoom(graphics, sourceRectangle, destinationRectangle);
			}
		}

		#endregion
	}
}