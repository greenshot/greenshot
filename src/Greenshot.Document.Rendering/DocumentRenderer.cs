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
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Greenshot.ImageSharpDrawing;
using LineHeads = Greenshot.ImageSharpDrawing.ArrowHeads;

namespace Greenshot.Document.Rendering
{
    /// <summary>
    /// Draws a document into an image of its canvas, without the editor. Rectangles, ellipses, text boxes, lines, arrows,
    /// speech bubbles and step labels are drawn with the painters of the editor's ImageSharp drawing (Greenshot.ImageSharpDrawing),
    /// so they look like in the editor. Turned elements are drawn turned (R37), obfuscate and highlight work on the pixels of the
    /// canvas cut to their turned shape (R38), a frame cuts its target, draws its band, line and shadow (R22) and follows a turned
    /// target (R41).
    /// </summary>
    public sealed class DocumentRenderer : IDisposable
    {
        private static readonly DrawingOptions Replace = new() { GraphicsOptions = new GraphicsOptions { AlphaCompositionMode = PixelAlphaCompositionMode.Src } };
        private static readonly GraphicsOptions ReplaceImage = new() { AlphaCompositionMode = PixelAlphaCompositionMode.Src };

        // The canvas is 8-bit sRGB
        private static readonly ImageValues Values = ImageValues.Srgb;

        private readonly GreenshotDocument _document;
        private readonly RectD _canvas;
        private readonly int _width, _height;
        private readonly Dictionary<Guid, Image<Rgba32>> _images = new();
        private readonly DocumentRenderCache _cache;

        private DocumentRenderer(GreenshotDocument document, RectD? area, DocumentRenderCache cache)
        {
            _document = Anchors.Apply(document);
            _canvas = area ?? _document.CanvasArea();
            _width = Math.Max(1, (int)Math.Ceiling(_canvas.Width));
            _height = Math.Max(1, (int)Math.Ceiling(_canvas.Height));
            _cache = cache;
        }

        /// <summary>
        /// The document drawn: the background of the canvas, then the visible layers in drawing order
        /// </summary>
        public static Image<Rgba32> Render(GreenshotDocument document)
        {
            using var renderer = new DocumentRenderer(document, null, null);
            var image = renderer.Draw(_ => true);
            if (renderer._document.Canvas.Background is { A: > 0 } background)
            {
                // Behind everything: what is drawn stays as it is where it covers, the background shows where it doesn't
                var below = new Image<Rgba32>(image.Width, image.Height, new Rgba32(background.R, background.G, background.B, background.A));
                using (image)
                {
                    below.Mutate(context => context.DrawImage(image, 1));
                }

                return below;
            }

            return image;
        }

        /// <summary>
        /// A part of the document drawn, e.g. what an editor changed: area is in the coordinates of the elements and not cut
        /// to the canvas. Only the elements which draw into the area are drawn, and of those only the ones include takes (all
        /// without include). Blur, pixelate and magnify near the edge of the area see only the pixels in the area: draw a
        /// bigger area and use its middle. The cache keeps the decoded images for the next call.
        /// </summary>
        public static Image<Rgba32> Render(GreenshotDocument document, RectD area, DocumentRenderCache cache, Func<Element, bool> include = null)
        {
            using var renderer = new DocumentRenderer(document, area, cache);
            return renderer.Draw(element => (include == null || include(element)) && renderer.DrawsInto(element));
        }

        private Image<Rgba32> Draw(Func<Element, bool> include)
        {
            var canvas = new Image<Rgba32>(_width, _height);
            foreach (var element in _document.Layers.Where(layer => layer.Visible).SelectMany(layer => layer.Elements).Where(include))
            {
                Draw(canvas, element);
            }

            return canvas;
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

            // The editor's shadow has no size in the style, it reaches 5 pixels; a frame around its target draws with it
            var bounds = _document.VisualBounds(element).Inflate(8);
            if (_document.FrameOf(element) is FrameElement frame)
            {
                bounds = RectD.Union(bounds, _document.VisualBounds(frame with { Area = element.Bounds }).Inflate(8));
            }

            return bounds.X <= _canvas.Right && _canvas.X <= bounds.Right && bounds.Y <= _canvas.Bottom && _canvas.Y <= bounds.Bottom;
        }

        public void Dispose()
        {
            foreach (var image in _images.Values)
            {
                image?.Dispose();
            }
        }

