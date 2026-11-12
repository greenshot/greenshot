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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.WpfEditor.Editing;
using Greenshot.WpfEditor.Rendering;
using Greenshot.WpfEditor.Tools;
using Greenshot.WpfEditor.ViewModels;

namespace Greenshot.WpfEditor.Controls
{
    /// <summary>
    /// Shows the document of a tab and gives the mouse to its tool. Three layers: the bitmap the document renderer drew
    /// (ImageSharp, the same pixels as an export), the adorners of the tool drawn by WPF in screen pixels, and a text box
    /// while a text is typed. Zoom and pan: Ctrl+wheel zooms at the mouse, the wheel scrolls (Shift+wheel sideways), the
    /// middle button or Space with the left button drags the view. It scrolls itself (IScrollInfo) in a ScrollViewer.
    /// </summary>
    public sealed class DocumentCanvas : FrameworkElement, IScrollInfo
    {
        // Room around the canvas, in screen pixels, so handles at the edge can be grabbed
        private const double CanvasMargin = 48;
        private const double HandleSize = 8;
        private const double WheelStep = 48;

        public static readonly DependencyProperty TabProperty = DependencyProperty.Register(
            nameof(Tab), typeof(DocumentTabViewModel), typeof(DocumentCanvas), new FrameworkPropertyMetadata(null, (d, e) => ((DocumentCanvas)d).TabReplaced((DocumentTabViewModel)e.OldValue, (DocumentTabViewModel)e.NewValue)));

        private readonly TextBox _textBox;
        private Size _viewport;
        private Vector _offset;
        private Point? _panFrom;
        private Vector _panOffset;
        private bool _toolDragging;
        private bool _spaceDown;
        private bool _fitPending = true;

        public DocumentCanvas()
        {
            Focusable = true;
            FocusVisualStyle = null;
            ClipToBounds = true;
            _textBox = new TextBox
            {
                AcceptsReturn = true,
                AcceptsTab = false,
                TextWrapping = TextWrapping.Wrap,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                Visibility = Visibility.Collapsed
            };
            _textBox.SetBinding(Control.BorderBrushProperty, new System.Windows.Data.Binding(nameof(ThemeManager.AccentBrush)) { Source = ThemeManager.Instance });
            _textBox.PreviewKeyDown += TextBoxKeyDown;
            _textBox.LostKeyboardFocus += (_, _) => EndTextEdit(true);
            AddVisualChild(_textBox);
            AddLogicalChild(_textBox);
            ThemeManager.Instance.PaletteChanged += () => _ = Dispatcher.BeginInvoke(new Action(InvalidateVisual));
        }

        public DocumentTabViewModel Tab
        {
            get => (DocumentTabViewModel)GetValue(TabProperty);
            set => SetValue(TabProperty, value);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _textBox;

        private void TabReplaced(DocumentTabViewModel before, DocumentTabViewModel after)
        {
            if (before != null)
            {
                EndTextEdit(true);
                before.ToolStateChanged -= InvalidateVisual;
                before.TextEditRequested -= BeginTextEdit;
                before.PropertyChanged -= TabPropertyChanged;
                before.Composition.AreaChanged -= BitmapReplaced;
                before.Composition.Changed -= InvalidateVisual;
                before.ScrollOffset = _offset;
            }

            if (after != null)
            {
                after.ToolStateChanged += InvalidateVisual;
                after.TextEditRequested += BeginTextEdit;
                after.PropertyChanged += TabPropertyChanged;
                after.Composition.AreaChanged += BitmapReplaced;
                after.Composition.Changed += InvalidateVisual;
                _offset = after.ScrollOffset;
                _fitPending = !after.HasBeenShown;
                after.HasBeenShown = true;
            }

            UpdateScrolling();
            InvalidateVisual();
        }

        private void TabPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DocumentTabViewModel.Zoom))
            {
                RenderOptions.SetBitmapScalingMode(this, Tab.Zoom >= 2 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
                UpdateScrolling();
                InvalidateArrange();
                InvalidateVisual();
            }
            else if (e.PropertyName == nameof(DocumentTabViewModel.ToolKind))
            {
                UpdateCursor();
            }
        }

