/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
using ImageLockMode = System.Drawing.Imaging.ImageLockMode;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using log4net;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;
using Point = System.Windows.Point;

namespace Greenshot.UI.Capture
{
    /// <summary>
    /// Lets the user select a region, a window or text on the frozen capture of the screen.
    /// Space switches between region and window, T to text, Escape cancels.
    /// </summary>
    public partial class CaptureWindow : Window
    {
        private enum FixMode
        {
            None,
            Initiated,
            Horizontal,
            Vertical
        }

        private static readonly ILog Log = LogManager.GetLogger(typeof(CaptureWindow));
        private static readonly ICoreConfiguration Conf = IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <summary>
        /// The zoomer shows this many pixels around the cursor, an odd number so the cursor's pixel is in the middle
        /// </summary>
        private const int ZoomSourceSize = 25;

        private const int WM_DPICHANGED = 0x02E0;

        private readonly ICapture _capture;
        private readonly NativeRect _screenBounds;
        // Remote desktop: no animations and no crosshair, every repaint of the screen costs bandwidth (OptimizeForRDP / DisableRDPOptimizing as before)
        private readonly bool _isRemoteSession = !Conf.DisableRDPOptimizing && (Conf.OptimizeForRDP || SystemParameters.IsRemoteSession);
        private readonly Typeface _labelTypeface;
        private readonly Typeface _boldLabelTypeface;

        private CaptureMode _captureMode;
        private List<WindowDetails> _windows = new List<WindowDetails>();
        private List<CaptureFormHotspot> _hotspots = new List<CaptureFormHotspot>();
        private CaptureFormHotspot _hoveredHotspot;
        private IOcrLineFeature _hoveredLine;
        private WindowDetails _selectedCaptureWindow;
        private NativeRect _captureRect = NativeRect.Empty;
        private NativePoint _cursorPos;
        private NativePoint _mouseDownPos;
        private NativePoint _previousMousePos;
        private FixMode _fixMode = FixMode.None;
        private bool _mouseDown;
        private bool _isCtrlPressed;
        private bool _showDebugInfo;
        // True while the label fade-in storyboard is applied to the label layer
        private bool _labelsFading;
        private bool _zoomerShown;
        private int _zoomSize;
        private NativePoint _zoomOffset = new NativePoint(ZoomerPlacement.Distance, ZoomerPlacement.Distance);
        private double _dpiScale = 1;
        private AnimationClock _selectionClock;

        /// <summary>
        /// The selected rectangle, in coordinates of the capture
        /// </summary>
        public NativeRect CaptureRectangle => _captureRect;

        /// <summary>
        /// The mode the selection ended in
        /// </summary>
        public CaptureMode UsedCaptureMode => _captureMode;

        /// <summary>
        /// The selected window
        /// </summary>
        public WindowDetails SelectedCaptureWindow => _selectedCaptureWindow;

