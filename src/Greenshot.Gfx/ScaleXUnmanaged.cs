// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Greenshot.Gfx
{
    /// <summary>
    /// This is the ScaleX code for the UnmanagedBitmap
    /// https://www.scale2x.it/algorithm
    /// </summary>
    public static class ScaleXUnmanaged
    {
        /// <summary>
        /// Use "Scale2x" algorithm to produce bitmap from the original.
        /// Every pixel from input texture produces 4 output pixels, for more details check out http://scale2x.sourceforge.net/algorithm.html
        /// </summary>
        /// <param name="sourceBitmap">UnmanagedBitmap to scale 2x</param>
        /// <returns>UnmanagedBitmap</returns>
        public static UnmanagedBitmap<TPixelLayout> Scale2X<TPixelLayout>(this UnmanagedBitmap<TPixelLayout> sourceBitmap) where TPixelLayout : unmanaged
        {
            if (Marshal.SizeOf<TPixelLayout>() != 4)
            {
                throw new NotSupportedException("Only 4 byte unmanaged structs are supported.");
            }
            var destinationBitmap = new UnmanagedBitmap<TPixelLayout>(sourceBitmap.Width << 1, sourceBitmap.Height << 1);

            var sourceWidth = sourceBitmap.Width;
            var destinationWidth = destinationBitmap.Width;
            ref var destinationPointer = ref Unsafe.As<TPixelLayout, uint>(ref destinationBitmap.Span.GetPinnableReference());
            ref var sourcePointer = ref Unsafe.As<TPixelLayout, uint>(ref sourceBitmap.Span.GetPinnableReference());
            for (var y = 0; y < sourceBitmap.Height; y++)
            {
                var sourceYOffset = y * sourceWidth;
                var destinationYOffset = (y << 1) * destinationWidth;
                for (var x = 0; x < sourceWidth; x++)
                {
                    ref var colorE = ref Unsafe.Add(ref sourcePointer, sourceYOffset + x);

                    ref readonly var colorB = ref colorE;
                    if (y != 0)
                    {
                        colorB = ref Unsafe.Subtract(ref colorE, sourceWidth);
                    }

                    ref readonly var colorH = ref colorE;
                    if (y != sourceBitmap.Height - 1)
                    {
                        colorH = ref Unsafe.Add(ref colorE, sourceWidth);
                    }

                    ref readonly var colorD = ref colorE;
                    if (x > 0)
                    {
                        colorD = ref Unsafe.Subtract(ref colorE, 1);
                    }

                    ref readonly var colorF = ref colorE;
                    if (x < sourceBitmap.Width - 1)
                    {
                        colorF = ref Unsafe.Add(ref colorE, 1);
                    }

                    ref readonly var colorE0 = ref colorE;
                    ref readonly var colorE1 = ref colorE;
                    ref readonly var colorE2 = ref colorE;
                    ref readonly var colorE3 = ref colorE;
                    if (colorB != colorH && colorD != colorF)
                    {
                        if (colorH == colorF)
                        {
                            colorE3 = ref colorF;
                        }
                        if (colorD == colorH)
                        {
                            colorE2 = ref colorD;
                        }
                        if (colorB == colorF)
                        {
                            colorE1 = ref colorF;
                        }
                        if (colorD == colorB)
                        {
                            colorE0 = ref colorD;
                        }
                    }

                    ref var destColorE0 = ref Unsafe.Add(ref destinationPointer, (x << 1) + destinationYOffset);
                    destColorE0 = colorE0;
                    Unsafe.Add(ref destColorE0, 1 + destinationWidth) = colorE3;
                    Unsafe.Add(ref destColorE0, destinationWidth) = colorE2;
                    Unsafe.Add(ref destColorE0, 1) = colorE1;
                }
            }

            return destinationBitmap;
        }


        /// <summary>
        /// Use "Scale3x" algorithm to produce bitmap from the original.
        /// Every pixel from input texture produces 6 output pixels, for more details check out http://scale2x.sourceforge.net/algorithm.html
        /// </summary>
        /// <param name="sourceBitmap">UnmanagedBitmap to scale 3x</param>
        /// <returns>UnmanagedBitmap</returns>
        public static UnmanagedBitmap<TPixelLayout> Scale3X<TPixelLayout>(this UnmanagedBitmap<TPixelLayout> sourceBitmap) where TPixelLayout : unmanaged
        {
            if (Marshal.SizeOf<TPixelLayout>() != 4)
            {
                throw new NotSupportedException("Only 4 byte unmanaged structs are supported.");
            }
            // Create destination bitmap, where the scaled image is written to
            var destinationBitmap = new UnmanagedBitmap<TPixelLayout>(sourceBitmap.Width * 3, sourceBitmap.Height * 3);

            var sourceWidth = sourceBitmap.Width;
            var destinationWidth = destinationBitmap.Width;

            ref var destinationPointer = ref Unsafe.As<TPixelLayout, uint>(ref destinationBitmap.Span.GetPinnableReference());
            ref var sourcePointer = ref Unsafe.As<TPixelLayout, uint>(ref sourceBitmap.Span.GetPinnableReference());

            for (var y = 0; y < sourceBitmap.Height; y++)
            {
                var sourceYOffset = y * sourceWidth;
                var destinationYOffset = y * 3 * destinationWidth;
                for (var x = 0; x < sourceWidth; x++)
                {
                    ref var colorE = ref Unsafe.Add(ref sourcePointer, sourceYOffset + x);
                    ref readonly var colorA = ref colorE;
                    ref readonly var colorB = ref colorE;
                    ref readonly var colorC = ref colorE;
                    ref readonly var colorD = ref colorE;
                    ref readonly var colorF = ref colorE;
                    ref readonly var colorG = ref colorE;
                    ref readonly var colorH = ref colorE;
                    ref readonly var colorI = ref colorE;

                    if (y != 0 && x != 0)
                    {
                        colorA = ref Unsafe.Subtract(ref colorE, 1 + sourceWidth);
                    }

                    if (y != 0)
                    {
                        colorB = ref Unsafe.Subtract(ref colorE, sourceWidth);
                    }

                    if (x < sourceWidth - 1 && y != 0)
                    {
                        colorC = ref Unsafe.Subtract(ref colorE, sourceWidth - 1);
                    }

                    if (x != 0)
                    {
                        colorD = ref Unsafe.Subtract(ref colorE, 1);
                    }

                    if (x < sourceWidth - 1)
                    {
                        colorF = ref Unsafe.Add(ref colorE, 1);
                    }

                    if (x != 0 && y < sourceBitmap.Height - 1)
                    {
                        colorG = ref Unsafe.Add(ref colorE, sourceWidth - 1);
                    }

                    if (y < sourceBitmap.Height - 1)
                    {
                        colorH = ref Unsafe.Add(ref colorE, sourceWidth);
                    }

                    if (x < sourceWidth - 1 && y < sourceBitmap.Height - 1)
                    {
                        colorI = ref Unsafe.Add(ref colorE, sourceWidth + 1);
                    }

                    ref readonly var colorE0 = ref colorE;
                    ref readonly var colorE1 = ref colorE;
                    ref readonly var colorE2 = ref colorE;
                    ref readonly var colorE3 = ref colorE;
                    ref readonly var colorE4 = ref colorE;
                    ref readonly var colorE5 = ref colorE;
                    ref readonly var colorE6 = ref colorE;
                    ref readonly var colorE7 = ref colorE;
                    ref readonly var colorE8 = ref colorE;
                    if (colorB != colorH && colorD != colorF)
                    {
                        if (colorH == colorF)
                        {
                            colorE8 = ref colorF;
                        }
                        if (colorD == colorH && colorE != colorI || colorH == colorF && colorE != colorG)
                        {
                            colorE7 = ref colorH;
                        }
                        if (colorD == colorH)
                        {
                            colorE6 = ref colorD;
                        }
                        if (colorB == colorF && colorE != colorI || colorH == colorF && colorE != colorC)
                        {
                            colorE5 = ref colorF;
                        }

                        if (colorD == colorB && colorE != colorG || colorD == colorH && colorE != colorA)
                        {
                            colorE3 = ref colorD;
                        }
                        if (colorB == colorF)
                        {
                            colorE2 = ref colorF;
                        }
                        if (colorD == colorB && colorE != colorC || colorB == colorF && colorE != colorA)
                        {
                            colorE1 = ref colorB;
                        }
                        if (colorD == colorB)
                        {
                            colorE0 = ref colorD;
                        }
                    }

                    var destinationOffset = x * 3 + destinationYOffset;

                    Unsafe.Add(ref destinationPointer, destinationOffset) = colorE0;
                    Unsafe.Add(ref destinationPointer, destinationOffset + 1) = colorE1;
                    Unsafe.Add(ref destinationPointer, destinationOffset + 2) = colorE2;

                    destinationOffset += destinationWidth;
                    Unsafe.Add(ref destinationPointer, destinationOffset) = colorE3;
                    Unsafe.Add(ref destinationPointer, destinationOffset + 1) = colorE4;
                    Unsafe.Add(ref destinationPointer, destinationOffset + 2) = colorE5;

                    destinationOffset += destinationWidth;
                    Unsafe.Add(ref destinationPointer, destinationOffset) = colorE6;
                    Unsafe.Add(ref destinationPointer, destinationOffset + 1) = colorE7;
                    Unsafe.Add(ref destinationPointer, destinationOffset + 2) = colorE8;
                }
            }

            return destinationBitmap;
        }
    }
}