        private void BitmapReplaced()
        {
            UpdateScrolling();
            InvalidateArrange();
            InvalidateVisual();
        }

        private double Zoom => Tab?.Zoom ?? 1;

        private RectD Area => Tab?.Composition.Area ?? default;

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
        /// Raised after the canvas was drawn: the zoom, the scroll position or the document may have changed
        /// </summary>
        public event Action ViewChanged;

        /// <summary>
        /// The visible part of the canvas, in pixels from its top left corner
        /// </summary>
        public Rect VisibleArea
        {
            get
            {
                var area = Area;
                var topLeft = ToCanvas(new Point(0, 0));
                var bottomRight = ToCanvas(new Point(_viewport.Width, _viewport.Height));
                var visible = new Rect(new Point(topLeft.X - area.X, topLeft.Y - area.Y), new Point(bottomRight.X - area.X, bottomRight.Y - area.Y));
                visible.Intersect(new Rect(0, 0, area.Width, area.Height));
                return visible;
            }
        }

        /// <summary>
        /// True when not all of the canvas is visible
        /// </summary>
        public bool CanPan => Tab != null && (Area.Width * Zoom + 2 * CanvasMargin > _viewport.Width + 0.5 || Area.Height * Zoom + 2 * CanvasMargin > _viewport.Height + 0.5);

        /// <summary>
        /// Scrolls so the point (in pixels from the top left corner of the canvas) is in the middle
        /// </summary>
        public void CenterOn(Point point)
        {
            var area = Area;
            var screen = ToScreen(new PointD(point.X + area.X, point.Y + area.Y));
            SetOffset(_offset + (screen - new Point(_viewport.Width / 2, _viewport.Height / 2)));
        }

        private IReadOnlyList<CompositionPiece> _overviewPieces;
        private DrawingBrush _overviewBrush;

