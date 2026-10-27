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

using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.RecipeEditor.ViewModels;
using RePoint = Greenshot.Linux.RecipeEditor.Point;
using ReSize = Greenshot.Linux.RecipeEditor.Size;

namespace Greenshot.Linux.RecipeEditing;

/// <summary>
/// The canvas of the recipe editor, Nodify on Windows: the steps as cards which are moved with the mouse, the pins
/// which are connected by dragging from one to the other, and the arrows between them. Right or middle drag (or Space
/// and left drag, or a left drag on the empty canvas) pans, the wheel scrolls (Shift sideways) and Ctrl + wheel zooms.
/// A click on an arrow selects it, a right click on a step or an arrow opens its menu.
/// </summary>
public sealed class RecipeGraphCanvas : Panel
{
    /// <summary>
    /// The size of the world the steps live in: the layout keeps them at positive coordinates
    /// </summary>
    private const double WorldSize = 20000;

    private const double MinZoom = 0.3;
    private const double MaxZoom = 2.0;
    private const double DragThreshold = 4;

    public static readonly StyledProperty<IDataTemplate?> NodeTemplateProperty =
        AvaloniaProperty.Register<RecipeGraphCanvas, IDataTemplate?>(nameof(NodeTemplate));

    private readonly GridLayer _grid;
    private readonly Canvas _world;
    private readonly ConnectionLayer _connections;
    private readonly ItemsControl _nodes;
    private readonly MatrixTransform _transform = new();
    private readonly List<PortPin> _pins = new();

    private RecipeEditorViewModel? _viewModel;
    private double _zoom = 1;
    private Vector _pan;

    // What the pressed mouse button does
    private enum DragMode { None, Pan, Node, Connect }

    private DragMode _drag;
    private global::Avalonia.Point _pressScreen;
    private global::Avalonia.Point _pressWorld;
    private Vector _panAtPress;
    private RePoint _nodeAtPress;
    private StepNodeViewModel? _dragNode;
    private bool _moved;
    private bool _rightPressed;
    private bool _spaceDown;

    public RecipeGraphCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
        Background = Brushes.Transparent;

        _connections = new ConnectionLayer(this) { Width = WorldSize, Height = WorldSize, IsHitTestVisible = false };
        _nodes = new ItemsControl
        {
            Width = WorldSize,
            Height = WorldSize,
            ItemsPanel = new FuncTemplate<Panel?>(() => new Canvas()),
        };
        _world = new Canvas
        {
            Width = WorldSize,
            Height = WorldSize,
            RenderTransformOrigin = RelativePoint.TopLeft,
            RenderTransform = _transform,
            // Larger than the canvas: kept at the top left corner, not centered
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        _world.Children.Add(_connections);
        _world.Children.Add(_nodes);
        _grid = new GridLayer(this) { IsHitTestVisible = false };
        Children.Add(_grid);
        Children.Add(_world);
        _world.LayoutUpdated += (_, _) => UpdateAnchorsAndSizes();
        _nodes.ContainerPrepared += (_, e) => PlaceContainer(e.Container);
    }

    public IDataTemplate? NodeTemplate
    {
        get => GetValue(NodeTemplateProperty);
        set => SetValue(NodeTemplateProperty, value);
    }

    internal RecipeEditorViewModel? ViewModel => _viewModel;

    internal double Zoom => _zoom;

