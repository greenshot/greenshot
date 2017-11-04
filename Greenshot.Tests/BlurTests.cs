using System.Drawing;
using System.Drawing.Imaging;
using BenchmarkDotNet.Attributes;
using Greenshot.Gfx;
using Greenshot.Tests.Implementation;
using Xunit;

namespace Greenshot.Tests
{
    /// <summary>
    /// This tests if the new blur works
    /// </summary>
    public class BlurTests
    {
        [Fact]
        public void Test_Blur()
        {
            using (var bitmapNew = BitmapFactory.CreateEmpty(400, 400, backgroundColor: Color.White))
            using (var bitmapOld = BitmapFactory.CreateEmpty(400, 400, backgroundColor: Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmapNew))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                    bitmapNew.ApplyBoxBlur(10);
                }
                using (var graphics = Graphics.FromImage(bitmapOld))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                    bitmapOld.ApplyOldBoxBlur(10);
                }
                bitmapOld.Save(@"old.png", ImageFormat.Png);
                bitmapNew.Save(@"new.png", ImageFormat.Png);

                Assert.True(bitmapOld.IsEqualTo(bitmapNew));
            }
        }
    }
}
