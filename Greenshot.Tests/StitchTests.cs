using System.Drawing.Imaging;
using Greenshot.Gfx;
using Greenshot.Gfx.Stitching;
using Xunit;

namespace Greenshot.Tests
{
    public class StitchTests
    {
        [Fact]
        public void BitmapStitcher_Default()
        {
            var bitmapStitcher = new BitmapStitcher();

            bitmapStitcher
                .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll0.png"))
                .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll35.png"))
                .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll70.png"))
                .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll105.png"))
                .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll124.png"));

            using (var completedBitmap = bitmapStitcher.Result())
            {
                completedBitmap.Save("scroll.png", ImageFormat.Png);
            }
        }
    }
}
