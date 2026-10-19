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
using System.Windows.Threading;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Base.Capturing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Native;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes.Sources;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Capturing.Controls;
using Greenshot.Capturing.Overlays;
using Greenshot.Capturing.Tools;
using log4net;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;
using Point = System.Windows.Point;

namespace Greenshot.Capturing.Views
{
    /// <summary>
    /// Lets the user select something on the frozen capture of the screen with one of the capture tools (region, window, text),
    /// or, for a capture without an image, on the live screen (see ApplyLiveMode).
    /// Takes care of what the tools share: the cursor and its keys, the crosshair, the zoomer, the hotspots, the selection and its labels.
    /// Space switches between region and window, the shortcut key of a tool (T for text) to that tool, L between the frozen and the live screen,
    /// Escape cancels.
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

        private ICapture _capture;
        private NativeRect _screenBounds;
        // Remote desktop: no animations and no crosshair, every repaint of the screen costs bandwidth (OptimizeForRDP / DisableRDPOptimizing as before)
        private readonly bool _isRemoteSession = !Conf.DisableRDPOptimizing && (Conf.OptimizeForRDP || SystemParameters.IsRemoteSession);
        // No animations in a remote session, nor when "Animation effects" are off in Windows (motion can make people sick)
        private readonly bool _noAnimations;
        // What the screen reader said last, see Announce
        private string _announced;
        private DispatcherTimer _announceTimer;
        // A screen reader key (Insert or Caps Lock) is held on the live screen: its keys belong to the screen reader, see OnLiveKey
        private volatile bool _screenReaderKeyDown;
        private BitmapSource _screenImage;
        // The screen isn't frozen: the window is see-through, takes no focus and is left out of every capture of the screen, see ApplyLiveMode
        private bool _live;
        // L can switch between the frozen and the live screen
        private bool _liveAllowed;
        private bool _switching;
        private IntPtr _previousForeground;
        private bool _liveApplied;
        private IDisposable _keyboardSubscription;
        private SafeRegionHandle _transparentRegion;
        // The pixels around the cursor for the zoomer, copied from the screen
        private WriteableBitmap _livePixels;
        private System.Drawing.Bitmap _livePixelsCopy;
        private DispatcherTimer _livePixelsTimer;
        // The preview of the selection, told about every change of the selection (also those from keys, without a mouse move)
        private SelectionPreviewOverlay _selectionPreview;
        // What the zoomer shows in its place: the magnifier (square) or the preview, which was drawn for this area and size
        private NativeSize _zoomerSlotSize = NativeSize.Empty;
        private NativeRect _zoomerPreviewArea = NativeRect.Empty;
        private NativeSize _zoomerPreviewSize = NativeSize.Empty;
        private readonly Typeface _labelTypeface;
        private readonly Typeface _boldLabelTypeface;

        // The built-in tools first, the region tool is the default, then the tools of the plugins
        private readonly RegionCaptureTool _regionTool = new RegionCaptureTool();
        private readonly WindowCaptureTool _windowTool = new WindowCaptureTool();
        private IList<ICaptureTool> _tools = new List<ICaptureTool>();
        // The overlays of the plugins, each with its own layer above the tool layer
        private readonly IList<KeyValuePair<ICaptureOverlay, DrawingLayer>> _overlays = new List<KeyValuePair<ICaptureOverlay, DrawingLayer>>();

        private ICaptureTool _activeTool;
        private CaptureMode _usedCaptureMode;
        private List<IInteropWindow> _windows = new List<IInteropWindow>();
        private List<CaptureFormHotspot> _hotspots = new List<CaptureFormHotspot>();
        private CaptureFormHotspot _hoveredHotspot;
        private IInteropWindow _windowUnderCursor;
        private IInteropWindow _acceptedWindow;
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
        // Prepare: the window and its handle are created before the capture, it is shown (without ShowDialog) when it has the capture
        private bool _prepared;
        private bool? _modelessResult;
        private bool _handleCreated;
        private bool _closed;
        private bool _lostFocus;
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
        /// The tool the selection ended in
        /// </summary>
        public ICaptureTool UsedTool => _activeTool;

        /// <summary>
        /// The selected window, or the top level window under the cursor
        /// </summary>
        public IInteropWindow SelectedCaptureWindow => _acceptedWindow ?? _windowUnderCursor;

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

            // Shown again after HidePanel: the old panel may still be fading out with the content in it
            if (content.Parent is Decorator oldPanel)
            {
                oldPanel.Child = null;
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
            // The selection only once it stopped changing: the window tool changes it with every window under the cursor.
            // The zoomer is not avoided, it avoids the panels itself and moves with the cursor.
            var avoidLoosely = IsSelectionVisible && IsSelectionSettled ? new[] { _selectionRect } : Array.Empty<NativeRect>();
            var bounds = PanelPlacement.Place(panel.Size, GetMonitorBounds(), _cursorPos, panel.Bounds, avoid, avoidLoosely);
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
            if (!animate || _noAnimations)
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
            if (_noAnimations)
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
            if (_live)
            {
                return GetLivePixelColor(location);
            }
            if (_screenImage == null || location.X < 0 || location.Y < 0 || location.X >= _screenImage.PixelWidth || location.Y >= _screenImage.PixelHeight)
            {
                return Colors.Transparent;
            }
            // Bgr32: the capture of the screen is opaque
            var pixel = new byte[4];
            _screenImage.CopyPixels(new Int32Rect(location.X, location.Y, 1, 1), pixel, 4, 0);
            return Color.FromRgb(pixel[2], pixel[1], pixel[0]);
        }

