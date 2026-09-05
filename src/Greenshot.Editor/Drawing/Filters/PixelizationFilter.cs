/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;

namespace Greenshot.Editor.Drawing.Filters
{
    /// <summary>
    /// Pixelate an area
    /// </summary>
    [Serializable()]
    public class PixelizationFilter : AbstractFilter
    {
        public PixelizationFilter(DrawableContainer parent) : base(parent)
        {
            AddField(GetType(), FieldType.PIXEL_SIZE, 5);
        }

        private class CryptoRandomBuffer : IDisposable
        {
            private readonly RandomNumberGenerator _rng;
            private readonly byte[] _buffer;
            private int _index;

            public CryptoRandomBuffer(int size)
            {
                // Ensure size is a multiple of 4
                int alignedSize = ((size + 3) / 4) * 4;
                _buffer = new byte[alignedSize];
                _rng = RandomNumberGenerator.Create();
                Refill();
            }

            private void Refill()
            {
                _rng.GetBytes(_buffer);
                _index = 0;
            }

            private uint GetNextUInt32()
            {
                if (_index + 4 > _buffer.Length)
                {
                    Refill();
                }
                uint val = BitConverter.ToUInt32(_buffer, _index);
                _index += 4;
                return val;
            }

            public int GetNextInt(int min, int max)
            {
                uint range = (uint)(max - min + 1);
                if (range <= 1) return min;

                uint limit = uint.MaxValue - (uint.MaxValue % range);
                uint val;
                do
                {
                    val = GetNextUInt32();
                } while (val >= limit);

                return min + (int)(val % range);
            }

            public void Dispose()
            {
                _rng.Dispose();
            }
        }

        private static byte ClampToByte(int value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (byte)value;
        }

        public override void Apply(Graphics graphics, Bitmap applyBitmap, NativeRect rect, RenderMode renderMode)
        {
            int pixelSize = GetFieldValueAsInt(FieldType.PIXEL_SIZE);
            var applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);
            if (pixelSize <= 1 || applyRect.Width == 0 || applyRect.Height == 0)
            {
                // Nothing to do
                return;
            }

            if (applyRect.Width < pixelSize)
            {
                pixelSize = applyRect.Width;
            }

            if (applyRect.Height < pixelSize)
            {
                pixelSize = applyRect.Height;
            }

            // Secure randomized pixelation
            // Create a small 4KB cryptographically secure random buffer
            using CryptoRandomBuffer cryptoRandom = new CryptoRandomBuffer(4096);

