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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using Greenshot.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// SaveFile with AskForFilename: the save dialog, which suggests the HDR format when the HDR file can be written
    /// </summary>
    public class FileDestinationAskForFilenameTests
    {
        public FileDestinationAskForFilenameTests()
        {
            TestEnvironment.EnsureInitialized();
            if (SimpleServiceProvider.Current.GetInstance<IHdrImageEncoder>(true) == null)
            {
                SimpleServiceProvider.Current.AddService<IHdrImageEncoder>(new WicHdrImageEncoder());
            }
        }

        /// <summary>
        /// An interactive user who picks a file (or cancels) in the save dialog
        /// </summary>
        private class SaveDialogUserInteraction : IUserInteraction
        {
            private readonly string _pick;

            public SaveDialogUserInteraction(string pick, bool interactive = true)
            {
                _pick = pick;
                IsInteractive = interactive;
            }

            public List<SaveFileRequest> Dialogs { get; } = new List<SaveFileRequest>();

            public List<Notification> Notifications { get; } = new List<Notification>();

            public bool IsInteractive { get; }

            public Task<SurfaceOutputSettings> PromptOutputSettingsAsync(SurfaceOutputSettings current, CancellationToken cancellationToken) => Task.FromResult(current);

            public Task NotifyAsync(Notification notification)
            {
                Notifications.Add(notification);
                return Task.CompletedTask;
            }

            public Task<string> PickSaveFileAsync(SaveFileRequest request, CancellationToken cancellationToken)
            {
                Dialogs.Add(request);
                return Task.FromResult(_pick);
            }

            public Task<IDestination> PickDestinationAsync(IReadOnlyList<IDestination> choices, ICaptureDetails captureDetails, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<T> RunWithProgressAsync<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> work, CancellationToken cancellationToken) => work(new Progress<ProgressInfo>(), cancellationToken);

            /// <summary>
            /// The answer to a question, null: no question expected
            /// </summary>
            public bool? ConfirmAnswer { get; set; }

            public List<string> Questions { get; } = new List<string>();

            public Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken)
            {
                Questions.Add(message);
                return ConfirmAnswer.HasValue ? Task.FromResult(ConfirmAnswer) : throw new InvalidOperationException("Not expected");
            }
        }

        /// <summary>
        /// A new capture as a surface, with an HDR image when asked
        /// </summary>
        private static Surface CreateSurface(bool withHdr)
        {
            var capture = new Capture(new Bitmap(8, 4));
            if (withHdr)
            {
                var hdrImage = new HdrImage(8, 4, 240, 400);
                // A highlight: without one the capture has no HDR content
                hdrImage.Pixels[7, 3] = new SixLabors.ImageSharp.PixelFormats.HalfVector4(5, 5, 5, 1);
                hdrImage.BelongTo(capture.Image);
                capture.HdrImage = hdrImage;
            }
            // Without a WinForms SynchronizationContext on the test thread, nothing pumps it
            return WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
        }

        private static async Task<ExportResult> ExportAsync(Surface surface, IUserInteraction ui, string hdrFormat, bool manuallyInitiated = false)
        {
            var options = new FileDestinationOptions
            {
                AskForFilename = true,
                HdrFormat = hdrFormat,
                CopyPathToClipboard = false
            };
            // Without a SynchronizationContext, like a flow: nothing may wait for the UI thread of the test
            return await Task.Run(async () =>
            {
                using var source = await SurfaceExportSource.CreateAsync(surface, InlineUiDispatcher.Instance);
                var request = new ExportRequest(source, surface.CaptureDetails, manuallyInitiated, ui);
                return await new FileDestination(options).ExportAsync(request, CancellationToken.None);
            });
        }

        private static string TempFile(string extension) => Path.Combine(Path.GetTempPath(), $"ask_{Guid.NewGuid():N}.{extension}");

        /// <summary>
        /// A capture of 16x8 pixels on an HDR display: white in the 8-bit image, HDR highlights of 5.0 (400 nits) in the HDR image
        /// </summary>
        private static Surface CreateBrightHdrSurface()
        {
            var bitmap = new Bitmap(16, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
            }
            var capture = new Capture(bitmap);
            var hdrImage = new HdrImage(16, 8, 240, 400);
            hdrImage.Pixels.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < 8; y++)
                {
                    accessor.GetRowSpan(y).Fill(new SixLabors.ImageSharp.PixelFormats.HalfVector4(5, 5, 5, 1));
                }
            });
            hdrImage.BelongTo(capture.Image);
            capture.HdrImage = hdrImage;
            return WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
        }

        /// <summary>
        /// A row of an HDR file as RGBA floats (scRGB: 3.0 is the SDR white of 240 nits)
        /// </summary>
        private static float[] ReadHdrRow(string path, int y)
        {
            var frame = System.Windows.Media.Imaging.BitmapDecoder.Create(new Uri(path), System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                System.Windows.Media.Imaging.BitmapCacheOption.OnLoad).Frames[0];
            var floats = new System.Windows.Media.Imaging.FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Rgba128Float, null, 0);
            var row = new float[frame.PixelWidth * 4];
            floats.CopyPixels(new System.Windows.Int32Rect(0, y, frame.PixelWidth, 1), row, frame.PixelWidth * 16, 0);
            return row;
        }

        [Fact]
        public async Task DrawnElement_IsInTheHdrFile_AtTheSdrWhiteLevel_TheRestKeepsTheHighlights()
        {
            string path = TempFile("jxr");
            try
            {
                using var surface = CreateBrightHdrSurface();
                var rectangle = new RectangleContainer(surface) { Left = 2, Top = 2, Width = 8, Height = 4 };
                rectangle.SetFieldValue(FieldType.SHADOW, false);
                surface.AddElement(rectangle, false, false);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, HdrOutput.Auto);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Empty(ui.Notifications);
                var row = ReadHdrRow(path, 4);
                // The red border at the left: red at the SDR white level, no green
                Assert.True(row[2 * 4] > 1.5f, $"red {row[2 * 4]}");
                Assert.True(row[2 * 4 + 1] < 0.5f, $"green {row[2 * 4 + 1]}");
                // Inside the (unfilled) rectangle and next to it the HDR highlights
                Assert.Equal(5f, row[6 * 4], 2);
                Assert.Equal(5f, row[14 * 4], 2);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task TornEdgesOfACrop_OnlyTheEdgesAreSdr_TheMiddleKeepsTheHighlights()
        {
            string path = TempFile("jxr");
            try
            {
                var bigger = new Bitmap(64, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(bigger))
                {
                    graphics.Clear(Color.White);
                }
                // A capture big enough for torn edges, cropped with them on all four sides
                var capture = new Capture(bigger);
                var hdrImage = new HdrImage(64, 32, 240, 400);
                hdrImage.Pixels.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        accessor.GetRowSpan(y).Fill(new SixLabors.ImageSharp.PixelFormats.HalfVector4(5, 5, 5, 1));
                    }
                });
                hdrImage.BelongTo(capture.Image);
                capture.HdrImage = hdrImage;
                using var tornSurface = WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
                var edges = new Greenshot.Base.Effects.TornEdgeEffect { ToothHeight = 2, HorizontalToothRange = 6, VerticalToothRange = 6, GenerateShadow = false };
                Assert.True(tornSurface.ApplyCrop(new Dapplo.Windows.Common.Structs.NativeRect(4, 4, 56, 24), CutMarkStyle.Torn, edges));
                Assert.True(HdrExport.CanWriteHdr(tornSurface));
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(tornSurface, ui, HdrOutput.Auto);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                // The middle keeps the HDR highlights, only the torn bands are from the 8-bit render
                var row = ReadHdrRow(path, 12);
                Assert.Equal(5f, row[28 * 4], 2);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData(false, null)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public async Task SavingAJpegXr_WhoseHdrWasLost_TheUserIsAsked_AFlowSavesIt(bool manuallyInitiated, bool? answer)
        {
            string path = TempFile("jxr");
            try
            {
                using var surface = CreateBrightHdrSurface();
                await Task.Run(() => surface.ApplyBitmapEffectAsync(new Greenshot.Base.Effects.MonochromeEffect(127)));
                Assert.False(HdrExport.CanWriteHdr(surface));
                var ui = new SaveDialogUserInteraction(path) { ConfirmAnswer = answer };

                var result = await ExportAsync(surface, ui, null, manuallyInitiated);

                // An 8-bit JPEG XR: the user who saves it is asked first, a flow saves it (the recipe can check payload.hdr); never a notification
                Assert.Equal(manuallyInitiated ? 1 : 0, ui.Questions.Count);
                Assert.Empty(ui.Notifications);
                bool saved = answer != false;
                Assert.Equal(saved ? ExportStatus.Succeeded : ExportStatus.Declined, result.Status);
                Assert.Equal(saved, File.Exists(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task PixelatedArea_IsTheRenderedCapture_InTheHdrFile()
        {
            string path = TempFile("jxr");
            try
            {
                using var surface = CreateBrightHdrSurface();
                surface.AddElement(new ObfuscateContainer(surface) { Left = 4, Top = 2, Width = 6, Height = 4 }, false, false);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, HdrOutput.Auto);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                var row = ReadHdrRow(path, 4);
                // What is pixelated is the 8-bit capture (white), not the HDR content under it
                Assert.Equal(3f, row[6 * 4], 2);
                Assert.Equal(5f, row[14 * 4], 2);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Save the 16x8 bright HDR capture (5.0, SDR white 3.0) with a highlight element of the filter over 4,2 6x4 as JPEG XR: row 4 of the file
        /// </summary>
        private static async Task<float[]> SaveWithHighlightAsync(FilterContainer.PreparedFilter preparedFilter, bool rectangleBefore)
        {
            string path = TempFile("jxr");
            try
            {
                using var surface = CreateBrightHdrSurface();
                if (rectangleBefore)
                {
                    // Drawn before the filter, so it's under it: the filter works on it too, like in the editor
                    surface.AddElement(new RectangleContainer(surface) { Left = 8, Top = 2, Width = 1, Height = 1 }, false, false);
                }
                var highlight = new HighlightContainer(surface) { Left = 4, Top = 2, Width = 6, Height = 4 };
                highlight.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, preparedFilter);
                surface.AddElement(highlight, false, false);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, HdrOutput.Auto);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                return ReadHdrRow(path, 4);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task Magnifier_IsDoneOnTheHdrPixels_TheHighlightsStay()
        {
            var row = await SaveWithHighlightAsync(FilterContainer.PreparedFilter.MAGNIFICATION, false);

            Assert.Equal(5f, row[6 * 4], 2);
            Assert.Equal(5f, row[14 * 4], 2);
        }

        [Fact]
        public async Task AreaHighlight_DarkensTheHdrPixelsOutside_TheHighlightsStayInside()
        {
            var row = await SaveWithHighlightAsync(FilterContainer.PreparedFilter.AREA_HIGHLIGHT, false);

            Assert.Equal(5f, row[6 * 4], 2);
            // Darker than the highlight, but still brighter than SDR white: not the 8-bit capture
            Assert.InRange(row[14 * 4], 3.1f, 4.9f);
        }

        [Fact]
        public async Task TextHighlighter_LimitsTheHdrPixelsToItsColor()
        {
            var row = await SaveWithHighlightAsync(FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT, false);

            Assert.InRange(row[6 * 4 + 2], 0f, 3.05f);
            Assert.Equal(5f, row[14 * 4], 2);
        }

        [Fact]
        public async Task Magnifier_OverAnElementDrawnBefore_MagnifiesItToo_TheHighlightsStay()
        {
            var row = await SaveWithHighlightAsync(FilterContainer.PreparedFilter.MAGNIFICATION, true);

            // Like in the editor: the element drawn before is magnified with the capture, between its lines the highlights stay
            var magnified = Enumerable.Range(4, 6).Select(x => row[x * 4]).ToList();
            Assert.Contains(magnified, value => value < 4.6f);
            Assert.Contains(magnified, value => value > 4.9f);
            Assert.Equal(5f, row[14 * 4], 2);
        }

        [Fact]
        public async Task AreaHighlight_OverAnElementDrawnBefore_KeepsTheHdrContent()
        {
            var row = await SaveWithHighlightAsync(FilterContainer.PreparedFilter.AREA_HIGHLIGHT, true);

            // The rest of the capture is darkened but keeps its HDR content (not the 8-bit export), the area itself keeps the highlights
            Assert.InRange(row[0], 3.1f, 4.9f);
            Assert.Equal(5f, row[5 * 4], 2);
        }

        [Fact]
        public async Task SaveAsJpegXr_OfAnHdrCapture_IsTheHdrFile_AlsoWithoutHdrFormat()
        {
            string path = TempFile("jxr");
            try
            {
                using var surface = CreateBrightHdrSurface();
                surface.AddElement(new RectangleContainer(surface) { Left = 2, Top = 2, Width = 8, Height = 4 }, false, false);
                var ui = new SaveDialogUserInteraction(path);

                // Like the editor's Save as: no HdrFormat, the user picks JPEG XR
                var result = await ExportAsync(surface, ui, null);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                // Away from the rectangle and its shadow: the HDR highlights
                Assert.Equal(5f, ReadHdrRow(path, 0)[15 * 4], 2);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task EffectsOfTheEditor_AreInTheHdrFile_TheBorderFromThe8BitImage()
        {
            string path = TempFile("jxr");
            try
            {
                using var surface = CreateBrightHdrSurface();
                await Task.Run(() => surface.ApplyBitmapEffectAsync(new Greenshot.Base.Effects.GrayscaleEffect()));
                await Task.Run(() => surface.ApplyBitmapEffectAsync(new Greenshot.Base.Effects.BorderEffect { Width = 2, Color = Color.Black }));
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, null);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Empty(ui.Notifications);
                var row = ReadHdrRow(path, 4);
                // The black border of the 8-bit image, then the gray highlight of the capture (the luminance of 5, 5, 5 is 5)
                Assert.Equal(0f, row[0], 2);
                Assert.Equal(5f, row[2 * 4], 2);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task HdrCapture_DialogSuggestsJpegXr_AndWritesTheHdrFile()
        {
            string path = TempFile("jxr");
            try
            {
                using var surface = CreateSurface(true);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, HdrOutput.Auto);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Equal("jxr", Assert.Single(ui.Dialogs).Format);
                // JPEG XR is labeled as the HDR file
                Assert.True(ui.Dialogs[0].HdrAvailable);
                Assert.Empty(ui.Notifications);
                byte[] bytes = File.ReadAllBytes(path);
                // JPEG XR: "II", 0xBC
                Assert.Equal(new byte[] { 0x49, 0x49, 0xBC }, new[] { bytes[0], bytes[1], bytes[2] });
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task HdrCaptureAcrossAnSdrDisplay_IsComposedWhenSaved()
        {
            string path = TempFile("jxr");
            try
            {
                // The left half was on an HDR display, the right half on an SDR display (white in the 8-bit image)
                var bitmap = new Bitmap(8, 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                }
                var capture = new Capture(bitmap);
                using (var hdrDisplay = new HdrImage(4, 4, 240, 400))
                {
                    hdrDisplay.Pixels[3, 3] = new SixLabors.ImageSharp.PixelFormats.HalfVector4(5, 5, 5, 1);
                    capture.HdrImage = HdrImage.Combine(8, 4, new[] { (hdrDisplay, new Dapplo.Windows.Common.Structs.NativePoint(0, 0)) });
                }
                capture.HdrImage.BelongTo(capture.Image);
                using var surface = WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
                Assert.False(surface.HdrImage.IsComplete);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, HdrOutput.Auto);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Empty(ui.Notifications);
                var frame = System.Windows.Media.Imaging.BitmapDecoder.Create(new Uri(path), System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                    System.Windows.Media.Imaging.BitmapCacheOption.OnLoad).Frames[0];
                var floats = new System.Windows.Media.Imaging.FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Rgba128Float, null, 0);
                var row = new float[8 * 4];
                floats.CopyPixels(new System.Windows.Int32Rect(0, 0, 8, 1), row, 8 * 16, 0);
                // The HDR display as it was (black here), the SDR display at the SDR white level: 240 nits is 3.0
                Assert.Equal(0f, row[0], 2);
                Assert.Equal(3f, row[7 * 4], 2);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task HdrCaptureWithCursor_HasTheCursorInTheHdrFile()
        {
            string path = TempFile("jxr");
            try
            {
                // A black HDR capture with a red (alpha) cursor at 2,1
                var capture = new Capture(new Bitmap(8, 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb));
                var hdrImage = new HdrImage(8, 4, 240, 400);
                hdrImage.Pixels.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < 4; y++)
                    {
                        accessor.GetRowSpan(y).Fill(new SixLabors.ImageSharp.PixelFormats.HalfVector4(0, 0, 0, 1));
                    }
                });
                hdrImage.Pixels[7, 3] = new SixLabors.ImageSharp.PixelFormats.HalfVector4(5, 5, 5, 1);
                hdrImage.BelongTo(capture.Image);
                capture.HdrImage = hdrImage;
                var red = new Bitmap(2, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(red))
                {
                    graphics.Clear(Color.Red);
                }
                capture.Cursor = new Dapplo.Windows.Icons.CapturedCursor { ColorLayer = red, Size = new Dapplo.Windows.Common.Structs.NativeSize(2, 2) };
                capture.CursorVisible = true;
                capture.CursorLocation = new Dapplo.Windows.Common.Structs.NativePoint(2, 1);
                using var surface = WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
                Assert.True(surface.HasCursor);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, HdrOutput.Auto);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Empty(ui.Notifications);
                var frame = System.Windows.Media.Imaging.BitmapDecoder.Create(new Uri(path), System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                    System.Windows.Media.Imaging.BitmapCacheOption.OnLoad).Frames[0];
                var floats = new System.Windows.Media.Imaging.FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Rgba128Float, null, 0);
                var row = new float[8 * 4];
                floats.CopyPixels(new System.Windows.Int32Rect(0, 1, 8, 1), row, 8 * 16, 0);
                // Next to the cursor the HDR pixels, under it the red of the cursor at the SDR white level (240 nits is 3.0)
                Assert.Equal(0f, row[0], 2);
                Assert.Equal(3f, row[2 * 4], 2);
                Assert.Equal(0f, row[2 * 4 + 1], 2);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task HdrCapture_UserPicksPng_SavesThePng_WithoutNotification()
        {
            string path = TempFile("png");
            try
            {
                using var surface = CreateSurface(true);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, HdrOutput.Auto);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Empty(ui.Notifications);
                byte[] bytes = File.ReadAllBytes(path);
                Assert.Equal(0x89, bytes[0]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData("Jxr")]
        [InlineData("Auto")]
        public async Task NoHdrImage_DialogSuggestsNothing_SavesTheNormalFile_Quietly(string hdrFormat)
        {
            string path = TempFile("png");
            try
            {
                using var surface = CreateSurface(false);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, hdrFormat);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Null(Assert.Single(ui.Dialogs).Format);
                Assert.False(ui.Dialogs[0].HdrAvailable);
                // A recipe which wants to tell or ask checks payload.hdr before
                Assert.Empty(ui.Notifications);
                Assert.True(File.Exists(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task WithoutHdrFormat_OnlyAsks()
        {
            string path = TempFile("png");
            try
            {
                using var surface = CreateSurface(true);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, null);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                // HDR content only by the setting: the configured format, JPEG XR only marked as HDR
                Assert.Null(Assert.Single(ui.Dialogs).Format);
                Assert.True(ui.Dialogs[0].HdrAvailable);
                Assert.Empty(ui.Notifications);
                Assert.True(File.Exists(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task RequestedHdr_SuggestsJpegXr()
        {
            string path = TempFile("jxr");
            try
            {
                using var surface = CreateSurface(true);
                // A Source step with Hdr, or an opened HDR file
                surface.HdrImage.IsRequested = true;
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, null);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Equal("jxr", Assert.Single(ui.Dialogs).Format);
                Assert.True(surface.HdrImage.Crop(new Dapplo.Windows.Common.Structs.NativeRect(0, 0, 2, 2)).IsRequested);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task UserCancels_Declined()
        {
            using var surface = CreateSurface(true);
            var ui = new SaveDialogUserInteraction(null);

            var result = await ExportAsync(surface, ui, HdrOutput.Auto);

            Assert.Equal(ExportStatus.Declined, result.Status);
        }

        [Fact]
        public async Task NotInteractive_SavesWithoutTheDialog()
        {
            string path = TempFile("png");
            try
            {
                using var surface = CreateSurface(false);
                // The capture's own file: saved there without asking
                surface.CaptureDetails.Filename = path;
                var ui = new SaveDialogUserInteraction(null, false);

                var result = await ExportAsync(surface, ui, null);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Empty(ui.Dialogs);
                Assert.True(File.Exists(path));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
