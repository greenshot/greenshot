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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editor;
using Greenshot.Document.Legacy;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Tests.Document
{
    public class DocumentToSurfaceTests
    {
        private static readonly string TestDataDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "LegacyFormat");

        public DocumentToSurfaceTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        public static IEnumerable<object[]> GreenshotFiles() => Directory.GetFiles(Path.Combine(TestDataDirectory, "Greenshotfile"), "*.greenshot", SearchOption.AllDirectories)
            .Select(file => new object[] { file.Substring(TestDataDirectory.Length + 1) });

        /// <summary>
        /// Ids are new on every mapping and the image bytes are encoded again: compare without them, but with the media type
        /// </summary>
        private static object Comparable(GreenshotDocument document, Element element) => element switch
        {
            ImageElement image => (image with { Id = Guid.Empty, Resource = Guid.Empty }, document.Resources[image.Resource].MediaType),
            CursorElement cursor => cursor with { Id = Guid.Empty, Resource = Guid.Empty, MaskResource = cursor.MaskResource.HasValue ? Guid.Empty : (Guid?)null },
            FreehandElement freehand => (freehand with { Id = Guid.Empty, Points = null }, string.Join(";", freehand.Points)),
            _ => element with { Id = Guid.Empty }
        };

        [Theory]
        [MemberData(nameof(GreenshotFiles))]
        public void ToSurface_AndBack_KeepsTheElements(string relativePath)
        {
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, relativePath));
            var document = LegacyToDocument.ReadGreenshotFile(stream);

            using var surface = DocumentToSurface.ToSurface(document);
            var mappedBack = SurfaceToDocument.ToDocument(surface);

            Assert.Equal(document.Id, mappedBack.Id);
            Assert.Equal(document.Canvas, mappedBack.Canvas);
            Assert.Equal(document.Layers[1].Elements.Select(element => Comparable(document, element)),
                mappedBack.Layers[1].Elements.Select(element => Comparable(mappedBack, element)));
        }

        [Fact]
        public void ToImage_DrawsTheElementsOnTheCapture()
        {
            using var white = new Bitmap(100, 50);
            using (var graphics = Graphics.FromImage(white))
            {
                graphics.Clear(Color.White);
            }

            using var png = new MemoryStream();
            white.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            var capture = new Resource(Guid.NewGuid(), "image/png", png.ToArray());
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 20, 20)) { Style = new ElementStyle(Fill: new Fill(new Rgba(255, 0, 0, 255))) };
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(100, 50), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 100, 50), capture.Id) }),
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, new List<Element> { rectangle })
            }, new Dictionary<Guid, Resource> { [capture.Id] = capture });

            using var image = (Bitmap)DocumentToSurface.ToImage(document);

            Assert.Equal(new Size(100, 50), image.Size);
            Assert.Equal(Color.Red.ToArgb(), image.GetPixel(20, 20).ToArgb());
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(60, 40).ToArgb());
        }

        /// <summary>
        /// An EMF with a rectangle, recorded with GDI+
        /// </summary>
        private static byte[] CreateEmf()
        {
            using var stream = new MemoryStream();
            using (var reference = Graphics.FromHwnd(IntPtr.Zero))
            {
                var hdc = reference.GetHdc();
                try
                {
                    using var metafile = new Metafile(stream, hdc, EmfType.EmfPlusDual);
                    using var graphics = Graphics.FromImage(metafile);
                    graphics.FillRectangle(Brushes.Blue, 10, 10, 80, 40);
                }
                finally
                {
                    reference.ReleaseHdc(hdc);
                }
            }

            return stream.ToArray();
        }

        [Fact]
        public void Metafile_KeepsTheEmfFile()
        {
            byte[] emf = CreateEmf();
            using var surface = new Surface(new Bitmap(200, 100));
            var container = Assert.IsType<MetafileContainer>(Assert.Single(new MetaFileFormatHandler().LoadDrawablesFromStream(new MemoryStream(emf), ".emf", surface)));
            surface.AddElement(container, false, false);

            var document = SurfaceToDocument.ToDocument(surface);
            var image = Assert.IsType<ImageElement>(Assert.Single(document.Layers[1].Elements));
            Assert.Equal("image/emf", document.Resources[image.Resource].MediaType);
            Assert.Equal(emf, document.Resources[image.Resource].Data);

            using var mappedSurface = DocumentToSurface.ToSurface(document);
            var mapped = Assert.IsType<MetafileContainer>(Assert.Single(mappedSurface.Elements));
            var mappedBack = SurfaceToDocument.ToDocument(mappedSurface);
            var mappedImage = (ImageElement)Assert.Single(mappedBack.Layers[1].Elements);
            Assert.Equal(emf, mappedBack.Resources[mappedImage.Resource].Data);
            Assert.Equal(new Rectangle(container.Left, container.Top, container.Width, container.Height), new Rectangle(mapped.Left, mapped.Top, mapped.Width, mapped.Height));
        }
    }
}
