using System.Drawing;
using System.Drawing.Imaging;
using BenchmarkDotNet.Attributes;
using Greenshot.Gfx;
using Greenshot.Gfx.Experimental;
using Greenshot.Gfx.Quantizer;
using Greenshot.Tests.Implementation;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class GfxPerformance
    {
        [Benchmark]
        [Arguments(PixelFormat.Format24bppRgb)]
        [Arguments(PixelFormat.Format32bppRgb)]
        [Arguments(PixelFormat.Format32bppArgb)]
        public void WuQuantizer(PixelFormat pixelFormat)
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, pixelFormat, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                var quantizer = new WuQuantizer(bitmap);
                using (var quantizedImage = quantizer.GetQuantizedImage())
                {
                    quantizedImage.Save(@"quantized.png", ImageFormat.Png);
                }
            }
        }

        [Benchmark]
        [Arguments(PixelFormat.Format24bppRgb)]
        [Arguments(PixelFormat.Format32bppRgb)]
        [Arguments(PixelFormat.Format32bppArgb)]
        public void Blur(PixelFormat pixelFormat)
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, pixelFormat, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                bitmap.ApplyBoxBlur(10);
            }
        }

        [Benchmark]
        [Arguments(PixelFormat.Format24bppRgb)]
        [Arguments(PixelFormat.Format32bppRgb)]
        [Arguments(PixelFormat.Format32bppArgb)]
        public void BlurSpan(PixelFormat pixelFormat)
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, pixelFormat, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                bitmap.ApplyBoxBlurSpan(10);
            }
        }

        [Benchmark]
        [Arguments(PixelFormat.Format24bppRgb)]
        [Arguments(PixelFormat.Format32bppRgb)]
        [Arguments(PixelFormat.Format32bppArgb)]
        public void BlurOld(PixelFormat pixelFormat)
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, pixelFormat, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                bitmap.ApplyOldBoxBlur(10);
            }
        }

        [Benchmark]
        [Arguments(PixelFormat.Format24bppRgb)]
        [Arguments(PixelFormat.Format32bppRgb)]
        [Arguments(PixelFormat.Format32bppArgb)]
        public void Scale(PixelFormat pixelFormat)
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, pixelFormat, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                bitmap.Scale2X().Dispose();
            }
        }
    }
}
