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

using System.Drawing.Imaging;
using System.Numerics;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.ImageSharpDrawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The pixel copies which go row by row instead of through a buffer of the whole image
    /// </summary>
    public class FloatPixelsCopyTests
    {
        [Fact]
        public void Of_AnyPixelType_ReadsTheAreaAsItIs()
        {
            using var source = new Image<Rgba64>(5, 4);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    source[x, y] = new Rgba64((ushort)(x * 1000), (ushort)(y * 1000), 500, ushort.MaxValue);
                }
            }

            using var floats = FloatPixels.Of(source, new Rectangle(1, 2, 3, 2));

            Assert.Equal(3, floats.Width);
            Assert.Equal(2, floats.Height);
            Assert.Equal(source[1, 2].ToVector4(), floats[0, 0].ToVector4());
            Assert.Equal(source[3, 3].ToVector4(), floats[2, 1].ToVector4());
            // The source is unchanged
            Assert.Equal(new Rgba64(3000, 3000, 500, ushort.MaxValue), source[3, 3]);
        }

        [Fact]
        public void CopyArea_FromFloatPixels_KeepsHdrValues()
        {
            using var target = new Image<HalfVector4>(6, 4, new HalfVector4(0, 0, 0, 1));
            using var source = new Image<RgbaVector>(4, 3);
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    source[x, y] = new RgbaVector(x + 1, y + 2, 0.5f, 1);
                }
            }

            // The source shows the image from (1, 1) on; copy the image area (2, 2, 2, 2) = source (1, 1, 2, 2)
            ImageFilter.CopyArea(target, source, new Point(1, 1), new Rectangle(2, 2, 2, 2));

            Assert.Equal(new Vector4(2, 3, 0.5f, 1), target[2, 2].ToVector4());
            Assert.Equal(new Vector4(3, 4, 0.5f, 1), target[3, 3].ToVector4());
            Assert.Equal(new Vector4(0, 0, 0, 1), target[1, 1].ToVector4());
            Assert.Equal(new Vector4(0, 0, 0, 1), target[4, 2].ToVector4());
        }

        [Fact]
        public void CopyArea_FromOtherPixels_ConvertsTheArea()
        {
            using var target = new Image<RgbaVector>(4, 4, new RgbaVector(0, 0, 0, 1));
            using var source = new Image<Rgba32>(4, 4, new Rgba32(255, 0, 0, 255));
            source[2, 3] = new Rgba32(0, 255, 0, 255);

            ImageFilter.CopyArea(target, source, new Point(0, 0), new Rectangle(1, 2, 2, 2));

            Assert.Equal(new Vector4(1, 0, 0, 1), target[1, 2].ToVector4());
            Assert.Equal(new Vector4(0, 1, 0, 1), target[2, 3].ToVector4());
            Assert.Equal(new Vector4(0, 0, 0, 1), target[0, 2].ToVector4());
        }

        [Fact]
        public void ToBitmap_Rgba32_IsConvertedRowByRow()
        {
            using var image = new Image<Rgba32>(3, 2, new Rgba32(10, 20, 30, 40));
            image[2, 1] = new Rgba32(200, 100, 50, 255);

            using var bitmap = ImageSharpHelper.ToBitmap(image);

            Assert.Equal(PixelFormat.Format32bppArgb, bitmap.PixelFormat);
            Assert.Equal(System.Drawing.Color.FromArgb(40, 10, 20, 30), bitmap.GetPixel(0, 0));
            Assert.Equal(System.Drawing.Color.FromArgb(255, 200, 100, 50), bitmap.GetPixel(2, 1));
        }

        [Fact]
        public void UsePixels_ReadsArgbInPlaceAndConvertsOtherFormats()
        {
            using var argb = new System.Drawing.Bitmap(3, 2, PixelFormat.Format32bppArgb);
            argb.SetPixel(1, 1, System.Drawing.Color.FromArgb(128, 1, 2, 3));
            using var rgbSource = new System.Drawing.Bitmap(2, 2, PixelFormat.Format32bppRgb);
            rgbSource.SetPixel(1, 0, System.Drawing.Color.FromArgb(255, 9, 8, 7));

            Bgra32 inPlace = default;
            ImageSharpHelper.UsePixels(argb, image => inPlace = ((Image<Bgra32>)image)[1, 1]);
            Vector4 converted = default;
            ImageSharpHelper.UsePixels(rgbSource, image => converted = ((Image<Bgr24>)image)[1, 0].ToVector4());

            Assert.Equal(new Bgra32(1, 2, 3, 128), inPlace);
            Assert.Equal(new Bgr24(9, 8, 7).ToVector4(), converted);
        }
    }
}
