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

using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Capturing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Sources;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// Selecting on the live screen (freezing turned off): nothing is captured before the selection, the selection step captures what was selected.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class LiveSelectionTests
    {
        private static readonly NativeRect ScreenBounds = new NativeRect(-100, -50, 400, 300);

        public LiveSelectionTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private sealed class FixedSelector : IInteractiveCaptureSelector
        {
            private readonly NativeRect _region;

            public FixedSelector(NativeRect region)
            {
                _region = region;
            }

            public ICapture Capture { get; private set; }

            public bool IsSelecting => false;

            public void BringToFront()
            {
            }

            public Task<SelectionResult> SelectAsync(ICapture fullscreenCapture, IReadOnlyList<IInteropWindow> visibleWindows, CaptureMode initialMode, string initialTool, CancellationToken cancellationToken = default)
            {
                Capture = fullscreenCapture;
                return Task.FromResult(new SelectionResult { SelectedRegion = _region, FinalMode = initialMode });
            }
        }

        private sealed class FakeBackend : IScreenCaptureBackend
        {
            public string Name => "Fake";
            public bool IsAvailable => true;
            public bool CapturesWindowContentOnly => false;
            public Task<Bitmap> CaptureRectangleAsync(NativeRect captureBounds, CancellationToken cancellationToken = default) => Task.FromResult<Bitmap>(null);
            public Task<Bitmap> CaptureWindowAsync(IInteropWindow window, CancellationToken cancellationToken = default) => Task.FromResult<Bitmap>(null);
        }

        /// <summary>
        /// Records the captured areas, null makes the capture fail
        /// </summary>
        private sealed class AreaCapture
        {
            private readonly bool _fails;

            public AreaCapture(bool fails = false)
            {
                _fails = fails;
            }

            public List<NativeRect> Areas { get; } = new List<NativeRect>();

            public Task<ScreenCaptureResult> CaptureAsync(NativeRect area, CancellationToken cancellationToken)
            {
                Areas.Add(area);
                return Task.FromResult(_fails ? null : new ScreenCaptureResult(new Bitmap(area.Width, area.Height), area.Location, new FakeBackend()));
            }
        }

        private sealed class FoundFeature : IDetectedFeature
        {
            public NativeRect Bounds => new NativeRect(1, 2, 3, 4);
            public string FeatureType => "Found";
            public string Text => "found";
            public string ToolTipText => "found";

            public void Offset(int x, int y)
            {
            }
        }

        private static CaptureFlowContext CreateLiveContext()
        {
            var capture = new Capture { ScreenBounds = ScreenBounds, Location = ScreenBounds.Location, CursorLocation = new NativePoint(50, 60) };
            capture.CaptureDetails.AddMetaData("source", "Screen");
            return new CaptureFlowContext(new CaptureRecipe("live_test", "Live test"))
            {
                Payload = new CapturePayload(capture)
            };
        }

        private static Task SelectAsync(CaptureFlowContext context, NativeRect region, AreaCapture areaCapture) =>
            new InteractiveSelectionStep(RecipeStepConfig.CreateSelection("select", CaptureMode.Region, allowWindowSnapping: false), new FixedSelector(region), areaCapture.CaptureAsync)
                .ExecuteAsync(context);

        [Fact]
        public async Task LiveSelection_CapturesOnlyTheSelection()
        {
            var areaCapture = new AreaCapture();
            using var context = CreateLiveContext();

            await SelectAsync(context, new NativeRect(10, 20, 30, 40), areaCapture);

            Assert.False(context.IsAborted);
            // In screen coordinates
            Assert.Equal(new[] { new NativeRect(-90, -30, 30, 40) }, areaCapture.Areas);
            var capture = context.Payload.RawCapture;
            Assert.Equal(30, capture.Image.Width);
            Assert.Equal(40, capture.Image.Height);
            // Placed like a crop of the whole screen
            Assert.Equal(new NativePoint(10, 20), capture.Location);
            Assert.Equal(new NativePoint(40, 40), capture.CursorLocation);
            Assert.Equal("Fake", capture.CaptureDetails.MetaData[ScreenCapture.CaptureMethodKey]);
        }

        [Fact]
        public async Task LiveSelection_WithoutASelection_CapturesTheWholeScreen()
        {
            var areaCapture = new AreaCapture();
            using var context = CreateLiveContext();

            await SelectAsync(context, NativeRect.Empty, areaCapture);

            Assert.False(context.IsAborted);
            Assert.Equal(new[] { ScreenBounds }, areaCapture.Areas);
            Assert.Equal(400, context.Payload.RawCapture.Image.Width);
            Assert.Equal(ScreenBounds.Location, context.Payload.RawCapture.Location);
        }

        [Fact]
        public async Task LiveSelection_WhenTheCaptureFails_IsAborted()
        {
            using var context = CreateLiveContext();

            await SelectAsync(context, new NativeRect(10, 20, 30, 40), new AreaCapture(fails: true));

            Assert.True(context.IsAborted);
            Assert.Null(context.Payload.RawCapture.Image);
        }

        [Fact]
        public async Task LiveSelection_AfterTheFrozenScreen_DropsWhatWasFoundOnIt()
        {
            using var context = CreateLiveContext();
            var details = context.Payload.RawCapture.CaptureDetails;
            details.Features.Add(new WindowFeature(InteropWindowQuery.GetDesktopWindow(), 0));
            details.Features.Add(new FoundFeature());
            // A processor of the frozen capture (before L) which is still running
            var processing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            details.ProcessingTask = processing.Task.ContinueWith(_ =>
            {
                lock (details.Features)
                {
                    details.Features.Add(new FoundFeature());
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            var selecting = SelectAsync(context, new NativeRect(10, 20, 30, 40), new AreaCapture());
            processing.SetResult(true);
            await selecting;

            Assert.False(context.IsAborted);
            var feature = Assert.Single(details.Features);
            Assert.IsType<WindowFeature>(feature);
        }

        [Fact]
        public async Task FrozenSelection_IsCropped_WithoutAnotherCapture()
        {
            var areaCapture = new AreaCapture();
            var capture = new Capture(new Bitmap(100, 50));
            capture.CaptureDetails.AddMetaData("source", "Screen");
            using var context = new CaptureFlowContext(new CaptureRecipe("frozen_test", "Frozen test")) { Payload = new CapturePayload(capture) };

            await SelectAsync(context, new NativeRect(10, 20, 30, 10), areaCapture);

            Assert.Empty(areaCapture.Areas);
            Assert.Equal(30, context.Payload.RawCapture.Image.Width);
        }

        [Fact]
        public async Task LiveScreenSource_TakesNoPixels()
        {
            using var context = new CaptureFlowContext(new CaptureRecipe("live_source_test", "Live source test"));

            var payload = await new LiveScreenCaptureSource().AcquireAsync(context);

            Assert.Null(payload.RawCapture.Image);
            Assert.Equal("Screen", payload.RawCapture.CaptureDetails.MetaData["source"]);
            Assert.False(payload.RawCapture.ScreenBounds.IsEmpty);
        }
    }
}
