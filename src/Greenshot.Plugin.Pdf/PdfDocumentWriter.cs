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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Plugin.Pdf.Configuration;

namespace Greenshot.Plugin.Pdf;

/// <summary>
/// Creates a PDF document containing capture images and their metadata.
/// </summary>
internal static class PdfDocumentWriter
{
    private const double PdfPointsPerInch = 72.0;
    private const double MillimetersPerInch = 25.4;

    /// <summary>
    /// Writes the capture images to a PDF document using the supplied page layout settings.
    /// </summary>
    /// <param name="bitmaps">The images to place in the PDF, one image per page.</param>
    /// <param name="destination">The writable stream that receives the PDF document.</param>
    /// <param name="configuration">Page size, margins, and scaling settings for the document.</param>
    /// <param name="captureDetails">Optional capture metadata to include in the PDF document information.</param>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The bitmap list is empty or contains a null entry, or the destination stream is not writable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The page layout contains invalid dimensions or margins.</exception>
    public static void Write(IReadOnlyList<Bitmap> bitmaps, Stream destination, IPdfConfiguration configuration, ICaptureDetails captureDetails)
            {
        if (bitmaps == null) throw new ArgumentNullException(nameof(bitmaps));
        if (bitmaps.Count == 0) throw new ArgumentException("At least one bitmap is required.", nameof(bitmaps));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException("The destination stream is not writable.", nameof(destination));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        foreach (Bitmap bitmap in bitmaps)
        {
            if (bitmap == null) throw new ArgumentException("The bitmap list cannot contain null entries.", nameof(bitmaps));
        }

        string creator = "Created with Greenshot v" + EnvironmentInfo.GetGreenshotVersion(true);

        using (var pdf = new MemoryStream())
        {
            WriteAscii(pdf, "%PDF-1.4\n");
            WriteBytes(pdf, new byte[] { 37, 0xE2, 0xE3, 0xCF, 0xD3, 10 });

            int infoObjectNumber = checked(3 + bitmaps.Count * 3);
            long[] offsets = new long[infoObjectNumber + 1];
            offsets[1] = pdf.Position;
            WriteAscii(pdf, "1 0 obj\n<</Type/Catalog/Pages 2 0 R>>\nendobj\n");

            offsets[2] = pdf.Position;
            var pageReferences = new StringBuilder();
            for (int i = 0; i < bitmaps.Count; i++)
            {
                pageReferences.Append(3 + i * 3).Append(" 0 R ");
            }

            WriteAscii(pdf, $"2 0 obj\n<</Type/Pages/Kids[{pageReferences}]/Count {bitmaps.Count}>>\nendobj\n");

            for (int i = 0; i < bitmaps.Count; i++)
            {
                Bitmap bitmap = bitmaps[i];
                double dpiX = GetDpi(bitmap.HorizontalResolution);
                double dpiY = GetDpi(bitmap.VerticalResolution);
                double imageWidthPt = bitmap.Width * PdfPointsPerInch / dpiX;
                double imageHeightPt = bitmap.Height * PdfPointsPerInch / dpiY;
                PdfLayout layout = CalculateLayout(configuration, imageWidthPt, imageHeightPt);
                byte[] imageBytes = EncodeLosslessRgb(bitmap);
                int pageObjectNumber = 3 + i * 3;
                int imageObjectNumber = pageObjectNumber + 1;
                int contentObjectNumber = pageObjectNumber + 2;

                offsets[pageObjectNumber] = pdf.Position;
                WriteAscii(pdf, $"{pageObjectNumber} 0 obj\n<</Type/Page/Parent 2 0 R/MediaBox[0 0 {PdfPageSizes.FormatNumber(layout.PageWidthPt)} {PdfPageSizes.FormatNumber(layout.PageHeightPt)}]/Resources<</XObject<</Img1 {imageObjectNumber} 0 R>>>>/Contents {contentObjectNumber} 0 R>>\nendobj\n");

                offsets[imageObjectNumber] = pdf.Position;
                WriteAscii(pdf, $"{imageObjectNumber} 0 obj\n<</Type/XObject/Subtype/Image/Width {bitmap.Width}/Height {bitmap.Height}/ColorSpace/DeviceRGB/BitsPerComponent 8/Filter/FlateDecode/Length {imageBytes.Length}>>\nstream\n");
                WriteBytes(pdf, imageBytes);
                WriteAscii(pdf, "\nendstream\nendobj\n");

                string content = $"q {PdfPageSizes.FormatNumber(layout.ImageWidthPt)} 0 0 {PdfPageSizes.FormatNumber(layout.ImageHeightPt)} {PdfPageSizes.FormatNumber(layout.LeftPt)} {PdfPageSizes.FormatNumber(layout.BottomPt)} cm /Img1 Do Q\n";
                byte[] contentBytes = Encoding.ASCII.GetBytes(content);
                offsets[contentObjectNumber] = pdf.Position;
                WriteAscii(pdf, $"{contentObjectNumber} 0 obj\n<</Length {contentBytes.Length}>>\nstream\n");
                WriteBytes(pdf, contentBytes);
                WriteAscii(pdf, "endstream\nendobj\n");
            }

            string infoDictionary = CreateInfoDictionary(captureDetails, creator);
            offsets[infoObjectNumber] = pdf.Position;
            WriteAscii(pdf, $"{infoObjectNumber} 0 obj\n{infoDictionary}\nendobj\n");

            long xrefOffset = pdf.Position;
            WriteAscii(pdf, $"xref\n0 {offsets.Length}\n0000000000 65535 f \n");
            for (int i = 1; i < offsets.Length; i++)
            {
                if (offsets[i] > 9999999999L)
                {
                    throw new InvalidOperationException("The generated PDF exceeds the supported cross-reference offset range.");
                }

                WriteAscii(pdf, offsets[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
            }

            WriteAscii(pdf, $"trailer\n<</Size {offsets.Length}/Root 1 0 R/Info {infoObjectNumber} 0 R>>\nstartxref\n{xrefOffset}\n%%EOF\n");
            pdf.Position = 0;
            pdf.CopyTo(destination);
        }
    }

    /// <summary>
    /// Calculates the page dimensions and centered image placement from the PDF configuration.
    /// </summary>
    /// <param name="configuration">The page size, margins, and scaling settings.</param>
    /// <param name="imageWidthPt">The unscaled image width in PDF points.</param>
    /// <param name="imageHeightPt">The unscaled image height in PDF points.</param>
    /// <returns>The page dimensions and image placement, in PDF points.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A margin, page dimension, or image dimension is invalid.</exception>
    private static PdfLayout CalculateLayout(Greenshot.Plugin.Pdf.Configuration.IPdfConfiguration configuration, double imageWidthPt, double imageHeightPt)
    {
        if (!IsValidMargin(configuration.MarginLeftMm)
            || !IsValidMargin(configuration.MarginRightMm)
            || !IsValidMargin(configuration.MarginTopMm)
            || !IsValidMargin(configuration.MarginBottomMm))
        {
            throw new ArgumentOutOfRangeException(nameof(configuration), "Page margins must be finite and non-negative.");
        }

        double marginLeftPt = MillimetersToPoints(configuration.MarginLeftMm);
        double marginRightPt = MillimetersToPoints(configuration.MarginRightMm);
        double marginTopPt = MillimetersToPoints(configuration.MarginTopMm);
        double marginBottomPt = MillimetersToPoints(configuration.MarginBottomMm);

        double pageWidthPt;
        double pageHeightPt;
        if (string.Equals(configuration.PageSize, PdfPageSizes.Image, StringComparison.OrdinalIgnoreCase))
        {
            pageWidthPt = imageWidthPt + marginLeftPt + marginRightPt;
            pageHeightPt = imageHeightPt + marginTopPt + marginBottomPt;
        }
        else
        {
            double widthMm = configuration.PageWidthMm;
            double heightMm = configuration.PageHeightMm;
            if (PdfPageSizes.IsPreset(configuration.PageSize))
            {
                PdfPageSize preset = PdfPageSizes.ForPreset(configuration.PageSize);
                widthMm = preset.WidthMm;
                heightMm = preset.HeightMm;
            }

            if (!IsPositiveFinite(widthMm) || !IsPositiveFinite(heightMm))
            {
                throw new ArgumentOutOfRangeException(nameof(configuration), "Page dimensions must be finite and greater than zero.");
            }

            pageWidthPt = MillimetersToPoints(widthMm);
            pageHeightPt = MillimetersToPoints(heightMm);
        }

        double availableWidthPt = pageWidthPt - marginLeftPt - marginRightPt;
        double availableHeightPt = pageHeightPt - marginTopPt - marginBottomPt;
        if (!IsPositiveFinite(pageWidthPt) || !IsPositiveFinite(pageHeightPt)
            || !IsPositiveFinite(availableWidthPt) || !IsPositiveFinite(availableHeightPt))
        {
            throw new ArgumentOutOfRangeException(nameof(configuration), "Page dimensions and printable area must be greater than zero.");
        }

        if (!IsPositiveFinite(imageWidthPt) || !IsPositiveFinite(imageHeightPt))
        {
            throw new ArgumentOutOfRangeException(nameof(configuration), "Image dimensions must be greater than zero.");
        }

        double scale = Math.Min(availableWidthPt / imageWidthPt, availableHeightPt / imageHeightPt);
        if (string.Equals(configuration.PageSize, PdfPageSizes.Image, StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuration.ScalingMode, PdfScalingModes.OnlyShrinkToFit, StringComparison.OrdinalIgnoreCase))
        {
            scale = Math.Min(1.0, scale);
        }

        imageWidthPt *= scale;
        imageHeightPt *= scale;
        double leftPt = marginLeftPt + (availableWidthPt - imageWidthPt) / 2.0;
        double bottomPt = pageHeightPt - marginTopPt - (availableHeightPt - imageHeightPt) / 2.0 - imageHeightPt;

        return new PdfLayout(pageWidthPt, pageHeightPt, imageWidthPt, imageHeightPt, leftPt, bottomPt);
    }

    /// <summary>
    /// Converts the source image to RGB bytes and compresses them for embedding in a PDF image stream.
    /// </summary>
    /// <param name="source">The bitmap to encode.</param>
    /// <returns>The compressed RGB image data, including its zlib header and checksum.</returns>
    private static byte[] EncodeLosslessRgb(Bitmap source)
    {
        using (var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb))
        {
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
            }

            var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int rowLength = checked(bitmap.Width * 3);
                var sourceRow = new byte[rowLength];
                var rgbRow = new byte[rowLength];
                uint adlerA = 1;
                uint adlerB = 0;

                using (var memory = new MemoryStream())
                {
                    WriteBytes(memory, new byte[] { 0x78, 0x9C });
                    using (var deflate = new DeflateStream(memory, CompressionLevel.Optimal, true))
                    {
                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            IntPtr row = new IntPtr(bitmapData.Scan0.ToInt64() + (long)y * bitmapData.Stride);
                            Marshal.Copy(row, sourceRow, 0, rowLength);
                            for (int x = 0; x < bitmap.Width; x++)
                            {
                                int pixelOffset = x * 3;
                                rgbRow[pixelOffset] = sourceRow[pixelOffset + 2];
                                rgbRow[pixelOffset + 1] = sourceRow[pixelOffset + 1];
                                rgbRow[pixelOffset + 2] = sourceRow[pixelOffset];
                            }

                            for (int i = 0; i < rgbRow.Length; i++)
                            {
                                adlerA = (adlerA + rgbRow[i]) % 65521;
                                adlerB = (adlerB + adlerA) % 65521;
                            }

                            deflate.Write(rgbRow, 0, rgbRow.Length);
                        }
                    }

                    uint adler32 = (adlerB << 16) | adlerA;
                    WriteBytes(memory, new[]
                    {
                        (byte)((adler32 >> 24) & 0xFF),
                        (byte)((adler32 >> 16) & 0xFF),
                        (byte)((adler32 >> 8) & 0xFF),
                        (byte)(adler32 & 0xFF)
                    });
                    return memory.ToArray();
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
    }

    /// <summary>
    /// Creates the PDF document information dictionary from capture details and creator information.
    /// </summary>
    /// <param name="captureDetails">Optional metadata for the capture.</param>
    /// <param name="creator">The document creator value.</param>
    /// <returns>A serialized PDF information dictionary.</returns>
    private static string CreateInfoDictionary(ICaptureDetails captureDetails, string creator)
    {
        var entries = new List<string>();
        if (!string.IsNullOrWhiteSpace(captureDetails?.Title))
        {
            entries.Add("/Title " + EncodePdfText(captureDetails.Title));
        }

        string filename = captureDetails?.Filename;
        if (!string.IsNullOrWhiteSpace(filename))
        {
            entries.Add("/Filename " + EncodePdfText(filename));
        }

        if (captureDetails != null && captureDetails.DateTime != default)
        {
            DateTime captureTime = captureDetails.DateTime;
            string timeZone = captureTime.Kind == DateTimeKind.Utc
                ? "Z"
                : FormatPdfTimeZone(new DateTimeOffset(captureTime).Offset);
            entries.Add("/CreationDate (D:" + captureTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + timeZone + ")");
        }

        entries.Add("/Creator " + EncodePdfText(creator));
        return "<<" + string.Join(" ", entries) + ">>";
    }

    /// <summary>
    /// Formats a time-zone offset using the PDF date-string representation.
    /// </summary>
    /// <param name="offset">The offset from UTC.</param>
    /// <returns>The offset formatted as a sign, hours, and minutes.</returns>
    private static string FormatPdfTimeZone(TimeSpan offset)
    {
        string sign = offset < TimeSpan.Zero ? "-" : "+";
        TimeSpan absoluteOffset = offset.Duration();
        return sign + absoluteOffset.Hours.ToString("D2", CultureInfo.InvariantCulture)
            + "'" + absoluteOffset.Minutes.ToString("D2", CultureInfo.InvariantCulture) + "'";
    }

    /// <summary>
    /// Encodes text as a hexadecimal UTF-16BE PDF text string with a byte-order mark.
    /// </summary>
    /// <param name="value">The text to encode.</param>
    /// <returns>The PDF hexadecimal text-string representation.</returns>
    private static string EncodePdfText(string value)
    {
        byte[] bytes = Encoding.BigEndianUnicode.GetBytes(value);
        var builder = new StringBuilder(bytes.Length * 2 + 8);
        builder.Append("<FEFF");
        foreach (byte valueByte in bytes)
        {
            builder.Append(valueByte.ToString("X2", CultureInfo.InvariantCulture));
        }

        builder.Append('>');
        return builder.ToString();
    }

    /// <summary>
    /// Returns a usable DPI value, substituting 96 DPI when the input is invalid or non-positive.
    /// </summary>
    /// <param name="dpi">The DPI value reported by the bitmap.</param>
    /// <returns>A positive, finite DPI value.</returns>
    private static double GetDpi(float dpi)
    {
        return dpi > 0 && !float.IsNaN(dpi) && !float.IsInfinity(dpi) ? dpi : 96.0;
    }

    /// <summary>
    /// Determines whether a value is finite and greater than zero.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><see langword="true"/> if the value is finite and positive; otherwise, <see langword="false"/>.</returns>
    private static bool IsPositiveFinite(double value)
    {
        return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>
    /// Determines whether a margin is finite and non-negative.
    /// </summary>
    /// <param name="value">The margin value in millimeters.</param>
    /// <returns><see langword="true"/> if the value is finite and non-negative; otherwise, <see langword="false"/>.</returns>
    private static bool IsValidMargin(double value)
    {
        return value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>
    /// Converts millimeters to PDF points.
    /// </summary>
    /// <param name="millimeters">The value in millimeters.</param>
    /// <returns>The equivalent value in PDF points.</returns>
    private static double MillimetersToPoints(double millimeters)
    {
        return millimeters * PdfPointsPerInch / MillimetersPerInch;
    }

    /// <summary>
    /// Writes ASCII-encoded text to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The text to encode and write.</param>
    private static void WriteAscii(Stream stream, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        WriteBytes(stream, bytes);
    }

    /// <summary>
    /// Writes a byte array to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="bytes">The bytes to write.</param>
    private static void WriteBytes(Stream stream, byte[] bytes)
    {
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Contains the PDF page dimensions and the image position and size, all in PDF points.
    /// </summary>
    private readonly struct PdfLayout
    {
        /// <summary>
        /// Initializes a layout with page dimensions and image placement.
        /// </summary>
        /// <param name="pageWidthPt">The page width in PDF points.</param>
        /// <param name="pageHeightPt">The page height in PDF points.</param>
        /// <param name="imageWidthPt">The image width in PDF points.</param>
        /// <param name="imageHeightPt">The image height in PDF points.</param>
        /// <param name="leftPt">The image's left position in PDF points.</param>
        /// <param name="bottomPt">The image's bottom position in PDF points.</param>
        public PdfLayout(double pageWidthPt, double pageHeightPt, double imageWidthPt, double imageHeightPt, double leftPt, double bottomPt)
        {
            PageWidthPt = pageWidthPt;
            PageHeightPt = pageHeightPt;
            ImageWidthPt = imageWidthPt;
            ImageHeightPt = imageHeightPt;
            LeftPt = leftPt;
            BottomPt = bottomPt;
        }

        public double PageWidthPt { get; }

        public double PageHeightPt { get; }

        public double ImageWidthPt { get; }

        public double ImageHeightPt { get; }

        public double LeftPt { get; }

        public double BottomPt { get; }
    }
}
