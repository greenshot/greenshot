/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Linq;
using System.Text;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.FileFormatHandlers;
using Greenshot.Plugin.Pdf.Configuration;

namespace Greenshot.Plugin.Pdf
{
    internal class PdfFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PdfFileFormatHandler));
        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".pdf" };
        private PdfExportSettings _settings;

        public PdfFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToFile] = _ourExtensions;
            _settings = IniConfig.GetIniSection<PdfExportSettings>();
        }

        /// <summary>
        /// Set the export settings for PDF generation
        /// </summary>
        public void SetSettings(PdfExportSettings settings)
        {
            _settings = settings ?? new PdfExportSettings();
        }

        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            throw new NotImplementedException();
        }

        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            return TrySaveToPdf(bitmap, destination);
        }

        /// <summary>
        /// Saves bitmap to PDF with configured settings
        /// </summary>
        private bool TrySaveToPdf(Bitmap bitmap, Stream destination)
        {
            try
            {
                // 1. Extract DPI from bitmap
                double bitmapDpiX = bitmap.HorizontalResolution;
                double bitmapDpiY = bitmap.VerticalResolution;

                // Fallback to 96 DPI if not set
                if (bitmapDpiX <= 0) bitmapDpiX = 96.0;
                if (bitmapDpiY <= 0) bitmapDpiY = 96.0;

                LOG.Debug($"Bitmap DPI: {bitmapDpiX:F2}x{bitmapDpiY:F2}");

                const double pdfPointsPerInch = 72.0;
                const double mmToInch = 1.0 / 25.4;

                // DPI factor: convert from bitmap DPI to PDF points (72 DPI)
                double pointsPerPixelX = pdfPointsPerInch / bitmapDpiX;
                double pointsPerPixelY = pdfPointsPerInch / bitmapDpiY;

                // 2. image in points
                double imageWidthPt = bitmap.Width * pointsPerPixelX;
                double imageHeightPt = bitmap.Height * pointsPerPixelY;

                double mmToPt = pdfPointsPerInch * mmToInch;
                double documentWidthPt;
                double documentHeightPt;

                if (_settings.UseFixedDocument)
                {
                    documentWidthPt = _settings.DocumentWidth * mmToPt;
                    documentHeightPt = _settings.DocumentHeight * mmToPt;

                    double marginLPt = _settings.MarginLeft * mmToPt;
                    double marginRPt = _settings.MarginRight * mmToPt;
                    double marginTPt = _settings.MarginTop * mmToPt;
                    double marginBPt = _settings.MarginBottom * mmToPt;

                    double availWPt = documentWidthPt - marginLPt - marginRPt;
                    double availHPt = documentHeightPt - marginTPt - marginBPt;

                    if (imageWidthPt > availWPt || imageHeightPt > availHPt)
                    {
                        double scale = Math.Min(availWPt / imageWidthPt, availHPt / imageHeightPt);
                        imageWidthPt *= scale;
                        imageHeightPt *= scale;
                    }
                }
                else
                {
                    // Image defines document size
                    documentWidthPt = imageWidthPt + (_settings.MarginLeft + _settings.MarginRight) * mmToPt;
                    documentHeightPt = imageHeightPt + (_settings.MarginTop + _settings.MarginBottom) * mmToPt;
                }

                // 3. center
                double totalMarginWPt = (_settings.MarginLeft + _settings.MarginRight) * mmToPt;
                double totalMarginHPt = (_settings.MarginTop + _settings.MarginBottom) * mmToPt;

                double centerOffsetX = Math.Max(0, (documentWidthPt - totalMarginWPt - imageWidthPt) / 2.0);
                double centerOffsetY = Math.Max(0, (documentHeightPt - totalMarginHPt - imageHeightPt) / 2.0);

                double contentLeftPt = (_settings.MarginLeft * mmToPt) + centerOffsetX;
                double contentTopPt = (_settings.MarginTop * mmToPt) + centerOffsetY;

                // 
                double contentBottomPt = documentHeightPt - contentTopPt - imageHeightPt;

                LOG.Debug($"Image size: {imageWidthPt:F2}x{imageHeightPt:F2}pt, Document size: {documentWidthPt:F2}x{documentHeightPt:F2}pt");

                // 4. extract JPEG data
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Jpeg);
                byte[] jpegData = ms.ToArray();

                // 5. PDF 
                using var writer = new StreamWriter(destination, Encoding.ASCII, 1024, leaveOpen: true);
                string F(double value) => value.ToString("F2", CultureInfo.InvariantCulture);

                writer.Write("%PDF-1.4\n");
                writer.Flush();

                // Obj 1: Catalog
                long obj1Offset = destination.Position;
                writer.Write("1 0 obj\n<</Type/Catalog/Pages 2 0 R>>\nendobj\n");
                writer.Flush();

                // Obj 2: Pages tree
                long obj2Offset = destination.Position;
                writer.Write("2 0 obj\n<</Type/Pages/Kids[3 0 R]/Count 1>>\nendobj\n");
                writer.Flush();

                // Obj 3: Page
                long obj3Offset = destination.Position;
                writer.Write($"3 0 obj\n<</Type/Page/Parent 2 0 R/MediaBox[0 0 {F(documentWidthPt)} {F(documentHeightPt)}]/Resources<</XObject<</Img1 4 0 R>>>>/Contents 5 0 R>>\nendobj\n");
                writer.Flush();

                // Obj 4: Image
                long obj4Offset = destination.Position;
                writer.Write($"4 0 obj\n<</Type/XObject/Subtype/Image/Width {bitmap.Width}/Height {bitmap.Height}/ColorSpace/DeviceRGB/BitsPerComponent 8/Filter/DCTDecode/Length {jpegData.Length}>>\nstream\n");
                writer.Flush();
                destination.Write(jpegData, 0, jpegData.Length);
                writer.Write("\nendstream\nendobj\n");
                writer.Flush();

                // Obj 5: Content Stream
                long obj5Offset = destination.Position;
                string content = $"q {F(imageWidthPt)} 0 0 {F(imageHeightPt)} {F(contentLeftPt)} {F(contentBottomPt)} cm /Img1 Do Q";
                writer.Write($"5 0 obj\n<</Length {content.Length}>>\nstream\n{content}\nendstream\nendobj\n");
                writer.Flush();

                // Cross-Reference Table
                long xrefOffset = destination.Position;
                writer.Write("xref\n0 6\n0000000000 65535 f \n");
                writer.Write($"{obj1Offset:0000000000} 00000 n \n");
                writer.Write($"{obj2Offset:0000000000} 00000 n \n");
                writer.Write($"{obj3Offset:0000000000} 00000 n \n");
                writer.Write($"{obj4Offset:0000000000} 00000 n \n");
                writer.Write($"{obj5Offset:0000000000} 00000 n \n");

                // Trailer
                writer.Write($"trailer\n<</Size 6/Root 1 0 R>>\nstartxref\n{xrefOffset}\n%%EOF");
                writer.Flush();

                return true;
            }
            catch (Exception ex)
            {
                LOG.Error("Error saving PDF: ", ex);
                return false;
            }
        }
    }
}
