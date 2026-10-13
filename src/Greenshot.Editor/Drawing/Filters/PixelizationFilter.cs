/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Imaging;
using System.Security.Cryptography;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using SixLabors.ImageSharp.PixelFormats;

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

        // Secret key and IV for the noise, generated once per filter and never stored, so every repaint and the export use the same noise
        [NonSerialized] private byte[] _noiseKey;
        [NonSerialized] private byte[] _noiseIv;

        /// <summary>
        /// Cryptographically secure random numbers (the AES-CBC encrypted zeros), the same key and IV give the same numbers
        /// </summary>
        private class CryptoRandomBuffer : IDisposable
        {
            private readonly Aes _aes;
            private readonly ICryptoTransform _encryptor;
            private readonly byte[] _zeros;
            private readonly byte[] _buffer;
            private int _index;

            public CryptoRandomBuffer(byte[] key, byte[] iv, int size)
            {
                // Ensure size is a multiple of the AES block size
                int alignedSize = ((size + 15) / 16) * 16;
                _buffer = new byte[alignedSize];
                _zeros = new byte[alignedSize];
                _aes = Aes.Create();
                _aes.Padding = PaddingMode.None;
                _aes.Key = key;
                _aes.IV = iv;
                _encryptor = _aes.CreateEncryptor();
                Refill();
            }

            private void Refill()
            {
                // The encryptor keeps chaining, so every refill gives new numbers
                _encryptor.TransformBlock(_zeros, 0, _zeros.Length, _buffer, 0);
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
                _encryptor.Dispose();
                _aes.Dispose();
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
            if (_noiseKey == null)
            {
                // Aes.Create generates a random key and IV
                using var aes = Aes.Create();
                _noiseKey = aes.Key;
                _noiseIv = aes.IV;
            }

            using CryptoRandomBuffer cryptoRandom = new CryptoRandomBuffer(_noiseKey, _noiseIv, 4096);

            // The blocks don't overlap, so every block is read and written in place
            using Bitmap pixelated = ImageHelper.CloneArea(applyBitmap, applyRect, PixelFormat.Format32bppArgb);
            BitmapPixels.ProcessPixelRows<Bgra32>(pixelated, pixels =>
            {
                int jitter = Math.Max(1, pixelSize / 3);

                // Generate randomized row boundaries (Y coordinates)
                List<int> yCoords = new List<int>();
                yCoords.Add(0);
                int currentY = 0;
                while (currentY < pixels.Height)
                {
                    int nextStep = pixelSize + cryptoRandom.GetNextInt(-jitter, jitter);
                    if (nextStep < 2) nextStep = 2;
                    currentY += nextStep;
                    if (currentY >= pixels.Height)
                    {
                        yCoords.Add(pixels.Height);
                        break;
                    }
                    yCoords.Add(currentY);
                }
                if (yCoords[yCoords.Count - 1] < pixels.Height)
                {
                    yCoords.Add(pixels.Height);
                }

                // Pre-allocate xCoords list to avoid allocation inside the loop
                List<int> xCoords = new List<int>();

                for (int i = 0; i < yCoords.Count - 1; i++)
                {
                    int yStart = yCoords[i];
                    int yEnd = yCoords[i + 1];

                    // Generate randomized column boundaries (X coordinates) independently for each row
                    xCoords.Clear();
                    xCoords.Add(0);
                    int currentX = 0;
                    while (currentX < pixels.Width)
                    {
                        int nextStep = pixelSize + cryptoRandom.GetNextInt(-jitter, jitter);
                        if (nextStep < 2) nextStep = 2;
                        currentX += nextStep;
                        if (currentX >= pixels.Width)
                        {
                            xCoords.Add(pixels.Width);
                            break;
                        }
                        xCoords.Add(currentX);
                    }
                    if (xCoords[xCoords.Count - 1] < pixels.Width)
                    {
                        xCoords.Add(pixels.Width);
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
                            var row = pixels.GetRowSpan(yy);
                            for (int xx = xStart; xx < xEnd; xx++)
                            {
                                Bgra32 c = row[xx];
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

                        if (count == 0)
                        {
                            continue;
                        }

                        int averageA = sumA / count, averageR = sumR / count, averageG = sumG / count, averageB = sumB / count;

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
                            var row = pixels.GetRowSpan(yy);
                            for (int xx = xStart; xx < xEnd; xx++)
                            {
                                int pixelR = pixelNoiseRange > 0 ? cryptoRandom.GetNextInt(-pixelNoiseRange, pixelNoiseRange) : 0;
                                int pixelG = pixelNoiseRange > 0 ? cryptoRandom.GetNextInt(-pixelNoiseRange, pixelNoiseRange) : 0;
                                int pixelB = pixelNoiseRange > 0 ? cryptoRandom.GetNextInt(-pixelNoiseRange, pixelNoiseRange) : 0;

                                byte r = ClampToByte(averageR + blockR + pixelR);
                                byte g = ClampToByte(averageG + blockG + pixelG);
                                byte b = ClampToByte(averageB + blockB + pixelB);

                                row[xx] = new Bgra32(r, g, b, (byte)averageA);
                            }
                        }
                    }
                }
            });

            graphics.DrawImage(pixelated, applyRect, new Rectangle(0, 0, pixelated.Width, pixelated.Height), GraphicsUnit.Pixel);
        }

        public override void Apply(Graphics graphics, Bitmap applyBitmap, IEnumerable<NativeRect> rects, RenderMode renderMode)
        {
            if (rects == null)
            {
                return;
            }

            foreach (var r in rects)
            {
                Apply(graphics, applyBitmap, r, renderMode);
            }
        }
    }
}