        private void Draw(Image<Rgba32> canvas, Element element)
        {
            var style = _document.StyleOf(element);
            switch (element)
            {
                case HighlightElement highlight:
                    Highlight(canvas, highlight);
                    FilterBorder(canvas, highlight.Area, highlight.Rotation, style);
                    return;
                case ObfuscateElement obfuscate:
                    Obfuscate(canvas, obfuscate);
                    FilterBorder(canvas, obfuscate.Area, obfuscate.Rotation, style);
                    return;
                case CutMarkElement cutMark:
                    CutMark(canvas, cutMark, style);
                    return;
                case FrameElement { Target: null } frame:
                    // A frame without a target frames what is drawn below it
                    using (var under = new Image<Rgba32>(_width, _height))
                    {
                        Frame(canvas, under, frame, null);
                        under.Mutate(context => context.DrawImage(canvas, 1));
                        canvas.Mutate(context => context.DrawImage(under, ReplaceImage));
                    }

                    return;
                case FrameElement:
                    // Drawn with its target
                    return;
            }

            using var layer = new Image<Rgba32>(_width, _height);
            var framing = _document.FrameOf(element) as FrameElement;
            // The editor's shadow (no size, darkness or offset) is drawn by the painters like in the editor, else from the layer
            bool editorShadow = framing == null && style.Shadow is { Enabled: true, Size: null, Darkness: null, Offset: null };
            bool shadowDrawn = Paint(layer, element, style, editorShadow);
            if (framing != null)
            {
                Frame(layer, canvas, framing, element);
            }
            else if (!shadowDrawn && style.Shadow is { Enabled: true } shadow)
            {
                Shadow(canvas, layer, shadow, false);
            }

            canvas.Mutate(context => context.DrawImage(layer, 1));
        }

        /// <summary>
        /// The element on its layer; true when the painter drew the editor's shadow with it
        /// </summary>
        private bool Paint(Image<Rgba32> layer, Element element, Style style, bool editorShadow)
        {
            bool shadowDrawn = false;
            switch (element)
            {
                case RectangleElement e:
                    Shapes.Rectangle(layer, Values, Pixels(e.Area, style.Stroke), LineThickness(style.Stroke), LineColor(style.Stroke), FillColor(style.Fill),
                        editorShadow, Turn(e.Rotation, e.Area));
                    shadowDrawn = editorShadow;
                    break;
                case EllipseElement e:
                    Shapes.Ellipse(layer, Values, Pixels(e.Area, style.Stroke), LineThickness(style.Stroke), LineColor(style.Stroke), FillColor(style.Fill),
                        editorShadow, Turn(e.Rotation, e.Area));
                    shadowDrawn = editorShadow;
                    break;
                case TextElement e:
                {
                    // Like the text box of the editor: the box, then the text inside its line, its shadow only without a fill
                    var turn = Turn(e.Rotation, e.Area);
                    var fill = FillColor(style.Fill);
                    Shapes.Rectangle(layer, Values, Pixels(e.Area, style.Stroke), LineThickness(style.Stroke), LineColor(style.Stroke), fill, editorShadow, turn);
                    BoxText(layer, e.Text, style, Pixels(e.Area), editorShadow && fill.A == 0, turn);
                    shadowDrawn = editorShadow;
                    break;
                }
                case LineElement e:
                    Lines.Line(layer, Values, Canvas(e.Start), Canvas(e.End), LineThickness(style.Stroke), LineColor(style.Stroke), LineHeads.None, editorShadow);
                    shadowDrawn = editorShadow;
                    break;
                case ArrowElement e:
                    Lines.Line(layer, Values, Canvas(e.Start), Canvas(e.End), LineThickness(style.Stroke), LineColor(style.Stroke), (LineHeads)(int)e.Heads,
                        editorShadow);
                    shadowDrawn = editorShadow;
                    break;
                case FreehandElement e when e.Points.Count > 1 && Width(style.Stroke) is float width:
                    // The editor smooths the points it captured into curves, the document keeps the points: drawn as they are
                    layer.Mutate(context => context.Draw(new SolidPen(new PenOptions(Colour(style.Stroke.Color), width) { JointStyle = JointStyle.Round, EndCapStyle = EndCapStyle.Round }),
                        new SixLabors.ImageSharp.Drawing.Path(new LinearLineSegment(e.Points.Select(Canvas).ToArray()))));
                    break;
                case SpeechBubbleElement e:
                {
                    var box = Pixels(e.Area);
                    var turn = Turn(e.Rotation, e.Area);
                    // The tail point is on the canvas, the painter turns it with the box: where it is before the turn
                    var tail = Canvas(e.Tail);
                    if (turn is Matrix3x2 transform && Matrix3x2.Invert(transform, out var back))
                    {
                        tail = Vector2.Transform(tail, back);
                    }

                    SpeechBubbles.Bubble(layer, Values, box, new Point((int)Math.Round(tail.X), (int)Math.Round(tail.Y)), LineThickness(style.Stroke),
                        LineColor(style.Stroke), FillColor(style.Fill), editorShadow, turn);
                    BoxText(layer, e.Text, style, box, false, turn);
                    shadowDrawn = editorShadow;
                    break;
                }
                case StepLabelElement e:
                    StepLabel(layer, e, style, editorShadow);
                    shadowDrawn = editorShadow;
                    break;
                case ImageElement e:
                    layer.Mutate(context => Picture(context, e.Resource, e.Area, e.Source, e.Scaling, e.Rotation));
                    break;
                case PluginElement { Fallback: Guid fallback } e:
                    layer.Mutate(context => Picture(context, fallback, e.Area, null, ImageScaling.Smooth, e.Rotation));
                    break;
                case CursorElement e:
                    layer.Mutate(context => Picture(context, e.Resource, e.Area, null, ImageScaling.Smooth, 0));
                    break;
                case EmojiElement e:
                    // The Twemoji font of Greenshot, in color, as the editor draws it
                    layer.Mutate(context => EmojiDrawing.Draw(context.SetDrawingTransform(Turn(e.Rotation, e.Area.Center)), e.Emoji,
                        new PointF((float)e.Area.Center.X, (float)e.Area.Center.Y), (float)Math.Min(e.Area.Width, e.Area.Height)));
                    break;
            }

            layer.Mutate(context => Label(context, element, style));
            return shadowDrawn;
        }

