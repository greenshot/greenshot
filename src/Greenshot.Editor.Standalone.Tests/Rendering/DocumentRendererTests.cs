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
using Greenshot.Document;
using Greenshot.Editor.Standalone.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace Greenshot.Editor.Standalone.Tests.Rendering
{
    public class DocumentRendererTests
    {
        [Fact]
        public void TheGallery_IsDrawn()
        {
            using var image = DocumentRenderer.Render(Samples.Gallery());
            Samples.Keep(image, "gallery");

            Assert.Equal(640, image.Width);
            Assert.Equal(420, image.Height);
        }

        /// <summary>
        /// The biggest difference of a colour channel between two images of the same size
        /// </summary>
        private static int Difference(Image<Rgba32> a, Image<Rgba32> b)
        {
            Assert.Equal(a.Size, b.Size);
            int worst = 0;
            for (int y = 0; y < a.Height; y++)
            {
                for (int x = 0; x < a.Width; x++)
                {
                    Rgba32 p = a[x, y], q = b[x, y];
                    worst = Math.Max(worst, Math.Max(Math.Max(Math.Abs(p.R - q.R), Math.Abs(p.G - q.G)), Math.Max(Math.Abs(p.B - q.B), Math.Abs(p.A - q.A))));
                }
            }

            return worst;
        }

        [Theory]
        [InlineData(300, 200, 40, 30)]
        [InlineData(-8, -8, 70, 50)]
        [InlineData(600, 380, 46, 46)]
        [InlineData(100, 400, 200, 26)]
        public void APartOfACaptureWithTornEdges_IsDrawnLikeTheWhole_AndOnlyThatPartIsDrawn(double x, double y, double width, double height)
        {
            var document = Greenshot.Editor.Standalone.Editing.CaptureFrames.WithFrame(Samples.Gallery(), frame => frame with
            {
                Shape = new EdgeShape(EdgeStyle.Torn),
                Style = frame.Style with { Shadow = new ShadowStyle { Enabled = true, Size = 7, Darkness = 0.6, Offset = new PointD(-1, -1) } }
            });
            var canvas = document.CanvasArea();
            using var cache = new RenderCache();
            using var whole = DocumentRenderer.Render(document, cache);
            var area = DocumentRenderer.Affected(document, new RectD(x, y, width, height));
            Assert.True(area.Width <= width + 2 && area.Height <= height + 2, $"Only the changed part is drawn, not {area}");

            using var part = DocumentRenderer.Render(document, area, cache);
            Samples.Keep(part, $"torn-part-{x}-{y}");
            using var expected = whole.Clone(context => context.Crop(new Rectangle((int)(area.X - canvas.X), (int)(area.Y - canvas.Y), (int)area.Width, (int)area.Height)));
            Assert.True(Difference(expected, part) <= 2, $"A part differs from the whole by {Difference(expected, part)}");

            // The frame is made again with every change (the same values in another object): that alone changes nothing
            var again = document with { Layers = document.Layers.Select(layer => layer with { Elements = layer.Elements.Select(element => element is FrameElement frame ? frame with { Seed = frame.Seed } : element).ToList() }).ToList() };
            Assert.Equal(default(RectD), DocumentDiff.Changed(document, again));
        }

        [Theory]
        [InlineData(400, 130, 30, 30)]
        [InlineData(10, 10, 300, 200)]
        [InlineData(205.5, 285.5, 100, 60)]
        [InlineData(500, 255, 20, 20)]
        public void APartOfTheDocument_IsDrawnLikeTheWholeDocument(double x, double y, double width, double height)
        {
            var document = Samples.Gallery();
            using var cache = new RenderCache();
            using var whole = DocumentRenderer.Render(document, cache);
            var area = DocumentRenderer.Affected(document, new RectD(x, y, width, height));

            using var part = DocumentRenderer.Render(document, area, cache);
            Samples.Keep(part, $"part-{x}-{y}");

            using var expected = whole.Clone(context => context.Crop(new Rectangle((int)area.X, (int)area.Y, (int)area.Width, (int)area.Height)));
            Assert.True(Difference(expected, part) <= 2, $"A part differs from the whole by {Difference(expected, part)}");
        }

        /// <summary>
        /// What the canvas does while an element is dragged: only the area in which the document changed is drawn again
        /// </summary>
        [Theory]
        [InlineData("emoji")]
        [InlineData("bubble")]
        [InlineData("step")]
        public void DrawingOnlyTheChanges_OfATurningElement_EndsLikeTheWholeDocument(string kind)
        {
            Element Make(double rotation) => kind switch
            {
                "emoji" => new EmojiElement(Guid.Parse("11111111-1111-1111-1111-111111111111"), new RectD(150, 100, 90, 60), "\U0001F600") { Rotation = rotation },
                "bubble" => new SpeechBubbleElement(Guid.Parse("11111111-1111-1111-1111-111111111111"), new RectD(150, 100, 120, 60), "Turned", new PointD(120, 220)) { Rotation = rotation },
                _ => new StepLabelElement(Guid.Parse("11111111-1111-1111-1111-111111111111"), new RectD(150, 100, 40, 40)) { Rotation = rotation }
            };
            var document = Samples.Page(400, 300, null, null, Make(0));
            using var cache = new RenderCache();
            using var shown = DocumentRenderer.Render(document, cache);
            for (int step = 1; step <= 40; step++)
            {
                var next = Greenshot.Document.Editing.DocumentEdits.Replace(document, Make(step * 23 % 360));
                var changed = Assert.IsType<RectD>(DocumentDiff.Changed(document, next));
                var area = DocumentRenderer.Affected(next, changed);
                using (var part = DocumentRenderer.Render(next, area, cache))
                {
                    shown.Mutate(context => context.DrawImage(part, new Point((int)area.X, (int)area.Y), PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.Src, 1f));
                }

                document = next;
                using var whole = DocumentRenderer.Render(document, cache);
                int difference = Difference(whole, shown);
                if (difference > 2)
                {
                    Samples.Keep(shown, $"turning-{kind}-shown");
                    Samples.Keep(whole, $"turning-{kind}-whole");
                }

                Assert.True(difference <= 2, $"After step {step} the shown {kind} differs from the document by {difference}");
            }
        }

        [Fact]
        public void ATurnedEmoji_IsDrawnTheSame_WithAndWithoutWhatWasDrawnBefore()
        {
            var id = Guid.NewGuid();
            var upright = Samples.Page(400, 300, null, null, new EmojiElement(id, new RectD(60, 60, 64, 64), "\U0001F600"));
            var turned = Samples.Page(400, 300, null, null, new EmojiElement(id, new RectD(60, 60, 64, 64), "\U0001F600") { Rotation = 90 });
            using var cache = new RenderCache();
            using var first = DocumentRenderer.Render(upright, cache);
            using var again = DocumentRenderer.Render(turned, cache);
            using var fresh = DocumentRenderer.Render(turned);
            Samples.Keep(again, "emoji-turned-cached");
            Samples.Keep(fresh, "emoji-turned-fresh");

            Assert.True(Difference(first, fresh) > 50, "A turned emoji looks different");
            Assert.True(Difference(again, fresh) <= 2, $"The emoji drawn after another differs by {Difference(again, fresh)}");
        }

        /// <summary>
        /// The same text or emoji twice in one document, the second one turned (or of another size): what was drawn for the
        /// first must not be used again for the second
        /// </summary>
        [Theory]
        [InlineData("emoji", 90, 64)]
        [InlineData("emoji", 0, 120)]
        [InlineData("emoji", 200, 40)]
        [InlineData("text", 90, 64)]
        [InlineData("text", 30, 90)]
        [InlineData("step", 180, 64)]
        [InlineData("step", 0, 30)]
        public void TheSameTwice_TheSecondTurnedOrResized_IsDrawnLikeItIsAlone(string kind, double rotation, double size)
        {
            Element Make(double x, double turned, double side) => kind switch
            {
                "emoji" => new EmojiElement(Guid.NewGuid(), new RectD(x, 80, side, side), "\U0001F600") { Rotation = turned },
                "text" => new TextElement(Guid.NewGuid(), new RectD(x, 80, side, side), "Ag1") { Rotation = turned, Style = new Style { Text = new TextStyle { Size = side / 3 } } },
                _ => new StepLabelElement(Guid.NewGuid(), new RectD(x, 80, side, side)) { Rotation = turned }
            };
            var second = Make(220, rotation, size);
            using var two = DocumentRenderer.Render(Samples.Page(400, 300, Samples.White, null, Make(40, 0, 64), second));
            using var one = DocumentRenderer.Render(Samples.Page(400, 300, Samples.White, null, kind == "step" ? Make(-500, 0, 64) : Make(-500, 0, 64), second));
            Samples.Keep(two, $"twice-{kind}-{rotation}-{size}");
            var right = new Rectangle(160, 20, 240, 240);
            using var a = two.Clone(context => context.Crop(right));
            using var b = one.Clone(context => context.Crop(right));

            Assert.True(Difference(a, b) <= 2, $"The second {kind} differs by {Difference(a, b)} from the same one drawn alone");
        }

        [Fact]
        public void TornEdgesWithAShadow_GrowTheCanvasByTheShadow()
        {
            var document = Greenshot.Editor.Standalone.Editing.CaptureFrames.WithFrame(Samples.Page(480, 300, null, null), frame => frame with
            {
                Shape = new EdgeShape(EdgeStyle.Torn),
                Style = new Style { Shadow = new ShadowStyle { Enabled = true, Size = 7, Darkness = 0.6, Offset = new PointD(-1, -1) } }
            });

            Assert.Equal(new RectD(-8, -8, 494, 314), document.CanvasArea());
            using var image = DocumentRenderer.Render(document);
            Samples.Keep(image, "torn");
            Assert.Equal(494, image.Width);
            // The corner is cut off and the shadow doesn't reach it; next to the edge the shadow is dark
            Assert.True(image[0, 0].A < 40, $"Nothing in the corner: {image[0, 0]}");
            int darkest = Enumerable.Range(0, 22).Max(y => image[247, y].R < 40 ? image[247, y].A : 0);
            Assert.True(darkest > 40, $"The shadow above the top edge, at most {darkest}");
            Assert.Equal(new Rgba32(250, 250, 245), image[247, 165]);
        }
    }
}
