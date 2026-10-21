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
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace Greenshot.Tests.Document
{
    public class DocumentRendererTests
    {
        private static readonly Rgba White = new(255, 255, 255, 255), Green = new(0, 128, 0, 255), Black = new(0, 0, 0, 255);
        private static readonly Style Plain = new() { Stroke = new StrokeStyle { Enabled = false }, Shadow = new ShadowStyle { Enabled = false } };

        // A capture of 200 x 100 at 0, 0 in the colour, or in stripes of one pixel black and white
        private static GreenshotDocument Page(Canvas canvas, Rgba? color, params Element[] annotations)
        {
            using var capture = new Image<Rgba32>(200, 100);
            capture.ProcessPixelRows(pixels =>
            {
                for (int y = 0; y < pixels.Height; y++)
                {
                    var row = pixels.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        row[x] = color is Rgba c ? new Rgba32(c.R, c.G, c.B, c.A) : x % 2 == 0 ? new Rgba32(0, 0, 0) : new Rgba32(255, 255, 255);
                    }
                }
            });
            using var png = new MemoryStream();
            capture.SaveAsPng(png);
            var resource = new Resource(Guid.NewGuid(), "image/png", png.ToArray()) { Width = 200, Height = 100 };
            return new GreenshotDocument(Guid.NewGuid(), canvas, new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 200, 100), resource.Id) }),
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, annotations)
            }, new Dictionary<Guid, Resource> { [resource.Id] = resource });
        }

        private static Guid CaptureId(GreenshotDocument document) => document.Layers[0].Elements[0].Id;

        private static Rgba32 Of(Rgba color) => new(color.R, color.G, color.B, color.A);

        [Fact]
        public void ATurnedElement_IsDrawnTurned_ItsShadowFallsTheSameWay()
        {
            var box = new RectangleElement(Guid.NewGuid(), new RectD(60, 30, 80, 40))
            {
                Rotation = 90,
                Style = Plain with { Fill = FillStyle.Of(Green), Shadow = new ShadowStyle { Enabled = true, Size = 0, Darkness = 1, Offset = new PointD(10, 0) } }
            };

            using var image = DocumentRenderer.Render(Page(new Canvas(), White, box));

            Assert.Equal(200, image.Width);
            // Turned around (100, 50): x 80 - 120, y 10 - 90
            Assert.Equal(Of(Green), image[100, 15]);
            Assert.Equal(Of(White), image[65, 50]);
            // The shadow offset is on the canvas: still to the right, not below
            Assert.Equal(new Rgba32(0, 0, 0, 255), image[125, 50]);
            Assert.Equal(Of(White), image[100, 95]);
        }

        [Fact]
        public void ATurnedObfuscate_ChangesOnlyThePixelsOfItsTurnedShape()
        {
            var obfuscate = new ObfuscateElement(Guid.NewGuid(), new RectD(60, 40, 80, 20), ObfuscateMode.Pixelate, 8) { Rotation = 45 };

            using var image = DocumentRenderer.Render(Page(new Canvas(), null, obfuscate));

            // In the corner of the box around it, outside of the turned shape, the stripes stay
            Assert.Equal(new Rgba32(0, 0, 0), image[130, 20]);
            Assert.Equal(new Rgba32(255, 255, 255), image[131, 20]);
            // In the middle the stripes are mixed: grey, not black or white
            var middle = image[100, 50];
            Assert.InRange(middle.R, 60, 195);
        }

        [Fact]
        public void AFrameBand_LiesByItsPlacement_TheTeethCutInward()
        {
            var document = Page(new Canvas { Fit = CanvasFit.Content }, Green);
            var band = new FrameElement(Guid.NewGuid(), new RectD(0, 0, 200, 100), 1)
            {
                Target = CaptureId(document),
                Placement = FramePlacement.Outside,
                Band = new FrameBand(10, White),
                Shape = new EdgeShape(EdgeStyle.None),
                Style = Plain
            };
            var torn = new FrameElement(Guid.NewGuid(), new RectD(0, 0, 200, 100), 7) { Style = Plain };

            using var outside = DocumentRenderer.Render(document with { Layers = new List<Layer> { document.Layers[0], document.Layers[1] with { Elements = new List<Element> { band } } } });
            using var cut = DocumentRenderer.Render(Page(new Canvas(), Green, torn));

            // The canvas grows by the band: the capture starts at 10, 10
            Assert.Equal(220, outside.Width);
            Assert.Equal(Of(White), outside[5, 50]);
            Assert.Equal(Of(Green), outside[15, 50]);
            // Torn: the corner is cut off, transparent, the middle stays
            Assert.Equal(0, cut[0, 0].A);
            Assert.Equal(Of(Green), cut[100, 50]);
        }

        [Fact]
        public void AFrame_FollowsItsTurnedTarget()
        {
            var box = new RectangleElement(Guid.NewGuid(), new RectD(60, 30, 80, 40)) { Rotation = 90, Style = Plain with { Fill = FillStyle.Of(Green) } };
            var frame = new FrameElement(Guid.NewGuid(), default, 1) { Target = box.Id, Band = new FrameBand(5, Black), Shape = new EdgeShape(EdgeStyle.None), Style = Plain };

            using var image = DocumentRenderer.Render(Page(new Canvas(), White, box, frame));

            // The band inside the turned box: x 80 - 120, y 10 - 90
            Assert.Equal(Of(Black), image[82, 50]);
            Assert.Equal(Of(Black), image[100, 12]);
            Assert.Equal(Of(Green), image[100, 50]);
            // Where the band of the upright box would be
            Assert.Equal(Of(White), image[62, 50]);
        }

        [Fact]
        public void TurnedText_StaysInItsTurnedBox()
        {
            var text = new TextElement(Guid.NewGuid(), new RectD(40, 40, 120, 20), "IIIIII")
            {
                Rotation = 90,
                Style = Plain with { Text = new TextStyle { Size = 16, Color = Black, Wrap = false } }
            };

            using var image = DocumentRenderer.Render(Page(new Canvas(), White, text));

            // Turned around (100, 50): x 90 - 110, y -10 - 110
            int Dark(int fromX, int toX)
            {
                int count = 0;
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = fromX; x < toX; x++)
                    {
                        count += image[x, y].R < 128 ? 1 : 0;
                    }
                }

                return count;
            }

            Assert.True(Dark(90, 110) > 20);
            Assert.Equal(0, Dark(0, 88) + Dark(112, 200));
        }
    }
}
