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
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Plugin.RecipeEditor.ViewModels;
using Greenshot.Recipes;
using Greenshot.Triggers;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeManagerAndActivationTests
    {
        public RecipeManagerAndActivationTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void BuiltInRecipes_DefaultToEnabled()
        {
            var manager = new RecipeManager();
            var regionRecipe = manager.GetRecipeById(RecipeManager.RecipeIdRegion);

            Assert.NotNull(regionRecipe);
            Assert.True(regionRecipe.IsEnabled);
            Assert.True(manager.IsRecipeEnabled(RecipeManager.RecipeIdRegion));
        }

        [Fact]
        public void SetRecipeEnabled_TogglesStateAndPersistsToConfig()
        {
            var config = IniConfigHelper.EnsureSection<IRecipeConfiguration>(() => new RecipeConfigurationImpl());
            config.DisabledRecipeIds = "";

            var manager = new RecipeManager();
            string testRecipeId = RecipeManager.RecipeIdWindow;

            Assert.True(manager.IsRecipeEnabled(testRecipeId));

            // Deactivate
            manager.SetRecipeEnabled(testRecipeId, false);
            Assert.False(manager.IsRecipeEnabled(testRecipeId));
            var windowRecipe = manager.GetRecipeById(testRecipeId);
            Assert.NotNull(windowRecipe);
            Assert.False(windowRecipe.IsEnabled);
            var disabledList = (config.DisabledRecipeIds ?? "").Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Assert.Contains(testRecipeId, disabledList);

            // Re-activate
            manager.SetRecipeEnabled(testRecipeId, true);
            Assert.True(manager.IsRecipeEnabled(testRecipeId));
            Assert.True(windowRecipe.IsEnabled);
            disabledList = (config.DisabledRecipeIds ?? "").Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Assert.DoesNotContain(testRecipeId, disabledList);
        }

        [Fact]
        public void RegisterRecipe_HonorsExistingDisabledStateInConfig()
        {
            var config = IniConfigHelper.EnsureSection<IRecipeConfiguration>(() => new RecipeConfigurationImpl());
            string customRecipeId = "test_custom_disabled_" + Guid.NewGuid().ToString("N");
            config.DisabledRecipeIds = customRecipeId;

            var manager = new RecipeManager();
            var recipe = new CaptureRecipe(customRecipeId, "Test Disabled Recipe");
            recipe.IsEnabled = true; // initially true

            manager.RegisterRecipe(recipe);

            var registered = manager.GetRecipeById(customRecipeId);
            Assert.NotNull(registered);
            Assert.False(registered.IsEnabled, "Registered recipe should be marked disabled if its ID is in DisabledRecipeIds");
            Assert.False(manager.IsRecipeEnabled(customRecipeId));

            // Clean up
            manager.UnregisterRecipe(customRecipeId);
            config.DisabledRecipeIds = "";
        }

        [Fact]
        public void UnregisterRecipe_RemovesSourceFileFromRecipeFiles()
        {
            var config = IniConfigHelper.EnsureSection<IRecipeConfiguration>(() => new RecipeConfigurationImpl());
            string dummyPath = @"C:\greenshot_tests\my_custom_recipe.json";
            config.RecipeFiles = dummyPath;

            var manager = new RecipeManager();
            string customRecipeId = "test_unregister_" + Guid.NewGuid().ToString("N");
            var recipe = new CaptureRecipe(customRecipeId, "Test Unregister Recipe")
            {
                FilePath = dummyPath
            };

            manager.RegisterRecipe(recipe);
            Assert.NotNull(manager.GetRecipeById(customRecipeId));

            bool unregistered = manager.UnregisterRecipe(customRecipeId);
            Assert.True(unregistered);
            Assert.Null(manager.GetRecipeById(customRecipeId));
            var configuredFiles = (config.RecipeFiles ?? "").Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Assert.DoesNotContain(dummyPath, configuredFiles);
        }

        [Fact]
        public void TriggerManager_SyncRecipeTriggers_SkipsDisabledRecipes()
        {
            var triggerManager = new TriggerManager();
            var enabledRecipe = new CaptureRecipe("enabled_rec", "Enabled Recipe")
            {
                IsEnabled = true
            };
            enabledRecipe.Triggers.Add(TriggerConfig.CreateEditor("Enabled Editor", "Test"));

            var disabledRecipe = new CaptureRecipe("disabled_rec", "Disabled Recipe")
            {
                IsEnabled = false
            };
            disabledRecipe.Triggers.Add(TriggerConfig.CreateEditor("Disabled Editor", "Test"));

            triggerManager.SyncRecipeTriggers(new[] { enabledRecipe, disabledRecipe });

            var registeredTriggers = triggerManager.GetAllTriggers().ToList();
            Assert.Contains(registeredTriggers, t => t.TargetRecipeId == "enabled_rec");
            Assert.DoesNotContain(registeredTriggers, t => t.TargetRecipeId == "disabled_rec");
        }

        [Fact]
        public void RecipeManagerViewModel_FiltersRecipesCorrectly()
        {
            var manager = new RecipeManager();
            var vm = new RecipeManagerViewModel(manager);

            // Initially All
            Assert.Equal("All", vm.SelectedFilterCategory);
            Assert.NotEmpty(vm.FilteredRecipes);
            int totalCount = vm.TotalCount;
            Assert.True(totalCount > 0);

            // Filter Active
            vm.SelectedFilterCategory = "Active";
            Assert.All(vm.FilteredRecipes, r => Assert.True(r.IsEnabled));

            // Deactivate one recipe to test Deactivated filter
            var firstItem = vm.AllRecipes.First();
            string deactivatedId = firstItem.Id;
            manager.SetRecipeEnabled(deactivatedId, false);
            vm.LoadRecipes();

            vm.SelectedFilterCategory = "Deactivated";
            Assert.Contains(vm.FilteredRecipes, r => r.Id == deactivatedId);
            Assert.All(vm.FilteredRecipes, r => Assert.False(r.IsEnabled));

            // Built-in filter
            vm.SelectedFilterCategory = "BuiltIn";
            Assert.All(vm.FilteredRecipes, r => Assert.True(r.IsBuiltIn));

            // Search filter
            vm.SelectedFilterCategory = "All";
            vm.SearchText = firstItem.Name;
            Assert.Contains(vm.FilteredRecipes, r => r.Id == deactivatedId);

            // Reset
            manager.SetRecipeEnabled(deactivatedId, true);
        }

        [Fact]
        public void RecipeManagerViewModel_ToggleCommand_FlipsEnabledState()
        {
            var manager = new RecipeManager();
            var vm = new RecipeManagerViewModel(manager);
            var item = vm.AllRecipes.First(r => r.IsBuiltIn);

            bool initial = item.IsEnabled;
            item.ToggleActiveCommand.Execute(null);

            Assert.NotEqual(initial, item.IsEnabled);
            Assert.Equal(item.IsEnabled, manager.IsRecipeEnabled(item.Id));

            // Toggle back
            item.ToggleActiveCommand.Execute(null);
            Assert.Equal(initial, item.IsEnabled);
            Assert.Equal(initial, manager.IsRecipeEnabled(item.Id));
        }
    }
}