        /// <summary>
        /// The line around a highlight or obfuscate, like the editor's, with its shadow when it has the editor's
        /// </summary>
        private void FilterBorder(Image<Rgba32> canvas, RectD area, double rotation, Style style) =>
            Shapes.FilterBorder(canvas, Values, Pixels(area, style.Stroke), LineThickness(style.Stroke), LineColor(style.Stroke),
                style.Shadow is { Enabled: true, Size: null, Darkness: null, Offset: null }, Turn(rotation, area));

        /// <summary>
        /// The area in whole canvas pixels, the editor's coordinates; with a line inside or outside the area the box of its middle,
        /// as the painters draw the line centred (R2)
        /// </summary>
        private Rectangle Pixels(RectD area, StrokeStyle stroke = null)
        {
            double move = Width(stroke) is float width
                ? (stroke.Position ?? StrokePosition.Center) switch { StrokePosition.Inside => -width / 2, StrokePosition.Outside => width / 2, _ => 0 }
                : 0;
            var box = area.Inflate(move);
            return new Rectangle((int)Math.Round(box.X - _canvas.X), (int)Math.Round(box.Y - _canvas.Y), (int)Math.Round(box.Width), (int)Math.Round(box.Height));
        }

        /// <summary>
        /// The turn of a box-shaped element for the painters: clockwise around the centre of its area in canvas pixels, null when upright
        /// </summary>
        private Matrix3x2? Turn(double degrees, RectD area) =>
            degrees % 360 == 0 ? null : Matrix3x2.CreateRotation((float)(degrees * Math.PI / 180), new Vector2((float)(area.Center.X - _canvas.X), (float)(area.Center.Y - _canvas.Y)));

        private static int LineThickness(StrokeStyle stroke) => Width(stroke) is float width ? Math.Max(1, (int)Math.Round(width)) : 0;

        private static Rgba32 LineColor(StrokeStyle stroke) => stroke?.Color is Rgba c ? new Rgba32(c.R, c.G, c.B, c.A) : default;

        private static Rgba32 FillColor(FillStyle fill) => fill is { Enabled: not false, Color: Rgba c } ? new Rgba32(c.R, c.G, c.B, c.A) : default;