            using IFastBitmap dest = FastBitmap.CreateCloneOf(applyBitmap, applyRect);
            using (IFastBitmap src = FastBitmap.Create(applyBitmap, applyRect))
            {
                int jitter = Math.Max(1, pixelSize / 3);

                // Generate randomized row boundaries (Y coordinates)
                List<int> yCoords = new List<int>();
                yCoords.Add(src.Top);
                int currentY = src.Top;
                while (currentY < src.Bottom)
                {
                    int nextStep = pixelSize + cryptoRandom.GetNextInt(-jitter, jitter);
                    if (nextStep < 2) nextStep = 2;
                    currentY += nextStep;
                    if (currentY >= src.Bottom)
                    {
                        yCoords.Add(src.Bottom);
                        break;
                    }
                    yCoords.Add(currentY);
                }
                if (yCoords[yCoords.Count - 1] < src.Bottom)
                {
                    yCoords.Add(src.Bottom);
                }

                // Pre-allocate xCoords list to avoid allocation inside the loop
                List<int> xCoords = new List<int>();

                for (int i = 0; i < yCoords.Count - 1; i++)
                {
                    int yStart = yCoords[i];
                    int yEnd = yCoords[i + 1];

                    // Generate randomized column boundaries (X coordinates) independently for each row
                    xCoords.Clear();
                    xCoords.Add(src.Left);
                    int currentX = src.Left;
                    while (currentX < src.Right)
                    {
                        int nextStep = pixelSize + cryptoRandom.GetNextInt(-jitter, jitter);
                        if (nextStep < 2) nextStep = 2;
                        currentX += nextStep;
                        if (currentX >= src.Right)
                        {
                            xCoords.Add(src.Right);
                            break;
                        }
                        xCoords.Add(currentX);
                    }
                    if (xCoords[xCoords.Count - 1] < src.Right)
                    {
                        xCoords.Add(src.Right);
                    }

                    for (int j = 0; j < xCoords.Count - 1; j++)
                    {
                        int xStart = xCoords[j];
                        int xEnd = xCoords[j + 1];

                        // Gather colors in this block to compute average and check variation directly (no list allocations)
                        int sumA = 0, sumR = 0, sumG = 0, sumB = 0;
                        int count = 0;
                        int minR = 255, maxR = 0;
                        int minG = 255, maxG = 0;
                        int minB = 255, maxB = 0;

                        for (int yy = yStart; yy < yEnd; yy++)
                        {
                            for (int xx = xStart; xx < xEnd; xx++)
                            {
                                Color c = src.GetColorAt(xx, yy);
                                if (!c.Equals(Color.Empty))
                                {
                                    sumA += c.A;
                                    sumR += c.R;
                                    sumG += c.G;
                                    sumB += c.B;
                                    count++;

                                    if (c.R < minR) minR = c.R;
                                    if (c.R > maxR) maxR = c.R;
                                    if (c.G < minG) minG = c.G;
                                    if (c.G > maxG) maxG = c.G;
                                    if (c.B < minB) minB = c.B;
                                    if (c.B > maxB) maxB = c.B;
                                }
                            }
                        }

                        if (count == 0)
                        {
                            continue;
                        }

                        Color currentAvgColor = Color.FromArgb(sumA / count, sumR / count, sumG / count, sumB / count);

                        int diffR = maxR - minR;
                        int diffG = maxG - minG;
                        int diffB = maxB - minB;
                        int maxDiff = Math.Max(diffR, Math.Max(diffG, diffB));

                        // Scale noise based on color variation in the block (maxDiff).
                        // Solid colors (maxDiff == 0) will have scale = 0, meaning absolutely 0 noise.
                        double scale = Math.Min(1.0, maxDiff / 32.0);

                        // Generate block-level random color offset: [-12, 12] scaled
                        int blockNoiseRange = (int)Math.Round(12 * scale);
                        int blockR = blockNoiseRange > 0 ? cryptoRandom.GetNextInt(-blockNoiseRange, blockNoiseRange) : 0;
                        int blockG = blockNoiseRange > 0 ? cryptoRandom.GetNextInt(-blockNoiseRange, blockNoiseRange) : 0;
                        int blockB = blockNoiseRange > 0 ? cryptoRandom.GetNextInt(-blockNoiseRange, blockNoiseRange) : 0;

                        // Generate pixel-level noise range: [-3, 3] scaled
                        int pixelNoiseRange = (int)Math.Round(3 * scale);

                        for (int yy = yStart; yy < yEnd; yy++)
                        {
                            for (int xx = xStart; xx < xEnd; xx++)
                            {
                                int pixelR = pixelNoiseRange > 0 ? cryptoRandom.GetNextInt(-pixelNoiseRange, pixelNoiseRange) : 0;
                                int pixelG = pixelNoiseRange > 0 ? cryptoRandom.GetNextInt(-pixelNoiseRange, pixelNoiseRange) : 0;
                                int pixelB = pixelNoiseRange > 0 ? cryptoRandom.GetNextInt(-pixelNoiseRange, pixelNoiseRange) : 0;

                                byte r = ClampToByte(currentAvgColor.R + blockR + pixelR);
                                byte g = ClampToByte(currentAvgColor.G + blockG + pixelG);
                                byte b = ClampToByte(currentAvgColor.B + blockB + pixelB);

                                dest.SetColorAt(xx, yy, Color.FromArgb(currentAvgColor.A, r, g, b));
                            }
                        }
                    }
                }
            }

            dest.DrawTo(graphics, applyRect.Location);
        }
    }
}