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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Recipes;
using Greenshot.Recipes.Pipeline;
using Greenshot.Recipes.Steps;
using Greenshot.Recipes.Triggers;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// Which recipes a click on the tray icon can start (#1708), and the built-in recipes of the tray menu
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class TrayClickRecipesTests
    {
        private readonly StepRegistry _registry = new StepRegistry();

        public TrayClickRecipesTests()
        {
            TestEnvironment.EnsureInitialized();
            // The contracts of the steps, registered by a pipeline (its selector isn't used)
            _ = new CapturePipeline(new NoSelector(), stepRegistry: _registry);
        }

        [Theory]
        [InlineData(RecipeManager.RecipeIdRegion)]
        [InlineData(RecipeManager.RecipeIdFullScreen)]
        [InlineData(RecipeManager.RecipeIdWindow)]
        [InlineData(RecipeManager.RecipeIdClipboard)]
        [InlineData(RecipeManager.RecipeIdFile)]
        [InlineData(RecipeManager.RecipeIdOpenCaptureFolder)]
        [InlineData(RecipeManager.RecipeIdLastCaptureInEditor)]
        [InlineData(RecipeManager.RecipeIdTrayMenu)]
        [InlineData(RecipeManager.RecipeIdOpenSettings)]
        [InlineData(RecipeManager.RecipeIdEmptyEditor)]
        public void BuiltInRecipe_CanRunFromAClick(string recipeId)
        {
            var manager = new RecipeManager();
            var recipe = manager.GetRecipeById(recipeId);

            Assert.NotNull(recipe);
            Assert.True(TrayClickRecipes.IsSuitable(recipe, _registry), $"'{recipeId}' should be in the list");
            Assert.Contains(TrayClickRecipes.GetSuitable(manager, _registry), suitable => suitable.Id == recipeId);
        }

        [Theory]
        [InlineData(RecipeManager.RecipeIdExtension)]
        [InlineData(RecipeManager.RecipeIdAiCaptureRegion)]
        public void BuiltInRecipeForAnInput_IsNotInTheList(string recipeId)
        {
            var recipe = new RecipeManager().GetRecipeById(recipeId);

            Assert.NotNull(recipe);
            Assert.False(TrayClickRecipes.IsSuitable(recipe, _registry));
        }

        [Fact]
        public void NotificationRecipe_IsNotInTheList()
        {
            var recipe = new Greenshot.Notifications.NotificationRecipes().CreateRecipes().Single();

            Assert.False(TrayClickRecipes.IsSuitable(recipe, _registry));
        }

        [Fact]
        public void SwitchedOffRecipe_IsNotInTheList()
        {
            var recipe = OpenFolderRecipe("switched_off");
            recipe.IsEnabled = false;

            Assert.False(TrayClickRecipes.IsSuitable(recipe, _registry));
        }

        [Theory]
        [InlineData(TriggerConfig.TypeEditor)]
        [InlineData(TriggerConfig.TypeOpenFile)]
        [InlineData(TriggerConfig.TypeExtension)]
        [InlineData(TriggerConfig.TypeCommandline)]
        [InlineData(TriggerConfig.TypeAiTool)]
        [InlineData(TriggerConfig.TypeNotification)]
        public void RecipeOnlyForAnInputTrigger_IsNotInTheList(string triggerType)
        {
            var recipe = OpenFolderRecipe("input_only");
            recipe.Triggers.Add(new TriggerConfig { TriggerType = triggerType });

            Assert.False(TrayClickRecipes.IsSuitable(recipe, _registry));
        }

        [Fact]
        public void RecipeWithAnInputAndAHotkeyTrigger_IsInTheList()
        {
            var recipe = OpenFolderRecipe("input_and_hotkey");
            recipe.Triggers.Add(new TriggerConfig { TriggerType = TriggerConfig.TypeEditor });
            recipe.Triggers.Add(new TriggerConfig { TriggerType = TriggerConfig.TypeHotkey });

            Assert.True(TrayClickRecipes.IsSuitable(recipe, _registry));
        }

        [Fact]
        public void RecipeWithoutTriggers_IsInTheList()
        {
            Assert.True(TrayClickRecipes.IsSuitable(OpenFolderRecipe("no_triggers"), _registry));
        }

        [Fact]
        public void RecipeNeedingAVariableFromItsCaller_IsNotInTheList()
        {
            // A step which needs a variable no step before it sets: only a caller (e.g. a command line argument) can give it
            _registry.Register(new StepContract("NeedsCallerValue", inputVariables: new[] { new VariableContract("CallerValue", ContractDataType.String, true, "Given by the caller") }),
                config => throw new InvalidOperationException("Not expected"));
            var recipe = new CaptureRecipe("needs_variable", "Needs a variable").AddNode(new RecipeNodeConfig("step", "NeedsCallerValue"));
            recipe.Flow = new RecipeFlowConfig("step");

            Assert.False(TrayClickRecipes.IsSuitable(recipe, _registry));
        }

        [Fact]
        public void DisplayName_IsTheTextOfTheMenuItem()
        {
            var manager = new RecipeManager();

            Assert.Equal(Texts.Core.ContextmenuOpenrecentcapture, TrayClickRecipes.GetDisplayName(manager.GetRecipeById(RecipeManager.RecipeIdOpenCaptureFolder)));
            Assert.Equal(Texts.Core.ContextmenuShowtraymenu, TrayClickRecipes.GetDisplayName(manager.GetRecipeById(RecipeManager.RecipeIdTrayMenu)));
            Assert.Equal(Texts.Core.ContextmenuCapturearea, TrayClickRecipes.GetDisplayName(manager.GetRecipeById(RecipeManager.RecipeIdRegion)));
        }

        [Fact]
        public void RecipeWithoutImageSteps_GetsNoSourceOrDestination()
        {
            var recipe = OpenFolderRecipe("no_image");

            Assert.False(recipe.UsesImage(_registry));
        }

        [Fact]
        public void Prepare_LeavesARecipeWithoutImageStepsAsItIs()
        {
            // Like the pipeline registers it
            StepRegistry.Instance.Register<ShowInExplorerStep>(config => new ShowInExplorerStep(config));
            var recipe = OpenFolderRecipe("prepare_no_image");

            Assert.Same(recipe, TriggerRecipePreparer.Prepare(recipe, new ManualTrigger("Tray icon click", recipe.Id)));
        }

        [Fact]
        public void RecipeWithAnUnknownStep_UsesAnImage()
        {
            var recipe = new CaptureRecipe("unknown_step", "Unknown step").AddNode(new RecipeNodeConfig("x", "NotAKnownStep"));
            recipe.Flow = new RecipeFlowConfig("x");

            Assert.True(recipe.UsesImage(_registry));
        }

        [Fact]
        public void ShowInExplorer_FindExisting_FallsBackToTheFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "greenshot-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string file = Path.Combine(folder, "capture.png");
                File.WriteAllBytes(file, new byte[] { 1 });

                Assert.Equal(file, ShowInExplorerStep.FindExisting(file));
                Assert.Equal(folder, ShowInExplorerStep.FindExisting(Path.Combine(folder, "gone.png")));
                Assert.Null(ShowInExplorerStep.FindExisting(Path.Combine(folder, "gone", "gone.png")));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        private sealed class NoSelector : IInteractiveCaptureSelector
        {
            public bool IsSelecting => false;

            public void BringToFront()
            {
            }

            public Task<SelectionResult> SelectAsync(ICapture fullscreenCapture, IReadOnlyList<IInteropWindow> visibleWindows, CaptureMode initialMode, string initialTool, string fixedCaptureArea, CancellationToken cancellationToken = default) =>
                throw new InvalidOperationException("Not expected");
        }

        private static CaptureRecipe OpenFolderRecipe(string id)
        {
            var recipe = new CaptureRecipe(id, id).AddNode(new RecipeNodeConfig("show", WellKnownStepTypes.ShowInExplorer));
            recipe.Flow = new RecipeFlowConfig("show");
            return recipe;
        }
    }
}
