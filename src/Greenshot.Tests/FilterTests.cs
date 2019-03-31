using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Greenshot.Gfx;
using Greenshot.Tests.Implementation;
using Xunit;
using Xunit.Abstractions;

namespace Greenshot.Tests
{
    public class FilterTests
    {
        public FilterTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void TestBlur()
        {
            using (var bitmap1 = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format24bppRgb, Color.White))
            using (var bitmap2 = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format24bppRgb, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmap1.NativeBitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                bitmap1.NativeBitmap.Save("bitmap0.png", ImageFormat.Png);
                bitmap1.ApplyBoxBlur(10);
                bitmap1.NativeBitmap.Save("bitmap1.png", ImageFormat.Png);

                using (var graphics = Graphics.FromImage(bitmap2.NativeBitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                BoxBlurOld.ApplyOldBoxBlur(bitmap2, 10);
                bitmap2.NativeBitmap.Save("bitmap2.png", ImageFormat.Png);
                Assert.True(bitmap1.IsEqualTo(bitmap2));
            }
        }
    }
}