        /// <summary>
        /// The text of a box like the editor's: in the line colour unless the text has its own, inside the line of the box
        /// </summary>
        private static void BoxText(Image<Rgba32> layer, string text, Style style, Rectangle box, bool shadow, Matrix3x2? turn)
        {
            var textStyle = style.Text ?? new TextStyle();
            if (string.IsNullOrEmpty(text) || textStyle.Enabled == false)
            {
                return;
            }

            var color = textStyle.Color ?? style.Stroke?.Color ?? new Rgba(0, 0, 0, 255);
            Texts.Draw(layer, Values, box, text, TextFontOf(textStyle), new Rgba32(color.R, color.G, color.B, color.A), Placement(textStyle.Horizontal),
                Placement(textStyle.Vertical), LineThickness(style.Stroke), shadow, turn);
        }

        private static TextPlacement Placement(TextAlignment? alignment) => alignment switch
        {
            TextAlignment.Start => TextPlacement.Near,
            TextAlignment.End => TextPlacement.Far,
            _ => TextPlacement.Center
        };

        // Canvas pixels: the canvas starts at its top left corner
        private PointF Canvas(PointD point) => new((float)(point.X - _canvas.X), (float)(point.Y - _canvas.Y));

        // Turned clockwise around the pivot, then into canvas pixels
        private Matrix3x2 Turn(double degrees, PointD pivot) =>
            Matrix3x2.CreateRotation((float)(degrees * Math.PI / 180), new Vector2((float)pivot.X, (float)pivot.Y)) * Matrix3x2.CreateTranslation((float)-_canvas.X, (float)-_canvas.Y);

        private static IPath Box(RectD box) => new RectangularPolygon((float)box.X, (float)box.Y, (float)box.Width, (float)box.Height);

        private IPath Whole => new RectangularPolygon(-1, -1, _width + 2, _height + 2);

        private static Color Colour(Rgba? color) => color is Rgba c ? Color.FromRgba(c.R, c.G, c.B, c.A) : Color.Transparent;

        private static float? Width(StrokeStyle stroke) =>
            stroke is { Enabled: not false, Width: > 0 and double width, Color: Rgba { A: > 0 } } ? (float)width : null;

        /// <summary>
        /// The turned shape of a highlight or obfuscate in canvas pixels
        /// </summary>
        private IPath Shape(Element element)
        {
            var area = element switch
            {
                HighlightElement e => e.Area,
                ObfuscateElement e => e.Area,
                _ => element.Bounds
            };
            return Box(area).Transform(Turn(element.Rotation, area.Center));
        }

        /// <summary>
        /// The line along a shape by its position (R2), open shapes are always centred
        /// </summary>
        private void Stroke(IImageProcessingContext context, IPath shape, StrokeStyle stroke, bool open)
        {
            if (Width(stroke) is not float width)
            {
                return;
            }

            var color = Colour(stroke.Color);
            switch (open ? StrokePosition.Center : stroke.Position ?? StrokePosition.Center)
            {
                case StrokePosition.Inside:
                    context.Clip(shape, inside => inside.Draw(Pens.Solid(color, 2 * width), shape));
                    break;
                case StrokePosition.Outside:
                    context.Clip(Whole.Clip(shape), outside => outside.Draw(Pens.Solid(color, 2 * width), shape));
                    break;
                default:
                    context.Draw(Pens.Solid(color, width), shape);
                    break;
            }
        }

        /// <summary>
        /// A step label like the editor's: the circle and its number, bold and 0.7 of the size which fits, in the line colour
        /// </summary>
        private void StepLabel(Image<Rgba32> layer, StepLabelElement label, Style style, bool shadow)
        {
            var area = label.Area;
            string number = _document.LabelOf(label);
            double fits = Math.Min(area.Width, area.Height);
            var measured = TextMeasurer.MeasureSize(number, new TextOptions(FontOf(new TextStyle { Size = fits, Weight = 700 })));
            double size = measured.Width > 0 ? fits * measured.Height / measured.Width * 0.7 : fits * 0.7;
            Texts.StepLabel(layer, Values, Pixels(area), number, FamilyOf(new TextStyle()), (float)size, false, LineThickness(style.Stroke), LineColor(style.Stroke),
                FillColor(style.Fill), shadow);
        }

