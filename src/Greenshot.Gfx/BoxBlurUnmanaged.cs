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

using System;
using System.Threading.Tasks;
using Greenshot.Gfx.Structs;

namespace Greenshot.Gfx
{
    /// <summary>
    /// Code to apply a box blur
    /// </summary>
    public static class BoxBlurUnmanaged
    {
        /// <summary>
        ///     Apply BoxBlur to the UnmanagedBitmap
        /// </summary>
        /// <param name="unmanagedBitmap">UnmanagedBitmap</param>
        /// <param name="range">Must be even!</param>
        public static void ApplyBoxBlur(this UnmanagedBitmap<Bgr32> unmanagedBitmap, int range)
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
            BoxBlurHorizontal(unmanagedBitmap, range);
            BoxBlurVertical(unmanagedBitmap, range);
            BoxBlurHorizontal(unmanagedBitmap, range);
            BoxBlurVertical(unmanagedBitmap, range);
        }

        /// <summary>
        ///     Apply BoxBlur to the UnmanagedBitmap
        /// </summary>
        /// <param name="unmanagedBitmap">UnmanagedBitmap</param>
        /// <param name="range">Must be even!</param>
        public static void ApplyBoxBlur(this UnmanagedBitmap<Bgra32> unmanagedBitmap, int range)
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
            BoxBlurHorizontal(unmanagedBitmap, range);
            BoxBlurVertical(unmanagedBitmap, range);
            BoxBlurHorizontal(unmanagedBitmap, range);
            BoxBlurVertical(unmanagedBitmap, range);
        }

        /// <summary>
        ///     BoxBlurHorizontal is a private helper method for the BoxBlur, only for IFastBitmaps with alpha channel
        /// </summary>
        /// <param name="unmanagedBitmap">UnmanagedBitmap</param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurHorizontal(UnmanagedBitmap<Bgr32> unmanagedBitmap, int range)
        {
            var halfRange = range / 2;

            Parallel.For(0, unmanagedBitmap.Height, y =>
            {
                unsafe {
                    var rgb32 = unmanagedBitmap[y];
                    Span<Bgr32> averages = stackalloc Bgr32[range];
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    var hits = halfRange;
                    for (var x = 0; x < halfRange; x++)
                    {
                        ref Bgr32 color = ref rgb32[x];

                        r += color.R;
                        g += color.G;
                        b += color.B;
                    }
                    for (var x = 0; x < unmanagedBitmap.Width; x++)
                    {
                        var leftSide = x - halfRange - 1;
                        if (leftSide >= 0)
                        {
                            // Get value at the left side of range
                            ref Bgr32 color = ref rgb32[leftSide];
                            r -= color.R;
                            g -= color.G;
                            b -= color.B;
                            hits--;
                        }

                        var rightSide = x + halfRange;
                        if (rightSide < unmanagedBitmap.Width)
                        {
                            ref Bgr32 color = ref rgb32[rightSide];
                            r += color.R;
                            g += color.G;
                            b += color.B;
                            hits++;
                        }

                        ref Bgr32 average = ref averages[x % range];
                        average.R = (byte)(r / hits);
                        average.G = (byte)(g / hits);
                        average.B = (byte)(b / hits);

                        if (leftSide >= 0)
                        {
                            // Now we can write the value from the calculated avarages
                            var readLocation = (leftSide % range);

                            rgb32[leftSide] = averages[readLocation];
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     BoxBlurVertical is a private helper method for the BoxBlur
        /// </summary>
        /// <param name="unmanagedBitmap">UnmanagedBitmap</param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurVertical(UnmanagedBitmap<Bgr32> unmanagedBitmap, int range)
        {
            var halfRange = range / 2;
            Parallel.For(0, unmanagedBitmap.Width, x =>
            {
                unsafe {
                    var rgb32 = unmanagedBitmap.Span;
                    Span<Bgr32> averages = stackalloc Bgr32[range];
                    var hits = 0;
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    for (var y = 0; y < halfRange; y++)
                    {
                        ref Bgr32 color = ref rgb32[(y * unmanagedBitmap.Width) + x];
                        r += color.R;
                        g += color.G;
                        b += color.B;
                        hits++;
                    }
                    for (var y = 0; y < unmanagedBitmap.Height; y++)
                    {
                        var topSide = y - halfRange - 1;
                        if (topSide >= 0)
                        {
                            // Get value at the top side of range
                            ref Bgr32 color = ref rgb32[x + (topSide * unmanagedBitmap.Width)];
                            r -= color.R;
                            g -= color.G;
                            b -= color.B;
                            hits--;
                        }

                        var bottomSide = y + halfRange;
                        if (bottomSide < unmanagedBitmap.Height)
                        {
                            ref Bgr32 color = ref rgb32[x + (bottomSide * unmanagedBitmap.Width)];
                            r += color.R;
                            g += color.G;
                            b += color.B;
                            hits++;
                        }

                        ref Bgr32 average = ref averages[y % range];
                        average.R = (byte)(r / hits);
                        average.G = (byte)(g / hits);
                        average.B = (byte)(b / hits);

                        if (topSide >= 0)
                        {
                            // Write the value from the calculated avarages
                            var readLocation = topSide % range;

                            rgb32[x + (topSide * unmanagedBitmap.Width)] = averages[readLocation];
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     BoxBlurHorizontal is a private helper method for the BoxBlur, only for IFastBitmaps with alpha channel
        /// </summary>
        /// <param name="unmanagedBitmap">UnmanagedBitmap</param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurHorizontal(UnmanagedBitmap<Bgra32> unmanagedBitmap, int range)
        {
            var halfRange = range / 2;

            Parallel.For(0, unmanagedBitmap.Height, y =>
            {
                unsafe {
                    var argb32 = unmanagedBitmap[y];
                    Span<Bgra32> averages = stackalloc Bgra32[range];
                    var a = 0;
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    var hits = halfRange;
                    for (var x = 0; x < halfRange; x++)
                    {
                        ref Bgra32 color = ref argb32[x];
                        a += color.A;
                        r += color.R;
                        g += color.G;
                        b += color.B;
                    }
                    for (var x = 0; x < unmanagedBitmap.Width; x++)
                    {
                        var leftSide = x - halfRange - 1;
                        if (leftSide >= 0)
                        {
                            // Get value at the left side of range
                            ref Bgra32 color = ref argb32[leftSide];
                            a -= color.A;
                            r -= color.R;
                            g -= color.G;
                            b -= color.B;
                            hits--;
                        }

                        var rightSide = x + halfRange;
                        if (rightSide < unmanagedBitmap.Width)
                        {
                            ref Bgra32 color = ref argb32[rightSide];
                            a += color.A;
                            r += color.R;
                            g += color.G;
                            b += color.B;
                            hits++;
                        }

                        ref Bgra32 average = ref averages[x % range];
                        average.A = (byte)(a / hits);
                        average.R = (byte)(r / hits);
                        average.G = (byte)(g / hits);
                        average.B = (byte)(b / hits);

                        if (leftSide >= 0)
                        {
                            // Now we can write the value from the calculated avarages
                            var readLocation = leftSide % range;

                            argb32[leftSide] = averages[readLocation];
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     BoxBlurVertical is a private helper method for the BoxBlur
        /// </summary>
        /// <param name="unmanagedBitmap">UnmanagedBitmap</param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurVertical(UnmanagedBitmap<Bgra32> unmanagedBitmap, int range)
        {
            var halfRange = range / 2;
            Parallel.For(0, unmanagedBitmap.Width, x =>
            {
                unsafe
                {
                    var argb32 = unmanagedBitmap.Span;
                    Span<Bgra32> averages = stackalloc Bgra32[range];
                    var hits = 0;
                    var a = 0;
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    for (var y = 0; y < halfRange; y++)
                    {
                        ref Bgra32 color = ref argb32[x + (y * unmanagedBitmap.Width)];
                        a += color.A;
                        r += color.R;
                        g += color.G;
                        b += color.B;
                        hits++;
                    }
                    for (var y = 0; y < unmanagedBitmap.Height; y++)
                    {
                        var topSide = y - halfRange - 1;
                        if (topSide >= 0)
                        {
                            // Get value at the top side of range
                            ref Bgra32 color = ref argb32[x + (topSide * unmanagedBitmap.Width)];
                            a -= color.A;
                            r -= color.R;
                            g -= color.G;
                            b -= color.B;
                            hits--;
                        }

                        var bottomSide = y + halfRange;
                        if (bottomSide < unmanagedBitmap.Height)
                        {
                            ref Bgra32 color = ref argb32[x + (bottomSide * unmanagedBitmap.Width)];
                            a += color.A;
                            r += color.R;
                            g += color.G;
                            b += color.B;
                            hits++;
                        }

                        ref Bgra32 average = ref averages[(y % range)];
                        average.A = (byte)(a / hits);
                        average.R = (byte)(r / hits);
                        average.G = (byte)(g / hits);
                        average.B = (byte)(b / hits);

                        if (topSide >= 0)
                        {
                            // Write the value from the calculated avarages
                            var readLocation = (topSide % range);

                            argb32[x + (topSide * unmanagedBitmap.Width)] = averages[readLocation];
                        }
                    }
                }
            });
        }
    }
}
