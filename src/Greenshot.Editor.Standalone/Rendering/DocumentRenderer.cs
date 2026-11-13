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
using System.Globalization;
using System.Linq;
using System.Numerics;
using Greenshot.Document;
using Greenshot.Document.Editing;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TextAlignment = Greenshot.Document.TextAlignment;

namespace Greenshot.Editor.Standalone.Rendering
{
    /// <summary>
    /// Draws a document, or a part of it, into an image with the drawing canvas of ImageSharp.Drawing. The editor shows
    /// what this draws and an export is the same drawing, so both always look the same.
    /// Everything is drawn in the coordinates of the document (floating point, no rounding to pixels): turned elements are
    /// turned paths, shadows are layer effects, highlight and obfuscate work on the pixels below their turned shape and a
    /// frame clips what it frames.
    /// </summary>
    public static class DocumentRenderer
    {
        /// <summary>
        /// The whole canvas of the document: its background, then the visible layers in drawing order
        /// </summary>
        public static Image<Rgba32> Render(GreenshotDocument document, RenderCache cache = null) => Render(document, null, cache);

        /// <summary>
        /// A part of the document, e.g. what an editor changed. The area is in the coordinates of the elements; take it
        /// from <see cref="Affected"/>, so obfuscations and magnifiers which reach into it are drawn as a whole.
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="area">The area to draw, null for the canvas</param>
        /// <param name="cache">Decoded images and glyphs of earlier drawings, null to draw without</param>
        /// <param name="include">The elements to draw, null for all</param>
        public static Image<Rgba32> Render(GreenshotDocument document, RectD? area, RenderCache cache, Func<Element, bool> include = null)
        {
            var placed = Anchors.Apply(document);
            var canvas = placed.CanvasArea();
            var wanted = area ?? canvas;
            // Around a part a margin is drawn too, so a shadow or blur at its edge sees what lies next to it; not past the
            // canvas, where the whole document sees nothing either
            var drawn = wanted;
            if (area != null)
            {
                drawn = wanted.Inflate(ShadowReach);
                if (HitTesting.Contains(canvas, wanted))
                {
                    drawn = RectD.FromPoints(new PointD(Math.Max(canvas.X, drawn.X), Math.Max(canvas.Y, drawn.Y)), new PointD(Math.Min(canvas.Right, drawn.Right), Math.Min(canvas.Bottom, drawn.Bottom)));
                }
            }

            var image = new Image<Rgba32>(Math.Max(1, (int)Math.Ceiling(drawn.Width)), Math.Max(1, (int)Math.Ceiling(drawn.Height)));
            var own = cache == null ? new RenderCache() : null;
            try
            {
                var used = cache ?? own;
                var copied = CopyCapture(placed, drawn, used, image, include);
                Func<Element, bool> drawnByCanvas = copied == null ? include : element => element.Id != copied && (include == null || include(element));
                image.Mutate(context => context.Paint(new DrawingOptions(), used.Text, target => new Pass(placed, drawn, used, target, drawnByCanvas).Draw()));
                if (drawn != wanted)
                {
                    image.Mutate(context => context.Crop(new Rectangle((int)Math.Round(wanted.X - drawn.X), (int)Math.Round(wanted.Y - drawn.Y),
                        Math.Max(1, (int)Math.Ceiling(wanted.Width)), Math.Max(1, (int)Math.Ceiling(wanted.Height)))));
                }
            }
            catch
            {
                image.Dispose();
                throw;
            }
            finally
            {
                own?.Dispose();
            }

            return image;
        }

        /// <summary>
        /// The usual start of a drawing is the capture, shown pixel for pixel below everything else: its pixels are copied
        /// into the image as they are, which is much faster than drawing it. Returns the id of the element which was copied,
        /// null when the first element isn't such an image.
        /// </summary>
        private static Guid? CopyCapture(GreenshotDocument document, RectD drawn, RenderCache cache, Image<Rgba32> image, Func<Element, bool> include)
        {
            if (document.Canvas.Background.A > 0 || drawn.X != Math.Round(drawn.X) || drawn.Y != Math.Round(drawn.Y))
            {
                return null;
            }

            var elements = document.Layers.Where(layer => layer.Visible).SelectMany(layer => layer.Elements).ToList();
            if (elements.Any(element => element is FrameElement { Target: null }))
            {
                return null;
            }

            if (elements.FirstOrDefault(element => include == null || include(element)) is not ImageElement { Source: null, Label: null } first || first.Rotation % 360 != 0)
            {
                return null;
            }

            var area = first.Area;
            if (area.X != Math.Round(area.X) || area.Y != Math.Round(area.Y) || document.FrameOf(first) != null || document.StyleOf(first).Shadow is { Enabled: true } ||
                cache.ImageOf(document, first.Resource) is not Image<Rgba32> source || source.Width != area.Width || source.Height != area.Height)
            {
                return null;
            }

            // The part of the picture which lies in the drawn area, in the pixels of both
            int left = (int)Math.Max(area.X, drawn.X), top = (int)Math.Max(area.Y, drawn.Y);
            int right = (int)Math.Min(area.Right, drawn.X + image.Width), bottom = (int)Math.Min(area.Bottom, drawn.Y + image.Height);
            if (right > left && bottom > top)
            {
                int sourceX = left - (int)area.X, sourceY = top - (int)area.Y, targetX = left - (int)drawn.X, targetY = top - (int)drawn.Y;
                image.ProcessPixelRows(source, (target, from) =>
                {
                    for (int y = 0; y < bottom - top; y++)
                    {
                        from.GetRowSpan(sourceY + y).Slice(sourceX, right - left).CopyTo(target.GetRowSpan(targetY + y).Slice(targetX));
                    }
                });
            }

            return first.Id;
        }

