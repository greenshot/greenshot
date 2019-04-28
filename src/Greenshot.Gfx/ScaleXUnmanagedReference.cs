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
    /// </summary>
    public static class ScaleXUnmanagedReference
    {
        private const byte ColorB = 0;
        private const byte ColorD = 1;
        private const byte ColorE = 4;
        private const byte ColorF = 2;
        private const byte ColorH = 3;
        private const byte ColorA = 5;
        private const byte ColorC = 6;
        private const byte ColorG = 7;
        private const byte ColorI = 8;

        /// <summary>
        /// Use "Scale2x" algorithm to produce bitmap from the original.
        /// Every pixel from input texture produces 4 output pixels, for more details check out http://scale2x.sourceforge.net/algorithm.html
        /// </summary>
        /// <param name="sourceBitmap">UnmanagedBitmap to scale 2x</param>
        /// <returns>UnmanagedBitmap</returns>
        public static UnmanagedBitmap<TPixelLayout> Scale2XReference<TPixelLayout>(this UnmanagedBitmap<TPixelLayout> sourceBitmap) where TPixelLayout : unmanaged
        {
            if (Marshal.SizeOf<TPixelLayout>() != 4)
            {
                throw new NotSupportedException("Only 4 byte unmanaged structs are supported.");
            }
            var destinationBitmap = new UnmanagedBitmap<TPixelLayout>(sourceBitmap.Width << 1, sourceBitmap.Height << 1);

            var colorBOffset = -sourceBitmap.Width;
            var colorHOffset = sourceBitmap.Width;
            ReadOnlySpan<int> sourceSpan = MemoryMarshal.Cast<TPixelLayout, int>(sourceBitmap.Span);
            var destinationSpan = MemoryMarshal.Cast<TPixelLayout, int>(destinationBitmap.Span);
            unsafe
            {
                var colors = stackalloc int[5];
                var colorsE = stackalloc int[4];

                for (var y = 0; y < sourceBitmap.Height; y++)
                {
                    var offset = y * sourceBitmap.Width;
                    for (var x = 0; x < sourceBitmap.Width; x++)
                    {
                        var xOffset = offset + x;
                        colors[ColorE] = sourceSpan[xOffset];

                        if (y != 0)
                        {
                            colors[ColorB] = sourceSpan[xOffset + colorBOffset];
                        }
                        else
                        {
                            colors[ColorB] = colors[ColorE];
                        }

                        if (y != sourceBitmap.Height - 1)
                        {
                            colors[ColorH] = sourceSpan[xOffset + colorHOffset];
                        }
                        else
                        {
                            colors[ColorH] = colors[ColorE];
                        }
                        if (x > 0)
                        {
                            colors[ColorD] = sourceSpan[xOffset - 1];
                        }
                        else
                        {
                            colors[ColorD] = colors[ColorE];
                        }
                        if (x < sourceBitmap.Width - 1)
                        {
                            colors[ColorF] = sourceSpan[xOffset + 1];
                        }
                        else
                        {
                            colors[ColorF] = colors[ColorE];
                        }

                        if (colors[ColorB] != colors[ColorH] && colors[ColorD] != colors[ColorF])
                        {
                            colorsE[0] = colors[ColorD] == colors[ColorB] ? colors[ColorD] : colors[ColorE];
                            colorsE[1] = colors[ColorB] == colors[ColorF] ? colors[ColorF] : colors[ColorE];
                            colorsE[2] = colors[ColorD] == colors[ColorH] ? colors[ColorD] : colors[ColorE];
                            colorsE[3] = colors[ColorH] == colors[ColorF] ? colors[ColorF] : colors[ColorE];
                        }
                        else
                        {
                            colorsE[0] = colors[ColorE];
                            colorsE[1] = colors[ColorE];
                            colorsE[2] = colors[ColorE];
                            colorsE[3] = colors[ColorE];
                        }

                        var destinationOffset = (x << 1) + ((y << 1) * destinationBitmap.Width);
                        destinationSpan[destinationOffset] = colorsE[0];
                        destinationSpan[destinationOffset + 1] = colorsE[1];
                        destinationSpan[destinationOffset + destinationBitmap.Width] = colorsE[2];
                        destinationSpan[destinationOffset + 1 + destinationBitmap.Width] = colorsE[3];
                    }
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
        public static UnmanagedBitmap<TPixelLayout> Scale3XReference<TPixelLayout>(this UnmanagedBitmap<TPixelLayout> sourceBitmap) where TPixelLayout : unmanaged
        {
            if (Marshal.SizeOf<TPixelLayout>() != 4)
            {
                throw new NotSupportedException("Only 4 byte unmanaged structs are supported.");
            }
            var destinationBitmap = new UnmanagedBitmap<TPixelLayout>(sourceBitmap.Width * 3, sourceBitmap.Height * 3);

            ReadOnlySpan<uint> sourceSpan = MemoryMarshal.Cast<TPixelLayout, uint>(sourceBitmap.Span);
            var destinationSpan = MemoryMarshal.Cast<TPixelLayout, uint>(destinationBitmap.Span);

            var sourceWidth = sourceBitmap.Width;
            var destinationWidth = destinationBitmap.Width;

            unsafe
            {
                var colors = stackalloc uint[9];
                var colorsE = stackalloc uint[9];

                for (var y = 0; y < sourceBitmap.Height; y++)
                {
                    var sourceYOffset = y * sourceWidth;
                    var destinationYOffset = y * 3 * destinationWidth;
                    for (var x = 0; x < sourceWidth; x++)
                    {
                        var sourceOffset = sourceYOffset + x;

                        colors[ColorE] = sourceSpan[sourceOffset];

                        if (y != 0 && x != 0)
                        {
                            colors[ColorA] = sourceSpan[(sourceOffset - 1) - sourceWidth];
                        }
                        else
                        {
                            colors[ColorA] = colors[ColorE];
                        }

                        if (y != 0)
                        {
                            colors[ColorB] = sourceSpan[sourceOffset - sourceWidth];
                        }
                        else
                        {
                            colors[ColorB] = colors[ColorE];
                        }

                        if (x < sourceWidth - 1 && y != 0)
                        {
                            colors[ColorC] = sourceSpan[(sourceOffset + 1) - sourceWidth];
                        }
                        else
                        {
                            colors[ColorC] = colors[ColorE];
                        }

                        if (x != 0)
                        {
                            colors[ColorD] = sourceSpan[sourceOffset - 1];
                        }
                        else
                        {
                            colors[ColorD] = colors[ColorE];
                        }

                        if (x < sourceWidth - 1)
                        {
                            colors[ColorF] = sourceSpan[sourceOffset + 1];
                        }
                        else
                        {
                            colors[ColorF] = colors[ColorE];
                        }

                        if (x != 0 && y < sourceBitmap.Height - 1)
                        {
                            colors[ColorG] = sourceSpan[(sourceOffset - 1) + sourceWidth];
                        }
                        else
                        {
                            colors[ColorG] = colors[ColorE];
                        }

                        if (y < sourceBitmap.Height - 1)
                        {
                            colors[ColorH] = sourceSpan[sourceOffset + sourceWidth];
                        }
                        else
                        {
                            colors[ColorH] = colors[ColorE];
                        }

                        if (x < sourceWidth - 1 && y < sourceBitmap.Height - 1)
                        {
                            colors[ColorI] = sourceSpan[sourceOffset + 1 + sourceWidth];
                        }
                        else
                        {
                            colors[ColorI] = colors[ColorE];
                        }

                        if (colors[ColorB] != colors[ColorH] && colors[ColorD] != colors[ColorF])
                        {
                            colorsE[8] = colors[ColorH] == colors[ColorF] ? colors[ColorF] : colors[ColorE];
                            colorsE[7] = colors[ColorD] == colors[ColorH] && colors[ColorE] != colors[ColorI] || colors[ColorH] == colors[ColorF] && colors[ColorE] != colors[ColorG] ? colors[ColorH] : colors[ColorE];
                            colorsE[6] = colors[ColorD] == colors[ColorH] ? colors[ColorD] : colors[ColorE];
                            colorsE[5] = colors[ColorB] == colors[ColorF] && colors[ColorE] != colors[ColorI] || colors[ColorH] == colors[ColorF] && colors[ColorE] != colors[ColorC] ? colors[ColorF] : colors[ColorE];

                            colorsE[4] = colors[ColorE];

                            colorsE[3] = colors[ColorD] == colors[ColorB] && colors[ColorE] != colors[ColorG] || colors[ColorD] == colors[ColorH] && colors[ColorE] != colors[ColorA] ? colors[ColorD] : colors[ColorE];
                            colorsE[2] = colors[ColorB] == colors[ColorF] ? colors[ColorF] : colors[ColorE];
                            colorsE[1] = colors[ColorD] == colors[ColorB] && colors[ColorE] != colors[ColorC] || colors[ColorB] == colors[ColorF] && colors[ColorE] != colors[ColorA] ? colors[ColorB] : colors[ColorE];
                            colorsE[0] = colors[ColorD] == colors[ColorB] ? colors[ColorD] : colors[ColorE];
                        }
                        else
                        {
                            colorsE[8] = colors[ColorE];
                            colorsE[7] = colors[ColorE];
                            colorsE[6] = colors[ColorE];
                            colorsE[5] = colors[ColorE];
                            colorsE[4] = colors[ColorE];
                            colorsE[3] = colors[ColorE];
                            colorsE[2] = colors[ColorE];
                            colorsE[1] = colors[ColorE];
                            colorsE[0] = colors[ColorE];
                        }
                        var destinationOffset = x * 3 + destinationYOffset;

                        destinationSpan[destinationOffset] = colorsE[0];
                        destinationSpan[destinationOffset + 1] = colorsE[1];
                        destinationSpan[destinationOffset + 2] = colorsE[2];

                        destinationOffset += destinationWidth;
                        destinationSpan[destinationOffset] = colorsE[3];
                        destinationSpan[destinationOffset + 1] = colorsE[4];
                        destinationSpan[destinationOffset + 2] = colorsE[5];

                        destinationOffset += destinationWidth;
                        destinationSpan[destinationOffset] = colorsE[6];
                        destinationSpan[destinationOffset + 1] = colorsE[7];
                        destinationSpan[destinationOffset + 2] = colorsE[8];
                    }
                }
            }
            return destinationBitmap;
        }
    }
}
