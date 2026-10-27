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

#nullable disable

using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Recipes.Steps;
using SkiaSharp;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Tests.Recipes
{
    /// <summary>
    /// A capture handed to a recipe (forwarded from the destination picker, imported from the browser) is used as a whole:
    /// no new capture, no selection overlay on top of it.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class PreSuppliedPayloadTests
    {
        public PreSuppliedPayloadTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static CaptureFlowContext CreateContext(string source, bool preSupplied, string ocrText = null)
        {
            var payload = FakeRecipeHost.Payload();
            if (source != null)
            {
                payload.Metadata["source"] = source;
            }
            if (ocrText != null)
            {
                payload.Metadata[OcrStep.OcrLinesKey] = new List<OcrLine> { new OcrLine(ocrText, new SKRectI(5, 5, 65, 25), Array.Empty<OcrWord>()) };
            }
            return new CaptureFlowContext(new CaptureRecipe("presupplied_test", "Pre-supplied test"))
            {
                Payload = payload,
                IsPayloadPreSupplied = preSupplied
            };
        }

        private static InteractiveSelectionStep CreateSelectionStep(CaptureMode mode)
        {
            return new InteractiveSelectionStep(RecipeStepConfig.CreateSelection("select", mode, allowWindowSnapping: false));
        }

        [Theory]
        [InlineData(CaptureMode.Region)]
        [InlineData(CaptureMode.Text)]
        public async Task Selection_OnPreSuppliedPayload_IsSkipped(CaptureMode mode)
        {
            using var scope = new HostScope();
            using var context = CreateContext("Screen", preSupplied: true);

            await CreateSelectionStep(mode).ExecuteAsync(context);

            Assert.Equal(0, scope.Host.Selections);
            Assert.False(context.IsAborted);
            Assert.Equal(100, context.Payload.RawCapture.Image.Width);
        }

        [Theory]
        [InlineData("Clipboard")]
        [InlineData("file")]
        [InlineData("Window")]
        public async Task Selection_OnNonScreenCapture_IsSkipped(string source)
        {
            using var scope = new HostScope();
            using var context = CreateContext(source, preSupplied: false);

            await CreateSelectionStep(CaptureMode.Region).ExecuteAsync(context);

            Assert.Equal(0, scope.Host.Selections);
            Assert.False(context.IsAborted);
        }

        [Fact]
        public async Task Selection_OnScreenCapture_ShowsTheSelector()
        {
            using var scope = new HostScope();
            using var context = CreateContext("Screen", preSupplied: false);

            await CreateSelectionStep(CaptureMode.Region).ExecuteAsync(context);

            Assert.Equal(1, scope.Host.Selections);
            Assert.Equal(10, context.Payload.RawCapture.Image.Width);
        }

        [Fact]
        public async Task TextSelection_OnPreSuppliedPayload_ExtractsTheTextOfTheWholeImage()
        {
            using var scope = new HostScope();
            using var context = CreateContext("Screen", preSupplied: true, ocrText: "Hello Greenshot");

            await CreateSelectionStep(CaptureMode.Text).ExecuteAsync(context);

            Assert.Equal(0, scope.Host.Selections);
            Assert.Equal("Hello Greenshot", context.Payload.ExtractedText);
        }

        [Fact]
        public async Task Source_WithPayloadAlreadySet_MarksItAsPreSupplied()
        {
            using var context = CreateContext("Screen", preSupplied: false);
            var payload = context.Payload;

            await new SourceAcquisitionStep(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.TextOcr, captureMouse: false)).ExecuteAsync(context);

            Assert.True(context.IsPayloadPreSupplied);
            Assert.Same(payload, context.Payload);
        }

        [Fact]
        public void BranchContext_KeepsThePreSuppliedFlag()
        {
            using var context = CreateContext("Screen", preSupplied: true);
            var branch = context.CreateBranchContext();
            Assert.True(branch.IsPayloadPreSupplied);
        }

        [Fact]
        public void OcrRecipe_PutsTheTextOnTheClipboard()
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdOcr);
            var export = recipe.FindNode("export");

            Assert.Equal(WellKnownStepTypes.Clipboard, export.StepType);
            Assert.Equal("TextOnly", export.GetParameter<string>("ClipboardMode"));
        }
    }
}
