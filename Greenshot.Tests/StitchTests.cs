using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Greenshot.Gfx;
using Greenshot.Gfx.FastBitmap;
using Xunit;

namespace Greenshot.Tests
{
    public class StitchTests
    {
        [Fact]
        public void Stitch_DetectHeader()
        {
            var images = new List<Bitmap>
            {
                BitmapHelper.LoadBitmap(@"TestFiles\scroll0.png"),
                BitmapHelper.LoadBitmap(@"TestFiles\scroll35.png"),
                BitmapHelper.LoadBitmap(@"TestFiles\scroll70.png"),
                BitmapHelper.LoadBitmap(@"TestFiles\scroll105.png"),
                BitmapHelper.LoadBitmap(@"TestFiles\scroll124.png")
            };

            IList<IList<uint>> bitmapHashes = new List<IList<uint>>();
            foreach (var bitmap in images)
            {
                using (var fastBitmap = FastBitmapFactory.Create(bitmap))
                {
                    bitmapHashes.Add(Enumerable.Range(0, fastBitmap.Height).Select(i => fastBitmap.HorizontalHash(i)).ToList());
                }
            }

            int totalHeight = images.First().Height;
            // The header on the first bitmap is kept
            IList<int> headerLines = new List<int> {0};
            foreach (var hashes in bitmapHashes.Skip(1))
            {
                int y = 0;

                // Find header
                while (bitmapHashes[0].Count > y && bitmapHashes[0][y] == hashes[y])
                {
                    y++;
                }
                headerLines.Add(y);

                totalHeight += hashes.Count - y;
            }

            using (var completedBitmap = BitmapFactory.CreateEmpty(images[0].Width, totalHeight, images[0].PixelFormat))
            using (var graphics = Graphics.FromImage(completedBitmap))
            {
                int currentPosition = 0;
                foreach (var bitmapNr in Enumerable.Range(0, images.Count))
                {
                    var bitmap = images[bitmapNr];
                    var offset = headerLines[bitmapNr];
                    graphics.DrawImage(bitmap, new Rectangle(0, currentPosition, bitmap.Width, bitmap.Height - offset), new Rectangle(0, offset, bitmap.Width, bitmap.Height - offset), GraphicsUnit.Pixel);
                    currentPosition += bitmap.Height - offset;
                }
                completedBitmap.Save("scroll.png", ImageFormat.Png);
            }
        }
    }
}
