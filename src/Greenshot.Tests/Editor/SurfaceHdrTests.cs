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
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Extensions;
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
        /// A capture of 4x2 pixels with an HDR image whose red is the x and green the y of the pixel, its blue is a highlight
        /// </summary>
        private static Surface CreateSurface()
        {
            var capture = new Capture(new Bitmap(4, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb));
            var hdrImage = new HdrImage(4, 2, 240, 400);
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    hdrImage.Pixels[x, y] = new HalfVector4(x, y, 5, 1);
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
        public async Task Monochrome_LeavesTheHdrImageBehind()
        {
            using var surface = CreateSurface();

            await Task.Run(() => surface.ApplyBitmapEffectAsync(new MonochromeEffect(127)));

            Assert.Equal(HdrState.Lost, surface.HdrState);
            Assert.NotNull(surface.HdrImage);
            Assert.False(HdrExport.CanWriteHdr(surface));
        }

        [Fact]
        public async Task Grayscale_IsDoneOnTheHdrImage_InLinearLight()
        {
            using var surface = CreateSurface();

            await Task.Run(() => surface.ApplyBitmapEffectAsync(new GrayscaleEffect()));

            Assert.Equal(HdrState.Available, surface.HdrState);
            var pixel = surface.HdrImage.Pixels[3, 1].ToVector4();
            // BT.709 of (3, 1, 5): the highlight of blue stays in the luminance
            float luminance = 0.2126f * 3 + 0.7152f * 1 + 0.0722f * 5;
            Assert.Equal(luminance, pixel.X, 2);
            Assert.Equal(luminance, pixel.Y, 2);
            Assert.Equal(luminance, pixel.Z, 2);
        }

        [Fact]
        public async Task Edits_AreDoneOnlyWhenTheHdrImageIsAskedFor_UndoComesBackToTheCapture()
        {
            using var surface = CreateSurface();
            var original = surface.HdrImage;

            await Task.Run(() => surface.ApplyBitmapEffectAsync(new InvertEffect()));
            await Task.Run(() => surface.ApplyBitmapEffectAsync(new BorderEffect { Width = 2 }));

            // Nothing was made for the state
            Assert.Equal(HdrState.Available, surface.HdrState);
            var made = surface.HdrImage;
            Assert.NotSame(original, made);
            Assert.Same(made, surface.HdrImage);
            Assert.Equal(8, made.Width);
            Assert.Equal(6, made.Height);
            // The capture inside the border, the border itself comes from the 8-bit image
            Assert.False(made.IsComplete);
            Assert.Equal(new NativeRect(2, 2, 4, 2), Assert.Single(made.TileAreas));

            surface.Undo();
            surface.Undo();
            Assert.Same(original, surface.HdrImage);
            Assert.Equal(HdrState.Available, surface.HdrState);
        }

        [Fact]
        public async Task Preparation_MakesTheHdrImageOnAnotherThread_TheSurfaceGetsThatOne()
        {
            using var surface = CreateSurface();
            Assert.Null(surface.HdrImagePreparation);
            await Task.Run(() => surface.ApplyBitmapEffectAsync(new BorderEffect { Width = 2 }));

            var preparation = surface.HdrImagePreparation;
            Assert.NotNull(preparation);
            int threadId = Environment.CurrentManagedThreadId;
            int madeOn = await Task.Run(() =>
            {
                preparation();
                return Environment.CurrentManagedThreadId;
            });

            Assert.NotEqual(threadId, madeOn);
            Assert.Null(surface.HdrImagePreparation);
            var made = surface.HdrImage;
            Assert.Same(made, surface.HdrImage);
            Assert.True(made.HasHdrContent);
        }

        [Fact]
        public async Task Preparation_AfterTheEditorWasClosed_DoesNothingHarmful()
        {
            Action preparation;
            using (var surface = CreateSurface())
            {
                await Task.Run(() => surface.ApplyBitmapEffectAsync(new InvertEffect()));
                preparation = surface.HdrImagePreparation;
            }

            // The surface released its HDR images, the preparation has its own share of the capture's
            await Task.Run(preparation);
        }

        [Fact]
        public async Task Resize_ResizesTheHdrImage_TheHighlightsStay()
        {
            using var surface = CreateSurface();

            await Task.Run(() => surface.ApplyBitmapEffectAsync(new ResizeEffect(8, 4, false)));

            var hdrImage = surface.HdrImage;
            Assert.Equal(8, hdrImage.Width);
            Assert.Equal(4, hdrImage.Height);
            // The blue of 5 everywhere: bicubic keeps a constant as it is
            Assert.Equal(5f, hdrImage.Pixels[4, 2].ToVector4().Z, 1);
        }

        [Fact]
        public void CropWithoutTheHighlights_IsKnownWhenTheHdrImageIsMade()
        {
            var capture = new Capture(new Bitmap(4, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb));
            var hdrImage = new HdrImage(4, 2, 240, 400);
            hdrImage.Pixels[3, 1] = new HalfVector4(5, 5, 5, 1);
            hdrImage.BelongTo(capture.Image);
            capture.HdrImage = hdrImage;
            using var surface = WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));

            Assert.True(surface.ApplyCrop(new NativeRect(0, 0, 2, 2)));

            // Decided by the capture until the crop is done on the HDR image
            Assert.Equal(HdrState.Available, surface.HdrState);
            Assert.False(surface.HdrImage.HasHdrContent);
            Assert.Equal(HdrState.None, surface.HdrState);
        }

        [Fact]
        public void TornEdgesWithShadow_MoveTheHdrImageIntoTheRoom_DrawnIntoTheImage_TheirBandsAreLeftOut()
        {
            var capture = new Capture(new Bitmap(100, 100, System.Drawing.Imaging.PixelFormat.Format32bppArgb));
            var hdrImage = new HdrImage(100, 100, 240, 400);
            hdrImage.Pixels[50, 50] = new HalfVector4(5, 5, 5, 1);
            hdrImage.BelongTo(capture.Image);
            capture.HdrImage = hdrImage;
            using var surface = WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
            var settings = new TornEdgeEffect { ToothHeight = 12, HorizontalToothRange = 20, VerticalToothRange = 20 };
            int margin = TornEdgeContainer.GetShadowMargin(settings);

            var tornEdges = surface.AddTornEdges(settings);

            Assert.Equal(HdrState.Available, surface.HdrState);
            Assert.Equal(new NativeRect(margin, margin, 100, 100), Assert.Single(surface.HdrImage.TileAreas));

            Assert.True(surface.ApplyElementToImage(tornEdges));

            Assert.Equal(HdrState.Available, surface.HdrState);
            // The middle keeps its HDR content, the torn bands (with the room for the shadow) come from the 8-bit image
            var middle = Assert.Single(surface.HdrImage.TileAreas);
            var bands = tornEdges.EdgeAreas().ToList();
            Assert.True(Covers(middle, 50 + margin, 50 + margin));
            Assert.DoesNotContain(bands, band => band.IntersectsWith(middle));
            Assert.True(surface.HdrImage.HasHdrContent);
        }

        private static bool Covers(NativeRect area, int x, int y) => x >= area.Left && x < area.Right && y >= area.Top && y < area.Bottom;
    }
}
