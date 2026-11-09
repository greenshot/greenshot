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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using Greenshot.Base.Core;
using Greenshot.ImageSharpDrawing;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Threading;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using static Greenshot.Editor.Drawing.ArrowContainer;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using Image = SixLabors.ImageSharp.Image;
using PointF = SixLabors.ImageSharp.PointF;

namespace Greenshot.Tests.ImageSharpDrawing
{
    /// <summary>
    /// The drawing routines with ImageSharp draw what the editor draws with GDI+: each element is drawn by the editor into a transparent bitmap and by
    /// the routine into an 8-bit sRGB image (where the colors are mixed like GDI+ does), the pixels are compared. Anti-aliasing of edges differs a
    /// little between both, so a few pixels may differ.
    /// </summary>
    public class ElementDrawingTests
    {
        private const int Width = 120;
        private const int Height = 90;

        // A pixel differs when a channel (premultiplied) differs by more than this
        private const int Tolerance = 96;

        private static readonly Color Red = Color.FromArgb(255, 220, 30, 40);
        private static readonly Color HalfBlue = Color.FromArgb(128, 30, 60, 200);

        public ElementDrawingTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static Rgba32 Rgba(Color color) => new Rgba32(color.R, color.G, color.B, color.A);

        private static Surface CreateSurface() =>
            WinFormsContextGuard.CreateWithoutContext(() => new Surface(new Capture(new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))));

