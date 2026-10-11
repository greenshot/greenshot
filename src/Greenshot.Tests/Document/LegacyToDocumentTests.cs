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
using System.IO;
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editor;
using Greenshot.Document.Legacy;
using Greenshot.Editor.Drawing;
using Xunit;

namespace Greenshot.Tests.Document
{
    /// <summary>
    /// The legacy reader must give the same document as loading the file the current way (BinaryFormatter into a Surface)
    /// and mapping that Surface with SurfaceToDocument.
    /// </summary>
    public class LegacyToDocumentTests
    {
        private static readonly string TestDataDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "LegacyFormat");

        public LegacyToDocumentTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// Files the current loader can't read: it loses the image of 1.02 image containers, and refuses metafiles
        /// </summary>
        private static readonly string[] NotReadableByTheBinaryFormatter =
        {
            Path.Combine("Greenshotfile", "File_Version_1.02", "ImageContainer_lt_300_200_wh_100_100.greenshot"),
            Path.Combine("Greenshotfile", "File_Version_1.02", "Surface_with_11_different_DrawableContainer.greenshot"),
            Path.Combine("Greenshotfile", "File_Version_1.03", "MetafileContainer_lt_300_200_wh_120_100.greenshot"),
            Path.Combine("Greenshotfile", "File_Version_1.03", "Surface_with_14_different_DrawableContainer.greenshot")
        };

        public static IEnumerable<object[]> GreenshotFiles() => Directory.GetFiles(Path.Combine(TestDataDirectory, "Greenshotfile"), "*.greenshot", SearchOption.AllDirectories)
            .Select(file => file.Substring(TestDataDirectory.Length + 1))
            .Except(NotReadableByTheBinaryFormatter)
            .Select(file => new object[] { file });

        private static GreenshotDocument LoadWithBinaryFormatter(Stream stream)
        {
            Assert.True(LegacyGreenshotFile.TryLocate(stream, out var file));
            stream.Seek(0, SeekOrigin.Begin);
            var png = new byte[file.ElementsOffset];
            Assert.Equal(png.Length, stream.Read(png, 0, png.Length));
            using var surface = new Surface(new Bitmap(new MemoryStream(png)));
            stream.Seek(file.ElementsOffset, SeekOrigin.Begin);
            surface.LoadElementsFromStream(stream);
            return SurfaceToDocument.ToDocument(surface);
        }

        /// <summary>
        /// Ids are new on every mapping, and the image bytes are encoded again by the Surface: compare without them.
        /// The current loader also replaces the filters of highlights and obfuscations with new ones from the settings,
        /// so their parameters are compared in their own tests.
        /// </summary>
        private static Element WithoutIds(Element element) =>
            (element switch
            {
                HighlightElement highlight => new HighlightElement(Guid.Empty, highlight.Area, highlight.Mode) { Style = highlight.Style },
                ObfuscateElement obfuscate => obfuscate with { Size = 0 },
                ImageElement image => image with { Resource = Guid.Empty },
                CursorElement cursor => cursor with { Resource = Guid.Empty, MaskResource = cursor.MaskResource.HasValue ? Guid.Empty : (Guid?)null },
                FreehandElement freehand => freehand with { Points = null },
                _ => element
            }) with { Id = Guid.Empty };

        private static string MediaTypes(GreenshotDocument document, Element element) => element switch
        {
            ImageElement image => document.Resources[image.Resource].MediaType,
            CursorElement cursor => document.Resources[cursor.Resource].MediaType,
            _ => null
        };

