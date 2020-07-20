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

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Greenshot.Gfx;
using Greenshot.Gfx.Formats;
using Greenshot.Gfx.Quantizer;
using Greenshot.Gfx.Structs;
using Xunit;

namespace Greenshot.Tests
{
    /// <summary>
    /// This tests if the Quantize works
    /// </summary>
    public class QuantizeTests
    {
        public QuantizeTests()
        {
            BitmapHelper.RegisterFormatReader<GenericGdiFormatReader>();
        }

        [Fact]
        public void Test_WuQuantizer_128()
        {
            var bitmap = BitmapHelper.LoadBitmap(@"TestFiles\paiS7wOGKN0XA1wIaq8qHNoWQqq64wnFu3svA9Ux.jpeg");

            var quantizerOld = new WuQuantizerOld(bitmap);
            using var quantizedImageOld = quantizerOld.GetQuantizedImage(128);
            quantizedImageOld.NativeBitmap.Save(@"quantized1.png", ImageFormat.Png);

            var quantizer = new WuQuantizer<Bgra32>(bitmap);
            using var quantizedImage = quantizer.GetQuantizedImage(128);
            quantizedImage.NativeBitmap.Save(@"quantized2.png", ImageFormat.Png);

            Assert.Equal(quantizerOld.GetColorCount(), quantizer.GetColorCount());
            FileAssert.AreEqual("quantized1.png", "quantized2.png");
            File.Delete("quantized1.png");
            File.Delete("quantized2.png");
        }

        [Fact]
        public void Test_WuQuantizer()
        {
            using var bitmap = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format32bppArgb, Color.White);
            using (var graphics = Graphics.FromImage(bitmap.NativeBitmap))
            {
                using var pen = new SolidBrush(Color.Blue);
                graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
            }

            var quantizerOld = new WuQuantizerOld(bitmap);
            using var quantizedImageOld = quantizerOld.GetQuantizedImage();
            quantizedImageOld.NativeBitmap.Save(@"quantized1.png", ImageFormat.Png);

            var bitmap2 = new UnmanagedBitmap<Bgr32>(400, 400);
            bitmap2.Span.Fill(Color.White.FromColor());
            using (var graphics = Graphics.FromImage(bitmap2.NativeBitmap))
            {
                using var pen = new SolidBrush(Color.Blue);
                graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
            }

            var quantizer = new WuQuantizer<Bgra32>(bitmap2);
            using var quantizedImage = quantizer.GetQuantizedImage();
            quantizedImage.NativeBitmap.Save(@"quantized2.png", ImageFormat.Png);

            FileAssert.AreEqual("quantized1.png", "quantized2.png");
        }

    }
}
