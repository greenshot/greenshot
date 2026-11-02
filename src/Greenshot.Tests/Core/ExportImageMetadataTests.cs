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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class ExportImageMetadataTests
    {
        private static readonly DateTimeOffset Captured = new DateTimeOffset(2026, 11, 2, 16, 30, 5, TimeSpan.FromHours(1));

        public ExportImageMetadataTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData(CaptureMode.Region, true)]
        [InlineData(CaptureMode.FullScreen, true)]
        [InlineData(CaptureMode.Window, true)]
        [InlineData(CaptureMode.File, false)]
        [InlineData(CaptureMode.Clipboard, false)]
        [InlineData(CaptureMode.Import, false)]
        [InlineData(CaptureMode.None, false)]
        public void CaptureTime_OnlyForScreenCaptures(CaptureMode captureMode, bool expected)
        {
            var captureDetails = new CaptureDetails { CaptureMode = captureMode };
            var metadata = ExportImageMetadata.ForExport(captureDetails, [ExportMetadataField.CaptureTime], null, null);
            Assert.Equal(expected, metadata.CaptureTime.HasValue);
            if (expected)
            {
                Assert.Equal(new DateTimeOffset(captureDetails.DateTime), metadata.CaptureTime.Value);
            }
        }

        [Fact]
        public void NoFields_OnlyTheProgramName()
        {
            var captureDetails = new CaptureDetails { CaptureMode = CaptureMode.Region };
            var metadata = ExportImageMetadata.ForExport(captureDetails, [], "Author", "Copyright");
            Assert.StartsWith("Greenshot ", metadata.Software);
            Assert.False(metadata.HasDetails);
            Assert.Null(metadata.Author);
            Assert.Null(metadata.Copyright);
        }

        [Fact]
        public void Author_EmptyValuesAreNotWritten()
        {
            var metadata = ExportImageMetadata.ForExport(null, [ExportMetadataField.Author, ExportMetadataField.CaptureTime], "  ", " (c) Robin ");
            Assert.Null(metadata.Author);
            Assert.Equal("(c) Robin", metadata.Copyright);
            // Without capture details (the clipboard) there is no capture time
            Assert.Null(metadata.CaptureTime);
        }

        [Fact]
        public void CaptureTime_ExifFormat()
        {
            var metadata = new ExportImageMetadata { CaptureTime = Captured };
            Assert.Equal("2026:11:02 16:30:05", metadata.ExifCaptureTime);
            Assert.Equal("+01:00", metadata.ExifCaptureTimeOffset);
        }

        [Fact]
        public void Xmp_HasTheDetails()
        {
            var metadata = new ExportImageMetadata { Software = "Greenshot test", CaptureTime = Captured, Author = "Jürgen", Copyright = "(c) Test" };
            var document = XDocument.Parse(System.Text.Encoding.UTF8.GetString(metadata.ToXmpPacket()));
            XNamespace xmp = "http://ns.adobe.com/xap/1.0/", dc = "http://purl.org/dc/elements/1.1/";
            Assert.Equal("Greenshot test", document.Descendants(xmp + "CreatorTool").Single().Value);
            Assert.Equal("2026-11-02T16:30:05+01:00", document.Descendants(xmp + "CreateDate").Single().Value);
            Assert.Equal("Jürgen", document.Descendants(dc + "creator").Single().Value);
            Assert.Equal("(c) Test", document.Descendants(dc + "rights").Single().Value);
            Assert.Equal(2, document.Nodes().OfType<XProcessingInstruction>().Count());
        }

        /// <summary>
        /// Windows shows the details of a PNG from XMP, other programs (e.g. Directory Opus) from EXIF: both are written
        /// </summary>
        [Fact]
        public void Png_WithDetails_HasExifAndXmp()
        {
            var metadata = new ExportImageMetadata { Software = "Greenshot test", CaptureTime = Captured, Author = "Test Author", Copyright = "(c) Test" };
            using var image = SaveAndLoadPng(metadata);
            var exif = image.Metadata.ExifProfile;
            Assert.NotNull(exif);
            Assert.Equal("2026:11:02 16:30:05", exif.GetValue(ExifTag.DateTimeOriginal).Value);
            Assert.Equal("+01:00", exif.GetValue(ExifTag.OffsetTimeOriginal).Value);
            Assert.Equal("Test Author", exif.GetValue(ExifTag.Artist).Value);
            Assert.Equal("(c) Test", exif.GetValue(ExifTag.Copyright).Value);
            Assert.NotNull(image.Metadata.XmpProfile);
        }

        /// <summary>
        /// Without details a PNG stays as small as before: only the Software text chunk
        /// </summary>
        [Fact]
        public void Png_WithoutDetails_HasNoExifOrXmp()
        {
            using var image = SaveAndLoadPng(new ExportImageMetadata { Software = "Greenshot test" });
            Assert.Null(image.Metadata.ExifProfile);
            Assert.Null(image.Metadata.XmpProfile);
            var software = image.Metadata.GetPngMetadata().TextData.Single(text => text.Keyword == "Software");
            Assert.Equal("Greenshot test", software.Value);
        }

        private static SixLabors.ImageSharp.Image SaveAndLoadPng(ExportImageMetadata metadata)
        {
            using var bitmap = new Bitmap(32, 16, PixelFormat.Format24bppRgb);
            using var stream = new MemoryStream();
            PngOptimizer.Save(bitmap, stream, new SurfaceOutputSettings(WellKnownFileFormats.Png), metadata);
            stream.Position = 0;
            return SixLabors.ImageSharp.Image.Load(stream);
        }
    }
}
