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
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;

namespace Greenshot.Editor.Standalone.Rendering
{
    /// <summary>
    /// Greenshot's pixelation: blocks of jittered size with noise, so the pixelated area can't be turned back into the original
    /// by trying texts. The noise comes from AES with a key which only this process knows and the id of the element, so an
    /// element looks the same every time it is drawn while the editor runs, and nothing in a saved file gives the noise away.
    /// </summary>
    internal static class SecurePixelation
    {
        /// <summary>
        /// The pixelation as a processor, e.g. for the pixels a drawing canvas hands out for an area
        /// </summary>
        public static IImageProcessor Processor(int pixelSize, Guid elementId) => new PixelateProcessor(pixelSize, elementId);

        private sealed class PixelateProcessor : IImageProcessor
        {
            private readonly int _pixelSize;
            private readonly Guid _elementId;

            public PixelateProcessor(int pixelSize, Guid elementId)
            {
                _pixelSize = pixelSize;
                _elementId = elementId;
            }

            public IImageProcessor<TPixel> CreatePixelSpecificProcessor<TPixel>(Configuration configuration, Image<TPixel> source, Rectangle sourceRectangle)
                where TPixel : unmanaged, IPixel<TPixel> => new Pixelate<TPixel>(source, sourceRectangle, _pixelSize, _elementId);
        }

        private sealed class Pixelate<TPixel> : IImageProcessor<TPixel> where TPixel : unmanaged, IPixel<TPixel>
        {
            private readonly Image<TPixel> _source;
            private readonly Rectangle _area;
            private readonly int _pixelSize;
            private readonly Guid _elementId;

            public Pixelate(Image<TPixel> source, Rectangle area, int pixelSize, Guid elementId)
            {
                _source = source;
                _area = area;
                _pixelSize = pixelSize;
                _elementId = elementId;
            }

            public void Execute()
            {
                if (_source is Image<Rgba32> image)
                {
                    Apply(image, _area, _pixelSize, _elementId);
                    return;
                }

                // Another pixel type: pixelated as 8-bit, put back as it was
                using var copy = _source.CloneAs<Rgba32>();
                Apply(copy, _area, _pixelSize, _elementId);
                using var back = copy.CloneAs<TPixel>();
                _source.Mutate(context => context.DrawImage(back, new GraphicsOptions { AlphaCompositionMode = PixelAlphaCompositionMode.Src }));
            }

            public void Dispose()
            {
            }
        }

        // Never stored: another start of the editor draws other noise
        private static readonly byte[] ProcessSecret = RandomNumberGenerator.GetBytes(32);

        /// <summary>
        /// Pixelates the area of the image, which is the area of the element
        /// </summary>
        public static void Apply(Image<Rgba32> image, Rectangle area, int pixelSize, Guid elementId)
        {
            area.Intersect(image.Bounds);
            int width = area.Width, height = area.Height;
            if (pixelSize <= 1 || width <= 0 || height <= 0)
            {
                return;
            }

            pixelSize = Math.Min(pixelSize, Math.Min(width, height));
            using var aes = Aes.Create();
            aes.Key = HMACSHA256.HashData(ProcessSecret, elementId.ToByteArray());
            using var random = new CryptoRandom(aes, 4096);
            int jitter = Math.Max(1, pixelSize / 3);
            var yCoords = Steps(height, pixelSize, jitter, random);
            image.ProcessPixelRows(pixels =>
            {
                for (int i = 0; i < yCoords.Count - 1; i++)
                {
                    int yStart = area.Y + yCoords[i], yEnd = area.Y + yCoords[i + 1];
                    var xCoords = Steps(width, pixelSize, jitter, random);
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

        private static byte Clamp(int value) => (byte)Math.Clamp(value, 0, 255);

        /// <summary>
        /// Random numbers from AES encrypted counters
        /// </summary>
        private sealed class CryptoRandom : IDisposable
        {
            private readonly ICryptoTransform _encryptor;
            private readonly byte[] _counters, _buffer;
            private ulong _block;
            private int _index;

            public CryptoRandom(Aes aes, int size)
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                _counters = new byte[(size + 15) / 16 * 16];
                _buffer = new byte[_counters.Length];
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
                        for (int offset = 0; offset < _counters.Length; offset += 16)
                        {
                            BitConverter.TryWriteBytes(_counters.AsSpan(offset, 8), _block++);
                        }

                        _encryptor.TransformBlock(_counters, 0, _counters.Length, _buffer, 0);
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
