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

using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Greenshot.Mcp
{
    /// <summary>
    /// Tools to write recipes for the user: the schema and what Greenshot offers, checking a recipe, and proposing it. A proposed
    /// recipe is shown to the user in Greenshot's recipe approval window, marked as written by an AI tool; nothing is saved unless the
    /// user approves it, and its triggers start switched off.
    /// </summary>
    [McpServerToolType]
    public sealed class RecipeAuthoringTools
    {
        /// <summary>
        /// The example recipes embedded with the schema
        /// </summary>
        internal static readonly string[] ExampleNames =
        {
            "region_blue_border.gsrecipe.json",
            "window_regex_redact.gsrecipe.json",
            "cli_ocr_extract.gsrecipe.json"
        };

        [McpServerTool(Name = "get_recipe_schema", Title = "Get the recipe schema", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns the JSON schema of Greenshot recipes and some example recipes. A recipe is a capture workflow: triggers " +
                     "(hotkey, menu entry, command line, AI tool, ...) start a flow of steps (capture source, selection, effects, OCR, " +
                     "destinations such as file, clipboard, editor or an upload). Use it with get_recipe_catalog to write a recipe the user asked for.")]
        public static CallToolResult GetRecipeSchema()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Greenshot recipe JSON schema");
            builder.AppendLine(ReadResource("recipe.schema.json"));
            foreach (var name in ExampleNames)
            {
                builder.AppendLine();
                builder.AppendLine($"# Example: {name}");
                builder.AppendLine(ReadResource(name));
            }
            return new CallToolResult { Content = [new TextContentBlock { Text = builder.ToString() }] };
        }

        [McpServerTool(Name = "get_recipe_catalog", Title = "Get the recipe catalog", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns what recipes can use in this Greenshot: the trigger types, the step types with their parameters, the " +
                     "destinations (uploads=true sends the capture to the internet), the processors and the user's recipes (id, name, triggers). " +
                     "With recipe_id it returns the JSON of that recipe instead, to change it with update_recipe.")]
        public static Task<CallToolResult> GetRecipeCatalogAsync(McpServer server,
            [Description("Optional: the id of a recipe to get its JSON")] string? recipe_id = null,
            CancellationToken cancellationToken = default)
        {
            var request = new JsonObject { ["command"] = "RECIPE_CATALOG" };
            if (!string.IsNullOrWhiteSpace(recipe_id))
            {
                request["parameters"] = new JsonObject { ["recipe"] = recipe_id };
            }
            return ToolCalls.CallAsync(server, request, reply => ToolCalls.TextResult(ToolCalls.Without(reply, "status", "exit_code")), cancellationToken);
        }

        [McpServerTool(Name = "validate_recipe", Title = "Validate a recipe", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Checks a recipe (JSON of one recipe) without saving or showing it: errors, warnings, what it does in plain words, " +
                     "its triggers, and what the user will be asked to allow (uploads, files, external commands). Fix the errors before propose_recipe.")]
        public static Task<CallToolResult> ValidateRecipeAsync(McpServer server,
            [Description("The recipe as a JSON object")] string recipe_json,
            CancellationToken cancellationToken = default)
        {
            var request = new JsonObject
            {
                ["command"] = "VALIDATE_RECIPE",
                ["parameters"] = new JsonObject { ["recipe"] = recipe_json }
            };
            return ToolCalls.CallAsync(server, request, reply => ToolCalls.TextResult(ToolCalls.Without(reply, "status", "exit_code")), cancellationToken);
        }

        [McpServerTool(Name = "propose_recipe", Title = "Propose a new recipe", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
        [Description("Shows a new recipe to the user in Greenshot, marked as written by you, with what it does and what it needs. " +
                     "Greenshot saves it only when the user approves it; the user decides which triggers are switched on (they start off) " +
                     "and allows uploads, files and external commands. Waits for the user's decision. The id must not be used by another recipe.")]
        public static Task<CallToolResult> ProposeRecipeAsync(McpServer server,
            [Description("The recipe as a JSON object, checked with validate_recipe")] string recipe_json,
            [Description("What the user asked for, in a sentence; shown to the user as your words")] string request,
            [Description("How the recipe does that, in a few sentences; shown to the user as your words")] string explanation,
            CancellationToken cancellationToken = default)
        {
            return ProposeAsync(server, recipe_json, request, explanation, null, cancellationToken);
        }

        [McpServerTool(Name = "update_recipe", Title = "Propose a change to a recipe", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
        [Description("Shows a changed version of an existing recipe (also a built-in one) to the user in Greenshot, with the changed lines. " +
                     "Get the current JSON with get_recipe_catalog(recipe_id). Greenshot saves it only when the user approves it; a built-in " +
                     "recipe can be restored in Greenshot's recipe manager. Waits for the user's decision.")]
        public static Task<CallToolResult> UpdateRecipeAsync(McpServer server,
            [Description("The id of the recipe to change")] string recipe_id,
            [Description("The complete changed recipe as a JSON object, checked with validate_recipe")] string recipe_json,
            [Description("What the user asked for, in a sentence; shown to the user as your words")] string request,
            [Description("What you changed and why, in a few sentences; shown to the user as your words")] string explanation,
            CancellationToken cancellationToken = default)
        {
            return ProposeAsync(server, recipe_json, request, explanation, recipe_id, cancellationToken);
        }

        private static async Task<CallToolResult> ProposeAsync(McpServer server, string recipeJson, string request, string explanation, string? replaces, CancellationToken cancellationToken)
        {
            var parameters = new JsonObject
            {
                ["recipe"] = recipeJson,
                ["request"] = request,
                ["explanation"] = explanation
            };
            if (!string.IsNullOrWhiteSpace(replaces))
            {
                parameters["replaces"] = replaces;
            }
            var message = new JsonObject
            {
                ["command"] = "PROPOSE_RECIPE",
                ["parameters"] = parameters
            };
            var result = await ToolCalls.CallAsync(server, message, reply => ToolCalls.TextResult(ToolCalls.Without(reply, "status", "exit_code")), cancellationToken).ConfigureAwait(false);
            // An approved recipe with an AI tool trigger is a new tool
            RecipeToolSync.RequestSync();
            return result;
        }

        internal static string ReadResource(string name)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (stream == null)
            {
                return $"(missing: {name})";
            }
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }

    /// <summary>
    /// The recipe schema as an MCP resource, for clients which read resources
    /// </summary>
    [McpServerResourceType]
    public sealed class RecipeAuthoringResources
    {
        [McpServerResource(UriTemplate = "greenshot-mcp://recipes/schema", Name = "recipe_schema", Title = "Greenshot recipe JSON schema", MimeType = "application/schema+json")]
        [Description("The JSON schema of Greenshot recipes")]
        public static string GetRecipeSchema() => RecipeAuthoringTools.ReadResource("recipe.schema.json");
    }
}