        /// <inheritdoc />
        public IReadOnlyList<IInteropWindow> Windows => _windows;

        /// <summary>
        /// Create the window for the capture of the screen
        /// </summary>
        /// <param name="capture">ICapture of the whole screen</param>
        /// <param name="windows">The windows to snap to, in z-order</param>
        /// <param name="initialTool">Id of the tool to start with, null: the tool of the capture mode</param>
        public CaptureWindow(ICapture capture, IList<IInteropWindow> windows, string initialTool = null) : this((capture ?? throw new ArgumentNullException(nameof(capture))).ScreenBounds)
        {
            SetCapture(capture, windows, initialTool);
        }

        /// <summary>
        /// Create the window before the capture exists, see Prepare and SetCapture: the window is built while the screen is captured
        /// </summary>
        /// <param name="screenBounds">NativeRect with the bounds of the whole screen</param>
        public CaptureWindow(NativeRect screenBounds)
        {
            _screenBounds = screenBounds;

            InitializeComponent();
            _noAnimations = _isRemoteSession || !SystemParameters.ClientAreaAnimation;
            if (ThemeManager.Instance.IsHighContrast)
            {
                UseHighContrastColors();
            }
            // The DPI of the primary monitor until the window is placed, see ApplyDpiScale
            _dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            var labelFont = (FontFamily)FindResource("LabelFontFamily");
            _labelTypeface = new Typeface(labelFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _boldLabelTypeface = new Typeface(labelFont, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            Root.Width = _screenBounds.Width;
            Root.Height = _screenBounds.Height;

            SourceInitialized += OnSourceInitialized;
            ContentRendered += OnContentRendered;
            Closed += OnClosed;
        }

        /// <summary>
        /// True when the window was prepared and can still get its capture
        /// </summary>
        public bool IsPrepared => _prepared && !_closed && _capture == null;

        /// <summary>
        /// Create the window and its handle without showing it, SetCapture shows it. An invisible window is in no capture and takes no focus.
        /// Showing it before it has the capture doesn't help: WPF doesn't render it earlier, and uncloaking a window which rendered without
        /// the capture shows a black frame.
        /// </summary>
        public void Prepare()
        {
            _prepared = true;
            new WindowInteropHelper(this).EnsureHandle();
        }

        /// <summary>
        /// Give the window its capture, the window becomes visible when it rendered it
        /// </summary>
        /// <param name="capture">ICapture of the whole screen</param>
        /// <param name="windows">The windows to snap to, in z-order</param>
        /// <param name="initialTool">Id of the tool to start with, null: the tool of the capture mode</param>
        public void SetCapture(ICapture capture, IList<IInteropWindow> windows, string initialTool = null)
        {
            if (_capture != null)
            {
                throw new InvalidOperationException("The capture window already has a capture.");
            }
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            if (capture.ScreenBounds != _screenBounds)
            {
                // The screen changed since the window was created
                _screenBounds = capture.ScreenBounds;
                Root.Width = _screenBounds.Width;
                Root.Height = _screenBounds.Height;
                if (_handleCreated)
                {
                    PlaceWindow();
                }
            }
            _live = capture.Image == null;
            _liveAllowed = LiveScreenCaptureSource.IsSupported && !capture.CaptureDetails.MetaData.ContainsKey(LiveScreenCaptureSource.FrozenOnlyKey);
            // Not shown yet: the application below has the focus, it gets it back when L switches to the live screen
            _previousForeground = User32Api.GetForegroundWindow();
            _tools = CreateTools();
            var initialMode = capture.CaptureDetails.CaptureMode;
            _activeTool = _tools.FirstOrDefault(tool => string.Equals(tool.Id, initialTool, StringComparison.OrdinalIgnoreCase))
                          ?? _tools.FirstOrDefault(tool => tool.Mode == initialMode) ?? _tools[0];
            if (_live && _activeTool is TextCaptureTool)
            {
                // The text needs a capture of the screen, T freezes it later
                _activeTool = _regionTool;
            }
            // Someone who uses a screen reader can't see a region, but hears which window is selected:
            // start with window capture and the window on top already selected, Enter captures it, Space still switches to the region
            bool selectWindowForScreenReader = initialTool == null && _activeTool == _regionTool && ScreenReader.IsRunning;
            if (selectWindowForScreenReader)
            {
                _activeTool = _windowTool;
            }
            _usedCaptureMode = _activeTool.Mode;

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

            if (!_live)
            {
                _screenImage = CreateBitmapSource(capture.Image, true);
                ScreenImage.Source = _screenImage;
                ZoomBrush.ImageSource = _screenImage;
            }
            else if (_handleCreated)
            {
                ShowLive(true);
            }
            ShowCapturedCursor();
            DrawCrosshairLines();

            _cursorPos = WindowCapture.GetCursorLocationRelativeToScreenBounds();
            _previousMousePos = User32Api.GetCursorLocation();
            SetSelection(new Rect(ToPoint(_cursorPos), new Size(0, 0)));
            // The window's keys first, then the built-in tools, then plugins: the built-in keys win a conflict
            RegisterWindowKeys();
            AttachTools();
            _activeTool.Activate(this);
            if (selectWindowForScreenReader)
            {
                _windowTool.SelectFirstWindow();
            }
            CreateOverlays();

            _capture.CaptureDetails.FeaturesChanged += OnFeaturesChanged;
            RebuildFeatureHotspots();

            // Preview: the arrow keys would otherwise be taken by the keyboard navigation
            PreviewKeyDown += OnKeyDown;
            PreviewKeyUp += OnKeyUp;
            MouseMove += (sender, args) =>
            {
                // Another window took the focus (e.g. Alt+Tab, a dialog of another application) while the capture stays on top:
                // the next mouse move takes it back once, so the keys work again
                if (_lostFocus && !_live)
                {
                    _lostFocus = false;
                    WindowHelper.ToForeground(new WindowInteropHelper(this).Handle);
                }
                UpdateSelection();
            };
            Deactivated += (sender, args) => _lostFocus = !_closed;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            if (_handleCreated)
            {
                PrepareContent();
            }
            if (_prepared)
            {
                ShowActivated = !_live;
                Show();
            }
        }

        /// <summary>
        /// For a prepared window: wait, like ShowDialog, until the window closed
        /// </summary>
        /// <returns>true when something was selected</returns>
        public bool? WaitUntilClosed()
        {
            if (!_prepared)
            {
                return ShowDialog();
            }
            if (!_closed)
            {
                var frame = new DispatcherFrame();
                Closed += (sender, args) => frame.Continue = false;
                Dispatcher.PushFrame(frame);
            }
            return _modelessResult;
        }

        private void Finish(bool selected)
        {
            if (_prepared)
            {
                _modelessResult = selected;
                Close();
            }
            else
            {
                DialogResult = selected;
            }
        }

        /// <summary>
        /// Close the window without a selection
        /// </summary>
        public void Cancel()
        {
            if (IsLoaded)
            {
                Finish(false);
            }
            else
            {
                Close();
            }
        }

        /// <inheritdoc />
        public void Accept(NativeRect rect, IInteropWindow window = null)
        {
            _captureRect = rect;
            _acceptedWindow = window;
            _usedCaptureMode = _activeTool.Mode;
            Finish(true);
        }

        /// <summary>
        /// The built-in tools and those of the registered ICaptureToolProviders (plugins)
        /// </summary>
        private IList<ICaptureTool> CreateTools()
        {
            var tools = new List<ICaptureTool> { _regionTool, _windowTool };
            // The text of the selection is recognized on the capture of the screen, on the live screen T freezes it first (see SwitchTool)
            tools.Add(new TextCaptureTool());
            tools.Add(new FixedSizeCaptureTool());
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
            // Moving the cursor by a pixel and the zoomer only make sense for tools which select pixels,
            // not e.g. for the window tool: those tools show the zoomer
            static bool SelectsPixels(ICaptureTool tool) => tool?.ShowsZoomer == true;
            CaptureKeyBinding RegisterPixelKey(Key key, ModifierKeys modifiers, Func<string> description, Action execute) =>
                _keys.Register(new CaptureKeyBinding(this, null, key, modifiers, description, execute, SelectsPixels));

            RegisterKey(this, Key.Space, ModifierKeys.None, () => Texts.Core.CaptureKeyRegionWindow,
                // Region to window, every other tool back to region
                () => SwitchTool(_activeTool == _regionTool ? _windowTool : _regionTool));
            foreach (var (key, dx, dy) in new[] { (Key.Up, 0, -1), (Key.Down, 0, 1), (Key.Left, -1, 0), (Key.Right, 1, 0) })
            {
                RegisterPixelKey(key, ModifierKeys.None, () => Texts.Core.CaptureKeyMove, () => MoveCursor(dx, dy));
            }
            foreach (var (key, dx, dy) in new[] { (Key.Up, 0, -10), (Key.Down, 0, 10), (Key.Left, -10, 0), (Key.Right, 10, 0) })
            {
                RegisterPixelKey(key, ModifierKeys.Control, () => Texts.Core.CaptureKeyMoveFast, () => MoveCursor(dx, dy));
            }
            // Fixing the direction is for dragging a rectangle (the region and the text tool), not for an area which follows the cursor
            foreach (var key in new[] { Key.LeftShift, Key.RightShift })
            {
                _keys.Register(new CaptureKeyBinding(this, null, key, ModifierKeys.None, () => Texts.Core.CaptureKeyFixDirection, () =>
                {
                    // Fix mode: keep the selection to one direction, until Shift is released
                    if (_fixMode == FixMode.None)
                    {
                        _fixMode = FixMode.Initiated;
                    }
                }, tool => tool is RegionCaptureTool));
            }
            RegisterKey(this, Key.M, ModifierKeys.None, () => Texts.Core.CaptureKeyMouseCursor, () =>
            {
                _capture.CursorVisible = !_capture.CursorVisible;
                ShowCapturedCursor();
            });
            RegisterPixelKey(Key.Z, ModifierKeys.None, () => Texts.Core.CaptureKeyZoomer, () =>
            {
                Conf.ZoomerEnabled = !Conf.ZoomerEnabled;
                UpdateZoomerVisibility();
            });
            if (_liveAllowed)
            {
                RegisterKey(this, Key.L, ModifierKeys.None, () => Texts.Core.CaptureKeyLive, ToggleLive);
            }
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
            Add(new InfoOverlay());
            // P can put the preview in the place of the zoomer, which belongs to the window
            _selectionPreview = new SelectionPreviewOverlay(_screenImage, () => _zoomerShown, UpdateZoomer);
            Add(_selectionPreview);
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
            if (_live && tool is TextCaptureTool)
            {
                // The text is recognized on a capture of the screen
                if (_liveAllowed)
                {
                    FreezeAsync(tool).FireAndLog("Freeze the screen for the text tool", Log);
                }
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
            AnnounceTool();
        }

        /// <summary>
        /// Cover the whole virtual screen, in screen pixels, and map the pixels of the capture 1:1 to the screen
        /// </summary>
        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            // Make sure we never capture the capture window
            WindowHelper.RegisterIgnoreHandle(handle);
            HwndSource.FromHwnd(handle)?.AddHook(WndProc);
            // Browsers and Electron apps treat a window covered by an opaque window like a minimized one and don't build their
            // UI Automation content then, which the window tool needs for its areas. Chromium doesn't count tool windows as covering.
            var exStyle = (ExtendedWindowStyleFlags)User32Api.GetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE);
            User32Api.SetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)(exStyle | ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW)));
            PlaceWindow();

            ApplyDpiScale(VisualTreeHelper.GetDpi(this));
            _handleCreated = true;
            if (_capture != null)
            {
                if (_live)
                {
                    ShowLive(true);
                }
                PrepareContent();
            }
        }

        /// <summary>
        /// The live screen: the window is see-through but still gets every click (it isn't a layered window, so DWM composes its alpha without
        /// hit testing it), it doesn't take the focus from the application below, and Windows leaves it out of every capture of the screen
        /// (also the one of the selection, the pixels of the zoomer and a new frozen capture), so it never has to be hidden.
        /// Done once, the frozen screen covers the window with an opaque image.
        /// </summary>
        private void ApplyLiveMode()
        {
            if (_liveApplied)
            {
                return;
            }
            _liveApplied = true;
            var handle = new WindowInteropHelper(this).Handle;
            if (!User32Api.SetWindowDisplayAffinity(handle, WindowDisplayAffinity.ExcludeFromCapture))
            {
                Log.Warn("Couldn't leave the capture window out of the capture of the screen, it may show in the capture.");
            }

            // WPF renders with alpha, DWM uses it for a window with "blur behind" on an empty region: Windows 8 and newer don't blur, only blend
            var hwndSource = HwndSource.FromHwnd(handle);
            if (hwndSource?.CompositionTarget != null)
            {
                hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
            }
            _transparentRegion = SafeRegionHandle.CreateRectRgn(0, 0, -1, -1);
            var blurBehind = new DwmBlurBehind
            {
                Enable = true,
                BlurRegion = _transparentRegion.DangerousGetHandle()
            };
            var result = DwmApi.DwmEnableBlurBehindWindow(handle, ref blurBehind);
            if (result.Failed())
            {
                Log.Warn($"Couldn't make the capture window see-through: {result}");
            }

            _livePixels = new WriteableBitmap(ZoomSourceSize, ZoomSourceSize, 96, 96, PixelFormats.Bgr32, null);
            // The zoomer follows what moves on the screen, also while the mouse doesn't
            _livePixelsTimer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher) { Interval = TimeSpan.FromMilliseconds(50) };
            _livePixelsTimer.Tick += (sender, args) =>
            {
                if (_zoomerShown)
                {
                    CopyLivePixels();
                }
            };
        }

        /// <summary>
        /// Show the live or the frozen screen, see ApplyLiveMode
        /// </summary>
        private void ShowLive(bool live)
        {
            ApplyLiveMode();
            var handle = new WindowInteropHelper(this).Handle;
            var exStyle = (ExtendedWindowStyleFlags)User32Api.GetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE);
            exStyle = live ? exStyle | ExtendedWindowStyleFlags.WS_EX_NOACTIVATE : exStyle & ~ExtendedWindowStyleFlags.WS_EX_NOACTIVATE;
            User32Api.SetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)exStyle));
            Background = live ? Brushes.Transparent : Brushes.Black;
            ScreenImage.Source = live ? null : _screenImage;
            ZoomBrush.ImageSource = live ? _livePixels : _screenImage;
            _selectionPreview?.SetImage(live ? null : _screenImage);
            if (live)
            {
                _livePixelsTimer.Start();
                // The window doesn't get the focus, the keys come from the keyboard hook
                _keyboardSubscription ??= KeyboardHook.KeyboardEvents.Subscribe(OnLiveKey);
            }
            else
            {
                _livePixelsTimer.Stop();
                _keyboardSubscription?.Dispose();
                _keyboardSubscription = null;
            }
        }

        /// <summary>
        /// L: switch between the frozen and the live screen
        /// </summary>
        private void ToggleLive()
        {
            if (_switching)
            {
                return;
            }
            if (_live)
            {
                FreezeAsync(null).FireAndLog("Freeze the screen", Log);
            }
            else
            {
                GoLive();
            }
        }

        /// <summary>
        /// From the frozen to the live screen: the capture is thrown away, the selection step captures the selection when it is done
        /// </summary>
        private void GoLive()
        {
            if (_activeTool is TextCaptureTool)
            {
                SwitchTool(_regionTool);
            }
            _live = true;
            // Disposes the capture of the whole screen
            _capture.Image = null;
            _screenImage = null;
            ImageFeatures.Remove(_capture.CaptureDetails);
            bool hadFocus = IsActive;
            ShowLive(true);
            if (hadFocus && _previousForeground != IntPtr.Zero)
            {
                WindowHelper.ToForeground(_previousForeground);
            }
            RefreshCapture();
        }

        /// <summary>
        /// From the live to the frozen screen: capture the whole screen again, the window isn't in it (see ApplyLiveMode)
        /// </summary>
        /// <param name="tool">The tool to switch to when frozen, null: keep the tool</param>
        private async Task FreezeAsync(ICaptureTool tool)
        {
            if (_switching)
            {
                return;
            }
            _switching = true;
            try
            {
                var result = await ScreenCapture.CaptureRectangleAsync(_screenBounds).ConfigureAwait(true);
                if (result == null)
                {
                    Log.Warn("Capturing the screen to freeze it failed, the screen stays live.");
                    return;
                }
                if (_closed || !_live)
                {
                    result.Image.Dispose();
                    return;
                }
                // Processors which still looked at the old capture (e.g. a QR code) found it on that image
                await ImageFeatures.RemoveWhenProcessedAsync(_capture.CaptureDetails).ConfigureAwait(true);
                if (_closed)
                {
                    result.Image.Dispose();
                    return;
                }
                if (_capture.CaptureDetails.DpiX > 0 && _capture.CaptureDetails.DpiY > 0)
                {
                    // Set by the acquire step for the screen
                    result.Image.SetResolution(_capture.CaptureDetails.DpiX, _capture.CaptureDetails.DpiY);
                }
                _capture.Image = result.Image;
                _capture.Location = result.Location;
                _capture.CaptureDetails.AddMetaData(ScreenCapture.CaptureMethodKey, result.Backend.Name);
                _live = false;
                _screenImage = CreateBitmapSource(result.Image, true);
                ShowLive(false);
                _lostFocus = false;
                Activate();
                WindowHelper.ToForeground(new WindowInteropHelper(this).Handle);
                RefreshCapture();
            }
            finally
            {
                _switching = false;
            }
            SwitchTool(tool);
        }

        /// <summary>
        /// The capture changed between frozen and live: the hotspots, the selection and the zoomer show the new one
        /// </summary>
        private void RefreshCapture()
        {
            RebuildFeatureHotspots();
            DrawFeatures();
            UpdateZoomer();
            UpdateSelection();
            Redraw();
            ForEachOverlay(Redraw);
        }

        /// <summary>
        /// What needs the window handle and the capture
        /// </summary>
        private void PrepareContent()
        {
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
            Reveal();
        }

        /// <summary>
        /// The capture is rendered: activate the window
        /// </summary>
        private void Reveal()
        {
            if (_closed)
            {
                return;
            }
            // Showing the window must not have changed the bounds, but make sure
            PlaceWindow();
            AnnounceTool();
            if (_live)
            {
                // The application below keeps the focus
                return;
            }
            Activate();
            WindowHelper.ToForeground(new WindowInteropHelper(this).Handle);
        }

        /// <summary>
        /// Tell a screen reader how the built-in modes work, the keys of every mode are on F1
        /// </summary>
        private void AnnounceTool()
        {
            if (_activeTool == _regionTool)
            {
                Announce(Texts.Core.AccessibleCaptureRegion);
            }
            else if (_activeTool == _windowTool)
            {
                Announce(Texts.Core.AccessibleCaptureWindowMode);
            }
        }

        /// <summary>
        /// Let a screen reader say the text, without a visible change. The same text twice in a row is said once.
        /// </summary>
        private void Announce(string text)
        {
            if (string.IsNullOrEmpty(text) || text == _announced)
            {
                return;
            }
            _announced = text;
            Announcement.Text = text;
        }

        /// <summary>
        /// The size of the selection (and the window) is said once it stopped changing, not for every pixel the cursor moves
        /// </summary>
        private void AnnounceSelectionSoon()
        {
            if (_announceTimer == null)
            {
                _announceTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = TimeSpan.FromMilliseconds(500) };
                _announceTimer.Tick += (sender, args) =>
                {
                    _announceTimer.Stop();
                    var size = SelectionSize;
                    if (_closed || size.Width <= 0 || size.Height <= 0)
                    {
                        return;
                    }
                    Announce(_activeTool is WindowCaptureTool
                        ? string.Format(Texts.Core.AccessibleCaptureWindow, _capture?.CaptureDetails.Title, size.Width, size.Height)
                        : string.Format(Texts.Core.AccessibleCaptureSelection, size.Width, size.Height));
                };
            }
            _announceTimer.Stop();
            _announceTimer.Start();
        }

        /// <summary>
        /// Windows high contrast: the selection, rulers and crosshair in the colors the user chose, the green look doesn't fit every contrast theme
        /// </summary>
        private void UseHighContrastColors()
        {
            var highlight = SystemColors.HighlightColor;
            Resources["SelectionFillBrush"] = Frozen(new SolidColorBrush(Color.FromArgb(0x40, highlight.R, highlight.G, highlight.B)));
            Resources["SelectionBorderBrush"] = SystemColors.HighlightBrush;
            Resources["RulerBackgroundBrush"] = SystemColors.WindowBrush;
            Resources["RulerBrush"] = SystemColors.WindowTextBrush;
            Resources["SizeTextBrush"] = SystemColors.HighlightBrush;
            Resources["CrosshairBrush"] = SystemColors.HighlightBrush;

            static Brush Frozen(Brush brush)
            {
                brush.Freeze();
                return brush;
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Log.Debug("Closing capture window");
            _closed = true;
            WindowHelper.UnregisterIgnoreHandle(new WindowInteropHelper(this).Handle);
            if (_capture != null)
            {
                _capture.CaptureDetails.FeaturesChanged -= OnFeaturesChanged;
            }
            _selectionSettleTimer?.Stop();
            _announceTimer?.Stop();
            _livePixelsTimer?.Stop();
            _keyboardSubscription?.Dispose();
            _keyboardSubscription = null;
            _transparentRegion?.Dispose();
            _livePixelsCopy?.Dispose();
            _livePixelsCopy = null;
            // The copy of the whole screen is large, don't keep it until the window is garbage collected
            ZoomBrush.ImageSource = null;
            ScreenImage.Source = null;
            _screenImage = null;
        }

        #region features

        private void RebuildFeatureHotspots()
        {
            List<IDetectedFeature> features;
            lock (_capture.CaptureDetails.Features)
            {
                features = new List<IDetectedFeature>(_capture.CaptureDetails.Features);
            }
            if (_live)
            {
                // A processor of a frozen capture before L may still add what it found on that image
                features.RemoveAll(feature => feature is not WindowFeature);
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
            HandleKeyUp(e.Key);
        }

        private void HandleKeyUp(Key key)
        {
            if (key is Key.LeftShift or Key.RightShift)
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
            e.Handled = HandleKeyDown(key, Keyboard.Modifiers);
        }

        /// <returns>true when the key did something</returns>
        private bool HandleKeyDown(Key key, ModifierKeys modifiers)
        {
            var binding = _keys.Find(key, modifiers, _activeTool);
            if (binding == null)
            {
                return false;
            }
            try
            {
                binding.Execute();
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling the capture key {binding.KeyText} of {binding.Owner.GetType().FullName}", ex);
            }
            return true;
        }

        /// <summary>
        /// The live screen: the application below has the focus, the keys come from the keyboard hook. Called on the hook thread.
        /// Every key is taken away from the application below while selecting, except the Windows key and the keys of a screen reader.
        /// </summary>
        private void OnLiveKey(KeyboardHookEventArgs e)
        {
            if (_closed || e.IsWindows || e.Key is VirtualKeyCode.LeftWin or VirtualKeyCode.RightWin)
            {
                return;
            }
            // Insert and Caps Lock are the keys of screen readers (Narrator, NVDA, JAWS): they and the keys pressed with them stay theirs
            if (e.Key is VirtualKeyCode.Insert or VirtualKeyCode.Capital)
            {
                _screenReaderKeyDown = e.IsKeyDown;
                return;
            }
            if (_screenReaderKeyDown)
            {
                return;
            }
            e.Handled = true;
            var key = KeyInterop.KeyFromVirtualKey((int)e.Key);
            var modifiers = (e.IsShift ? ModifierKeys.Shift : ModifierKeys.None) | (e.IsControl ? ModifierKeys.Control : ModifierKeys.None) | (e.IsAlt ? ModifierKeys.Alt : ModifierKeys.None);
            bool isKeyDown = e.IsKeyDown;
            Dispatcher.InvokeAsync(() =>
            {
                if (_closed)
                {
                    return;
                }
                if (isKeyDown)
                {
                    HandleKeyDown(key, modifiers);
                }
                else
                {
                    HandleKeyUp(key);
                }
            }, DispatcherPriority.Input).Task.FireAndLog("Handle a key of the live capture", Log);
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
            var title = window.GetCaption();
            _capture.CaptureDetails.Title = title;
            _capture.CaptureDetails.AddMetaData("windowtitle", title);
        }

        /// <inheritdoc />
        public IInteropWindow FindWindowUnderCursor(bool includeChildren)
        {
            // In screen coordinates, as the windows are
            var cursorPosition = User32Api.GetCursorLocation();
            var window = _windows.FirstOrDefault(w => w.GetInfo().Bounds.Contains(cursorPosition));
            if (window == null || !includeChildren)
            {
                return window;
            }

            // At the edge of the window take the whole window, else the (deepest) child window under the cursor
            var bounds = window.GetInfo().Bounds;
            if (bounds.X == cursorPosition.X || bounds.Y == cursorPosition.Y || bounds.Right == cursorPosition.X || bounds.Bottom == cursorPosition.Y)
            {
                return window;
            }
            return window.FindChildAt(cursorPosition) ?? window;
        }

        /// <inheritdoc />
        public void ShowSelection(NativeRect rect, bool animate = false, Action completed = null)
        {
            if (!rect.Equals(_selectionRect))
            {
                SelectionChanged();
            }
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
            UpdateZoomerSelection();
            UpdateSelectionPreview();
        }

        /// <inheritdoc />
        public bool IsSelectionVisible => SelectionPath.Visibility == Visibility.Visible;

        /// <inheritdoc />
        public bool IsSelectionAnimating => _selectionClock != null;

        /// <inheritdoc />
        public NativeRect Selection => IsSelectionVisible ? _selectionRect : NativeRect.Empty;

        /// <summary>
        /// How long the selection has to stay the same before the panels move out of its way
        /// </summary>
        private static readonly TimeSpan SelectionSettleTime = TimeSpan.FromMilliseconds(300);
        private DateTime _selectionChangedAt = DateTime.MinValue;
        private DispatcherTimer _selectionSettleTimer;

        private bool IsSelectionSettled => DateTime.UtcNow - _selectionChangedAt >= SelectionSettleTime;

        /// <summary>
        /// The panels wait until the selection settled, then check their place once more
        /// </summary>
        private void SelectionChanged()
        {
            _selectionChangedAt = DateTime.UtcNow;
            if (_panels.Count == 0)
            {
                return;
            }
            if (_selectionSettleTimer == null)
            {
                _selectionSettleTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = SelectionSettleTime };
                _selectionSettleTimer.Tick += (sender, args) =>
                {
                    _selectionSettleTimer.Stop();
                    UpdatePanels();
                };
            }
            _selectionSettleTimer.Stop();
            _selectionSettleTimer.Start();
        }

        /// <inheritdoc />
        public NativeSize SelectionSize { get; private set; } = NativeSize.Empty;

        /// <inheritdoc />
        public void ClearLabels()
        {
            SelectionSize = NativeSize.Empty;
            LabelLayer.Clear();
            UpdateSelectionPreview();
        }

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
            if (_noAnimations)
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
                ClearLabels();
                return;
            }
            SelectionSize = size;
            UpdateZoomerSelection();
            UpdateSelectionPreview();
            AnnounceSelectionSoon();

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

            if (fadeIn && !_noAnimations)
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
            // Without the zoomer the preview of the selection goes back to its corner, and the other way round
            UpdateSelectionPreview();
            if (_noAnimations)
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
            // The live pixels are only those around the cursor
            ZoomBrush.Viewbox = _live
                ? new Rect(0, 0, ZoomSourceSize, ZoomSourceSize)
                : new Rect(_cursorPos.X - ZoomSourceSize / 2, _cursorPos.Y - ZoomSourceSize / 2, ZoomSourceSize, ZoomSourceSize);
            if (!_zoomerShown)
            {
                return;
            }
            if (_live)
            {
                CopyLivePixels();
            }

            var screenBounds = GetMonitorBounds();
            int zoomSize = ZoomerPlacement.GetSize(screenBounds);
            if (zoomSize != _zoomSize)
            {
                ResizeZoomer(zoomSize);
            }
            UpdateZoomerSelection();

            var slotSize = UpdateZoomerContent(zoomSize);
            if (slotSize != _zoomerSlotSize)
            {
                // The magnifier and the preview differ in size: keep the corner, at the same distance from the cursor
                _zoomerSlotSize = slotSize;
                _zoomOffset = ZoomerPlacement.Resize(_zoomOffset, slotSize);
                ZoomerOffset.BeginAnimation(TranslateTransform.XProperty, null);
                ZoomerOffset.BeginAnimation(TranslateTransform.YProperty, null);
                ZoomerOffset.X = _zoomOffset.X;
                ZoomerOffset.Y = _zoomOffset.Y;
            }

            var offset = ZoomerPlacement.GetOffset(_cursorPos, _zoomOffset, slotSize, screenBounds, GetZoomerAvoids());
            if (offset.Equals(_zoomOffset))
            {
                return;
            }
            _zoomOffset = offset;
            if (_noAnimations)
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
        /// The preview shows the selection as it is now, an error is logged and doesn't stop the capture
        /// </summary>
        private void UpdateSelectionPreview()
        {
            try
            {
                _selectionPreview?.Update();
                if (_selectionPreview?.ShowsInZoomer == true)
                {
                    // E.g. another size from a key, without a mouse move
                    UpdateZoomer();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error updating the selection preview", ex);
            }
        }

        /// <summary>
        /// The zoomer shows the magnifier, or (P) the preview of the selection when there is one
        /// </summary>
        /// <returns>The size of what it shows, for the placement</returns>
        private NativeSize UpdateZoomerContent(int zoomSize)
        {
            var area = _selectionPreview?.ShowsInZoomer == true ? _selectionPreview.CurrentArea : NativeRect.Empty;
            if (area.IsEmpty)
            {
                ZoomerMagnifier.Visibility = Visibility.Visible;
                ZoomerPreview.Visibility = Visibility.Collapsed;
                _zoomerPreviewArea = NativeRect.Empty;
                return new NativeSize(zoomSize, zoomSize);
            }
            var size = SelectionPreviewOverlay.GetZoomerSize(area.Size, zoomSize, out double scale);
            if (area != _zoomerPreviewArea || size != _zoomerPreviewSize)
            {
                _zoomerPreviewArea = area;
                _zoomerPreviewSize = size;
                using var dc = ZoomerPreview.Open();
                _selectionPreview.DrawInZoomer(dc, area, size, scale);
            }
            ZoomerMagnifier.Visibility = Visibility.Collapsed;
            ZoomerPreview.Visibility = Visibility.Visible;
            return size;
        }

        /// <summary>
        /// The selection in the zoomer, with the size the labels show (the region tool includes the pixel under the cursor)
        /// </summary>
        private void UpdateZoomerSelection()
        {
            var size = SelectionSize.IsEmpty ? _selectionRect.Size : SelectionSize;
            if (!_zoomerShown || _zoomSize == 0 || !IsSelectionVisible || IsSelectionAnimating || size.IsEmpty)
            {
                ZoomerSelection.Visibility = Visibility.Collapsed;
                return;
            }
            double pixel = (double)_zoomSize / ZoomSourceSize;
            Canvas.SetLeft(ZoomerSelection, (_selectionRect.X - _cursorPos.X + ZoomSourceSize / 2) * pixel);
            Canvas.SetTop(ZoomerSelection, (_selectionRect.Y - _cursorPos.Y + ZoomSourceSize / 2) * pixel);
            ZoomerSelection.Width = size.Width * pixel;
            ZoomerSelection.Height = size.Height * pixel;
            ZoomerSelection.Visibility = Visibility.Visible;
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
            ZoomerSelectionLayer.Clip = new EllipseGeometry(new Rect(0, 0, zoomSize, zoomSize));
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
                CursorImage.Source = CreateBitmapSource(cursorBitmap, false);
                Canvas.SetLeft(CursorImage, _capture.CursorLocation.X);
                Canvas.SetTop(CursorImage, _capture.CursorLocation.Y);
            }
            CursorImage.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Text in pixels, the size in points scaled like the rest of the UI (DPI and the "Text size" of Windows)
        /// </summary>
        private FormattedText CreateText(string text, double points, Brush brush, bool bold = false)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, bold ? _boldLabelTypeface : _labelTypeface,
                FontSizeExtension.Scale(points * 96 / 72 * _dpiScale), brush, 1.0);
        }

        /// <summary>
        /// The pixels of an image at 96 DPI, so one unit is one pixel
        /// </summary>
        /// <param name="image">The image</param>
        /// <param name="opaque">True for the capture of the screen: its pixels are used as they are (Bgr32 ignores the alpha byte), so neither
        /// GDI+ nor WPF has to convert the whole capture (premultiplying 7 million pixels takes noticeable time); gaps between monitors become black,
        /// like the background. False for images with transparency (the mouse cursor), which are premultiplied (Pbgra32).</param>
        private static BitmapSource CreateBitmapSource(System.Drawing.Image image, bool opaque)
        {
            if (image is not System.Drawing.Bitmap bitmap)
            {
                using var copy = new System.Drawing.Bitmap(image);
                return CreateBitmapSource(copy, opaque);
            }

            DrawingPixelFormat lockFormat;
            PixelFormat pixelFormat;
            if (opaque)
            {
                lockFormat = bitmap.PixelFormat is DrawingPixelFormat.Format32bppArgb or DrawingPixelFormat.Format32bppPArgb or DrawingPixelFormat.Format32bppRgb
                    ? bitmap.PixelFormat
                    : DrawingPixelFormat.Format32bppRgb;
                pixelFormat = PixelFormats.Bgr32;
            }
            else
            {
                lockFormat = DrawingPixelFormat.Format32bppPArgb;
                pixelFormat = PixelFormats.Pbgra32;
            }
            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, lockFormat);
            try
            {
                var bitmapSource = BitmapSource.Create(bitmapData.Width, bitmapData.Height, 96, 96, pixelFormat, null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// The live screen: copy the pixels around the cursor for the zoomer, the window itself isn't in them (see ApplyLiveMode)
        /// </summary>
        private void CopyLivePixels()
        {
            if (_livePixels == null || _closed)
            {
                return;
            }
            _livePixelsCopy ??= new System.Drawing.Bitmap(ZoomSourceSize, ZoomSourceSize, DrawingPixelFormat.Format32bppRgb);
            var screenLocation = new NativePoint(_screenBounds.X + _cursorPos.X - ZoomSourceSize / 2, _screenBounds.Y + _cursorPos.Y - ZoomSourceSize / 2);
            if (!CopyFromScreen(screenLocation, _livePixelsCopy))
            {
                return;
            }
            var bitmapData = _livePixelsCopy.LockBits(new System.Drawing.Rectangle(0, 0, ZoomSourceSize, ZoomSourceSize), ImageLockMode.ReadOnly, DrawingPixelFormat.Format32bppRgb);
            try
            {
                _livePixels.WritePixels(new Int32Rect(0, 0, ZoomSourceSize, ZoomSourceSize), bitmapData.Scan0, bitmapData.Stride * ZoomSourceSize, bitmapData.Stride);
            }
            finally
            {
                _livePixelsCopy.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// The live screen: the color of a pixel on the screen
        /// </summary>
        private Color GetLivePixelColor(NativePoint location)
        {
            using var pixel = new System.Drawing.Bitmap(1, 1, DrawingPixelFormat.Format32bppRgb);
            if (!CopyFromScreen(new NativePoint(_screenBounds.X + location.X, _screenBounds.Y + location.Y), pixel))
            {
                return Colors.Transparent;
            }
            var color = pixel.GetPixel(0, 0);
            return Color.FromRgb(color.R, color.G, color.B);
        }

        /// <summary>
        /// Copy what the screen shows at the location into the bitmap, outside of the screens it is black
        /// </summary>
        /// <returns>false when the screen can't be read (e.g. while the secure desktop shows)</returns>
        private static bool CopyFromScreen(NativePoint screenLocation, System.Drawing.Bitmap target)
        {
            try
            {
                using var graphics = System.Drawing.Graphics.FromImage(target);
                graphics.CopyFromScreen(screenLocation.X, screenLocation.Y, 0, 0, target.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                return true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Log.Debug("Couldn't copy pixels from the screen", ex);
                return false;
            }
        }

        private static Point ToPoint(NativePoint point) => new Point(point.X, point.Y);

        private static Rect ToRect(NativeRect rect) => new Rect(rect.X, rect.Y, rect.Width, rect.Height);

        #endregion
    }
}
