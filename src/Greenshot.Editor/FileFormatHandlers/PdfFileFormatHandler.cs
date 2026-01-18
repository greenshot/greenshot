using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Editor.FileFormatHandlers
{
    internal class PdfFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {        
        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".pdf" };
        public PdfFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions; //Bug have to use wrong action, already fixed in open PR #638 
        }

        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            throw new NotImplementedException();
        }

        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            try
            {
                // 1. use JPEG because pdf supports it natively
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Jpeg);
                byte[] jpegData = ms.ToArray();

                // 2. dimensions scale
                // current target 210mm (DIN A4). 
                double targetWidthMm = 210.0;
                double aspectRatio = (double)bitmap.Height / bitmap.Width;
                double targetHeightMm = targetWidthMm * aspectRatio;

                double mmToPt = 72.0 / 25.4;
                double pageWidthPt = targetWidthMm * mmToPt;
                double pageHeightPt = targetHeightMm * mmToPt;

                // 3. PDF structure
                using var writer = new StreamWriter(destination, Encoding.ASCII, 1024, leaveOpen: true);
                string F(double value) => value.ToString("F2", CultureInfo.InvariantCulture);

                // Header
                writer.Write("%PDF-1.4\n");
                writer.Flush();

                // Objects
                long obj1Offset = destination.Position;
                writer.Write("1 0 obj\n<</Type/Catalog/Pages 2 0 R>>\nendobj\n");
                writer.Flush();

                long obj2Offset = destination.Position;
                writer.Write("2 0 obj\n<</Type/Pages/Kids[3 0 R]/Count 1>>\nendobj\n");
                writer.Flush();

                long obj3Offset = destination.Position;
                writer.Write($"3 0 obj\n<</Type/Page/Parent 2 0 R/MediaBox[0 0 {F(pageWidthPt)} {F(pageHeightPt)}]/Resources<</XObject<</Img1 4 0 R>>>>/Contents 5 0 R>>\n");
                writer.Write("endobj\n");
                writer.Flush();

                long obj4Offset = destination.Position;
                writer.Write($"4 0 obj\n<</Type/XObject/Subtype/Image/Width {bitmap.Width}/Height {bitmap.Height}/ColorSpace/DeviceRGB/BitsPerComponent 8/Filter/DCTDecode/Length {jpegData.Length}>>\nstream\n");
                writer.Flush();
                destination.Write(jpegData, 0, jpegData.Length);
                writer.Write("\nendstream\nendobj\n");
                writer.Flush();

                long obj5Offset = destination.Position;
                string content = $"q {F(pageWidthPt)} 0 0 {F(pageHeightPt)} 0 0 cm /Img1 Do Q";
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
            catch
            {
                return false;
            }
        }
    }
}
