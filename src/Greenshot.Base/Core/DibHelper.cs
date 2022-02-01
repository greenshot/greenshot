using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Greenshot.Base.UnmanagedHelpers;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Though Greenshot implements the specs for the DIB image format,
    /// it seems to cause a lot of issues when using the clipboard.
    /// There is some research done about the DIB on the clipboard, this code was taking from:
    /// <a href="https://stackoverflow.com/questions/44177115/copying-from-and-to-clipboard-loses-image-transparency">here</a>
    /// </summary>
    internal static class DibHelper
    {
        /// <summary>
        /// Converts the image to Device Independent Bitmap format of type BITFIELDS.
        /// This is (wrongly) accepted by many applications as containing transparency,
        /// so I'm abusing it for that.
        /// </summary>
        /// <param name="image">Image to convert to DIB</param>
        /// <returns>The image converted to DIB, in bytes.</returns>
        public static byte[] ConvertToDib(Image image)
        {
            byte[] bm32bData;
            int width = image.Width;
            int height = image.Height;
            // Ensure image is 32bppARGB by painting it on a new 32bppARGB image.
            using (var bm32b = ImageHelper.CreateEmptyLike(image, Color.Transparent, PixelFormat.Format32bppArgb))
            {
                using (var graphics = Graphics.FromImage(bm32b))
                {
                    graphics.DrawImage(image, new Rectangle(0, 0, bm32b.Width, bm32b.Height));
                }
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                bm32bData = GetImageData(bm32b, out var stride);
            }
            // BITMAPINFOHEADER struct for DIB.
            uint hdrSize = 0x28;
            var fullImage = new byte[hdrSize + 12 + bm32bData.Length];
            var bitmapInfoHeader = MemoryMarshal.Cast<byte, BITMAPINFOHEADER>(fullImage.AsSpan());

            bitmapInfoHeader[0].biSize = hdrSize;
            bitmapInfoHeader[0].biWidth = width;
            bitmapInfoHeader[0].biHeight = height;
            bitmapInfoHeader[0].biPlanes = 1;
            bitmapInfoHeader[0].biBitCount = 32;
            bitmapInfoHeader[0].biCompression = BI_COMPRESSION.BI_BITFIELDS;
            bitmapInfoHeader[0].biSizeImage = (uint)bm32bData.Length;
            bitmapInfoHeader[0].biXPelsPerMeter = (int)(image.HorizontalResolution * 39.3701);
            bitmapInfoHeader[0].biYPelsPerMeter = (int)(image.VerticalResolution * 39.3701);


            // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
            bitmapInfoHeader[0].bV5RedMask = 0x00FF0000;
            bitmapInfoHeader[0].bV5GreenMask = 0x0000FF00;
            bitmapInfoHeader[0].bV5BlueMask = 0x000000FF;

            // These are all 0. Since .net clears new arrays, don't bother writing them.
            //Int32 biClrUsed = 0;
            //Int32 biClrImportant = 0;

            Array.Copy(bm32bData, 0, fullImage, hdrSize + 12, bm32bData.Length);
            return fullImage;
        }

        /// <summary>
        /// Gets the raw bytes from an image.
        /// </summary>
        /// <param name="sourceImage">The image to get the bytes from.</param>
        /// <param name="stride">Stride of the retrieved image data.</param>
        /// <returns>The raw bytes of the image</returns>
        public static byte[] GetImageData(Bitmap sourceImage, out int stride)
        {
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            stride = sourceData.Stride;
            byte[] data = new byte[stride * sourceImage.Height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            sourceImage.UnlockBits(sourceData);
            return data;
        }
    }
}
