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
using Greenshot.Base.Recipes.Expressions;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeExpressionHelpTests
    {
        private const string RecipeJson = @"{
  ""id"": ""recipe_help_test"",
  ""name"": ""Help test"",
  ""options"": [ { ""key"": ""border"", ""label"": ""Border width"", ""type"": ""Integer"", ""default"": 2 } ],
  ""nodes"": [
    { ""id"": ""a"", ""stepType"": ""SetVariable"", ""parameters"": { ""Variables"": { ""Ticket"": ""${payload.title}"" } } }
  ],
  ""flow"": { ""startNodes"": [ ""a"" ] }
}";

        public RecipeExpressionHelpTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void For_Recipe_ListsOptionsVariablesValuesAndFunctions()
        {
            var recipe = RecipeSerializer.Deserialize(RecipeJson);

            var entries = RecipeExpressionHelp.For(recipe);

            var option = Assert.Single(entries, e => e.Group == ExpressionHelpGroup.RecipeOption);
            Assert.Equal("${option.border}", option.Expression);
            Assert.Contains("Border width", option.Description);
            Assert.Contains(entries, e => e.Group == ExpressionHelpGroup.RecipeVariable && e.Expression == "${var.Ticket}");
            Assert.Contains(entries, e => e.Group == ExpressionHelpGroup.Value && e.Expression == "${payload.title}");
            Assert.Contains(entries, e => e.Group == ExpressionHelpGroup.Function && e.Expression.StartsWith("left("));
            // option.* and var.* of the engine are listed per recipe, not as generic values
            Assert.DoesNotContain(entries, e => e.Group == ExpressionHelpGroup.Value && (e.Expression.StartsWith("${option.") || e.Expression.StartsWith("${var.")));
        }

        [Fact]
        public void For_NoRecipe_ListsValuesAndFunctionsOnly()
        {
            var entries = RecipeExpressionHelp.For(null);

            Assert.NotEmpty(entries);
            Assert.All(entries, e => Assert.True(e.Group == ExpressionHelpGroup.Value || e.Group == ExpressionHelpGroup.Function));
        }
    }
}
