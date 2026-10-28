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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Rendering;
using SkiaSharp;

namespace Greenshot.Linux.Editing;

/// <summary>
/// Shows the surface at the zoom and passes the mouse to the <see cref="EditorController"/>.
/// The image with the elements is rendered by the editor library into a bitmap, which Skia draws directly;
/// the selection, the handles and the crop area are drawn on top in screen pixels so they stay sharp at every zoom.
/// </summary>
internal sealed class EditorCanvas : Control
{
    private readonly EditorController _controller;
    private readonly object _compositeLock = new();
    private readonly Dictionary<EditorCursor, Cursor> _cursors = new();
    private SKBitmap? _composite;
    private bool _compositeDirty = true;
    private double _zoom = 1;

    public EditorCanvas(EditorController controller)
    {
        _controller = controller;
        Focusable = true;
        ClipToBounds = true;
        _controller.Surface.Changed += () =>
        {
            _compositeDirty = true;
            InvalidateMeasure();
            InvalidateVisual();
        };
        _controller.StateChanged += InvalidateVisual;
    }

    public EditorSurface Surface => _controller.Surface;

    public double Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Clamp(value, 0.05, 32);
            _controller.Zoom = (float)_zoom;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    /// <summary>
    /// The image pixel under a point of this control
    /// </summary>
    public SKPoint ToImage(Point point) => new((float)(point.X / _zoom), (float)(point.Y / _zoom));

    public Point ToControl(SKPoint point) => new(point.X * _zoom, point.Y * _zoom);

    protected override Size MeasureOverride(Size availableSize) => new(Surface.Width * _zoom, Surface.Height * _zoom);

    public override void Render(DrawingContext context)
    {
        UpdateComposite();
        context.Custom(new CanvasDrawOperation(new Rect(Bounds.Size), this, CaptureOverlay()));
    }

