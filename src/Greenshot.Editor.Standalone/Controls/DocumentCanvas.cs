/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.Editor.Standalone.Editing;
using Greenshot.Editor.Standalone.Tools;
using Greenshot.Editor.Standalone.ViewModels;
using Adorner = Greenshot.Editor.Standalone.Tools.Adorner;

namespace Greenshot.Editor.Standalone.Controls
{
    /// <summary>
    /// Shows the document of a tab and gives the pointer to its tool. Three layers: the pixels the document renderer drew
    /// (ImageSharp, the same pixels as an export), the adorners of the tool drawn in screen pixels, and a text box while
    /// a text is typed. Zoom and pan: Ctrl+wheel zooms at the pointer, the wheel scrolls (Shift+wheel sideways), the
    /// middle button or Space with the left button drags the view. It scrolls itself (ILogicalScrollable) in a ScrollViewer.
    /// </summary>
    public sealed class DocumentCanvas : Control, ILogicalScrollable
    {
        // Room around the canvas, in screen pixels, so handles at the edge can be grabbed
        private const double CanvasMargin = 48;
        private const double HandleSize = 8;
        private const double WheelStep = 48;

        public static readonly StyledProperty<DocumentTabViewModel> TabProperty = AvaloniaProperty.Register<DocumentCanvas, DocumentTabViewModel>(nameof(Tab));

        private static readonly ConditionalWeakTable<DocumentTabViewModel, TabSurface> Surfaces = new();
        private static readonly Dictionary<StandardCursorType, Cursor> Cursors = new();
        private static readonly IBrush PageShadow = new SolidColorBrush(Color.FromArgb(48, 0, 0, 0)).ToImmutable();
        private static IBrush _checkerboard;

        private readonly TextBox _textBox;
        private (Element Element, double Size)? _editing;
        private TabSurface _surface;
        private Size _viewport;
        private Vector _offset;
        private Point? _panFrom;
        private Vector _panOffset;
        private bool _toolDragging;
        private bool _spaceDown;
        private bool _fitPending = true;

        static DocumentCanvas()
        {
            FocusableProperty.OverrideDefaultValue<DocumentCanvas>(true);
            ClipToBoundsProperty.OverrideDefaultValue<DocumentCanvas>(true);
            TabProperty.Changed.AddClassHandler<DocumentCanvas>((canvas, e) => canvas.TabReplaced(e.OldValue as DocumentTabViewModel, e.NewValue as DocumentTabViewModel));
        }

        public DocumentCanvas()
        {
            _textBox = new TextBox
            {
                AcceptsReturn = true,
                AcceptsTab = false,
                TextWrapping = TextWrapping.Wrap,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0),
                MinHeight = 0,
                MinWidth = 0,
                Background = Brushes.Transparent,
                VerticalContentAlignment = VerticalAlignment.Center,
                IsVisible = false
            };
            // The text is seen as it will be drawn: no fill of the theme behind it, also not while it has the focus
            _textBox.Resources["TextControlBackgroundFocused"] = Brushes.Transparent;
            _textBox.Resources["TextControlBackgroundPointerOver"] = Brushes.Transparent;
            _textBox.AddHandler(KeyDownEvent, TextBoxKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            _textBox.LostFocus += (_, _) => EndTextEdit(true);
            VisualChildren.Add(_textBox);
            LogicalChildren.Add(_textBox);
            ActualThemeVariantChanged += (_, _) => InvalidateVisual();
            LostFocus += (_, _) => _spaceDown = false;
        }

        public DocumentTabViewModel Tab
        {
            get => GetValue(TabProperty);
            set => SetValue(TabProperty, value);
        }

