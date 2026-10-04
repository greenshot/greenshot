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
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.UI.Capture.Tools;
using log4net;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;
using Point = System.Windows.Point;

namespace Greenshot.UI.Capture
{
    /// <summary>
    /// Lets the user select something on the frozen capture of the screen with one of the capture tools (region, window, text).
    /// Takes care of what the tools share: the cursor and its keys, the crosshair, the zoomer, the hotspots, the selection and its labels.
    /// Space switches between region and window, the shortcut key of a tool (T for text) to that tool, Escape cancels.
    /// </summary>
    public partial class CaptureWindow : Window, ICaptureToolHost
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
        private BitmapSource _screenImage;
        private readonly Typeface _labelTypeface;
        private readonly Typeface _boldLabelTypeface;

        // The built-in tools first, the region tool is the default, then the tools of the plugins
        private readonly RegionCaptureTool _regionTool = new RegionCaptureTool();
        private readonly WindowCaptureTool _windowTool = new WindowCaptureTool();
        private readonly IList<ICaptureTool> _tools;
        // The overlays of the plugins, each with its own layer above the tool layer
        private readonly IList<KeyValuePair<ICaptureOverlay, DrawingLayer>> _overlays = new List<KeyValuePair<ICaptureOverlay, DrawingLayer>>();

        private ICaptureTool _activeTool;
        private CaptureMode _usedCaptureMode;
        private List<WindowDetails> _windows = new List<WindowDetails>();
        private List<CaptureFormHotspot> _hotspots = new List<CaptureFormHotspot>();
        private CaptureFormHotspot _hoveredHotspot;
        private WindowDetails _windowUnderCursor;
        private WindowDetails _acceptedWindow;
        private NativeRect _captureRect = NativeRect.Empty;
        private NativeRect _selectionRect = NativeRect.Empty;
        // The panels of tools and overlays, by owner
        private readonly Dictionary<object, Panel> _panels = new Dictionary<object, Panel>();
        private CaptureToolStyle _toolStyle;

        /// <summary>
        /// A panel of a tool or overlay: its layer, where it is (the target while it moves) and what is drawn on it
        /// </summary>
        private sealed class Panel
        {
            public Panel(UIElement element)
            {
                Element = element;
                Element.RenderTransform = Position;
            }

            /// <summary>
            /// A DrawingLayer for drawn content, a Border around WPF content
            /// </summary>
            public UIElement Element { get; }
            public TranslateTransform Position { get; } = new TranslateTransform();
            public NativeSize Size { get; set; }
            public NativeRect Bounds { get; set; } = NativeRect.Empty;
        }
        private NativePoint _cursorPos;
        private NativePoint _previousMousePos;
        private FixMode _fixMode = FixMode.None;
        private readonly CaptureKeyRegistry _keys = new CaptureKeyRegistry();
        // The mouse button was pressed for the active tool, not on a hotspot
        private bool _toolMouseDown;
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
        public CaptureMode UsedCaptureMode => _usedCaptureMode;

        /// <summary>
        /// The selected window, or the top level window under the cursor
        /// </summary>
        public WindowDetails SelectedCaptureWindow => _acceptedWindow ?? _windowUnderCursor;

        /// <inheritdoc />
        public ICapture Capture => _capture;

        /// <inheritdoc />
        public NativeRect ScreenBounds => _screenBounds;

        /// <inheritdoc />
        public NativePoint CursorPosition => _cursorPos;

        /// <inheritdoc />
        public ICaptureTool ActiveTool => _activeTool;

        /// <inheritdoc />
        public NativeRect GetMonitorBounds() => DisplayInfo.GetBounds(User32Api.GetCursorLocation()).Offset(-_screenBounds.X, -_screenBounds.Y);

        /// <inheritdoc />
        public CaptureToolStyle ToolStyle => _toolStyle ??= new CaptureToolStyle(_dpiScale);

