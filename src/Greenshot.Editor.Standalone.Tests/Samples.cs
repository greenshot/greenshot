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
using System.IO;
using Greenshot.Document;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Editor.Standalone.Tests
{
    /// <summary>
    /// Documents for the tests
    /// </summary>
    internal static class Samples
    {
        public static readonly Rgba White = new(255, 255, 255, 255), Green = new(0, 128, 0, 255), Black = new(0, 0, 0, 255), Red = new(255, 0, 0, 255);
        public static readonly Style Plain = new() { Stroke = new StrokeStyle { Enabled = false }, Shadow = new ShadowStyle { Enabled = false } };

        public static Rgba32 Of(Rgba color) => new(color.R, color.G, color.B, color.A);

        /// <summary>
        /// A PNG of one colour, or with a pattern of text-like stripes on white without one
        /// </summary>
        public static Resource Png(int width, int height, Rgba? color)
        {
            using var capture = new Image<Rgba32>(width, height);
            capture.ProcessPixelRows(pixels =>
            {
                for (int y = 0; y < pixels.Height; y++)
                {
                    var row = pixels.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        row[x] = color is Rgba c ? Of(c) : y % 20 is > 6 and < 14 && x / 7 % 3 != 2 && x % 90 > 8 ? new Rgba32(40, 40, 60) : new Rgba32(250, 250, 245);
                    }
                }
            });
            using var png = new MemoryStream();
            capture.SaveAsPng(png);
            return new Resource(Guid.NewGuid(), "image/png", png.ToArray()) { Width = width, Height = height };
        }

        /// <summary>
        /// A capture of the size at 0, 0 in the colour (or with stripes) and the annotations above it
        /// </summary>
        public static GreenshotDocument Page(int width, int height, Rgba? color, Canvas canvas, params Element[] annotations)
        {
            var resource = Png(width, height, color);
            return new GreenshotDocument(Guid.NewGuid(), canvas ?? new Canvas(), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, width, height), resource.Id) }),
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, annotations)
            }, new Dictionary<Guid, Resource> { [resource.Id] = resource });
        }

        public static GreenshotDocument Page(Rgba? color, params Element[] annotations) => Page(200, 100, color, null, annotations);

        /// <summary>
        /// One of every element the editor draws, on a capture with stripes
        /// </summary>
        public static GreenshotDocument Gallery()
        {
            static Guid Id() => Guid.NewGuid();
            var blue = new Rgba(0, 90, 200, 255);
            var steps = new[] { new StepLabelElement(Id(), new RectD(40, 300, 30, 30)), new StepLabelElement(Id(), new RectD(90, 300, 30, 30)), new StepLabelElement(Id(), new RectD(140, 296, 44, 44)) };
            var document = Page(640, 420, null, null,
                new RectangleElement(Id(), new RectD(20, 20, 120, 60)),
                new RectangleElement(Id(), new RectD(160, 20, 120, 60)) { Rotation = 20, Style = new Style { Stroke = StrokeStyle.Of(blue, 6), Fill = FillStyle.Of(new Rgba(255, 220, 0, 160)) } },
                new EllipseElement(Id(), new RectD(300, 20, 120, 60)) { Style = new Style { Fill = FillStyle.Of(new Rgba(0, 160, 0, 120)) } },
                new LineElement(Id(), new PointD(440, 20), new PointD(620, 50)),
                new ArrowElement(Id(), new PointD(440, 60), new PointD(620, 90)) { Style = new Style { Stroke = StrokeStyle.Of(blue, 4) } },
                new ArrowElement(Id(), new PointD(440, 100), new PointD(620, 110), ArrowHeads.Both),
                new FreehandElement(Id(), new[] { new PointD(20, 110), new PointD(50, 140), new PointD(80, 105), new PointD(110, 140), new PointD(140, 110), new PointD(170, 135) }),
                new TextElement(Id(), new RectD(200, 100, 200, 50), "Text in a box \U0001F600 wraps when it is long") { Style = new Style { Text = new TextStyle { Size = 14 } } },
                new SpeechBubbleElement(Id(), new RectD(20, 170, 180, 70), "Speech bubble", new PointD(60, 280)),
                new SpeechBubbleElement(Id(), new RectD(230, 170, 150, 60), "Turned", new PointD(400, 260)) { Rotation = -15, Style = new Style { Text = new TextStyle { Size = 16, Underline = true } } },
                steps[0], steps[1], steps[2],
                new EmojiElement(Id(), new RectD(200, 290, 60, 60), "\U0001F680"),
                new EmojiElement(Id(), new RectD(270, 290, 60, 60), "\U0001F44D") { Rotation = 30 },
                new HighlightElement(Id(), new RectD(420, 140, 200, 30), HighlightMode.Text),
                new HighlightElement(Id(), new RectD(420, 180, 100, 60), HighlightMode.Magnify),
                new ObfuscateElement(Id(), new RectD(420, 250, 100, 50), ObfuscateMode.Pixelate, 8),
                new ObfuscateElement(Id(), new RectD(530, 250, 100, 50), ObfuscateMode.Blur, 4) { Rotation = 10, Style = new Style { Stroke = StrokeStyle.Of(Red, 2) } },
                new CutMarkElement(Id(), new RectD(0, 360, 640, 24), true, 7),
                new RectangleElement(Id(), new RectD(350, 310, 60, 40)) { Label = new BoxLabel("Label") { Y = LabelPlace.After, Offset = 2 }, Style = new Style { Text = new TextStyle { Size = 12, Color = Black } } });
            var numbers = new Group(Id(), GroupTypes.Steps, new Guid?[] { steps[0].Id, steps[1].Id, steps[2].Id });
            return document with { Groups = new[] { numbers } };
        }

        /// <summary>
        /// Where the tests write the images they draw, to look at them: the folder GREENSHOT_TEST_OUTPUT, nothing is written without it
        /// </summary>
        public static void Keep(Image image, string name)
        {
            if (Environment.GetEnvironmentVariable("GREENSHOT_TEST_OUTPUT") is { Length: > 0 } folder)
            {
                Directory.CreateDirectory(folder);
                image.SaveAsPng(Path.Combine(folder, name + ".png"));
            }
        }
    }
}