    private void UpdateComposite()
    {
        if (!_compositeDirty)
        {
            return;
        }

        lock (_compositeLock)
        {
            if (_composite == null || _composite.Width != Surface.Width || _composite.Height != Surface.Height)
            {
                _composite?.Dispose();
                _composite = new SKBitmap(new SKImageInfo(Surface.Width, Surface.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
            }

            Surface.Render(_composite, RenderMode.Edit);
            _compositeDirty = false;
        }
    }

    /// <summary>
    /// What to draw over the image, copied so the render thread doesn't read the model while it changes
    /// </summary>
    private Overlay CaptureOverlay()
    {
        var outlines = new List<(SKRect Rect, bool IsLine)>();
        var handles = new List<SKPoint>();
        foreach (var element in Surface.Selection)
        {
            outlines.Add((element.NormalizedBounds, element is LineElement));
            handles.AddRange(element.GetHandles().Select(h => h.Location));
        }

        var cropHandles = _controller.CropHandles.Select(h => h.Location).ToList();
        return new Overlay(outlines, handles, _controller.CropArea, cropHandles, _controller.RubberBand?.Standardized, Surface.Width, Surface.Height,
            _controller.CropMode != CropMode.Default);
    }

    private sealed record Overlay(
        List<(SKRect Rect, bool IsLine)> Outlines,
        List<SKPoint> Handles,
        SKRect? Crop,
        List<SKPoint> CropHandles,
        SKRect? RubberBand,
        int ImageWidth,
        int ImageHeight,
        bool CropOut);

    #region Mouse

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        Focus();
        e.Pointer.Capture(this);
        _controller.PointerPressed(ToImage(point.Position), Modifiers(e.KeyModifiers), e.ClickCount);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var image = ToImage(e.GetPosition(this));
        _controller.PointerMoved(image, Modifiers(e.KeyModifiers));
        Cursor = CursorFor(_controller.CursorAt(image));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        _controller.PointerReleased(ToImage(e.GetPosition(this)), Modifiers(e.KeyModifiers));
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    internal static InputModifiers Modifiers(KeyModifiers keys)
    {
        var modifiers = InputModifiers.None;
        if (keys.HasFlag(KeyModifiers.Shift))
        {
            modifiers |= InputModifiers.Shift;
        }

        if (keys.HasFlag(KeyModifiers.Control))
        {
            modifiers |= InputModifiers.Control;
        }

        if (keys.HasFlag(KeyModifiers.Alt))
        {
            modifiers |= InputModifiers.Alt;
        }

        return modifiers;
    }

    private Cursor CursorFor(EditorCursor cursor)
    {
        if (!_cursors.TryGetValue(cursor, out var result))
        {
            result = new Cursor(cursor switch
            {
                EditorCursor.Cross => StandardCursorType.Cross,
                EditorCursor.Move => StandardCursorType.SizeAll,
                EditorCursor.SizeNorthWestSouthEast => StandardCursorType.TopLeftCorner,
                EditorCursor.SizeNorthEastSouthWest => StandardCursorType.TopRightCorner,
                EditorCursor.SizeNorthSouth => StandardCursorType.SizeNorthSouth,
                EditorCursor.SizeWestEast => StandardCursorType.SizeWestEast,
                EditorCursor.Hand => StandardCursorType.Hand,
                _ => StandardCursorType.Arrow,
            });
            _cursors[cursor] = result;
        }

        return result;
    }

    #endregion

    /// <summary>
    /// Draws with Skia on the render thread
    /// </summary>
    private sealed class CanvasDrawOperation(Rect bounds, EditorCanvas owner, Overlay overlay) : ICustomDrawOperation
    {
        private static readonly SKColor HandleFill = SKColors.White;
        private static readonly SKColor HandleBorder = new(0x20, 0x20, 0x20);
        private static readonly SKColor SelectionColor = new(0x20, 0x70, 0xE0);

        private readonly double _zoom = owner._zoom;

        public Rect Bounds => bounds;

        public bool HitTest(Point p) => bounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose()
        {
        }

        public void Render(ImmediateDrawingContext context)
        {
            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature leaseFeature)
            {
                return;
            }

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            float zoom = (float)_zoom;
            var imageRect = new SKRect(0, 0, overlay.ImageWidth * zoom, overlay.ImageHeight * zoom);

            DrawCheckerboard(canvas, imageRect);
            lock (owner._compositeLock)
            {
                if (owner._composite is { } composite)
                {
                    using var image = SKImage.FromBitmap(composite);
                    // Sharp pixels when zoomed in, smooth when zoomed out
                    var sampling = zoom >= 2
                        ? new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None)
                        : new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
                    canvas.DrawImage(image, imageRect, sampling);
                }
            }

            DrawOverlay(canvas, zoom);
        }

        private void DrawOverlay(SKCanvas canvas, float zoom)
        {
            SKRect Scale(SKRect r) => new(r.Left * zoom, r.Top * zoom, r.Right * zoom, r.Bottom * zoom);

            using var dashed = new SKPaint
            {
                Color = SelectionColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = false,
                PathEffect = SKPathEffect.CreateDash([4, 3], 0),
            };
            foreach (var (rect, isLine) in overlay.Outlines)
            {
                if (!isLine)
                {
                    var scaled = Scale(rect);
                    scaled.Inflate(2, 2);
                    canvas.DrawRect(scaled, dashed);
                }
            }

            if (overlay.Crop is { } crop)
            {
                var scaled = Scale(crop);
                using var dim = new SKPaint { Color = new SKColor(0, 0, 0, 0x90) };
                if (overlay.CropOut)
                {
                    // A crop out removes what is inside
                    canvas.DrawRect(scaled, dim);
                }
                else
                {
                    canvas.Save();
                    canvas.ClipRect(scaled, SKClipOperation.Difference);
                    canvas.DrawRect(new SKRect(0, 0, overlay.ImageWidth * zoom, overlay.ImageHeight * zoom), dim);
                    canvas.Restore();
                }
                using var cropPen = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
                canvas.DrawRect(scaled, cropPen);
                DrawHandles(canvas, overlay.CropHandles, zoom);
            }

            if (overlay.RubberBand is { } band)
            {
                var scaled = Scale(band);
                using var fill = new SKPaint { Color = SelectionColor.WithAlpha(0x30) };
                canvas.DrawRect(scaled, fill);
                canvas.DrawRect(scaled, dashed);
            }

            DrawHandles(canvas, overlay.Handles, zoom);
        }

        private static void DrawHandles(SKCanvas canvas, IEnumerable<SKPoint> handles, float zoom)
        {
            using var fill = new SKPaint { Color = HandleFill, Style = SKPaintStyle.Fill };
            using var border = new SKPaint { Color = HandleBorder, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            foreach (var handle in handles)
            {
                var rect = SKRect.Create(handle.X * zoom - 4, handle.Y * zoom - 4, 8, 8);
                canvas.DrawRect(rect, fill);
                canvas.DrawRect(rect, border);
            }
        }

        /// <summary>
        /// A gray checkerboard below the image, so transparent parts can be seen
        /// </summary>
        private static void DrawCheckerboard(SKCanvas canvas, SKRect rect)
        {
            using var tile = new SKBitmap(16, 16);
            tile.Erase(new SKColor(0xFF, 0xFF, 0xFF));
            using (var tileCanvas = new SKCanvas(tile))
            using (var gray = new SKPaint { Color = new SKColor(0xCC, 0xCC, 0xCC) })
            {
                tileCanvas.DrawRect(0, 0, 8, 8, gray);
                tileCanvas.DrawRect(8, 8, 8, 8, gray);
            }

            using var shader = SKShader.CreateBitmap(tile, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
            using var paint = new SKPaint { Shader = shader };
            canvas.DrawRect(rect, paint);
        }
    }
}
