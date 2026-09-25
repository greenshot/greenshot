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
using System.Threading.Tasks;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
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

        private class StubDestination : AbstractDestination
        {
            private readonly string _designation;
            public StubDestination(string designation) => _designation = designation;
            public override string Designation => _designation;
            public override string Description => _designation;
            public override IEnumerable<IDestination> DynamicDestinations() => Enumerable.Empty<IDestination>();
            public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
                => new ExportInformation(Designation, Description, true);
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
            context.Properties["Destination.SurfaceOutputSettings"] = new SurfaceOutputSettings(OutputFormat.greenshot);
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
        public async Task Dispatch_FileAndEditorWithGreenshotFormat_DisposingPayloadKeepsSurfaceImage()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dispatcher_{Guid.NewGuid():N}.greenshot");
            try
            {
                CaptureFlowContext context = CreateContext(path);
                ISurface surface = context.Payload.Surface;

                await DispatchWithoutUiContext(context,
                    new StubDestination(nameof(WellKnownDestinations.FileNoDialog)),
                    new StubDestination(EditorDestination.DESIGNATION));
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
