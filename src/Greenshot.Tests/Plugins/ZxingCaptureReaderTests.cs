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

using System.Drawing;
using System.Linq;
using Greenshot.Plugin.Zxing.Processing;
using Xunit;
using ZXing;
using ZXing.QrCode;

namespace Greenshot.Tests.Plugins
{
    /// <summary>
    /// The scan of every capture reads without AutoRotate (it costs a rotated copy of the capture per extra pass):
    /// QR codes in any orientation are still found
    /// </summary>
    public class ZxingCaptureReaderTests
    {
        private const string Text = "https://getgreenshot.org/";

        /// <summary>
        /// A screen-like bitmap with a QR code somewhere in it
        /// </summary>
        private static Bitmap CreateCapture(RotateFlipType rotation, bool inverted)
        {
            var matrix = new QRCodeWriter().encode(Text, BarcodeFormat.QR_CODE, 0, 0);
            const int scale = 6;
            using var code = new Bitmap(matrix.Width * scale, matrix.Height * scale);
            using (var graphics = Graphics.FromImage(code))
            using (var dark = new SolidBrush(inverted ? Color.White : Color.Black))
            {
                graphics.Clear(inverted ? Color.FromArgb(30, 30, 30) : Color.White);
                for (int y = 0; y < matrix.Height; y++)
                {
                    for (int x = 0; x < matrix.Width; x++)
                    {
                        if (matrix[x, y])
                        {
                            graphics.FillRectangle(dark, x * scale, y * scale, scale, scale);
                        }
                    }
                }
            }

            code.RotateFlip(rotation);
            var capture = new Bitmap(1280, 720);
            using (var graphics = Graphics.FromImage(capture))
            {
                graphics.Clear(inverted ? Color.FromArgb(30, 30, 30) : Color.FromArgb(240, 240, 240));
                graphics.DrawImage(code, 700, 150, code.Width, code.Height);
            }

            return capture;
        }

        [Theory]
        [InlineData(RotateFlipType.RotateNoneFlipNone, false)]
        [InlineData(RotateFlipType.Rotate90FlipNone, false)]
        [InlineData(RotateFlipType.Rotate180FlipNone, false)]
        [InlineData(RotateFlipType.Rotate270FlipNone, false)]
        // Light on dark (inverted) isn't found: ZXing.Net 0.16 ignores TryInverted in DecodeMultiple, with or without AutoRotate
        public void CaptureReader_FindsTheCode(RotateFlipType rotation, bool inverted)
        {
            using var capture = CreateCapture(rotation, inverted);

            var results = ZxingCaptureProcessor.CreateCaptureReader().DecodeMultiple(capture);

            Assert.NotNull(results);
            Assert.Contains(results, r => r.Text == Text && r.BarcodeFormat == BarcodeFormat.QR_CODE);
        }

        [Fact]
        public void CaptureReader_DoesNotRotate()
        {
            Assert.False(ZxingCaptureProcessor.CreateCaptureReader().AutoRotate);
        }

        [Fact]
        public void CaptureReader_NoCode_NoResult()
        {
            using var capture = new Bitmap(1280, 720);
            using (var graphics = Graphics.FromImage(capture))
            {
                graphics.Clear(Color.White);
                graphics.FillRectangle(Brushes.DarkBlue, 100, 100, 400, 200);
            }

            var results = ZxingCaptureProcessor.CreateCaptureReader().DecodeMultiple(capture);

            Assert.True(results == null || !results.Any());
        }
    }
}
