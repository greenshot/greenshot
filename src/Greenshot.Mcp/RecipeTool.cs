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
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Greenshot.Mcp
{
    /// <summary>
    /// A tool which runs a Greenshot recipe: one per AI tool trigger of the user's recipes (LIST_AI_TOOLS).
    /// The input schema comes from the trigger's arguments.
    /// </summary>
    public sealed class RecipeTool : McpServerTool
    {
        private readonly Tool _tool;

        private RecipeTool(Tool tool, string signature)
        {
            _tool = tool;
            Signature = signature;
        }

        /// <summary>
        /// The tool's definition as Greenshot sent it, to see whether it changed
        /// </summary>
        public string Signature { get; }

        public override Tool ProtocolTool => _tool;

        public override IReadOnlyList<object> Metadata => Array.Empty<object>();

        /// <summary>
        /// Creates the tool from an entry of LIST_AI_TOOLS, null when it isn't valid.
        /// </summary>
        public static RecipeTool? Create(JsonObject entry)
        {
            string? name = (string?)entry["name"];
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var properties = new JsonObject();
            var required = new JsonArray();
            if (entry["arguments"] is JsonArray arguments)
            {
                foreach (var argument in arguments.OfType<JsonObject>())
                {
                    string? argumentName = (string?)argument["name"];
                    if (string.IsNullOrWhiteSpace(argumentName))
                    {
                        continue;
                    }
                    properties[argumentName] = CreateSchema(argument);
                    if (argument["required"]?.GetValue<bool>() == true)
                    {
                        required.Add((JsonNode)JsonValue.Create(argumentName));
                    }
                }
            }
            var schema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties
            };
            if (required.Count > 0)
            {
                schema["required"] = required;
            }

            bool readOnly = entry["read_only"]?.GetValue<bool>() ?? false;
            bool destructive = entry["destructive"]?.GetValue<bool>() ?? false;
            using var schemaDocument = JsonDocument.Parse(schema.ToJsonString());
            var tool = new Tool
            {
                Name = name,
                Title = (string?)entry["title"],
                Description = (string?)entry["description"],
                InputSchema = schemaDocument.RootElement.Clone(),
                Annotations = new ToolAnnotations
                {
                    Title = (string?)entry["title"],
                    ReadOnlyHint = readOnly,
                    DestructiveHint = !readOnly && destructive,
                    OpenWorldHint = !readOnly
                }
            };
            return new RecipeTool(tool, entry.ToJsonString());
        }

        /// <summary>
        /// The JSON schema of one argument (the declared type of the recipe argument)
        /// </summary>
        private static JsonObject CreateSchema(JsonObject argument)
        {
            string type = (string?)argument["type"] ?? "String";
            string description = (string?)argument["description"] ?? string.Empty;
            string? defaultValue = (string?)argument["default_value"];
            var schema = new JsonObject();
            switch (type)
            {
                case "Integer":
                    schema["type"] = "integer";
                    if (long.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer))
                    {
                        schema["default"] = integer;
                    }
                    break;
                case "Decimal":
                    schema["type"] = "number";
                    if (double.TryParse(defaultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                    {
                        schema["default"] = number;
                    }
                    break;
                case "Boolean":
                    schema["type"] = "boolean";
                    if (bool.TryParse(defaultValue, out bool boolean))
                    {
                        schema["default"] = boolean;
                    }
                    break;
                case "Window":
                    schema["type"] = "string";
                    description = AddHint(description, "list_windows", "A window id from list_windows, e.g. w7.");
                    break;
                case "Region":
                    schema["type"] = "string";
                    schema["pattern"] = @"^\s*-?\d+\s*,\s*-?\d+\s*,\s*\d+\s*,\s*\d+\s*$";
                    description = AddHint(description, "x,y,width,height", "x,y,width,height in screen coordinates.");
                    break;
                default:
                    schema["type"] = "string";
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        schema["default"] = defaultValue;
                    }
                    break;
            }
            if (argument["allowed_values"] is JsonArray allowed && allowed.Count > 0)
            {
                schema["enum"] = new JsonArray(allowed.Select(v => (JsonNode?)JsonValue.Create((string?)v)).ToArray());
            }
            if (!string.IsNullOrWhiteSpace(description))
            {
                schema["description"] = description;
            }
            return schema;
        }

        /// <summary>
        /// Adds how to pass the value, unless the description already says it
        /// </summary>
        private static string AddHint(string description, string keyword, string hint)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return hint;
            }
            return description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ? description : $"{description} ({hint})";
        }

        public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
        {
            var parameters = new JsonObject();
            if (request.Params?.Arguments != null)
            {
                foreach (var argument in request.Params.Arguments)
                {
                    string? value = ToArgumentValue(argument.Value);
                    if (value != null)
                    {
                        parameters[argument.Key] = value;
                    }
                }
            }

            var message = new JsonObject
            {
                ["command"] = "RUN_AI_TOOL",
                ["recipe"] = _tool.Name,
                ["parameters"] = parameters
            };
            var result = await ToolCalls.CallAsync(request.Server, message, ToolCalls.RecipeResult, cancellationToken).ConfigureAwait(false);
            RecipeToolSync.RequestSync();
            return result;
        }

        /// <summary>
        /// Recipe arguments are text: numbers and booleans as JSON writes them, a region given as array [x, y, width, height] as "x,y,width,height".
        /// </summary>
        internal static string? ToArgumentValue(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return value.GetString();
                case JsonValueKind.True:
                    return "true";
                case JsonValueKind.False:
                    return "false";
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                case JsonValueKind.Array when value.EnumerateArray().All(e => e.ValueKind == JsonValueKind.Number):
                    return string.Join(",", value.EnumerateArray().Select(e => e.GetRawText()));
                default:
                    return value.GetRawText();
            }
        }
    }
}