    internal Canvas World => _world;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NodeTemplateProperty)
        {
            _nodes.ItemTemplate = NodeTemplate;
        }
        else if (change.Property == DataContextProperty)
        {
            Attach(DataContext as RecipeEditorViewModel);
        }
    }

    private void Attach(RecipeEditorViewModel? viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.Connections.CollectionChanged -= OnConnectionsChanged;
            _viewModel.PendingConnection.PropertyChanged -= OnPendingChanged;
            _viewModel.PropertyChanged -= OnViewModelChanged;
            foreach (var connection in _viewModel.Connections)
            {
                connection.PropertyChanged -= OnConnectionChanged;
            }
        }

        _viewModel = viewModel;
        _nodes.ItemsSource = viewModel?.Nodes;
        if (viewModel != null)
        {
            viewModel.Connections.CollectionChanged += OnConnectionsChanged;
            viewModel.PendingConnection.PropertyChanged += OnPendingChanged;
            viewModel.PropertyChanged += OnViewModelChanged;
            foreach (var connection in viewModel.Connections)
            {
                connection.PropertyChanged += OnConnectionChanged;
            }
        }

        _connections.InvalidateVisual();
    }

    private void OnConnectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (StepConnectionViewModel connection in e.OldItems)
            {
                connection.PropertyChanged -= OnConnectionChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (StepConnectionViewModel connection in e.NewItems)
            {
                connection.PropertyChanged += OnConnectionChanged;
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset && _viewModel != null)
        {
            foreach (var connection in _viewModel.Connections)
            {
                connection.PropertyChanged -= OnConnectionChanged;
                connection.PropertyChanged += OnConnectionChanged;
            }
        }

        _connections.InvalidateVisual();
    }

    private void OnConnectionChanged(object? sender, PropertyChangedEventArgs e) => _connections.InvalidateVisual();

    private void OnPendingChanged(object? sender, PropertyChangedEventArgs e) => _connections.InvalidateVisual();

    /// <summary>
    /// A step selected outside the canvas (a new one, from the inspector) is scrolled into view
    /// </summary>
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RecipeEditorViewModel.SelectedNode) && _viewModel?.SelectedNode is { } node && _drag == DragMode.None)
        {
            // After the auto-layout placed it
            Avalonia.Threading.Dispatcher.UIThread.Post(() => EnsureVisible(node), Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    private void EnsureVisible(StepNodeViewModel node)
    {
        double width = Math.Max(node.Size.Width, 220), height = Math.Max(node.Size.Height, 80);
        var topLeft = new global::Avalonia.Point(node.Location.X * _zoom + _pan.X, node.Location.Y * _zoom + _pan.Y);
        var bottomRight = new global::Avalonia.Point(topLeft.X + width * _zoom, topLeft.Y + height * _zoom);
        const double margin = 30;
        double dx = 0, dy = 0;
        if (bottomRight.X > Bounds.Width - margin) dx = Bounds.Width - margin - bottomRight.X;
        if (topLeft.X + dx < margin) dx = margin - topLeft.X;
        if (bottomRight.Y > Bounds.Height - margin) dy = Bounds.Height - margin - bottomRight.Y;
        if (topLeft.Y + dy < margin) dy = margin - topLeft.Y;
        if (dx != 0 || dy != 0)
        {
            _pan += new Vector(dx, dy);
            ApplyTransform();
        }
    }

    /// <summary>
    /// The container of a step follows the location of its view model
    /// </summary>
    private static void PlaceContainer(Control container)
    {
        if (container.DataContext is not StepNodeViewModel node)
        {
            return;
        }

        void Place()
        {
            Canvas.SetLeft(container, node.Location.X);
            Canvas.SetTop(container, node.Location.Y);
        }

        Place();
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(StepNodeViewModel.Location))
            {
                Place();
            }
        };
    }

    internal void Register(PortPin pin) => _pins.Add(pin);

    internal void Unregister(PortPin pin) => _pins.Remove(pin);

    /// <summary>
    /// The pins tell their view models where they are, the arrows are drawn between them; the steps their size, for the auto-layout
    /// </summary>
    private void UpdateAnchorsAndSizes()
    {
        bool changed = false;
        foreach (var pin in _pins)
        {
            if (pin.Port == null || !pin.IsEffectivelyVisible)
            {
                continue;
            }

            var center = pin.TranslatePoint(new global::Avalonia.Point(pin.Bounds.Width / 2, pin.Bounds.Height / 2), _world);
            if (center is { } c)
            {
                var anchor = new RePoint(Math.Round(c.X, 1), Math.Round(c.Y, 1));
                if (pin.Port.Anchor != anchor)
                {
                    pin.Port.Anchor = anchor;
                    changed = true;
                }
            }
        }

        foreach (var container in _nodes.GetRealizedContainers())
        {
            if (container.DataContext is StepNodeViewModel node)
            {
                var size = new ReSize(container.Bounds.Width, container.Bounds.Height);
                if (node.Size != size && size.Width > 0)
                {
                    node.Size = size;
                }
            }
        }

        if (changed)
        {
            _connections.InvalidateVisual();
        }
    }

    private void ApplyTransform()
    {
        _transform.Matrix = Matrix.CreateScale(_zoom, _zoom) * Matrix.CreateTranslation(_pan.X, _pan.Y);
        _grid.InvalidateVisual();
    }

    /// <summary>
    /// Shows the steps: scrolled so the top left one is near the top left corner, at 100 %
    /// </summary>
    public void ResetView()
    {
        _zoom = 1;
        if (_viewModel != null && _viewModel.Nodes.Count > 0)
        {
            double left = _viewModel.Nodes.Min(n => n.Location.X);
            double top = _viewModel.Nodes.Min(n => n.Location.Y);
            double width = _viewModel.Nodes.Max(n => n.Location.X + Math.Max(n.Size.Width, 220)) - left;
            // Centered when the steps fit, else from the left
            double x = Bounds.Width > width + 60 ? (Bounds.Width - width) / 2 : 30;
            _pan = new Vector(x - left, 30 - top);
        }
        else
        {
            _pan = default;
        }

        ApplyTransform();
    }

    /// <summary>
    /// The dots of the grid behind the steps, they move with the canvas
    /// </summary>
    private sealed class GridLayer(RecipeGraphCanvas owner) : Control
    {
        public override void Render(DrawingContext context)
        {
            if (!owner.TryFindResource("RE.GridDot", owner.ActualThemeVariant, out var resource) || resource is not IBrush dotBrush)
            {
                return;
            }

            double spacing = 24 * owner._zoom;
            if (spacing < 8)
            {
                return;
            }

            double startX = owner._pan.X % spacing;
            double startY = owner._pan.Y % spacing;
            double radius = Math.Max(0.8, 1.1 * owner._zoom);
            for (double x = startX; x < Bounds.Width; x += spacing)
            {
                for (double y = startY; y < Bounds.Height; y += spacing)
                {
                    context.DrawEllipse(dotBrush, null, new global::Avalonia.Point(x, y), radius, radius);
                }
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space)
        {
            _spaceDown = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key == Key.Space)
        {
            _spaceDown = false;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_viewModel == null)
        {
            return;
        }

        // Clicks into the controls of a card (none yet) are theirs
        if (e.Source is Visual source && source.FindAncestorOfType<Button>(includeSelf: true) != null)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        _pressScreen = point.Position;
        _pressWorld = e.GetPosition(_world);
        _moved = false;
        _rightPressed = point.Properties.IsRightButtonPressed;
        Focus();

        if (point.Properties.IsMiddleButtonPressed || point.Properties.IsRightButtonPressed || (point.Properties.IsLeftButtonPressed && _spaceDown))
        {
            StartPan(e);
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var pin = (e.Source as Visual)?.FindAncestorOfType<PortPin>(includeSelf: true);
        if (pin?.Port != null && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            // Alt + click on a pin removes its arrows, like in Nodify
            _viewModel.DisconnectConnectorCommand.Execute(pin.Port);
            e.Handled = true;
            return;
        }

        if (pin?.Port != null)
        {
            _drag = DragMode.Connect;
            _viewModel.PendingConnection.TargetLocation = new RePoint(_pressWorld.X, _pressWorld.Y);
            _viewModel.PendingConnection.StartedCommand?.Execute(pin.Port);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        var node = NodeAt(e.Source as Visual);
        if (node != null)
        {
            _viewModel.SelectedNode = node;
            _drag = DragMode.Node;
            _dragNode = node;
            _nodeAtPress = node.Location;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        var connection = ConnectionAt(_pressWorld);
        if (connection != null)
        {
            _viewModel.SelectedNode = null;
            _viewModel.SelectedConnection = connection;
            e.Handled = true;
            return;
        }

        // The empty canvas: nothing is selected, a drag pans
        _viewModel.SelectedNode = null;
        _viewModel.SelectedConnection = null;
        StartPan(e);
    }

    private void StartPan(PointerPressedEventArgs e)
    {
        _drag = DragMode.Pan;
        _panAtPress = _pan;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_drag == DragMode.None || _viewModel == null)
        {
            return;
        }

        var screen = e.GetPosition(this);
        if (!_moved && Math.Abs(screen.X - _pressScreen.X) < DragThreshold && Math.Abs(screen.Y - _pressScreen.Y) < DragThreshold)
        {
            return;
        }

        _moved = true;
        switch (_drag)
        {
            case DragMode.Pan:
                _pan = _panAtPress + (screen - _pressScreen);
                ApplyTransform();
                break;
            case DragMode.Node when _dragNode != null:
                var world = e.GetPosition(_world);
                _dragNode.Location = new RePoint(
                    Math.Max(0, Math.Round(_nodeAtPress.X + world.X - _pressWorld.X)),
                    Math.Max(0, Math.Round(_nodeAtPress.Y + world.Y - _pressWorld.Y)));
                break;
            case DragMode.Connect:
                var target = e.GetPosition(_world);
                _viewModel.PendingConnection.TargetLocation = new RePoint(target.X, target.Y);
                _viewModel.PendingConnection.Target = PinAt(screen)?.Port;
                break;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var drag = _drag;
        _drag = DragMode.None;
        e.Pointer.Capture(null);
        if (_viewModel == null)
        {
            return;
        }

        switch (drag)
        {
            case DragMode.Node when _moved:
                _viewModel.NodesDraggedCommand.Execute(null);
                break;
            case DragMode.Connect:
                var target = PinAt(e.GetPosition(this))?.Port;
                _viewModel.PendingConnection.CompletedCommand?.Execute(target);
                break;
        }

        _dragNode = null;
        if (e.InitialPressMouseButton == MouseButton.Right && _rightPressed && !_moved)
        {
            OpenContextMenu(e);
        }

        _rightPressed = false;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_drag == DragMode.Connect && _viewModel != null)
        {
            _viewModel.PendingConnection.CompletedCommand?.Execute(null);
        }

        _drag = DragMode.None;
        _dragNode = null;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var at = e.GetPosition(this);
            double zoom = Math.Clamp(_zoom * (e.Delta.Y > 0 ? 1.1 : 1 / 1.1), MinZoom, MaxZoom);
            // The point under the mouse stays where it is
            var world = (at - _pan) / _zoom;
            _zoom = zoom;
            _pan = at - world * _zoom;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _pan += new Vector((e.Delta.Y + e.Delta.X) * 50, 0);
        }
        else
        {
            _pan += new Vector(e.Delta.X * 50, e.Delta.Y * 50);
        }

        ApplyTransform();
        e.Handled = true;
    }

    /// <summary>
    /// The step whose card holds the visual
    /// </summary>
    private StepNodeViewModel? NodeAt(Visual? visual)
    {
        for (var current = visual; current != null && current != _nodes; current = current.GetVisualParent())
        {
            if (current is Control { DataContext: StepNodeViewModel node } control && control.GetVisualParent() is Canvas canvas && canvas.GetVisualParent() is ItemsPresenter)
            {
                return node;
            }
        }

        return null;
    }

    private PortPin? PinAt(global::Avalonia.Point screen) =>
        (this.InputHitTest(screen) as Visual)?.FindAncestorOfType<PortPin>(includeSelf: true);

    /// <summary>
    /// The arrow near the point (in canvas coordinates), within a few pixels on the screen
    /// </summary>
    private StepConnectionViewModel? ConnectionAt(global::Avalonia.Point world)
    {
        if (_viewModel == null)
        {
            return null;
        }

        double tolerance = 6 / _zoom;
        StepConnectionViewModel? nearest = null;
        double best = tolerance;
        foreach (var connection in _viewModel.Connections)
        {
            double distance = ConnectionLayer.Distance(connection.Source.Anchor, connection.Target.Anchor, world);
            if (distance < best)
            {
                best = distance;
                nearest = connection;
            }
        }

        return nearest;
    }

    /// <summary>
    /// The menu of the step or the arrow under the mouse, after a right click without dragging
    /// </summary>
    private void OpenContextMenu(PointerReleasedEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        var menu = new ContextMenu();
        var node = NodeAt(e.Source as Visual);
        if (node != null)
        {
            _viewModel.SelectedNode = node;
            menu.Items.Add(new MenuItem { Header = Texts.Get("RecipeEditor", "toggle_start_step"), Command = node.ToggleStartNodeCommand });
            menu.Items.Add(new Separator());
            menu.Items.Add(new MenuItem { Header = Texts.Get("RecipeEditor", "delete_step"), Command = node.DeleteNodeCommand });
        }
        else if (ConnectionAt(e.GetPosition(_world)) is { } connection)
        {
            _viewModel.SelectedNode = null;
            _viewModel.SelectedConnection = connection;
            menu.Items.Add(new MenuItem { Header = Texts.Get("RecipeEditor", "delete_connection"), Command = connection.DisconnectCommand });
        }
        else
        {
            menu.Items.Add(new MenuItem { Header = Texts.Get("RecipeEditor", "btn_auto_layout"), Command = _viewModel.AutoLayoutCommand });
            var reset = new MenuItem { Header = "100 %" };
            reset.Click += (_, _) => ResetView();
            menu.Items.Add(reset);
        }

        menu.Open(this);
    }

    /// <summary>
    /// The arrows between the steps, below the cards, and the arrow which is being drawn
    /// </summary>
    private sealed class ConnectionLayer(RecipeGraphCanvas owner) : Control
    {
        private const double ArrowLength = 10;

        public override void Render(DrawingContext context)
        {
            var viewModel = owner.ViewModel;
            if (viewModel == null)
            {
                return;
            }

            var normal = Resource("RE.TextSecondary", Brushes.Gray);
            var accent = Resource("RE.Accent", Brushes.DodgerBlue);
            var badge = Resource("RE.BadgeBackground", Brushes.LightGray);
            var border = Resource("RE.CardBorder", Brushes.Gray);

            foreach (var connection in viewModel.Connections)
            {
                var source = connection.Source.Anchor;
                var target = connection.Target.Anchor;
                if (source == default || target == default)
                {
                    continue;
                }

                IBrush stroke = connection.IsSelected || connection.IsActive ? accent : normal;
                double thickness = connection.IsSelected ? 3 : 2.5;
                if (connection.IsSelected)
                {
                    // A soft outline marks the selected arrow
                    DrawCurve(context, new Pen(new SolidColorBrush(Color.FromArgb(0x44, 0x00, 0xD2, 0xFF)), 9), source, target);
                }

                DrawCurve(context, new Pen(stroke, thickness), source, target);
                DrawArrowHead(context, stroke, target);
                if (!string.IsNullOrEmpty(connection.Label))
                {
                    DrawLabel(context, connection.Label, Middle(source, target), badge, border, accent);
                }
            }

            var pending = viewModel.PendingConnection;
            if (pending.IsVisible && pending.Source != null && pending.Source.Anchor != default)
            {
                var start = pending.Source.Anchor;
                var end = pending.Target?.Anchor is { } snapped && snapped != default ? snapped : pending.TargetLocation;
                var pen = new Pen(accent, 2.5, new DashStyle([4, 3], 0));
                // From an input pin the arrow is drawn the other way round
                if (pending.Source.IsInput)
                {
                    DrawCurve(context, pen, end, start);
                }
                else
                {
                    DrawCurve(context, pen, start, end);
                }
            }
        }

        private IBrush Resource(string key, IBrush fallback) =>
            owner.TryFindResource(key, owner.ActualThemeVariant, out var value) && value is IBrush brush ? brush : fallback;

        private static (RePoint P1, RePoint P2) ControlPoints(RePoint p0, RePoint p3)
        {
            double offset = Math.Max(Math.Abs(p3.Y - p0.Y) / 2, 30);
            return (new RePoint(p0.X, p0.Y + offset), new RePoint(p3.X, p3.Y - offset));
        }

        private static void DrawCurve(DrawingContext context, IPen pen, RePoint p0, RePoint p3)
        {
            var (p1, p2) = ControlPoints(p0, p3);
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                g.BeginFigure(ToAvalonia(p0), false);
                // The curve ends at the tip of the arrow head
                g.CubicBezierTo(ToAvalonia(p1), ToAvalonia(new RePoint(p2.X, p2.Y - ArrowLength)), ToAvalonia(new RePoint(p3.X, p3.Y - ArrowLength)));
                g.EndFigure(false);
            }

            context.DrawGeometry(null, pen, geometry);
        }

        private static void DrawArrowHead(DrawingContext context, IBrush brush, RePoint tip)
        {
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                g.BeginFigure(ToAvalonia(new RePoint(tip.X, tip.Y - 1)), true);
                g.LineTo(ToAvalonia(new RePoint(tip.X - ArrowLength / 2, tip.Y - ArrowLength - 1)));
                g.LineTo(ToAvalonia(new RePoint(tip.X + ArrowLength / 2, tip.Y - ArrowLength - 1)));
                g.EndFigure(true);
            }

            context.DrawGeometry(brush, null, geometry);
        }

        private static void DrawLabel(DrawingContext context, string label, RePoint at, IBrush background, IBrush border, IBrush foreground)
        {
            var text = new FormattedText(label, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold), 10, foreground);
            var rect = new Rect(at.X - text.Width / 2 - 5, at.Y - text.Height / 2 - 2, text.Width + 10, text.Height + 4);
            context.DrawRectangle(background, new Pen(border, 1), rect, 3, 3);
            context.DrawText(text, new global::Avalonia.Point(rect.X + 5, rect.Y + 2));
        }

        private static RePoint Middle(RePoint p0, RePoint p3)
        {
            var (p1, p2) = ControlPoints(p0, p3);
            return Bezier(p0, p1, p2, p3, 0.5);
        }

        private static RePoint Bezier(RePoint p0, RePoint p1, RePoint p2, RePoint p3, double t)
        {
            double u = 1 - t;
            return new RePoint(
                u * u * u * p0.X + 3 * u * u * t * p1.X + 3 * u * t * t * p2.X + t * t * t * p3.X,
                u * u * u * p0.Y + 3 * u * u * t * p1.Y + 3 * u * t * t * p2.Y + t * t * t * p3.Y);
        }

        /// <summary>
        /// The distance of a point to the arrow from <paramref name="p0"/> to <paramref name="p3"/>
        /// </summary>
        public static double Distance(RePoint p0, RePoint p3, global::Avalonia.Point point)
        {
            if (p0 == default || p3 == default)
            {
                return double.MaxValue;
            }

            var (p1, p2) = ControlPoints(p0, p3);
            double best = double.MaxValue;
            var previous = p0;
            for (int i = 1; i <= 32; i++)
            {
                var current = Bezier(p0, p1, p2, p3, i / 32.0);
                best = Math.Min(best, SegmentDistance(previous, current, point));
                previous = current;
            }

            return best;
        }

        private static double SegmentDistance(RePoint a, RePoint b, global::Avalonia.Point p)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double length = dx * dx + dy * dy;
            double t = length == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / length, 0, 1);
            double x = a.X + t * dx - p.X, y = a.Y + t * dy - p.Y;
            return Math.Sqrt(x * x + y * y);
        }

        private static global::Avalonia.Point ToAvalonia(RePoint point) => new(point.X, point.Y);
    }
}

