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

using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Recipes;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
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

        private sealed class RecordingSelector : IInteractiveCaptureSelector
        {
            public int Calls { get; private set; }

            public string InitialTool { get; private set; }

            public ICaptureTool Tool { get; set; }

            public bool IsSelecting => false;

            public void BringToFront()
            {
            }

            public Task<SelectionResult> SelectAsync(ICapture fullscreenCapture, IReadOnlyList<IInteropWindow> visibleWindows, CaptureMode initialMode, string initialTool, CancellationToken cancellationToken = default)
            {
                Calls++;
                InitialTool = initialTool;
                return Task.FromResult(new SelectionResult { SelectedRegion = new NativeRect(0, 0, 10, 10), FinalMode = initialMode, Tool = Tool });
            }
        }

        private sealed class OwnImageTool : CaptureTool, ISelectionCaptureTool
        {
            public override CaptureMode Mode => CaptureMode.Window;

            public override string Id => "OwnImage";

            public Task<Bitmap> CaptureSelectionAsync(SelectionResult selection, NativeRect screenArea, IUiDispatcher ui, CancellationToken cancellationToken) =>
                Task.FromResult(new Bitmap(30, 20));
        }

        private static CaptureFlowContext CreateContext(string source, bool preSupplied, string ocrText = null)
        {
            var capture = new Capture(new Bitmap(100, 50));
            if (source != null)
            {
                capture.CaptureDetails.AddMetaData("source", source);
            }
            if (ocrText != null)
            {
                capture.CaptureDetails.Features.Add(new DetectedOcrLine(new NativeRect(5, 5, 60, 20), ocrText, new List<OcrWordInfo>()));
            }
            return new CaptureFlowContext(new CaptureRecipe("presupplied_test", "Pre-supplied test"))
            {
                Payload = new CapturePayload(capture),
                IsPayloadPreSupplied = preSupplied
            };
        }

        private static InteractiveSelectionStep CreateSelectionStep(RecordingSelector selector, CaptureMode mode)
        {
            return new InteractiveSelectionStep(RecipeStepConfig.CreateSelection("select", mode, allowWindowSnapping: false), selector);
        }

        [Theory]
        [InlineData(CaptureMode.Region)]
        [InlineData(CaptureMode.Text)]
        public async Task Selection_OnPreSuppliedPayload_IsSkipped(CaptureMode mode)
        {
            var selector = new RecordingSelector();
            using var context = CreateContext("Screen", preSupplied: true);

            await CreateSelectionStep(selector, mode).ExecuteAsync(context);

            Assert.Equal(0, selector.Calls);
            Assert.False(context.IsAborted);
            Assert.Equal(100, context.Payload.RawCapture.Image.Width);
        }

        [Theory]
        [InlineData("Clipboard")]
        [InlineData("file")]
        [InlineData("Window")]
        public async Task Selection_OnNonScreenCapture_IsSkipped(string source)
        {
            var selector = new RecordingSelector();
            using var context = CreateContext(source, preSupplied: false);

            await CreateSelectionStep(selector, CaptureMode.Region).ExecuteAsync(context);

            Assert.Equal(0, selector.Calls);
            Assert.False(context.IsAborted);
        }

        [Fact]
        public async Task Selection_OnScreenCapture_ShowsTheSelector()
        {
            var selector = new RecordingSelector();
            using var context = CreateContext("Screen", preSupplied: false);

            await CreateSelectionStep(selector, CaptureMode.Region).ExecuteAsync(context);

            Assert.Equal(1, selector.Calls);
            Assert.Equal(10, context.Payload.RawCapture.Image.Width);
        }

        [Fact]
        public async Task Selection_WithASelectionCaptureTool_UsesTheImageOfTheTool()
        {
            var selector = new RecordingSelector { Tool = new OwnImageTool() };
            using var context = CreateContext("Screen", preSupplied: false);
            var config = RecipeStepConfig.CreateSelection("select", CaptureMode.Region, allowWindowSnapping: false);
            config.Set("SelectionTool", "OwnImage");

            await new InteractiveSelectionStep(config, selector).ExecuteAsync(context);

            Assert.Equal("OwnImage", selector.InitialTool);
            Assert.False(context.IsAborted);
            Assert.Equal(30, context.Payload.RawCapture.Image.Width);
            Assert.False(context.Payload.RawCapture.CursorVisible);
        }

        [Fact]
        public async Task TextSelection_OnPreSuppliedPayload_ExtractsTheTextOfTheWholeImage()
        {
            var selector = new RecordingSelector();
            using var context = CreateContext("Screen", preSupplied: true, ocrText: "Hello Greenshot");

            await CreateSelectionStep(selector, CaptureMode.Text).ExecuteAsync(context);

            Assert.Equal(0, selector.Calls);
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