        /// <summary>
        /// Text in a box, aligned by its style, turned with the transform
        /// </summary>
        private static void Text(IImageProcessingContext context, string text, TextStyle style, RectD box, Matrix3x2 transform, Rgba? lineColor)
        {
            if (string.IsNullOrEmpty(text) || style?.Enabled == false)
            {
                return;
            }

            var options = Options(style, box.Width);
            var size = TextMeasurer.MeasureSize(text, options);
            bool wrap = options.WrappingLength > 0;
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
            var glyphs = TextBuilder.GenerateGlyphs(text, options).Transform(transform);
            context.Fill(Colour(style.Color ?? lineColor ?? new Rgba(0, 0, 0, 255)), glyphs);
            if (style.Outline is { } outline && Width(outline) is float width)
            {
                context.Draw(Pens.Solid(Colour(outline.Color), width), glyphs);
            }
        }

        private static TextOptions Options(TextStyle style, double width)
        {
            var font = FontOf(style);
            var options = new TextOptions(font)
            {
                WrappingLength = style.Wrap == false ? -1 : (float)width,
                TextAlignment = (style.Horizontal ?? TextAlignment.Center) switch
                {
                    TextAlignment.Start => SixLabors.Fonts.TextAlignment.Start,
                    TextAlignment.End => SixLabors.Fonts.TextAlignment.End,
                    _ => SixLabors.Fonts.TextAlignment.Center
                }
            };
            if (style.LineHeight is double lineHeight)
            {
                float natural = TextMeasurer.MeasureSize("X", new TextOptions(font)).Height;
                options.LineSpacing = natural > 0 ? (float)(lineHeight * font.Size / natural) : 1;
            }

            return options;
        }

        // Generic families and the fonts which stand for them on Windows and Linux
        private static readonly Dictionary<string, string[]> Generic = new(StringComparer.OrdinalIgnoreCase)
        {
            [TextStyle.SansSerif] = new[] { "Segoe UI", "Microsoft Sans Serif", "Arial", "DejaVu Sans", "Liberation Sans" },
            [TextStyle.Serif] = new[] { "Times New Roman", "DejaVu Serif", "Liberation Serif" },
            [TextStyle.Monospace] = new[] { "Consolas", "Courier New", "DejaVu Sans Mono", "Liberation Mono" }
        };

        /// <summary>
        /// The first family of the list which is installed
        /// </summary>
        private static FontFamily InstalledFamily(TextStyle style)
        {
            var names = TextStyle.Families(style.Family).SelectMany(name => Generic.TryGetValue(name, out var stand) ? stand : new[] { name }).Concat(Generic[TextStyle.SansSerif]);
            return names.Select(name => SystemFonts.TryGet(name, out var found) ? (FontFamily?)found : null).FirstOrDefault(found => found != null)
                   ?? SystemFonts.Families.First();
        }

        private static string FamilyOf(TextStyle style) => InstalledFamily(style).Name;

        /// <summary>
        /// The font for the painters: the installed family, the size in canvas pixels
        /// </summary>
        private static TextFont TextFontOf(TextStyle style) =>
            new(FamilyOf(style), (float)Math.Max(1, style.Size ?? 11), (style.Weight ?? 400) >= 600, style.Italic == true);

        /// <summary>
        /// The first family of the list which is installed; a size in canvas pixels
        /// </summary>
        private static Font FontOf(TextStyle style)
        {
            var family = InstalledFamily(style);
            var fontStyle = (style.Weight ?? 400) >= 600 ? style.Italic == true ? FontStyle.BoldItalic : FontStyle.Bold : style.Italic == true ? FontStyle.Italic : FontStyle.Regular;
            // Fonts measures in points at 72 dpi, that is one point per pixel
            return family.CreateFont((float)Math.Max(1, style.Size ?? 11), fontStyle);
        }

        /// <summary>
        /// The label of an element, measured in its font, placed and turned with its element (R43 - R46)
        /// </summary>
        private void Label(IImageProcessingContext context, Element element, Style style)
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
            var size = TextMeasurer.MeasureSize(text, Options(lineStyle, wrapWidth));
            if (GreenshotDocument.LabelPlacement(element, size.Width, size.Height) is var (box, rotation, pivot))
            {
                Text(context, text, lineStyle, box, Turn(rotation, pivot), style.Stroke?.Color);
            }
        }

