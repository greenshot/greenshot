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

using System.Collections.Generic;
using System.Threading.Tasks;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Steps;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Recipes
{
    /// <summary>
    /// The steps of the Linux version with the fake app: what they ask the app for and what they do with the image
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class StepTests
    {
        public StepTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static CaptureFlowContext Context(ICapturePayload payload = null) =>
            new(new CaptureRecipe("step_test", "Step test")) { Payload = payload };

        [Fact]
        public async Task Source_ActiveWindow_AsksTheAppForTheWindow()
        {
            using var scope = new HostScope();
            using var context = Context();

            await SingleNodeRunner.RunAsync(RecipeStepConfig.CreateSource(sourceType: CaptureSourceType.ActiveWindow), cfg => new SourceAcquisitionStep(cfg), context);

            Assert.Contains(scope.Host.Calls, c => c.StartsWith("window:"));
            Assert.Equal(80, context.Payload.RawCapture.Image.Width);
        }

        [Fact]
        public async Task Source_Region_TakesTheScreenForTheSelection()
        {
            using var scope = new HostScope();
            using var context = Context();

            await SingleNodeRunner.RunAsync(RecipeStepConfig.CreateSource(sourceType: CaptureSourceType.Region, captureMouse: false), cfg => new SourceAcquisitionStep(cfg), context);

            Assert.Contains("screen:True:False", scope.Host.Calls);
        }

        [Fact]
        public async Task Border_GrowsTheImageByTheWidthOnEachSide()
        {
            using var context = Context(FakeRecipeHost.Payload(100, 50));

            await SingleNodeRunner.RunAsync(RecipeStepConfig.CreateBorder(width: 5, color: "#FF0000"), cfg => new EffectCaptureStep(cfg), context);

            var image = context.Payload.Surface.Image;
            Assert.Equal(110, image.Width);
            Assert.Equal(60, image.Height);
            Assert.Equal(SKColors.Red, image.GetPixel(2, 30));
        }

        [Fact]
        public async Task Rotate_SwapsWidthAndHeight()
        {
            using var context = Context(FakeRecipeHost.Payload(100, 50));

            await SingleNodeRunner.RunAsync(RecipeStepConfig.CreateEffect(effect: "Rotate", parameters: new Dictionary<string, object> { ["Angle"] = 90 }),
                cfg => new EffectCaptureStep(cfg), context);

            Assert.Equal(50, context.Payload.Surface.Width);
            Assert.Equal(100, context.Payload.Surface.Height);
        }

        [Fact]
        public async Task CropAnnotation_CropsTheSurface()
        {
            using var context = Context(FakeRecipeHost.Payload(100, 50));

            await SingleNodeRunner.RunAsync(RecipeStepConfig.CreateAnnotation(annotationType: "Crop",
                    properties: new Dictionary<string, object> { ["Left"] = 10, ["Top"] = 5, ["Width"] = 40, ["Height"] = 20 }),
                cfg => new AnnotationStep(cfg), context);

            Assert.Equal(40, context.Payload.Surface.Width);
            Assert.Equal(20, context.Payload.Surface.Height);
            Assert.Empty(context.Payload.Surface.Elements);
        }

        [Fact]
        public async Task RectangleAnnotation_IsAddedToTheSurface()
        {
            using var context = Context(FakeRecipeHost.Payload(100, 50));

            await SingleNodeRunner.RunAsync(RecipeStepConfig.CreateAnnotation(annotationType: "Rectangle",
                    properties: new Dictionary<string, object> { ["Left"] = 10, ["Top"] = 5, ["Width"] = 40, ["Height"] = 20 }),
                cfg => new AnnotationStep(cfg), context);

            Assert.Single(context.Payload.Surface.Elements);
        }

        [Fact]
        public async Task Destinations_ExportTheFlattenedCaptureToTheDestination()
        {
            var host = new FakeRecipeHost();
            var destination = new StubDestination("Stub");
            host.DestinationList.Add(destination);
            using var scope = new HostScope(host);
            using var context = Context(FakeRecipeHost.Payload(100, 50));
            context.Payload.EnsureSurface().Add(new Greenshot.Linux.Editor.Elements.RectangleElement(), select: false);

            await SingleNodeRunner.RunAsync(RecipeStepConfig.CreateDestinations(destinationDesignations: ["Stub"]), cfg => new DestinationExportStep(cfg), context);

            var export = Assert.Single(destination.Exports);
            Assert.Equal(100, export.Image.Width);
            Assert.False(context.IsAborted);
        }

        [Fact]
        public async Task Editor_OpensThePayloadInTheApp()
        {
            var host = new FakeRecipeHost();
            host.DestinationList.Add(new StubDestination(Greenshot.Linux.Core.Destinations.WellKnownDestinations.Editor));
            using var scope = new HostScope(host);
            using var context = Context(FakeRecipeHost.Payload());

            await SingleNodeRunner.RunAsync(RecipeStepConfig.CreateEditor(), cfg => new DestinationExportStep(cfg), context);

            Assert.Contains(scope.Host.Calls, c => c.StartsWith("editor:"));
        }
    }
}
