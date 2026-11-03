/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Effects;
using Greenshot.Base.Threading;
using Greenshot.Editor.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The editor keeps the HDR image of a capture with it: a crop or a rotation is done to it too (also undone), other changes of the image
    /// leave it behind, so the capture is saved without HDR then
    /// </summary>
    public class SurfaceHdrTests
    {
        public SurfaceHdrTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// A capture of 4x2 pixels with an HDR image whose red is the x and green the y of the pixel
        /// </summary>
        private static Surface CreateSurface()
        {
            var capture = new Capture(new Bitmap(4, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb));
            var hdrImage = new HdrImage(4, 2, 240, 400);
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    hdrImage.Pixels[x, y] = new HalfVector4(x, y, 0, 1);
                }
            }
            hdrImage.BelongTo(capture.Image);
            capture.HdrImage = hdrImage;
            return WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
        }

        private static (float Red, float Green) Get(HdrImage hdrImage, int x, int y)
        {
            var pixel = hdrImage.Pixels[x, y].ToVector4();
            return (pixel.X, pixel.Y);
        }

        [Fact]
        public void CutOut_CutsTheHdrImageTheSameWay()
        {
            using var surface = CreateSurface();

            // The columns 1 and 2 are cut out, 0 and 3 are joined
            Assert.True(surface.ApplyCutOut(new NativeRect(1, 0, 2, 2), CropContainer.CropModes.Vertical));

            Assert.True(HdrExport.CanWriteHdr(surface));
            Assert.Equal(2, surface.HdrImage.Width);
            // Two tiles (before and after the band), together they cover it
            using (var composed = surface.HdrImage.Compose((Bitmap)surface.Image))
            {
                Assert.Equal((0f, 1f), Get(composed, 0, 1));
                Assert.Equal((3f, 1f), Get(composed, 1, 1));
            }

            surface.Undo();
            Assert.Equal(4, surface.HdrImage.Width);
        }

        [Fact]
        public void Crop_CropsTheHdrImage_Undo_BringsBackTheWholeOne()
        {
            using var surface = CreateSurface();

            Assert.True(surface.ApplyCrop(new NativeRect(1, 0, 2, 2)));

            Assert.True(HdrExport.CanWriteHdr(surface));
            Assert.Equal(2, surface.HdrImage.Width);
            Assert.Equal((1f, 0f), Get(surface.HdrImage, 0, 0));

            surface.Undo();
            Assert.True(HdrExport.CanWriteHdr(surface));
            Assert.Equal(4, surface.HdrImage.Width);

            surface.Redo();
            Assert.True(HdrExport.CanWriteHdr(surface));
            Assert.Equal(2, surface.HdrImage.Width);
        }

        [Fact]
        public async Task Rotate_RotatesTheHdrImage()
        {
            using var surface = CreateSurface();

            await Task.Run(() => surface.ApplyBitmapEffectAsync(new RotateEffect(90)));

            Assert.True(HdrExport.CanWriteHdr(surface));
            var hdrImage = surface.HdrImage;
            Assert.Equal(2, hdrImage.Width);
            Assert.Equal(4, hdrImage.Height);
            // Clockwise: the top left pixel goes to the top right
            Assert.Equal((0f, 0f), Get(hdrImage, 1, 0));
            Assert.Equal((0f, 1f), Get(hdrImage, 0, 0));
            Assert.Equal((3f, 0f), Get(hdrImage, 1, 3));
        }

        [Fact]
        public async Task AnotherEffect_LeavesTheHdrImageBehind()
        {
            using var surface = CreateSurface();

            await Task.Run(() => surface.ApplyBitmapEffectAsync(new GrayscaleEffect()));

            Assert.NotNull(surface.HdrImage);
            Assert.False(HdrExport.CanWriteHdr(surface));
        }
    }
}