        /// <summary>
        /// An image resource: the part Source of it, scaled to the area, turned around its centre
        /// </summary>
        private void Picture(IImageProcessingContext context, Guid resource, RectD area, RectD? source, ImageScaling scaling, double rotation)
        {
            if (Decoded(resource) is not Image<Rgba32> image || area.Width < 1 || area.Height < 1)
            {
                return;
            }

            if (source == null && rotation % 360 == 0 && (int)Math.Round(area.Width) == image.Width && (int)Math.Round(area.Height) == image.Height)
            {
                // Shown as it is, e.g. the capture: drawn without a copy
                var at = Canvas(new PointD(area.X, area.Y));
                context.DrawImage(image, new Point((int)Math.Round(at.X), (int)Math.Round(at.Y)), 1);
                return;
            }

            using var shown = image.Clone(picture =>
            {
                if (source is RectD part)
                {
                    var cut = new Rectangle((int)Math.Round(part.X * image.Width), (int)Math.Round(part.Y * image.Height),
                        Math.Max(1, (int)Math.Round(part.Width * image.Width)), Math.Max(1, (int)Math.Round(part.Height * image.Height)));
                    cut.Intersect(image.Bounds());
                    if (cut.Width > 0 && cut.Height > 0)
                    {
                        picture.Crop(cut);
                    }
                }

                picture.Resize((int)Math.Round(area.Width), (int)Math.Round(area.Height), scaling == ImageScaling.Pixelated ? KnownResamplers.NearestNeighbor : KnownResamplers.Bicubic);
                double turn = (rotation % 360 + 360) % 360;
                if (turn % 90 == 0)
                {
                    // Quarter turns move pixels, nothing is resampled
                    picture.Rotate((RotateMode)(int)turn);
                }
                else
                {
                    picture.Rotate((float)turn);
                }
            });
            var center = Canvas(area.Center);
            context.DrawImage(shown, new Point((int)Math.Round(center.X - shown.Width / 2.0), (int)Math.Round(center.Y - shown.Height / 2.0)), 1);
        }

        private Image<Rgba32> Decoded(Guid id)
        {
            if (_images.TryGetValue(id, out var image))
            {
                return image;
            }

            if (_cache != null && _cache.TryGet(id, out image))
            {
                return image;
            }

            image = null;
            if (_document.Resources.TryGetValue(id, out var resource) && resource.MediaType != null && resource.MediaType.StartsWith("image/", StringComparison.Ordinal) && resource.MediaType != "image/svg+xml")
            {
                using var stream = resource.OpenRead();
                try
                {
                    image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
                }
                catch (UnknownImageFormatException)
                {
                    // A format ImageSharp can't read (e.g. an icon) isn't drawn
                }
            }

            if (_cache != null)
            {
                _cache.Add(id, image);
            }
            else
            {
                _images[id] = image;
            }

            return image;
        }

        /// <summary>
        /// The shadow of what is on the layer: without size, darkness and offset the editor's five steps, else the shape
        /// blurred by size, as dark as darkness, moved by offset. The offset is on the canvas, it doesn't turn (R37).
        /// </summary>
        private static void Shadow(Image<Rgba32> canvas, Image<Rgba32> layer, ShadowStyle shadow, bool frame)
        {
            using var dark = layer.Clone(context => context.Brightness(0));
            if (!frame && shadow.Size == null && shadow.Darkness == null && shadow.Offset == null)
            {
                for (int step = 4; step >= 0; step--)
                {
                    int offset = step;
                    canvas.Mutate(context => context.DrawImage(dark, new Point(offset, offset), (100 - 20 * offset) / 255f));
                }

                return;
            }

            int size = (int)Math.Round(shadow.Size ?? 7);
            var move = shadow.Offset ?? new PointD(2, 2);
            if (size > 1)
            {
                dark.Mutate(context => context.BoxBlur(size / 2));
            }

            canvas.Mutate(context => context.DrawImage(dark, new Point((int)Math.Round(move.X), (int)Math.Round(move.Y)), (float)Math.Max(0, Math.Min(1, shadow.Darkness ?? 0.6))));
        }

