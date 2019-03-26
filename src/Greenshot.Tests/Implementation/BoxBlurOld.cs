using System;
using System.Drawing;
using Greenshot.Gfx;
using Greenshot.Gfx.FastBitmap;

namespace Greenshot.Tests.Implementation
{
    /// <summary>
    /// Code to apply a box blur
    /// </summary>
    public static class BoxBlurOld
    {
        /// <summary>
        ///     Apply BoxBlur to the destinationBitmap
        /// </summary>
        /// <param name="destinationBitmap">Bitmap to blur</param>
        /// <param name="range">Must be ODD!</param>
        public static void ApplyOldBoxBlur(this IBitmapWithNativeSupport destinationBitmap, int range)
        {
            // We only need one fastbitmap as we use it as source and target (the reading is done for one line H/V, writing after "parsing" one line H/V)
            using (var fastBitmap = FastBitmapFactory.Create(destinationBitmap))
            {
                fastBitmap.ApplyOldBoxBlur(range);
            }
        }

        /// <summary>
        ///     Apply BoxBlur to the fastBitmap
        /// </summary>
        /// <param name="fastBitmap">IFastBitmap to blur</param>
        /// <param name="range">Must be ODD!</param>
        public static void ApplyOldBoxBlur(this IFastBitmap fastBitmap, int range)
        {
            // Range must be odd!
            if ((range & 1) == 0)
            {
                range++;
            }
            if (range <= 1)
            {
                return;
            }
            // Box blurs are frequently used to approximate a Gaussian blur.
            // By the central limit theorem, if applied 3 times on the same image, a box blur approximates the Gaussian kernel to within about 3%, yielding the same result as a quadratic convolution kernel.
            // This might be true, but the GDI+ BlurEffect doesn't look the same, a 2x blur is more simular and we only make 2x Box-Blur.
            // (Might also be a mistake in our blur, but for now it looks great)
            if (fastBitmap.HasAlphaChannel)
            {
                BoxBlurHorizontalAlpha(fastBitmap, range);
                BoxBlurVerticalAlpha(fastBitmap, range);
                BoxBlurHorizontalAlpha(fastBitmap, range);
                BoxBlurVerticalAlpha(fastBitmap, range);
            }
            else
            {
                BoxBlurHorizontal(fastBitmap, range);
                BoxBlurVertical(fastBitmap, range);
                BoxBlurHorizontal(fastBitmap, range);
                BoxBlurVertical(fastBitmap, range);
            }
        }

        /// <summary>
        ///     BoxBlurHorizontal is a private helper method for the BoxBlur
        /// </summary>
        /// <param name="targetFastBitmap">Target BitmapBuffer</param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurHorizontal(IFastBitmap targetFastBitmap, int range)
        {
            if (targetFastBitmap.HasAlphaChannel)
            {
                throw new NotSupportedException("BoxBlurHorizontal should NOT be called for bitmaps with alpha channel");
            }

            var halfRange = range / 2;
            var newColors = new Color[targetFastBitmap.Width];
            var tmpColor = new byte[3];

            for (var y = targetFastBitmap.Top; y < targetFastBitmap.Bottom; y++)
            {
                var hits = 0;
                var r = 0;
                var g = 0;
                var b = 0;

                for (var x = targetFastBitmap.Left - halfRange; x < targetFastBitmap.Right; x++)
                {
                    var oldPixel = x - halfRange - 1;

                    if (oldPixel >= targetFastBitmap.Left)
                    {
                        targetFastBitmap.GetColorAt(oldPixel, y, tmpColor);
                        r -= tmpColor[FastBitmapBase.ColorIndexR];
                        g -= tmpColor[FastBitmapBase.ColorIndexG];
                        b -= tmpColor[FastBitmapBase.ColorIndexB];
                        hits--;
                    }

                    var newPixel = x + halfRange;

                    if (newPixel < targetFastBitmap.Right)
                    {
                        targetFastBitmap.GetColorAt(newPixel, y, tmpColor);
                        r += tmpColor[FastBitmapBase.ColorIndexR];
                        g += tmpColor[FastBitmapBase.ColorIndexG];
                        b += tmpColor[FastBitmapBase.ColorIndexB];
                        hits++;
                    }

                    if (x >= targetFastBitmap.Left)
                    {
                        newColors[x - targetFastBitmap.Left] = Color.FromArgb(255, (byte)(r / hits), (byte)(g / hits), (byte)(b / hits));
                    }
                }
                for (var x = targetFastBitmap.Left; x < targetFastBitmap.Right; x++)
                {
                    targetFastBitmap.SetColorAt(x, y, ref newColors[x - targetFastBitmap.Left]);
                }
            }
        }

