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
using System.Linq;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Recipes;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// Triggers which read the general settings: the hotkeys of the built-in recipes ("${config.RegionHotkey}")
    /// and a trigger's 'when'
    /// </summary>
    public class RecipeTriggerSettingTests
    {
        public RecipeTriggerSettingTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData(RecipeManager.RecipeIdRegion, "${config.RegionHotkey}")]
        [InlineData(RecipeManager.RecipeIdWindow, "${config.WindowHotkey}")]
        [InlineData(RecipeManager.RecipeIdActiveWindow, "${config.WindowHotkey}")]
        [InlineData(RecipeManager.RecipeIdFullScreen, "${config.FullscreenHotkey}")]
        [InlineData(RecipeManager.RecipeIdLastRegion, "${config.LastregionHotkey}")]
        [InlineData(RecipeManager.RecipeIdClipboard, "${config.ClipboardHotkey}")]
        public void BuiltInRecipe_HasTheHotkeyOfTheSettings(string recipeId, string hotkey)
        {
            var recipe = new RecipeManager().GetRecipeById(recipeId);

            var trigger = Assert.Single(recipe.Triggers, t => t.TriggerType == TriggerConfig.TypeHotkey);
            Assert.Equal(hotkey, trigger.GetParameter<string>(TriggerConfig.HotkeyParameter));
            var result = RecipeValidator.Validate(recipe);
            Assert.DoesNotContain(result.Errors, e => e.Contains("Hotkey trigger") || e.Contains("Trigger '"));
        }

        [Fact]
        public void WindowHotkey_StartsOneOfTheWindowRecipes()
        {
            var manager = new RecipeManager();
            string interactive = manager.GetRecipeById(RecipeManager.RecipeIdWindow).Triggers.Single().When;
            string active = manager.GetRecipeById(RecipeManager.RecipeIdActiveWindow).Triggers.Single().When;

            Assert.Equal("${config.CaptureWindowsInteractive}", interactive);
            Assert.Equal("${config.CaptureWindowsInteractive == false}", active);
        }

        [Fact]
        public void SettingHotkey_IsUnusedOnlyWhenItsRecipesAreOff()
        {
            var manager = new RecipeManager();
            Assert.False(manager.IsSettingHotkeyUnused("RegionHotkey"));
            Assert.False(manager.IsSettingHotkeyUnused("NoRecipeUsesThis"));

            manager.GetRecipeById(RecipeManager.RecipeIdRegion).IsEnabled = false;
            Assert.True(manager.IsSettingHotkeyUnused("RegionHotkey"));
            Assert.False(manager.IsSettingHotkeyUnused("FullscreenHotkey"));
        }

        [Theory]
        [InlineData("${config.NoSuchSetting}", true)]
        [InlineData("${config.CaptureWindowsInteractive}", true)]
        [InlineData("${config.RegionHotkey}", false)]
        public void Validator_TakesOnlyTextSettingsAsHotkey(string hotkey, bool error)
        {
            var recipe = CreateRecipe("trigger_setting_hotkey");
            recipe.AddTrigger(TriggerConfig.CreateHotkey(hotkey));

            var result = RecipeValidator.Validate(recipe);

            Assert.Equal(error, result.Errors.Any(e => e.Contains("text setting of the general settings")));
        }

        [Fact]
        public void TriggerWhen_DecidesWhetherTheTriggerIsRegistered()
        {
            var recipe = CreateRecipe("trigger_setting_when");
            var trigger = TriggerConfig.CreateEditor("Test");
            trigger.When = "${option.on}";
            recipe.AddTrigger(trigger);

            Assert.False(trigger.IsWanted(recipe));
            RecipeOptionStore.SetValue(recipe.Id, recipe.FindOption("on"), true);
            try
            {
                Assert.True(trigger.IsWanted(recipe));
            }
            finally
            {
                RecipeOptionStore.Reset(recipe.Id);
            }

            trigger.Enabled = false;
            Assert.False(trigger.IsWanted(recipe));
        }

        [Fact]
        public void Validator_ChecksTheWhenOfATrigger()
        {
            var recipe = CreateRecipe("trigger_setting_when_check");
            var trigger = TriggerConfig.CreateEditor("Test");
            trigger.When = "option.on";
            recipe.AddTrigger(trigger);
            var other = TriggerConfig.CreateEditor("Other");
            other.When = "${option.missing}";
            recipe.AddTrigger(other);

            var result = RecipeValidator.Validate(recipe);

            Assert.Contains(result.Errors, e => e.Contains("'when' must be an expression"));
            Assert.Contains(result.Errors, e => e.Contains("'missing'"));
        }

        private static CaptureRecipe CreateRecipe(string id)
        {
            RecipeOptionStore.Reset(id);
            var recipe = new CaptureRecipe(id, "Trigger settings")
            {
                Options = new List<RecipeOption> { new RecipeOption { Key = "on", Type = ContractDataType.Boolean, DefaultValue = false } }
            };
            recipe.AddNode(new RecipeNodeConfig("source", "Source") { Parameters = new Dictionary<string, object> { { "SourceType", "FullScreen" } } });
            recipe.Flow = new RecipeFlowConfig("source");
            return recipe;
        }
    }
}
