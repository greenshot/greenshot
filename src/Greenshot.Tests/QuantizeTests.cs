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

using System.Drawing;
using System.Drawing.Imaging;
using Greenshot.Gfx;
using Greenshot.Gfx.Quantizer;
using Xunit;

namespace Greenshot.Tests
{
    /// <summary>
    /// This tests if the Quantize works
    /// </summary>
    public class QuantizeTests
    {
        [Fact]
        public void Test_WuQuantizer()
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format24bppRgb, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap.NativeBitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                var quantizer = new WuQuantizer(bitmap);
                using (var quantizedImage = quantizer.GetQuantizedImage())
                {
                    quantizedImage.NativeBitmap.Save(@"quantized.png", ImageFormat.Png);
                }
            }
        }
    }
}
