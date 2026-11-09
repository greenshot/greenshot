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
using Dapplo.Windows.Common.Structs;
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

        [Fact]
        public void PrivateDetails_OnlyWhenIncluded()
        {
            var captureDetails = CaptureWithPrivateDetails();
            ExportMetadataField[] fields = [ExportMetadataField.Source, ExportMetadataField.Text, ExportMetadataField.Barcodes];

            var withoutPrivate = ExportImageMetadata.ForExport(captureDetails, fields, null, null);
            Assert.False(withoutPrivate.HasPrivateDetails);

            var withPrivate = ExportImageMetadata.ForExport(captureDetails, fields, null, null, includePrivate: true);
            Assert.Equal("Inbox - Mail", withPrivate.Title);
            Assert.Equal("https://example.org/page", withPrivate.SourceUrl);
            Assert.Equal("Invoice 4711" + Environment.NewLine + "Total 99 EUR", withPrivate.Description);
            Assert.Equal(["https://example.org/qr"], withPrivate.Barcodes);
        }

        [Fact]
        public void PrivateDetails_OnlyTheChosenFields()
        {
            var metadata = ExportImageMetadata.ForExport(CaptureWithPrivateDetails(), [ExportMetadataField.Text], null, null, includePrivate: true);
            Assert.Null(metadata.Title);
            Assert.Null(metadata.SourceUrl);
            Assert.Empty(metadata.Barcodes);
            Assert.NotNull(metadata.Description);
        }

        [Fact]
        public void Xmp_HasThePrivateDetails()
        {
            var metadata = new ExportImageMetadata { Software = "Greenshot test", Title = "Inbox", SourceUrl = "https://example.org", Description = "Some text" };
            metadata.Barcodes.Add("QR content");
            var document = XDocument.Parse(System.Text.Encoding.UTF8.GetString(metadata.ToXmpPacket()));
            XNamespace dc = "http://purl.org/dc/elements/1.1/", greenshot = "https://getgreenshot.org/ns/xmp/1.0/";
            Assert.Equal("Inbox", document.Descendants(dc + "title").Single().Value);
            Assert.Equal("https://example.org", document.Descendants(dc + "source").Single().Value);
            Assert.Equal("Some text", document.Descendants(dc + "description").Single().Value);
            Assert.Equal("QR content", document.Descendants(greenshot + "Barcodes").Single().Value);
        }

        /// <summary>
        /// What Greenshot writes into a PNG comes back when the file is opened: capture time, title and web address
        /// </summary>
        [Fact]
        public void Png_RoundTrip_ReadsTheDetailsBack()
        {
            var metadata = new ExportImageMetadata { Software = "Greenshot test", CaptureTime = Captured, Title = "Inbox - Mail", SourceUrl = "https://example.org/page" };
            var opened = new CaptureDetails { CaptureMode = CaptureMode.File };
            using (var stream = new MemoryStream())
            {
                using var bitmap = new Bitmap(32, 16, PixelFormat.Format24bppRgb);
                PngOptimizer.Save(bitmap, stream, new SurfaceOutputSettings(WellKnownFileFormats.Png), metadata);
                stream.Position = 0;
                ImageMetadataReader.ReadInto(opened, stream);
            }

            Assert.Equal(Captured.LocalDateTime, opened.DateTime);
            Assert.Equal("Inbox - Mail", opened.Title);
            Assert.Equal("Inbox - Mail", opened.MetaData["windowtitle"]);
            Assert.Equal("https://example.org/page", opened.MetaData["url"]);
            // Saved again, the opened image keeps its capture time
            Assert.Equal(Captured, ExportImageMetadata.CaptureTimeOf(opened));
        }

        /// <summary>
        /// A photo from a camera: the EXIF capture time is read, with its offset
        /// </summary>
        [Fact]
        public void Jpeg_ExifCaptureTime_IsRead()
        {
            var opened = new CaptureDetails { CaptureMode = CaptureMode.File };
            using (var stream = new MemoryStream())
            {
                using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(8, 8);
                image.Metadata.ExifProfile = new ExifProfile();
                image.Metadata.ExifProfile.SetValue(ExifTag.DateTimeOriginal, "2026:11:02 16:30:05");
                image.Metadata.ExifProfile.SetValue(ExifTag.OffsetTimeOriginal, "+01:00");
                image.SaveAsJpeg(stream);
                stream.Position = 0;
                ImageMetadataReader.ReadInto(opened, stream);
            }

            Assert.Equal(Captured, ExportImageMetadata.CaptureTimeOf(opened));
            Assert.False(opened.MetaData.ContainsKey("url"));
        }

        [Fact]
        public void Question_DescribesTheDetails_TextOnlyCounted()
        {
            var metadata = new ExportImageMetadata { Title = "Inbox - Mail", SourceUrl = "https://example.org/page", Description = "Invoice 4711\nTotal 99 EUR" };
            metadata.Barcodes.Add("QR content");
            var lines = new Greenshot.Base.Core.Export.PrivateMetadataRequest(metadata).DescribeDetails();
            Assert.Equal(4, lines.Count);
            Assert.Contains(lines, line => line.Contains("Inbox - Mail"));
            Assert.Contains(lines, line => line.Contains("https://example.org/page"));
            Assert.Contains(lines, line => line.Contains("5") && !line.Contains("Invoice"));
            Assert.Contains(lines, line => line.Contains("QR content"));
        }

        /// <summary>
        /// Without a window to ask in (a flow run from the command line), private details are removed
        /// </summary>
        [Fact]
        public async System.Threading.Tasks.Task Question_WithoutWindow_RemovesTheDetails()
        {
            var metadata = new ExportImageMetadata { Title = "Inbox - Mail" };
            Assert.False(await Greenshot.Base.Core.Export.PrivateMetadataRequest.AskAsync(HeadlessUserInteraction.Instance, metadata, TestContext.Current.CancellationToken));
        }

        private static CaptureDetails CaptureWithPrivateDetails()
        {
            var captureDetails = new CaptureDetails { CaptureMode = CaptureMode.Region, Title = "Inbox - Mail" };
            captureDetails.AddMetaData("url", "https://example.org/page");
            captureDetails.Features.Add(new OcrLine("Invoice 4711"));
            captureDetails.Features.Add(new OcrLine("Total 99 EUR"));
            captureDetails.Features.Add(new Barcode("https://example.org/qr"));
            captureDetails.Features.Add(new Barcode("https://example.org/qr"));
            return captureDetails;
        }

        private sealed class OcrLine(string text) : IOcrLineFeature
        {
            public NativeRect Bounds => Box.Bounds;
            public OcrBox Box => OcrBox.FromRect(new NativeRect(0, 0, 10, 10));
            public double? Score => null;
            public string FeatureType => "OcrLine";
            public string Text => text;
            public string ToolTipText => text;
            public List<OcrWordInfo> Words { get; } = new List<OcrWordInfo>();
            public void Offset(int x, int y) { }
        }

        private sealed class Barcode(string text) : IBarcodeFeature
        {
            public NativeRect Bounds => new NativeRect(0, 0, 10, 10);
            public string FeatureType => "Barcode";
            public string Format => "QR_CODE";
            public string RawText => text;
            public string Text => text;
            public string ToolTipText => text;
            public void Offset(int x, int y) { }
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