        /// <summary>
        /// The area which has to be drawn again when the pixels in an area changed: the area itself and every obfuscation and
        /// magnifier it touches as a whole, as they work on all pixels below them. With an element which changes the whole
        /// canvas (a highlight which dims or greys everything around it, a frame around everything) it is the canvas.
        /// The result has whole pixels and lies inside the canvas; empty when the area is outside of it.
        /// </summary>
        public static RectD Affected(GreenshotDocument document, RectD changed)
        {
            var canvas = document.CanvasArea();
            var elements = document.Layers.Where(layer => layer.Visible).SelectMany(layer => layer.Elements).ToList();
            if (elements.Any(element => element is HighlightElement { Mode: HighlightMode.Area or HighlightMode.Grayscale } or FrameElement { Target: null }))
            {
                return canvas;
            }

            var filters = elements.Where(element => element is ObfuscateElement or HighlightElement { Mode: HighlightMode.Magnify }).Select(element => element.Bounds.Inflate(1)).ToList();
            bool grown = true;
            while (grown)
            {
                grown = false;
                for (int i = filters.Count - 1; i >= 0; i--)
                {
                    if (HitTesting.Intersects(filters[i], changed))
                    {
                        if (!HitTesting.Contains(changed, filters[i]))
                        {
                            changed = RectD.Union(changed, filters[i]);
                            grown = true;
                        }

                        filters.RemoveAt(i);
                    }
                }
            }

            double left = Math.Max(canvas.X, Math.Floor(changed.X)), top = Math.Max(canvas.Y, Math.Floor(changed.Y));
            double right = Math.Min(canvas.Right, Math.Ceiling(changed.Right)), bottom = Math.Min(canvas.Bottom, Math.Ceiling(changed.Bottom));
            return right > left && bottom > top ? new RectD(left, top, right - left, bottom - top) : default;
        }

        /// <summary>
        /// How far an element can draw outside of its visual bounds: its shadow
        /// </summary>
        public const double ShadowReach = 16;

        /// <summary>
        /// One drawing of an area
        /// </summary>
        private sealed class Pass
        {
            // The editor's shadow, used for what a style doesn't set
            private const double DefaultShadowSize = 6, DefaultShadowDarkness = 0.45;
            private static readonly PointD DefaultShadowOffset = new(2, 2);

            // The arrow head: 4 line widths wide and 6 long, the tip on the end point, the line ends at its base
            private const float HeadWidth = 4, HeadLength = 6;

            private readonly GreenshotDocument _document;
            private readonly RectD _area;
            private readonly RenderCache _cache;
            private readonly DrawingCanvas _canvas;
            private readonly Func<Element, bool> _include;
            private readonly Matrix3x2 _toPixels;
            private readonly Rectangle _all;

            public Pass(GreenshotDocument document, RectD area, RenderCache cache, DrawingCanvas canvas, Func<Element, bool> include)
            {
                _document = document;
                _area = area;
                _cache = cache;
                _canvas = canvas;
                _include = include;
                _toPixels = Matrix3x2.CreateTranslation((float)-area.X, (float)-area.Y);
                _all = canvas.Bounds;
            }

            public void Draw()
            {
                _canvas.Save(Options(Matrix3x2.Identity));
                if (_document.Canvas.Background is { A: > 0 } background)
                {
                    _canvas.Fill(Brushes.Solid(Colour(background)), Box(_area.Inflate(1)));
                }

                var elements = _document.Layers.Where(layer => layer.Visible).SelectMany(layer => layer.Elements)
                    .Where(element => (_include == null || _include(element)) && DrawsInto(element)).ToList();
                Draw(elements, elements.Count);
                _canvas.Restore();
            }

            /// <summary>
            /// The first count elements. A frame without a target frames everything below it: that is drawn inside of it.
            /// </summary>
            private void Draw(List<Element> elements, int count)
            {
                int framing = count == 0 ? -1 : elements.FindLastIndex(count - 1, count, element => element is FrameElement { Target: null });
                if (framing < 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Draw(elements[i]);
                    }

                    return;
                }

