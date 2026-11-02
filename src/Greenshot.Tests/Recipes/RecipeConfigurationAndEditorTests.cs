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

using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.RecipeEditor;
using Greenshot.Recipes;
using Xunit;
using System.Linq;

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
        }

        private class StepTypeProvider : IRecipeStepProvider
        {
            private readonly string _stepType;

            public StepTypeProvider(string stepType)
            {
                _stepType = stepType;
            }

            public void RegisterSteps(IStepRegistry registry)
            {
                registry.Register(new StepContract(_stepType), config => null);
            }
        }

        private class CountingStepProvider : IRecipeStepProvider
        {
            public int Calls { get; private set; }

            public void RegisterSteps(IStepRegistry registry)
            {
                Calls++;
            }
        }

        private class DrawableTypeProvider : IRecipeDrawableProvider
        {
            private readonly string _drawableType;

            public DrawableTypeProvider(string drawableType)
            {
                _drawableType = drawableType;
            }

            public void RegisterDrawables(IRecipeDrawableRegistry registry)
            {
                registry.RegisterDrawableFactory(_drawableType, (surface, parameters, context) => null);
                registry.RegisterDrawableConfigurator(_drawableType, (parameters, owner) => true);
            }
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
        public void StepRegistry_UnregisterProvider_RemovesOnlyItsStepTypes()
        {
            IniConfigHelper.EnsureSection<IRecipeConfiguration>(() => new RecipeConfigurationImpl()).Enabled = true;
            var registry = new StepRegistry();
            var disabledProvider = new StepTypeProvider("DisabledPluginStep");
            registry.RegisterProvider(disabledProvider);
            registry.RegisterProvider(new StepTypeProvider("OtherPluginStep"));
            registry.Register(new StepContract("CoreStep"), config => null);

            registry.UnregisterProvider(disabledProvider);

            Assert.False(registry.IsRegistered("DisabledPluginStep"));
            Assert.True(registry.IsRegistered("OtherPluginStep"));
            Assert.True(registry.IsRegistered("CoreStep"));
        }

        [Fact]
        public void StepRegistry_RegistersAProviderOnce_UntilItIsUnregistered()
        {
            IniConfigHelper.EnsureSection<IRecipeConfiguration>(() => new RecipeConfigurationImpl()).Enabled = true;
            var registry = new StepRegistry();
            var provider = new CountingStepProvider();

            registry.RegisterProvider(provider);
            registry.RegisterProvider(provider);
            Assert.Equal(1, provider.Calls);

            registry.UnregisterProvider(provider);
            registry.RegisterProvider(provider);
            Assert.Equal(2, provider.Calls);
        }

        [Fact]
        public void RecipeDrawableRegistry_UnregisterProvider_RemovesOnlyItsDrawableTypes()
        {
            IniConfigHelper.EnsureSection<IRecipeConfiguration>(() => new RecipeConfigurationImpl()).Enabled = true;
            var registry = new RecipeDrawableRegistry();
            var disabledProvider = new DrawableTypeProvider("DisabledPluginDrawable");
            registry.RegisterProvider(disabledProvider);
            registry.RegisterProvider(new DrawableTypeProvider("OtherPluginDrawable"));

            registry.UnregisterProvider(disabledProvider);

            Assert.False(registry.IsRegistered("DisabledPluginDrawable"));
            Assert.False(registry.CanConfigureDrawable("DisabledPluginDrawable"));
            Assert.True(registry.IsRegistered("OtherPluginDrawable"));
            Assert.True(registry.CanConfigureDrawable("OtherPluginDrawable"));
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
            var services = new Greenshot.Tests.Plugins.TestPluginServices();
            plugin.ConfigureServices(services);

            var editorService = services.GetServices<IRecipeEditorService>().SingleOrDefault();
            Assert.NotNull(editorService);
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
        public void StepNodeViewModel_OnError_IsTheErrorTransitionOfTheStep()
        {
            var editorVm = new Greenshot.Plugin.RecipeEditor.ViewModels.RecipeEditorViewModel(RecipeManager.Instance);
            try
            {
                editorVm.NewRecipe();
                var vm = editorVm.Nodes.First(n => n.Id == "export");
                Assert.Equal("Default", vm.OnErrorAction);
                Assert.Empty(editorVm.ActiveRecipe.Flow.ErrorTransitions);

                // A step: one error transition for any error
                vm.OnErrorAction = "Step";
                vm.OnErrorNodeId = "feedback";
                var transition = Assert.Single(editorVm.ActiveRecipe.Flow.ErrorTransitions);
                Assert.Equal(("export", "feedback", (string)null, "*"), (transition.From, transition.To, transition.Recipe, transition.OnError));
                Assert.True(vm.IsOnErrorStep);
                Assert.Single(editorVm.ErrorTransitions);

                // A recipe: the same transition, now to the recipe
                vm.OnErrorAction = "Recipe";
                vm.OnErrorRecipeId = "rec_recovery";
                transition = Assert.Single(editorVm.ActiveRecipe.Flow.ErrorTransitions);
                Assert.Equal(("export", (string)null, "rec_recovery"), (transition.From, transition.To, transition.Recipe));
                Assert.True(vm.IsOnErrorRecipe);
                Assert.Null(vm.OnErrorNodeId);

                // Changed in the list of error transitions: the step shows it
                editorVm.ErrorTransitions[0].TargetType = "Step";
                editorVm.ErrorTransitions[0].ToNodeId = "acquire";
                Assert.Equal("acquire", vm.OnErrorNodeId);

                // Default removes it
                vm.OnErrorAction = "Default";
                Assert.Empty(editorVm.ActiveRecipe.Flow.ErrorTransitions);
                Assert.Empty(editorVm.ErrorTransitions);
            }
            finally
            {
                editorVm.Detach();
            }
        }

        [Fact]
        public void RecipeEditorViewModel_UndoAndRedo_RestoreTheChangeAndItsApprovalNotice()
        {
            var editorVm = new Greenshot.Plugin.RecipeEditor.ViewModels.RecipeEditorViewModel(RecipeManager.Instance);
            try
            {
                editorVm.NewRecipe();
                editorVm.RefreshUnsavedState();
                Assert.False(editorVm.CanUndo);
                Assert.DoesNotContain("clipboard", editorVm.ApprovalNotice ?? "");

                // A trigger which starts the recipe on its own: saving will ask
                editorVm.AddTrigger("Clipboard");
                editorVm.RefreshUnsavedState();
                editorVm.RefreshUnsavedState();
                Assert.True(editorVm.CanUndo);
                Assert.Contains("clipboard", editorVm.ApprovalNotice);

                editorVm.Undo();
                Assert.Empty(editorVm.ActiveRecipe.Triggers);
                Assert.DoesNotContain("clipboard", editorVm.ApprovalNotice ?? "");
                Assert.True(editorVm.CanRedo);

                editorVm.Redo();
                Assert.Single(editorVm.ActiveRecipe.Triggers);
                Assert.Contains("clipboard", editorVm.ApprovalNotice);
                Assert.True(editorVm.IsDirty);
            }
            finally
            {
                editorVm.Detach();
            }
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
            var savedTransition = editorVm.ActiveRecipe.Flow.ErrorTransitions.First();
            Assert.Equal("acquire", savedTransition.From);
            Assert.Equal(dynStep.Id, savedTransition.To);
            Assert.Equal("ClipboardException", savedTransition.OnError);
        }
    }
}
