// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Security.Permissions;
using System.Windows.Forms;
using Dapplo.Windows.Desktop;
using Dapplo.Log;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Addons.Animation;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Resources;
using Greenshot.Gfx.Legacy;

namespace Greenshot.Forms
{
    /// <summary>
    ///     The capture form is used to select a part of the capture
    /// </summary>
    public sealed partial class CaptureForm : AnimatingForm
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly Brush GreenOverlayBrush = new SolidBrush(Color.FromArgb(50, Color.MediumSeaGreen));
        private static readonly Brush ScrollingOverlayBrush = new SolidBrush(Color.FromArgb(50, Color.GreenYellow));
        private static readonly Pen OverlayPen = new Pen(Color.FromArgb(50, Color.Black));

        private static readonly Brush BackgroundBrush;
        private readonly ICapture _capture;
        private readonly bool _isZoomerTransparent;
        private readonly IList<IInteropWindow> _windows;
        private readonly IEnumerable<IFormEnhancer> _formEnhancers;
        private NativeRect _captureRect = NativeRect.Empty;
        private NativePoint _cursorPos;
        private FixMode _fixMode = FixMode.None;
        private bool _isCtrlPressed;
        private bool _mouseDown;
        private NativePoint _mouseMovePos;

        private int _mX;
        private int _mY;
        private NativePoint _previousMousePos = NativePoint.Empty;
        // the window which is selected
        private bool _showDebugInfo;
        private RectangleAnimator _windowAnimator;
        private RectangleAnimator _zoomAnimator;

        /// <summary>
        ///     Initialize the background brush
        /// </summary>
        static CaptureForm()
        {
            var backgroundForTransparency = GreenshotResources.Instance.GetBitmap("Checkerboard.Image");
            BackgroundBrush = new TextureBrush(backgroundForTransparency.NativeBitmap, WrapMode.Tile);
        }

        /// <summary>
        ///     This creates the capture form
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="capture">ICapture</param>
        /// <param name="windows">IList of IInteropWindow</param>
        /// <param name="formEnhancers">IEnumerable with IFormEnhancer</param>
        public CaptureForm(ICoreConfiguration coreConfiguration, ICapture capture, IList<IInteropWindow> windows, IEnumerable<IFormEnhancer> formEnhancers) : base(coreConfiguration, null)
        {
            _isZoomerTransparent = _coreConfiguration.ZoomerOpacity < 1;
            ManualLanguageApply = true;
            ManualStoreFields = true;

            // Enable the AnimatingForm
            EnableAnimation = true;

            _capture = capture;
            _windows = windows;
            _formEnhancers = formEnhancers;
            UsedCaptureMode = capture.CaptureDetails.CaptureMode;

            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();

            Text = "Greenshot capture form";

            // Log at close
            if (Log.IsDebugEnabled())
            {
                FormClosing += (s, e) => Log.Debug().WriteLine("Closing captureform");
            }

            // set cursor location
            _cursorPos = _mouseMovePos = WindowCapture.GetCursorLocationRelativeToScreenBounds();

            // Initialize the animations, the window capture zooms out from the cursor to the window under the cursor 
            if (UsedCaptureMode == CaptureMode.Window)
            {
                _windowAnimator = new RectangleAnimator(new NativeRect(_cursorPos, NativeSize.Empty), _captureRect, FramesForMillis(700), EasingTypes.Quintic, EasingModes.EaseOut);
            }

            // Set the zoomer animation
            InitializeZoomer(_coreConfiguration.ZoomerEnabled);

            Bounds = capture.ScreenBounds;

            // Fix missing focus
            ToFront = true;
            TopMost = true;
        }

        /// <summary>
        ///     Property to access the selected capture rectangle
        /// </summary>
        public Rectangle CaptureRectangle => _captureRect;

        /// <summary>
        ///     Property to access the used capture mode
        /// </summary>
        public CaptureMode UsedCaptureMode { get; private set; }

        /// <summary>
        ///     Get the selected window
        /// </summary>
        public IInteropWindow SelectedCaptureWindow { get; private set; }

        /// <summary>
        ///     Get the WindowScroller
        /// </summary>
        public WindowScroller WindowScroller { get; private set; }

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

        /// <summary>
        ///     Create an animation for the zoomer, depending on if it's active or not.
        /// </summary>
        private void InitializeZoomer(bool isOn)
        {
            if (isOn)
            {
                var screenBounds = DisplayInfo.GetBounds(MousePosition);
                var zoomerSize = CalculateZoomSize(screenBounds);

                var initialPosition = new NativePoint(20, 20);
                // Initialize the zoom with an initial position
                _zoomAnimator = new RectangleAnimator(new NativeRect(initialPosition, NativeSize.Empty), new NativeRect(initialPosition, zoomerSize), FramesForMillis(1000), EasingTypes.Quintic, EasingModes.EaseOut);
                VerifyZoomAnimation(_cursorPos, false);
            }
            else
            {
                _zoomAnimator?.ChangeDestination(new NativeRect(NativePoint.Empty, NativeSize.Empty), FramesForMillis(1000));
            }
        }

