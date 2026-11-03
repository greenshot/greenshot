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

        /// <summary>
        /// A white capture of 100x100 pixels on an HDR display, all of it highlights of 5.0 (SDR white 3.0)
        /// </summary>
        private static Surface CreateBrightSurface()
        {
            var bitmap = new Bitmap(100, 100, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
            }
            var capture = new Capture(bitmap);
            var hdrImage = new HdrImage(100, 100, 240, 400);
            hdrImage.Pixels.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < 100; y++)
                {
                    accessor.GetRowSpan(y).Fill(new HalfVector4(5, 5, 5, 1));
                }
            });
            hdrImage.BelongTo(capture.Image);
            capture.HdrImage = hdrImage;
            return WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
        }

        /// <summary>
        /// In the row of the composed HDR image between the columns: how many pixels keep the highlight, how many are torn off (transparent, or the 8-bit image at most SDR white)
        /// </summary>
        /// <summary>
        /// <see cref="CountRow"/> over several rows
        /// </summary>
        private static (int Highlights, int FromSdr) CountBand(Surface surface, int fromY, int toY, int fromX, int toX, bool withElements = false)
        {
            int highlights = 0, fromSdr = 0;
            for (int y = fromY; y < toY; y++)
            {
                var (rowHighlights, rowFromSdr) = CountRow(surface, y, fromX, toX, withElements);
                highlights += rowHighlights;
                fromSdr += rowFromSdr;
            }
            return (highlights, fromSdr);
        }

        private static (int Highlights, int FromSdr) CountRow(Surface surface, int y, int fromX, int toX, bool withElements = false)
        {
            using var overlay = withElements ? surface.CreateHdrOverlay() : null;
            using var composed = surface.HdrImage.Compose((Bitmap)surface.Image, overlay);
            int highlights = 0, fromSdr = 0;
            for (int x = fromX; x < toX; x++)
            {
                var pixel = composed.Pixels[x, y].ToVector4();
                // A torn off pixel is transparent (cut away in the HDR image) or the 8-bit image
                if (pixel.W > 0.99f && pixel.X > 4.9f)
                {
                    highlights++;
                }
                else if (pixel.W < 0.01f || pixel.X <= 3.05f)
                {
                    fromSdr++;
                }
            }
            return (highlights, fromSdr);
        }

        [Fact]
        public void TornEdgesWithShadow_MoveTheHdrImageIntoTheRoom_DrawnIntoTheImage_OnlyTheTeethComeFromThe8BitImage()
        {
            using var surface = CreateBrightSurface();
            var settings = new TornEdgeEffect { ToothHeight = 12, HorizontalToothRange = 20, VerticalToothRange = 20, Seed = 1 };
            int margin = TornEdgeContainer.GetShadowMargin(settings);

            var tornEdges = surface.AddTornEdges(settings);

            Assert.Equal(HdrState.Available, surface.HdrState);
            Assert.Equal(new NativeRect(margin, margin, 100, 100), Assert.Single(surface.HdrImage.TileAreas));

            Assert.True(surface.ApplyElementToImage(tornEdges));

            Assert.Equal(HdrState.Available, surface.HdrState);
            Assert.True(surface.HdrImage.HasClearedPixels);
            // In the middle of the band of the top teeth: the teeth come from the 8-bit image, between them the highlights stay
            // The element chooses its own teeth: over the rows of the band some pixels are torn off, some keep the highlight
            var (highlights, fromSdr) = CountBand(surface, margin + 1, margin + 12, margin + 12, margin + 88);
            Assert.True(highlights > 0, "highlights between the teeth");
            Assert.True(fromSdr > 0, "torn off teeth");
            // Below the band all of it keeps its HDR content
            Assert.Equal((76, 0), CountRow(surface, margin + 20, margin + 12, margin + 88));
        }

        [Fact]
        public void TornEdgesElement_WhenSaved_OnlyTheTeethComeFromThe8BitImage()
        {
            using var surface = CreateBrightSurface();
            var settings = new TornEdgeEffect { ToothHeight = 12, HorizontalToothRange = 20, VerticalToothRange = 20, Seed = 1, GenerateShadow = false };

            surface.AddTornEdges(settings);

            // The element chooses its own teeth: over the rows of the band some pixels are torn off, some keep the highlight
            var (highlights, fromSdr) = CountBand(surface, 1, 12, 12, 88, true);
            Assert.True(highlights > 0, "highlights between the teeth");
            Assert.True(fromSdr > 0, "torn off teeth");
            Assert.Equal((76, 0), CountRow(surface, 20, 12, 88, true));
        }

        [Fact]
        public void CutOutVertically_WithTornEdges_OnlyTheTeethAndTheGapComeFromThe8BitImage()
        {
            using var surface = CreateBrightSurface();
            var settings = new TornEdgeEffect { ToothHeight = 12, HorizontalToothRange = 20, VerticalToothRange = 20, Seed = 1 };

            Assert.True(surface.ApplyCutOut(new NativeRect(30, 0, 30, 100), CropContainer.CropModes.Vertical, CutMarkStyle.Torn, settings));

            var cutMark = Assert.Single(surface.Elements.OfType<CutMarkContainer>());
            // Across the band of the cut mark: the gap and the teeth from the 8-bit image, between the teeth the highlights stay
            var (highlights, fromSdr) = CountRow(surface, 50, cutMark.Left, cutMark.Left + cutMark.Width, true);
            Assert.True(highlights > 0, "highlights between the teeth");
            Assert.True(fromSdr > 0, "gap and teeth");
            // Left of the band all of it keeps its HDR content
            Assert.Equal((cutMark.Left, 0), CountRow(surface, 50, 0, cutMark.Left, true));
        }

        [Fact]
        public async Task TornEdgeEffect_OnlyTheTeethComeFromThe8BitImage()
        {
            using var surface = CreateBrightSurface();
            var settings = new TornEdgeEffect { ToothHeight = 12, HorizontalToothRange = 20, VerticalToothRange = 20, Seed = 1, GenerateShadow = false };

            await Task.Run(() => surface.ApplyBitmapEffectAsync(settings));

            var (highlights, fromSdr) = CountRow(surface, 6, 12, 88);
            Assert.True(highlights > 0, "highlights between the teeth");
            Assert.True(fromSdr > 0, "torn off teeth");
            Assert.Equal((76, 0), CountRow(surface, 20, 12, 88));
        }

        /// <summary>
        /// The pixels of the composed HDR image (with the elements) in the rows which are partly covered: edge pixels of the teeth
        /// </summary>
        private static System.Collections.Generic.List<System.Numerics.Vector4> EdgePixels(Surface surface, int fromY, int toY)
        {
            using var overlay = surface.CreateHdrOverlay();
            using var composed = surface.HdrImage.Compose((Bitmap)surface.Image, overlay);
            var edge = new System.Collections.Generic.List<System.Numerics.Vector4>();
            for (int y = fromY; y < toY; y++)
            {
                for (int x = 0; x < composed.Width; x++)
                {
                    var pixel = composed.Pixels[x, y].ToVector4();
                    if (pixel.W > 0.05f && pixel.W < 0.95f)
                    {
                        edge.Add(pixel);
                    }
                }
            }
            return edge;
        }

        [Fact]
        public async Task TornEdgeEffect_IsDrawnForReal_TheEdgeOfTheTeethKeepsTheHdrColor()
        {
            using var surface = CreateBrightSurface();
            var settings = new TornEdgeEffect { ToothHeight = 12, HorizontalToothRange = 20, VerticalToothRange = 20, Seed = 1, GenerateShadow = false };

            await Task.Run(() => surface.ApplyBitmapEffectAsync(settings));

            // One HDR image of all of it, drawn by ImageSharp
            Assert.True(surface.HdrImage.IsComplete);
            var edge = EdgePixels(surface, 0, 12);
            Assert.NotEmpty(edge);
            // Anti-aliased: partly transparent, with the color of the HDR image (not the tone mapped 8-bit image)
            Assert.All(edge, pixel => Assert.Equal(5f, pixel.X, 1));
        }

        [Fact]
        public async Task TornEdgeEffectWithShadow_TheShadowIsDrawnInTheHdrImage()
        {
            using var surface = CreateBrightSurface();
            var settings = new TornEdgeEffect { ToothHeight = 12, HorizontalToothRange = 20, VerticalToothRange = 20, Seed = 1, GenerateShadow = true };

            await Task.Run(() => surface.ApplyBitmapEffectAsync(settings));

            var hdrImage = surface.HdrImage;
            var sdrImage = (Bitmap)surface.Image;
            Assert.Equal(sdrImage.Size, new Size(hdrImage.Width, hdrImage.Height));
            // Where the 8-bit image has its shadow (black, partly transparent, outside the image) the HDR image has one too
            int shadows = 0;
            for (int y = hdrImage.Height - 20; y < hdrImage.Height; y++)
            {
                for (int x = 20; x < 80; x++)
                {
                    var sdr = sdrImage.GetPixel(x, y);
                    if (sdr.A > 20 && sdr.A < 235 && sdr.R < 40)
                    {
                        var hdr = hdrImage.Pixels[x, y].ToVector4();
                        Assert.InRange(hdr.W, 0.01f, 0.99f);
                        Assert.Equal(0f, hdr.X, 2);
                        shadows++;
                    }
                }
            }
            Assert.True(shadows > 0, "a shadow below the image");
            // The middle of the image keeps its highlights
            Assert.Equal(5f, hdrImage.Pixels[hdrImage.Width / 2, hdrImage.Height / 2].ToVector4().X, 1);
        }

        [Fact]
        public void TornEdgesElement_IsCutForReal_TheEdgeOfTheTeethKeepsTheHdrColor()
        {
            using var surface = CreateBrightSurface();
            var settings = new TornEdgeEffect { ToothHeight = 12, HorizontalToothRange = 20, VerticalToothRange = 20, Seed = 1, GenerateShadow = false };

            surface.AddTornEdges(settings);

            var edge = EdgePixels(surface, 0, 12);
            Assert.NotEmpty(edge);
            Assert.All(edge, pixel => Assert.Equal(5f, pixel.X, 1));
        }

        [Fact]
        public void ApplyEffect_OfAFlow_KeepsTheHdrContent_WithoutUndo()
        {
            using var surface = CreateSurface();

            Assert.True(surface.ApplyEffect(new BorderEffect { Width = 2 }));
            Assert.True(surface.ApplyEffect(new GrayscaleEffect()));

            Assert.False(surface.CanUndo);
            Assert.Equal(HdrState.Available, surface.HdrState);
            Assert.Equal(new NativeRect(2, 2, 4, 2), Assert.Single(surface.HdrImage.TileAreas));
        }

        [Fact]
        public void Clear_HasNoHdrContent_UndoBringsItBack()
        {
            using var surface = CreateSurface();

            surface.Clear(Color.White);

            Assert.Equal(HdrState.None, surface.HdrState);
            Assert.Null(surface.HdrImagePreparation);
            surface.Undo();
            Assert.Equal(HdrState.Available, surface.HdrState);
        }

        [Fact]
        public void Highlighters_WithoutBorder_NeedNoLayerOfTheirOwn()
        {
            using var surface = CreateBrightSurface();
            for (int i = 0; i < 3; i++)
            {
                surface.AddElement(new HighlightContainer(surface) { Left = 10 + i * 20, Top = 10, Width = 10, Height = 10 }, false, false);
            }

            using var overlay = surface.CreateHdrOverlay();

            Assert.Equal(3, overlay.Steps.Count(step => step.Filter != null));
            Assert.Empty(overlay.Steps.Where(step => step.Layer != null));
        }

        private static bool Covers(NativeRect area, int x, int y) => x >= area.Left && x < area.Right && y >= area.Top && y < area.Bottom;
    }
}