        /// <summary>
        ///     BoxBlurHorizontal is a private helper method for the BoxBlur, only for IFastBitmaps with alpha channel
        /// </summary>
        /// <param name="targetFastBitmap">Target BitmapBuffer</param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurHorizontalAlpha(IFastBitmap targetFastBitmap, int range)
        {
            if (!targetFastBitmap.HasAlphaChannel)
            {
                throw new NotSupportedException("BoxBlurHorizontalAlpha should be called for bitmaps with alpha channel");
            }

            var halfRange = range / 2;
            var newColors = new Color[targetFastBitmap.Width];
            var tmpColor = new byte[4];

            for (var y = targetFastBitmap.Top; y < targetFastBitmap.Bottom; y++)
            {
                var hits = 0;
                var a = 0;
                var r = 0;
                var g = 0;
                var b = 0;

                for (var x = targetFastBitmap.Left - halfRange; x < targetFastBitmap.Right; x++)
                {
                    var oldPixel = x - halfRange - 1;

                    if (oldPixel >= targetFastBitmap.Left)
                    {
                        targetFastBitmap.GetColorAt(oldPixel, y, tmpColor);
                        a -= tmpColor[FastBitmapBase.ColorIndexA];
                        r -= tmpColor[FastBitmapBase.ColorIndexR];
                        g -= tmpColor[FastBitmapBase.ColorIndexG];
                        b -= tmpColor[FastBitmapBase.ColorIndexB];
                        hits--;
                    }

                    var newPixel = x + halfRange;

                    if (newPixel < targetFastBitmap.Right)
                    {
                        targetFastBitmap.GetColorAt(newPixel, y, tmpColor);
                        a += tmpColor[FastBitmapBase.ColorIndexA];
                        r += tmpColor[FastBitmapBase.ColorIndexR];
                        g += tmpColor[FastBitmapBase.ColorIndexG];
                        b += tmpColor[FastBitmapBase.ColorIndexB];
                        hits++;
                    }

                    if (x >= targetFastBitmap.Left)
                    {
                        newColors[x - targetFastBitmap.Left] = Color.FromArgb((byte)(a / hits), (byte)(r / hits), (byte)(g / hits), (byte)(b / hits));
                    }
                }
                for (var x = targetFastBitmap.Left; x < targetFastBitmap.Right; x++)
                {
                    targetFastBitmap.SetColorAt(x, y, ref newColors[x - targetFastBitmap.Left]);
                }
            }
        }

