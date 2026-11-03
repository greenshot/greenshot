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
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The Hotkey and DirectoryPath options: a hotkey trigger which takes its hotkey from an option, a folder for a file step
    /// </summary>
    public class RecipeOptionTypeTests
    {
        private static readonly string CtrlShiftD = HotkeySequence.Parse("Ctrl + Shift + D").ToString();

        public RecipeOptionTypeTests()
        {
            TestEnvironment.EnsureInitialized();
            StepRegistry.Instance.Register<DestinationExportStep>(config => new DestinationExportStep(config));
        }

        [Theory]
        [InlineData("Ctrl + Shift + D", true)]
        [InlineData("", true)]
        [InlineData("None", true)]
        [InlineData("D", false)]
        [InlineData("not a hotkey", false)]
        public void HotkeyOption_TakesOnlyHotkeys(string text, bool valid)
        {
            var option = new RecipeOption { Key = "hotkey", Type = ContractDataType.Hotkey };
            Assert.Equal(valid, option.TryConvert(text, out var value));
            if (valid)
            {
                Assert.Equal(text == "Ctrl + Shift + D" ? CtrlShiftD : string.Empty, value);
            }
        }

        [Theory]
        [InlineData(@"C:\Screenshots", true)]
        [InlineData(@"C:/Screenshots", true)]
        [InlineData(@"\\server\share\Screenshots", true)]
        [InlineData("", true)]
        [InlineData(@"Screenshots", false)]
        [InlineData(@"\Screenshots", false)]
        [InlineData(@"C:Screenshots", false)]
        [InlineData(@"%GREENSHOT_NO_SUCH_VARIABLE%\Screenshots", false)]
        [InlineData("C:\\Screen|shots", false)]
        public void DirectoryOption_TakesOnlyFullPaths(string text, bool valid)
        {
            var option = new RecipeOption { Key = "folder", Type = ContractDataType.DirectoryPath };
            Assert.Equal(valid, option.TryConvert(text, out _));
        }

        [Fact]
        public void DirectoryOption_ReplacesVariables()
        {
            var option = new RecipeOption { Key = "folder", Type = ContractDataType.DirectoryPath, DefaultValue = @"%TEMP%\Greenshot" };
            Assert.Equal(Environment.ExpandEnvironmentVariables(@"%TEMP%\Greenshot"), option.GetDefaultValue());
        }

        [Fact]
        public void HotkeyTrigger_TakesTheHotkeyOfTheOption()
        {
            var recipe = CreateRecipe("option_type_hotkey");
            var trigger = recipe.Triggers[0];
            Assert.Equal(CtrlShiftD, trigger.GetHotkey(recipe));

            var option = recipe.FindOption("hotkey");
            RecipeOptionStore.SetValue(recipe.Id, option, "Ctrl + Alt + K");
            try
            {
                Assert.Equal(HotkeySequence.Parse("Ctrl + Alt + K").ToString(), trigger.GetHotkey(recipe));
                RecipeOptionStore.SetValue(recipe.Id, option, "None");
                Assert.Equal(string.Empty, trigger.GetHotkey(recipe));
            }
            finally
            {
                RecipeOptionStore.Reset(recipe.Id);
            }

            Assert.Equal("Ctrl + Shift + B", TriggerConfig.CreateHotkey("Ctrl + Shift + B").GetHotkey(recipe));
        }

        [Fact]
        public void Validator_AcceptsAHotkeyTriggerWithAHotkeyOption()
        {
            var result = RecipeValidator.Validate(CreateRecipe("option_type_valid"));
            Assert.True(result.IsValid, result.ToString());
        }

        [Theory]
        [InlineData("${option.missing}")]
        [InlineData("${option.folder}")]
        public void Validator_RejectsAHotkeyTriggerWithoutAHotkeyOption(string hotkey)
        {
            var recipe = CreateRecipe("option_type_wrong_hotkey");
            recipe.Triggers[0].SetParameter(TriggerConfig.HotkeyParameter, hotkey);

            var result = RecipeValidator.Validate(recipe);

            Assert.Contains(result.Errors, e => e.Contains("must be a Hotkey option"));
        }

        [Fact]
        public void Validator_RejectsADefaultWhichIsNoHotkey()
        {
            var recipe = CreateRecipe("option_type_bad_default");
            recipe.FindOption("hotkey").DefaultValue = "D";

            var result = RecipeValidator.Validate(recipe);

            Assert.Contains(result.Errors, e => e.Contains("not a valid Hotkey"));
        }

        [Fact]
        public void Validator_LetsAFolderOptionIntoAFileStep_AndShowsTheFolder()
        {
            var recipe = CreateRecipe("option_type_folder");

            var result = RecipeValidator.Validate(recipe);

            Assert.DoesNotContain(result.Errors, e => e.Contains("text option"));
            var files = result.GatedActions.Where(a => a.GateType == RecipeGateType.FileSystemAccess).ToList();
            Assert.Contains(files, a => a.Target.Contains(@"C:\Screenshots\Shots"));
            Assert.DoesNotContain(files, a => a.Target.Contains("option."));

            // A text option still can't choose where files go
            recipe.FindOption("folder").Type = ContractDataType.String;
            result = RecipeValidator.Validate(recipe);
            Assert.Contains(result.Errors, e => e.Contains("'folder'") && e.Contains("text option"));
        }

        private static CaptureRecipe CreateRecipe(string id)
        {
            RecipeOptionStore.Reset(id);
            var recipe = new CaptureRecipe(id, "Option types")
            {
                Options = new List<RecipeOption>
                {
                    new RecipeOption { Key = "hotkey", Type = ContractDataType.Hotkey, DefaultValue = "Ctrl + Shift + D" },
                    new RecipeOption { Key = "folder", Type = ContractDataType.DirectoryPath, DefaultValue = @"C:\Screenshots" }
                },
                Triggers = new List<TriggerConfig> { TriggerConfig.CreateHotkey("${option.hotkey}") }
            };
            recipe.AddNode(new RecipeNodeConfig("source", "Source") { Parameters = new Dictionary<string, object> { { "SourceType", "Window" } } });
            recipe.AddNode(new RecipeNodeConfig("save", WellKnownStepTypes.Destinations)
            {
                Parameters = new Dictionary<string, object>
                {
                    { "SaveDirectory", @"${option.folder}\Shots" }
                }
            });
            recipe.Flow = new RecipeFlowConfig("source").AddTransition("source", "save");
            return recipe;
        }
    }
}
