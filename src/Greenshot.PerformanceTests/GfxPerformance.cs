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
            using (var bitmap = _unmanagedTestBitmap.NativeBitmap)
            using (var graphics = Graphics.FromImage(bitmap))
            using (var pen = new SolidBrush(Color.Blue))
            {
                graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
            }
        }

        [GlobalCleanup]
        public void Dispose()
        {
            _unmanagedTestBitmap.Dispose();
        }


        //[Benchmark]
        [Arguments(PixelFormat.Format24bppRgb)]
        [Arguments(PixelFormat.Format32bppRgb)]
        [Arguments(PixelFormat.Format32bppArgb)]
        public void WuQuantizer(PixelFormat pixelFormat)
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, pixelFormat, Color.White))
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

        //[Benchmark]
        public void Blur_FastBitmap()
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format32bppRgb, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap.NativeBitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                bitmap.ApplyBoxBlur(10);
            }
        }

        //[Benchmark]
        public void Blur_UnmanagedBitmap()
        {
            using (var unmanagedBitmap = new UnmanagedBitmap<Bgr32>(400, 400))
            {
                unmanagedBitmap.Span.Fill(new Bgr32 { B = 255, G = 255, R = 255 });
                using (var bitmap = unmanagedBitmap.NativeBitmap)
                using (var graphics = Graphics.FromImage(bitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }

                unmanagedBitmap.ApplyBoxBlur(10);
            }
        }


        //[Benchmark]
        public void Blur_Old()
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format32bppRgb, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap.NativeBitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                BoxBlurOld.ApplyOldBoxBlur(bitmap, 10);
            }
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

        [Benchmark]
        public void Scale3x_FastBitmap()
        {
            ScaleX.Scale3X(_unmanagedTestBitmap).Dispose();
        }

        [Benchmark]
        public void Scale3x_Unmanaged()
        {
            _unmanagedTestBitmap.Scale3X().Dispose();
        }

        [Benchmark]
        public void Scale3x_Unmanaged_Reference()
        {
            _unmanagedTestBitmap.Scale3XReference().Dispose();
        }
    }
}
