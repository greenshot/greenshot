using System.Drawing;
using BenchmarkDotNet.Attributes;
using Greenshot.Gfx;

namespace Greenshot.Tests
{
    public class GfxPerformance
    {
        [Benchmark]
        public void Blur()
        {
            using (var bitmap = BitmapFactory.CreateEmpty(400, 400, backgroundColor:Color.White))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var pen = new SolidBrush(Color.Blue))
            {
                graphics.FillRectangle(pen, new Rectangle(30,30, 340, 340));
                bitmap.ApplyBoxBlur(10);
            }
        }
    }
}