        /// <inheritdoc />
        public void ShowPanel(object owner, Size contentSize, Action<DrawingContext> drawContent)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }
            bool isNew = !(_panels.TryGetValue(owner, out var panel) && panel.Element is DrawingLayer);
            if (isNew)
            {
                panel = AddPanel(owner, new DrawingLayer());
            }

            double padding = ToolStyle.PanelPadding;
            panel.Size = new NativeSize((int)Math.Ceiling(contentSize.Width + 2 * padding), (int)Math.Ceiling(contentSize.Height + 2 * padding));
            using (var dc = ((DrawingLayer)panel.Element).Open())
            {
                ToolStyle.DrawPanel(dc, new Rect(0, 0, panel.Size.Width, panel.Size.Height));
                dc.PushTransform(new TranslateTransform(padding, padding));
                drawContent?.Invoke(dc);
                dc.Pop();
            }

            // A new panel appears where it belongs, a changed one may need another place
            PlacePanel(owner, panel, !isNew, animateResize: false);
        }

        /// <inheritdoc />
        public void ShowPanel(object owner, FrameworkElement content)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (_panels.TryGetValue(owner, out var existing) && existing.Element is Border existingBorder && existingBorder.Child == content)
            {
                // The same content: its bindings update it, only the size may have changed
                UpdatePanelSize(owner, existing);
                return;
            }

            var border = ToolStyle.CreatePanel(content);
            var panel = AddPanel(owner, border);
            // Bound values which change the size of the content (e.g. longer text) move the panel when needed
            border.SizeChanged += (sender, args) =>
            {
                if (_panels.TryGetValue(owner, out var current) && current == panel)
                {
                    UpdatePanelSize(owner, panel);
                }
            };
            UpdatePanelSize(owner, panel);
        }

        /// <summary>
        /// Measure the WPF content of a panel, which only lays out the panel itself, and place it for that size
        /// </summary>
        private void UpdatePanelSize(object owner, Panel panel)
        {
            var element = (FrameworkElement)panel.Element;
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desired = element.DesiredSize;
            bool isFirst = panel.Bounds.IsEmpty;
            // While shown, the panel only grows: changing values (e.g. 99 and 100) would otherwise make it wobble.
            // MinWidth and MinHeight are in device independent units, DesiredSize in pixels (the panel scales its content).
            double scale = ToolStyle.Scale(1);
            element.MinWidth = Math.Max(element.MinWidth, desired.Width / scale);
            element.MinHeight = Math.Max(element.MinHeight, desired.Height / scale);
            var size = new NativeSize((int)Math.Ceiling(desired.Width), (int)Math.Ceiling(desired.Height));
            if (!isFirst && size.Width <= panel.Size.Width && size.Height <= panel.Size.Height)
            {
                return;
            }
            size = new NativeSize(Math.Max(size.Width, panel.Size.Width), Math.Max(size.Height, panel.Size.Height));
            panel.Size = size;
            // Growing keeps the corner without animation, only a move to another corner slides
            PlacePanel(owner, panel, !isFirst, animateResize: false);
        }

        /// <summary>
        /// A new panel (replacing an older one of the owner), fading in
        /// </summary>
        private Panel AddPanel(object owner, UIElement element)
        {
            if (_panels.TryGetValue(owner, out var old))
            {
                PanelHost.Children.Remove(old.Element);
            }
            var panel = new Panel(element);
            PanelHost.Children.Add(element);
            _panels[owner] = panel;
            Fade(element, 0, 1, null);
            return panel;
        }

        /// <inheritdoc />
        public void HidePanel(object owner)
        {
            if (owner == null || !_panels.TryGetValue(owner, out var panel))
            {
                return;
            }
            // Released right away, the zoomer and the other panels may go there
            _panels.Remove(owner);
            Fade(panel.Element, panel.Element.Opacity, 0, () => PanelHost.Children.Remove(panel.Element));
        }

        /// <summary>
        /// Move every panel which is in the way of the cursor, the selection or the zoomer
        /// </summary>
        private void UpdatePanels()
        {
            foreach (var entry in _panels.ToList())
            {
                PlacePanel(entry.Key, entry.Value, true);
            }
        }

        private void PlacePanel(object owner, Panel panel, bool animate, bool animateResize = true)
        {
            var avoid = _panels.Where(other => !Equals(other.Key, owner)).Select(other => other.Value.Bounds).ToList();
            if (IsSelectionVisible)
            {
                avoid.Add(_selectionRect);
            }
            if (_zoomerShown && _zoomSize > 0)
            {
                avoid.Add(new NativeRect(_cursorPos.X + _zoomOffset.X, _cursorPos.Y + _zoomOffset.Y, _zoomSize, _zoomSize));
            }
            var bounds = PanelPlacement.Place(panel.Size, GetMonitorBounds(), _cursorPos, panel.Bounds, avoid);
            if (bounds.Equals(panel.Bounds))
            {
                return;
            }
            if (!animateResize && !panel.Bounds.IsEmpty && bounds.Equals(PanelPlacement.Resize(panel.Bounds, panel.Size, GetMonitorBounds())))
            {
                animate = false;
            }
            panel.Bounds = bounds;
            MovePanel(panel.Position, TranslateTransform.XProperty, bounds.X, animate);
            MovePanel(panel.Position, TranslateTransform.YProperty, bounds.Y, animate);
        }

        private void MovePanel(TranslateTransform position, DependencyProperty property, double to, bool animate)
        {
            if (!animate || _isRemoteSession)
            {
                position.BeginAnimation(property, null);
                position.SetValue(property, to);
                return;
            }
            var move = ((DoubleAnimation)FindResource("PanelMoveAnimation")).Clone();
            move.To = to;
            position.BeginAnimation(property, move);
        }

        /// <summary>
        /// Fade a panel in or out, at once in a remote session
        /// </summary>
        private void Fade(UIElement element, double from, double to, Action completed)
        {
            if (_isRemoteSession)
            {
                element.BeginAnimation(OpacityProperty, null);
                element.Opacity = to;
                completed?.Invoke();
                return;
            }
            var fade = ((DoubleAnimation)FindResource("PanelFadeAnimation")).Clone();
            fade.From = from;
            fade.To = to;
            if (completed != null)
            {
                fade.Completed += (sender, args) => completed();
            }
            element.BeginAnimation(OpacityProperty, fade);
        }

        /// <inheritdoc />
        public Color GetPixelColor(NativePoint location)
        {
            if (location.X < 0 || location.Y < 0 || location.X >= _screenImage.PixelWidth || location.Y >= _screenImage.PixelHeight)
            {
                return Colors.Transparent;
            }
            // Pbgra32, premultiplied: the capture of the screen is opaque, so the color is the pixel as it is
            var pixel = new byte[4];
            _screenImage.CopyPixels(new Int32Rect(location.X, location.Y, 1, 1), pixel, 4, 0);
            return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
        }

        /// <inheritdoc />
        public IReadOnlyList<WindowDetails> Windows => _windows;

        /// <summary>
        /// Create the window for the capture of the screen
        /// </summary>
        /// <param name="capture">ICapture of the whole screen</param>
        /// <param name="windows">The windows to snap to, in z-order</param>
        public CaptureWindow(ICapture capture, IList<WindowDetails> windows)
        {
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _screenBounds = capture.ScreenBounds;
            _tools = CreateTools();
            var initialMode = capture.CaptureDetails.CaptureMode;
            _activeTool = _tools.FirstOrDefault(tool => tool.Mode == initialMode) ?? _tools[0];
            _usedCaptureMode = _activeTool.Mode;

            InitializeComponent();
            // The DPI of the primary monitor until the window is placed, see ApplyDpiScale
            _dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
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
            _screenImage = CreateBitmapSource(capture.Image);
            ScreenImage.Source = _screenImage;
            ZoomBrush.ImageSource = _screenImage;
            ShowCapturedCursor();
            DrawCrosshairLines();

            _cursorPos = WindowCapture.GetCursorLocationRelativeToScreenBounds();
            _previousMousePos = User32Api.GetCursorLocation();
            SetSelection(new Rect(ToPoint(_cursorPos), new Size(0, 0)));
            // The window's keys first, then the built-in tools, then plugins: the built-in keys win a conflict
            RegisterWindowKeys();
            AttachTools();
            _activeTool.Activate(this);
            CreateOverlays();

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

        /// <inheritdoc />
        public void Accept(NativeRect rect, WindowDetails window = null)
        {
            _captureRect = rect;
            _acceptedWindow = window;
            _usedCaptureMode = _activeTool.Mode;
            DialogResult = true;
        }

        /// <summary>
        /// The built-in tools and those of the registered ICaptureToolProviders (plugins)
        /// </summary>
        private IList<ICaptureTool> CreateTools()
        {
            var tools = new List<ICaptureTool> { _regionTool, _windowTool, new TextCaptureTool() };
            foreach (var provider in SimpleServiceProvider.Current.GetAllInstances<ICaptureToolProvider>())
            {
                try
                {
                    tools.AddRange(provider.CreateTools()?.Where(tool => tool != null) ?? Enumerable.Empty<ICaptureTool>());
                }
                catch (Exception ex)
                {
                    Log.Error($"Error creating the capture tools of {provider.GetType().FullName}", ex);
                }
            }
            return tools;
        }

        /// <summary>
        /// Every tool registers its keys, a tool which fails (e.g. a key which is already used) is logged and stays available with the keys it got
        /// </summary>
        private void AttachTools()
        {
            foreach (var tool in _tools)
            {
                try
                {
                    tool.Attach(this);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error attaching the capture tool {tool.GetType().FullName}", ex);
                }
            }
        }

        /// <summary>
        /// The keys of the window itself, for every tool
        /// </summary>
        private void RegisterWindowKeys()
        {
            RegisterKey(this, Key.Space, ModifierKeys.None, () => Texts.Core.CaptureKeyRegionWindow,
                // Region to window, every other tool back to region
                () => SwitchTool(_activeTool == _regionTool ? _windowTool : _regionTool));
            foreach (var (key, dx, dy) in new[] { (Key.Up, 0, -1), (Key.Down, 0, 1), (Key.Left, -1, 0), (Key.Right, 1, 0) })
            {
                RegisterKey(this, key, ModifierKeys.None, () => Texts.Core.CaptureKeyMove, () => MoveCursor(dx, dy));
            }
            foreach (var (key, dx, dy) in new[] { (Key.Up, 0, -10), (Key.Down, 0, 10), (Key.Left, -10, 0), (Key.Right, 10, 0) })
            {
                RegisterKey(this, key, ModifierKeys.Control, () => Texts.Core.CaptureKeyMoveFast, () => MoveCursor(dx, dy));
            }
            foreach (var key in new[] { Key.LeftShift, Key.RightShift })
            {
                RegisterKey(this, key, ModifierKeys.None, () => Texts.Core.CaptureKeyFixDirection, () =>
                {
                    // Fix mode: keep the selection to one direction, until Shift is released
                    if (_fixMode == FixMode.None)
                    {
                        _fixMode = FixMode.Initiated;
                    }
                });
            }
            RegisterKey(this, Key.M, ModifierKeys.None, () => Texts.Core.CaptureKeyMouseCursor, () =>
            {
                _capture.CursorVisible = !_capture.CursorVisible;
                ShowCapturedCursor();
            });
            RegisterKey(this, Key.Z, ModifierKeys.None, () => Texts.Core.CaptureKeyZoomer, () =>
            {
                if (_activeTool.ShowsZoomer)
                {
                    Conf.ZoomerEnabled = !Conf.ZoomerEnabled;
                    UpdateZoomerVisibility();
                }
            });
            RegisterKey(this, Key.Escape, ModifierKeys.None, () => Texts.Core.CaptureKeyCancel, Cancel);
        }

        private static void MoveCursor(int dx, int dy)
        {
            var cursor = System.Windows.Forms.Cursor.Position;
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(cursor.X + dx, cursor.Y + dy);
        }

        /// <inheritdoc />
        public CaptureKeyBinding RegisterKey(object owner, Key key, ModifierKeys modifiers, Func<string> description, Action execute) =>
            _keys.Register(new CaptureKeyBinding(owner, null, key, modifiers, description, execute));

        /// <inheritdoc />
        public CaptureKeyBinding RegisterToolKey(ICaptureTool tool, Key key, ModifierKeys modifiers, Func<string> description, Action execute) =>
            _keys.Register(new CaptureKeyBinding(tool, tool ?? throw new ArgumentNullException(nameof(tool)), key, modifiers, description, execute));

        /// <inheritdoc />
        public IReadOnlyList<CaptureKeyBinding> KeyBindings => _keys.Bindings;

        /// <inheritdoc />
        public void ActivateTool(ICaptureTool tool) => SwitchTool(tool);

        /// <summary>
        /// The built-in overlays (the help) and those of the registered ICaptureOverlayProviders (plugins), each gets a layer above the tool layer
        /// </summary>
        private void CreateOverlays()
        {
            int layerIndex = Root.Children.IndexOf(ToolLayer) + 1;
            void Add(ICaptureOverlay overlay)
            {
                if (overlay == null)
                {
                    return;
                }
                var layer = new DrawingLayer();
                Root.Children.Insert(layerIndex++, layer);
                _overlays.Add(new KeyValuePair<ICaptureOverlay, DrawingLayer>(overlay, layer));
                try
                {
                    overlay.Attach(this);
                }
                catch (Exception ex)
                {
                    // E.g. a key which is already used, the overlay keeps what it registered before
                    Log.Error($"Error attaching the capture overlay {overlay.GetType().FullName}", ex);
                }
            }

            Add(new HelpOverlay());
            foreach (var provider in SimpleServiceProvider.Current.GetAllInstances<ICaptureOverlayProvider>())
            {
                try
                {
                    foreach (var overlay in provider.CreateOverlays() ?? Enumerable.Empty<ICaptureOverlay>())
                    {
                        Add(overlay);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error creating the capture overlays of {provider.GetType().FullName}", ex);
                }
            }
        }

        /// <summary>
        /// Call all overlays, an error in one of them is logged and doesn't stop the capture
        /// </summary>
        private void ForEachOverlay(Action<ICaptureOverlay> action)
        {
            foreach (var entry in _overlays)
            {
                try
                {
                    action(entry.Key);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in the capture overlay {entry.Key.GetType().FullName}", ex);
                }
            }
        }

        /// <summary>
        /// Make another tool the active one
        /// </summary>
        private void SwitchTool(ICaptureTool tool)
        {
            if (tool == null || tool == _activeTool)
            {
                return;
            }
            _activeTool.Deactivate();
            _activeTool = tool;
            _usedCaptureMode = tool.Mode;
            tool.Activate(this);
            ForEachOverlay(overlay => overlay.OnToolChanged());
            UpdateZoomerVisibility();
            UpdateSelection();
            Redraw();
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
            Redraw();
            ForEachOverlay(Redraw);
        }

        /// <summary>
        /// WPF scales everything in the window with the DPI of the window, undo that so one unit is one pixel
        /// </summary>
        private void ApplyDpiScale(DpiScale dpi)
        {
            if (Math.Abs(_dpiScale - dpi.DpiScaleX) > 0.001)
            {
                // The style scales with the DPI
                _toolStyle = null;
            }
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
                Redraw();
                ForEachOverlay(Redraw);
            }).FireAndLog("Capture window features changed", Log);
        }

        #endregion

        #region input

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.LeftShift or Key.RightShift)
            {
                _fixMode = FixMode.None;
            }
        }

        /// <summary>
        /// A key does what its registration says, see RegisterKey and RegisterToolKey; keys nobody registered do nothing
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // With Alt the key is reported as Key.System
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var binding = _keys.Find(key, Keyboard.Modifiers, _activeTool);
            if (binding == null)
            {
                return;
            }
            e.Handled = true;
            try
            {
                binding.Execute();
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling the capture key {binding.KeyText} of {binding.Owner.GetType().FullName}", ex);
            }
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
            CaptureMouse();
            _toolMouseDown = true;
            _activeTool.OnMouseDown();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            if (!_toolMouseDown)
            {
                return;
            }
            _toolMouseDown = false;
            _activeTool.OnMouseUp();
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

        #endregion

        #region selection

        /// <summary>
        /// Update everything which depends on the cursor, called for every mouse move
        /// </summary>
        private void UpdateSelection()
        {
            _cursorPos = GetCursorPosition();

            var hoveredHotspot = FindHotspot(_cursorPos);
            Cursor = hoveredHotspot != null ? Cursors.Hand : Cursors.Cross;
            ToolTip = hoveredHotspot?.ToolTipText;
            bool redrawFeatures = hoveredHotspot != _hoveredHotspot;
            _hoveredHotspot = hoveredHotspot;

            UpdateWindowUnderCursor();
            _activeTool.OnMouseMove();
            ForEachOverlay(overlay => overlay.OnMouseMove());
            UpdatePanels();
            UpdateCrosshair(_activeTool.ShowsCrosshair);
            UpdateZoomer();

            if (redrawFeatures)
            {
                DrawFeatures();
            }
        }

        /// <summary>
        /// The top level window under the cursor gives the capture its title
        /// </summary>
        private void UpdateWindowUnderCursor()
        {
            var window = FindWindowUnderCursor(false);
            if (window == null || window.Equals(_windowUnderCursor))
            {
                return;
            }
            _windowUnderCursor = window;
            _capture.CaptureDetails.Title = window.Text;
            _capture.CaptureDetails.AddMetaData("windowtitle", window.Text);
        }

        /// <inheritdoc />
        public WindowDetails FindWindowUnderCursor(bool includeChildren)
        {
            // In screen coordinates, as the windows are
            var cursorPosition = User32Api.GetCursorLocation();
            var window = _windows.FirstOrDefault(w => w.Contains(cursorPosition));
            return includeChildren ? window?.FindChildUnderPoint(cursorPosition) : window;
        }

        /// <inheritdoc />
        public void ShowSelection(NativeRect rect, bool animate = false, Action completed = null)
        {
            _selectionRect = rect;
            SelectionPath.Visibility = Visibility.Visible;
            if (animate)
            {
                AnimateSelection(ToRect(rect), completed);
            }
            else
            {
                SetSelection(ToRect(rect));
                completed?.Invoke();
            }
        }

        /// <inheritdoc />
        public void HideSelection()
        {
            SelectionPath.Visibility = Visibility.Collapsed;
        }

        /// <inheritdoc />
        public bool IsSelectionVisible => SelectionPath.Visibility == Visibility.Visible;

        /// <inheritdoc />
        public bool IsSelectionAnimating => _selectionClock != null;

        /// <inheritdoc />
        public void ClearLabels() => LabelLayer.Clear();

        /// <inheritdoc />
        public void Redraw()
        {
            using var dc = ToolLayer.Open();
            _activeTool.Draw(dc);
        }

        /// <inheritdoc />
        public void Redraw(ICaptureOverlay overlay)
        {
            var layer = _overlays.FirstOrDefault(entry => entry.Key == overlay).Value;
            if (layer == null)
            {
                return;
            }
            using var dc = layer.Open();
            overlay.Draw(dc);
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

        /// <summary>
        /// Show the selection there, without animation
        /// </summary>
        private void SetSelection(Rect rect)
        {
            _selectionClock = null;
            SelectionGeometry.BeginAnimation(RectangleGeometry.RectProperty, null);
            SelectionGeometry.Rect = rect;
        }

        /// <inheritdoc />
        public void ShowLabels(NativeRect rect, NativeSize size, bool fadeIn = false, string debugText = null)
        {
            if (rect.IsEmpty)
            {
                LabelLayer.Clear();
                return;
            }

            int width = size.Width;
            int height = size.Height;
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

                    if (!string.IsNullOrEmpty(debugText))
                    {
                        dc.DrawText(CreateText(debugText, 12, (Brush)FindResource("DebugTextBrush")), new Point(rect.X, rect.Y));
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
            bool show = Conf.ZoomerEnabled && _activeTool.ShowsZoomer;
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

            var screenBounds = GetMonitorBounds();
            int zoomSize = ZoomerPlacement.GetSize(screenBounds);
            if (zoomSize != _zoomSize)
            {
                ResizeZoomer(zoomSize);
            }

            var offset = ZoomerPlacement.GetOffset(_cursorPos, _zoomOffset, zoomSize, screenBounds, GetZoomerAvoids());
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

        /// <summary>
        /// The zoomer stays off the selection (not while it animates) and the panels of tools and overlays
        /// </summary>
        private List<NativeRect> GetZoomerAvoids()
        {
            var avoid = _panels.Values.Select(panel => panel.Bounds).ToList();
            if (IsSelectionVisible && !IsSelectionAnimating)
            {
                avoid.Add(_selectionRect);
            }
            return avoid;
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
        /// Draw the hotspots of the detected features
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
