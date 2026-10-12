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
                Style = new ElementStyle(Stroke: new Stroke(Rgba.FromArgb(unchecked((int)0xFFFF0000)), 2), Shadow: Shadow.Default)
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
            Assert.Equal(1, document.Canvas.CounterStart);
            Assert.True(document.Layers[0].Locked);
            Assert.True(document.Layers[1].Visible);
        }
    }
}