                var frame = (FrameElement)elements[framing];
                Framed(frame, frame.Area, 0, () => Draw(elements, framing));
                for (int i = framing + 1; i < count; i++)
                {
                    Draw(elements[i]);
                }
            }

            /// <summary>
            /// True when the element can change pixels in the area: what it covers with its line, shadow and label, a highlight
            /// which changes everything outside of it and a frame without target, which frames all below it
            /// </summary>
            private bool DrawsInto(Element element)
            {
                if (element is HighlightElement { Mode: HighlightMode.Area or HighlightMode.Grayscale } or FrameElement { Target: null })
                {
                    return true;
                }

                var bounds = _document.VisualBounds(element).Inflate(ShadowReach);
                if (_document.FrameOf(element) is FrameElement frame)
                {
                    bounds = RectD.Union(bounds, _document.VisualBounds(frame with { Area = element.Bounds }).Inflate(ShadowReach));
                }

                return HitTesting.Intersects(bounds, _area);
            }

            private void Draw(Element element)
            {
                var style = _document.StyleOf(element);
                switch (element)
                {
                    case HighlightElement highlight:
                        Highlight(highlight);
                        Border(element, highlight.Area, style);
                        return;
                    case ObfuscateElement obfuscate:
                        Obfuscate(obfuscate);
                        Border(element, obfuscate.Area, style);
                        return;
                    case CutMarkElement cutMark:
                        CutMark(cutMark, style);
                        return;
                    case FrameElement:
                        // Drawn with its target, or around everything below it
                        return;
                }

                if (_document.FrameOf(element) is FrameElement framing)
                {
                    var area = ElementShapes.AreaOf(element) ?? element.Bounds;
                    Framed(framing, area, ElementShapes.CanTurn(element) ? element.Rotation : 0, () => Paint(element, style));
                    return;
                }

                Shadowed(style, _document.VisualBounds(element), () => Paint(element, style));
            }

            /// <summary>
            /// What draw draws gets the shadow of the style: it is drawn on a layer of its own, the shadow is the effect of the layer
            /// </summary>
            private void Shadowed(Style style, RectD bounds, Action draw)
            {
                if (style.Shadow is not { Enabled: true } shadow)
                {
                    draw();
                    return;
                }

                double size = shadow.Size ?? DefaultShadowSize;
                var offset = shadow.Offset ?? DefaultShadowOffset;
                var color = Color.FromPixel(new Rgba32(0, 0, 0, (byte)Math.Round(255 * Math.Clamp(shadow.Darkness ?? DefaultShadowDarkness, 0, 1))));
                var effect = new DropShadowLayerEffect(new Point((int)Math.Round(offset.X), (int)Math.Round(offset.Y)), (float)Math.Max(0.1, size / 3), color);
                // The offset is on the canvas, it doesn't turn with the element
                var grown = bounds.Inflate(2);
                int left = (int)Math.Floor(grown.X), top = (int)Math.Floor(grown.Y);
                var layer = new Rectangle(left, top, (int)Math.Ceiling(grown.Right) - left, (int)Math.Ceiling(grown.Bottom) - top);
                _canvas.SaveLayer(new GraphicsOptions(), layer, effect);
                draw();
                _canvas.Restore();
            }

            /// <summary>
            /// The element itself, without shadow or frame
            /// </summary>
            private void Paint(Element element, Style style)
            {
                switch (element)
                {
                    case RectangleElement e:
                        Shape(Box(e.Area).Transform(Turn(e.Rotation, e.Area)), style);
                        break;
                    case EllipseElement e:
                        Shape(Ellipse(e.Area).Transform(Turn(e.Rotation, e.Area)), style);
                        break;
                    case TextElement e:
                        Shape(Box(e.Area).Transform(Turn(e.Rotation, e.Area)), style);
                        BoxText(e.Text, style, e.Area, e.Rotation);
                        break;
                    case LineElement e:
                        Line(e.Start, e.End, style.Stroke, ArrowHeads.None);
                        break;
                    case ArrowElement e:
                        Line(e.Start, e.End, style.Stroke, e.Heads);
                        break;
                    case FreehandElement e:
                        Freehand(e, style.Stroke);
                        break;
                    case SpeechBubbleElement e:
                        Shape(Bubble(e), style);
                        BoxText(e.Text, style, e.Area, e.Rotation);
                        break;
                    case StepLabelElement e:
                        StepLabel(e, style);
                        break;
                    case ImageElement e:
                        Picture(e.Resource, e.Area, e.Source, e.Scaling, e.Rotation);
                        break;
                    case PluginElement { Fallback: Guid fallback } e:
                        Picture(fallback, e.Area, null, ImageScaling.Smooth, e.Rotation);
                        break;
                    case CursorElement e:
                        Picture(e.Resource, e.Area, null, ImageScaling.Smooth, 0);
                        break;
                    case EmojiElement e:
                        Emoji(e);
                        break;
                }

                Label(element, style);
            }

