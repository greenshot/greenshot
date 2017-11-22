using System.Drawing;
using System.Drawing.Imaging;
using Greenshot.Gfx;
using Greenshot.Gfx.Quantizer;
using Greenshot.Tests.Implementation;
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
    }
}
