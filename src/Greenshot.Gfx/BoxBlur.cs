// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Greenshot.Gfx.FastBitmap;

namespace Greenshot.Gfx
{
    /// <summary>
    /// Code to apply a box blur
    /// </summary>
    public static class BoxBlur
    {
        /// <summary>
        ///     Apply BoxBlur to the destinationBitmap
        /// </summary>
        /// <param name="destinationBitmap">Bitmap to blur</param>
        /// <param name="range">Must be ODD, if not +1 is used</param>
        public static void ApplyBoxBlur(this IBitmapWithNativeSupport destinationBitmap, int range)
        {
            // We only need one fastbitmap as we use it as source and target (the reading is done for one line H/V, writing after "parsing" one line H/V)
            using (var fastBitmap = FastBitmapFactory.Create(destinationBitmap))
            {
                fastBitmap.ApplyBoxBlur(range);
            }
        }

        /// <summary>
        ///     Apply BoxBlur to the fastBitmap
        /// </summary>
        /// <param name="fastBitmap">IFastBitmap to blur</param>
        /// <param name="range">Must be even!</param>
        public static void ApplyBoxBlur(this IFastBitmap fastBitmap, int range)
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
            BoxBlurHorizontal(fastBitmap, range);
            BoxBlurVertical(fastBitmap, range);
            BoxBlurHorizontal(fastBitmap, range);
            BoxBlurVertical(fastBitmap, range);
        }

        /// <summary>
        ///     BoxBlurHorizontal is a private helper method for the BoxBlur, only for IFastBitmaps with alpha channel
        /// </summary>
        /// <param name="targetFastBitmap">Target BitmapBuffer</param>
        /// <param name="range">Range must be odd!</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BoxBlurHorizontal(IFastBitmap targetFastBitmap, int range)
        {
            var halfRange = range / 2;
            Parallel.For(targetFastBitmap.Top, targetFastBitmap.Bottom, y =>
            {
                unsafe
                {
                    var averages = stackalloc byte[range << 2];
                    var readColor = stackalloc byte[4];
                    var a = 0;
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    var hits = halfRange;
                    for (var x = targetFastBitmap.Left; x < targetFastBitmap.Left + halfRange; x++)
                    {
                        targetFastBitmap.GetColorAt(x, y, readColor);
                        a += readColor[FastBitmapBase.ColorIndexA];
                        r += readColor[FastBitmapBase.ColorIndexR];
                        g += readColor[FastBitmapBase.ColorIndexG];
                        b += readColor[FastBitmapBase.ColorIndexB];
                    }
                    for (var x = targetFastBitmap.Left; x < targetFastBitmap.Right; x++)
                    {
                        var leftSide = x - halfRange - 1;
                        if (leftSide >= targetFastBitmap.Left)
                        {
                            // Get value at the left side of range
                            targetFastBitmap.GetColorAt(leftSide, y, readColor);
                            a -= readColor[FastBitmapBase.ColorIndexA];
                            r -= readColor[FastBitmapBase.ColorIndexR];
                            g -= readColor[FastBitmapBase.ColorIndexG];
                            b -= readColor[FastBitmapBase.ColorIndexB];
                            hits--;
                        }

                        var rightSide = x + halfRange;
                        if (rightSide < targetFastBitmap.Right)
                        {
                            targetFastBitmap.GetColorAt(rightSide, y, readColor);
                            a += readColor[FastBitmapBase.ColorIndexA];
                            r += readColor[FastBitmapBase.ColorIndexR];
                            g += readColor[FastBitmapBase.ColorIndexG];
                            b += readColor[FastBitmapBase.ColorIndexB];
                            hits++;
                        }

                        var writeLocation = (x % range) << 2;
                        averages[writeLocation + FastBitmapBase.ColorIndexA] = (byte) (a / hits);
                        averages[writeLocation + FastBitmapBase.ColorIndexR] = (byte) (r / hits);
                        averages[writeLocation + FastBitmapBase.ColorIndexG] = (byte) (g / hits);
                        averages[writeLocation + FastBitmapBase.ColorIndexB] = (byte) (b / hits);

                        if (leftSide >= targetFastBitmap.Left) {
                            // Now we can write the value from the calculated avarages
                            var readLocation = (leftSide % range) << 2;
                            targetFastBitmap.SetColorAt(leftSide, y, averages, readLocation);
                        }
                }
                }
            });
        }

        /// <summary>
        ///     BoxBlurVertical is a private helper method for the BoxBlur
        /// </summary>
        /// <param name="targetFastBitmap">BitmapBuffer which previously was created with BoxBlurHorizontal</param>
        /// <param name="range">Range must be odd!</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BoxBlurVertical(IFastBitmap targetFastBitmap, int range)
        {
            var halfRange = range / 2;
            Parallel.For(targetFastBitmap.Left, targetFastBitmap.Right, x =>
            {
                unsafe
                {
                    var readColor = stackalloc byte[4];
                    var averages = stackalloc byte[range << 2];
                    var hits = 0;
                    var a = 0;
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    for (var y = targetFastBitmap.Top; y < targetFastBitmap.Top + halfRange; y++)
                    {
                        targetFastBitmap.GetColorAt(x, y, readColor);
                        a += readColor[FastBitmapBase.ColorIndexA];
                        r += readColor[FastBitmapBase.ColorIndexR];
                        g += readColor[FastBitmapBase.ColorIndexG];
                        b += readColor[FastBitmapBase.ColorIndexB];
                        hits++;
                    }
                    for (var y = targetFastBitmap.Top; y < targetFastBitmap.Bottom; y++)
                    {
                        var topSide = y - halfRange - 1;
                        if (topSide >= targetFastBitmap.Top)
                        {
                            // Get value at the top side of range
                            targetFastBitmap.GetColorAt(x, topSide, readColor);
                            a -= readColor[FastBitmapBase.ColorIndexA];
                            r -= readColor[FastBitmapBase.ColorIndexR];
                            g -= readColor[FastBitmapBase.ColorIndexG];
                            b -= readColor[FastBitmapBase.ColorIndexB];
                            hits--;
                        }

                        var bottomSide = y + halfRange;
                        if (bottomSide < targetFastBitmap.Bottom)
                        {
                            targetFastBitmap.GetColorAt(x, bottomSide, readColor);
                            a += readColor[FastBitmapBase.ColorIndexA];
                            r += readColor[FastBitmapBase.ColorIndexR];
                            g += readColor[FastBitmapBase.ColorIndexG];
                            b += readColor[FastBitmapBase.ColorIndexB];
                            hits++;
                        }

                        var writeLocation = (y % range) << 2;
                        averages[writeLocation + FastBitmapBase.ColorIndexA] = (byte)(a / hits);
                        averages[writeLocation + FastBitmapBase.ColorIndexR] = (byte)(r / hits);
                        averages[writeLocation + FastBitmapBase.ColorIndexG] = (byte)(g / hits);
                        averages[writeLocation + FastBitmapBase.ColorIndexB] = (byte)(b / hits);

                        if (topSide >= targetFastBitmap.Top)
                        {
                            // Write the value from the calculated avarages
                            var readLocation = (topSide % range) << 2;
                            targetFastBitmap.SetColorAt(x, topSide, averages, readLocation);
                        }

                    }
                }
            });
        }
    }
}
