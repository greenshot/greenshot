using System.Drawing;
using System.Drawing.Imaging;
using BenchmarkDotNet.Attributes;
using Greenshot.Gfx;
using Greenshot.Gfx.Experimental;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class GfxPerformanceShort
    {
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
    }
}