            // ---- shapes ----

            private static Color Colour(Rgba color) => Color.FromPixel(new Rgba32(color.R, color.G, color.B, color.A));

            private static IPath Box(RectD box) => new RectanglePolygon((float)box.X, (float)box.Y, (float)box.Width, (float)box.Height);

            private static IPath Ellipse(RectD box) => new EllipsePolygon((float)box.Center.X, (float)box.Center.Y, (float)box.Width, (float)box.Height);

            private static PointF Point(PointD point) => new((float)point.X, (float)point.Y);

            /// <summary>
            /// Clockwise by degrees around the centre of the area
            /// </summary>
            private static Matrix4x4 Turn(double degrees, RectD area) => new(Turn3(degrees, area.Center));

            private static Matrix3x2 Turn3(double degrees, PointD pivot) =>
                degrees % 360 == 0 ? Matrix3x2.Identity : Matrix3x2.CreateRotation((float)(degrees * Math.PI / 180), new Vector2((float)pivot.X, (float)pivot.Y));

            /// <summary>
            /// The options of the canvas for something drawn in the coordinates of the document after the transform
            /// </summary>
            private DrawingOptions Options(Matrix3x2 transform, GraphicsOptions graphics = null) => new()
            {
                Transform = new Matrix4x4(transform * _toPixels),
                GraphicsOptions = graphics ?? new GraphicsOptions()
            };

            /// <summary>
            /// The pixels of the image which cover the area, not cut to the image
            /// </summary>
            private Rectangle Pixels(RectD area)
            {
                int left = (int)Math.Floor(area.X - _area.X), top = (int)Math.Floor(area.Y - _area.Y);
                return new Rectangle(left, top, Math.Max(1, (int)Math.Ceiling(area.Right - _area.X) - left), Math.Max(1, (int)Math.Ceiling(area.Bottom - _area.Y) - top));
            }

            private static float? Width(StrokeStyle stroke) =>
                stroke is { Enabled: not false, Width: > 0 and double width, Color: Rgba { A: > 0 } } ? (float)width : null;

            private static Pen PenOf(Rgba color, float width, LineJoin join = LineJoin.Miter, LineCap cap = LineCap.Butt) =>
                new SolidPen(new PenOptions(Colour(color), width) { StrokeOptions = new StrokeOptions { LineJoin = join, LineCap = cap } });

            /// <summary>
            /// The fill and the line of a closed shape
            /// </summary>
            private void Shape(IPath shape, Style style)
            {
                if (style.Fill is { Enabled: not false, Color: Rgba { A: > 0 } fill })
                {
                    _canvas.Fill(Brushes.Solid(Colour(fill)), shape);
                }

                Stroke(shape, style.Stroke);
            }

            /// <summary>
            /// The line along a closed shape by its position: centred on the outline, inside or outside of it
            /// </summary>
            private void Stroke(IPath shape, StrokeStyle stroke)
            {
                if (Width(stroke) is not float width)
                {
                    return;
                }

                var color = stroke.Color.Value;
                switch (stroke.Position ?? StrokePosition.Center)
                {
                    case StrokePosition.Inside:
                        _canvas.Save();
                        _canvas.Clip(shape);
                        _canvas.Draw(PenOf(color, 2 * width), shape);
                        _canvas.Restore();
                        break;
                    case StrokePosition.Outside:
                        _canvas.Save();
                        _canvas.Clip(ClipOperation.Difference, shape);
                        _canvas.Draw(PenOf(color, 2 * width), shape);
                        _canvas.Restore();
                        break;
                    default:
                        _canvas.Draw(PenOf(color, width), shape);
                        break;
                }
            }

            /// <summary>
            /// A line with its arrow heads as one shape, so a see-through colour isn't drawn twice where they meet
            /// </summary>
            private void Line(PointD start, PointD end, StrokeStyle stroke, ArrowHeads heads)
            {
                if (Width(stroke) is not float width)
                {
                    return;
                }

                Vector2 from = Point(start), to = Point(end);
                float length = Vector2.Distance(from, to);
                if (length <= 0)
                {
                    return;
                }

                var direction = (to - from) / length;
                var parts = new List<IPath>();
                Vector2 lineStart = from, lineEnd = to;
                // A head is never longer than half of the line
                float headLength = Math.Min(HeadLength * width, heads == ArrowHeads.Both ? length / 2 : length);
                if (heads is ArrowHeads.Start or ArrowHeads.Both)
                {
                    parts.Add(Head(from, -direction, width, headLength));
                    lineStart = from + direction * headLength;
                }

                if (heads is ArrowHeads.End or ArrowHeads.Both)
                {
                    parts.Add(Head(to, direction, width, headLength));
                    lineEnd = to - direction * headLength;
                }

                var brush = Brushes.Solid(Colour(stroke.Color.Value));
                if (Vector2.Dot(lineEnd - lineStart, direction) > 0)
                {
                    // A line without heads has round ends, with a head it ends flat in the base of the head
                    var cap = heads == ArrowHeads.None ? LineCap.Round : LineCap.Butt;
                    var body = new Path(new LinearLineSegment(lineStart, lineEnd)).GenerateOutline(width, new StrokeOptions { LineCap = cap });
                    _canvas.Fill(brush, parts.Count == 0 ? body : body.Clip(BooleanOperation.Union, parts));
                    return;
                }

                foreach (var part in parts)
                {
                    _canvas.Fill(brush, part);
                }
            }

