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
using Greenshot.Base.Core;
using Greenshot.Base.Drawing;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.ExternalCommand;
using Greenshot.Plugin.RecipeEditor;
using Greenshot.Plugin.Zxing;
using Greenshot.Recipes;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeConfigurationAndEditorTests
    {
        private class TestStepProvider : IRecipeStepProvider
        {
            public bool RegisterStepsCalled { get; private set; }

            public void RegisterSteps(IStepRegistry registry)
            {
                RegisterStepsCalled = true;
            }
        }

        private class TestDrawableProvider : IRecipeDrawableProvider
        {
            public bool RegisterDrawablesCalled { get; private set; }

            public void RegisterDrawables(IRecipeDrawableRegistry registry)
            {
                RegisterDrawablesCalled = true;
            }

            public string GetDrawableSchemaJson() => null;
        }

        public RecipeConfigurationAndEditorTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void RecipeConfigHelper_ReflectsConfigurationState()
        {
            var config = new RecipeConfigurationImpl
            {
                Enabled = false
            };
            Assert.False(RecipeConfigHelper.IsRecipeFeatureEnabled(config));

            config.Enabled = true;
            Assert.True(RecipeConfigHelper.IsRecipeFeatureEnabled(config));

            Assert.False(RecipeConfigHelper.IsRecipeFeatureEnabled((IRecipeConfiguration)null));
        }

        [Fact]
        public void StepRegistry_SkipsProviderRegistration_WhenRecipeFeatureDisabled()
        {
            var recipeConfig = IniConfigHelper.EnsureSection<IRecipeConfiguration>(() => new RecipeConfigurationImpl());
            recipeConfig.Enabled = false;

            var provider = new TestStepProvider();
            StepRegistry.Instance.RegisterProvider(provider);

            Assert.False(provider.RegisterStepsCalled, "Step provider registration should be skipped when recipe feature is disabled");

            recipeConfig.Enabled = true;
            StepRegistry.Instance.RegisterProvider(provider);
            Assert.True(provider.RegisterStepsCalled, "Step provider registration should execute when recipe feature is enabled");
        }

        [Fact]
        public void RecipeDrawableRegistry_SkipsProviderRegistration_WhenRecipeFeatureDisabled()
        {
            var recipeConfig = IniConfigHelper.EnsureSection<IRecipeConfiguration>(() => new RecipeConfigurationImpl());
            recipeConfig.Enabled = false;

            var provider = new TestDrawableProvider();
            RecipeDrawableRegistry.Instance.RegisterProvider(provider);

            Assert.False(provider.RegisterDrawablesCalled, "Drawable provider registration should be skipped when recipe feature is disabled");

            recipeConfig.Enabled = true;
            RecipeDrawableRegistry.Instance.RegisterProvider(provider);
            Assert.True(provider.RegisterDrawablesCalled, "Drawable provider registration should execute when recipe feature is enabled");
        }

        [Fact]
        public void BuiltInRecipes_CanExecuteWithoutRecipeEditorPlugin()
        {
            var manager = new RecipeManager();
            var regionRecipe = manager.GetRecipeById(RecipeManager.RecipeIdRegion);

            Assert.NotNull(regionRecipe);
            Assert.Equal(RecipeManager.RecipeIdRegion, regionRecipe.Id);
            Assert.NotEmpty(regionRecipe.Nodes);
            Assert.NotNull(regionRecipe.Flow);
        }

        [Fact]
        public void RecipeEditorPlugin_RegistersEditorService()
        {
            var plugin = new RecipeEditorPlugin();
            var locator = new SimpleServiceProvider();
            plugin.RegisterServices(locator);

            var editorService = locator.GetInstance<IRecipeEditorService>(isOptional: true);
            Assert.NotNull(editorService);

            plugin.Shutdown();
        }

        [Fact]
        public void DynamicDestinationStep_ConfiguresCorrectlyInStepNodeViewModel()
        {
            var config = RecipeStepConfig.CreateDynamicDestination("dyn_dest", "Select Destination", true, true, new[] { "Clipboard", "Editor" }, 15);
            var vm = new Greenshot.Plugin.RecipeEditor.ViewModels.StepNodeViewModel(config, new System.Windows.Point(0, 0));

            Assert.True(vm.IsDynamicDestination);
            Assert.Equal("Select Destination", vm.DynamicDestinationTitle);
            Assert.True(vm.DynamicDestinationShowPreview);
            Assert.True(vm.DynamicDestinationAllowRecipeForwarding);
            Assert.Equal(15, vm.DynamicDestinationTimeoutSeconds);
            Assert.Contains("Clipboard", vm.DynamicDestinationSpecificDestinations);
            Assert.Contains("Editor", vm.DynamicDestinationSpecificDestinations);
            Assert.Contains("15s", vm.Summary);
        }

        [Fact]
        public void StepNodeViewModel_StepErrorHandling_UpdatesConfigCorrectly()
        {
            var config = RecipeStepConfig.CreateClipboard("clipboard_step");
            var vm = new Greenshot.Plugin.RecipeEditor.ViewModels.StepNodeViewModel(config, new System.Windows.Point(0, 0));

            // Default
            Assert.Equal("Default", vm.OnErrorAction);
            Assert.Null(config.OnErrorNodeId);
            Assert.Null(config.OnErrorRecipeId);

            // Set to Step
            vm.OnErrorAction = "Step";
            vm.OnErrorNodeId = "dyn_dest";
            Assert.Equal("dyn_dest", config.OnErrorNodeId);
            Assert.Null(config.OnErrorRecipeId);
            Assert.True(vm.IsOnErrorStep);
            Assert.Contains("dyn_dest", vm.OnErrorSummary);

            // Set to Recipe
            vm.OnErrorAction = "Recipe";
            vm.OnErrorRecipeId = "rec_recovery";
            Assert.Equal("rec_recovery", config.OnErrorRecipeId);
            Assert.Null(config.OnErrorNodeId);
            Assert.True(vm.IsOnErrorRecipe);
            Assert.Contains("rec_recovery", vm.OnErrorSummary);
        }

        [Fact]
        public void RecipeEditorViewModel_ManagesErrorTransitionsAndDynamicDestination()
        {
            var editorVm = new Greenshot.Plugin.RecipeEditor.ViewModels.RecipeEditorViewModel();
            editorVm.NewRecipe();

            // Add DynamicDestination step
            editorVm.AddStep(WellKnownStepTypes.DynamicDestination);
            var dynStep = System.Linq.Enumerable.FirstOrDefault(editorVm.Nodes, n => n.IsDynamicDestination);
            Assert.NotNull(dynStep);
            Assert.Equal("Export Capture", dynStep.DynamicDestinationTitle);

            // Add error transition
            editorVm.AddErrorTransition();
            Assert.Single(editorVm.ErrorTransitions);

            var errItem = editorVm.ErrorTransitions[0];
            errItem.FromNodeId = "acquire";
            errItem.TargetType = "Step";
            errItem.ToNodeId = dynStep.Id;
            errItem.ErrorType = "ClipboardException";

            editorVm.SyncRecipeTransitions();

            Assert.NotNull(editorVm.ActiveRecipe.Flow.ErrorTransitions);
            Assert.Single(editorVm.ActiveRecipe.Flow.ErrorTransitions);
            var savedTransition = editorVm.ActiveRecipe.Flow.ErrorTransitions[0];
            Assert.Equal("acquire", savedTransition.From);
            Assert.Equal(dynStep.Id, savedTransition.To);
            Assert.Equal("ClipboardException", savedTransition.ErrorType);
        }
    }
}
