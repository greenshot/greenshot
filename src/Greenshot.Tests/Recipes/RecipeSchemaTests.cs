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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Greenshot.Base.Recipes;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The recipe schema (docs/vNext/recipe.schema.json) is written by hand for editors and AI tools, Greenshot reads recipes
    /// with its own model. These tests keep the two in step.
    /// </summary>
    public class RecipeSchemaTests
    {
        private static JsonObject LoadSchema()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "docs", "vNext", "recipe.schema.json");
                if (File.Exists(candidate))
                {
                    return JsonNode.Parse(File.ReadAllText(candidate)).AsObject();
                }
                directory = directory.Parent;
            }
            throw new FileNotFoundException("docs/vNext/recipe.schema.json not found above " + AppDomain.CurrentDomain.BaseDirectory);
        }

        /// <summary>
        /// The names Greenshot reads and writes for a type, as the recipe files spell them
        /// </summary>
        private static string[] ModelProperties(params Type[] types) =>
            types.SelectMany(type => new DefaultJsonTypeInfoResolver().GetTypeInfo(type, RecipeSerializer.Options).Properties.Select(p => p.Name))
                .Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray();

        private static string[] SchemaProperties(JsonNode properties) =>
            properties.AsObject().Select(p => p.Key).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        [Fact]
        public void Schema_HasTheTopLevelKeysOfRecipesAndExtensions()
        {
            var schema = LoadSchema();

            Assert.Equal(ModelProperties(typeof(CaptureRecipe), typeof(RecipeExtension)), SchemaProperties(schema["properties"]));
            Assert.False(schema["additionalProperties"]?.GetValue<bool>() ?? true, "The schema should reject keys Greenshot doesn't read.");
        }

        [Fact]
        public void Schema_HasTheKeysOfAnAuthor()
        {
            var authors = LoadSchema()["properties"]["authors"]["items"]["properties"];

            Assert.Equal(ModelProperties(typeof(RecipeAuthor)), SchemaProperties(authors));
        }
    }
}
