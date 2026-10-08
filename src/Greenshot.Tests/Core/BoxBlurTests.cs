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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class BoxBlurTests
    {
        /// <summary>
        /// ApplyBoxBlur must give exactly what the straightforward definition gives: horizontal, vertical, horizontal, vertical,
        /// every pixel the truncated average of the pixels within range / 2 which are inside the image.
        /// </summary>
        [Theory]
        [InlineData(PixelFormat.Format24bppRgb, 3)]
        [InlineData(PixelFormat.Format24bppRgb, 9)]
        [InlineData(PixelFormat.Format32bppRgb, 5)]
        [InlineData(PixelFormat.Format32bppArgb, 3)]
        [InlineData(PixelFormat.Format32bppArgb, 4)]
        [InlineData(PixelFormat.Format32bppArgb, 15)]
        [InlineData(PixelFormat.Format32bppArgb, 61)]
        public void ApplyBoxBlur_MatchesTheDefinition(PixelFormat pixelFormat, int range)
        {
            const int width = 37;
            const int height = 23;
            using var bitmap = new Bitmap(width, height, pixelFormat);
            int bytesPerPixel = Image.GetPixelFormatSize(pixelFormat) / 8;
            var random = new Random(range);
            var pixels = ReadPixels(bitmap, out int stride);
            random.NextBytes(pixels);
            WritePixels(bitmap, pixels);

            int channelCount = pixelFormat == PixelFormat.Format32bppArgb ? 4 : 3;
            int oddRange = (range & 1) == 0 ? range + 1 : range;
            var expected = (byte[])pixels.Clone();
            for (int pass = 0; pass < 2; pass++)
            {
                // Rows, then columns
                BlurByDefinition(expected, channelCount, oddRange, height, stride, width, bytesPerPixel);
                BlurByDefinition(expected, channelCount, oddRange, width, bytesPerPixel, height, stride);
            }

            ImageHelper.ApplyBoxBlur(bitmap, range);

            var actual = ReadPixels(bitmap, out _);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int c = 0; c < channelCount; c++)
                    {
                        int index = y * stride + x * bytesPerPixel + c;
                        Assert.True(expected[index] == actual[index], $"Channel {c} of pixel {x},{y} differs: expected {expected[index]}, got {actual[index]}");
                    }
                }
            }
        }

        /// <summary>
        /// One pass along lines (rows or columns): every pixel of a line becomes the average of its neighbours on that line
        /// </summary>
        private static void BlurByDefinition(byte[] pixels, int channelCount, int range, int lineCount, int lineStep, int length, int pixelStep)
        {
            int halfRange = range / 2;
            var original = (byte[])pixels.Clone();
            for (int line = 0; line < lineCount; line++)
            {
                for (int i = 0; i < length; i++)
                {
                    int from = Math.Max(0, i - halfRange);
                    int to = Math.Min(length - 1, i + halfRange);
                    for (int c = 0; c < channelCount; c++)
                    {
                        int sum = 0;
                        for (int j = from; j <= to; j++)
                        {
                            sum += original[line * lineStep + j * pixelStep + c];
                        }
                        pixels[line * lineStep + i * pixelStep + c] = (byte)(sum / (to - from + 1));
                    }
                }
            }
        }

        private static byte[] ReadPixels(Bitmap bitmap, out int stride)
        {
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            try
            {
                stride = data.Stride;
                var pixels = new byte[stride * bitmap.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                return pixels;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static void WritePixels(Bitmap bitmap, byte[] pixels)
        {
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            try
            {
                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