        /// <summary>
        /// A frame on what is drawn on the layer (R22): the band by its placement, the teeth cut inward from the outer edge
        /// and get the fill colour, the line along the outer edge, the shadow outside of it onto the canvas below. Along a
        /// turned target it turns with it (R41).
        /// </summary>
        private void Frame(Image<Rgba32> layer, Image<Rgba32> below, FrameElement frame, Element target)
        {
            var style = _document.StyleOf(frame);
            var (area, rotation) = target switch
            {
                RectangleElement e => (e.Area, e.Rotation),
                EllipseElement e => (e.Area, e.Rotation),
                TextElement e => (e.Area, e.Rotation),
                SpeechBubbleElement e => (e.Area, e.Rotation),
                ImageElement e => (e.Area, e.Rotation),
                EmojiElement e => (e.Area, e.Rotation),
                PluginElement e => (e.Area, e.Rotation),
                null => (frame.Area, 0.0),
                _ => (target.Bounds, 0.0)
            };
            var outer = (frame with { Area = area }).OuterEdge;
            var turn = Turn(rotation, area.Center);
            IPath edge = Box(outer).Transform(turn);
            IPath shape = new Polygon(new LinearLineSegment(Outlines.Outline(frame, outer))).Transform(turn);
            IPath round = frame.CornerRadius > 0 ? new Polygon(new LinearLineSegment(Outlines.RoundedBox(outer, frame.CornerRadius))).Transform(turn) : null;
            var cutOff = style.Fill is { Enabled: not false, Color: Rgba fill } ? Colour(fill) : Color.Transparent;
            // What is in the band and the shape is put back from copies, so only simple clips are needed
            if (frame.Band is FrameBand band && band.Width > 0)
            {
                using var unbanded = layer.Clone();
                layer.Mutate(context => context.Fill(Colour(band.Color), edge).Clip(Box(outer.Inflate(-band.Width)).Transform(turn), inside => inside.DrawImage(unbanded, ReplaceImage)));
            }

            using (var uncut = layer.Clone())
            {
                layer.Mutate(context =>
                {
                    context.Fill(Replace, cutOff, edge);
                    context.Clip(shape, inside => inside.DrawImage(uncut, ReplaceImage));
                    if (round != null)
                    {
                        using var square = layer.Clone();
                        context.Fill(Replace, cutOff, edge).Clip(round, inside => inside.DrawImage(square, ReplaceImage));
                    }

                    Stroke(context, shape, style.Stroke, false);
                });
            }

            if (style.Shadow is { Enabled: true } shadow)
            {
                using var mask = new Image<Rgba32>(_width, _height);
                mask.Mutate(context =>
                {
                    context.Fill(Color.Black, shape);
                    if (round != null)
                    {
                        using var square = mask.Clone();
                        context.Fill(Replace, Color.Transparent, Whole).Clip(round, inside => inside.DrawImage(square, ReplaceImage));
                    }
                });
                Shadow(below, mask, shadow, true);
            }
        }

        /// <summary>
        /// The effect of the highlight on the canvas pixels, cut to its turned shape (R38)
        /// </summary>
        private void Highlight(Image<Rgba32> canvas, HighlightElement highlight)
        {
            var shape = Shape(highlight);
            switch (highlight.Mode)
            {
                case HighlightMode.Text:
                    var color = highlight.Color;
                    Effect(canvas, shape, false, image => image.ProcessPixelRows(pixels =>
                    {
                        for (int y = 0; y < pixels.Height; y++)
                        {
                            foreach (ref var pixel in pixels.GetRowSpan(y))
                            {
                                pixel.R = Math.Min(color.R, pixel.R);
                                pixel.G = Math.Min(color.G, pixel.G);
                                pixel.B = Math.Min(color.B, pixel.B);
                            }
                        }
                    }));
                    break;
                case HighlightMode.Area:
                    int add = (int)Math.Round((highlight.Brightness - 1) * 255);
                    Effect(canvas, shape, true, image =>
                    {
                        image.ProcessPixelRows(pixels =>
                        {
                            for (int y = 0; y < pixels.Height; y++)
                            {
                                foreach (ref var pixel in pixels.GetRowSpan(y))
                                {
                                    pixel = new Rgba32(Clamp(pixel.R + add), Clamp(pixel.G + add), Clamp(pixel.B + add), pixel.A);
                                }
                            }
                        });
                        if (highlight.BlurRadius > 1)
                        {
                            image.Mutate(context => context.BoxBlur(highlight.BlurRadius / 2).BoxBlur(highlight.BlurRadius / 2));
                        }
                    });
                    break;
                case HighlightMode.Grayscale:
                    Effect(canvas, shape, true, image => image.Mutate(context => context.Grayscale(GrayscaleMode.Bt601)));
                    break;
                case HighlightMode.Magnify:
                    var bounds = Pixels(shape.Bounds);
                    int factor = Math.Max(1, highlight.Magnification);
                    var from = new Rectangle(bounds.X + bounds.Width / 2 - bounds.Width / factor / 2, bounds.Y + bounds.Height / 2 - bounds.Height / factor / 2,
                        Math.Max(1, bounds.Width / factor), Math.Max(1, bounds.Height / factor));
                    from.Intersect(canvas.Bounds());
                    if (bounds.Width > 0 && bounds.Height > 0 && from.Width > 0 && from.Height > 0)
                    {
                        Effect(canvas, shape, false, image =>
                        {
                            using var magnified = canvas.Clone(context => context.Crop(from).Resize(bounds.Width, bounds.Height, KnownResamplers.NearestNeighbor));
                            image.Mutate(context => context.DrawImage(magnified, bounds.Location, ReplaceImage));
                        });
                    }

                    break;
            }
        }

