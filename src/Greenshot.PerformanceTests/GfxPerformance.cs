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
using BenchmarkDotNet.Attributes;
using Greenshot.Gfx;
using Greenshot.Gfx.Quantizer;
using Greenshot.Gfx.Structs;
using Greenshot.Tests.Implementation;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class GfxPerformance
    {
        private UnmanagedBitmap<Bgr32> _unmanagedTestBitmap;

        [GlobalSetup]
        public void CreateTestImage()
        {
            _unmanagedTestBitmap = new UnmanagedBitmap<Bgr32>(400, 400);
            _unmanagedTestBitmap.Span.Fill(new Bgr32 { B = 255, G = 255, R = 255, Unused = 0});
            using var graphics = Graphics.FromImage(_unmanagedTestBitmap.NativeBitmap);
            using var pen = new SolidBrush(Color.Blue);
            graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
        }

        [GlobalCleanup]
        public void Dispose()
        {
            _unmanagedTestBitmap.Dispose();
        }


        [Benchmark]
        //[Arguments(PixelFormat.Format24bppRgb)]
        [Arguments(PixelFormat.Format32bppRgb, 256)]
        [Arguments(PixelFormat.Format32bppRgb, 128)]
        //[Arguments(PixelFormat.Format32bppArgb)]
        public void WuQuantizer(PixelFormat pixelFormat, int maxColors)
        {
            var bitmap = new UnmanagedBitmap<Bgr32>(400, 400);
            bitmap.Span.Fill(Color.White.FromColor());
            using (var graphics = Graphics.FromImage(bitmap.NativeBitmap))
            {
                using var pen = new SolidBrush(Color.Blue);
                graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
            }

            var quantizer = new WuQuantizer<Bgra32>(bitmap);
            using var quantizedImage = quantizer.GetQuantizedImage(maxColors);
            quantizedImage.NativeBitmap.Save(@"quantized.png", ImageFormat.Png);
        }

        [Benchmark]
        //[Arguments(PixelFormat.Format24bppRgb)]
        [Arguments(PixelFormat.Format32bppRgb, 256)]
        [Arguments(PixelFormat.Format32bppRgb, 128)]
        //[Arguments(PixelFormat.Format32bppArgb)]
        public void WuQuantizerOld(PixelFormat pixelFormat, int maxColors)
        {
            using var bitmap = BitmapFactory.CreateEmpty(400, 400, pixelFormat, Color.White);
            using (var graphics = Graphics.FromImage(bitmap.NativeBitmap))
            {
                using var pen = new SolidBrush(Color.Blue);
                graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
            }

            var quantizer = new WuQuantizerOld(bitmap);
            using var quantizedImage = quantizer.GetQuantizedImage(maxColors);
            quantizedImage.NativeBitmap.Save(@"quantized.png", ImageFormat.Png);
        }

        //[Benchmark]
        [Arguments(PixelFormat.Format24bppRgb)]
        [Arguments(PixelFormat.Format32bppRgb)]
        [Arguments(PixelFormat.Format32bppArgb)]
        public void Blur_FastBitmap(PixelFormat pixelFormat)
        {
            using var bitmap = BitmapFactory.CreateEmpty(400, 400, pixelFormat, Color.White);
            using (var graphics = Graphics.FromImage(bitmap.NativeBitmap))
            {
                using var pen = new SolidBrush(Color.Blue);
                graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
            }

            bitmap.ApplyBoxBlur(10);
        }

        //[Benchmark]
        public void Blur_UnmanagedBitmap()
        {
            using var unmanagedBitmap = new UnmanagedBitmap<Bgr32>(400, 400);
            unmanagedBitmap.Span.Fill(new Bgr32 { B = 255, G = 255, R = 255 });
            using (var graphics = Graphics.FromImage(unmanagedBitmap.NativeBitmap))
            {
                using var pen = new SolidBrush(Color.Blue);
                graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
            }

            unmanagedBitmap.ApplyBoxBlur(10);
        }

        //[Benchmark]
        public void Blur_Old()
        {
            using var bitmap = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format32bppRgb, Color.White);
            using (var graphics = Graphics.FromImage(bitmap.NativeBitmap))
            {
                using var pen = new SolidBrush(Color.Blue);
                graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
            }

            BoxBlurOld.ApplyOldBoxBlur(bitmap, 10);
        }

        //[Benchmark]
        public void Scale2x_FastBitmap()
        {
            ScaleX.Scale2X(_unmanagedTestBitmap).Dispose();
        }

        //[Benchmark]
        public void Scale2x_Unmanaged()
        {
            _unmanagedTestBitmap.Scale2X().Dispose();
        }

        //[Benchmark]
        public void Scale2x_Unmanaged_Reference()
        {
            _unmanagedTestBitmap.Scale2XReference().Dispose();
        }

        //[Benchmark]
        public void Scale3x_FastBitmap()
        {
            ScaleX.Scale3X(_unmanagedTestBitmap).Dispose();
        }

        //[Benchmark]
        public void Scale3x_Unmanaged()
        {
            _unmanagedTestBitmap.Scale3X().Dispose();
        }

        //[Benchmark]
        public void Scale3x_Unmanaged_Reference()
        {
            _unmanagedTestBitmap.Scale3XReference().Dispose();
        }
    }
}
