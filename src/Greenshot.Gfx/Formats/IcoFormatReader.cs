using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Dapplo.Log;

namespace Greenshot.Gfx.Formats
{
    /// <summary>
    /// This implements a IImageFormatReader which reads .ico files
    /// </summary>
    public class IcoFormatReader : IImageFormatReader
    {
        private static readonly LogSource Log = new LogSource();
        private readonly GenericGdiFormatReader _genericGdiFormatReader;

        public IcoFormatReader(GenericGdiFormatReader genericGdiFormatReader)
        {
            _genericGdiFormatReader = genericGdiFormatReader;
        }
        /// <inheritdoc/>
        public IEnumerable<string> SupportedFormats { get; } = new []{ "ico" };

        /// <inheritdoc/>
        public IBitmapWithNativeSupport Read(Stream stream, string extension = null)
        {
            // Icon logic, try to get the Vista icon, else the biggest possible
            try
            {
                using var tmpBitmap = ExtractVistaIcon(stream);
                if (tmpBitmap != null)
                {
                    return tmpBitmap.CloneBitmap(PixelFormat.Format32bppArgb);
                }
            }
            catch (Exception vistaIconException)
            {
                Log.Warn().WriteLine(vistaIconException, "Can't read icon");
            }
            try
            {
                // No vista icon, try normal icon
                stream.Position = 0;
                // We create a copy of the bitmap, so everything else can be disposed
                using var tmpIcon = new Icon(stream, new Size(1024, 1024));
                using var tmpImage = tmpIcon.ToBitmap();
                return tmpImage.CloneBitmap(PixelFormat.Format32bppArgb);
            }
            catch (Exception iconException)
            {
                Log.Warn().WriteLine(iconException, "Can't read icon");
            }

            stream.Position = 0;
            return _genericGdiFormatReader.Read(stream, extension);
        }


        /// <summary>
        ///     Based on: http://www.codeproject.com/KB/cs/IconExtractor.aspx
        ///     And a hint from: http://www.codeproject.com/KB/cs/IconLib.aspx
        /// </summary>
        /// <param name="iconStream">Stream with the icon information</param>
        /// <returns>Bitmap with the Vista Icon (256x256)</returns>
        private static IBitmapWithNativeSupport ExtractVistaIcon(Stream iconStream)
        {
            const int sizeIconDir = 6;
            const int sizeIconDirEntry = 16;
            IBitmapWithNativeSupport bmpPngExtracted = null;
            try
            {
                var srcBuf = new byte[iconStream.Length];
                iconStream.Read(srcBuf, 0, (int)iconStream.Length);
                int iCount = BitConverter.ToInt16(srcBuf, 4);
                for (var iIndex = 0; iIndex < iCount; iIndex++)
                {
                    int iWidth = srcBuf[sizeIconDir + sizeIconDirEntry * iIndex];
                    int iHeight = srcBuf[sizeIconDir + sizeIconDirEntry * iIndex + 1];
                    if (iWidth != 0 || iHeight != 0)
                    {
                        continue;
                    }
                    var iImageSize = BitConverter.ToInt32(srcBuf, sizeIconDir + sizeIconDirEntry * iIndex + 8);
                    var iImageOffset = BitConverter.ToInt32(srcBuf, sizeIconDir + sizeIconDirEntry * iIndex + 12);
                    using var destStream = new MemoryStream();
                    destStream.Write(srcBuf, iImageOffset, iImageSize);
                    destStream.Seek(0, SeekOrigin.Begin);
                    bmpPngExtracted = BitmapWrapper.FromBitmap(new Bitmap(destStream)); // This is PNG! :)
                    break;
                }
            }
            catch
            {
                return null;
            }
            return bmpPngExtracted;
        }

    }
}