            private static IPath Head(Vector2 tip, Vector2 direction, float width, float length)
            {
                var across = new Vector2(-direction.Y, direction.X) * (HeadWidth / 2 * width);
                var headBase = tip - direction * length;
                return new Polygon(new PointF[] { tip, headBase + across, headBase - across });
            }

            /// <summary>
            /// The points the pen went through as one smooth curve (Catmull-Rom), with round ends and joins
            /// </summary>
            private void Freehand(FreehandElement freehand, StrokeStyle stroke)
            {
                if (Width(stroke) is not float width || freehand.Points.Count < 2)
                {
                    return;
                }

                var points = freehand.Points.Select(point => (Vector2)Point(point)).ToList();
                var builder = new PathBuilder();
                builder.MoveTo(points[0]);
                for (int i = 0; i < points.Count - 1; i++)
                {
                    var before = points[Math.Max(0, i - 1)];
                    var after = points[Math.Min(points.Count - 1, i + 2)];
                    builder.CubicBezierTo(points[i] + (points[i + 1] - before) / 6, points[i + 1] - (after - points[i]) / 6, points[i + 1]);
                }

                _canvas.Draw(PenOf(stroke.Color.Value, width, LineJoin.Round, LineCap.Round), builder);
            }

            /// <summary>
            /// The box of a speech bubble with round corners and its tail as one shape. The tail starts in the middle of the
            /// box, which doesn't move when the box turns, and ends on the tail point.
            /// </summary>
            private static IPath Bubble(SpeechBubbleElement bubble)
            {
                var area = bubble.Area;
                float radius = (float)Math.Min(15, Math.Min(area.Width, area.Height) / 4);
                IPath box = radius >= 1
                    ? new RoundedRectanglePolygon((float)area.X, (float)area.Y, (float)area.Width, (float)area.Height, radius)
                    : Box(area);
                box = box.Transform(Turn(bubble.Rotation, area));
                // The tail is as wide as a tenth of width and height together, at most half of either
                double tailWidth = Math.Min((area.Width + area.Height) / 20, Math.Min(area.Width, area.Height) / 2);
                Vector2 center = Point(area.Center), tip = Point(bubble.Tail);
                float length = Vector2.Distance(center, tip);
                if (length <= 0 || tailWidth <= 0)
                {
                    return box;
                }

                var across = new Vector2(-(tip.Y - center.Y), tip.X - center.X) / length * (float)tailWidth;
                var tail = new Polygon(new PointF[] { center + across, center - across, tip });
                return box.Clip(BooleanOperation.Union, tail);
            }

            /// <summary>
            /// A step label: the circle and its number, bold, as big as fits
            /// </summary>
            private void StepLabel(StepLabelElement label, Style style)
            {
                var area = label.Area;
                Shape(Ellipse(area), style);
                string number = _document.LabelOf(label);
                if (string.IsNullOrEmpty(number))
                {
                    return;
                }

                double fits = Math.Min(area.Width, area.Height);
                var textStyle = (style.Text ?? new TextStyle()) with { Size = fits * 0.6, Weight = 700 };
                var options = TextOptions(textStyle, -1);
                var ink = TextMeasurer.MeasureBounds(number, options);
                // More digits make the number smaller, until it fits into the circle
                double widest = fits * 0.7;
                if (ink.Width > widest)
                {
                    textStyle = textStyle with { Size = textStyle.Size * widest / ink.Width };
                    options = TextOptions(textStyle, -1);
                    ink = TextMeasurer.MeasureBounds(number, options);
                }

                // Centred by what is drawn of the digits, not by the line height of the font
                options.Origin = new Vector2((float)(area.Center.X - ink.X - ink.Width / 2), (float)(area.Center.Y - ink.Y - ink.Height / 2));
                var color = style.Text?.Color ?? style.Stroke?.Color ?? new Rgba(255, 255, 255, 255);
                _canvas.DrawText(options, number, Brushes.Solid(Colour(color)), null);
            }

            // ---- pictures and emoji ----