        private void TabReplaced(DocumentTabViewModel before, DocumentTabViewModel after)
        {
            if (before != null)
            {
                EndTextEdit(true);
                before.ToolStateChanged -= InvalidateVisual;
                before.TextEditRequested -= BeginTextEdit;
                before.TextEditEnding -= KeepTypedText;
                before.PropertyChanged -= TabPropertyChanged;
                before.ShownChanged -= DocumentShown;
                before.ScrollOffset = new PointD(_offset.X, _offset.Y);
            }

            if (_surface != null)
            {
                _surface.Painted -= InvalidateVisual;
                _surface = null;
            }

            _toolDragging = false;
            _panFrom = null;
            if (after != null)
            {
                after.ToolStateChanged += InvalidateVisual;
                after.TextEditRequested += BeginTextEdit;
                after.TextEditEnding += KeepTypedText;
                after.PropertyChanged += TabPropertyChanged;
                after.ShownChanged += DocumentShown;
                _surface = Surfaces.GetValue(after, tab => new TabSurface(tab));
                _surface.Painted += InvalidateVisual;
                _offset = new Vector(after.ScrollOffset.X, after.ScrollOffset.Y);
                _fitPending = !after.HasBeenShown;
                if (_fitPending && _viewport.Width > 0)
                {
                    _fitPending = false;
                    after.HasBeenShown = true;
                    ZoomBestFit();
                }
            }

            UpdateScrolling();
            UpdateCursor();
            InvalidateVisual();
        }

