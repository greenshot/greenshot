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

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Greenshot.Gfx.FastBitmap;

namespace Greenshot.Gfx
{
    /// <summary>
    /// This is the scale X implementation using IFastBitmap
    /// </summary>
    public static class ScaleX
    {
        /// <summary>
        ///     Use "Scale2x" algorithm to produce bitmap from the original.
        /// </summary>
        /// <param name="original">Bitmap to scale 2x</param>
        public static IBitmapWithNativeSupport Scale2X(IBitmapWithNativeSupport original)
        {
            using (var source = (IFastBitmapWithClip)FastBitmapFactory.Create(original))
            using (var destination = (IFastBitmapWithClip)FastBitmapFactory.CreateEmpty(new Size(original.Width << 1, original.Height << 1), original.PixelFormat))
            {
                // Every pixel from input texture produces 4 output pixels, for more details check out http://scale2x.sourceforge.net/algorithm.html
                Parallel.For(0, source.Height, y =>
                {
                    unsafe
                    {
                        var colorB = stackalloc byte[4];
                        var colorD = stackalloc byte[4];
                        var colorE = stackalloc byte[4];
                        var colorF = stackalloc byte[4];
                        var colorH = stackalloc byte[4];
                        var x = 0;
                        while (x < source.Width)
                        {
                            source.GetColorAt(x, y - 1, colorB);
                            source.GetColorAt(x, y + 1, colorH);
                            source.GetColorAt(x - 1, y, colorD);
                            source.GetColorAt(x + 1, y, colorF);
                            source.GetColorAt(x, y, colorE);

                            byte* colorE0, colorE1, colorE2, colorE3;
                            if (!AreColorsSame(colorB, colorH) && !AreColorsSame(colorD, colorF))
                            {
                                colorE0 = AreColorsSame(colorD, colorB) ? colorD : colorE;
                                colorE1 = AreColorsSame(colorB, colorF) ? colorF : colorE;
                                colorE2 = AreColorsSame(colorD, colorH) ? colorD : colorE;
                                colorE3 = AreColorsSame(colorH, colorF) ? colorF : colorE;
                            }
                            else
                            {
                                colorE0 = colorE;
                                colorE1 = colorE;
                                colorE2 = colorE;
                                colorE3 = colorE;
                            }

                            destination.SetColorAt(x << 1, y << 1, colorE0);
                            destination.SetColorAt((x << 1) + 1, y << 1, colorE1);
                            destination.SetColorAt(x << 1, (y << 1) + 1, colorE2);
                            destination.SetColorAt((x << 1) + 1, (y << 1) + 1, colorE3);

                            x++;
                        }

                    }
                });
                return destination.UnlockAndReturnBitmap();
            }
        }

        /// <summary>
        ///     Use "Scale3x" algorithm to produce bitmap from the original.
        /// </summary>
        /// <param name="original">Bitmap to scale 3x</param>
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public static IBitmapWithNativeSupport Scale3X(IBitmapWithNativeSupport original)
        {
            using (var source = (IFastBitmapWithClip)FastBitmapFactory.Create(original))
            using (var destination = (IFastBitmapWithClip)FastBitmapFactory.CreateEmpty(new Size(original.Width * 3, original.Height * 3), original.PixelFormat))
            {
                // Every pixel from input texture produces 6 output pixels, for more details check out http://scale2x.sourceforge.net/algorithm.html
                Parallel.For(0, source.Height, y =>
                {
                    unsafe
                    {
                        var x = 0;
                        var colorA = stackalloc byte[4];
                        var colorB = stackalloc byte[4];
                        var colorC = stackalloc byte[4];
                        var colorD = stackalloc byte[4];
                        var colorE = stackalloc byte[4];
                        var colorF = stackalloc byte[4];
                        var colorG = stackalloc byte[4];
                        var colorH = stackalloc byte[4];
                        var colorI = stackalloc byte[4];
                        while (x < source.Width)
                        {
                            source.GetColorAt(x - 1, y - 1, colorA);
                            source.GetColorAt(x, y - 1, colorB);
                            source.GetColorAt(x + 1, y - 1, colorC);

                            source.GetColorAt(x - 1, y, colorD);
                            source.GetColorAt(x, y, colorE);
                            source.GetColorAt(x + 1, y, colorF);

                            source.GetColorAt(x - 1, y + 1, colorG);
                            source.GetColorAt(x, y + 1, colorH);
                            source.GetColorAt(x + 1, y + 1, colorI);

                            byte* colorE0, colorE1, colorE2, colorE3, colorE4, colorE5, colorE6, colorE7, colorE8;

                            if (!AreColorsSame(colorB, colorH) && !AreColorsSame(colorD, colorF))
                            {
                                colorE0 = AreColorsSame(colorD, colorB) ? colorD : colorE;
                                colorE1 = AreColorsSame(colorD, colorB) && !AreColorsSame(colorE, colorC) || AreColorsSame(colorB, colorF) && !AreColorsSame(colorE, colorA) ? colorB : colorE;
                                colorE2 = AreColorsSame(colorB, colorF) ? colorF : colorE;
                                colorE3 = AreColorsSame(colorD, colorB) && !AreColorsSame(colorE, colorG) || AreColorsSame(colorD, colorH) && !AreColorsSame(colorE, colorA) ? colorD : colorE;

                                colorE4 = colorE;
                                colorE5 = AreColorsSame(colorB, colorF) && !AreColorsSame(colorE, colorI) || AreColorsSame(colorH, colorF) && !AreColorsSame(colorE, colorC) ? colorF : colorE;
                                colorE6 = AreColorsSame(colorD, colorH) ? colorD : colorE;
                                colorE7 = AreColorsSame(colorD, colorH) && !AreColorsSame(colorE, colorI) || AreColorsSame(colorH, colorF) && !AreColorsSame(colorE, colorG) ? colorH : colorE;
                                colorE8 = AreColorsSame(colorH, colorF) ? colorF : colorE;
                            }
                            else
                            {
                                colorE0 = colorE;
                                colorE1 = colorE;
                                colorE2 = colorE;
                                colorE3 = colorE;
                                colorE4 = colorE;
                                colorE5 = colorE;
                                colorE6 = colorE;
                                colorE7 = colorE;
                                colorE8 = colorE;
                            }
                            var multipliedX = 3 * x;
                            var multipliedY = 3 * y;

                            destination.SetColorAt(multipliedX, multipliedY, colorE0);
                            destination.SetColorAt(multipliedX + 1, multipliedY, colorE1);
                            destination.SetColorAt(multipliedX + 2, multipliedY, colorE2);

                            multipliedY++;
                            destination.SetColorAt(multipliedX, multipliedY, colorE3);
                            destination.SetColorAt(multipliedX + 1, multipliedY, colorE4);
                            destination.SetColorAt(multipliedX + 2, multipliedY, colorE5);

                            multipliedY++;
                            destination.SetColorAt(multipliedX, multipliedY, colorE6);
                            destination.SetColorAt(multipliedX + 1, multipliedY, colorE7);
                            destination.SetColorAt(multipliedX + 2, multipliedY, colorE8);

                            x++;
                        }

                    }
                });
                return destination.UnlockAndReturnBitmap();
            }
        }

        /// <summary>
        ///     Checks if the colors are the same.
        /// </summary>
        /// <param name="aColor">Color first</param>
        /// <param name="bColor">Color second</param>
        /// <returns>True if they are; otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool AreColorsSame(byte* aColor, byte* bColor)
        {
            return aColor[0] == bColor[0] && aColor[1] == bColor[1] && aColor[2] == bColor[2] && aColor[3] == bColor[3];
        }
    }
}