        /// <summary>
        /// The bitmaps of the document as one brush, for the overview
        /// </summary>
        public Brush OverviewBrush
        {
            get
            {
                var pieces = Tab?.Composition.Pieces;
                if (pieces == null)
                {
                    return null;
                }

                if (!ReferenceEquals(pieces, _overviewPieces))
                {
                    _overviewPieces = pieces;
                    var area = Area;
                    var group = new DrawingGroup();
                    // The whole canvas, also where nothing is drawn, so the brush has its size
                    group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, area.Width, area.Height))));
                    if (Tab.Document.Canvas.Background is { A: > 0 } background)
                    {
                        group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromArgb(background.A, background.R, background.G, background.B)), null,
                            new RectangleGeometry(new Rect(0, 0, area.Width, area.Height))));
                    }

                    foreach (var piece in pieces)
                    {
                        group.Children.Add(new ImageDrawing(piece.Bitmap, new Rect(piece.Area.X - area.X, piece.Area.Y - area.Y, piece.Area.Width, piece.Area.Height)));
                    }

                    // Not frozen: the bitmaps are written again when their elements change
                    group.ClipGeometry = new RectangleGeometry(new Rect(0, 0, area.Width, area.Height));
                    _overviewBrush = new DrawingBrush(group) { Stretch = Stretch.Fill };
                }

                return _overviewBrush;
            }
        }

        /// <summary>
        /// The middle of what is visible, in canvas pixels: where a pasted image goes
        /// </summary>
        public PointD VisibleCenter => ToCanvas(new Point(_viewport.Width / 2, _viewport.Height / 2));

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
            // The canvas point under the mouse stays under the mouse
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
            _textBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
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
                    ZoomBestFit();
                }

                UpdateScrolling();
            }

            ArrangeTextBox();
            return finalSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            var theme = ThemeManager.Instance;
            dc.DrawRectangle(theme.ScrollBarTrackBrush, null, new Rect(_viewport));
            if (Tab == null)
            {
                ViewChanged?.Invoke();
                return;
            }

            var area = Area;
            var origin = Origin;
            var page = new Rect(origin.X, origin.Y, area.Width * Zoom, area.Height * Zoom);
            // A soft shadow under the page, then the checkerboard for transparent pixels, the background of the canvas and the
            // bitmaps of the elements, which WPF blends on the graphics card
            dc.DrawRectangle(ShadowBrush, null, new Rect(page.X + 2, page.Y + 2, page.Width + 2, page.Height + 2));
            dc.DrawRectangle(Checkerboard, null, page);
            if (Tab.Document.Canvas.Background is { A: > 0 } background)
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(background.A, background.R, background.G, background.B)), null, page);
            }

            dc.PushClip(new RectangleGeometry(page));
            foreach (var piece in Tab.Composition.Pieces)
            {
                var at = piece.Area;
                dc.DrawImage(piece.Bitmap, new Rect(origin.X + (at.X - area.X) * Zoom, origin.Y + (at.Y - area.Y) * Zoom, at.Width * Zoom, at.Height * Zoom));
            }

            dc.Pop();
            DrawAdorners(dc);
            ViewChanged?.Invoke();
        }

        private static readonly Brush ShadowBrush = Frozen(new SolidColorBrush(Color.FromArgb(48, 0, 0, 0)));

        private static readonly Brush Checkerboard = CreateCheckerboard();

        private static Brush CreateCheckerboard()
        {
            var light = Frozen(new SolidColorBrush(Color.FromRgb(255, 255, 255)));
            var dark = Frozen(new SolidColorBrush(Color.FromRgb(214, 214, 214)));
            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing(light, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
            group.Children.Add(new GeometryDrawing(dark, null, new RectangleGeometry(new Rect(0, 0, 8, 8))));
            group.Children.Add(new GeometryDrawing(dark, null, new RectangleGeometry(new Rect(8, 8, 8, 8))));
            return Frozen(new DrawingBrush(group) { TileMode = TileMode.Tile, Viewport = new Rect(0, 0, 16, 16), ViewportUnits = BrushMappingMode.Absolute });
        }

        private static T Frozen<T>(T freezable) where T : Freezable
        {
            freezable.Freeze();
            return freezable;
        }

        private void DrawAdorners(DrawingContext dc)
        {
            var theme = ThemeManager.Instance;
            var accent = theme.AccentBrush;
            var outline = new Pen(accent, 1);
            var contrast = new Pen(Brushes.White, 3);
            var handleFill = Brushes.White;
            var adorners = Tab.Tool.Adorners;
            if (Tab.Tool is not SelectTool)
            {
                // A drawing tool keeps the element it just drew selected: its outline shows which one the panel changes
                adorners = adorners.Concat(Tab.SelectedElements.Select(element => ElementShapes.AreaOf(element) is RectD box
                    ? new Adorner(AdornerKind.Outline, box, ElementShapes.CanTurn(element) ? element.Rotation : 0)
                    : new Adorner(AdornerKind.Outline, element.Bounds))).ToList();
            }

            foreach (var adorner in adorners)
            {
                switch (adorner.Kind)
                {
                    case AdornerKind.Outline:
                        var corners = Corners(adorner.Area, adorner.Rotation);
                        var geometry = Polygon(corners);
                        dc.DrawGeometry(null, contrast, geometry);
                        dc.DrawGeometry(null, outline, geometry);
                        break;
                    case AdornerKind.Handle:
                        var point = Snap(ToScreen(adorner.Point));
                        dc.DrawRectangle(handleFill, outline, new Rect(point.X - HandleSize / 2, point.Y - HandleSize / 2, HandleSize, HandleSize));
                        break;
                    case AdornerKind.RotateHandle:
                        var to = ToScreen(adorner.To);
                        var at = ToScreen(adorner.Point);
                        dc.DrawLine(outline, to, at);
                        dc.DrawEllipse(accent, new Pen(Brushes.White, 1.5), at, HandleSize / 2 + 1, HandleSize / 2 + 1);
                        break;
                    case AdornerKind.PointHandle:
                        dc.DrawEllipse(handleFill, new Pen(accent, 1.5), ToScreen(adorner.Point), HandleSize / 2 + 1, HandleSize / 2 + 1);
                        break;
                    case AdornerKind.RubberBand:
                        var band = ScreenRect(adorner.Area);
                        var fill = accent.CloneCurrentValue();
                        fill.Opacity = 0.15;
                        dc.DrawRectangle(fill, outline, band);
                        break;
                    case AdornerKind.CropArea:
                        DrawCropArea(dc, ScreenRect(adorner.Area), outline);
                        break;
                }
            }
        }

        private void DrawCropArea(DrawingContext dc, Rect keep, Pen outline)
        {
            var area = Area;
            var page = new Rect(ToScreen(new PointD(area.X, area.Y)), new Size(area.Width * Zoom, area.Height * Zoom));
            var dim = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(page), new RectangleGeometry(keep));
            dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)), null, dim);
            var dashed = new Pen(Brushes.White, 1) { DashStyle = DashStyles.Dash };
            dc.DrawRectangle(null, outline, keep);
            dc.DrawRectangle(null, dashed, keep);
            // Thirds, as a help to place the crop
            var thirds = new Pen(new SolidColorBrush(Color.FromArgb(96, 255, 255, 255)), 1);
            for (int i = 1; i < 3; i++)
            {
                dc.DrawLine(thirds, new Point(keep.X + keep.Width * i / 3, keep.Y), new Point(keep.X + keep.Width * i / 3, keep.Bottom));
                dc.DrawLine(thirds, new Point(keep.X, keep.Y + keep.Height * i / 3), new Point(keep.Right, keep.Y + keep.Height * i / 3));
            }
        }

        private Point[] Corners(RectD area, double rotation)
        {
            var corners = new[] { new PointD(area.X, area.Y), new PointD(area.Right, area.Y), new PointD(area.Right, area.Bottom), new PointD(area.X, area.Bottom) };
            return corners.Select(corner => Snap(ToScreen(rotation == 0 ? corner : corner.Turned(rotation, area.Center)))).ToArray();
        }

        private static Geometry Polygon(Point[] points)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(points[0], false, true);
                context.PolyLineTo(points.Skip(1).ToArray(), true, false);
            }

            geometry.Freeze();
            return geometry;
        }

        private Rect ScreenRect(RectD area) => new(Snap(ToScreen(new PointD(area.X, area.Y))), Snap(ToScreen(new PointD(area.Right, area.Bottom))));

        // Lines of one pixel are sharp in the middle of a pixel
        private static Point Snap(Point point) => new(Math.Round(point.X) + 0.5, Math.Round(point.Y) + 0.5);

        private ToolPointer PointerOf(MouseEventArgs e, int clickCount = 1)
        {
            var modifiers = Keyboard.Modifiers;
            return new ToolPointer(ToCanvas(e.GetPosition(this)), modifiers.HasFlag(ModifierKeys.Shift), modifiers.HasFlag(ModifierKeys.Control), modifiers.HasFlag(ModifierKeys.Alt), 1 / Zoom, clickCount);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (Tab == null)
            {
                return;
            }

            Focus();
            if (e.ChangedButton == MouseButton.Middle || (e.ChangedButton == MouseButton.Left && _spaceDown))
            {
                _panFrom = e.GetPosition(this);
                _panOffset = _offset;
                Cursor = Cursors.SizeAll;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            EndTextEdit(true);
            FlushMove();
            _toolDragging = true;
            CaptureMouse();
            Tab.Tool.Down(PointerOf(e, e.ClickCount));
            UpdateCursor();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Tab == null)
            {
                return;
            }

            if (_panFrom is Point from)
            {
                SetOffset(_panOffset - (e.GetPosition(this) - from));
                return;
            }

            // Only the last position counts: the tool gets it when the input and the drawing of the window are done, so moves
            // which come in faster than a change can be drawn don't pile up
            _pendingPointer = PointerOf(e);
            _pendingPosition = e.GetPosition(this);
            if (!_moveScheduled)
            {
                _moveScheduled = true;
                _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(FlushMove));
            }
        }

        private ToolPointer? _pendingPointer;
        private Point _pendingPosition;
        private bool _moveScheduled;

        /// <summary>
        /// Gives the tool the last position of the mouse, before a button changes too
        /// </summary>
        private void FlushMove()
        {
            _moveScheduled = false;
            if (_pendingPointer is not ToolPointer pointer || Tab == null)
            {
                _pendingPointer = null;
                return;
            }

            _pendingPointer = null;
            if (_toolDragging)
            {
                Tab.Tool.Move(pointer);
                ScrollIntoView(_pendingPosition);
            }
            else
            {
                Tab.Tool.Hover(pointer);
            }

            UpdateCursor();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (_panFrom != null && (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Left))
            {
                _panFrom = null;
                ReleaseMouseCapture();
                UpdateCursor();
                return;
            }

            if (_toolDragging && e.ChangedButton == MouseButton.Left)
            {
                FlushMove();
                _toolDragging = false;
                ReleaseMouseCapture();
                Tab?.Tool.Up(PointerOf(e));
                UpdateCursor();
                e.Handled = true;
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            _panFrom = null;
            if (_toolDragging)
            {
                // Lost while dragging (e.g. Alt+Tab): the change is dropped
                _toolDragging = false;
                Tab?.Tool.Cancel();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (Tab == null)
            {
                return;
            }

            var modifiers = Keyboard.Modifiers;
            if (modifiers.HasFlag(ModifierKeys.Control))
            {
                ZoomTo(NextZoom(Tab.Zoom, e.Delta > 0), e.GetPosition(this));
            }
            else if (modifiers.HasFlag(ModifierKeys.Shift))
            {
                SetOffset(_offset + new Vector(-Math.Sign(e.Delta) * WheelStep * 2, 0));
            }
            else
            {
                SetOffset(_offset + new Vector(0, -Math.Sign(e.Delta) * WheelStep * 2));
            }

            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Space && !_spaceDown && !_toolDragging)
            {
                _spaceDown = true;
                Cursor = Cursors.Hand;
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

        private void UpdateCursor()
        {
            if (Tab == null || _panFrom != null)
            {
                return;
            }

            Cursor = Tab.Tool.Cursor switch
            {
                ToolCursor.Cross => Cursors.Cross,
                ToolCursor.Move => Cursors.SizeAll,
                ToolCursor.SizeNorthSouth => Cursors.SizeNS,
                ToolCursor.SizeWestEast => Cursors.SizeWE,
                ToolCursor.SizeNorthWestSouthEast => Cursors.SizeNWSE,
                ToolCursor.SizeNorthEastSouthWest => Cursors.SizeNESW,
                ToolCursor.Rotate => Cursors.Hand,
                ToolCursor.Pen => Cursors.Pen,
                ToolCursor.Text => Cursors.IBeam,
                _ => Cursors.Arrow
            };
        }

        /// <summary>
        /// While dragging near the edge the view follows the mouse
        /// </summary>
        private void ScrollIntoView(Point point)
        {
            const double edge = 16;
            var delta = new Vector(point.X < edge ? -WheelStep / 4 : point.X > _viewport.Width - edge ? WheelStep / 4 : 0,
                point.Y < edge ? -WheelStep / 4 : point.Y > _viewport.Height - edge ? WheelStep / 4 : 0);
            if (delta.Length > 0)
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
            _textBox.FontFamily = new FontFamily(string.Join(", ", TextStyle.Families(style.Family).DefaultIfEmpty("Segoe UI")));
            _textBox.FontWeight = FontWeight.FromOpenTypeWeight(Math.Max(1, Math.Min(999, style.Weight ?? 400)));
            _textBox.FontStyle = style.Italic == true ? FontStyles.Italic : FontStyles.Normal;
            _textBox.Foreground = new SolidColorBrush(PropertiesViewModel.ToColor(style.Color ?? new Rgba(0, 0, 0, 255)));
            _textBox.TextAlignment = style.Horizontal switch
            {
                Document.TextAlignment.Start => System.Windows.TextAlignment.Left,
                Document.TextAlignment.End => System.Windows.TextAlignment.Right,
                _ => System.Windows.TextAlignment.Center
            };
            _textBox.Tag = (element, style.Size ?? 11);
            _textBox.Visibility = Visibility.Visible;
            InvalidateArrange();
            _textBox.Focus();
            _textBox.SelectAll();
        }

        private void ArrangeTextBox()
        {
            if (_textBox.Visibility != Visibility.Visible || Tab == null || _textBox.Tag is not (Element element, double size))
            {
                _textBox.Arrange(new Rect(0, 0, 0, 0));
                return;
            }

            var (area, rotation) = ElementText.EditAreaOf(Tab.Committed, element);
            var topLeft = ToScreen(new PointD(area.X, area.Y));
            var rect = new Rect(topLeft.X, topLeft.Y, Math.Max(40, area.Width * Zoom), Math.Max(size * Zoom * 1.6, area.Height * Zoom));
            // The renderer draws the size in canvas pixels
            _textBox.FontSize = Math.Max(1, size * Zoom);
            _textBox.RenderTransformOrigin = new Point(0.5, 0.5);
            _textBox.RenderTransform = rotation == 0 ? Transform.Identity : new RotateTransform(rotation);
            _textBox.Arrange(rect);
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                EndTextEdit(false);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                EndTextEdit(true);
                e.Handled = true;
            }
        }

        private void EndTextEdit(bool keep)
        {
            if (_textBox.Visibility != Visibility.Visible)
            {
                return;
            }

            _textBox.Visibility = Visibility.Collapsed;
            _textBox.Tag = null;
            Tab?.EndTextEdit(_textBox.Text.Replace("\r\n", "\n"), keep);
            Focus();
        }

        // IScrollInfo: the extent is the canvas at the zoom with the margin around it
        public bool CanVerticallyScroll { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public double ExtentWidth => Area.Width * Zoom + 2 * CanvasMargin;

        public double ExtentHeight => Area.Height * Zoom + 2 * CanvasMargin;

        public double ViewportWidth => _viewport.Width;

        public double ViewportHeight => _viewport.Height;

        public double HorizontalOffset => _offset.X;

        public double VerticalOffset => _offset.Y;

        public ScrollViewer ScrollOwner { get; set; }

        public void LineUp() => SetOffset(_offset + new Vector(0, -WheelStep));

        public void LineDown() => SetOffset(_offset + new Vector(0, WheelStep));

        public void LineLeft() => SetOffset(_offset + new Vector(-WheelStep, 0));

        public void LineRight() => SetOffset(_offset + new Vector(WheelStep, 0));

        public void PageUp() => SetOffset(_offset + new Vector(0, -_viewport.Height));

        public void PageDown() => SetOffset(_offset + new Vector(0, _viewport.Height));

        public void PageLeft() => SetOffset(_offset + new Vector(-_viewport.Width, 0));

        public void PageRight() => SetOffset(_offset + new Vector(_viewport.Width, 0));

        public void MouseWheelUp() => LineUp();

        public void MouseWheelDown() => LineDown();

        public void MouseWheelLeft() => LineLeft();

        public void MouseWheelRight() => LineRight();

        public void SetHorizontalOffset(double offset) => SetOffset(new Vector(offset, _offset.Y));

        public void SetVerticalOffset(double offset) => SetOffset(new Vector(_offset.X, offset));

        public Rect MakeVisible(Visual visual, Rect rectangle) => rectangle;

        private void SetOffset(Vector offset)
        {
            var clamped = new Vector(Math.Max(0, Math.Min(offset.X, ExtentWidth - _viewport.Width)), Math.Max(0, Math.Min(offset.Y, ExtentHeight - _viewport.Height)));
            if (clamped == _offset)
            {
                return;
            }

            _offset = clamped;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateArrange();
            InvalidateVisual();
        }

        private void UpdateScrolling()
        {
            _offset = new Vector(Math.Max(0, Math.Min(_offset.X, ExtentWidth - _viewport.Width)), Math.Max(0, Math.Min(_offset.Y, ExtentHeight - _viewport.Height)));
            ScrollOwner?.InvalidateScrollInfo();
        }
    }
}
