// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
            using (var bitmapStitcher = new BitmapStitcher())
            {
                bitmapStitcher
                    .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll0.png"))
                    .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll35.png"))
                    .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll70.png"))
                    .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll105.png"))
                    .AddBitmap(BitmapHelper.LoadBitmap(@"TestFiles\scroll124.png"));

                using (var completedBitmap = bitmapStitcher.Result())
                {
                    completedBitmap.NativeBitmap.Save("scroll.png", ImageFormat.Png);
                }
            }
        }
    }
}
