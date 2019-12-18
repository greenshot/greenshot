using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Dapplo.Log;

namespace Greenshot.Gfx.Formats
{
    /// <summary>
    /// This implements a IImageFormatReader with the help of Gdi
    /// </summary>
    public class GenericGdiFormatReader : IImageFormatReader
    {
        private static readonly LogSource Log = new LogSource();

        /// <inheritdoc/>
        public IEnumerable<string> SupportedFormats { get; } = new []{ "","gif", "bmp", "jpg", "jpeg", "png", "wmf" };

        /// <inheritdoc/>
        public IBitmapWithNativeSupport Read(Stream stream, string extension = null)
        {
            using var tmpImage = Image.FromStream(stream, true, true);
            if (!(tmpImage is Bitmap bitmap))
            {
                return null;
            }
            Log.Debug().WriteLine("Loaded bitmap with Size {0}x{1} and PixelFormat {2}", bitmap.Width, bitmap.Height, bitmap.PixelFormat);
            return bitmap.CloneBitmap(PixelFormat.Format32bppArgb);
        }
    }
}
