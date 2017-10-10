using System.Drawing;
using System.Drawing.Imaging;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Diagnosers;
using Greenshot.Gfx;
using Greenshot.Gfx.Quantizer;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class GfxPerformance
    {
        //[Benchmark]
        public void WuQuantizer()
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format24bppRgb, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                var quantizer = new WuQuantizer(bitmap);
                quantizer.GetQuantizedImage().Dispose();
            }
        }

        //[Benchmark]
        public void Blur()
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format24bppRgb, Color.White))
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
        public void Scale()
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format24bppRgb, Color.White))
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
