#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Greenshot.Gfx.Experimental.Structs;


namespace Greenshot.Gfx.Experimental
{
    /// <summary>
    /// Code to apply a box blur
    /// </summary>
    public static class BoxBlurSpan
    {
        /// <summary>
        ///     Apply BoxBlur to the destinationBitmap
        /// </summary>
        /// <param name="destinationBitmap">Bitmap to blur</param>
        /// <param name="range">Must be ODD, if not +1 is used</param>
        public static void ApplyBoxBlurSpan(this Bitmap destinationBitmap, int range)
        {
            var bitmapData = destinationBitmap.LockBits(new Rectangle(Point.Empty, destinationBitmap.Size), ImageLockMode.ReadWrite, destinationBitmap.PixelFormat);
            try
            {
                var pixelStride = destinationBitmap.Width;
                bool isAlpha = false;
                switch (bitmapData.PixelFormat)
                {
                    case PixelFormat.Format24bppRgb:
                        pixelStride = bitmapData.Stride / 3;
                        break;
                    case PixelFormat.Format32bppRgb:
                    case PixelFormat.Format32bppArgb:
                        pixelStride = bitmapData.Stride / 4;
                        isAlpha = true;
                        break;
                }

                var spanInfo = new SpanInfo
                {
                    Width = destinationBitmap.Width,
                    Height = destinationBitmap.Height,
                    Left = 0,
                    Right = destinationBitmap.Width,
                    Top = 0,
                    Bottom = destinationBitmap.Height,
                    Pointer = bitmapData.Scan0,
                    PixelStride = pixelStride,
                    BitmapSize = destinationBitmap.Height * pixelStride
                };
                if (isAlpha)
                {
                    ApplyBoxBlurSpanAlpha(spanInfo, range);
                }
                else
                {
                    ApplyBoxBlurSpan(spanInfo, range);
                }

            }
            finally
            {
                destinationBitmap.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        ///     Apply BoxBlur to the fastBitmap
        /// </summary>
        /// <param name="spanInfo">SpanInfo</param>
        /// <param name="range">Must be even!</param>
        private static void ApplyBoxBlurSpan(SpanInfo spanInfo, int range)
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
            BoxBlurHorizontalSpan(spanInfo, range);
            BoxBlurVerticalSpan(spanInfo, range);
            BoxBlurHorizontalSpan(spanInfo, range);
            BoxBlurVerticalSpan(spanInfo, range);
        }


        /// <summary>
        ///     Apply BoxBlur to the fastBitmap
        /// </summary>
        /// <param name="spanInfo">SpanInfo</param>
        /// <param name="range">Must be even!</param>
        private static void ApplyBoxBlurSpanAlpha(SpanInfo spanInfo, int range)
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
            BoxBlurHorizontalSpanAlpha(spanInfo, range);
            BoxBlurVerticalSpanAlpha(spanInfo, range);
            BoxBlurHorizontalSpanAlpha(spanInfo, range);
            BoxBlurVerticalSpanAlpha(spanInfo, range);
        }
        /// <summary>
        ///     BoxBlurHorizontal is a private helper method for the BoxBlur, only for IFastBitmaps with alpha channel
        /// </summary>
        /// <param name="spanInfo"></param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurHorizontalSpan(SpanInfo spanInfo, int range)
        {
            var halfRange = range / 2;

            Parallel.For(spanInfo.Top, spanInfo.Bottom, y =>
            {
                Span<Rgb24> rgb24;
                unsafe { rgb24 = new Span<Rgb24>((byte*)spanInfo.Pointer, spanInfo.BitmapSize); }
                unsafe
                {
                    Span<Rgb24> averages = stackalloc Rgb24[range];
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    var hits = halfRange;
                    for (var x = spanInfo.Left; x < spanInfo.Left + halfRange; x++)
                    {
                        ref Rgb24 color = ref rgb24[x + y * spanInfo.PixelStride];

                        r += color.R;
                        g += color.G;
                        b += color.B;
                    }
                    for (var x = spanInfo.Left; x < spanInfo.Right; x++)
                    {
                        var leftSide = x - halfRange - 1;
                        if (leftSide >= spanInfo.Left)
                        {
                            // Get value at the left side of range
                            ref Rgb24 color = ref rgb24[leftSide + y * spanInfo.PixelStride];
                            r -= color.R;
                            g -= color.G;
                            b -= color.B;
                            hits--;
                        }

                        var rightSide = x + halfRange;
                        if (rightSide < spanInfo.Right)
                        {
                            ref Rgb24 color = ref rgb24[rightSide + y * spanInfo.PixelStride];
                            r += color.R;
                            g += color.G;
                            b += color.B;
                            hits++;
                        }

                        ref Rgb24 average = ref averages[(x % range)];
                        average.R = (byte)(r / hits);
                        average.G = (byte)(g / hits);
                        average.B = (byte)(b / hits);

                        if (leftSide >= spanInfo.Left)
                        {
                            // Now we can write the value from the calculated avarages
                            var readLocation = (leftSide % range);

                            rgb24[leftSide + y * spanInfo.PixelStride] = averages[readLocation];
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     BoxBlurVertical is a private helper method for the BoxBlur
        /// </summary>
        /// <param name="spanInfo"></param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurVerticalSpan(SpanInfo spanInfo, int range)
        {
            var halfRange = range / 2;
            Parallel.For(spanInfo.Left, spanInfo.Right, x =>
            {
                Span<Rgb24> rgb24;
                unsafe { rgb24 = new Span<Rgb24>((byte*)spanInfo.Pointer, spanInfo.BitmapSize); }
                unsafe
                {
                    Span<Rgb24> averages = stackalloc Rgb24[range];
                    var hits = 0;
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    for (var y = spanInfo.Top; y < spanInfo.Top + halfRange; y++)
                    {
                        ref Rgb24 color = ref rgb24[x + y * spanInfo.PixelStride];
                        r += color.R;
                        g += color.G;
                        b += color.B;
                        hits++;
                    }
                    for (var y = spanInfo.Top; y < spanInfo.Bottom; y++)
                    {
                        var topSide = y - halfRange - 1;
                        if (topSide >= spanInfo.Top)
                        {
                            // Get value at the top side of range
                            ref Rgb24 color = ref rgb24[x + topSide * spanInfo.PixelStride];
                            r -= color.R;
                            g -= color.G;
                            b -= color.B;
                            hits--;
                        }

                        var bottomSide = y + halfRange;
                        if (bottomSide < spanInfo.Bottom)
                        {
                            ref Rgb24 color = ref rgb24[x + bottomSide * spanInfo.PixelStride];
                            r += color.R;
                            g += color.G;
                            b += color.B;
                            hits++;
                        }

                        ref Rgb24 average = ref averages[(y % range)];
                        average.R = (byte)(r / hits);
                        average.G = (byte)(g / hits);
                        average.B = (byte)(b / hits);

                        if (topSide >= spanInfo.Top)
                        {
                            // Write the value from the calculated avarages
                            var readLocation = (topSide % range);

                            rgb24[x + topSide * spanInfo.PixelStride] = averages[readLocation];
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     BoxBlurHorizontal is a private helper method for the BoxBlur, only for IFastBitmaps with alpha channel
        /// </summary>
        /// <param name="spanInfo"></param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurHorizontalSpanAlpha(SpanInfo spanInfo, int range)
        {
            var halfRange = range / 2;

            Parallel.For(spanInfo.Top, spanInfo.Bottom, y =>
            {
                Span<Bgra32> argb32;
                unsafe { argb32 = new Span<Bgra32>((byte*)spanInfo.Pointer, spanInfo.BitmapSize); }
                unsafe
                {
                    Span<Bgra32> averages = stackalloc Bgra32[range];
                    var a = 0;
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    var hits = halfRange;
                    for (var x = spanInfo.Left; x < spanInfo.Left + halfRange; x++)
                    {
                        ref Bgra32 color = ref argb32[x + y * spanInfo.PixelStride];
                        a += color.A;
                        r += color.R;
                        g += color.G;
                        b += color.B;
                    }
                    for (var x = spanInfo.Left; x < spanInfo.Right; x++)
                    {
                        var leftSide = x - halfRange - 1;
                        if (leftSide >= spanInfo.Left)
                        {
                            // Get value at the left side of range
                            ref Bgra32 color = ref argb32[leftSide + y * spanInfo.PixelStride];
                            a -= color.A;
                            r -= color.R;
                            g -= color.G;
                            b -= color.B;
                            hits--;
                        }

                        var rightSide = x + halfRange;
                        if (rightSide < spanInfo.Right)
                        {
                            ref Bgra32 color = ref argb32[rightSide + y * spanInfo.PixelStride];
                            a += color.A;
                            r += color.R;
                            g += color.G;
                            b += color.B;
                            hits++;
                        }

                        ref Bgra32 average = ref averages[(x % range)];
                        average.A = (byte)(a / hits);
                        average.R = (byte)(r / hits);
                        average.G = (byte)(g / hits);
                        average.B = (byte)(b / hits);

                        if (leftSide >= spanInfo.Left)
                        {
                            // Now we can write the value from the calculated avarages
                            var readLocation = (leftSide % range);

                            argb32[leftSide + y * spanInfo.PixelStride] = averages[readLocation];
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     BoxBlurVertical is a private helper method for the BoxBlur
        /// </summary>
        /// <param name="spanInfo"></param>
        /// <param name="range">Range must be odd!</param>
        private static void BoxBlurVerticalSpanAlpha(SpanInfo spanInfo, int range)
        {
            var halfRange = range / 2;
            Parallel.For(spanInfo.Left, spanInfo.Right, x =>
            {
                Span<Bgra32> argb32;
                unsafe { argb32 = new Span<Bgra32>((byte*)spanInfo.Pointer, spanInfo.BitmapSize); }
                unsafe
                {
                    Span<Bgra32> averages = stackalloc Bgra32[range];
                    var hits = 0;
                    var a = 0;
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    for (var y = spanInfo.Top; y < spanInfo.Top + halfRange; y++)
                    {
                        ref Bgra32 color = ref argb32[x + y * spanInfo.PixelStride];
                        a += color.A;
                        r += color.R;
                        g += color.G;
                        b += color.B;
                        hits++;
                    }
                    for (var y = spanInfo.Top; y < spanInfo.Bottom; y++)
                    {
                        var topSide = y - halfRange - 1;
                        if (topSide >= spanInfo.Top)
                        {
                            // Get value at the top side of range
                            ref Bgra32 color = ref argb32[x + topSide * spanInfo.PixelStride];
                            a -= color.A;
                            r -= color.R;
                            g -= color.G;
                            b -= color.B;
                            hits--;
                        }

                        var bottomSide = y + halfRange;
                        if (bottomSide < spanInfo.Bottom)
                        {
                            ref Bgra32 color = ref argb32[x + bottomSide * spanInfo.PixelStride];
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

                        if (topSide >= spanInfo.Top)
                        {
                            // Write the value from the calculated avarages
                            var readLocation = (topSide % range);

                            argb32[x + topSide * spanInfo.PixelStride] = averages[readLocation];
                        }
                    }
                }
            });
        }
    }
}
