using System.Drawing;
using System.Drawing.Imaging;
using BenchmarkDotNet.Attributes;
using Greenshot.Gfx;
using Greenshot.Gfx.Experimental;
using Greenshot.Gfx.Quantizer;
using Greenshot.Tests.Implementation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class GfxPerformance
    {
        [GlobalSetup]
        public void Setup()
        {
            BoxBlurImageSharp();
        }

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
        public void BoxBlurImageSharp()
        {
            var color = NamedColors<Rgb24>.Blue;
            var solidBlueBrush = SixLabors.ImageSharp.Processing.Brushes.Solid(color);
            var graphicsOptions = new GraphicsOptions(false);
            using (var image = new Image<Rgb24>(SixLabors.ImageSharp.Configuration.Default, 400, 400, NamedColors<Rgb24>.White))
            {
                image.Mutate(c => c
                .Fill(new GraphicsOptions(false), solidBlueBrush, new SixLabors.Primitives.Rectangle(30, 30, 340, 340))
                .BoxBlur(10));
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
