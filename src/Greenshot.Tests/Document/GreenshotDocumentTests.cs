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

            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(800, 600), new List<Layer> { captureLayer, annotations },
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
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(10, 10), new List<Layer>(), new Dictionary<Guid, Resource>()) { Groups = new[] { marker } };

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
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(10, 10), new List<Layer> { layer }, new Dictionary<Guid, Resource>())
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