        /// <summary>
        /// What GDI+ draws on a transparent bitmap
        /// </summary>
        private static Rgba32[] Gdi(Action<Graphics> draw)
        {
            using var bitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                draw(graphics);
            }
            var pixels = new Rgba32[Width * Height];
            BitmapPixels.ProcessPixelRows<Bgra32>(bitmap, rows =>
            {
                for (int y = 0; y < rows.Height; y++)
                {
                    var row = rows.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        pixels[y * Width + x] = new Rgba32(row[x].R, row[x].G, row[x].B, row[x].A);
                    }
                }
            }, ImageLockMode.ReadOnly);
            return pixels;
        }

        /// <summary>
        /// What the routine draws on a transparent 8-bit sRGB image
        /// </summary>
        private static Rgba32[] Sharp(Action<Image> draw)
        {
            using var image = new SixLabors.ImageSharp.Image<Rgba32>(Width, Height);
            draw(image);
            var pixels = new Rgba32[Width * Height];
            image.CopyPixelDataTo(pixels);
            return pixels;
        }

        /// <summary>
        /// The pixels which differ, and how many pixels either drew
        /// </summary>
        private static (int Differ, int Drawn) Compare(Rgba32[] expected, Rgba32[] actual)
        {
            int differ = 0, drawn = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i].A == 0 && actual[i].A == 0)
                {
                    continue;
                }
                drawn++;
                int Premultiplied(byte channel, byte alpha) => channel * alpha / 255;
                var e = expected[i];
                var a = actual[i];
                if (Math.Abs(e.A - a.A) > Tolerance || Math.Abs(Premultiplied(e.R, e.A) - Premultiplied(a.R, a.A)) > Tolerance
                    || Math.Abs(Premultiplied(e.G, e.A) - Premultiplied(a.G, a.A)) > Tolerance || Math.Abs(Premultiplied(e.B, e.A) - Premultiplied(a.B, a.A)) > Tolerance)
                {
                    differ++;
                }
            }
            return (differ, drawn);
        }

        private static void AssertSame(Rgba32[] expected, Rgba32[] actual, double allowed = 0.03)
        {
            var (differ, drawn) = Compare(expected, actual);
            Assert.True(drawn > 20, $"Too little is drawn: {drawn} pixels");
            Assert.True(differ <= drawn * allowed, $"{differ} of {drawn} pixels differ");
        }

        [Theory]
        // Lines of odd width: the editor draws rectangle lines without anti-aliasing, the routine anti-aliased, which looks the same only where the
        // line's edges are on pixel edges
        [InlineData(3, true, true)]
        [InlineData(1, false, false)]
        [InlineData(0, true, true)]
        [InlineData(5, true, false)]
        public void Rectangle_AsTheEditor(int lineThickness, bool fill, bool shadow)
        {
            var fillColor = fill ? HalfBlue : Color.Transparent;
            var expected = Gdi(graphics => RectangleContainer.DrawRectangle(new Dapplo.Windows.Common.Structs.NativeRect(10, 12, 60, 40), graphics, RenderMode.EXPORT,
                lineThickness, Red, fillColor, shadow));
            var actual = Sharp(image => Shapes.Rectangle(image, ImageValues.Srgb, new Rectangle(10, 12, 60, 40), lineThickness, Rgba(Red), Rgba(fillColor), shadow));

            AssertSame(expected, actual);
        }

        [Theory]
        [InlineData(3, true, true)]
        [InlineData(1, false, false)]
        [InlineData(0, true, true)]
        public void Ellipse_AsTheEditor(int lineThickness, bool fill, bool shadow)
        {
            var fillColor = fill ? HalfBlue : Color.Transparent;
            var expected = Gdi(graphics =>
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.None;
                EllipseContainer.DrawEllipse(new Dapplo.Windows.Common.Structs.NativeRect(15, 10, 70, 45), graphics, RenderMode.EXPORT, lineThickness, Red, fillColor, shadow);
            });
            var actual = Sharp(image => Shapes.Ellipse(image, ImageValues.Srgb, new Rectangle(15, 10, 70, 45), lineThickness, Rgba(Red), Rgba(fillColor), shadow));

            AssertSame(expected, actual);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FilterBorder_AsTheEditor(bool shadow)
        {
            using var surface = CreateSurface();
            var highlight = new HighlightContainer(surface) { Left = 20, Top = 15, Width = 50, Height = 30 };
            // Odd width, see Rectangle_AsTheEditor
            highlight.SetFieldValue(FieldType.LINE_THICKNESS, 3);
            highlight.SetFieldValue(FieldType.LINE_COLOR, Red);
            highlight.SetFieldValue(FieldType.SHADOW, shadow);
            var expected = Gdi(graphics => highlight.Draw(graphics, RenderMode.EXPORT));
            var actual = Sharp(image => Shapes.FilterBorder(image, ImageValues.Srgb, new Rectangle(20, 15, 50, 30), 3, Rgba(Red), shadow));

            AssertSame(expected, actual);
        }

        [Theory]
        [InlineData(3, ArrowHeadCombination.NONE, true)]
        [InlineData(2, ArrowHeadCombination.END_POINT, false)]
        [InlineData(2, ArrowHeadCombination.BOTH, true)]
        [InlineData(5, ArrowHeadCombination.START_POINT, false)]
        public void LineAndArrow_AsTheEditor(int lineThickness, ArrowHeadCombination heads, bool shadow)
        {
            using var surface = CreateSurface();
            DrawableContainer line = heads == ArrowHeadCombination.NONE
                ? new LineContainer(surface) { Left = 15, Top = 70, Width = 85, Height = -50 }
                : new ArrowContainer(surface) { Left = 15, Top = 70, Width = 85, Height = -50 };
            line.SetFieldValue(FieldType.LINE_THICKNESS, lineThickness);
            line.SetFieldValue(FieldType.LINE_COLOR, Red);
            line.SetFieldValue(FieldType.SHADOW, shadow);
            if (line is ArrowContainer)
            {
                line.SetFieldValue(FieldType.ARROWHEADS, heads);
            }
            var expected = Gdi(graphics => line.Draw(graphics, RenderMode.EXPORT));
            var routineHeads = heads switch
            {
                ArrowHeadCombination.START_POINT => ArrowHeads.Start,
                ArrowHeadCombination.END_POINT => ArrowHeads.End,
                ArrowHeadCombination.BOTH => ArrowHeads.Both,
                _ => ArrowHeads.None
            };
            var actual = Sharp(image => Lines.Line(image, ImageValues.Srgb, new PointF(15, 70), new PointF(100, 20), lineThickness, Rgba(Red), routineHeads, shadow));

            AssertSame(expected, actual);
        }

        [Fact]
        public void Freehand_AsTheEditor()
        {
            using var surface = CreateSurface();
            var freehand = new FreehandContainer(surface);
            freehand.SetFieldValue(FieldType.LINE_THICKNESS, 4);
            freehand.SetFieldValue(FieldType.LINE_COLOR, Red);
            freehand.HandleMouseDown(10, 60);
            for (int x = 12; x <= 110; x += 3)
            {
                freehand.HandleMouseMove(x, 45 + (int)(25 * Math.Sin(x / 12.0)));
            }
            freehand.HandleMouseUp(110, 45 + (int)(25 * Math.Sin(110 / 12.0)));
            // The points as the editor smoothed them (it adds points until they make whole curves)
            var points = ((List<System.Drawing.Point>)typeof(FreehandContainer).GetField("capturePoints", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(freehand))
                .Select(point => new PointF(point.X + freehand.Left, point.Y + freehand.Top)).ToList();

            var expected = Gdi(graphics => freehand.Draw(graphics, RenderMode.EXPORT));
            var actual = Sharp(image => Lines.Freehand(image, ImageValues.Srgb, points, 4, Rgba(Red)));

            AssertSame(expected, actual);
        }

        // The editor draws the tail as its own shape over the box: its shadow shows inside a box without fill, and with a line thickness of 0 its
        // line is drawn one pixel wide while the box has none. Here the bubble is one shape, so these cases are compared by the tests below.
        [Theory]
        [InlineData(2, true, true, 100, 80)]
        [InlineData(1, true, false, 10, 85)]
        [InlineData(4, true, false, 110, 5)]
        public void SpeechBubble_AsTheEditor(int lineThickness, bool fill, bool shadow, int tailX, int tailY)
        {
            using var surface = CreateSurface();
            var bubble = new SpeechbubbleContainer(surface) { Left = 25, Top = 15, Width = 60, Height = 35 };
            bubble.SetFieldValue(FieldType.LINE_THICKNESS, lineThickness);
            bubble.SetFieldValue(FieldType.LINE_COLOR, Red);
            bubble.SetFieldValue(FieldType.FILL_COLOR, fill ? HalfBlue : Color.Transparent);
            bubble.SetFieldValue(FieldType.SHADOW, shadow);
            bubble.SetTailLocation(tailX, tailY);
            var expected = Gdi(graphics => bubble.Draw(graphics, RenderMode.EXPORT));
            var actual = Sharp(image => SpeechBubbles.Bubble(image, ImageValues.Srgb, new Rectangle(25, 15, 60, 35), new SixLabors.ImageSharp.Point(tailX, tailY),
                lineThickness, Rgba(Red), Rgba(fill ? HalfBlue : Color.Transparent), shadow));

            AssertSame(expected, actual);
        }

        [Fact]
        public void SpeechBubble_WithoutLineThickness_HasNoLine()
        {
            var withLine = Sharp(image => SpeechBubbles.Bubble(image, ImageValues.Srgb, new Rectangle(25, 15, 60, 35), new SixLabors.ImageSharp.Point(10, 85), 0,
                Rgba(Red), Rgba(HalfBlue), false));
            var withoutLine = Sharp(image => SpeechBubbles.Bubble(image, ImageValues.Srgb, new Rectangle(25, 15, 60, 35), new SixLabors.ImageSharp.Point(10, 85), 0,
                new Rgba32(0, 0, 0, 0), Rgba(HalfBlue), false));

            Assert.Equal(withoutLine, withLine);
        }

        [Fact]
        public void SpeechBubble_WithoutFill_KeepsTheBoxEmpty()
        {
            var pixels = Sharp(image => SpeechBubbles.Bubble(image, ImageValues.Srgb, new Rectangle(25, 15, 60, 35), new SixLabors.ImageSharp.Point(110, 5), 4,
                Rgba(Red), new Rgba32(0, 0, 0, 0), true));

            // Inside the box, away from its line and shadow: no shadow of the tail
            var drawn = new List<string>();
            for (int y = 25; y < 44; y++)
            {
                for (int x = 34; x < 78; x++)
                {
                    if (pixels[y * Width + x].A > 0)
                    {
                        drawn.Add($"{x},{y}");
                    }
                }
            }
            Assert.Empty(drawn);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Picture_AsTheEditor(bool shadow)
        {
            using var surface = CreateSurface();
            using var bitmap = new Bitmap(20, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(255, 40, 160, 90));
            }
            bitmap.SetPixel(0, 0, Color.Transparent);
            using var picture = new SixLabors.ImageSharp.Image<Rgba32>(20, 16, new Rgba32(40, 160, 90, 255));
            picture[0, 0] = new Rgba32(0, 0, 0, 0);
            var container = new ImageContainer(surface) { Image = bitmap, Left = 30, Top = 20, Width = 20, Height = 16 };
            container.SetFieldValue(FieldType.SHADOW, shadow);
            var expected = Gdi(graphics => container.Draw(graphics, RenderMode.EXPORT));
            var actual = Sharp(image => Pictures.Draw(image, ImageValues.Srgb, picture, new Rectangle(container.Left, container.Top, container.Width, container.Height), shadow: shadow));

            AssertSame(expected, actual);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(2, true)]
        public void StepLabel_AsTheEditor(int lineThickness, bool shadow)
        {
            using var surface = CreateSurface();
            var label = new StepLabelContainer(surface) { Left = 30, Top = 20, Width = 40, Height = 40 };
            surface.AddElement(label, false, false);
            label.SetFieldValue(FieldType.LINE_THICKNESS, lineThickness);
            label.SetFieldValue(FieldType.LINE_COLOR, Color.White);
            label.SetFieldValue(FieldType.FILL_COLOR, Color.DarkRed);
            label.SetFieldValue(FieldType.SHADOW, shadow);
            string number = surface.CountStepLabels(label).ToString();
            // The size of the number as the editor works it out
            using var family = new FontFamily(FontFamily.GenericSansSerif.Name);
            using var measureFont = new Font(family, 40, FontStyle.Bold, GraphicsUnit.Pixel);
            var measured = System.Windows.Forms.TextRenderer.MeasureText(number, measureFont);
            float fontSize = 40f * measured.Height / measured.Width * 0.7f;

            var expected = Gdi(graphics => label.Draw(graphics, RenderMode.EXPORT));
            var actual = Sharp(image => Texts.StepLabel(image, ImageValues.Srgb, new Rectangle(30, 20, 40, 40), number, family.Name, fontSize, false, lineThickness,
                new Rgba32(255, 255, 255, 255), Rgba(Color.DarkRed), shadow));

            // The glyph of the number differs a little
            AssertSame(expected, actual, 0.06);
        }

        [Theory]
        [InlineData(StringAlignment.Near, StringAlignment.Near)]
        [InlineData(StringAlignment.Center, StringAlignment.Center)]
        [InlineData(StringAlignment.Far, StringAlignment.Far)]
        public void Text_IsWhereTheEditorPutsIt(StringAlignment horizontal, StringAlignment vertical)
        {
            const string text = "Greenshot draws text\nin lines";
            using var font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Regular, GraphicsUnit.Pixel);
            using var format = new StringFormat { Trimming = StringTrimming.EllipsisWord, Alignment = horizontal, LineAlignment = vertical };
            var box = new Dapplo.Windows.Common.Structs.NativeRect(5, 5, 110, 80);
            var expected = Gdi(graphics =>
            {
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                TextContainer.DrawText(graphics, box, 2, Red, false, format, text, font);
            });
            var actual = Sharp(image => Texts.Draw(image, ImageValues.Srgb, new Rectangle(5, 5, 110, 80), text, new TextFont(font.FontFamily.Name, 14), Rgba(Red),
                Placement(horizontal), Placement(vertical), 2));

            // Glyphs and line height differ a little, the place of the text not: its ink is about as much as the editor's, at the same side of the box
            var expectedInk = Ink(expected);
            var actualInk = Ink(actual);
            Assert.True(actualInk.Amount > 0, "Nothing is drawn");
            Assert.InRange(actualInk.Amount, expectedInk.Amount * 0.7, expectedInk.Amount * 1.3);
            Assert.InRange(actualInk.Bounds.Width, expectedInk.Bounds.Width * 0.8, expectedInk.Bounds.Width * 1.2);
            Assert.InRange(actualInk.Bounds.Height, expectedInk.Bounds.Height * 0.75, expectedInk.Bounds.Height * 1.25);
            double Middle(int from, int to) => (from + to) / 2.0;
            Assert.InRange(Side(actualInk.Bounds.Left, actualInk.Bounds.Right, horizontal), Side(expectedInk.Bounds.Left, expectedInk.Bounds.Right, horizontal) - 4,
                Side(expectedInk.Bounds.Left, expectedInk.Bounds.Right, horizontal) + 4);
            Assert.InRange(Side(actualInk.Bounds.Top, actualInk.Bounds.Bottom, vertical), Side(expectedInk.Bounds.Top, expectedInk.Bounds.Bottom, vertical) - 4,
                Side(expectedInk.Bounds.Top, expectedInk.Bounds.Bottom, vertical) + 4);

            double Side(int from, int to, StringAlignment alignment) => alignment switch
            {
                StringAlignment.Near => from,
                StringAlignment.Far => to,
                _ => Middle(from, to)
            };
        }

        [Fact]
        public void Text_TooLong_WrapsInsideTheBox()
        {
            var pixels = Sharp(image => Texts.Draw(image, ImageValues.Srgb, new Rectangle(10, 10, 80, 40), "one two three four five six seven eight nine ten eleven twelve",
                new TextFont("Arial", 14), Rgba(Red), TextPlacement.Near, TextPlacement.Near));

            var ink = Ink(pixels);
            Assert.True(ink.Amount > 0, "Nothing is drawn");
            Assert.InRange(ink.Bounds.Left, 10, 90);
            Assert.InRange(ink.Bounds.Right, 10, 91);
            Assert.InRange(ink.Bounds.Top, 10, 50);
            Assert.InRange(ink.Bounds.Bottom, 10, 51);
            // More than one line
            Assert.True(ink.Bounds.Height > 20, $"Only {ink.Bounds.Height} pixels high");
        }

        [Fact]
        public void HdrImage_TheColorIsLinearLightAtTheWhite()
        {
            const float white = 3f;
            using var image = new SixLabors.ImageSharp.Image<RgbaVector>(Width, Height);

            Shapes.Rectangle(image, ImageValues.Linear(white), new Rectangle(10, 10, 40, 30), 0, Rgba(Red), new Rgba32(255, 255, 255, 255), false);
            Shapes.Rectangle(image, ImageValues.Linear(white), new Rectangle(60, 10, 40, 30), 0, Rgba(Red), new Rgba32(188, 188, 188, 255), false);

            Assert.Equal(white, image[30, 25].ToVector4().X, 2);
            // sRGB 188 is half as bright in linear light
            Assert.Equal(white * 0.5f, image[80, 25].ToVector4().X, 1);
            Assert.Equal(0f, image[5, 5].ToVector4().W, 3);
        }



        private static TextPlacement Placement(StringAlignment alignment) => alignment switch
        {
            StringAlignment.Near => TextPlacement.Near,
            StringAlignment.Far => TextPlacement.Far,
            _ => TextPlacement.Center
        };

        /// <summary>
        /// How much is drawn (the sum of the alpha) and where
        /// </summary>
        private static (double Amount, System.Drawing.Rectangle Bounds) Ink(Rgba32[] pixels)
        {
            double amount = 0;
            int left = Width, top = Height, right = 0, bottom = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    byte alpha = pixels[y * Width + x].A;
                    if (alpha < 64)
                    {
                        continue;
                    }
                    amount += alpha / 255.0;
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x + 1);
                    bottom = Math.Max(bottom, y + 1);
                }
            }
            return (amount, System.Drawing.Rectangle.FromLTRB(left, top, right, bottom));
        }
    }
}