            /// <summary>
            /// An image resource: the part source of it, scaled into the area, turned around its centre
            /// </summary>
            private void Picture(Guid resource, RectD area, RectD? source, ImageScaling scaling, double rotation)
            {
                if (_cache.ImageOf(_document, resource) is not Image<Rgba32> image || area.Width <= 0 || area.Height <= 0)
                {
                    return;
                }

                var from = image.Bounds;
                if (source is RectD part)
                {
                    from = new Rectangle((int)Math.Round(part.X * image.Width), (int)Math.Round(part.Y * image.Height),
                        Math.Max(1, (int)Math.Round(part.Width * image.Width)), Math.Max(1, (int)Math.Round(part.Height * image.Height)));
                    from.Intersect(image.Bounds);
                    if (from.Width <= 0 || from.Height <= 0)
                    {
                        return;
                    }
                }

                // Shown pixel for pixel (the capture, mostly) nothing is resampled
                bool asItIs = rotation % 360 == 0 && area.Width == from.Width && area.Height == from.Height && area.X == Math.Round(area.X) && area.Y == Math.Round(area.Y);
                _canvas.Save(Options(Turn3(rotation, area.Center)));
                _canvas.DrawImage(image, from, new RectangleF((float)area.X, (float)area.Y, (float)area.Width, (float)area.Height),
                    asItIs || scaling == ImageScaling.Pixelated ? KnownResamplers.NearestNeighbor : KnownResamplers.Bicubic);
                _canvas.Restore();
            }

            /// <summary>
            /// An emoji in color, with the Twemoji font of Greenshot, in the middle of its area
            /// </summary>
            private void Emoji(EmojiElement emoji)
            {
                var area = emoji.Area;
                float size = (float)Math.Min(area.Width, area.Height);
                if (string.IsNullOrEmpty(emoji.Emoji) || size <= 0)
                {
                    return;
                }

                var family = EditorFonts.Emoji ?? EditorFonts.FamilyOf(null);
                var font = family.CreateFont(size * 0.95f, FontStyle.Regular);
                var options = new RichTextOptions(font)
                {
                    // The glyphs of the font sit a bit high
                    Origin = new Vector2((float)area.Center.X, (float)area.Center.Y + font.Size * 0.045f),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    ColorFontSupport = ColorFontSupport.ColrV0 | ColorFontSupport.ColrV1 | ColorFontSupport.Svg
                };
                _canvas.Save(Options(Turn3(emoji.Rotation, area.Center)));
                _canvas.DrawText(options, emoji.Emoji, Brushes.Solid(Color.Black), null);
                _canvas.Restore();
            }

            // ---- text ----

            private static RichTextOptions TextOptions(TextStyle style, double wrapWidth)
            {
                var font = EditorFonts.FontOf(style);
                var options = new RichTextOptions(font)
                {
                    WrappingLength = style.Wrap == false || wrapWidth <= 0 ? -1 : (float)wrapWidth,
                    TextAlignment = (style.Horizontal ?? TextAlignment.Center) switch
                    {
                        TextAlignment.Start => SixLabors.Fonts.TextAlignment.Start,
                        TextAlignment.End => SixLabors.Fonts.TextAlignment.End,
                        _ => SixLabors.Fonts.TextAlignment.Center
                    },
                    ColorFontSupport = ColorFontSupport.ColrV0 | ColorFontSupport.ColrV1 | ColorFontSupport.Svg
                };
                if (EditorFonts.Emoji is FontFamily emoji)
                {
                    // An emoji inside a text is drawn like the emoji element
                    options.FallbackFontFamilies = new[] { emoji };
                }

                if (style.LineHeight is double lineHeight)
                {
                    float natural = TextMeasurer.MeasureAdvance("X", new TextOptions(font)).Height;
                    options.LineSpacing = natural > 0 ? (float)(lineHeight * font.Size / natural) : 1;
                }

                return options;
            }

            /// <summary>
            /// The size a text takes with a style, wrapped at a width (or not wrapped without one)
            /// </summary>
            private static FontRectangle Measure(string text, TextStyle style, double wrapWidth) => TextMeasurer.MeasureAdvance(text, TextOptions(style, wrapWidth));

            /// <summary>
            /// The text of a box: inside its line, in the colour of the text or else of the line
            /// </summary>
            private void BoxText(string text, Style style, RectD area, double rotation)
            {
                var textStyle = style.Text ?? new TextStyle();
                if (string.IsNullOrEmpty(text) || textStyle.Enabled == false)
                {
                    return;
                }

                double padding = (Width(style.Stroke) ?? 0) + 2;
                var box = area.Width > 2 * padding && area.Height > 2 * padding ? area.Inflate(-padding) : area;
                Text(text, textStyle, box, Turn3(rotation, area.Center), style.Stroke?.Color);
            }

