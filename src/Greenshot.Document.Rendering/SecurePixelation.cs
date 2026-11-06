/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Document.Rendering
{
    /// <summary>
    /// The pixelation of the editor's PixelizationFilter: blocks of jittered size with noise from AES, so the pixelated
    /// area can't be turned back into the original by trying texts. The same algorithm, until both use one copy.
    /// </summary>
    internal static class SecurePixelation
    {
        public static void Apply(Image<Rgba32> image, Rectangle area, int pixelSize)
        {
            area.Intersect(image.Bounds());
            if (pixelSize <= 1 || area.Width == 0 || area.Height == 0)
            {
                return;
            }

            pixelSize = Math.Min(pixelSize, Math.Min(area.Width, area.Height));
            using var aes = Aes.Create();
            using var random = new CryptoRandom(aes, 4096);
            int jitter = Math.Max(1, pixelSize / 3);
            var yCoords = Steps(area.Height, pixelSize, jitter, random);
            image.ProcessPixelRows(pixels =>
            {
                for (int i = 0; i < yCoords.Count - 1; i++)
                {
                    int yStart = area.Y + yCoords[i], yEnd = area.Y + yCoords[i + 1];
                    var xCoords = Steps(area.Width, pixelSize, jitter, random);
                    for (int j = 0; j < xCoords.Count - 1; j++)
                    {
                        int xStart = area.X + xCoords[j], xEnd = area.X + xCoords[j + 1];
                        int sumA = 0, sumR = 0, sumG = 0, sumB = 0, count = 0;
                        int minR = 255, maxR = 0, minG = 255, maxG = 0, minB = 255, maxB = 0;
                        for (int y = yStart; y < yEnd; y++)
                        {
                            var row = pixels.GetRowSpan(y);
                            for (int x = xStart; x < xEnd; x++)
                            {
                                var c = row[x];
                                sumA += c.A;
                                sumR += c.R;
                                sumG += c.G;
                                sumB += c.B;
                                count++;
                                minR = Math.Min(minR, c.R);
                                maxR = Math.Max(maxR, c.R);
                                minG = Math.Min(minG, c.G);
                                maxG = Math.Max(maxG, c.G);
                                minB = Math.Min(minB, c.B);
                                maxB = Math.Max(maxB, c.B);
                            }
                        }

                        if (count == 0)
                        {
                            continue;
                        }

                        int averageA = sumA / count, averageR = sumR / count, averageG = sumG / count, averageB = sumB / count;
                        // A plain block gets no noise, the more the colours differ the more noise
                        double scale = Math.Min(1.0, Math.Max(maxR - minR, Math.Max(maxG - minG, maxB - minB)) / 32.0);
                        int blockRange = (int)Math.Round(12 * scale), pixelRange = (int)Math.Round(3 * scale);
                        int blockR = random.Next(blockRange), blockG = random.Next(blockRange), blockB = random.Next(blockRange);
                        for (int y = yStart; y < yEnd; y++)
                        {
                            var row = pixels.GetRowSpan(y);
                            for (int x = xStart; x < xEnd; x++)
                            {
                                row[x] = new Rgba32(Clamp(averageR + blockR + random.Next(pixelRange)), Clamp(averageG + blockG + random.Next(pixelRange)),
                                    Clamp(averageB + blockB + random.Next(pixelRange)), (byte)averageA);
                            }
                        }
                    }
                }
            });
        }

        private static List<int> Steps(int length, int pixelSize, int jitter, CryptoRandom random)
        {
            var steps = new List<int> { 0 };
            int current = 0;
            while (current < length)
            {
                current += Math.Max(2, pixelSize + random.Next(jitter));
                steps.Add(Math.Min(current, length));
            }

            return steps;
        }

        private static byte Clamp(int value) => (byte)Math.Max(0, Math.Min(255, value));

        /// <summary>
        /// Random numbers from AES encrypted zeros with a random key
        /// </summary>
        private sealed class CryptoRandom : IDisposable
        {
            private readonly ICryptoTransform _encryptor;
            private readonly byte[] _zeros, _buffer;
            private int _index;

            public CryptoRandom(Aes aes, int size)
            {
                aes.Padding = PaddingMode.None;
                _zeros = new byte[(size + 15) / 16 * 16];
                _buffer = new byte[_zeros.Length];
                _encryptor = aes.CreateEncryptor();
                _index = _buffer.Length;
            }

            /// <summary>
            /// A number from -range to range
            /// </summary>
            public int Next(int range)
            {
                if (range <= 0)
                {
                    return 0;
                }

                uint count = (uint)(2 * range + 1), limit = uint.MaxValue - uint.MaxValue % count, value;
                do
                {
                    if (_index + 4 > _buffer.Length)
                    {
                        _encryptor.TransformBlock(_zeros, 0, _zeros.Length, _buffer, 0);
                        _index = 0;
                    }

                    value = BitConverter.ToUInt32(_buffer, _index);
                    _index += 4;
                } while (value >= limit);

                return (int)(value % count) - range;
            }

            public void Dispose() => _encryptor.Dispose();
        }
    }
}