        /// <summary>
        /// Create the window for the capture of the screen
        /// </summary>
        /// <param name="capture">ICapture of the whole screen</param>
        /// <param name="windows">The windows to snap to, in z-order</param>
        public CaptureWindow(ICapture capture, IList<WindowDetails> windows)
        {
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _screenBounds = capture.ScreenBounds;
            _captureMode = capture.CaptureDetails.CaptureMode;

            InitializeComponent();
            var labelFont = (FontFamily)FindResource("LabelFontFamily");
            _labelTypeface = new Typeface(labelFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _boldLabelTypeface = new Typeface(labelFont, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

            if (windows != null)
            {
                lock (capture.CaptureDetails.Features)
                {
                    if (!capture.CaptureDetails.Features.OfType<WindowFeature>().Any())
                    {
                        for (int i = 0; i < windows.Count; i++)
                        {
                            capture.CaptureDetails.Features.Add(new WindowFeature(windows[i], i));
                        }
                    }
                }
            }

            Root.Width = _screenBounds.Width;
            Root.Height = _screenBounds.Height;
            var screenImage = CreateBitmapSource(capture.Image);
            ScreenImage.Source = screenImage;
            ZoomBrush.ImageSource = screenImage;
            ShowCapturedCursor();
            DrawCrosshairLines();

            _cursorPos = WindowCapture.GetCursorLocationRelativeToScreenBounds();
            _previousMousePos = User32Api.GetCursorLocation();
            SetSelection(new Rect(ToPoint(_cursorPos), new Size(0, 0)));

            _capture.CaptureDetails.FeaturesChanged += OnFeaturesChanged;
            RebuildFeatureHotspots();

            SourceInitialized += OnSourceInitialized;
            ContentRendered += OnContentRendered;
            Closed += OnClosed;
            // Preview: the arrow keys would otherwise be taken by the keyboard navigation
            PreviewKeyDown += OnKeyDown;
            PreviewKeyUp += OnKeyUp;
            MouseMove += (sender, args) => UpdateSelection();
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
        }

        /// <summary>
        /// Close the window without a selection
        /// </summary>
        public void Cancel()
        {
            if (IsLoaded)
            {
                DialogResult = false;
            }
            else
            {
                Close();
            }
        }

        private void Accept()
        {
            if (_captureMode == CaptureMode.Text)
            {
                _capture.CaptureDetails.CaptureMode = CaptureMode.Text;
            }
            DialogResult = true;
        }

        /// <summary>
        /// Cover the whole virtual screen, in screen pixels, and map the pixels of the capture 1:1 to the screen
        /// </summary>
        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            // Make sure we never capture the capture window
            WindowDetails.RegisterIgnoreHandle(handle);
            HwndSource.FromHwnd(handle)?.AddHook(WndProc);
            PlaceWindow();

            ApplyDpiScale(VisualTreeHelper.GetDpi(this));
            InitializeZoomer();
            UpdateSelection();
        }

        /// <summary>
        /// WPF scales everything in the window with the DPI of the window, undo that so one unit is one pixel
        /// </summary>
        private void ApplyDpiScale(DpiScale dpi)
        {
            _dpiScale = dpi.DpiScaleX;
            Root.LayoutTransform = new ScaleTransform(1 / _dpiScale, 1 / _dpiScale);
        }

        /// <summary>
        /// Only when WPF changed the DPI of the window anyway, see WndProc
        /// </summary>
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            ApplyDpiScale(newDpi);
            PlaceWindow();
        }

        /// <summary>
        /// Position and size in screen pixels, WPF would use device independent units of one monitor
        /// </summary>
        private void PlaceWindow()
        {
            var handle = new WindowInteropHelper(this).Handle;
            User32Api.SetWindowPos(handle, IntPtr.Zero, _screenBounds.X, _screenBounds.Y, _screenBounds.Width, _screenBounds.Height,
                Dapplo.Windows.User32.Enums.WindowPos.SWP_NOZORDER | Dapplo.Windows.User32.Enums.WindowPos.SWP_NOACTIVATE);
        }