            /// <summary>
            /// Text in a box, placed by its style, turned (around a point of the document) with the transform
            /// </summary>
            private void Text(string text, TextStyle style, RectD box, Matrix3x2 transform, Rgba? lineColor)
            {
                var options = TextOptions(style, box.Width);
                var size = TextMeasurer.MeasureAdvance(text, options);
                bool wrap = options.WrappingLength > 0;
                // Wrapped text is placed in its wrapping width by the alignment, else the measured text is placed
                double x = wrap ? box.X : (style.Horizontal ?? TextAlignment.Center) switch
                {
                    TextAlignment.Start => box.X,
                    TextAlignment.End => box.Right - size.Width,
                    _ => box.X + (box.Width - size.Width) / 2
                };
                double y = (style.Vertical ?? TextAlignment.Center) switch
                {
                    TextAlignment.Start => box.Y,
                    TextAlignment.End => box.Bottom - size.Height,
                    _ => box.Y + (box.Height - size.Height) / 2
                };
                options.Origin = new Vector2((float)x, (float)y);
                var decorations = (style.Underline == true ? TextDecorations.Underline : TextDecorations.None) | (style.Strikeout == true ? TextDecorations.Strikeout : TextDecorations.None);
                if (decorations != TextDecorations.None)
                {
                    options.TextRuns = new[] { new RichTextRun { Start = 0, End = new StringInfo(text).LengthInTextElements, TextDecorations = decorations } };
                }

                var color = style.Color ?? lineColor ?? new Rgba(0, 0, 0, 255);
                Pen outline = style.Outline is { } stroke && Width(stroke) is float width ? PenOf(stroke.Color.Value, width, LineJoin.Round) : null;
                _canvas.Save(Options(transform));
                if (outline != null)
                {
                    // The outline lies behind the letters, so it doesn't make them thinner
                    _canvas.DrawText(options, text, null, outline);
                }

                _canvas.DrawText(options, text, Brushes.Solid(Colour(color)), null);
                _canvas.Restore();
            }

            /// <summary>
            /// The label of an element, measured in its font, placed and turned with its element
            /// </summary>
            private void Label(Element element, Style style)
            {
                string text = GreenshotDocument.LabelOf(element);
                if (string.IsNullOrEmpty(text) || style.Text?.Enabled == false)
                {
                    return;
                }

                var textStyle = style.Text ?? new TextStyle();
                // A box label wraps in the width of its box, a line label doesn't wrap
                double wrapWidth = GreenshotDocument.LabelPlacement(element, 0, 0)?.Box.Width is double boxWidth && boxWidth > 0 ? boxWidth : -1;
                var lineStyle = wrapWidth > 0 ? textStyle : textStyle with { Wrap = false };
                var size = Measure(text, lineStyle, wrapWidth);
                if (GreenshotDocument.LabelPlacement(element, size.Width, size.Height) is var (box, rotation, pivot))
                {
                    Text(text, lineStyle, box, Turn3(rotation, pivot), style.Stroke?.Color);
                }
            }

            // ---- highlight, obfuscate, cut mark, frame ----

            /// <summary>
            /// The turned shape of a highlight or obfuscate
            /// </summary>
            private static IPath FilterShape(RectD area, double rotation) => Box(area).Transform(Turn(rotation, area));

            /// <summary>
            /// Everything of the drawn area which isn't in the shape
            /// </summary>
            private IPath Outside(IPath shape) => Box(_area.Inflate(2)).Clip(BooleanOperation.Difference, shape);

            /// <summary>
            /// The line around a highlight or obfuscate, with the shadow of its style
            /// </summary>
            private void Border(Element element, RectD area, Style style)
            {
                if (Width(style.Stroke) is float)
                {
                    Shadowed(style, _document.VisualBounds(element), () => Stroke(FilterShape(area, element.Rotation), style.Stroke));
                }
            }

            private void Highlight(HighlightElement highlight)
            {
                var shape = FilterShape(highlight.Area, highlight.Rotation);
                switch (highlight.Mode)
                {
                    case HighlightMode.Text:
                        // Like a marker pen: every colour channel keeps the darker value of the pixel and the marker
                        _canvas.Save(Options(Matrix3x2.Identity, new GraphicsOptions { ColorBlendingMode = PixelColorBlendingMode.Darken }));
                        _canvas.Fill(Brushes.Solid(Colour(highlight.Color with { A = 255 })), shape);
                        _canvas.Restore();
                        break;
                    case HighlightMode.Area:
                        float brightness = (float)Math.Clamp(highlight.Brightness, 0, 2);
                        float blur = highlight.BlurRadius / 2f;
                        _canvas.Apply(Outside(shape), context =>
                        {
                            context.Brightness(brightness);
                            if (blur >= 0.5f)
                            {
                                context.GaussianBlur(blur);
                            }
                        });
                        break;
                    case HighlightMode.Grayscale:
                        _canvas.Apply(Outside(shape), context => context.Grayscale());
                        break;
                    case HighlightMode.Magnify:
                        int factor = Math.Max(1, highlight.Magnification);
                        if (factor > 1)
                        {
                            _canvas.Apply(shape, context =>
                            {
                                // The middle of what is below, as big as the whole
                                var size = context.GetCurrentSize();
                                var middle = new Rectangle(size.Width / 2 - size.Width / factor / 2, size.Height / 2 - size.Height / factor / 2,
                                    Math.Max(1, size.Width / factor), Math.Max(1, size.Height / factor));
                                context.Crop(middle).Resize(size.Width, size.Height, KnownResamplers.NearestNeighbor);
                            });
                        }

                        break;
                }
            }

