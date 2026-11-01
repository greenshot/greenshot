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
using System.IO;
using Greenshot.Base.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Color = System.Drawing.Color;

namespace Greenshot.Tests.Core
{
    public class PngOptimizerTests
    {
        private const int Size = 64;

        public static TheoryData<string, PngColorType> Cases => new()
        {
            // 200 colors which differ by one step, the ImageSharp 2.x quantizer cache would mix them up
            { "nearColors", PngColorType.Palette },
            { "fewColorsWithTransparency", PngColorType.Palette },
            // A gray image has at most 256 colors, so it gets a palette too
            { "gray", PngColorType.Palette },
            { "rgb", PngColorType.Rgb },
            { "rgbWithAlpha", PngColorType.RgbWithAlpha },
        };

        [Theory]
        [MemberData(nameof(Cases))]
        public void Save_KeepsEveryPixel_WithSmallestColorType(string kind, PngColorType expectedColorType)
        {
            bool alpha = kind is "fewColorsWithTransparency" or "rgbWithAlpha";
            using var bitmap = new Bitmap(Size, Size, alpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    bitmap.SetPixel(x, y, kind switch
                    {
                        "nearColors" => Color.FromArgb(100 + i % 200 / 10, 50 + i % 200 % 10, 7),
                        "fewColorsWithTransparency" => i % 3 == 0 ? Color.FromArgb(0, 0, 0, 0) : Color.FromArgb(128, 10, 20, 30),
                        "gray" => Color.FromArgb(i % 256, i % 256, i % 256),
                        "rgb" => Color.FromArgb(i % 256, i / 256, 3),
                        _ => Color.FromArgb(255 - i % 5, i % 256, i / 256, 3),
                    });
                }
            }

            using var stream = new MemoryStream();
            PngOptimizer.Save(bitmap, stream);

            stream.Position = 0;
            using var written = SixLabors.ImageSharp.Image.Load<Bgra32>(stream);
            Assert.Equal(expectedColorType, written.Metadata.GetPngMetadata().ColorType);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var expected = bitmap.GetPixel(x, y);
                    var actual = written[x, y];
                    Assert.Equal(expected.ToArgb(), Color.FromArgb(actual.A, actual.R, actual.G, actual.B).ToArgb());
                }
            }
        }
    }
}