        [Theory]
        [MemberData(nameof(GreenshotFiles))]
        public void ReadGreenshotFile_GivesTheSameDocumentAsTheBinaryFormatter(string relativePath)
        {
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, relativePath));
            var expected = LoadWithBinaryFormatter(stream);

            var actual = LegacyToDocument.ReadGreenshotFile(stream);

            Assert.Equal(expected.Canvas.Width, actual.Canvas.Width);
            Assert.Equal(expected.Canvas.Height, actual.Canvas.Height);
            var expectedElements = expected.Layers[1].Elements;
            var actualElements = actual.Layers[1].Elements;
            Assert.Equal(expectedElements.Count, actualElements.Count);
            for (int i = 0; i < expectedElements.Count; i++)
            {
                Assert.Equal(WithoutIds(expectedElements[i]), WithoutIds(actualElements[i]));
                Assert.Equal(MediaTypes(expected, expectedElements[i]), MediaTypes(actual, actualElements[i]));
                if (expectedElements[i] is FreehandElement expectedFreehand)
                {
                    Assert.Equal(expectedFreehand.Points, ((FreehandElement)actualElements[i]).Points);
                }
            }
        }

        [Theory]
        [MemberData(nameof(NotReadableByTheBinaryFormatterFiles))]
        public void ReadGreenshotFile_ReadsTheImagesTheBinaryFormatterLoses(string relativePath)
        {
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, relativePath));

            var document = LegacyToDocument.ReadGreenshotFile(stream);

            var images = document.Layers[1].Elements.OfType<ImageElement>().ToList();
            Assert.NotEmpty(images);
            Assert.All(images, image => Assert.NotEmpty(document.Resources[image.Resource].Data));
        }

        public static IEnumerable<object[]> NotReadableByTheBinaryFormatterFiles() => NotReadableByTheBinaryFormatter.Select(file => new object[] { file });

        [Fact]
        public void ReadGreenshotFile_KeepsTheFilterParameters()
        {
            var directory = Path.Combine(TestDataDirectory, "Greenshotfile", "File_Version_1.03");
            using var blurStream = File.OpenRead(Path.Combine(directory, "ObfuscateContainer_BlurFilter_lt_130_70_wh_180_70.greenshot"));
            using var highlightStream = File.OpenRead(Path.Combine(directory, "HighlightContainer_TextFilter_lt_310_70_wh_195_60.greenshot"));

            var blur = Assert.IsType<ObfuscateElement>(Assert.Single(LegacyToDocument.ReadGreenshotFile(blurStream).Layers[1].Elements));
            var highlight = Assert.IsType<HighlightElement>(Assert.Single(LegacyToDocument.ReadGreenshotFile(highlightStream).Layers[1].Elements));

            Assert.Equal(new ObfuscateElement(blur.Id, new RectD(130, 70, 180, 70), ObfuscateMode.Blur, 12) { Style = blur.Style }, blur);
            Assert.Equal(new Rgba(255, 0, 0, 255), highlight.Color);
        }

        [Fact]
        public void ReadGreenshotFile_PutsThePngOnTheCaptureLayer()
        {
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, "Greenshotfile", "File_Version_1.03", "Surface_with_Image_800x400.greenshot"));

            var document = LegacyToDocument.ReadGreenshotFile(stream);

            Assert.Equal(new Canvas(800, 400), document.Canvas);
            var capture = Assert.IsType<ImageElement>(Assert.Single(document.Layers[0].Elements));
            Assert.Equal(new RectD(0, 0, 800, 400), capture.Area);
            Assert.Equal("image/png", document.Resources[capture.Resource].MediaType);
            Assert.Empty(document.Layers[1].Elements);
        }

        [Fact]
        public void ReadTemplate_ReadsTheElements()
        {
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, "GreenshotTemplate", "File_Version_1.03", "Surface_with_14_different_DrawableContainer.gst"));
            var resources = new Dictionary<Guid, Resource>();

            var elements = LegacyToDocument.ReadTemplate(stream, resources);

            Assert.Equal(14, elements.Count);
            Assert.All(elements, element => Assert.IsNotType<UnknownElement>(element));
            Assert.Equal(4, resources.Count);
        }

        [Fact]
        public void ReadGreenshotFile_NotAGreenshotFile_Throws()
        {
            using var stream = new MemoryStream(new byte[100]);

            Assert.Throws<LegacyFormatException>(() => LegacyToDocument.ReadGreenshotFile(stream));
        }
    }
}
