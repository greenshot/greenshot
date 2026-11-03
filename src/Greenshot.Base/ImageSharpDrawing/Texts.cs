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
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Base.ImageSharpDrawing
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
    /// The text of the editor (TextContainer.DrawText, used by the text box, the speech bubble and the step label) drawn with ImageSharp into an image of
    /// any pixel type. The layout follows Graphics.DrawString with the editor's StringFormat: lines wrap at spaces in the width of the box (a word too
    /// long for it at its characters), 1/6 em of room left and right, only whole lines are shown, and when the text doesn't fit the last shown line ends
    /// with an ellipsis after a whole word (StringTrimming.EllipsisWord). The glyphs come from SixLabors.Fonts, their shapes and the line height from the
    /// font file, so they can be a little different from GDI+.
    /// </summary>
    public static class Texts
    {
        private const string Ellipsis = "…";

        // Fonts which stand in for a missing family, those of Windows first
        private static readonly string[] Fallbacks = { "Microsoft Sans Serif", "Segoe UI", "Arial", "DejaVu Sans", "Liberation Sans" };

        /// <summary>
        /// Like TextContainer.DrawText: the shadow (the text five times, from 0 to 4 pixels away), then the text in the box, inside the line of the box
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="box">The box of the text, normalized, in the editor's coordinates (GDI+ PixelOffsetMode.None)</param>
        /// <param name="text">The text, lines separated by line breaks</param>
        /// <param name="font">The font</param>
        /// <param name="color">The color of the text (the editor uses the line color)</param>
        /// <param name="horizontal">Where the lines go across the box</param>
        /// <param name="vertical">Where the lines go up and down the box</param>
        /// <param name="lineThickness">The width of the line of the box, the text stays inside it; 0 for none</param>
        /// <param name="shadow">Draw the shadow (the editor's text box only without a fill color)</param>
        public static void Draw(Image image, ImageValues values, Rectangle box, string text, TextFont font, Rgba32 color, TextPlacement horizontal,
            TextPlacement vertical, int lineThickness = 0, bool shadow = false)
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
            int textOffset = lineThickness > 0 ? (int)Math.Ceiling(lineThickness / 2d) : 0;
            if (lineThickness > 0)
            {
                box.Inflate(-textOffset, -textOffset);
            }
            if (box.Width <= 0 || box.Height <= 0)
            {
                return;
            }
            var lines = Layout(text, resolved, box, horizontal, vertical);
            if (shadow)
            {
                foreach (var (offset, alpha) in Painting.ShadowSteps())
                {
                    var moved = new Rectangle(box.X + offset, box.Y + offset, box.Width, box.Height);
                    Painting.Fill(image, Glyphs(lines, resolved, offset), new Vector4(0, 0, 0, alpha), clip: Clip(moved));
                }
            }
            Painting.Fill(image, Glyphs(lines, resolved, 0), values.Of(color), clip: Clip(box));
        }

        /// <summary>
        /// Like StepLabelContainer.Draw: the circle (or rectangle) with its shadow, then the number in the middle, bold, in the line color
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
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
        public static void StepLabel(Image image, ImageValues values, Rectangle rect, string number, string fontFamily, float fontSize, bool asRectangle,
            int lineThickness, Rgba32 lineColor, Rgba32 fillColor, bool shadow)
        {
            if (rect.Width == 0 || rect.Height == 0)
            {
                return;
            }
            if (asRectangle)
            {
                Shapes.Rectangle(image, values, rect, lineThickness, lineColor, fillColor, shadow);
            }
            else
            {
                Shapes.Ellipse(image, values, rect, lineThickness, lineColor, fillColor, shadow);
            }
            Draw(image, values, rect, number, new TextFont(fontFamily, fontSize, bold: true), lineColor, TextPlacement.Center, TextPlacement.Center);
        }

        /// <summary>
        /// The lines of the text in the box: the text of each and where its top left corner is, in the editor's coordinates
        /// </summary>
        internal static IReadOnlyList<(string Text, PointF Origin)> Layout(string text, Font font, Rectangle box, TextPlacement horizontal, TextPlacement vertical)
        {
            float size = font.Size;
            // DrawString leaves 1/6 em at both sides of the lines
            float padding = size / 6;
            float width = box.Width - 2 * padding;
            float lineHeight = LineHeight(font);
            var wrapped = Wrap(text, font, width);
            // Only whole lines, at least the first
            int fits = Math.Max(1, (int)Math.Floor(box.Height / lineHeight));
            if (wrapped.Count > fits)
            {
                var shown = wrapped.Take(fits).ToList();
                shown[fits - 1] = WithEllipsis(shown[fits - 1], font, width);
                wrapped = shown;
            }
            float blockHeight = wrapped.Count * lineHeight;
            float top = vertical switch
            {
                TextPlacement.Near => box.Top,
                TextPlacement.Far => box.Bottom - blockHeight,
                _ => box.Top + (box.Height - blockHeight) / 2
            };
            var lines = new List<(string, PointF)>();
            for (int i = 0; i < wrapped.Count; i++)
            {
                float lineWidth = Width(wrapped[i], font);
                float left = horizontal switch
                {
                    TextPlacement.Near => box.Left + padding,
                    TextPlacement.Far => box.Right - padding - lineWidth,
                    _ => box.Left + (box.Width - lineWidth) / 2
                };
                lines.Add((wrapped[i], new PointF(left, top + i * lineHeight)));
            }
            return lines;
        }

        /// <summary>
        /// The text in lines: at the line breaks, and where the next word doesn't fit in the width
        /// </summary>
        internal static List<string> Wrap(string text, Font font, float width)
        {
            var lines = new List<string>();
            foreach (var paragraph in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = string.Empty;
                foreach (var word in paragraph.Split(' '))
                {
                    var candidate = line.Length == 0 ? word : line + " " + word;
                    if (line.Length == 0 || Width(candidate, font) <= width)
                    {
                        line = candidate;
                    }
                    else
                    {
                        lines.Add(line);
                        line = word;
                    }
                    // A word too long for a line is broken at its characters
                    while (line.Length > 1 && Width(line, font) > width)
                    {
                        int fitting = line.Length - 1;
                        while (fitting > 1 && Width(line.Substring(0, fitting), font) > width)
                        {
                            fitting--;
                        }
                        lines.Add(line.Substring(0, fitting));
                        line = line.Substring(fitting);
                    }
                }
                lines.Add(line);
            }
            return lines;
        }

        /// <summary>
        /// The line with an ellipsis at its end, whole words dropped until it fits
        /// </summary>
        private static string WithEllipsis(string line, Font font, float width)
        {
            var words = line.Split(' ').ToList();
            while (words.Count > 1 && Width(string.Join(" ", words) + Ellipsis, font) > width)
            {
                words.RemoveAt(words.Count - 1);
            }
            return string.Join(" ", words) + Ellipsis;
        }

        /// <summary>
        /// The glyphs of the lines as shapes in ImageSharp's coordinates, moved by the offset
        /// </summary>
        private static IReadOnlyList<IPath> Glyphs(IReadOnlyList<(string Text, PointF Origin)> lines, Font font, int offset)
        {
            var glyphs = new List<IPath>();
            foreach (var (line, origin) in lines)
            {
                if (line.Length == 0)
                {
                    continue;
                }
                var options = new TextOptions(font)
                {
                    Origin = new PointF(origin.X + offset + Painting.GdiPixelCenter, origin.Y + offset + Painting.GdiPixelCenter)
                };
                glyphs.AddRange(TextBuilder.GenerateGlyphs(line, options));
            }
            return glyphs;
        }

        // DrawString clips the text to its box
        private static Rectangle Clip(Rectangle box) => new Rectangle(box.X, box.Y, box.Width + 1, box.Height + 1);

        private static float Width(string text, Font font) => text.Length == 0 ? 0 : TextMeasurer.MeasureAdvance(text, new TextOptions(font)).Width;

        /// <summary>
        /// The distance of the lines: ascent, descent and line gap of the font
        /// </summary>
        internal static float LineHeight(Font font)
        {
            var metrics = font.FontMetrics;
            return font.Size * metrics.HorizontalMetrics.LineHeight / metrics.UnitsPerEm;
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
