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

            public Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");
        }

        /// <summary>
        /// A new capture as a surface, with HDR pixels when asked
        /// </summary>
        private static Surface CreateSurface(bool withHdr)
        {
            var capture = new Capture(new Bitmap(8, 4));
            if (withHdr)
            {
                var pixels = new HdrPixels(8, 4, 240, 400);
                pixels.BelongTo(capture.Image);
                capture.CaptureDetails.HdrPixels = pixels;
            }
            // Without a WinForms SynchronizationContext on the test thread, nothing pumps it
            return WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
        }

        private static async Task<ExportResult> ExportAsync(Surface surface, IUserInteraction ui, string hdrFormat)
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
                var request = new ExportRequest(source, surface.CaptureDetails, false, ui);
                return await new FileDestination(options).ExportAsync(request, CancellationToken.None);
            });
        }

        private static string TempFile(string extension) => Path.Combine(Path.GetTempPath(), $"ask_{Guid.NewGuid():N}.{extension}");

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

        [Fact]
        public async Task NoHdrPixels_DialogSuggestsNothing_SavesTheNormalFile_AndTellsWhy()
        {
            string path = TempFile("png");
            try
            {
                using var surface = CreateSurface(false);
                var ui = new SaveDialogUserInteraction(path);

                var result = await ExportAsync(surface, ui, HdrOutput.Auto);

                Assert.Equal(ExportStatus.Succeeded, result.Status);
                Assert.Null(Assert.Single(ui.Dialogs).Format);
                Assert.Single(ui.Notifications);
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
                Assert.Null(Assert.Single(ui.Dialogs).Format);
                Assert.Empty(ui.Notifications);
                Assert.True(File.Exists(path));
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
