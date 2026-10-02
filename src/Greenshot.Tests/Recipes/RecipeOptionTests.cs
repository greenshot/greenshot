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
using System.Linq;
using Greenshot.Base.Expressions;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.ExternalCommand;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// Recipe options: the definition in the recipe, the values in greenshot.ini, ${option.key} and switchable nodes
    /// </summary>
    public class RecipeOptionTests
    {
        private const string BorderRecipeJson = @"{
  ""id"": ""recipe_option_border"",
  ""name"": ""Border test"",
  ""options"": [
    { ""key"": ""border"", ""type"": ""Boolean"", ""default"": false, ""label"": ""Add a border"", ""quickSettings"": true },
    { ""key"": ""width"", ""type"": ""Integer"", ""default"": 2, ""min"": 1, ""max"": 50, ""enabledWhen"": ""border"" },
    { ""key"": ""color"", ""type"": ""Color"", ""default"": ""#000000"", ""enabledWhen"": ""border"" }
  ],
  ""nodes"": [
    { ""id"": ""source"", ""stepType"": ""Source"", ""parameters"": { ""sourceType"": ""FullScreen"" } },
    { ""id"": ""frame"", ""stepType"": ""Effect"", ""enabled"": ""${option.border}"",
      ""parameters"": { ""effect"": ""Border"", ""width"": ""${option.width}"", ""color"": ""${option.color}"" } }
  ],
  ""flow"": { ""startNodes"": [ ""source"" ], ""transitions"": { ""source"": [ ""frame"" ] } }
}";

        public RecipeOptionTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// Each test uses its own recipe id, the stored values are shared
        /// </summary>
        private static CaptureRecipe LoadBorderRecipe(string id)
        {
            var recipe = RecipeSerializer.Deserialize(BorderRecipeJson);
            recipe.Id = id;
            RecipeOptionStore.Reset(id);
            return recipe;
        }

        [Fact]
        public void Recipe_ReadsOptionsAndEnabledExpression_AndWritesThemBack()
        {
            var recipe = LoadBorderRecipe("option_roundtrip");

            Assert.Equal(3, recipe.Options.Count);
            var width = recipe.FindOption("WIDTH");
            Assert.NotNull(width);
            Assert.Equal(ContractDataType.Integer, width.Type);
            Assert.Equal(1m, width.Min);
            Assert.Equal("border", width.EnabledWhen);
            Assert.True(recipe.FindOption("border").QuickSettings);

            var frame = recipe.FindNode("frame");
            Assert.True(frame.Enabled);
            Assert.Equal("${option.border}", frame.EnabledExpression);

            string json = RecipeSerializer.Serialize(recipe);
            Assert.Contains("\"options\"", json);
            Assert.Contains("\"enabled\": \"${option.border}\"", json);
            Assert.Contains("\"default\": 2", json);
            Assert.DoesNotContain("hasOptions", json, StringComparison.OrdinalIgnoreCase);

            var again = RecipeSerializer.Deserialize(json);
            Assert.Equal("${option.border}", again.FindNode("frame").EnabledExpression);
            Assert.Equal(3, again.Options.Count);

            var clone = recipe.Clone();
            Assert.Equal("${option.border}", clone.FindNode("frame").EnabledExpression);
            Assert.NotSame(recipe.Options[0], clone.Options[0]);
        }

        [Theory]
        [InlineData("false", false)]
        [InlineData("true", true)]
        [InlineData("\"False\"", false)]
        public void NodeEnabled_StaysABoolean(string jsonValue, bool expected)
        {
            string json = BorderRecipeJson.Replace("\"enabled\": \"${option.border}\"", $"\"enabled\": {jsonValue}");
            var node = RecipeSerializer.Deserialize(json).FindNode("frame");
            Assert.Equal(expected, node.Enabled);
            Assert.Null(node.EnabledExpression);
        }

        [Fact]
        public void Option_ConvertsValuesToItsType()
        {
            var integer = new RecipeOption { Key = "i", Type = ContractDataType.Integer, Min = 1, Max = 50 };
            Assert.True(integer.TryConvert("7", out var value));
            Assert.Equal(7, value);
            Assert.True(integer.TryConvert(500L, out value));
            Assert.Equal(50, value);
            Assert.True(integer.TryConvert(-3, out value));
            Assert.Equal(1, value);
            Assert.False(integer.TryConvert("2.5", out _));
            Assert.False(integer.TryConvert("wide", out _));

            var number = new RecipeOption { Key = "d", Type = ContractDataType.Decimal, Max = 1 };
            Assert.True(number.TryConvert("0.25", out value));
            Assert.Equal(0.25d, value);
            Assert.Equal("1", number.ToStorageString(3.5));

            var color = new RecipeOption { Key = "c", Type = ContractDataType.Color };
            Assert.True(color.TryConvert("#ff0000", out value));
            Assert.Equal("#FF0000", value);
            Assert.True(color.TryConvert("#80FF0000", out _));
            Assert.False(color.TryConvert("red", out _));
            Assert.Equal("#000000", color.GetDefaultValue());

            var choice = new RecipeOption
            {
                Key = "e",
                Type = ContractDataType.Enum,
                Choices = new List<RecipeOptionChoice> { new RecipeOptionChoice { Value = "Top" }, new RecipeOptionChoice { Value = "Bottom" } }
            };
            Assert.True(choice.TryConvert("bottom", out value));
            Assert.Equal("Bottom", value);
            Assert.False(choice.TryConvert("Left", out _));
            Assert.Equal("Top", choice.GetDefaultValue());

            var flag = new RecipeOption { Key = "b", Type = ContractDataType.Boolean, DefaultValue = true };
            Assert.Equal(true, flag.GetDefaultValue());
            Assert.Equal("False", flag.ToStorageString(false));
        }

        [Fact]
        public void Store_KeepsOnlyValuesWhichDifferFromTheDefault()
        {
            var recipe = LoadBorderRecipe("option_store");
            var width = recipe.FindOption("width");

            Assert.Equal(2, RecipeOptionStore.GetValue(recipe, width));

            RecipeOptionStore.SetValue(recipe.Id, width, 5);
            Assert.Equal(5, RecipeOptionStore.GetValue(recipe, width));

            // Not a number: ignored
            RecipeOptionStore.SetValue(recipe.Id, width, "wide");
            Assert.Equal(5, RecipeOptionStore.GetValue(recipe, width));

            // A recipe update with a smaller maximum limits the stored value
            width.Max = 4;
            Assert.Equal(4, RecipeOptionStore.GetValue(recipe, width));

            RecipeOptionStore.SetValue(recipe.Id, width, 2);
            Assert.Equal(2, RecipeOptionStore.GetValue(recipe, width));

            RecipeOptionStore.SetValue(recipe.Id, recipe.FindOption("border"), true);
            Assert.True(RecipeOptionStore.TryGetValue(recipe, "border", out var border));
            Assert.Equal(true, border);
            Assert.False(RecipeOptionStore.TryGetValue(recipe, "unknown", out _));

            RecipeOptionStore.Reset(recipe.Id);
            Assert.Equal(false, RecipeOptionStore.GetValue(recipe, recipe.FindOption("border")));
        }

        [Fact]
        public void Store_KeepsTheValuesOfEachRecipeApart()
        {
            var first = LoadBorderRecipe("option_first");
            var second = LoadBorderRecipe("option_second");

            RecipeOptionStore.SetValue(first.Id, first.FindOption("color"), "#FF0000");

            Assert.Equal("#FF0000", RecipeOptionStore.GetValue(first, first.FindOption("color")));
            Assert.Equal("#000000", RecipeOptionStore.GetValue(second, second.FindOption("color")));
        }

        [Fact]
        public void Expression_ReadsTheOption_AndAValueForTheRunCanNotReplaceIt()
        {
            var recipe = LoadBorderRecipe("option_expression");
            RecipeOptionStore.SetValue(recipe.Id, recipe.FindOption("width"), 7);

            using var context = new CaptureFlowContext(recipe);
            context.Properties["option.width"] = 99;

            Assert.Equal(7, ExpressionEvaluator.Instance.Evaluate("${option.width}", context));
            Assert.Equal("7 px", ExpressionEvaluator.Instance.Evaluate("${option.width} px", context));
            Assert.Equal(true, ExpressionEvaluator.Instance.Evaluate("${option.width > 5}", context));
            Assert.Null(ExpressionEvaluator.Instance.Evaluate("${option.missing}", context));

            var resolved = ExpressionEvaluator.Instance.ResolveParameters(recipe.FindNode("frame").Parameters, context);
            Assert.Equal("#000000", resolved["color"]);
        }

        [Fact]
        public void Node_RunsOnlyWhileTheOptionIsOn()
        {
            var recipe = LoadBorderRecipe("option_switch");
            var frame = recipe.FindNode("frame");
            using var context = new CaptureFlowContext(recipe);

            Assert.False(frame.ShouldRun(context));

            RecipeOptionStore.SetValue(recipe.Id, recipe.FindOption("border"), true);
            Assert.True(frame.ShouldRun(context));

            frame.Enabled = false;
            Assert.False(frame.ShouldRun(context));
        }

        [Fact]
        public void Validator_AcceptsTheBorderRecipe()
        {
            var result = RecipeValidator.Validate(RecipeSerializer.Deserialize(BorderRecipeJson, validate: false));
            Assert.True(result.IsValid, result.ToString());
        }

        [Theory]
        [InlineData("{ \"key\": \"border\", \"type\": \"Boolean\" }, { \"key\": \"Border\", \"type\": \"Boolean\" }", "Duplicate option key")]
        [InlineData("{ \"key\": \"1st\", \"type\": \"Boolean\" }", "invalid")]
        [InlineData("{ \"key\": \"region\", \"type\": \"Region\" }", "options can be")]
        [InlineData("{ \"key\": \"size\", \"type\": \"Integer\", \"quickSettings\": true }", "quick settings")]
        [InlineData("{ \"key\": \"size\", \"type\": \"Integer\", \"min\": 5, \"max\": 1 }", "larger than")]
        [InlineData("{ \"key\": \"size\", \"type\": \"Integer\", \"default\": \"big\" }", "not a valid Integer")]
        [InlineData("{ \"key\": \"place\", \"type\": \"Enum\" }", "needs 'choices'")]
        [InlineData("{ \"key\": \"size\", \"type\": \"Integer\", \"enabledWhen\": \"other\" }", "enabledWhen")]
        public void Validator_RejectsBrokenOptions(string options, string expectedError)
        {
            string json = $@"{{
  ""id"": ""broken_options"", ""name"": ""Broken"",
  ""options"": [ {options} ],
  ""nodes"": [ {{ ""id"": ""source"", ""stepType"": ""Source"", ""parameters"": {{ ""sourceType"": ""FullScreen"" }} }} ],
  ""flow"": {{ ""startNodes"": [ ""source"" ] }}
}}";
            var result = RecipeValidator.Validate(RecipeSerializer.Deserialize(json, validate: false));
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.IndexOf(expectedError, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void Validator_RejectsAnUndeclaredOption_AndAnEnabledWithoutExpression()
        {
            var recipe = RecipeSerializer.Deserialize(BorderRecipeJson, validate: false);
            recipe.FindNode("frame").Parameters["width"] = "${option.thickness}";
            recipe.FindNode("source").EnabledExpression = "option.border";

            var result = RecipeValidator.Validate(recipe);

            Assert.Contains(result.Errors, e => e.Contains("'thickness'"));
            Assert.Contains(result.Errors, e => e.Contains("Node 'source'") && e.Contains("expression"));
        }

        [Fact]
        public void Validator_KeepsTextOptionsOutOfStepsWhichNeedAnApproval()
        {
            new ExternalCommandPlugin().RegisterSteps(StepRegistry.Instance);
            var recipe = new CaptureRecipe("option_gated", "Gated")
            {
                Options = new List<RecipeOption>
                {
                    new RecipeOption { Key = "suffix", Type = ContractDataType.String },
                    new RecipeOption { Key = "quality", Type = ContractDataType.Integer, DefaultValue = 80 }
                }
            };
            recipe.AddNode(new RecipeNodeConfig("source", "Source") { Parameters = new Dictionary<string, object> { { "SourceType", "FullScreen" } } });
            recipe.AddNode(new RecipeNodeConfig("cmd", "ExternalCommand")
            {
                Parameters = new Dictionary<string, object>
                {
                    { "CommandLine", @"C:\Tools\optimize.exe" },
                    { "Arguments", "\"{0}\" --quality ${option.quality}" }
                }
            });
            recipe.Flow = new RecipeFlowConfig("source").AddTransition("source", "cmd");

            Assert.DoesNotContain(RecipeValidator.Validate(recipe).Errors, e => e.Contains("text option"));

            recipe.FindNode("cmd").Parameters["Arguments"] = "\"{0}\" ${option.suffix}";
            var result = RecipeValidator.Validate(recipe);
            Assert.Contains(result.Errors, e => e.Contains("'suffix'") && e.Contains("text option"));
        }

        [Fact]
        public void FindOptionReferences_OnlyLooksInsideExpressions()
        {
            var keys = RecipeValidator.FindOptionReferences("option.plain ${option.width * 2} ${ context.option.x } ${option.Color}");
            Assert.Equal(new[] { "Color", "width" }, keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
        }
    }
}
