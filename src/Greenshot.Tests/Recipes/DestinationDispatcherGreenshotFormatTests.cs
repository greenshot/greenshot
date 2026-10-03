/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.FileFormatHandlers;
using Greenshot.Pipeline;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// Regression tests for saving captures in the .greenshot format through the recipe DestinationDispatcher.
    /// </summary>
    public class DestinationDispatcherGreenshotFormatTests
    {
        public DestinationDispatcherGreenshotFormatTests()
        {
            TestEnvironment.EnsureInitialized();
            if (!SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>().OfType<GreenshotFileFormatHandler>().Any())
            {
                SimpleServiceProvider.Current.AddService<IFileFormatHandler>(new GreenshotFileFormatHandler());
            }
        }

        private class StubDestination : DestinationBase
        {
            private readonly bool _keepsCapture;

            public StubDestination(string designation, bool keepsCapture = false)
            {
                Designation = designation;
                Descriptor = new DestinationDescriptor(designation);
                _keepsCapture = keepsCapture;
            }

            public override string Designation { get; }
            public override DestinationDescriptor Descriptor { get; }

            public override Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
                => Task.FromResult(ExportResult.Succeeded(clearsModified: false, keepsCapture: _keepsCapture));
        }

        /// <summary>
        /// Counts how often it was called
        /// </summary>
        private class CountingDestination : StubDestination
        {
            public CountingDestination(string designation) : base(designation)
            {
            }

            public int Calls { get; private set; }

            public override Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
            {
                Calls++;
                return base.ExportAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// An interactive user who answers the quality dialog with OK or Cancel
        /// </summary>
        private class QualityUserInteraction : IUserInteraction
        {
            private readonly bool _ok;

            public QualityUserInteraction(bool ok)
            {
                _ok = ok;
            }

            public int QualityPrompts { get; private set; }

            public bool IsInteractive => true;

            public Task<SurfaceOutputSettings> PromptOutputSettingsAsync(SurfaceOutputSettings current, CancellationToken cancellationToken)
            {
                QualityPrompts++;
                return Task.FromResult(_ok ? current : null);
            }

            public Task NotifyAsync(Notification notification) => Task.CompletedTask;

            public Task<string> PickSaveFileAsync(SaveFileRequest request, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<IDestination> PickDestinationAsync(IReadOnlyList<IDestination> choices, ICaptureDetails captureDetails, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<T> RunWithProgressAsync<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> work, CancellationToken cancellationToken) => work(new Progress<ProgressInfo>(), cancellationToken);

            public Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");
        }

        private static CaptureFlowContext CreateContext(string filename)
        {
            var bmp = new Bitmap(64, 48);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Blue);
            }

            var capture = new Capture(bmp);
            capture.CaptureDetails.Filename = filename;
            var context = new CaptureFlowContext(new CaptureRecipe("greenshot_format", "Greenshot format"))
            {
                Payload = new CapturePayload(capture)
            };
            context.Properties["EnableCompletionNotification"] = false;
            context.Properties["Destination.PromptQuality"] = false;
            context.Properties["Destination.CopyPathToClipboard"] = false;
            // Same as OutputFileFormat=greenshot in the configuration
            context.Properties["Destination.SurfaceOutputSettings"] = new SurfaceOutputSettings(WellKnownFileFormats.Greenshot);
            context.Payload.EnsureSurface();
            return context;
        }

        /// <summary>
        /// Runs the dispatcher without a SynchronizationContext, so nothing (e.g. a save dialog) can be marshalled to a UI thread
        /// </summary>
        private static Task DispatchWithoutUiContext(CaptureFlowContext context, params IDestination[] destinations)
            => Task.Run(() => new DestinationDispatcher().DispatchAsync(context, destinations));

        [Fact]
        public async Task Dispatch_FileDestinationWithGreenshotFormat_WritesLoadableFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dispatcher_{Guid.NewGuid():N}.greenshot");
            try
            {
                using CaptureFlowContext context = CreateContext(path);

                await DispatchWithoutUiContext(context, new StubDestination(nameof(WellKnownDestinations.FileNoDialog)));

                Assert.True(File.Exists(path), "File was not written");
                byte[] bytes = File.ReadAllBytes(path);
                Assert.True(bytes.Length > 14, $"File is {bytes.Length} bytes");
                Assert.StartsWith("Greenshot", Encoding.ASCII.GetString(bytes, bytes.Length - 14, 14));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task Dispatch_QualityDialogOk_Saves()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dispatcher_{Guid.NewGuid():N}.png");
            try
            {
                using CaptureFlowContext context = CreateContext(path);
                context.Properties["Destination.PromptQuality"] = true;
                context.Properties["Destination.SurfaceOutputSettings"] = new SurfaceOutputSettings(WellKnownFileFormats.Png);
                var userInteraction = new QualityUserInteraction(true);
                context.UserInteraction = userInteraction;

                await DispatchWithoutUiContext(context, new StubDestination(nameof(WellKnownDestinations.FileNoDialog)));

                Assert.Equal(1, userInteraction.QualityPrompts);
                Assert.True(File.Exists(path), "The file was not saved");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task Dispatch_QualityDialogCancelled_DoesNotSaveButRunsTheOtherDestinations()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dispatcher_{Guid.NewGuid():N}.png");
            try
            {
                using CaptureFlowContext context = CreateContext(path);
                context.Properties["Destination.PromptQuality"] = true;
                context.Properties["Destination.SurfaceOutputSettings"] = new SurfaceOutputSettings(WellKnownFileFormats.Png);
                var userInteraction = new QualityUserInteraction(false);
                context.UserInteraction = userInteraction;
                var clipboard = new CountingDestination(nameof(WellKnownDestinations.Clipboard));

                await DispatchWithoutUiContext(context, new StubDestination(nameof(WellKnownDestinations.FileNoDialog)), clipboard);

                Assert.Equal(1, userInteraction.QualityPrompts);
                Assert.False(File.Exists(path), "The file was saved although the quality dialog was cancelled");
                Assert.Equal(1, clipboard.Calls);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task Dispatch_FileAndEditorWithGreenshotFormat_DisposingPayloadKeepsSurfaceImage()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dispatcher_{Guid.NewGuid():N}.greenshot");
            try
            {
                CaptureFlowContext context = CreateContext(path);
                ISurface surface = context.Payload.Surface;

                await DispatchWithoutUiContext(context,
                    new StubDestination(nameof(WellKnownDestinations.FileNoDialog)),
                    new StubDestination(EditorDestination.DESIGNATION, keepsCapture: true));
                context.Dispose();

                // A disposed GDI+ image throws ArgumentException from its properties
                Assert.Equal(64, surface.Image.Width);
                surface.Dispose();
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
