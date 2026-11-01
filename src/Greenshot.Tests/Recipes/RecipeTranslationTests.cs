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
using System.Reflection;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Plugin.RecipeEditor.ViewModels;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The texts of a recipe file in other languages ("translations")
    /// </summary>
    public class RecipeTranslationTests
    {
        private const string RecipeJson = @"{
  ""id"": ""recipe_translation_test"",
  ""name"": ""Capture to my folder"",
  ""description"": ""Saves the capture"",
  ""options"": [
    { ""key"": ""mode"", ""type"": ""Enum"", ""default"": ""fast"", ""label"": ""Mode"",
      ""choices"": [ { ""value"": ""fast"", ""label"": ""Fast"" }, { ""value"": ""good"", ""label"": ""Good"" } ] }
  ],
  ""nodes"": [ { ""id"": ""source"", ""stepType"": ""Source"", ""parameters"": { ""SourceType"": ""FullScreen"" } } ],
  ""flow"": { ""startNodes"": [ ""source"" ] },
  ""translations"": {
    ""de"": { ""name"": ""In meinen Ordner"", ""options.mode.label"": ""Modus"", ""options.mode.choices.fast"": ""Schnell"" },
    ""de-AT"": { ""name"": ""In mein Verzeichnis"" }
  }
}";

        public RecipeTranslationTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Translate_TakesTheLanguageThenTheLanguageAloneThenTheFile()
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);

            Assert.Equal("In mein Verzeichnis", recipe.Translate(FlowDefinition.TranslationName, recipe.Name, "de-AT"));
            Assert.Equal("In meinen Ordner", recipe.Translate(FlowDefinition.TranslationName, recipe.Name, "de-DE"));
            Assert.Equal("Capture to my folder", recipe.Translate(FlowDefinition.TranslationName, recipe.Name, "fr-FR"));
            // de-AT has no description, nor has de
            Assert.Equal("Saves the capture", recipe.Translate(FlowDefinition.TranslationDescription, recipe.Description, "de-AT"));
        }

        [Fact]
        public void Options_KnowTheirRecipe_AlsoInACopy()
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);
            var option = recipe.FindOption("mode");
            Assert.Same(recipe, option.Owner);
            Assert.All(option.Choices, choice => Assert.Same(option, choice.Option));
            Assert.Equal("Schnell", recipe.Translate("options.mode.choices.fast", "Fast", "de-DE"));

            var copy = recipe.Clone();
            Assert.Same(copy, copy.FindOption("mode").Owner);
            Assert.Equal("Modus", copy.Translate("options.mode.label", "Mode", "de-DE"));
        }

        [Fact]
        public void Translations_AreWrittenAndReadAgain()
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);
            var again = RecipeSerializer.Deserialize(RecipeSerializer.Serialize(recipe));

            Assert.Equal(new[] { "de", "de-AT" }, again.Translations.Keys.OrderBy(k => k));
            Assert.Equal("Schnell", again.Translations["de"]["options.mode.choices.fast"]);
        }

        [Theory]
        [InlineData("de", "name", "In meinen Ordner", false)]
        [InlineData("de", "options.mode.choices.good", "Gut", false)]
        [InlineData("de", "options.mode.choices.slow", "Langsam", true)]
        [InlineData("de", "options.size.label", "Größe", true)]
        [InlineData("de", "title", "Titel", true)]
        [InlineData("de", "name", " ", true)]
        [InlineData("German", "name", "In meinen Ordner", true)]
        public void Validator_ChecksLanguagesAndTexts(string language, string path, string text, bool error)
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);
            recipe.Translations = new Dictionary<string, Dictionary<string, string>>
            {
                [language] = new Dictionary<string, string> { [path] = text }
            };

            var result = RecipeValidator.Validate(recipe);
            Assert.Equal(error, result.Errors.Any(e => e.StartsWith("Translations")));
        }

        [Fact]
        public void TriggerMenuText_IsTranslatedByItsPlace_AndMovesWhenATriggerIsRemoved()
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);
            recipe.AddTrigger(TriggerConfig.CreateHotkey("Ctrl + Shift + T"));
            recipe.AddTrigger(TriggerConfig.CreateContextMenu("My folder"));
            recipe.Translations["de"]["triggers.1.menuItemText"] = "Mein Ordner";

            Assert.True(RecipeValidator.Validate(recipe).IsValid, string.Join("; ", RecipeValidator.Validate(recipe).Errors));
            Assert.Equal("Mein Ordner", recipe.Translate(CaptureRecipe.TriggerMenuTextPath(1), "My folder", "de-DE"));
            // The hotkey has no menu text to translate
            Assert.DoesNotContain(recipe.GetTranslatableTexts(), t => t.Key == CaptureRecipe.TriggerMenuTextPath(0));

            recipe.RemoveTriggerTranslations(0);
            recipe.Triggers.RemoveAt(0);
            Assert.Equal("Mein Ordner", recipe.Translations["de"][CaptureRecipe.TriggerMenuTextPath(0)]);
            Assert.True(RecipeValidator.Validate(recipe).IsValid, string.Join("; ", RecipeValidator.Validate(recipe).Errors));
        }

        [Fact]
        public void Editor_AddsALanguage_AndWritesTheTranslations()
        {
            var editor = new RecipeEditorViewModel();
            editor.ActiveRecipe = RecipeSerializer.Deserialize(RecipeJson);
            Assert.Equal(new[] { "de", "de-AT" }, editor.TranslationLanguages);

            editor.AddTranslationLanguage("German");
            Assert.DoesNotContain("German", editor.TranslationLanguages);

            editor.AddTranslationLanguage("fr");
            Assert.Equal("fr", editor.SelectedTranslationLanguage);
            var name = editor.TranslationRows.Single(r => r.Path == FlowDefinition.TranslationName);
            Assert.Equal("Capture to my folder", name.Original);
            name.Text = "Capturer dans mon dossier";
            Assert.Equal("Capturer dans mon dossier", editor.ActiveRecipe.Translations["fr"][FlowDefinition.TranslationName]);

            // Without texts the language isn't written
            name.Text = "";
            Assert.False(editor.ActiveRecipe.Translations.ContainsKey("fr"));
        }

        [Fact]
        public void Editor_KeepsAuthorsAndTranslationsOfAnAutomaticStep()
        {
            var extension = new RecipeExtension("ext_translation_test", "Frame")
            {
                Authors = new List<RecipeAuthor> { new RecipeAuthor { Name = "Jane Doe" } },
                Copyright = "Copyright (c) 2026 Jane Doe",
                License = "MIT",
                Translations = new Dictionary<string, Dictionary<string, string>> { ["de"] = new Dictionary<string, string> { ["name"] = "Rahmen" } }
            };
            extension.Nodes.Add(RecipeStepConfig.CreateFeedback("feedback"));
            var editor = new RecipeEditorViewModel();
            editor.OpenExtension(extension);

            var built = (RecipeExtension)typeof(RecipeEditorViewModel).GetMethod("BuildExtension", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(editor, new object[] { false });

            Assert.Equal("Jane Doe", Assert.Single(built.Authors).Name);
            Assert.Equal("Copyright (c) 2026 Jane Doe", built.Copyright);
            Assert.Equal("MIT", built.License);
            Assert.Equal("Rahmen", built.Translations["de"]["name"]);
        }
    }
}