/// <summary>
/// A pin of a step: arrows start at an output pin and end at an input pin. It tells its view model where it is.
/// </summary>
public sealed class PortPin : Control
{
    public static readonly StyledProperty<StepPortViewModel?> PortProperty =
        AvaloniaProperty.Register<PortPin, StepPortViewModel?>(nameof(Port));

    private RecipeGraphCanvas? _canvas;

    static PortPin()
    {
        AffectsRender<PortPin>(PortProperty);
    }

    public PortPin()
    {
        Width = 18;
        Height = 18;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public StepPortViewModel? Port
    {
        get => GetValue(PortProperty);
        set => SetValue(PortProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PortProperty)
        {
            if (change.OldValue is StepPortViewModel old)
            {
                old.PropertyChanged -= OnPortChanged;
            }

            if (change.NewValue is StepPortViewModel port)
            {
                port.PropertyChanged += OnPortChanged;
            }
        }
        else if (change.Property == IsPointerOverProperty)
        {
            InvalidateVisual();
        }
    }

    private void OnPortChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StepPortViewModel.IsConnected))
        {
            InvalidateVisual();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _canvas = this.FindAncestorOfType<RecipeGraphCanvas>();
        _canvas?.Register(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _canvas?.Unregister(this);
        _canvas = null;
    }

    public override void Render(DrawingContext context)
    {
        var center = new global::Avalonia.Point(Bounds.Width / 2, Bounds.Height / 2);
        var accent = Resource("RE.Accent", Brushes.DodgerBlue);
        // A soft glow on hover, the pin filled when it is connected
        context.DrawEllipse(Brushes.Transparent, null, center, 9, 9);
        if (IsPointerOver)
        {
            context.DrawEllipse(accent, null, center, 9, 9);
        }

        bool connected = Port?.IsConnected == true;
        var fill = connected ? accent : Resource("RE.CardBackground", Brushes.White);
        var stroke = IsPointerOver ? accent : connected ? Brushes.White : Resource("RE.CardBorder", Brushes.Gray);
        context.DrawEllipse(fill, new Pen(stroke, IsPointerOver ? 2 : 1.5), center, 5, 5);
    }

    private IBrush Resource(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) && value is IBrush brush ? brush : fallback;
}
