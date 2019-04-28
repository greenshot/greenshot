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
            ReadOnlySpan<uint> sourceSpan = MemoryMarshal.Cast<TPixelLayout, uint>(sourceBitmap.Span);
            var destinationSpan = MemoryMarshal.Cast<TPixelLayout, uint>(destinationBitmap.Span);
            for (var y = 0; y < sourceBitmap.Height; y++)
            {
                var sourceYOffset = y * sourceWidth;
                var destinationYOffset = (y << 1) * destinationWidth;
                for (var x = 0; x < sourceWidth; x++)
                {
                    var sourceOffset = sourceYOffset + x;
                    ref readonly uint colorE = ref sourceSpan[sourceOffset];

                    ref readonly uint colorB = ref colorE;
                    if (y != 0)
                    {
                        colorB = ref sourceSpan[sourceOffset - sourceWidth];
                    }

                    ref readonly uint colorH = ref colorE;
                    if (y != sourceBitmap.Height - 1)
                    {
                        colorH = ref sourceSpan[sourceOffset + sourceWidth];
                    }

                    ref readonly uint colorD = ref colorE;
                    if (x > 0)
                    {
                        colorD = ref sourceSpan[sourceOffset - 1];
                    }

                    ref readonly uint colorF = ref colorE;
                    if (x < sourceBitmap.Width - 1)
                    {
                        colorF = ref sourceSpan[sourceOffset + 1];
                    }

                    ref readonly uint colorE0 = ref colorE;
                    ref readonly uint colorE1 = ref colorE;
                    ref readonly uint colorE2 = ref colorE;
                    ref readonly uint colorE3 = ref colorE;
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
                    var destinationOffset = (x << 1) + destinationYOffset;
                    destinationSpan[destinationOffset + 1 + destinationWidth] = colorE3;
                    destinationSpan[destinationOffset + destinationWidth] = colorE2;
                    destinationSpan[destinationOffset + 1] = colorE1;
                    destinationSpan[destinationOffset] = colorE0;
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

            ReadOnlySpan<uint> sourceSpan = MemoryMarshal.Cast<TPixelLayout, uint>(sourceBitmap.Span);
            var destinationSpan = MemoryMarshal.Cast<TPixelLayout, uint>(destinationBitmap.Span);

            var sourceWidth = sourceBitmap.Width;
            var destinationWidth = destinationBitmap.Width;

            unchecked
            {
                for (var y = 0; y < sourceBitmap.Height; y++)
                {
                    var sourceYOffset = y * sourceWidth;
                    var destinationYOffset = y * 3 * destinationWidth;
                    for (var x = 0; x < sourceWidth; x++)
                    {
                        var sourceOffset = sourceYOffset + x;
                        ref readonly uint colorE = ref sourceSpan[sourceOffset];
                        ref readonly uint colorA = ref colorE;
                        ref readonly uint colorB = ref colorE;
                        ref readonly uint colorC = ref colorE;
                        ref readonly uint colorD = ref colorE;
                        ref readonly uint colorF = ref colorE;
                        ref readonly uint colorG = ref colorE;
                        ref readonly uint colorH = ref colorE;
                        ref readonly uint colorI = ref colorE;

                        if (y != 0 && x != 0)
                        {
                            colorA = ref sourceSpan[sourceOffset - 1 - sourceWidth];
                        }

                        if (y != 0)
                        {
                            colorB = ref sourceSpan[sourceOffset - sourceWidth];
                        }

                        if (x < sourceWidth - 1 && y != 0)
                        {
                            colorC = ref sourceSpan[sourceOffset + 1 - sourceWidth];
                        }

                        if (x != 0)
                        {
                            colorD = ref sourceSpan[sourceOffset - 1];
                        }

                        if (x < sourceWidth - 1)
                        {
                            colorF = ref sourceSpan[sourceOffset + 1];
                        }

                        if (x != 0 && y < sourceBitmap.Height - 1)
                        {
                            colorG = ref sourceSpan[sourceOffset - 1 + sourceWidth];
                        }

                        if (y < sourceBitmap.Height - 1)
                        {
                            colorH = ref sourceSpan[sourceOffset + sourceWidth];
                        }

                        if (x < sourceWidth - 1 && y < sourceBitmap.Height - 1)
                        {
                            colorI = ref sourceSpan[sourceOffset + 1 + sourceWidth];
                        }

                        ref readonly uint colorE0 = ref colorE;
                        ref readonly uint colorE1 = ref colorE;
                        ref readonly uint colorE2 = ref colorE;
                        ref readonly uint colorE3 = ref colorE;
                        ref readonly uint colorE4 = ref colorE;
                        ref readonly uint colorE5 = ref colorE;
                        ref readonly uint colorE6 = ref colorE;
                        ref readonly uint colorE7 = ref colorE;
                        ref readonly uint colorE8 = ref colorE;
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

                        destinationSpan[destinationOffset] = colorE0;
                        destinationSpan[destinationOffset + 1] = colorE1;
                        destinationSpan[destinationOffset + 2] = colorE2;

                        destinationOffset += destinationWidth;
                        destinationSpan[destinationOffset] = colorE3;
                        destinationSpan[destinationOffset + 1] = colorE4;
                        destinationSpan[destinationOffset + 2] = colorE5;

                        destinationOffset += destinationWidth;
                        destinationSpan[destinationOffset] = colorE6;
                        destinationSpan[destinationOffset + 1] = colorE7;
                        destinationSpan[destinationOffset + 2] = colorE8;
                    }
                }
            }

            return destinationBitmap;
        }
    }
}
