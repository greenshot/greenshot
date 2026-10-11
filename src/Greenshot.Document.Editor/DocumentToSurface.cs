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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Document.Editor
{
    /// <summary>
    /// Creates a Surface from a GreenshotDocument: the first image of the capture layer becomes the image of the surface,
    /// the elements of the other visible layers become containers, in the same order.
    /// UnknownElements become an image of their fallback, as the surface can't hold them otherwise.
    /// </summary>
    public static class DocumentToSurface
    {
        public static Surface ToSurface(GreenshotDocument document)
        {
            var capture = document.Layers.Where(layer => layer.Kind == LayerKind.Capture).SelectMany(layer => layer.Elements).OfType<ImageElement>().FirstOrDefault();
            var image = capture == null
                ? ImageHelper.CreateEmpty(document.Canvas.Width, document.Canvas.Height, PixelFormat.Format32bppArgb, ToColor(document.Canvas.Background))
                : LoadImage(document.Resources[capture.Resource]);
            var surface = new Surface(image)
            {
                ID = document.Id,
                CounterStart = document.Canvas.CounterStart
            };

            foreach (var element in document.Layers.Where(layer => layer.Kind != LayerKind.Capture && layer.Visible).SelectMany(layer => layer.Elements))
            {
                var container = ToContainer(surface, document, element);
                if (container != null)
                {
                    surface.AddElement(container, false, false);
                }
            }

            return surface;
        }

        /// <summary>
        /// The document as image, rendered by the editor
        /// </summary>
        public static Image ToImage(GreenshotDocument document)
        {
            using var surface = ToSurface(document);
            return surface.GetImageForExport();
        }

        private static DrawableContainer ToContainer(Surface surface, GreenshotDocument document, Element element)
        {
            DrawableContainer container = element switch
            {
                RectangleElement => new RectangleContainer(surface),
                EllipseElement => new EllipseContainer(surface),
                LineElement => new LineContainer(surface),
                ArrowElement => new ArrowContainer(surface),
                FreehandElement freehand => new FreehandContainer(surface) { CapturePoints = freehand.Points.Select(point => new Point(Round(point.X), Round(point.Y))).ToList() },
                TextElement text => new TextContainer(surface) { Text = text.Text },
                SpeechBubbleElement => new SpeechbubbleContainer(surface),
                StepLabelElement => new StepLabelContainer(surface),
                HighlightElement => new HighlightContainer(surface),
                ObfuscateElement => new ObfuscateContainer(surface),
                ImageElement image => ToImageContainer(surface, document.Resources[image.Resource], image.Rotation),
                EmojiElement emoji => new EmojiContainer(surface, emoji.Emoji) { RotationAngle = emoji.Rotation },
                CursorElement cursor => new CursorContainer(surface)
                {
                    Cursor = new CapturedCursor
                    {
                        ColorLayer = LoadImage(document.Resources[cursor.Resource]),
                        MaskLayer = cursor.MaskResource.HasValue ? LoadImage(document.Resources[cursor.MaskResource.Value]) : null,
                        Size = new NativeSize(Round(cursor.Area.Width), Round(cursor.Area.Height)),
                        HotSpot = new NativePoint(Round(cursor.Hotspot.X), Round(cursor.Hotspot.Y))
                    }
                },
                CutMarkElement cutMark => new CutMarkContainer(surface, cutMark.Horizontal, null),
                TornEdgeElement tornEdge => new TornEdgeContainer(surface, null,
                    new[] { tornEdge.Margins.Top, tornEdge.Margins.Right, tornEdge.Margins.Bottom, tornEdge.Margins.Left }, (CutMarkStyle)(int)tornEdge.CutStyle),
                UnknownElement { Fallback: Guid fallback } => ToImageContainer(surface, document.Resources[fallback], 0),
                _ => null
            };
            if (container == null)
            {
                return null;
            }

            // The filters are made new when the prepared filter changes, so it goes first
            switch (element)
            {
                case HighlightElement highlight:
                    container.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, highlight.Mode switch
                    {
                        HighlightMode.Area => PreparedFilter.AREA_HIGHLIGHT,
                        HighlightMode.Grayscale => PreparedFilter.GRAYSCALE,
                        HighlightMode.Magnify => PreparedFilter.MAGNIFICATION,
                        _ => PreparedFilter.TEXT_HIGHTLIGHT
                    });
                    break;
                case ObfuscateElement obfuscate:
                    container.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, obfuscate.Mode == ObfuscateMode.Blur ? PreparedFilter.BLUR : PreparedFilter.PIXELIZE);
                    break;
            }

            ApplyStyle(container, element.Style);

            switch (element)
            {
                case ArrowElement arrow:
                    container.SetFieldValue(FieldType.ARROWHEADS, (ArrowContainer.ArrowHeadCombination)(int)arrow.Heads);
                    break;
                case HighlightElement highlight:
                    SetField(container, FieldType.FILL_COLOR, ToColor(highlight.Color));
                    SetField(container, FieldType.BRIGHTNESS, highlight.Brightness);
                    SetField(container, FieldType.BLUR_RADIUS, highlight.BlurRadius);
                    SetField(container, FieldType.MAGNIFICATION_FACTOR, highlight.Magnification);
                    break;
                case ObfuscateElement obfuscate:
                    SetField(container, obfuscate.Mode == ObfuscateMode.Blur ? FieldType.BLUR_RADIUS : FieldType.PIXEL_SIZE, obfuscate.Size);
                    break;
                case CutMarkElement cutMark:
                    container.SetFieldValue(FieldType.CUT_MARK_STYLE, (CutMarkStyle)(int)cutMark.CutStyle);
                    container.SetFieldValue(FieldType.TOOTH_HEIGHT, cutMark.ToothHeight);
                    container.SetFieldValue(FieldType.TOOTH_RANGE, cutMark.ToothRange);
                    ((CutMarkContainer)container).SetSeed(cutMark.Seed);
                    break;
                case TornEdgeElement tornEdge:
                    container.SetFieldValue(FieldType.TOOTH_HEIGHT, tornEdge.ToothHeight);
                    container.SetFieldValue(FieldType.TOOTH_RANGE, tornEdge.ToothRange);
                    var tornEdgeContainer = (TornEdgeContainer)container;
                    tornEdgeContainer.SetLayout(
                        new[] { tornEdge.Edges.HasFlag(Edges.Top), tornEdge.Edges.HasFlag(Edges.Right), tornEdge.Edges.HasFlag(Edges.Bottom), tornEdge.Edges.HasFlag(Edges.Left) },
                        tornEdgeContainer.Margins);
                    tornEdgeContainer.SetSeed(tornEdge.Seed);
                    // Torn edges always cover the whole image
                    return container;
                case FreehandElement:
                    // The points are the position
                    return container;
            }

            switch (element)
            {
                case LineElement line:
                    SetLine(container, line.Start, line.End);
                    break;
                case ArrowElement arrow:
                    SetLine(container, arrow.Start, arrow.End);
                    break;
                default:
                    SetBounds(container, element.Bounds);
                    break;
            }

            if (element is SpeechBubbleElement bubble)
            {
                var speechbubble = (SpeechbubbleContainer)container;
                speechbubble.Text = bubble.Text;
                speechbubble.SetTailLocation(Round(bubble.Tail.X), Round(bubble.Tail.Y));
            }

            return container;
        }

        /// <summary>
        /// The opposite of SurfaceToDocument.ToStyle: a missing stroke is a line of width 0, a missing fill is transparent.
        /// The editor draws text in the line color.
        /// </summary>
        private static void ApplyStyle(DrawableContainer container, ElementStyle style)
        {
            var lineColor = style.Text?.Color ?? style.Stroke?.Color;
            if (lineColor.HasValue)
            {
                SetField(container, FieldType.LINE_COLOR, ToColor(lineColor.Value));
            }

            SetField(container, FieldType.LINE_THICKNESS, style.Stroke == null ? 0 : Round(style.Stroke.Width));
            if (container is not HighlightContainer)
            {
                SetField(container, FieldType.FILL_COLOR, ToColor(style.Fill?.Color ?? Rgba.Transparent));
            }

            SetField(container, FieldType.SHADOW, style.Shadow != null);
            if (style.Text is { } text)
            {
                SetField(container, FieldType.FONT_FAMILY, text.Font.Family);
                SetField(container, FieldType.FONT_SIZE, (float)text.Font.Size);
                SetField(container, FieldType.FONT_BOLD, text.Font.Bold);
                SetField(container, FieldType.FONT_ITALIC, text.Font.Italic);
                SetField(container, FieldType.TEXT_HORIZONTAL_ALIGNMENT, (StringAlignment)(int)text.Horizontal);
                SetField(container, FieldType.TEXT_VERTICAL_ALIGNMENT, (StringAlignment)(int)text.Vertical);
            }
        }

        // SetFieldValue and HasField of the container (not of AbstractFieldHolder) also reach the fields of its filters
        private static void SetField(DrawableContainer container, IFieldType fieldType, object value)
        {
            if (container.HasField(fieldType))
            {
                container.SetFieldValue(fieldType, value);
            }
        }

        private static void SetBounds(DrawableContainer container, RectD area)
        {
            container.Left = Round(area.X);
            container.Top = Round(area.Y);
            container.Width = Round(area.Width);
            container.Height = Round(area.Height);
        }

        /// <summary>
        /// Lines and arrows have a direction: the size is negative when the end is left of or above the start
        /// </summary>
        private static void SetLine(DrawableContainer container, PointD start, PointD end)
        {
            container.Left = Round(start.X);
            container.Top = Round(start.Y);
            container.Width = Round(end.X - start.X);
            container.Height = Round(end.Y - start.Y);
        }

        private static DrawableContainer ToImageContainer(Surface surface, Resource resource, int rotation)
        {
            switch (resource.MediaType)
            {
                case "image/svg+xml":
                    return new SvgContainer(new MemoryStream(resource.Data), surface) { RotationAngle = rotation };
                case "image/x-icon":
                    return new IconContainer(surface, new MemoryStream(resource.Data));
                case "image/emf":
                case "image/wmf":
                    return new MetafileContainer(new Metafile(new MemoryStream(resource.Data)), surface, resource.Data) { RotationAngle = rotation };
                default:
                    using (var image = LoadImage(resource))
                    using (var rotated = rotation == 0 ? null : image.Rotate(rotation))
                    {
                        // The setter makes its own copy
                        return new ImageContainer(surface) { Image = rotated ?? image };
                    }
            }
        }

        /// <summary>
        /// A bitmap which doesn't need the stream anymore
        /// </summary>
        private static Bitmap LoadImage(Resource resource)
        {
            using var stream = new MemoryStream(resource.Data);
            using var image = Image.FromStream(stream, true, true);
            return ImageHelper.Clone(image) as Bitmap;
        }

        private static Color ToColor(Rgba rgba) => Color.FromArgb(rgba.ToArgb());

        private static int Round(double value) => (int)Math.Round(value);
    }
}
