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
using System.Linq;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.ImageSharpDrawing
{
    /// <summary>
    /// Where text goes in its box, like StringAlignment of GDI+: Near is left or top
    /// </summary>
    public enum TextPlacement
    {
        Near,
        Center,
        Far
    }

    /// <summary>
    /// The font of a text: its family name (an installed font), the size in pixels, bold and italic
    /// </summary>
    public sealed class TextFont
    {
        public TextFont(string family, float size, bool bold = false, bool italic = false)
        {
            Family = family;
            Size = size;
            Bold = bold;
            Italic = italic;
        }

        public string Family { get; }

        /// <summary>
        /// The size of the em in pixels (GraphicsUnit.Pixel, like the editor's fonts)
        /// </summary>
        public float Size { get; }

        public bool Bold { get; }

        public bool Italic { get; }
    }

    /// <summary>
    /// The text of the editor (TextContainer.DrawText, used by the text box, the speech bubble and the step label) drawn with ImageSharp: wrapped
    /// in the box, aligned in it and cut off at its edges. Glyphs, line height and wrapping come from SixLabors.Fonts, so they can be a little
    /// different from the editor's.
    /// </summary>
    public static class Texts
    {
        // Fonts which stand in for a missing family, those of Windows first
        private static readonly string[] Fallbacks = { "Microsoft Sans Serif", "Segoe UI", "Arial", "DejaVu Sans", "Liberation Sans" };

        /// <summary>
        /// Like TextContainer.DrawText: the shadow (the text five times, from 0 to 4 pixels away), then the text in the box, inside the line of the box
        /// </summary>
        /// <param name="image">The image</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="box">The box of the text, normalized, in the editor's coordinates</param>
        /// <param name="text">The text, lines separated by line breaks</param>
        /// <param name="font">The font</param>
        /// <param name="color">The color of the text (the editor uses the line color)</param>
        /// <param name="horizontal">Where the lines go across the box</param>
        /// <param name="vertical">Where the lines go up and down the box</param>
        /// <param name="lineThickness">The width of the line of the box, the text stays inside it; 0 for none</param>
        /// <param name="shadow">Draw the shadow (the editor's text box only without a fill color)</param>
        /// <param name="turn">Turns (moves, scales) the drawing, e.g. around the centre of a turned element; null for none</param>
        public static void Draw(Image image, ImageValues values, Rectangle box, string text, TextFont font, Rgba32 color, TextPlacement horizontal,
            TextPlacement vertical, int lineThickness = 0, bool shadow = false, Matrix3x2? turn = null)
        {
            if (image == null || string.IsNullOrEmpty(text) || font == null || !(font.Size > 0))
            {
                return;
            }
            var resolved = Resolve(font);
            if (resolved == null)
            {
                return;
            }
            if (lineThickness > 0)
            {
                int inset = (int)Math.Ceiling(lineThickness / 2d);
                box.Inflate(-inset, -inset);
            }
            if (box.Width <= 0 || box.Height <= 0)
            {
                return;
            }
            image.Mutate(context =>
            {
                if (shadow)
                {
                    foreach (var (offset, alpha) in Painting.ShadowSteps())
                    {
                        if (turn is Matrix3x2 transform)
                        {
                            // The shadow falls the same way as that of an element which isn't turned
                            DrawIn(context, box, text, resolved, Painting.Black(alpha), horizontal, vertical, transform * Matrix3x2.CreateTranslation(offset, offset));
                        }
                        else
                        {
                            DrawIn(context, new Rectangle(box.X + offset, box.Y + offset, box.Width, box.Height), text, resolved, Painting.Black(alpha), horizontal, vertical);
                        }
                    }
                }
                DrawIn(context, box, text, resolved, values.ColorOf(color), horizontal, vertical, turn);
            });
        }

        /// <summary>
        /// Like StepLabelContainer.Draw: the circle (or rectangle) with its shadow, then the number in the middle, bold, in the line color
        /// </summary>
        /// <param name="image">The image</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="rect">The label, normalized, in the editor's coordinates</param>
        /// <param name="number">The number of the label</param>
        /// <param name="fontFamily">The family of the number (the editor: the generic sans serif family)</param>
        /// <param name="fontSize">The size of the number in pixels (the editor: 0.7 of what fits its measured text in the label)</param>
        /// <param name="asRectangle">A rectangle instead of the circle</param>
        /// <param name="lineThickness">The width of the line, 0 for none</param>
        /// <param name="lineColor">The color of the line and the number</param>
        /// <param name="fillColor">The color of the inside</param>
        /// <param name="shadow">Draw the shadow of the shape</param>
        /// <param name="turn">Turns (moves, scales) the drawing, e.g. around the centre of a turned element; null for none</param>
        public static void StepLabel(Image image, ImageValues values, Rectangle rect, string number, string fontFamily, float fontSize, bool asRectangle,
            int lineThickness, Rgba32 lineColor, Rgba32 fillColor, bool shadow, Matrix3x2? turn = null)
        {
            if (rect.Width == 0 || rect.Height == 0)
            {
                return;
            }
            if (asRectangle)
            {
                Shapes.Rectangle(image, values, rect, lineThickness, lineColor, fillColor, shadow, turn);
            }
            else
            {
                Shapes.Ellipse(image, values, rect, lineThickness, lineColor, fillColor, shadow, turn);
            }
            Draw(image, values, rect, number, new TextFont(fontFamily, fontSize, bold: true), lineColor, TextPlacement.Center, TextPlacement.Center, turn: turn);
        }

        /// <summary>
        /// The text wrapped in the box and placed in it, cut off at the box; turned, the letters are cut off at the box, then turned
        /// </summary>
        private static void DrawIn(IImageProcessingContext context, Rectangle box, string text, Font font, Color color, TextPlacement horizontal,
            TextPlacement vertical, Matrix3x2? turn = null)
        {
            // The editor (Graphics.DrawString) leaves 1/6 em at both sides of the lines
            float padding = font.Size / 6;
            float left = box.Left + padding, right = box.Right - padding;
            float top = box.Top, bottom = box.Bottom;
            var options = new RichTextOptions(font)
            {
                WrappingLength = right - left,
                Origin = new PointF(
                    horizontal switch { TextPlacement.Near => left, TextPlacement.Far => right, _ => (left + right) / 2 },
                    vertical switch { TextPlacement.Near => top, TextPlacement.Far => bottom, _ => (top + bottom) / 2 }),
                HorizontalAlignment = horizontal switch
                {
                    TextPlacement.Near => HorizontalAlignment.Left,
                    TextPlacement.Far => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Center
                },
                TextAlignment = horizontal switch
                {
                    TextPlacement.Near => TextAlignment.Start,
                    TextPlacement.Far => TextAlignment.End,
                    _ => TextAlignment.Center
                },
                VerticalAlignment = vertical switch
                {
                    TextPlacement.Near => VerticalAlignment.Top,
                    TextPlacement.Far => VerticalAlignment.Bottom,
                    _ => VerticalAlignment.Center
                }
            };
            var clip = new RectangularPolygon(box.X, box.Y, box.Width, box.Height);
            if (turn is Matrix3x2 transform)
            {
                // Not with Clip and the drawing transform: ImageSharp.Drawing 1.0 draws nothing when the turned clip reaches past the image
                var inBox = new ShapeOptions { ClippingOperation = ClippingOperation.Intersection };
                var letters = TextBuilder.GenerateGlyphs(text, options).Select(letter => letter.Clip(inBox, clip).Transform(transform));
                context.Fill(color, new PathCollection(letters));
                return;
            }
            context.Clip(clip, inside => inside.DrawText(options, text, color));
        }

        /// <summary>
        /// The installed font of the family, else of a fallback; null without any font
        /// </summary>
        internal static Font Resolve(TextFont font)
        {
            var style = font.Bold ? font.Italic ? FontStyle.BoldItalic : FontStyle.Bold : font.Italic ? FontStyle.Italic : FontStyle.Regular;
            foreach (var name in new[] { font.Family }.Concat(Fallbacks))
            {
                if (!string.IsNullOrEmpty(name) && SystemFonts.TryGet(name, out var family))
                {
                    return CreateFont(family, font.Size, style);
                }
            }
            var any = SystemFonts.Families.FirstOrDefault();
            return any == default ? null : CreateFont(any, font.Size, style);
        }

        private static Font CreateFont(FontFamily family, float size, FontStyle style) =>
            // A missing style (e.g. no bold) is drawn with the regular one; GDI+ would make it bold or italic itself
            family.GetAvailableStyles().Contains(style) ? family.CreateFont(size, style) : family.CreateFont(size);
    }
}