        private void TabPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DocumentTabViewModel.Zoom))
            {
                UpdateScrolling();
                InvalidateArrange();
                InvalidateVisual();
            }
            else if (e.PropertyName == nameof(DocumentTabViewModel.ToolKind))
            {
                UpdateCursor();
            }
        }

        private void DocumentShown()
        {
            // The canvas may have another size (crop, frame, resize): the scroll bars follow, the adorners are drawn at once
            UpdateScrolling();
            InvalidateArrange();
            InvalidateVisual();
        }

        /// <summary>
        /// True when the pixels which are shown are those of the document as it is now (drawing runs behind the changes)
        /// </summary>
        internal bool IsDrawn => _surface == null || _surface.IsCurrent;

        private double Zoom => Tab?.Zoom ?? 1;

        /// <summary>
        /// The canvas of the document which is shown
        /// </summary>
        private RectD Area => Tab?.Document.CanvasArea() ?? default;

        /// <summary>
        /// The screen point of the top left of the canvas
        /// </summary>
        private Point Origin
        {
            get
            {
                var area = Area;
                double width = area.Width * Zoom, height = area.Height * Zoom;
                double x = width + 2 * CanvasMargin <= _viewport.Width ? (_viewport.Width - width) / 2 : CanvasMargin - _offset.X;
                double y = height + 2 * CanvasMargin <= _viewport.Height ? (_viewport.Height - height) / 2 : CanvasMargin - _offset.Y;
                return new Point(Math.Round(x), Math.Round(y));
            }
        }

        public Point ToScreen(PointD point)
        {
            var origin = Origin;
            var area = Area;
            return new Point(origin.X + (point.X - area.X) * Zoom, origin.Y + (point.Y - area.Y) * Zoom);
        }

        public PointD ToCanvas(Point point)
        {
            var origin = Origin;
            var area = Area;
            return new PointD((point.X - origin.X) / Zoom + area.X, (point.Y - origin.Y) / Zoom + area.Y);
        }

        /// <summary>
        /// The middle of what is visible, in canvas pixels: where a pasted image goes
        /// </summary>
        public PointD VisibleCenter
        {
            get
            {
                var center = ToCanvas(new Point(_viewport.Width / 2, _viewport.Height / 2));
                var area = Area;
                // Never outside of the canvas, where it wouldn't be seen
                return new PointD(Math.Max(area.X, Math.Min(area.Right, center.X)), Math.Max(area.Y, Math.Min(area.Bottom, center.Y)));
            }
        }

        public void ZoomTo(double zoom, Point? at = null)
        {
            if (Tab == null)
            {
                return;
            }

            var screen = at ?? new Point(_viewport.Width / 2, _viewport.Height / 2);
            var fixedPoint = ToCanvas(screen);
            Tab.Zoom = zoom;
            UpdateScrolling();
            // The canvas point under the pointer stays under the pointer
            var moved = ToScreen(fixedPoint);
            SetOffset(_offset + (moved - screen));
        }

        public void ZoomBestFit()
        {
            if (Tab == null || _viewport.Width <= 0)
            {
                return;
            }

            var area = Area;
            double zoom = Math.Min((_viewport.Width - 2 * CanvasMargin) / Math.Max(1, area.Width), (_viewport.Height - 2 * CanvasMargin) / Math.Max(1, area.Height));
            Tab.Zoom = Math.Max(DocumentTabViewModel.MinZoom, Math.Min(1, zoom));
            _offset = default;
            UpdateScrolling();
            InvalidateVisual();
        }

        /// <summary>
        /// The next zoom step up or down: 100 % and the usual steps are kept
        /// </summary>
        public static double NextZoom(double zoom, bool larger)
        {
            double[] steps = { 0.05, 0.1, 0.15, 0.25, 0.33, 0.5, 0.66, 0.75, 1, 1.25, 1.5, 2, 3, 4, 6, 8, 12, 16, 24, 32 };
            return larger ? steps.FirstOrDefault(step => step > zoom + 0.001) is double up and > 0 ? up : steps[steps.Length - 1]
                : steps.LastOrDefault(step => step < zoom - 0.001) is double down and > 0 ? down : steps[0];
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _textBox.Measure(Size.Infinity);
            return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_viewport != finalSize)
            {
                _viewport = finalSize;
                if (_fitPending && finalSize.Width > 0 && Tab != null)
                {
                    _fitPending = false;
                    Tab.HasBeenShown = true;
                    ZoomBestFit();
                }

                UpdateScrolling();
            }

            ArrangeTextBox();
            return finalSize;
        }

        public override void Render(DrawingContext context)
        {
            bool dark = ActualThemeVariant == ThemeVariant.Dark;
            context.FillRectangle(dark ? DarkBackground : LightBackground, new Rect(_viewport));
            if (Tab == null)
            {
                return;
            }

            var area = Area;
            var origin = Origin;
            var page = new Rect(origin.X, origin.Y, area.Width * Zoom, area.Height * Zoom);
            // A soft shadow under the page, then the checkerboard for transparent pixels and the pixels of the document
            context.FillRectangle(PageShadow, new Rect(page.X + 2, page.Y + 2, page.Width + 2, page.Height + 2));
            context.FillRectangle(_checkerboard ??= CreateCheckerboard(), page);
            if (_surface?.Surface is CanvasSurface surface)
            {
                // Until a change of the canvas (a crop, a frame) is drawn the pixels are those of the canvas before it
                var shown = ToScreen(new PointD(surface.Area.X, surface.Area.Y));
                double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
                // Enlarged pixels stay pixels, a document which is shown smaller is smoothed
                var mode = Zoom >= 2 || Math.Abs(Zoom - 1) < 0.0001 ? BitmapInterpolationMode.None : BitmapInterpolationMode.HighQuality;
                using (context.PushClip(page))
                using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = mode }))
                {
                    surface.Draw(context, new Point(Math.Round(shown.X), Math.Round(shown.Y)), Zoom, new Rect(_viewport), scaling);
                }
            }

            DrawAdorners(context);
        }

        private static readonly IBrush LightBackground = new SolidColorBrush(Color.FromRgb(0xE9, 0xE9, 0xE9)).ToImmutable();
        private static readonly IBrush DarkBackground = new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x1C)).ToImmutable();

        private static unsafe IBrush CreateCheckerboard()
        {
            const int size = 16;
            var bitmap = new WriteableBitmap(new PixelSize(size, size), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
            using (var buffer = bitmap.Lock())
            {
                for (int y = 0; y < size; y++)
                {
                    var row = new Span<uint>((byte*)buffer.Address + y * buffer.RowBytes, size);
                    for (int x = 0; x < size; x++)
                    {
                        row[x] = (x < size / 2) == (y < size / 2) ? 0xFFD6D6D6 : 0xFFFFFFFF;
                    }
                }
            }

            return new ImageBrush(bitmap) { TileMode = TileMode.Tile, DestinationRect = new RelativeRect(0, 0, size, size, RelativeUnit.Absolute), Stretch = Stretch.None }.ToImmutable();
        }

        private Color AccentColor => this.TryFindResource("SystemAccentColor", ActualThemeVariant, out var value) && value is Color color ? color : Color.FromRgb(0x3D, 0x8B, 0x3D);

        private void DrawAdorners(DrawingContext context)
        {
            var accent = new SolidColorBrush(AccentColor);
            var outline = new Pen(accent, 1);
            var contrast = new Pen(Brushes.White, 3);
            var handleFill = Brushes.White;
            IEnumerable<Adorner> adorners = Tab.Tool.Adorners;
            if (Tab.Tool is not SelectTool)
            {
                // A drawing tool keeps the element it just drew selected: its outline shows which one the panel changes
                adorners = adorners.Concat(Tab.SelectedElements.Select(element => ElementShapes.AreaOf(element) is RectD box
                    ? new Adorner(AdornerKind.Outline, box, ElementShapes.CanTurn(element) ? element.Rotation : 0)
                    : new Adorner(AdornerKind.Outline, element.Bounds)));
            }

            foreach (var adorner in adorners)
            {
                switch (adorner.Kind)
                {
                    case AdornerKind.Outline:
                        var geometry = Polygon(Corners(adorner.Area, adorner.Rotation));
                        context.DrawGeometry(null, contrast, geometry);
                        context.DrawGeometry(null, outline, geometry);
                        break;
                    case AdornerKind.Handle:
                        var point = Snap(ToScreen(adorner.Point));
                        context.DrawRectangle(handleFill, outline, new Rect(point.X - HandleSize / 2, point.Y - HandleSize / 2, HandleSize, HandleSize));
                        break;
                    case AdornerKind.RotateHandle:
                        var at = ToScreen(adorner.Point);
                        context.DrawLine(outline, ToScreen(adorner.To), at);
                        context.DrawEllipse(accent, new Pen(Brushes.White, 1.5), at, HandleSize / 2 + 1, HandleSize / 2 + 1);
                        break;
                    case AdornerKind.PointHandle:
                        context.DrawEllipse(handleFill, new Pen(accent, 1.5), ToScreen(adorner.Point), HandleSize / 2 + 1, HandleSize / 2 + 1);
                        break;
                    case AdornerKind.RubberBand:
                        context.DrawRectangle(new SolidColorBrush(AccentColor, 0.15), outline, ScreenRect(adorner.Area));
                        break;
                    case AdornerKind.CropArea:
                        DrawCropArea(context, ScreenRect(adorner.Area), outline);
                        break;
                }
            }
        }

        private void DrawCropArea(DrawingContext context, Rect keep, Pen outline)
        {
            var area = Area;
            var page = new Rect(ToScreen(new PointD(area.X, area.Y)), new Size(area.Width * Zoom, area.Height * Zoom));
            var dim = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(page), new RectangleGeometry(keep));
            context.DrawGeometry(new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)), null, dim);
            context.DrawRectangle(null, outline, keep);
            context.DrawRectangle(null, new Pen(Brushes.White, 1) { DashStyle = DashStyle.Dash }, keep);
            // Thirds, as a help to place the crop
            var thirds = new Pen(new SolidColorBrush(Color.FromArgb(96, 255, 255, 255)), 1);
            for (int i = 1; i < 3; i++)
            {
                context.DrawLine(thirds, new Point(keep.X + keep.Width * i / 3, keep.Y), new Point(keep.X + keep.Width * i / 3, keep.Bottom));
                context.DrawLine(thirds, new Point(keep.X, keep.Y + keep.Height * i / 3), new Point(keep.Right, keep.Y + keep.Height * i / 3));
            }
        }

        private Point[] Corners(RectD area, double rotation)
        {
            var corners = new[] { new PointD(area.X, area.Y), new PointD(area.Right, area.Y), new PointD(area.Right, area.Bottom), new PointD(area.X, area.Bottom) };
            return corners.Select(corner => Snap(ToScreen(rotation == 0 ? corner : corner.Turned(rotation, area.Center)))).ToArray();
        }

        private static Geometry Polygon(Point[] points) => new PolylineGeometry(points.Append(points[0]), false);

        private Rect ScreenRect(RectD area) => new(Snap(ToScreen(new PointD(area.X, area.Y))), Snap(ToScreen(new PointD(area.Right, area.Bottom))));

        // Lines of one pixel are sharp in the middle of a pixel
        private static Point Snap(Point point) => new(Math.Round(point.X) + 0.5, Math.Round(point.Y) + 0.5);

        private ToolPointer PointerOf(PointerEventArgs e, int clickCount = 1)
        {
            var modifiers = e.KeyModifiers;
            return new ToolPointer(ToCanvas(e.GetPosition(this)), modifiers.HasFlag(KeyModifiers.Shift), modifiers.HasFlag(KeyModifiers.Control), modifiers.HasFlag(KeyModifiers.Alt), 1 / Zoom, clickCount);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (Tab == null)
            {
                return;
            }

            var kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
            if (kind == PointerUpdateKind.MiddleButtonPressed || (kind == PointerUpdateKind.LeftButtonPressed && _spaceDown))
            {
                Focus();
                _panFrom = e.GetPosition(this);
                _panOffset = _offset;
                Cursor = CursorOf(StandardCursorType.SizeAll);
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }

            if (kind != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }

            EndTextEdit(true);
            Focus();
            _toolDragging = true;
            e.Pointer.Capture(this);
            Tab.Tool.Down(PointerOf(e, e.ClickCount));
            UpdateCursor();
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (Tab == null)
            {
                return;
            }

            var position = e.GetPosition(this);
            if (_panFrom is Point from)
            {
                SetOffset(_panOffset - (position - from));
                return;
            }

            if (_toolDragging)
            {
                Tab.Tool.Move(PointerOf(e));
                ScrollIntoView(position);
            }
            else
            {
                Tab.Tool.Hover(PointerOf(e));
            }

            UpdateCursor();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (_panFrom != null && e.InitialPressMouseButton is MouseButton.Middle or MouseButton.Left)
            {
                _panFrom = null;
                e.Pointer.Capture(null);
                UpdateCursor();
                e.Handled = true;
                return;
            }

            if (_toolDragging && e.InitialPressMouseButton == MouseButton.Left)
            {
                // Released first: the capture which is lost by it must not cancel what the tool did
                _toolDragging = false;
                e.Pointer.Capture(null);
                Tab?.Tool.Up(PointerOf(e));
                InvalidateVisual();
                UpdateCursor();
                e.Handled = true;
            }
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            _panFrom = null;
            if (_toolDragging)
            {
                // Lost while dragging (e.g. Alt+Tab): the change is dropped
                _toolDragging = false;
                Tab?.Tool.Cancel();
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (Tab == null)
            {
                return;
            }

            double delta = e.Delta.Y != 0 ? e.Delta.Y : e.Delta.X;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ZoomTo(NextZoom(Tab.Zoom, delta > 0), e.GetPosition(this));
            }
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) || e.Delta.Y == 0)
            {
                SetOffset(_offset + new Vector(-Math.Sign(delta) * WheelStep * 2, 0));
            }
            else
            {
                SetOffset(_offset + new Vector(0, -Math.Sign(delta) * WheelStep * 2));
            }

            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Space && !_toolDragging)
            {
                if (!_spaceDown)
                {
                    _spaceDown = true;
                    Cursor = CursorOf(StandardCursorType.Hand);
                }

                e.Handled = true;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.Space)
            {
                _spaceDown = false;
                UpdateCursor();
                e.Handled = true;
            }
        }

        private static Cursor CursorOf(StandardCursorType type)
        {
            if (!Cursors.TryGetValue(type, out var cursor))
            {
                Cursors[type] = cursor = new Cursor(type);
            }

            return cursor;
        }

        private void UpdateCursor()
        {
            if (_panFrom != null || _spaceDown)
            {
                return;
            }

            Cursor = CursorOf(Tab?.Tool.Cursor switch
            {
                ToolCursor.Cross => StandardCursorType.Cross,
                ToolCursor.Move => StandardCursorType.SizeAll,
                ToolCursor.SizeNorthSouth => StandardCursorType.SizeNorthSouth,
                ToolCursor.SizeWestEast => StandardCursorType.SizeWestEast,
                ToolCursor.SizeNorthWestSouthEast => StandardCursorType.TopLeftCorner,
                ToolCursor.SizeNorthEastSouthWest => StandardCursorType.TopRightCorner,
                ToolCursor.Rotate => StandardCursorType.Hand,
                ToolCursor.Pen => StandardCursorType.Cross,
                ToolCursor.Text => StandardCursorType.Ibeam,
                _ => StandardCursorType.Arrow
            });
        }

        /// <summary>
        /// While dragging near the edge the view follows the pointer
        /// </summary>
        private void ScrollIntoView(Point point)
        {
            const double edge = 16;
            var delta = new Vector(point.X < edge ? -WheelStep / 4 : point.X > _viewport.Width - edge ? WheelStep / 4 : 0,
                point.Y < edge ? -WheelStep / 4 : point.Y > _viewport.Height - edge ? WheelStep / 4 : 0);
            if (delta != default)
            {
                SetOffset(_offset + delta);
            }
        }

        private void BeginTextEdit(Guid id)
        {
            if (Tab == null || DocumentEdits.Find(Tab.Committed, id) is not Element element)
            {
                return;
            }

            var style = Tab.Committed.StyleOf(element).Text ?? new TextStyle();
            _textBox.Text = ElementText.TextOf(element) ?? string.Empty;
            _textBox.FontFamily = new FontFamily(string.Join(", ", TextStyle.Families(style.Family).DefaultIfEmpty(FontFamily.Default.Name)));
            _textBox.FontWeight = (FontWeight)Math.Max(1, Math.Min(999, style.Weight ?? 400));
            _textBox.FontStyle = style.Italic == true ? FontStyle.Italic : FontStyle.Normal;
            var color = PropertiesViewModel.ToColor(style.Color ?? new Rgba(0, 0, 0, 255));
            _textBox.Foreground = new SolidColorBrush(color);
            _textBox.CaretBrush = new SolidColorBrush(color);
            _textBox.BorderBrush = new SolidColorBrush(AccentColor);
            _textBox.TextAlignment = style.Horizontal switch
            {
                Document.TextAlignment.Start => Avalonia.Media.TextAlignment.Left,
                Document.TextAlignment.End => Avalonia.Media.TextAlignment.Right,
                _ => Avalonia.Media.TextAlignment.Center
            };
            _textBox.VerticalContentAlignment = style.Vertical switch
            {
                Document.TextAlignment.Start => VerticalAlignment.Top,
                Document.TextAlignment.End => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center
            };
            _editing = (element, style.Size ?? 11);
            _textBox.IsVisible = true;
            InvalidateArrange();
            _textBox.Focus();
            _textBox.SelectAll();
        }

        private void ArrangeTextBox()
        {
            if (Tab == null || _editing is not var (element, size))
            {
                _textBox.Arrange(default);
                return;
            }

            var (area, rotation) = ElementText.EditAreaOf(Tab.Committed, element);
            var topLeft = ToScreen(new PointD(area.X, area.Y));
            var rect = new Rect(topLeft.X, topLeft.Y, Math.Max(40, area.Width * Zoom), Math.Max(size * Zoom * 1.6, area.Height * Zoom));
            // The renderer draws the size in canvas pixels
            _textBox.FontSize = Math.Max(1, size * Zoom);
            _textBox.RenderTransformOrigin = RelativePoint.Center;
            _textBox.RenderTransform = rotation == 0 ? null : new RotateTransform(rotation);
            _textBox.Measure(rect.Size);
            _textBox.Arrange(rect);
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                EndTextEdit(false);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                EndTextEdit(true);
                e.Handled = true;
            }
        }

        private void KeepTypedText() => EndTextEdit(true);

        private void EndTextEdit(bool keep)
        {
            if (_editing == null)
            {
                return;
            }

            _editing = null;
            bool hadFocus = _textBox.IsFocused;
            _textBox.IsVisible = false;
            Tab?.EndTextEdit((_textBox.Text ?? string.Empty).Replace("\r\n", "\n"), keep);
            InvalidateArrange();
            if (hadFocus)
            {
                Focus();
            }
        }

        // ILogicalScrollable: the extent is the canvas at the zoom with the margin around it
        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        public bool IsLogicalScrollEnabled => true;

        public Size ScrollSize => new(WheelStep, WheelStep);

        public Size PageScrollSize => _viewport;

        public Size Extent => new(Area.Width * Zoom + 2 * CanvasMargin, Area.Height * Zoom + 2 * CanvasMargin);

        public Size Viewport => _viewport;

        public Vector Offset
        {
            get => _offset;
            set => SetOffset(value);
        }

        public event EventHandler ScrollInvalidated;

        public bool BringIntoView(Control target, Rect targetRect) => false;

        public Control GetControlInDirection(NavigationDirection direction, Control from) => null;

        public void RaiseScrollInvalidated(EventArgs e) => ScrollInvalidated?.Invoke(this, e);

        private Vector Clamp(Vector offset)
        {
            var extent = Extent;
            return new Vector(Math.Max(0, Math.Min(offset.X, extent.Width - _viewport.Width)), Math.Max(0, Math.Min(offset.Y, extent.Height - _viewport.Height)));
        }

        private void SetOffset(Vector offset)
        {
            var clamped = Clamp(offset);
            if (clamped == _offset)
            {
                return;
            }

            _offset = clamped;
            RaiseScrollInvalidated(EventArgs.Empty);
            InvalidateArrange();
            InvalidateVisual();
        }

        private void UpdateScrolling()
        {
            _offset = Clamp(_offset);
            RaiseScrollInvalidated(EventArgs.Empty);
        }
    }
}