            private void Obfuscate(ObfuscateElement obfuscate)
            {
                var shape = FilterShape(obfuscate.Area, obfuscate.Rotation);
                if (obfuscate.Mode == ObfuscateMode.Pixelate)
                {
                    _canvas.Apply(shape, context => context.ApplyProcessor(SecurePixelation.Processor(obfuscate.Size, obfuscate.Id)));
                    return;
                }

                float sigma = Math.Max(1, obfuscate.Size);
                _canvas.Apply(shape, context => context.GaussianBlur(sigma));
            }

            /// <summary>
            /// A cut mark: the band between its two edges is cut out of what is below, its edges from the same EdgeShape as frames
            /// </summary>
            private void CutMark(CutMarkElement cutMark, Style style)
            {
                var area = cutMark.Area;
                double length = cutMark.Horizontal ? area.Width : area.Height, thickness = cutMark.Horizontal ? area.Height : area.Width;
                double height = Math.Min(cutMark.Shape.Height, thickness / 2);
                var random = new Random(cutMark.Seed);
                PointF Place(PointF point, double across) => cutMark.Horizontal
                    ? new PointF((float)(area.X + point.X), (float)(area.Y + across))
                    : new PointF((float)(area.X + across), (float)(area.Y + point.X));
                var first = Outlines.Of(cutMark.Shape, length, height, random).Select(point => Place(point, height - point.Y)).ToArray();
                var second = Outlines.Of(cutMark.Shape, length, height, random).Select(point => Place(point, thickness - height + point.Y)).ToArray();
                var gap = new Polygon(first.Concat(second.Reverse()).ToArray());
                // Replaces what is below: without a fill the gap is see-through
                _canvas.Clear(Brushes.Solid(style.Fill is { Enabled: not false, Color: Rgba fill } ? Colour(fill) : Color.Transparent), gap);
                if (Width(style.Stroke) is float width)
                {
                    var pen = PenOf(style.Stroke.Color.Value, width, LineJoin.Round);
                    _canvas.Draw(pen, new Path(first));
                    _canvas.Draw(pen, new Path(second));
                }
            }

            /// <summary>
            /// A frame around what content draws: it is cut to the outline of the frame (the teeth cut inward from the outer edge),
            /// the band lies over its edge, what the teeth cut off gets the fill colour, the line follows the outline and the
            /// shadow falls outside of it. Around a turned target the frame turns with it.
            /// </summary>
            private void Framed(FrameElement frame, RectD area, double rotation, Action content)
            {
                var style = _document.StyleOf(frame);
                var placed = frame with { Area = area };
                var outer = placed.OuterEdge;
                var turn = Turn(rotation, area);
                IPath edge = Box(outer).Transform(turn);
                IPath outline = new Polygon(Outlines.Outline(placed, outer));
                if (frame.CornerRadius > 0)
                {
                    float radius = (float)Math.Min(frame.CornerRadius, Math.Min(outer.Width, outer.Height) / 2);
                    outline = outline.Clip(BooleanOperation.Intersection, new RoundedRectanglePolygon((float)outer.X, (float)outer.Y, (float)outer.Width, (float)outer.Height, radius));
                }

                var shape = outline.Transform(turn);
                Shadowed(style, outer.Turned(rotation, area.Center), () =>
                {
                    _canvas.Save();
                    _canvas.Clip(shape);
                    content();
                    if (frame.Band is FrameBand band && band.Width > 0 && band.Color.A > 0)
                    {
                        var inner = outer.Inflate(-band.Width);
                        var ring = inner.Width > 0 && inner.Height > 0 ? edge.Clip(BooleanOperation.Difference, Box(inner).Transform(turn)) : edge;
                        _canvas.Fill(Brushes.Solid(Colour(band.Color)), ring);
                    }

                    _canvas.Restore();
                    if (style.Fill is { Enabled: not false, Color: Rgba { A: > 0 } cutOff })
                    {
                        _canvas.Fill(Brushes.Solid(Colour(cutOff)), edge.Clip(BooleanOperation.Difference, shape));
                    }

                    Stroke(shape, style.Stroke);
                });
            }
        }
    }
}