        /// <summary>
        /// Pixelate or blur the canvas pixels, cut to the turned shape (R38). Pixelate is the editor's, with noise.
        /// </summary>
        private void Obfuscate(Image<Rgba32> canvas, ObfuscateElement obfuscate)
        {
            var shape = Shape(obfuscate);
            var bounds = Pixels(shape.Bounds);
            if (obfuscate.Mode == ObfuscateMode.Pixelate)
            {
                Effect(canvas, shape, false, image => SecurePixelation.Apply(image, bounds, obfuscate.Size));
                return;
            }

            int radius = Math.Max(1, obfuscate.Size);
            var around = Rectangle.Inflate(bounds, 2 * radius, 2 * radius);
            around.Intersect(canvas.Bounds());
            if (around.Width > 0 && around.Height > 0)
            {
                Effect(canvas, shape, false, image => image.Mutate(context => context.BoxBlur(radius / 2 + 1, around).BoxBlur(radius / 2 + 1, around)));
            }
        }

        private static Rectangle Pixels(RectangleF bounds)
        {
            int left = (int)Math.Floor(bounds.Left), top = (int)Math.Floor(bounds.Top);
            return new Rectangle(left, top, (int)Math.Ceiling(bounds.Right) - left, (int)Math.Ceiling(bounds.Bottom) - top);
        }

        private static byte Clamp(int value) => (byte)Math.Max(0, Math.Min(255, value));

        /// <summary>
        /// The effect worked out on a copy of the canvas pixels, put back inside the shape, or outside of it when inverted
        /// </summary>
        private void Effect(Image<Rgba32> canvas, IPath shape, bool invert, Action<Image<Rgba32>> effect)
        {
            using var changed = canvas.Clone();
            effect(changed);
            canvas.Mutate(context => context.Clip(invert ? Whole.Clip(shape) : shape, inside => inside.DrawImage(changed, ReplaceImage)));
        }

        /// <summary>
        /// A cut mark: the band between its two edges is cut out of what is below, its edges from the same EdgeShape as
        /// frames
        /// </summary>
        private void CutMark(Image<Rgba32> canvas, CutMarkElement cutMark, Style style)
        {
            var area = cutMark.Area;
            double length = cutMark.Horizontal ? area.Width : area.Height, thickness = cutMark.Horizontal ? area.Height : area.Width;
            double height = Math.Min(cutMark.Shape.Height, thickness / 2);
            var random = new Random(cutMark.Seed);
            PointF Place(PointF point, double across) => cutMark.Horizontal
                ? Canvas(new PointD(area.X + point.X, area.Y + across))
                : Canvas(new PointD(area.X + across, area.Y + point.X));
            var first = Outlines.Of(cutMark.Shape, length, height, random).Select(point => Place(point, height - point.Y)).ToArray();
            var second = Outlines.Of(cutMark.Shape, length, height, random).Select(point => Place(point, thickness - height + point.Y)).ToArray();
            var gap = new Polygon(new LinearLineSegment(first.Concat(Enumerable.Reverse(second)).ToArray()));
            canvas.Mutate(context =>
            {
                context.Fill(Replace, style.Fill is { Enabled: not false, Color: Rgba fill } ? Colour(fill) : Color.Transparent, gap);
                if (Width(style.Stroke) is float width)
                {
                    var pen = new SolidPen(new PenOptions(Colour(style.Stroke.Color), width) { JointStyle = JointStyle.Round });
                    context.Draw(pen, new SixLabors.ImageSharp.Drawing.Path(new LinearLineSegment(first)));
                    context.Draw(pen, new SixLabors.ImageSharp.Drawing.Path(new LinearLineSegment(second)));
                }
            });
        }
    }
}
