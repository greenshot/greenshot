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
using System.Drawing;
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editor;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Xunit;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Tests.Document
{
    public class SurfaceToDocumentTests
    {
        public SurfaceToDocumentTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// Stands in for a container of a plugin
        /// </summary>
        private sealed class PluginContainer : RectangleContainer
        {
            public PluginContainer(Surface parent) : base(parent)
            {
            }
        }

        private static T Add<T>(Surface surface, T container, int left, int top, int width, int height) where T : DrawableContainer
        {
            container.Left = left;
            container.Top = top;
            container.Width = width;
            container.Height = height;
            surface.AddElement(container, false, false);
            return container;
        }

        [Fact]
        public void ToDocument_PutsTheImageOnACaptureLayer_AndTheElementsInOrderOnAnAnnotationsLayer()
        {
            using var surface = new Surface(new Bitmap(200, 100)) { CounterStart = 3 };
            Add(surface, new RectangleContainer(surface), 10, 10, 50, 20);
            Add(surface, new ArrowContainer(surface), 100, 80, -40, -30);
            Add(surface, new StepLabelContainer(surface), 5, 5, 30, 30);

            var document = SurfaceToDocument.ToDocument(surface);

            Assert.Equal(surface.ID, document.Id);
            Assert.Equal(new Canvas(200, 100) { CounterStart = 3 }, document.Canvas);
            Assert.Equal(2, document.Layers.Count);
            var capture = Assert.IsType<ImageElement>(Assert.Single(document.Layers[0].Elements));
            Assert.Equal(new RectD(0, 0, 200, 100), capture.Area);
            Assert.Equal("image/png", document.Resources[capture.Resource].MediaType);

            var elements = document.Layers[1].Elements;
            Assert.Equal(3, elements.Count);
            var rectangle = Assert.IsType<RectangleElement>(elements[0]);
            Assert.Equal(new RectD(10, 10, 50, 20), rectangle.Area);
            Assert.Equal(new Stroke(Rgba.FromArgb(Color.Red.ToArgb()), 2), rectangle.Style.Stroke);
            Assert.Equal(new Fill(Rgba.FromArgb(Color.Transparent.ToArgb())), rectangle.Style.Fill);
            Assert.Same(Shadow.Default, rectangle.Style.Shadow);

            var arrow = Assert.IsType<ArrowElement>(elements[1]);
            Assert.Equal(new PointD(100, 80), arrow.Start);
            Assert.Equal(new PointD(60, 50), arrow.End);
            Assert.Equal(ArrowHeads.End, arrow.Heads);

            var stepLabel = Assert.IsType<StepLabelElement>(elements[2]);
            Assert.Equal(0, stepLabel.Style.Stroke.Width);
            Assert.Equal(Rgba.FromArgb(Color.DarkRed.ToArgb()), stepLabel.Style.Fill.Color);
        }

        [Fact]
        public void ToDocument_MapsTextAndItsStyle()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            var text = Add(surface, new TextContainer(surface), 10, 10, 80, 30);
            text.Text = "Hello";
            text.SetFieldValue(FieldType.FONT_BOLD, true);
            text.SetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Near);

            var element = Assert.IsType<TextElement>(Assert.Single(SurfaceToDocument.ToDocument(surface).Layers[1].Elements));

            Assert.Equal("Hello", element.Text);
            Assert.True(element.Style.Text.Font.Bold);
            Assert.Equal(11, element.Style.Text.Font.Size);
            Assert.Equal(TextAlignment.Near, element.Style.Text.Horizontal);
            Assert.Equal(TextAlignment.Center, element.Style.Text.Vertical);
            Assert.Equal(element.Style.Stroke.Color, element.Style.Text.Color);
        }

        [Fact]
        public void ToDocument_MapsTheFiltersWithTheirSettings()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            Add(surface, new ObfuscateContainer(surface), 0, 0, 20, 20);
            var blur = Add(surface, new ObfuscateContainer(surface), 0, 0, 20, 20);
            blur.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, PreparedFilter.BLUR);
            Add(surface, new HighlightContainer(surface), 0, 0, 20, 20);
            var magnify = Add(surface, new HighlightContainer(surface), 0, 0, 20, 20);
            magnify.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, PreparedFilter.MAGNIFICATION);

            var elements = SurfaceToDocument.ToDocument(surface).Layers[1].Elements;

            Assert.Equal(new ObfuscateElement(elements[0].Id, new RectD(0, 0, 20, 20), ObfuscateMode.Pixelate, 5) { Style = elements[0].Style }, elements[0]);
            Assert.Equal(ObfuscateMode.Blur, Assert.IsType<ObfuscateElement>(elements[1]).Mode);
            Assert.Equal(3, ((ObfuscateElement)elements[1]).Size);
            var textHighlight = Assert.IsType<HighlightElement>(elements[2]);
            Assert.Equal(HighlightMode.Text, textHighlight.Mode);
            Assert.Equal(Rgba.FromArgb(Color.Yellow.ToArgb()), textHighlight.Color);
            Assert.Null(textHighlight.Style.Fill);
            Assert.Equal(HighlightMode.Magnify, Assert.IsType<HighlightElement>(elements[3]).Mode);
            Assert.Equal(2, ((HighlightElement)elements[3]).Magnification);
        }

        [Fact]
        public void ToDocument_MapsAPluginContainerToAnUnknownElementWithARendering()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            var container = Add(surface, new PluginContainer(surface), 10, 10, 30, 20);

            var document = SurfaceToDocument.ToDocument(surface);

            var unknown = Assert.IsType<UnknownElement>(Assert.Single(document.Layers[1].Elements));
            Assert.Equal(typeof(PluginContainer).FullName, unknown.Type);
            var drawingBounds = container.DrawingBounds;
            Assert.Equal(new RectD(drawingBounds.X, drawingBounds.Y, drawingBounds.Width, drawingBounds.Height), unknown.Area);
            Assert.Equal("image/png", document.Resources[unknown.Fallback.Value].MediaType);
        }

        [Fact]
        public void ToDocument_SkipsTheCropContainer()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            Add(surface, new CropContainer(surface), 10, 10, 30, 20);

            Assert.Empty(SurfaceToDocument.ToDocument(surface).Layers[1].Elements);
        }
    }
}
