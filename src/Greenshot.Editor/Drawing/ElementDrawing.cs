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
using System.Drawing;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.ImageSharpDrawing;
using Greenshot.Editor.Drawing.Fields;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using Point = SixLabors.ImageSharp.Point;
using PointF = SixLabors.ImageSharp.PointF;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using static Greenshot.Editor.Drawing.ArrowContainer;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// The elements of the editor drawn with ImageSharp (see Greenshot.Base.ImageSharpDrawing), e.g. on the pixels of an HDR image
    /// </summary>
    internal static class ElementDrawing
    {
        /// <summary>
        /// How the element is drawn with ImageSharp on an image whose top left corner is at the origin; null when it has no such drawing (it is drawn
        /// with GDI+ then). Only the editor's own element types: a subclass (e.g. a barcode) may draw differently.
        /// </summary>
        public static Action<Image, ImageValues> Of(DrawableContainer element, NativePoint origin)
        {
            var elementBounds = element.Bounds;
            var rect = new NativeRect(elementBounds.X - origin.X, elementBounds.Y - origin.Y, elementBounds.Width, elementBounds.Height);
            var bounds = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            int lineThickness = element.HasField(FieldType.LINE_THICKNESS) ? element.GetFieldValueAsInt(FieldType.LINE_THICKNESS) : 0;
            var lineColor = element.HasField(FieldType.LINE_COLOR) ? Rgba(element.GetFieldValueAsColor(FieldType.LINE_COLOR)) : default;
            var fillColor = element.HasField(FieldType.FILL_COLOR) ? Rgba(element.GetFieldValueAsColor(FieldType.FILL_COLOR)) : default;
            bool shadow = element.HasField(FieldType.SHADOW) && element.GetFieldValueAsBool(FieldType.SHADOW);
            var start = new PointF(element.Left - origin.X, element.Top - origin.Y);
            var end = new PointF(start.X + element.Width, start.Y + element.Height);

            switch (element)
            {
                case HighlightContainer or ObfuscateContainer when element.GetType() == typeof(HighlightContainer) || element.GetType() == typeof(ObfuscateContainer):
                    // The filter itself is done on the pixels, the element draws its border
                    return (image, values) => Shapes.FilterBorder(image, values, bounds, lineThickness, lineColor, shadow);
                case SpeechbubbleContainer bubble when element.GetType() == typeof(SpeechbubbleContainer):
                    var target = bubble.TargetAdorner?.Location ?? new NativePoint(rect.Left - 20, rect.Bottom + 25);
                    var tail = new Point(target.X - origin.X, target.Y - origin.Y);
                    return (image, values) =>
                    {
                        SpeechBubbles.Bubble(image, values, bounds, tail, lineThickness, lineColor, fillColor, shadow);
                        DrawText(image, values, bubble, bounds, lineThickness, lineColor, shadow);
                    };
                case TextContainer text when element.GetType() == typeof(TextContainer):
                    // The shadow of the text only without a background
                    bool textShadow = shadow && fillColor.A == 0;
                    return (image, values) =>
                    {
                        Shapes.Rectangle(image, values, bounds, lineThickness, lineColor, fillColor, shadow);
                        DrawText(image, values, text, bounds, lineThickness, lineColor, textShadow);
                    };
                case RectangleContainer when element.GetType() == typeof(RectangleContainer):
                    return (image, values) => Shapes.Rectangle(image, values, bounds, lineThickness, lineColor, fillColor, shadow);
                case EllipseContainer when element.GetType() == typeof(EllipseContainer):
                    return (image, values) => Shapes.Ellipse(image, values, bounds, lineThickness, lineColor, fillColor, shadow);
                case ArrowContainer when element.GetType() == typeof(ArrowContainer):
                    var heads = (ArrowHeadCombination)element.GetFieldValue(FieldType.ARROWHEADS) switch
                    {
                        ArrowHeadCombination.START_POINT => ArrowHeads.Start,
                        ArrowHeadCombination.END_POINT => ArrowHeads.End,
                        ArrowHeadCombination.BOTH => ArrowHeads.Both,
                        _ => ArrowHeads.None
                    };
                    return (image, values) => Lines.Line(image, values, start, end, lineThickness, lineColor, heads, shadow);
                case LineContainer when element.GetType() == typeof(LineContainer):
                    return (image, values) => Lines.Line(image, values, start, end, lineThickness, lineColor, ArrowHeads.None, shadow);
                case FreehandContainer freehand when element.GetType() == typeof(FreehandContainer):
                    // The path is relative to the element
                    var points = freehand.FreehandPath?.PathPoints.Select(point => new PointF(point.X + start.X, point.Y + start.Y)).ToArray();
                    return points == null ? null : (image, values) => Lines.Freehand(image, values, points, lineThickness, lineColor);
                case StepLabelContainer label:
                    string number = label.NumberText;
                    float fontSize = label.NumberFontSize(number);
                    return (image, values) => Texts.StepLabel(image, values, bounds, number, FontFamily.GenericSansSerif.Name, fontSize, label.DrawAsRectangle,
                        lineThickness, lineColor, fillColor, shadow);
                case ImageContainer picture when element.GetType() == typeof(ImageContainer):
                    return PictureOf(picture.Image, bounds, nearestNeighbor: false, shadow);
                case IconContainer icon when element.GetType() == typeof(IconContainer):
                    if (icon.Icon == null)
                    {
                        return null;
                    }
                    using (var iconBitmap = icon.Icon.ToBitmap())
                    {
                        return PictureOf(iconBitmap, bounds, nearestNeighbor: true, shadow: false);
                    }
                case SvgContainer or MetafileContainer or Emoji.EmojiContainer:
                    return PictureOf(((VectorGraphicsContainer)element).RenderedImage, bounds, nearestNeighbor: false, shadow: false);
                default:
                    return null;
            }
        }

        private static void DrawText(Image image, ImageValues values, TextContainer element, Rectangle bounds, int lineThickness, Rgba32 color, bool shadow)
        {
            var font = element.Font;
            if (string.IsNullOrEmpty(element.Text) || font == null)
            {
                return;
            }
            Texts.Draw(image, values, bounds, element.Text, new TextFont(font.FontFamily.Name, font.Size, font.Bold, font.Italic), color,
                Placement(element.StringFormat?.Alignment), Placement(element.StringFormat?.LineAlignment), lineThickness, shadow);
        }

        private static TextPlacement Placement(StringAlignment? alignment) => alignment switch
        {
            StringAlignment.Near => TextPlacement.Near,
            StringAlignment.Far => TextPlacement.Far,
            _ => TextPlacement.Center
        };

        /// <summary>
        /// The picture drawn into its place; the picture is copied now, the element may change it later
        /// </summary>
        private static Action<Image, ImageValues> PictureOf(System.Drawing.Image picture, Rectangle place, bool nearestNeighbor, bool shadow)
        {
            if (picture is not Bitmap bitmap)
            {
                return null;
            }
            using var converted = ImageSharpHelper.ConvertToImageSharp(bitmap);
            if (converted == null)
            {
                return null;
            }
            var pixels = converted.CloneAs<Rgba32>();
            // Disposed when collected: an overlay step has no end of its own
            return (image, values) => Pictures.Draw(image, values, pixels, place, nearestNeighbor, shadow);
        }

        private static Rgba32 Rgba(Color color) => new Rgba32(color.R, color.G, color.B, color.A);
    }
}