        private enum FixMode
        {
            None,
            Initiated,
            Horizontal,
            Vertical
        }

        /// <summary>
        ///     Handle the key up event
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="keyEventArgs">KeyEventArgs</param>
        private void CaptureFormKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.KeyCode)
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
            var step = _isCtrlPressed ? 10 : 1;

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
                    break;
                case Keys.M:
                    // Toggle mouse cursor
                    _capture.CursorVisible = !_capture.CursorVisible;
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
                    if (UsedCaptureMode == CaptureMode.Region)
                    {
                        // Toggle zoom
                        _coreConfiguration.ZoomerEnabled = !_coreConfiguration.ZoomerEnabled;
                        InitializeZoomer(_coreConfiguration.ZoomerEnabled);
                        Invalidate();
                    }
                    break;
                case Keys.D:
                    if (UsedCaptureMode == CaptureMode.Window)
                    {
                        // Toggle debug
                        _showDebugInfo = !_showDebugInfo;
                        Invalidate();
                    }
                    break;
                case Keys.Space:
                    // Toggle capture mode
                    switch (UsedCaptureMode)
                    {
                        case CaptureMode.Region:
                            // Set the window capture mode
                            UsedCaptureMode = CaptureMode.Window;
                            // "Fade out" Zoom
                            InitializeZoomer(false);
                            // "Fade in" window
                            _windowAnimator = new RectangleAnimator(new NativeRect(_cursorPos, NativeSize.Empty), _captureRect, FramesForMillis(700), EasingTypes.Quintic, EasingModes.EaseOut);
                            _captureRect = Rectangle.Empty;
                            Invalidate();
                            break;
                        case CaptureMode.Window:
                            // Set the region capture mode
                            UsedCaptureMode = CaptureMode.Region;
                            // "Fade out" window
                            _windowAnimator.ChangeDestination(new NativeRect(_cursorPos, NativeSize.Empty), FramesForMillis(700));
                            // Fade in zoom
                            InitializeZoomer(_coreConfiguration.ZoomerEnabled);
                            _captureRect = Rectangle.Empty;
                            Invalidate();
                            break;
                    }
                    SelectedCaptureWindow = null;
                    OnMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0));
                    break;
                case Keys.Return:
                    // Confirm
                    if (UsedCaptureMode == CaptureMode.Window)
                    {
                        DialogResult = DialogResult.OK;
                    }
                    else if (!_mouseDown)
                    {
                        StartSelecting();
                    }
                    else
                    {
                        FinishSelecting();
                    }
                    break;
                case Keys.F:
                    ToFront = !ToFront;
                    TopMost = !TopMost;
                    break;
            }
        }

        /// <summary>
        ///     The mousedown handler of the capture form
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">MouseEventArgs</param>
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                StartSelecting();
            }
        }

        /// <summary>
        ///     This is called when we start selecting a region
        /// </summary>
        private void StartSelecting()
        {
            var tmpCursorLocation = WindowCapture.GetCursorLocationRelativeToScreenBounds();
            _mX = tmpCursorLocation.X;
            _mY = tmpCursorLocation.Y;
            _mouseDown = true;
            OnMouseMove(this, null);
            Invalidate();
        }

        /// <summary>
        ///     This is called when we are finished selecting a region
        ///     It is possible, that when nothing is selected (empty rectangle), this only invalidates the screen
        /// </summary>
        private void FinishSelecting()
        {
            // If the mouse goes up we set down to false (nice logic!)
            _mouseDown = false;

            // Check if anything is selected
            if (UsedCaptureMode == CaptureMode.Window && SelectedCaptureWindow != null)
            {
                // Go and process the capture
                DialogResult = DialogResult.OK;
            }
            else if (_captureRect.Size.Width * _captureRect.Size.Height > 0)
            {
                // correct the GUI width to real width if Region mode
                if (UsedCaptureMode == CaptureMode.Region)
                {
                    _captureRect = _captureRect.Resize(_captureRect.Width + 1, _captureRect.Height + 1);
                }
                // Go and process the capture
                DialogResult = DialogResult.OK;
            }
            else
            {
                Invalidate();
            }
        }

        /// <summary>
        ///     The mouse up handler of the capture form
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        private void OnMouseUp(object sender, MouseEventArgs mouseEventArgs)
        {
            if (!_mouseDown)
            {
                return;
            }
            FinishSelecting();
        }

        /// <summary>
        ///     This method is used to "fix" the mouse coordinates when keeping shift/ctrl pressed
        /// </summary>
        /// <param name="currentMouse"></param>
        /// <returns></returns>
        private Point FixMouseCoordinates(NativePoint currentMouse)
        {
            switch (_fixMode)
            {
                case FixMode.Initiated:
                    if (_previousMousePos.X != currentMouse.X)
                    {
                        _fixMode = FixMode.Vertical;
                    }
                    else if (_previousMousePos.Y != currentMouse.Y)
                    {
                        _fixMode = FixMode.Horizontal;
                    }
                    break;
                case FixMode.Vertical:
                    currentMouse = new Point(currentMouse.X, _previousMousePos.Y);
                    break;
                case FixMode.Horizontal:
                    currentMouse = new Point(_previousMousePos.X, currentMouse.Y);
                    break;
            }
            _previousMousePos = currentMouse;
            return currentMouse;
        }

        /// <summary>
        ///     The mouse move handler of the capture form
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">MouseEventArgs</param>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var cursorLocation = User32Api.GetCursorLocation();
            var relativeCursorPosition = WindowCapture.GetLocationRelativeToScreenBounds(cursorLocation);
            // Make sure the mouse coordinates are fixed, e.g. when pressing shift
            _mouseMovePos = FixMouseCoordinates(relativeCursorPosition);
        }

        /// <summary>
        ///     Helper method to simplify check
        /// </summary>
        /// <param name="animator"></param>
        /// <returns>bool</returns>
        private bool IsAnimating(IAnimator animator)
        {
            return animator != null && animator.HasNext;
        }

        /// <summary>
        ///     update the frame, this only invalidates
        /// </summary>
        protected override void Animate()
        {
            var lastPos = _cursorPos;
            _cursorPos = _mouseMovePos;

            if (SelectedCaptureWindow != null && lastPos.Equals(_cursorPos) && !IsAnimating(_zoomAnimator) && !IsAnimating(_windowAnimator))
            {
                return;
            }

            var lastWindow = SelectedCaptureWindow;
            var horizontalMove = false;
            var verticalMove = false;

            if (lastPos.X != _cursorPos.X)
            {
                horizontalMove = true;
            }
            if (lastPos.Y != _cursorPos.Y)
            {
                verticalMove = true;
            }

            if (UsedCaptureMode == CaptureMode.Region && _mouseDown)
            {
                _captureRect = new NativeRect(_cursorPos.X, _cursorPos.Y, _mX - _cursorPos.X, _mY - _cursorPos.Y).Normalize();
            }

            // Iterate over the found windows and check if the current location is inside a window
            var cursorPosition = Cursor.Position;
            SelectedCaptureWindow = null;


            // Store the top window
            IInteropWindow selectedTopWindow = null;
            foreach (var window in _windows)
            {
                if (window.Handle == Handle)
                {
                    // Ignore us
                    continue;
                }
                if (!window.GetInfo().Bounds.Contains(cursorPosition))
                {
                    continue;
                }

                selectedTopWindow = window;
                SelectedCaptureWindow = window;

                // Only go over the children if we are in window mode
                if (CaptureMode.Window != UsedCaptureMode)
                {
                    break;
                }

                // Find the child window which is under the mouse
                // Start with the parent, drill down
                var selectedChildWindow = window;
                // TODO: Limit the levels we go down?
                do
                {
                    // Drill down, via the ZOrder
                    var tmpChildWindow = selectedChildWindow
                        .GetZOrderedChildren()
                        .FirstOrDefault(interopWindow => interopWindow.GetInfo().Bounds.Contains(cursorPosition));

                    if (tmpChildWindow == null)
                    {
                        break;
                    }
                    selectedChildWindow = tmpChildWindow;
                } while (true);

                // Assign the found child window
                SelectedCaptureWindow = selectedChildWindow;

                break;
            }

            // Test if something changed
            if (SelectedCaptureWindow != null && !SelectedCaptureWindow.Equals(lastWindow))
            {
                _capture.CaptureDetails.Title = selectedTopWindow.Text;
                _capture.CaptureDetails.AddMetaData("windowtitle", selectedTopWindow.Text);
                if (UsedCaptureMode == CaptureMode.Window)
                {
                    // Recreate the WindowScroller, if this is enabled, so we can detect if we can scroll
                    if (_coreConfiguration.IsScrollingCaptureEnabled)
                    {
                        WindowScroller = SelectedCaptureWindow.GetWindowScroller(ScrollBarTypes.Vertical);
                        if (WindowScroller == null)
                        {
                            foreach (var interopWindow in SelectedCaptureWindow.GetChildren())
                            {
                                interopWindow.Dump();
                            }

                            WindowScroller = SelectedCaptureWindow.GetChildren().Select(child => child.GetWindowScroller(ScrollBarTypes.Vertical)).FirstOrDefault(scroller => scroller != null);
                        }
                    }

                    // We store the bound of the selected (child) window
                    // If it's maximized we take the client-bounds, otherwise we have parts we should not copy.
                    if (SelectedCaptureWindow.IsMaximized())
                    {
                        _captureRect = SelectedCaptureWindow.GetInfo().ClientBounds;
                    }
                    else
                    {
                        _captureRect = SelectedCaptureWindow.GetInfo().Bounds;
                    }

                    // Make sure the bounds fit to it's parent, some windows are bigger than their parent
                    // But only for non popups
                    if (!SelectedCaptureWindow.GetInfo().Style.HasFlag(WindowStyleFlags.WS_POPUP))
                    {
                        var parent = SelectedCaptureWindow.GetParent();
                        while (parent != IntPtr.Zero)
                        {
                            var parentWindow = InteropWindowFactory.CreateFor(parent);
                            _captureRect = _captureRect.Intersect(parentWindow.GetInfo().Bounds);
                            parent = parentWindow.GetParent();
                        }
                    }

                    // As the ClientRectangle is in screen coordinates and not in bitmap coordinates, we need to correct.
                    _captureRect = _captureRect.Offset(-_capture.ScreenBounds.Location.X, -_capture.ScreenBounds.Location.Y);
                }
            }

            NativeRectFloat invalidateRectangle;
            if (_mouseDown && UsedCaptureMode != CaptureMode.Window)
            {
                var x1 = Math.Min(_mX, lastPos.X);
                var x2 = Math.Max(_mX, lastPos.X);
                var y1 = Math.Min(_mY, lastPos.Y);
                var y2 = Math.Max(_mY, lastPos.Y);
                x1 = Math.Min(x1, _cursorPos.X);
                x2 = Math.Max(x2, _cursorPos.X);
                y1 = Math.Min(y1, _cursorPos.Y);
                y2 = Math.Max(y2, _cursorPos.Y);

                // Safety correction
                x2 += 2;
                y2 += 2;

                // Here we correct for text-size

                // Calculate the size
                var textForWidth = Math.Max(Math.Abs(_mX - _cursorPos.X), Math.Abs(_mX - lastPos.X));
                var textForHeight = Math.Max(Math.Abs(_mY - _cursorPos.Y), Math.Abs(_mY - lastPos.Y));

                using (var rulerFont = new Font(FontFamily.GenericSansSerif, 8))
                {
                    var textWidth = TextRenderer.MeasureText(textForWidth.ToString(CultureInfo.InvariantCulture), rulerFont);
                    x1 -= textWidth.Width + 15;

                    var textHeight = TextRenderer.MeasureText(textForHeight.ToString(CultureInfo.InvariantCulture), rulerFont);
                    y1 -= textHeight.Height + 10;
                }
                invalidateRectangle = new Rectangle(x1, y1, x2 - x1, y2 - y1);
                Invalidate(invalidateRectangle);
            }
            else if (UsedCaptureMode != CaptureMode.Window)
            {
                var allScreenBounds = DisplayInfo.ScreenBounds;

                allScreenBounds = allScreenBounds.MoveTo(WindowCapture.GetLocationRelativeToScreenBounds(allScreenBounds.Location));
                if (verticalMove)
                {
                    // Before
                    invalidateRectangle = new NativeRect(allScreenBounds.Left, lastPos.Y - 2, Width + 2, 45).Normalize();
                    Invalidate(invalidateRectangle);
                    // After
                    invalidateRectangle = new NativeRect(allScreenBounds.Left, _cursorPos.Y - 2, Width + 2, 45).Normalize();
                    Invalidate(invalidateRectangle);
                }
                if (horizontalMove)
                {
                    // Before
                    invalidateRectangle = new NativeRect(lastPos.X - 2, allScreenBounds.Top, 75, Height + 2).Normalize();
                    Invalidate(invalidateRectangle);
                    // After
                    invalidateRectangle = new NativeRect(_cursorPos.X - 2, allScreenBounds.Top, 75, Height + 2).Normalize();
                    Invalidate(invalidateRectangle);
                }
            }
            else if (SelectedCaptureWindow != null && !SelectedCaptureWindow.Equals(lastWindow))
            {
                // Window changed, animate from current to newly selected window
                _windowAnimator.ChangeDestination(_captureRect, FramesForMillis(700));
            }
            // always animate the Window area through to the last frame, so we see the fade-in/out untill the end
            // Using a safety "offset" to make sure the text is invalidated too
            const int safetySize = 30;
            // Check if the animation needs to be drawn
            if (IsAnimating(_windowAnimator))
            {
                invalidateRectangle = _windowAnimator.Current.Inflate(safetySize, safetySize);
                Invalidate(invalidateRectangle);
                invalidateRectangle = _windowAnimator.Next().Inflate(safetySize, safetySize);
                Invalidate(invalidateRectangle);
                // Check if this was the last of the windows animations in the normal region capture.
                if (UsedCaptureMode != CaptureMode.Window && !IsAnimating(_windowAnimator))
                {
                    Invalidate();
                }
            }

            if (_zoomAnimator != null && (IsAnimating(_zoomAnimator) || UsedCaptureMode != CaptureMode.Window))
            {
                // Make sure we invalidate the old zoom area
                invalidateRectangle = _zoomAnimator.Current.Offset(lastPos);
                Invalidate(invalidateRectangle);
                // Only verify if we are really showing the zoom, not the outgoing animation
                if (_coreConfiguration.ZoomerEnabled && UsedCaptureMode != CaptureMode.Window)
                {
                    VerifyZoomAnimation(_cursorPos, false);
                }
                // The following logic is not needed, next always returns the current if there are no frames left
                // but it makes more sense if we want to change something in the logic
                invalidateRectangle = IsAnimating(_zoomAnimator) ? _zoomAnimator.Next() : _zoomAnimator.Current;
                Invalidate(invalidateRectangle.Offset(_cursorPos));
            }
            // Force update "now"
            Update();
        }

        /// <summary>
        ///     This makes sure there is no background painted, as we have complete "paint" control it doesn't make sense to do
        ///     otherwise.
        /// </summary>
        /// <param name="paintEventArgs">PaintEventArgs</param>
        protected override void OnPaintBackground(PaintEventArgs paintEventArgs)
        {
            // Ignore the event, to reduce painting
        }

        /// <summary>
        /// Calculate the zoom size
        /// </summary>
        /// <param name="screenBounds">NativeRect with the screenbounds</param>
        /// <returns>NativeSize</returns>
        private NativeSize CalculateZoomSize(NativeRect screenBounds)
        {
            // convert to be relative to top left corner of all screen bounds
            screenBounds = screenBounds.MoveTo(WindowCapture.GetLocationRelativeToScreenBounds(screenBounds.Location));
            var relativeZoomSize = Math.Min(screenBounds.Width, screenBounds.Height) / 5;
            // Make sure the final size is a plural of 4, this makes it look better
            relativeZoomSize = relativeZoomSize - relativeZoomSize % 4;
            return new NativeSize(relativeZoomSize, relativeZoomSize);
        }

        /// <summary>
        ///     Checks if the Zoom area can move there where it wants to go, change direction if not.
        /// </summary>
        /// <param name="pos">preferred destination location for the zoom area</param>
        /// <param name="allowZoomOverCaptureRect">
        ///     false to try to find a location which is neither out of screen bounds nor
        ///     intersects with the selected rectangle
        /// </param>
        private void VerifyZoomAnimation(NativePoint pos, bool allowZoomOverCaptureRect)
        {
            var screenBounds = DisplayInfo.GetBounds(MousePosition);
            var zoomSize = CalculateZoomSize(screenBounds);
            var zoomOffset = new NativePoint(20, 20);

            var targetRectangle = _zoomAnimator.Final.Offset(pos);
            if (screenBounds.Contains(targetRectangle) && (allowZoomOverCaptureRect || !_captureRect.IntersectsWith(targetRectangle)))
            {
                return;
            }
            var destinationLocation = NativePoint.Empty;
            var tl = new NativeRect(pos.X - (zoomOffset.X + zoomSize.Width), pos.Y - (zoomOffset.Y + zoomSize.Height), zoomSize.Width, zoomSize.Height);
            var tr = new NativeRect(pos.X + zoomOffset.X, pos.Y - (zoomOffset.Y + zoomSize.Height), zoomSize.Width, zoomSize.Height);
            var bl = new NativeRect(pos.X - (zoomOffset.X + zoomSize.Width), pos.Y + zoomOffset.Y, zoomSize.Width, zoomSize.Height);
            var br = new NativeRect(pos.X + zoomOffset.X, pos.Y + zoomOffset.Y, zoomSize.Width, zoomSize.Height);
            if (screenBounds.Contains(br) && (allowZoomOverCaptureRect || !_captureRect.IntersectsWith(br)))
            {
                destinationLocation = new NativePoint(zoomOffset.X, zoomOffset.Y);
            }
            else if (screenBounds.Contains(bl) && (allowZoomOverCaptureRect || !_captureRect.IntersectsWith(bl)))
            {
                destinationLocation = new NativePoint(-zoomOffset.X - zoomSize.Width, zoomOffset.Y);
            }
            else if (screenBounds.Contains(tr) && (allowZoomOverCaptureRect || !_captureRect.IntersectsWith(tr)))
            {
                destinationLocation = new NativePoint(zoomOffset.X, -zoomOffset.Y - zoomSize.Width);
            }
            else if (screenBounds.Contains(tl) && (allowZoomOverCaptureRect || !_captureRect.IntersectsWith(tl)))
            {
                destinationLocation = new NativePoint(-zoomOffset.X - zoomSize.Width, -zoomOffset.Y - zoomSize.Width);
            }
            if (destinationLocation == NativePoint.Empty && !allowZoomOverCaptureRect)
            {
                VerifyZoomAnimation(pos, true);
            }
            else
            {
                _zoomAnimator.ChangeDestination(new NativeRect(destinationLocation, zoomSize));
            }
        }

        /// <summary>
        ///     Draw the zoomed area
        /// </summary>
        /// <param name="graphics">Graphics</param>
        /// <param name="sourceRectangle">NativeRect</param>
        /// <param name="destinationRectangle">NativeRect</param>
        private void DrawZoom(Graphics graphics, NativeRect sourceRectangle, NativeRect destinationRectangle)
        {
            if (_capture.Bitmap == null)
            {
                return;
            }
            ImageAttributes attributes;

            if (_isZoomerTransparent)
            {
                //create a color matrix object to change the opacy
                var opacyMatrix = new ColorMatrix
                {
                    Matrix33 = _coreConfiguration.ZoomerOpacity
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

            using (var path = new GraphicsPath())
            {
                path.AddEllipse(destinationRectangle);
                graphics.SetClip(path);
                if (!_isZoomerTransparent)
                {
                    graphics.FillRectangle(BackgroundBrush, destinationRectangle);
                    graphics.DrawImage(_capture.Bitmap.NativeBitmap, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
                }
                else
                {
                    graphics.DrawImage(_capture.Bitmap.NativeBitmap, destinationRectangle, sourceRectangle.X, sourceRectangle.Y, sourceRectangle.Width, sourceRectangle.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            var alpha = (int) (255 * _coreConfiguration.ZoomerOpacity);
            var opacyWhite = Color.FromArgb(alpha, 255, 255, 255);
            var opacyBlack = Color.FromArgb(alpha, 0, 0, 0);

            // Draw the circle around the zoomer
            using (var pen = new Pen(opacyWhite, 2))
            {
                graphics.DrawEllipse(pen, destinationRectangle);
            }

            // Make sure we don't have a pixeloffsetmode/smoothingmode when drawing the crosshair
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.PixelOffsetMode = PixelOffsetMode.None;

            // Calculate some values
            var pixelThickness = destinationRectangle.Width / sourceRectangle.Width;
            var halfWidth = destinationRectangle.Width / 2;
            var halfWidthEnd = destinationRectangle.Width / 2 - pixelThickness / 2;
            var halfHeight = destinationRectangle.Height / 2;
            var halfHeightEnd = destinationRectangle.Height / 2 - pixelThickness / 2;

            var drawAtHeight = destinationRectangle.Y + halfHeight;
            var drawAtWidth = destinationRectangle.X + halfWidth;
            var padding = pixelThickness;

            // Pen to draw
            using (var pen = new Pen(opacyBlack, pixelThickness))
            {
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
                drawAtHeight -= pixelThickness / 2;
                drawAtWidth -= pixelThickness / 2;
                // Fix off by one error with the DrawRectangle
                pixelThickness -= 1;
                // Change the color and the pen width
                pen.Color = opacyWhite;
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
            attributes?.Dispose();
        }

        /// <summary>
        ///     Paint the actual visible parts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPaint(object sender, PaintEventArgs e)
        {
            var graphics = e.Graphics;
            var clipRectangle = e.ClipRectangle;
            graphics.DrawImageUnscaled(_capture.Bitmap.NativeBitmap, Point.Empty);
            // Only draw Cursor if it's (partly) visible
            if (_capture.Cursor != null && _capture.CursorVisible && clipRectangle.IntersectsWith(new Rectangle(_capture.CursorLocation, _capture.Cursor.Size)))
            {
                graphics.DrawIcon(_capture.Cursor, _capture.CursorLocation.X, _capture.CursorLocation.Y);
            }

            if (_mouseDown || UsedCaptureMode == CaptureMode.Window || IsAnimating(_windowAnimator))
            {
                _captureRect = _captureRect.Intersect(new Rectangle(Point.Empty, _capture.ScreenBounds.Size)); // crop what is outside the screen

                var fixedRect = IsAnimating(_windowAnimator) ? _windowAnimator.Current : _captureRect;

                // If the _windowScroller != null, we can (most likely) capture the window with a scrolling technique
                if (WindowScroller != null && Equals(WindowScroller.ScrollBarWindow, SelectedCaptureWindow))
                {
                    graphics.FillRectangle(ScrollingOverlayBrush, fixedRect);
                }
                else
                {
                    graphics.FillRectangle(GreenOverlayBrush, fixedRect);
                }
                graphics.DrawRectangle(OverlayPen, fixedRect);

                // rulers
                const int dist = 8;

                string captureWidth;
                string captureHeight;
                // The following fixes the very old incorrect size information bug
                if (UsedCaptureMode == CaptureMode.Window)
                {
                    captureWidth = _captureRect.Width.ToString(CultureInfo.InvariantCulture);
                    captureHeight = _captureRect.Height.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    captureWidth = (_captureRect.Width + 1).ToString(CultureInfo.InvariantCulture);
                    captureHeight = (_captureRect.Height + 1).ToString(CultureInfo.InvariantCulture);
                }
                using (var rulerFont = new Font(FontFamily.GenericSansSerif, 8))
                {
                    var measureWidth = TextRenderer.MeasureText(captureWidth, rulerFont);
                    var measureHeight = TextRenderer.MeasureText(captureHeight, rulerFont);
                    var hSpace = measureWidth.Width + 3;
                    var vSpace = measureHeight.Height + 3;
                    Brush bgBrush = new SolidBrush(Color.FromArgb(200, 217, 240, 227));
                    var rulerPen = new Pen(Color.SeaGreen);

                    // horizontal ruler
                    if (fixedRect.Width > hSpace + 3)
                    {
                        using (var p = RoundedRectangle.Create2(
                            fixedRect.X + (fixedRect.Width / 2 - hSpace / 2) + 3,
                            fixedRect.Y - dist - 7,
                            measureWidth.Width - 3,
                            measureWidth.Height,
                            3))
                        {
                            graphics.FillPath(bgBrush, p);
                            graphics.DrawPath(rulerPen, p);
                            graphics.DrawString(captureWidth, rulerFont, rulerPen.Brush, fixedRect.X + (fixedRect.Width / 2 - hSpace / 2) + 3, fixedRect.Y - dist - 7);
                            graphics.DrawLine(rulerPen, fixedRect.X, fixedRect.Y - dist, fixedRect.X + (fixedRect.Width / 2 - hSpace / 2), fixedRect.Y - dist);
                            graphics.DrawLine(rulerPen, fixedRect.X + fixedRect.Width / 2 + hSpace / 2, fixedRect.Y - dist, fixedRect.X + fixedRect.Width, fixedRect.Y - dist);
                            graphics.DrawLine(rulerPen, fixedRect.X, fixedRect.Y - dist - 3, fixedRect.X, fixedRect.Y - dist + 3);
                            graphics.DrawLine(rulerPen, fixedRect.X + fixedRect.Width, fixedRect.Y - dist - 3, fixedRect.X + fixedRect.Width, fixedRect.Y - dist + 3);
                        }
                    }

                    // vertical ruler
                    if (fixedRect.Height > vSpace + 3)
                    {
                        using (var p = RoundedRectangle.Create2(
                            fixedRect.X - measureHeight.Width + 1,
                            fixedRect.Y + (fixedRect.Height / 2 - vSpace / 2) + 2,
                            measureHeight.Width - 3,
                            measureHeight.Height - 1,
                            3))
                        {
                            graphics.FillPath(bgBrush, p);
                            graphics.DrawPath(rulerPen, p);
                            graphics.DrawString(captureHeight, rulerFont, rulerPen.Brush, fixedRect.X - measureHeight.Width + 1, fixedRect.Y + (fixedRect.Height / 2 - vSpace / 2) + 2);
                            graphics.DrawLine(rulerPen, fixedRect.X - dist, fixedRect.Y, fixedRect.X - dist, fixedRect.Y + (fixedRect.Height / 2 - vSpace / 2));
                            graphics.DrawLine(rulerPen, fixedRect.X - dist, fixedRect.Y + fixedRect.Height / 2 + vSpace / 2, fixedRect.X - dist, fixedRect.Y + fixedRect.Height);
                            graphics.DrawLine(rulerPen, fixedRect.X - dist - 3, fixedRect.Y, fixedRect.X - dist + 3, fixedRect.Y);
                            graphics.DrawLine(rulerPen, fixedRect.X - dist - 3, fixedRect.Y + fixedRect.Height, fixedRect.X - dist + 3, fixedRect.Y + fixedRect.Height);
                        }
                    }

                    rulerPen.Dispose();
                    bgBrush.Dispose();
                }

                // Display size of selected rectangle
                // Prepare the font and text.
                using (var sizeFont = new Font(FontFamily.GenericSansSerif, 12))
                {
                    // When capturing a Region we need to add 1 to the height/width for correction
                    string sizeText;
                    if (UsedCaptureMode == CaptureMode.Region)
                    {
                        // correct the GUI width to real width for the shown size
                        sizeText = _captureRect.Width + 1 + " x " + (_captureRect.Height + 1);
                    }
                    else
                    {
                        sizeText = _captureRect.Width + " x " + _captureRect.Height;
                    }

                    // Calculate the scaled font size.
                    var extent = graphics.MeasureString(sizeText, sizeFont);
                    var hRatio = _captureRect.Height / (extent.Height * 2);
                    var wRatio = _captureRect.Width / (extent.Width * 2);
                    var ratio = hRatio < wRatio ? hRatio : wRatio;
                    var newSize = sizeFont.Size * ratio;

                    if (newSize >= 4)
                    {
                        // Only show if 4pt or larger.
                        if (newSize > 20)
                        {
                            newSize = 20;
                        }
                        // Draw the size.
                        using (var newSizeFont = new Font(FontFamily.GenericSansSerif, newSize, FontStyle.Bold))
                        {
                            var sizeLocation = new PointF(fixedRect.X + _captureRect.Width / 2 - extent.Width / 2, fixedRect.Y + _captureRect.Height / 2 - newSizeFont.GetHeight() / 2);
                            graphics.DrawString(sizeText, newSizeFont, Brushes.LightSeaGreen, sizeLocation);

                            if (_showDebugInfo && SelectedCaptureWindow != null)
                            {
                                using (var process = Process.GetProcessById(SelectedCaptureWindow.GetProcessId()))
                                {
                                    string title = $"#{SelectedCaptureWindow.Handle.ToInt64():X} - {(SelectedCaptureWindow.Text.Length > 0 ? SelectedCaptureWindow.Text : process.ProcessName)}";
                                    var debugLocation = new PointF(fixedRect.X, fixedRect.Y);
                                    graphics.DrawString(title, sizeFont, Brushes.DarkOrange, debugLocation);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                using (var pen = new Pen(Color.LightSeaGreen))
                {
                    pen.DashStyle = DashStyle.Dot;
                    var screenBounds = _capture.ScreenBounds;
                    graphics.DrawLine(pen, _cursorPos.X, screenBounds.Y, _cursorPos.X, screenBounds.Height);
                    graphics.DrawLine(pen, screenBounds.X, _cursorPos.Y, screenBounds.Width, _cursorPos.Y);
                }

                var xy = _cursorPos.X + " x " + _cursorPos.Y;
                using (var f = new Font(FontFamily.GenericSansSerif, 8))
                {
                    var xySize = TextRenderer.MeasureText(xy, f);
                    using (var gp = RoundedRectangle.Create2(_cursorPos.X + 5, _cursorPos.Y + 5, xySize.Width - 3, xySize.Height, 3))
                    {
                        using (Brush bgBrush = new SolidBrush(Color.FromArgb(200, 217, 240, 227)))
                        {
                            graphics.FillPath(bgBrush, gp);
                        }
                        using (var pen = new Pen(Color.SeaGreen))
                        {
                            graphics.DrawPath(pen, gp);
                            var coordinatePosition = new Point(_cursorPos.X + 5, _cursorPos.Y + 5);
                            graphics.DrawString(xy, f, pen.Brush, coordinatePosition);
                        }
                    }
                }
            }

            // Zoom
            if (_zoomAnimator == null || (!IsAnimating(_zoomAnimator) && UsedCaptureMode == CaptureMode.Window))
            {
                return;
            }
            const int zoomSourceWidth = 25;
            const int zoomSourceHeight = 25;

            var sourceRectangle = new NativeRect(_cursorPos.X - zoomSourceWidth / 2, _cursorPos.Y - zoomSourceHeight / 2, zoomSourceWidth, zoomSourceHeight);

            var destinationRectangle = _zoomAnimator.Current.Offset(_cursorPos);
            DrawZoom(graphics, sourceRectangle, destinationRectangle);
        }
    }
}