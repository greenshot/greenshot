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

using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Greenshot.Gfx;
using Greenshot.Gfx.Structs;
using Xunit;
using Xunit.Abstractions;

namespace Greenshot.Tests
{
    /// <summary>
    /// This tests if the new blur works
    /// </summary>
    public class ScaleXTests
    {
        public ScaleXTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void Test_Scale2X_UnmanagedBitmap()
        {
            using (var bitmapNew = new UnmanagedBitmap<Bgr32>(400, 400))
            using (var bitmapOld = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format32bppRgb, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmapOld.NativeBitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                bitmapNew.Span.Fill(new Bgr32 { B = 255, G = 255, R = 255, Unused = 0 });
                using (var bitmap = bitmapNew.NativeBitmap)
                using (var graphics = Graphics.FromImage(bitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                    using (var scaledUnmanagedBitmap = bitmapNew.Scale2X())
                    using (var scaledBitmap = ScaleX.Scale2X(bitmapOld))
                    {
                        scaledUnmanagedBitmap.NativeBitmap.Save(@"new2x.png", ImageFormat.Png);
                        scaledBitmap.NativeBitmap.Save(@"old2x.png", ImageFormat.Png);
                        Assert.True(scaledBitmap.IsEqualTo(scaledUnmanagedBitmap), "New Scale2X doesn't compare to old.");
                    }
                }
            }
        }

        [Fact]
        public void Test_Scale3X_UnmanagedBitmap()
        {
            using (var bitmapNew = new UnmanagedBitmap<Bgr32>(400, 400))
            using (var bitmapOld = BitmapFactory.CreateEmpty(400, 400, PixelFormat.Format32bppRgb, Color.White))
            {
                using (var graphics = Graphics.FromImage(bitmapOld.NativeBitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                }
                bitmapNew.Span.Fill(new Bgr32 { B = 255, G = 255, R = 255, Unused = 0 });
                using (var bitmap = bitmapNew.NativeBitmap)
                using (var graphics = Graphics.FromImage(bitmap))
                using (var pen = new SolidBrush(Color.Blue))
                {
                    graphics.FillRectangle(pen, new Rectangle(30, 30, 340, 340));
                    using (var scaledUnmanagedBitmap = bitmapNew.Scale3X())
                    using (var scaledBitmap = ScaleX.Scale2X(bitmapOld))
                    {
                        scaledUnmanagedBitmap.NativeBitmap.Save(@"new3x.png", ImageFormat.Png);
                        scaledBitmap.NativeBitmap.Save(@"old3x.png", ImageFormat.Png);
                        Assert.True(scaledBitmap.IsEqualTo(scaledUnmanagedBitmap), "New Scale3X doesn't compare to old.");
                    }
                }
            }
        }
    }
}
