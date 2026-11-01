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

using System.Linq;
using Greenshot.Base.Recipes;
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
            recipe.Translations = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>
            {
                [language] = new System.Collections.Generic.Dictionary<string, string> { [path] = text }
            };

            var result = RecipeValidator.Validate(recipe);
            Assert.Equal(error, result.Errors.Any(e => e.StartsWith("Translations")));
        }
    }
}