        /// <summary>
        ///     BoxBlurVertical is a private helper method for the BoxBlur
        /// </summary>
        /// <param name="targetFastBitmap">BitmapBuffer which previously was created with BoxBlurHorizontal</param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurVertical(IFastBitmap targetFastBitmap, int range)
        {
            if (targetFastBitmap.HasAlphaChannel)
            {
                throw new NotSupportedException("BoxBlurVertical should NOT be called for bitmaps with alpha channel");
            }

            var halfRange = range / 2;
            var newColors = new Color[targetFastBitmap.Height];
            var tmpColor = new byte[4];

            for (var x = targetFastBitmap.Left; x < targetFastBitmap.Right; x++)
            {
                var hits = 0;
                var r = 0;
                var g = 0;
                var b = 0;

                for (var y = targetFastBitmap.Top - halfRange; y < targetFastBitmap.Bottom; y++)
                {
                    var oldPixel = y - halfRange - 1;

                    if (oldPixel >= targetFastBitmap.Top)
                    {
                        targetFastBitmap.GetColorAt(x, oldPixel, tmpColor);
                        r -= tmpColor[FastBitmapBase.ColorIndexR];
                        g -= tmpColor[FastBitmapBase.ColorIndexG];
                        b -= tmpColor[FastBitmapBase.ColorIndexB];
                        hits--;
                    }

                    var newPixel = y + halfRange;

                    if (newPixel < targetFastBitmap.Bottom)
                    {
                        targetFastBitmap.GetColorAt(x, newPixel, tmpColor);
                        r += tmpColor[FastBitmapBase.ColorIndexR];
                        g += tmpColor[FastBitmapBase.ColorIndexG];
                        b += tmpColor[FastBitmapBase.ColorIndexB];
                        hits++;
                    }

                    if (y >= targetFastBitmap.Top)
                    {
                        newColors[y - targetFastBitmap.Top] = Color.FromArgb(255, (byte)(r / hits), (byte)(g / hits), (byte)(b / hits));
                    }
                }

                for (var y = targetFastBitmap.Top; y < targetFastBitmap.Bottom; y++)
                {
                    targetFastBitmap.SetColorAt(x, y, ref newColors[y - targetFastBitmap.Top]);
                }
            }
        }

        /// <summary>
        ///     BoxBlurVertical is a private helper method for the BoxBlur
        /// </summary>
        /// <param name="targetFastBitmap">BitmapBuffer which previously was created with BoxBlurHorizontal</param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurVerticalAlpha(IFastBitmap targetFastBitmap, int range)
        {
            if (!targetFastBitmap.HasAlphaChannel)
            {
                throw new NotSupportedException("BoxBlurVerticalAlpha should be called for bitmaps with alpha channel");
            }

            var halfRange = range / 2;
            var newColors = new Color[targetFastBitmap.Height];
            var tmpColor = new byte[4];

            for (var x = targetFastBitmap.Left; x < targetFastBitmap.Right; x++)
            {
                var hits = 0;
                var a = 0;
                var r = 0;
                var g = 0;
                var b = 0;

                for (var y = targetFastBitmap.Top - halfRange; y < targetFastBitmap.Bottom; y++)
                {
                    var oldPixel = y - halfRange - 1;

                    if (oldPixel >= targetFastBitmap.Top)
                    {
                        targetFastBitmap.GetColorAt(x, oldPixel, tmpColor);
                        a -= tmpColor[FastBitmapBase.ColorIndexA];
                        r -= tmpColor[FastBitmapBase.ColorIndexR];
                        g -= tmpColor[FastBitmapBase.ColorIndexG];
                        b -= tmpColor[FastBitmapBase.ColorIndexB];
                        hits--;
                    }

                    var newPixel = y + halfRange;

                    if (newPixel < targetFastBitmap.Bottom)
                    {
                        //int colorg = pixels[index + newPixelOffset];
                        targetFastBitmap.GetColorAt(x, newPixel, tmpColor);
                        a += tmpColor[FastBitmapBase.ColorIndexA];
                        r += tmpColor[FastBitmapBase.ColorIndexR];
                        g += tmpColor[FastBitmapBase.ColorIndexG];
                        b += tmpColor[FastBitmapBase.ColorIndexB];
                        hits++;
                    }

                    if (y >= targetFastBitmap.Top)
                    {
                        newColors[y - targetFastBitmap.Top] = Color.FromArgb((byte)(a / hits), (byte)(r / hits), (byte)(g / hits), (byte)(b / hits));
                    }
                }

                for (var y = targetFastBitmap.Top; y < targetFastBitmap.Bottom; y++)
                {
                    targetFastBitmap.SetColorAt(x, y, ref newColors[y - targetFastBitmap.Top]);
                }
            }
        }

    }
}