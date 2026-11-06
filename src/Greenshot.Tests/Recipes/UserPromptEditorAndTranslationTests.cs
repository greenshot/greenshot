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
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.RecipeEditor.ViewModels;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The UserPrompt in the recipe editor and its translations (issue #1676)
    /// </summary>
    public class UserPromptEditorAndTranslationTests
    {
        private const string RecipeJson = @"{
  ""id"": ""recipe_prompt_test"",
  ""name"": ""Prompt test"",
  ""nodes"": [
    { ""id"": ""ask"", ""stepType"": ""UserPrompt"", ""parameters"": {
        ""Title"": ""Where to?"", ""Message"": ""Choose a place"",
        ""Choices"": [ { ""Key"": ""yes"", ""Label"": ""Yes"", ""IsDefault"": true }, { ""Key"": ""no"", ""Label"": ""No"", ""IsCancel"": true } ] } },
    { ""id"": ""a"", ""stepType"": ""SetVariable"", ""parameters"": { ""Variables"": { ""x"": ""1"" } } },
    { ""id"": ""b"", ""stepType"": ""SetVariable"", ""parameters"": { ""Variables"": { ""x"": ""2"" } } }
  ],
  ""flow"": { ""startNodes"": [ ""ask"" ], ""transitions"": [
    { ""from"": ""ask"", ""to"": ""a"", ""branch"": ""yes"" }, { ""from"": ""ask"", ""to"": ""b"", ""branch"": ""no"" } ] },
  ""translations"": { ""de"": { ""nodes.ask.Title"": ""Wohin?"", ""nodes.ask.Choices.yes"": ""Ja"" } }
}";

        public UserPromptEditorAndTranslationTests()
        {
            TestEnvironment.EnsureInitialized();
            // The translatable texts of a node come from the contract of its step
            StepRegistry.Instance.Register<UserPromptStep>(config => new UserPromptStep(config));
            StepRegistry.Instance.Register<SetVariableStep>(config => new SetVariableStep(config));
        }

        private static List<RecipeTransition> BranchTransitions(CaptureRecipe recipe) =>
            recipe.Flow.Transitions.Where(t => t.IsBranch).OrderBy(t => t.Branch).ToList();

        [Fact]
        public void Editor_KeepsTheConnectionsOfTheChoices_WhenTheTitleChanges()
        {
            var editor = new RecipeEditorViewModel();
            editor.ActiveRecipe = RecipeSerializer.Deserialize(RecipeJson);
            var prompt = editor.Nodes.Single(n => n.Id == "ask");
            var ports = prompt.PromptChoices.Select(c => c.Port).ToList();

            prompt.UserPromptTitle = "Where should it go?";
            prompt.UserPromptMessage = "Choose";

            Assert.Equal(ports, prompt.PromptChoices.Select(c => c.Port));
            Assert.Equal(ports, prompt.Output);
            Assert.All(editor.Connections.Where(c => c.SourceNode == prompt), c => Assert.Contains(c.Source, ports));

            editor.SyncRecipeTransitions();
            var transitions = BranchTransitions(editor.ActiveRecipe);
            Assert.Equal(new[] { "no", "yes" }, transitions.Select(t => t.Branch));
            Assert.Equal(new[] { "b", "a" }, transitions.Select(t => t.To));
        }

        [Fact]
        public void Editor_RemovesOnlyTheConnectionsOfARemovedChoice_WhenTheChoicesAreReloaded()
        {
            var editor = new RecipeEditorViewModel();
            editor.ActiveRecipe = RecipeSerializer.Deserialize(RecipeJson);
            var prompt = editor.Nodes.Single(n => n.Id == "ask");
            var yes = prompt.PromptChoices.Single(c => c.Key == "yes");

            prompt.Config.Set("Choices", new List<PromptChoice> { new PromptChoice("yes", "Yes, please") });
            prompt.NotifyConfigUpdated();

            Assert.Same(yes, Assert.Single(prompt.PromptChoices));
            Assert.Equal("Yes, please", yes.Label);
            var connection = Assert.Single(editor.Connections, c => c.SourceNode == prompt);
            Assert.Same(yes.Port, connection.Source);
        }

        [Fact]
        public void Editor_AllowsOnlyOneCancelChoice()
        {
            var editor = new RecipeEditorViewModel();
            editor.ActiveRecipe = RecipeSerializer.Deserialize(RecipeJson);
            var prompt = editor.Nodes.Single(n => n.Id == "ask");

            prompt.PromptChoices.Single(c => c.Key == "yes").IsCancel = true;

            Assert.Equal(new[] { "yes" }, prompt.PromptChoices.Where(c => c.IsCancel).Select(c => c.Key));
            Assert.Equal(new[] { "yes" }, prompt.Config.GetParameter<List<PromptChoice>>("Choices").Where(c => c.IsCancel).Select(c => c.Key));
        }

        [Fact]
        public void Validator_RejectsTwoCancelChoices()
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);
            Assert.DoesNotContain(RecipeValidator.Validate(recipe).Errors, e => e.Contains("IsCancel"));

            recipe.FindNode("ask").Set("Choices", new List<PromptChoice> { new PromptChoice("yes", "Yes", isCancel: true), new PromptChoice("no", "No", isCancel: true) });

            Assert.Contains(RecipeValidator.Validate(recipe).Errors, e => e.Contains("only one choice can be 'IsCancel'"));
        }

        [Fact]
        public void PromptTexts_AreTranslatable()
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);
            var texts = recipe.GetTranslatableTexts().ToDictionary(t => t.Key, t => t.Value);

            Assert.Equal("Where to?", texts["nodes.ask.Title"]);
            Assert.Equal("Choose a place", texts["nodes.ask.Message"]);
            Assert.Equal("Yes", texts["nodes.ask.Choices.yes"]);
            Assert.Equal("No", texts["nodes.ask.Choices.no"]);
            // Only texts the user sees
            Assert.DoesNotContain("nodes.ask.ShowPreview", texts.Keys);
            Assert.DoesNotContain(texts.Keys, k => k.StartsWith("nodes.a."));
        }

        [Theory]
        [InlineData("nodes.ask.Message", false)]
        [InlineData("nodes.ask.Choices.no", false)]
        [InlineData("nodes.ask.Choices.maybe", true)]
        [InlineData("nodes.ask.ShowPreview", true)]
        [InlineData("nodes.other.Title", true)]
        public void Validator_KnowsTheTextsOfTheNodes(string path, bool error)
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);
            recipe.Translations["de"][path] = "Text";

            Assert.Equal(error, RecipeValidator.Validate(recipe).Errors.Any(e => e.StartsWith("Translations")));
        }

        [Fact]
        public void TranslateNodeParameters_TranslatesTheTextsAndKeepsTheRest()
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);
            recipe.Translations["de-DE"] = new Dictionary<string, string> { ["nodes.ask.Message"] = "Wähle einen Ort für ${payload.title}" };
            var node = recipe.FindNode("ask");
            var choicesOfTheFile = node.Parameters["Choices"];
            var contract = StepRegistry.Instance.GetContract(WellKnownStepTypes.UserPrompt);

            var german = recipe.TranslateNodeParameters(node, contract, "de-DE");

            Assert.Equal("Wohin?", german["Title"]);
            // de-DE before de, the expression is evaluated later like the text of the file
            Assert.Equal("Wähle einen Ort für ${payload.title}", german["Message"]);
            var choices = Assert.IsType<List<PromptChoice>>(german["Choices"]);
            Assert.Equal(new[] { "yes", "no" }, choices.Select(c => c.Key));
            Assert.Equal(new[] { "Ja", "No" }, choices.Select(c => c.Label));
            Assert.True(choices[0].IsDefault);
            Assert.True(choices[1].IsCancel);

            var french = recipe.TranslateNodeParameters(node, contract, "fr");
            Assert.Equal("Where to?", french["Title"]);
            Assert.Equal(new[] { "Yes", "No" }, Assert.IsType<List<PromptChoice>>(french["Choices"]).Select(c => c.Label));

            // The node itself is not changed
            Assert.Equal("Where to?", node.GetParameter<string>("Title"));
            Assert.Same(choicesOfTheFile, node.Parameters["Choices"]);
        }

        [Fact]
        public void Translations_FollowARenamedNode_AndThoseOfARemovedChoiceAreRemoved()
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);
            recipe.FindNode("ask").Id = "question";
            recipe.RenameNodeTranslations("ask", "question");
            recipe.Translations["fr"] = new Dictionary<string, string> { ["nodes.question.Choices.no"] = "Non" };

            recipe.FindNode("question").Set("Choices", new List<PromptChoice> { new PromptChoice("yes", "Yes") });
            recipe.RemoveUnusedTranslations();

            Assert.Equal("Wohin?", recipe.Translations["de"]["nodes.question.Title"]);
            Assert.Equal("Ja", recipe.Translations["de"]["nodes.question.Choices.yes"]);
            Assert.False(recipe.Translations.ContainsKey("fr"));
        }
    }
}