        /// <summary>
        /// A window over monitors with different DPIs gets WM_DPICHANGED when it is moved, WPF would resize and rescale it.
        /// The window keeps the DPI it was created with, the transform of Root matches that.
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DPICHANGED)
            {
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            // Showing the window must not have changed the bounds, but make sure
            PlaceWindow();
            Activate();
            WindowDetails.ToForeground(new WindowInteropHelper(this).Handle);
            if (_captureMode == CaptureMode.Text)
            {
                EnsureOcr();
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Log.Debug("Closing capture window");
            WindowDetails.UnregisterIgnoreHandle(new WindowInteropHelper(this).Handle);
            _capture.CaptureDetails.FeaturesChanged -= OnFeaturesChanged;
        }

        #region features

        private void RebuildFeatureHotspots()
        {
            List<IDetectedFeature> features;
            lock (_capture.CaptureDetails.Features)
            {
                features = new List<IDetectedFeature>(_capture.CaptureDetails.Features);
            }
            _windows = features.OfType<WindowFeature>().OrderBy(f => f.ZIndex).Select(f => f.Window).ToList();

            var hotspots = new List<CaptureFormHotspot>();
            try
            {
                var transformers = SimpleServiceProvider.Current.GetAllInstances<IFeatureHotspotTransformer>();
                foreach (var feature in features)
                {
                    foreach (var transformer in transformers)
                    {
                        if (!transformer.CanTransform(feature))
                        {
                            continue;
                        }
                        var hotspot = transformer.Transform(feature);
                        if (hotspot != null)
                        {
                            hotspots.Add(hotspot);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error transforming capture features to hotspots", ex);
            }
            _hotspots = hotspots;
            _hoveredHotspot = null;
        }

        private void OnFeaturesChanged(object sender, EventArgs e)
        {
            // Raised by background processors: marshal to the UI thread (always posted, also when raised on the UI thread)
            var ui = SimpleServiceProvider.Current.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance;
            ui.InvokeAsync(() =>
            {
                if (!IsVisible) return;
                RebuildFeatureHotspots();
                DrawFeatures();
            }).FireAndLog("Capture window features changed", Log);
        }

        private List<IOcrLineFeature> GetOcrLines()
        {
            lock (_capture.CaptureDetails.Features)
            {
                return _capture.CaptureDetails.Features.OfType<IOcrLineFeature>().ToList();
            }
        }

        /// <summary>
        /// Start the OCR for the text mode when there are no text lines yet, and it isn't running in the background already
        /// </summary>
        private void EnsureOcr()
        {
            if (GetOcrLines().Any())
            {
                return;
            }
            var processingTask = _capture.CaptureDetails.ProcessingTask;
            if (processingTask != null && !processingTask.IsCompleted)
            {
                // Already processing in the background, the features changed event redraws when finished
                return;
            }
            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
            if (ocrProvider == null)
            {
                return;
            }
            // Started on the UI thread: the OCR result is merged and the window redrawn there
            var ocrTask = RunOcrAsync(ocrProvider);
            _capture.CaptureDetails.ProcessingTask = processingTask != null ? Task.WhenAll(processingTask, ocrTask) : ocrTask;
        }

        private async Task RunOcrAsync(IOcrProvider ocrProvider)
        {
            var ocrLines = await ocrProvider.DoOcrAsync(_capture.Image).ConfigureAwait(true);
            if (ocrLines != null && ocrLines.Any())
            {
                lock (_capture.CaptureDetails.Features)
                {
                    _capture.CaptureDetails.Features.AddRange(ocrLines);
                }

                if (_capture.CaptureDetails is CaptureDetails concreteDetails)
                {
                    concreteDetails.NotifyFeaturesChanged();
                }
            }

            if (IsVisible)
            {
                DrawFeatures();
            }
        }

        #endregion

        #region input

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                    _fixMode = FixMode.None;
                    break;
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    _isCtrlPressed = false;
                    break;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            int step = _isCtrlPressed ? 10 : 1;
            var cursor = System.Windows.Forms.Cursor.Position;
            switch (e.Key)
            {
                case Key.Up:
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(cursor.X, cursor.Y - step);
                    break;
                case Key.Down:
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(cursor.X, cursor.Y + step);
                    break;
                case Key.Left:
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(cursor.X - step, cursor.Y);
                    break;
                case Key.Right:
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(cursor.X + step, cursor.Y);
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    // Fix mode: keep the selection to one direction
                    if (_fixMode == FixMode.None)
                    {
                        _fixMode = FixMode.Initiated;
                    }
                    break;
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    _isCtrlPressed = true;
                    break;
                case Key.Escape:
                    Cancel();
                    break;
                case Key.M:
                    // Toggle mouse cursor
                    _capture.CursorVisible = !_capture.CursorVisible;
                    ShowCapturedCursor();
                    break;
                case Key.Z:
                    if (_captureMode == CaptureMode.Region)
                    {
                        Conf.ZoomerEnabled = !Conf.ZoomerEnabled;
                        UpdateZoomerVisibility();
                    }
                    break;
                case Key.D:
                    if (_captureMode == CaptureMode.Window)
                    {
                        _showDebugInfo = !_showDebugInfo;
                        DrawLabels(false);
                    }
                    break;
                case Key.Space:
                    ToggleCaptureMode();
                    break;
                case Key.Return:
                    if (_captureMode == CaptureMode.Window)
                    {
                        Accept();
                    }
                    else if (!_mouseDown)
                    {
                        StartSelection();
                    }
                    else
                    {
                        EndSelection();
                    }
                    break;
                case Key.F:
                    Topmost = !Topmost;
                    break;
                case Key.T:
                    _captureMode = CaptureMode.Text;
                    EnsureOcr();
                    UpdateSelection();
                    DrawFeatures();
                    break;
            }
        }

        private void ToggleCaptureMode()
        {
            switch (_captureMode)
            {
                case CaptureMode.Region:
                    _captureMode = CaptureMode.Window;
                    // The window selection grows out of the cursor
                    SetSelection(new Rect(ToPoint(_cursorPos), new Size(0, 0)));
                    SelectionPath.Visibility = Visibility.Visible;
                    break;
                case CaptureMode.Text:
                    _captureMode = CaptureMode.Region;
                    break;
                case CaptureMode.Window:
                    _captureMode = CaptureMode.Region;
                    // The window selection shrinks into the cursor
                    AnimateSelection(new Rect(ToPoint(_cursorPos), new Size(0, 0)), () =>
                    {
                        if (_captureMode != CaptureMode.Window && !_mouseDown)
                        {
                            SelectionPath.Visibility = Visibility.Collapsed;
                        }
                    });
                    LabelLayer.Clear();
                    break;
            }

            _captureRect = NativeRect.Empty;
            _selectedCaptureWindow = null;
            _mouseDown = false;
            UpdateZoomerVisibility();
            UpdateSelection();
            DrawFeatures();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var hotspot = FindHotspot(GetCursorPosition());
            if (hotspot != null)
            {
                ShowHotspotMenu(hotspot);
                e.Handled = true;
                return;
            }
            StartSelection();
            CaptureMouse();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            if (_mouseDown)
            {
                EndSelection();
            }
        }

        private void StartSelection()
        {
            _mouseDownPos = WindowCapture.GetCursorLocationRelativeToScreenBounds();
            _mouseDown = true;
            UpdateSelection();
        }

        private void EndSelection()
        {
            _mouseDown = false;
            if (_captureMode == CaptureMode.Window && _selectedCaptureWindow != null)
            {
                Accept();
            }
            else if (_captureRect.Height > 3 && _captureRect.Width > 3)
            {
                if (_captureMode is CaptureMode.Region or CaptureMode.Text)
                {
                    // The selection includes the pixel under the cursor
                    _captureRect = new NativeRect(_captureRect.Left, _captureRect.Top, _captureRect.Width + 1, _captureRect.Height + 1);
                }
                Accept();
            }
            else if (_captureMode == CaptureMode.Text && FindOcrLine(_cursorPos) is { } clickedLine)
            {
                // A click on a single line selects it
                _captureRect = clickedLine.Bounds;
                Accept();
            }
            else
            {
                UpdateSelection();
            }
        }

        private void ShowHotspotMenu(CaptureFormHotspot hotspot)
        {
            var menu = new ContextMenu
            {
                PlacementTarget = this,
                Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint
            };
            if (!string.IsNullOrEmpty(hotspot.Text))
            {
                menu.Items.Add(new MenuItem { Header = hotspot.Text, IsEnabled = false });
                menu.Items.Add(new Separator());
            }
            foreach (var action in hotspot.Actions)
            {
                var item = new MenuItem { Header = action.Text };
                item.Click += (sender, args) =>
                {
                    // The action replaces the capture
                    Cancel();
                    try
                    {
                        action.Execute();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error running the hotspot action {action.Text}", ex);
                    }
                };
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        /// <summary>
        /// The cursor position in coordinates of the capture, corrected for the fix mode (shift)
        /// </summary>
        private NativePoint GetCursorPosition()
        {
            var currentMouse = User32Api.GetCursorLocation();
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
                    currentMouse = new NativePoint(currentMouse.X, _previousMousePos.Y);
                    break;
                case FixMode.Horizontal:
                    currentMouse = new NativePoint(_previousMousePos.X, currentMouse.Y);
                    break;
            }

            _previousMousePos = currentMouse;
            return WindowCapture.GetLocationRelativeToScreenBounds(currentMouse);
        }

        private CaptureFormHotspot FindHotspot(NativePoint location) => _hotspots.FirstOrDefault(hotspot => hotspot.Bounds.Contains(location));

        private IOcrLineFeature FindOcrLine(NativePoint location) => _captureMode == CaptureMode.Text ? GetOcrLines().FirstOrDefault(line => line.Bounds.Contains(location)) : null;

        #endregion

        #region selection

        /// <summary>
        /// Update everything which depends on the cursor, called for every mouse move
        /// </summary>
        private void UpdateSelection()
        {
            _cursorPos = GetCursorPosition();
            bool isRegion = _captureMode is CaptureMode.Region or CaptureMode.Text;

            var hoveredHotspot = FindHotspot(_cursorPos);
            Cursor = hoveredHotspot != null ? Cursors.Hand : Cursors.Cross;
            ToolTip = hoveredHotspot?.ToolTipText;
            bool redrawFeatures = hoveredHotspot != _hoveredHotspot;
            _hoveredHotspot = hoveredHotspot;

            if (isRegion && _mouseDown)
            {
                _captureRect = new NativeRect(_cursorPos.X, _cursorPos.Y, _mouseDownPos.X - _cursorPos.X, _mouseDownPos.Y - _cursorPos.Y).Normalize();
                SetSelection(ToRect(_captureRect));
                SelectionPath.Visibility = Visibility.Visible;
                DrawLabels(false);
            }
            else if (isRegion && SelectionPath.Visibility == Visibility.Visible && !IsSelectionAnimating())
            {
                SelectionPath.Visibility = Visibility.Collapsed;
                LabelLayer.Clear();
            }

            UpdateSelectedWindow();
            UpdateCrosshair(isRegion && !_mouseDown);
            UpdateZoomer();

            if (_captureMode == CaptureMode.Text)
            {
                var hoveredLine = _mouseDown ? null : FindOcrLine(_cursorPos);
                // While selecting the highlighted words change with every move
                redrawFeatures |= _mouseDown || hoveredLine != _hoveredLine;
                _hoveredLine = hoveredLine;
            }

            if (redrawFeatures)
            {
                DrawFeatures();
            }
        }

        /// <summary>
        /// Find the window under the cursor, in window mode the selection moves there
        /// </summary>
        private void UpdateSelectedWindow()
        {
            var lastWindow = _selectedCaptureWindow;
            // In screen coordinates, as the windows are
            var cursorPosition = User32Api.GetCursorLocation();
            _selectedCaptureWindow = null;
            foreach (var window in _windows)
            {
                if (!window.Contains(cursorPosition))
                {
                    continue;
                }

                // Only go over the children in window mode
                _selectedCaptureWindow = _captureMode == CaptureMode.Window ? window.FindChildUnderPoint(cursorPosition) : window;
                break;
            }

            if (_selectedCaptureWindow == null || _selectedCaptureWindow.Equals(lastWindow))
            {
                return;
            }

            _capture.CaptureDetails.Title = _selectedCaptureWindow.Text;
            _capture.CaptureDetails.AddMetaData("windowtitle", _selectedCaptureWindow.Text);
            if (_captureMode != CaptureMode.Window)
            {
                return;
            }

            _captureRect = GetClippedWindowRectangle(_selectedCaptureWindow)
                .Offset(-_screenBounds.X, -_screenBounds.Y)
                .Intersect(new NativeRect(0, 0, _screenBounds.Width, _screenBounds.Height));
            SelectionPath.Visibility = Visibility.Visible;
            AnimateSelection(ToRect(_captureRect), null);
            DrawLabels(true);
        }

        private static NativeRect GetClippedWindowRectangle(WindowDetails window)
        {
            var rect = window.WindowRectangle;
            var parent = window.GetParent();
            while (parent != null)
            {
                rect = rect.Intersect(parent.WindowRectangle);
                parent = parent.GetParent();
            }
            return rect;
        }

        /// <summary>
        /// Animate the selection from where it is now to the target
        /// </summary>
        private void AnimateSelection(Rect target, Action completed)
        {
            var animation = ((RectAnimation)FindResource("WindowSelectionAnimation")).Clone();
            animation.To = target;
            if (_isRemoteSession)
            {
                // Jump there, the completed handler still runs
                animation.Duration = new Duration(TimeSpan.Zero);
            }
            var clock = animation.CreateClock();
            clock.Completed += (sender, args) =>
            {
                if (_selectionClock == clock)
                {
                    _selectionClock = null;
                    completed?.Invoke();
                }
            };
            _selectionClock = clock;
            SelectionGeometry.ApplyAnimationClock(RectangleGeometry.RectProperty, clock, HandoffBehavior.SnapshotAndReplace);
        }

        private bool IsSelectionAnimating() => _selectionClock != null;

        /// <summary>
        /// Show the selection there, without animation
        /// </summary>
        private void SetSelection(Rect rect)
        {
            _selectionClock = null;
            SelectionGeometry.BeginAnimation(RectangleGeometry.RectProperty, null);
            SelectionGeometry.Rect = rect;
        }

        /// <summary>
        /// The size of the selection and the rulers at its sides
        /// </summary>
        /// <param name="fadeIn">true to let them appear when a window selection arrives</param>
        private void DrawLabels(bool fadeIn)
        {
            if (_captureRect.IsEmpty)
            {
                LabelLayer.Clear();
                return;
            }

            var rect = _captureRect;
            bool isRegion = _captureMode is CaptureMode.Region or CaptureMode.Text;
            // A region includes the pixel under the cursor
            int width = isRegion ? rect.Width + 1 : rect.Width;
            int height = isRegion ? rect.Height + 1 : rect.Height;
            string widthText = width.ToString(CultureInfo.InvariantCulture);
            string heightText = height.ToString(CultureInfo.InvariantCulture);
            var rulerBrush = (Brush)FindResource("RulerBrush");
            var rulerBackground = (Brush)FindResource("RulerBackgroundBrush");
            var rulerPen = new Pen(rulerBrush, 1);
            rulerPen.Freeze();
            const int dist = 8;

            using (var dc = LabelLayer.Open())
            {
                // Horizontal ruler above the selection
                var widthLabel = CreateText(widthText, 8, rulerBrush);
                double hSpace = widthLabel.Width + 6;
                if (rect.Width > hSpace + 3)
                {
                    double y = rect.Y - dist + 0.5;
                    double middle = rect.X + rect.Width / 2.0;
                    var labelRect = new Rect(middle - widthLabel.Width / 2 - 2, y - widthLabel.Height / 2, widthLabel.Width + 4, widthLabel.Height);
                    dc.DrawLine(rulerPen, new Point(rect.X, y), new Point(labelRect.Left, y));
                    dc.DrawLine(rulerPen, new Point(labelRect.Right, y), new Point(rect.Right, y));
                    dc.DrawLine(rulerPen, new Point(rect.X + 0.5, y - 3), new Point(rect.X + 0.5, y + 3));
                    dc.DrawLine(rulerPen, new Point(rect.Right - 0.5, y - 3), new Point(rect.Right - 0.5, y + 3));
                    dc.DrawRoundedRectangle(rulerBackground, rulerPen, labelRect, 3, 3);
                    dc.DrawText(widthLabel, new Point(labelRect.X + 2, labelRect.Y));
                }

                // Vertical ruler left of the selection
                var heightLabel = CreateText(heightText, 8, rulerBrush);
                double vSpace = heightLabel.Height + 6;
                if (rect.Height > vSpace + 3)
                {
                    double x = rect.X - dist + 0.5;
                    double middle = rect.Y + rect.Height / 2.0;
                    var labelRect = new Rect(x - heightLabel.Width - 2, middle - heightLabel.Height / 2, heightLabel.Width + 4, heightLabel.Height);
                    dc.DrawLine(rulerPen, new Point(x, rect.Y), new Point(x, labelRect.Top));
                    dc.DrawLine(rulerPen, new Point(x, labelRect.Bottom), new Point(x, rect.Bottom));
                    dc.DrawLine(rulerPen, new Point(x - 3, rect.Y + 0.5), new Point(x + 3, rect.Y + 0.5));
                    dc.DrawLine(rulerPen, new Point(x - 3, rect.Bottom - 0.5), new Point(x + 3, rect.Bottom - 0.5));
                    dc.DrawRoundedRectangle(rulerBackground, rulerPen, labelRect, 3, 3);
                    dc.DrawText(heightLabel, new Point(labelRect.X + 2, labelRect.Y));
                }

                // The size in the middle, as large as fits up to 20pt, not shown below 4pt
                string sizeText = width + " x " + height;
                var measure = CreateText(sizeText, 12, Brushes.Transparent);
                double ratio = Math.Min(rect.Height / (measure.Height * 2), rect.Width / (measure.Width * 2));
                double fontSize = Math.Min(12 * ratio, 20);
                if (fontSize >= 4)
                {
                    var sizeLabel = CreateText(sizeText, fontSize, (Brush)FindResource("SizeTextBrush"), bold: true);
                    dc.DrawText(sizeLabel, new Point(rect.X + rect.Width / 2.0 - sizeLabel.Width / 2, rect.Y + rect.Height / 2.0 - sizeLabel.Height / 2));

                    if (_showDebugInfo && _selectedCaptureWindow != null)
                    {
                        string title = $"#{_selectedCaptureWindow.Handle.ToInt64():X} - {(_selectedCaptureWindow.Text.Length > 0 ? _selectedCaptureWindow.Text : _selectedCaptureWindow.Process?.ProcessName)}";
                        dc.DrawText(CreateText(title, 12, (Brush)FindResource("DebugTextBrush")), new Point(rect.X, rect.Y));
                    }
                }
            }

            if (fadeIn && !_isRemoteSession)
            {
                ((Storyboard)FindResource("ShowLabelsStoryboard")).Begin(this, true);
                _labelsFading = true;
            }
            else if (_labelsFading)
            {
                // Only remove a storyboard which was started, removing one which never was logs a warning on every mouse move
                ((Storyboard)FindResource("ShowLabelsStoryboard")).Remove(this);
                _labelsFading = false;
            }
        }

        #endregion

        #region crosshair and zoomer

        /// <summary>
        /// The crosshair lines are drawn once over the whole capture, moving them is a transform
        /// </summary>
        private void DrawCrosshairLines()
        {
            // Dotted: one pixel on, one off
            var pen = new Pen((Brush)FindResource("CrosshairBrush"), 1) { DashStyle = new DashStyle(new[] { 1.0, 1.0 }, 0) };
            pen.Freeze();
            using (var dc = VerticalLine.Open())
            {
                dc.DrawLine(pen, new Point(0.5, 0), new Point(0.5, _screenBounds.Height));
            }
            using (var dc = HorizontalLine.Open())
            {
                dc.DrawLine(pen, new Point(0, 0.5), new Point(_screenBounds.Width, 0.5));
            }
        }

        private void UpdateCrosshair(bool show)
        {
            // Not in a remote session, where every repaint of the screen costs bandwidth
            var visibility = show && !_isRemoteSession ? Visibility.Visible : Visibility.Collapsed;
            VerticalLine.Visibility = visibility;
            HorizontalLine.Visibility = visibility;
            CoordinatesLabel.Visibility = visibility;
            if (visibility != Visibility.Visible)
            {
                return;
            }

            VerticalLinePosition.X = _cursorPos.X;
            HorizontalLinePosition.Y = _cursorPos.Y;
            var text = CreateText(_cursorPos.X + " x " + _cursorPos.Y, 8, (Brush)FindResource("RulerBrush"));
            var labelRect = new Rect(_cursorPos.X + 5.5, _cursorPos.Y + 5.5, text.Width + 4, text.Height);
            using var dc = CoordinatesLabel.Open();
            dc.DrawRoundedRectangle((Brush)FindResource("RulerBackgroundBrush"), new Pen((Brush)FindResource("RulerBrush"), 1), labelRect, 3, 3);
            dc.DrawText(text, new Point(labelRect.X + 2, labelRect.Y));
        }

        private void InitializeZoomer()
        {
            Zoomer.Opacity = Conf.ZoomerOpacity;
            // A see-through zoomer shows the screen around it instead of the checkerboard
            ZoomerBackground.Visibility = Conf.ZoomerOpacity < 1 ? Visibility.Collapsed : Visibility.Visible;
            ZoomerOffset.X = _zoomOffset.X;
            ZoomerOffset.Y = _zoomOffset.Y;
            UpdateZoomerVisibility();
        }

        private void UpdateZoomerVisibility()
        {
            bool show = Conf.ZoomerEnabled && _captureMode != CaptureMode.Window;
            if (show == _zoomerShown)
            {
                return;
            }
            _zoomerShown = show;
            if (_isRemoteSession)
            {
                ZoomerScale.ScaleX = ZoomerScale.ScaleY = show ? 1 : 0;
                return;
            }
            ((Storyboard)FindResource(show ? "ShowZoomerStoryboard" : "HideZoomerStoryboard")).Begin(this, true);
        }

        /// <summary>
        /// The zoomer follows the cursor, shows the pixels around it and moves to another corner where needed
        /// </summary>
        private void UpdateZoomer()
        {
            ZoomerPosition.X = _cursorPos.X;
            ZoomerPosition.Y = _cursorPos.Y;
            ZoomBrush.Viewbox = new Rect(_cursorPos.X - ZoomSourceSize / 2, _cursorPos.Y - ZoomSourceSize / 2, ZoomSourceSize, ZoomSourceSize);
            if (!_zoomerShown)
            {
                return;
            }

            var screenBounds = DisplayInfo.GetBounds(User32Api.GetCursorLocation()).Offset(-_screenBounds.X, -_screenBounds.Y);
            int zoomSize = ZoomerPlacement.GetSize(screenBounds);
            if (zoomSize != _zoomSize)
            {
                ResizeZoomer(zoomSize);
            }

            var offset = ZoomerPlacement.GetOffset(_cursorPos, _zoomOffset, zoomSize, screenBounds, _mouseDown ? _captureRect : NativeRect.Empty);
            if (offset.Equals(_zoomOffset))
            {
                return;
            }
            _zoomOffset = offset;
            if (_isRemoteSession)
            {
                ZoomerOffset.X = offset.X;
                ZoomerOffset.Y = offset.Y;
                return;
            }
            var template = (DoubleAnimation)FindResource("ZoomerMoveAnimation");
            var moveX = template.Clone();
            moveX.To = offset.X;
            var moveY = template.Clone();
            moveY.To = offset.Y;
            ZoomerOffset.BeginAnimation(TranslateTransform.XProperty, moveX);
            ZoomerOffset.BeginAnimation(TranslateTransform.YProperty, moveY);
        }

        private void ResizeZoomer(int zoomSize)
        {
            _zoomSize = zoomSize;
            foreach (var ellipse in new[] { ZoomerBackground, ZoomerLens, ZoomerRing })
            {
                ellipse.Width = zoomSize;
                ellipse.Height = zoomSize;
            }

            // The crosshair marks the pixel under the cursor: lines from the border to it, one pixel wide in the zoom
            double pixel = (double)zoomSize / ZoomSourceSize;
            double start = (ZoomSourceSize / 2) * pixel;
            double end = start + pixel;
            var crosshair = new GeometryGroup();
            crosshair.Children.Add(new RectangleGeometry(new Rect(start, pixel, pixel, start - 2 * pixel)));
            crosshair.Children.Add(new RectangleGeometry(new Rect(start, end + pixel, pixel, zoomSize - end - 2 * pixel)));
            crosshair.Children.Add(new RectangleGeometry(new Rect(pixel, start, start - 2 * pixel, pixel)));
            crosshair.Children.Add(new RectangleGeometry(new Rect(end + pixel, start, zoomSize - end - 2 * pixel, pixel)));
            crosshair.Freeze();
            ZoomerCrosshair.Data = crosshair;
            ZoomerCrosshair.Opacity = Conf.ZoomerOpacity;
        }

        #endregion

        #region drawing

        /// <summary>
        /// Draw the hotspots and, in text mode, the text lines with the hovered or selected text
        /// </summary>
        private void DrawFeatures()
        {
            using var dc = FeatureLayer.Open();
            if (_hotspots.Count > 0)
            {
                var hotspotPen = new Pen((Brush)FindResource("HotspotBrush"), 2) { DashStyle = DashStyles.Dash };
                hotspotPen.Freeze();
                var hoverBrush = (Brush)FindResource("HotspotHoverBrush");
                foreach (var hotspot in _hotspots)
                {
                    dc.DrawRectangle(hotspot == _hoveredHotspot ? hoverBrush : null, hotspotPen, ToRect(hotspot.Bounds));
                }
            }

            if (_captureMode != CaptureMode.Text)
            {
                return;
            }

            var linePen = new Pen((Brush)FindResource("OcrLineBrush"), 1);
            linePen.Freeze();
            var highlightBrush = (Brush)FindResource("OcrHighlightBrush");
            foreach (var line in GetOcrLines())
            {
                var lineBounds = line.Bounds;
                if (lineBounds.IsEmpty)
                {
                    continue;
                }
                var lineRect = ToRect(lineBounds);
                dc.DrawRectangle(null, linePen, new Rect(lineRect.X + 0.5, lineRect.Y + 0.5, lineRect.Width, lineRect.Height));
                if (_mouseDown)
                {
                    // Highlight the words which are selected
                    if (!lineBounds.IntersectsWith(_captureRect))
                    {
                        continue;
                    }
                    foreach (var word in line.Words)
                    {
                        if (word.Bounds.IntersectsWith(_captureRect))
                        {
                            dc.DrawRectangle(highlightBrush, null, ToRect(word.Bounds));
                        }
                    }
                }
                else if (line == _hoveredLine)
                {
                    dc.DrawRectangle(highlightBrush, null, lineRect);
                }
            }
        }

        private void ShowCapturedCursor()
        {
            var cursor = _capture.Cursor;
            if (cursor == null || !_capture.CursorVisible)
            {
                CursorImage.Visibility = Visibility.Collapsed;
                return;
            }

            if (CursorImage.Source == null)
            {
                using var cursorBitmap = new System.Drawing.Bitmap(cursor.Size.Width, cursor.Size.Height, DrawingPixelFormat.Format32bppArgb);
                using (var graphics = System.Drawing.Graphics.FromImage(cursorBitmap))
                {
                    CursorHelper.DrawCursorOnGraphics(graphics, cursor, new NativePoint(0, 0));
                }
                CursorImage.Source = CreateBitmapSource(cursorBitmap);
                Canvas.SetLeft(CursorImage, _capture.CursorLocation.X);
                Canvas.SetTop(CursorImage, _capture.CursorLocation.Y);
            }
            CursorImage.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Text in pixels, the size in points scaled like the rest of the UI
        /// </summary>
        private FormattedText CreateText(string text, double points, Brush brush, bool bold = false)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, bold ? _boldLabelTypeface : _labelTypeface,
                points * 96 / 72 * _dpiScale, brush, 1.0);
        }

        /// <summary>
        /// The pixels of the capture at 96 DPI, so one unit is one pixel
        /// </summary>
        private static BitmapSource CreateBitmapSource(System.Drawing.Image image)
        {
            if (image is not System.Drawing.Bitmap bitmap)
            {
                using var copy = new System.Drawing.Bitmap(image);
                return CreateBitmapSource(copy);
            }

            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, DrawingPixelFormat.Format32bppPArgb);
            try
            {
                var bitmapSource = BitmapSource.Create(bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Pbgra32, null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        private static Point ToPoint(NativePoint point) => new Point(point.X, point.Y);

        private static Rect ToRect(NativeRect rect) => new Rect(rect.X, rect.Y, rect.Width, rect.Height);

        #endregion
    }
}
