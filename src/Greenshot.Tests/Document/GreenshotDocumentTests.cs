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
using Greenshot.Document;
using Xunit;

namespace Greenshot.Tests.Document
{
    public class GreenshotDocumentTests
    {
        [Theory]
        [InlineData(unchecked((int)0xFFE53935))]
        [InlineData(0x00000000)]
        [InlineData(0x7F102030)]
        public void Rgba_ArgbRoundTrip(int argb)
        {
            Assert.Equal(argb, Rgba.FromArgb(argb).ToArgb());
        }

        [Fact]
        public void Rgba_ToString_IsHexWithAlphaLast()
        {
            Assert.Equal("#E53935FF", Rgba.FromArgb(unchecked((int)0xFFE53935)).ToString());
        }

        [Theory]
        [InlineData(StrokePosition.Center, -5, 110)]
        [InlineData(StrokePosition.Inside, 0, 100)]
        [InlineData(StrokePosition.Outside, -10, 120)]
        public void VisualBounds_HaveThePartOfTheLineOutsideOfTheShape(StrokePosition position, double start, double size)
        {
            var line = new Style { Stroke = StrokeStyle.Of(new Rgba(255, 0, 0, 255), 10) with { Position = position }, Shadow = new ShadowStyle { Enabled = false } };
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 100, 100)) { Style = line };
            // A line has no inside or outside, its line is always centred
            var arrow = new ArrowElement(Guid.NewGuid(), new PointD(0, 0), new PointD(100, 0)) { Style = line };
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer>(), new Dictionary<Guid, Resource>());

            Assert.Equal(new RectD(start, start, size, size), document.VisualBounds(rectangle));
            Assert.Equal(new RectD(0, 0, 100, 100), rectangle.Bounds);
            Assert.Equal(new RectD(-5, -5, 110, 10), document.VisualBounds(arrow));
        }

        [Theory]
        [InlineData(FramePlacement.Inside, 0)]
        [InlineData(FramePlacement.Center, -8)]
        [InlineData(FramePlacement.Outside, -16)]
        public void AFrame_DrawsItsBandWhereItIsPlaced_AndTheShadowOutsideOfIt(FramePlacement placement, double start)
        {
            var frame = new FrameElement(Guid.NewGuid(), new RectD(0, 0, 200, 100), 1)
            {
                Placement = placement,
                Band = new FrameBand(16, new Rgba(255, 255, 255, 255)),
                Style = new Style { Shadow = new ShadowStyle { Enabled = true, Size = 2, Offset = new PointD(3, 3) } }
            };
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas { Fit = CanvasFit.Content }, new List<Layer>
            {
                new(Guid.NewGuid(), "Frame", LayerKind.Custom, new List<Element> { frame })
            }, new Dictionary<Guid, Resource>());

            Assert.Equal(new RectD(start, start, 200 - 2 * start, 100 - 2 * start), frame.OuterEdge);
            Assert.Equal(new RectD(0, 0, 200, 100), frame.Bounds);
            Assert.Equal(new RectD(start, start, 200 - 2 * start + 5, 100 - 2 * start + 5), document.CanvasArea());
            // Without a band there is nothing to place
            Assert.Equal(frame.Area, (frame with { Band = null }).OuterEdge);
        }

        [Fact]
        public void AFramedElement_HasNoShadowOfItsOwn_OnlyTheFirstFrameCounts_ALineIsNeverFramed()
        {
            var shadow = new Style { Shadow = new ShadowStyle { Enabled = true, Size = 4 } };
            var image = new ImageElement(Guid.NewGuid(), new RectD(0, 0, 100, 50), Guid.NewGuid()) { Style = shadow };
            var line = new LineElement(Guid.NewGuid(), new PointD(0, 0), new PointD(10, 10)) { Style = shadow };
            var first = new FrameElement(Guid.NewGuid(), image.Area, 1) { Target = image.Id };
            var second = new FrameElement(Guid.NewGuid(), image.Area, 2) { Target = image.Id };
            var onLine = new FrameElement(Guid.NewGuid(), line.Bounds, 3) { Target = line.Id };
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer>
            {
                new(Guid.NewGuid(), "Elements", LayerKind.Custom, new List<Element> { image, line, first, second, onLine })
            }, new Dictionary<Guid, Resource>());

            Assert.Same(first, document.FrameOf(image));
            Assert.False(document.StyleOf(image).Shadow.Enabled);
            Assert.True(document.CascadeOf(image).Shadow.Enabled);
            Assert.Null(document.FrameOf(line));
            Assert.True(document.StyleOf(line).Shadow.Enabled);
        }

        [Fact]
        public void ATurnedElement_HasTheBoxAroundItAsBounds_ALineKeepsItsPoints()
        {
            var box = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 100, 50)) { Rotation = 90 };
            var line = new LineElement(Guid.NewGuid(), new PointD(0, 0), new PointD(100, 0)) { Rotation = 90 };

            Assert.Equal(new RectD(25, -25, 50, 100), box.Bounds);
            Assert.Equal(new RectD(0, 0, 100, 50), box.Area);
            Assert.Equal(new RectD(0, 0, 100, 0), line.Bounds);
        }

        private static GreenshotDocument Page(Canvas canvas, params Element[] annotations) =>
            new(Guid.NewGuid(), canvas, new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 200, 100), Guid.NewGuid()) }),
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, annotations)
            }, new Dictionary<Guid, Resource>());

        [Fact]
        public void ATurnedCapture_KeepsItsPixels_EverythingStaysOnItsSpot_AndTurnsBackExactly()
        {
            var document = Page(new Canvas(),
                new ArrowElement(Guid.NewGuid(), new PointD(10, 10), new PointD(50, 10)),
                new ObfuscateElement(Guid.NewGuid(), new RectD(20, 60, 40, 20), ObfuscateMode.Pixelate, 8),
                new TextElement(Guid.NewGuid(), new RectD(100, 20, 60, 20), "Hi"),
                new RectangleElement(Guid.NewGuid(), new RectD(100, 60, 40, 20)) { Rotation = 30 });

            var turned = CaptureTurn.Apply(document, 90);
            var capture = (ImageElement)turned.Layers[0].Elements[0];
            var elements = turned.Layers[1].Elements;

            // A point x, y of the 200 x 100 capture is at 100 - y, x of the 100 x 200 one
            Assert.Equal(new RectD(-50, 50, 200, 100), capture.Area);
            Assert.Equal(90, capture.Rotation);
            Assert.Equal(((ImageElement)document.Layers[0].Elements[0]).Resource, capture.Resource);
            Assert.Equal(new RectD(0, 0, 100, 200), turned.CanvasArea());
            Assert.Equal(new PointD(90, 10), ((ArrowElement)elements[0]).Start);
            Assert.Equal(new PointD(90, 50), ((ArrowElement)elements[0]).End);
            Assert.Equal(new RectD(20, 20, 20, 40), elements[1].Bounds);
            Assert.Equal(0, elements[1].Rotation);
            Assert.Equal(new RectD(40, 120, 60, 20), ((TextElement)elements[2]).Area);
            Assert.Equal(90, elements[2].Rotation);
            Assert.Equal(new RectD(20, 100, 20, 40), ((RectangleElement)elements[3]).Area);
            Assert.Equal(30, elements[3].Rotation);

            var back = CaptureTurn.Apply(turned, -90);
            for (int layer = 0; layer < 2; layer++)
            {
                Assert.Equal(document.Layers[layer].Elements, back.Layers[layer].Elements);
            }

            Assert.Same(document, CaptureTurn.Apply(document, 360));
            Assert.Throws<ArgumentOutOfRangeException>(() => CaptureTurn.Apply(document, 45));
        }

        [Fact]
        public void ATurnedCapture_WithTextUpright_MovesTheText_ButRedactionAndImagesStillTurn()
        {
            var document = Page(Canvas.Fixed(200, 100),
                new TextElement(Guid.NewGuid(), new RectD(100, 20, 60, 20), "Hi"),
                new RectangleElement(Guid.NewGuid(), new RectD(100, 60, 40, 20)) { Label = new BoxLabel("Login") { Y = LabelPlace.After } },
                new ObfuscateElement(Guid.NewGuid(), new RectD(20, 60, 40, 20), ObfuscateMode.Pixelate, 8),
                new ImageElement(Guid.NewGuid(), new RectD(10, 10, 20, 10), Guid.NewGuid()),
                new FrameElement(Guid.NewGuid(), new RectD(0, 0, 200, 100), 1) { Sides = FrameSides.Except(Edges.Bottom) },
                new CutMarkElement(Guid.NewGuid(), new RectD(0, 40, 200, 20), true, 1));

            var turned = CaptureTurn.Apply(document, 90, keepTextUpright: true);
            var elements = turned.Layers[1].Elements;

            Assert.Equal(new Canvas { Fit = CanvasFit.Fixed, Width = 100, Height = 200 }, turned.Canvas);
            Assert.Equal(new RectD(40, 120, 60, 20), elements[0].Bounds);
            Assert.Equal(0, elements[0].Rotation);
            // The rectangle is swapped, its label stays readable and goes from below to the left, as the bottom side does
            var rectangle = (RectangleElement)elements[1];
            Assert.Equal(new RectD(20, 100, 20, 40), rectangle.Area);
            Assert.Equal(new BoxLabel("Login") { X = LabelPlace.Before, Y = LabelPlace.Center }, rectangle.Label);
            Assert.Equal(new RectD(20, 20, 20, 40), elements[2].Bounds);
            Assert.Equal(90, elements[3].Rotation);
            Assert.Equal(new RectD(0, 0, 100, 200), elements[4].Bounds);
            // Only the bottom side was torn, now the left one is
            Assert.Null(((FrameElement)elements[4]).Sides.Left);
            Assert.Equal(new EdgeShape(EdgeStyle.None), ((FrameElement)elements[4]).Sides.Bottom);
            Assert.False(((CutMarkElement)elements[5]).Horizontal);
            Assert.Equal(new RectD(40, 0, 20, 200), elements[5].Bounds);
        }

        [Fact]
        public void AnElementAnchoredBelowTheCapture_IsLeftOfItAfterAQuarterTurn()
        {
            var caption = new TextElement(Guid.NewGuid(), new RectD(70, 108, 60, 20), "Caption")
            {
                Anchor = new Anchor { X = new AxisAnchor(AnchorLine.Center, AnchorLine.Center), Y = new AxisAnchor(AnchorLine.End, AnchorLine.Start, 8) }
            };

            var turned = (TextElement)CaptureTurn.Apply(Page(new Canvas(), caption), 90).Layers[1].Elements[0];

            Assert.Equal(new Anchor { X = new AxisAnchor(AnchorLine.Start, AnchorLine.End, -8), Y = new AxisAnchor(AnchorLine.Center, AnchorLine.Center) }, turned.Anchor);
            Assert.Equal(new RectD(-28, 70, 20, 60), turned.Bounds);
        }

        [Fact]
        public void ALabel_IsInTheVisualBounds_NotInTheBounds_AndTurnsWithItsElement()
        {
            var text = new Style { Text = new TextStyle { Size = 10, LineHeight = 1.5 } };
            var image = new ImageElement(Guid.NewGuid(), new RectD(0, 0, 200, 100), Guid.NewGuid())
            {
                Label = new BoxLabel("Figure 1") { Y = LabelPlace.After, Offset = 8 },
                Style = text
            };
            var line = new LineElement(Guid.NewGuid(), new PointD(0, 0), new PointD(100, 0))
            {
                Label = new LineLabel("abc") { Offset = 4 },
                Style = new Style { Text = new TextStyle { Size = 10, LineHeight = 1 } }
            };
            var turned = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 100, 50))
            {
                Rotation = 90,
                Label = new BoxLabel("Login") { Y = LabelPlace.After },
                Style = new Style { Text = new TextStyle { Size = 10, LineHeight = 1 } }
            };
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer>(), new Dictionary<Guid, Resource>());

            // A caption below the image, 8 px away and one line of 15 px high
            Assert.Equal(new RectD(0, 0, 200, 123), document.VisualBounds(image));
            Assert.Equal(new RectD(0, 0, 200, 100), image.Bounds);
            // 4 px above the 2 px line: three letters of 6 px, 10 px high
            Assert.Equal(new RectD(-1, -14, 102, 15), document.VisualBounds(line));
            // Below the rectangle before it turns, left of it after
            Assert.Equal(new RectD(15, -26, 61, 102), document.VisualBounds(turned));
        }

        [Fact]
        public void OcrText_AtAnAngle_IsCoveredExactly()
        {
            var image = new ImageElement(Guid.NewGuid(), new RectD(0, 0, 200, 200), Guid.NewGuid());
            var ocr = new OcrResult(new List<OcrLine>()) { TextAngle = 90 };

            // The straightened word at the left middle of the 400 x 400 image is at the top middle, the image is shown at half its size
            var (area, rotation) = ocr.OnCanvas(new RectD(0, 190, 20, 20), image, 400, 400);

            Assert.Equal(new RectD(95, 0, 10, 10), area);
            Assert.Equal(90, rotation);
        }

        [Fact]
        public void PointElements_DeriveBounds()
        {
            var arrow = new ArrowElement(Guid.NewGuid(), new PointD(400, 300), new PointD(100, 350));
            Assert.Equal(new RectD(100, 300, 300, 50), arrow.Bounds);

            var freehand = new FreehandElement(Guid.NewGuid(), new List<PointD> { new(5, 7), new(1, 9), new(3, 2) });
            Assert.Equal(new RectD(1, 2, 4, 7), freehand.Bounds);
        }

        [Fact]
        public void With_KeepsIdAndChangesValue()
        {
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 100, 50))
            {
                Style = new Style { Stroke = StrokeStyle.Of(Rgba.FromArgb(unchecked((int)0xFFFF0000)), 2), Shadow = new ShadowStyle { Enabled = true } }
            };
            var moved = rectangle with { Area = new RectD(20, 20, 100, 50) };

            Assert.Equal(rectangle.Id, moved.Id);
            Assert.Same(rectangle.Style, moved.Style);
            Assert.NotEqual(rectangle, moved);
            Assert.Equal(20, moved.Bounds.X);
        }

        [Fact]
        public void Document_HoldsLayersAndResources()
        {
            var capture = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            var captureLayer = new Layer(Guid.NewGuid(), "Capture", LayerKind.Capture,
                new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 800, 600), capture.Id) }) { Locked = true };
            var annotations = new Layer(Guid.NewGuid(), "Annotations", LayerKind.Annotations,
                new List<Element>
                {
                    new StepLabelElement(Guid.NewGuid(), new RectD(10, 10, 30, 30)),
                    new PluginElement(Guid.NewGuid(), new RectD(50, 50, 100, 100), "greenshot.barcode")
                });

            var document = new GreenshotDocument(Guid.NewGuid(), Canvas.Fixed(800, 600), new List<Layer> { captureLayer, annotations },
                new Dictionary<Guid, Resource> { [capture.Id] = capture });

            var image = Assert.IsType<ImageElement>(Assert.Single(document.Layers[0].Elements));
            Assert.Same(capture, document.Resources[image.Resource]);
            Assert.Empty(document.Groups);
            Assert.True(document.Layers[0].Locked);
            Assert.True(document.Layers[1].Visible);
        }

        [Fact]
        public void StyleOf_IsTheOwnValues_OverTheGroupStyle_OverTheDefault()
        {
            var red = new Rgba(255, 0, 0, 255);
            var blue = new Rgba(0, 0, 255, 255);
            var arrow = new ArrowElement(Guid.NewGuid(), new PointD(0, 0), new PointD(10, 10)) { Style = new Style { Stroke = new StrokeStyle { Width = 5 } } };
            var hidden = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 10, 10)) { Style = new Style { Fill = new FillStyle { Enabled = false } } };
            var marker = new Group(Guid.NewGuid(), GroupTypes.Style, new Guid?[] { arrow.Id, hidden.Id })
            {
                Style = new Style { Stroke = StrokeStyle.Of(red, 3), Fill = FillStyle.Of(blue) }
            };
            var document = new GreenshotDocument(Guid.NewGuid(), Canvas.Fixed(10, 10), new List<Layer>(), new Dictionary<Guid, Resource>()) { Groups = new[] { marker } };

            var arrowStyle = document.StyleOf(arrow);
            Assert.Equal(StrokeStyle.Of(red, 5), arrowStyle.Stroke);
            // Not set by the arrow or its group: the default of an arrow
            Assert.Equal(new ShadowStyle { Enabled = true }, arrowStyle.Shadow);

            Assert.Equal(new FillStyle { Enabled = false, Color = blue }, document.StyleOf(hidden).Fill);

            // When the group changes, the members follow except for their own values
            var changed = document with { Groups = new[] { marker with { Style = new Style { Stroke = StrokeStyle.Of(blue, 3) } } } };
            Assert.Equal(StrokeStyle.Of(blue, 5), changed.StyleOf(arrow).Stroke);
        }

        [Fact]
        public void StepLabels_AreNumberedByTheirStepsGroup()
        {
            var first = new StepLabelElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));
            var second = new StepLabelElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));
            var other = new StepLabelElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));
            var single = new StepLabelElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));
            var alone = new StepLabelElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));
            var layer = new Layer(Guid.NewGuid(), "Annotations", LayerKind.Annotations, new List<Element> { first, single, second, other, alone });
            var document = new GreenshotDocument(Guid.NewGuid(), Canvas.Fixed(10, 10), new List<Layer> { layer }, new Dictionary<Guid, Resource>())
            {
                Groups = new[]
                {
                    // The member order is the numbering, not the order of the elements, null skips a number
                    new Group(Guid.NewGuid(), GroupTypes.Steps, new Guid?[] { second.Id, null, first.Id }) { Properties = new StepNumbering(3).ToProperties() },
                    new Group(Guid.NewGuid(), GroupTypes.Steps, new Guid?[] { other.Id }) { Properties = new RawJson("{\"format\":\"upperLetters\",\"newer\":true}") }
                }
            };

            Assert.Equal("3", document.LabelOf(second));
            Assert.Equal("5", document.LabelOf(first));
            Assert.Equal("A", document.LabelOf(other));
            // In no steps group: 1, 2, 3 in drawing order
            Assert.Equal("1", document.LabelOf(single));
            Assert.Equal("2", document.LabelOf(alone));
        }

        [Theory]
        [InlineData(1, StepFormat.Numbers, "1")]
        [InlineData(0, StepFormat.LowerLetters, "0")]
        [InlineData(1, StepFormat.LowerLetters, "a")]
        [InlineData(26, StepFormat.UpperLetters, "Z")]
        [InlineData(27, StepFormat.UpperLetters, "AA")]
        [InlineData(703, StepFormat.LowerLetters, "aaa")]
        [InlineData(4, StepFormat.LowerRoman, "iv")]
        [InlineData(1994, StepFormat.UpperRoman, "MCMXCIV")]
        [InlineData(4000, StepFormat.UpperRoman, "4000")]
        public void StepNumbering_ToText(int number, StepFormat format, string expected)
        {
            Assert.Equal(expected, StepNumbering.ToText(number, format));
        }
    }
}
