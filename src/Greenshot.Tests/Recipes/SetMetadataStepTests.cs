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
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Hdr;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The "Set metadata" step decides the details written into saved images, see docs/vNext/capture-recipes.md
    /// </summary>
    public class SetMetadataStepTests
    {
        private static readonly ExportMetadataField[] SettingsFields = [ExportMetadataField.CaptureTime, ExportMetadataField.Author];

        public SetMetadataStepTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// Runs the step with the parameters and returns the details a file on this computer (private) or elsewhere would get,
        /// with the settings: capture time + author "Settings author", copyright "Settings copyright"
        /// </summary>
        private static async Task<(ExportImageMetadata Local, ExportImageMetadata Shared, CaptureFlowContext Context)> RunAsync(Dictionary<string, object> parameters)
        {
            var capture = new TestCapture(new Bitmap(20, 10));
            capture.CaptureDetails.CaptureMode = CaptureMode.Region;
            capture.CaptureDetails.Title = "Quarterly report - Editor";
            var context = new CaptureFlowContext(new CaptureRecipe("set_metadata", "Set Metadata"))
            {
                Payload = new CapturePayload(capture)
            };
            var node = new RecipeNodeConfig("details", WellKnownStepTypes.SetMetadata) { Parameters = parameters };
            await SingleNodeRunner.RunAsync(node, config => new SetMetadataStep(config), context);

            var captureDetails = context.Payload.RawCapture.CaptureDetails;
            return (ExportImageMetadata.ForExport(captureDetails, SettingsFields, "Settings author", "Settings copyright", includePrivate: true),
                ExportImageMetadata.ForExport(captureDetails, SettingsFields, "Settings author", "Settings copyright"),
                context);
        }

        [Fact]
        public async Task WithoutParameters_TheSettingsDecide()
        {
            var (local, _, context) = await RunAsync(new Dictionary<string, object>());
            using (context)
            {
                Assert.False(context.IsAborted);
                Assert.NotNull(local.CaptureTime);
                Assert.Equal("Settings author", local.Author);
                Assert.Equal("Settings copyright", local.Copyright);
                Assert.Null(local.Title);
            }
        }

        [Fact]
        public async Task Fields_ReplaceThoseOfTheSettings()
        {
            var (local, _, context) = await RunAsync(new Dictionary<string, object> { ["Fields"] = new List<string> { "Source" } });
            using (context)
            {
                Assert.Null(local.CaptureTime);
                Assert.Null(local.Author);
                Assert.Equal("Quarterly report - Editor", local.Title);
            }
        }

        [Fact]
        public async Task EmptyFields_WriteNoneButTheValuesOfTheStep()
        {
            var (local, _, context) = await RunAsync(new Dictionary<string, object> { ["Fields"] = new List<string>(), ["Copyright"] = "(c) Example" });
            using (context)
            {
                Assert.Null(local.CaptureTime);
                Assert.Null(local.Author);
                Assert.Equal("(c) Example", local.Copyright);
            }
        }

        [Fact]
        public async Task TitleAndDescription_AreExpressions_AndPrivate()
        {
            var (local, shared, context) = await RunAsync(new Dictionary<string, object>
            {
                ["Title"] = "Capture of ${payload.title}",
                ["Description"] = "Width ${payload.width}"
            });
            using (context)
            {
                Assert.Equal("Capture of Quarterly report - Editor", local.Title);
                Assert.Equal("Width 20", local.Description);
                Assert.True(local.HasPrivateDetails);

                // Only with the private details, which are asked for before the image leaves the computer
                Assert.Null(shared.Title);
                Assert.Null(shared.Description);
                Assert.False(shared.HasPrivateDetails);
            }
        }

        [Fact]
        public async Task AuthorAndCopyright_ReplaceThoseOfTheSettings_EmptyLeavesThemOut()
        {
            var (_, shared, context) = await RunAsync(new Dictionary<string, object> { ["Author"] = "Support team", ["Copyright"] = "" });
            using (context)
            {
                Assert.Equal("Support team", shared.Author);
                Assert.Null(shared.Copyright);
            }
        }

        [Fact]
        public async Task Author_FromAVariable()
        {
            using var context = new CaptureFlowContext(new CaptureRecipe("set_metadata", "Set Metadata"))
            {
                Payload = new CapturePayload(new TestCapture(new Bitmap(20, 10)))
            };
            context.Properties["team"] = "Support";
            var node = new RecipeNodeConfig("details", WellKnownStepTypes.SetMetadata)
            {
                Parameters = new Dictionary<string, object> { ["Author"] = "${team}" }
            };
            await SingleNodeRunner.RunAsync(node, config => new SetMetadataStep(config), context);

            var metadata = ExportImageMetadata.ForExport(context.Payload.RawCapture.CaptureDetails, [], null, null);
            Assert.Equal("Support", metadata.Author);
        }

        [Fact]
        public void ParseFields_RejectsUnknownNames()
        {
            Assert.Equal([ExportMetadataField.CaptureTime, ExportMetadataField.Text], ExportImageMetadata.ParseFields("CaptureTime, Text"));
            Assert.Empty(ExportImageMetadata.ParseFields(""));
            Assert.Throws<ArgumentException>(() => ExportImageMetadata.ParseFields("CaptureTime,Location"));
            Assert.Throws<ArgumentException>(() => ExportImageMetadata.ParseFields("captureTime"));
        }
    
        /// <summary>
        /// A capture without the screen (Capture reads the screen bounds)
        /// </summary>
        private sealed class TestCapture : ICapture
        {
            public TestCapture(Image image) => Image = image;

            public ICaptureDetails CaptureDetails { get; set; } = new CaptureDetails();
            public Image Image { get; set; }
            public HdrImage HdrImage { get; set; }
            public HdrParts HdrParts { get; set; }
            public void NullImage() => Image = null;
            public NativeRect ScreenBounds { get; set; }
            public CapturedCursor Cursor { get; set; }
            public bool CursorVisible { get; set; }
            public NativePoint CursorLocation { get; set; }
            public NativePoint Location { get; set; }
            public bool Crop(NativeRect cropRectangle) => false;
            public void MoveMouseLocation(int x, int y) { }
            public ICapture Clone() => new TestCapture((Image)Image?.Clone()) { CaptureDetails = CaptureDetails };
            public void Dispose() => Image?.Dispose();
        }
    }
}
