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
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// Renders the human readable console output of greenshot-cli.exe for LIST_RECIPES and DESCRIBE_RECIPE,
    /// from the same data that JSON clients receive.
    /// </summary>
    public static class CliTextRenderer
    {
        public static string RenderRecipeList(JToken recipes)
        {
            var sb = new StringBuilder();
            sb.Append("Available recipes with CommandlineTrigger:\n\n");

            int count = 0;
            if (recipes is JArray recipeArray)
            {
                foreach (var recipe in recipeArray)
                {
                    string id = GetString(recipe, "id");
                    string command = GetString(recipe, "command");
                    string name = GetString(recipe, "name");
                    string description = GetString(recipe, "description");
                    string stdout = GetString(recipe, "stdout");

                    sb.AppendFormat(CultureInfo.InvariantCulture, "  {0,-20} {1} (ID: {2})\n",
                        string.IsNullOrEmpty(command) ? id : command,
                        string.IsNullOrEmpty(description) ? name : description,
                        id);
                    if (!string.IsNullOrEmpty(stdout))
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "      Stdout  : {0}\n", stdout);
                    }
                    AppendArguments(sb, GetToken(recipe, "arguments"), "      ");
                    sb.Append('\n');
                    count++;
                }
            }

            if (count == 0)
            {
                sb.Append("  (No recipes currently configured with active CommandlineTrigger)\n\n");
            }
            return sb.ToString();
        }

        public static string RenderRecipeDescription(JToken recipe)
        {
            var sb = new StringBuilder();
            string id = GetString(recipe, "id");
            string name = GetString(recipe, "name");
            string description = GetString(recipe, "description");
            string category = GetString(recipe, "category");

            sb.AppendFormat(CultureInfo.InvariantCulture, "Recipe: {0} (ID: {1}, Category: {2})\nDescription: {3}\n\n",
                string.IsNullOrEmpty(name) ? id : name,
                id,
                string.IsNullOrEmpty(category) ? "General" : category,
                string.IsNullOrEmpty(description) ? "(None)" : description);

            if (GetToken(recipe, "triggers") is JArray triggers)
            {
                foreach (var trigger in triggers)
                {
                    if (!string.Equals(GetString(trigger, "type"), "Commandline", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    string command = GetString(trigger, "command");
                    sb.AppendFormat(CultureInfo.InvariantCulture, "Command: {0}\n", string.IsNullOrEmpty(command) ? id : command);
                    AppendArguments(sb, GetToken(trigger, "arguments"), "  ");
                    sb.Append('\n');
                }
            }

            var contract = GetToken(recipe, "contract");
            if (contract == null || contract.Type != JTokenType.Object)
            {
                return sb.ToString();
            }

            var lifecycle = GetToken(contract, "lifecycle");
            if (lifecycle != null && lifecycle.Type == JTokenType.Object)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "Payload Lifecycle:\n  Acquires Image: {0}\n  Mutates Pixels: {1}\n  Extracts Text : {2}\n\n",
                    YesNo(GetBool(lifecycle, "acquires_image")),
                    YesNo(GetBool(lifecycle, "mutates_pixels")),
                    YesNo(GetBool(lifecycle, "extracts_text")));
            }

            AppendVariables(sb, GetToken(contract, "inputs"), "Inputs (Parameters & Variables)", true);
            AppendVariables(sb, GetToken(contract, "outputs"), "Outputs", false);

            if (GetToken(contract, "steps") is JArray steps)
            {
                sb.Append("Pipeline Steps:\n");
                int stepNumber = 1;
                foreach (var step in steps)
                {
                    string stepType = GetString(step, "step_type");
                    string displayName = GetString(step, "display_name");
                    sb.AppendFormat(CultureInfo.InvariantCulture, "  {0,2}. {1,-20} [{2}]{3}\n",
                        stepNumber++,
                        stepType,
                        string.IsNullOrEmpty(displayName) ? stepType : displayName,
                        (GetBool(step, "requires_image") ? " (requires image)" : string.Empty) +
                        (step["reachable"] != null && !GetBool(step, "reachable") ? " (never executed)" : string.Empty));
                }
                if (stepNumber == 1)
                {
                    sb.Append("  (None)\n");
                }
                sb.Append('\n');
            }

            if (GetToken(contract, "warnings") is JArray warnings && warnings.Count > 0)
            {
                sb.Append("Validation Warnings:\n");
                foreach (var warning in warnings)
                {
                    if (warning.Type == JTokenType.String)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "  [!] {0}\n", warning.Value<string>());
                    }
                }
                sb.Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>Declared Commandline arguments; accepts both snake_case (LIST_RECIPES) and PascalCase (DESCRIBE_RECIPE) names.</summary>
        private static void AppendArguments(StringBuilder sb, JToken arguments, string indent)
        {
            if (!(arguments is JArray argumentArray) || argumentArray.Count == 0)
            {
                return;
            }

            sb.Append(indent).Append("Arguments:\n");
            foreach (var argument in argumentArray)
            {
                string name = GetString(argument, "name");
                string variable = GetString(argument, "variable");
                string description = GetString(argument, "description");
                string defaultValue = GetString(argument, "default_value") ?? GetString(argument, "defaultValue");
                bool required = GetBool(argument, "required");
                string type = GetString(argument, "type");
                var allowedToken = GetToken(argument, "allowed_values");
                if (allowedToken is JArray allowedValues && allowedValues.Count > 0)
                {
                    description = $"{description} [{string.Join("|", allowedValues.Values<string>())}]".Trim();
                }
                if (!string.IsNullOrEmpty(type) && !string.Equals(type, "String", StringComparison.OrdinalIgnoreCase))
                {
                    description = $"{description} <{type}>".Trim();
                }

                if (required)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}  * --{1,-14} (required)          : {2}", indent, name, description);
                }
                else if (!string.IsNullOrEmpty(defaultValue))
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}    --{1,-14} (default: {2,-8}) : {3}", indent, name, defaultValue, description);
                }
                else
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}    --{1,-14} (optional)          : {2}", indent, name, description);
                }
                if (!string.IsNullOrEmpty(variable) && !string.Equals(variable, name, StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, " -> ${{{0}}}", variable);
                }
                sb.Append('\n');
            }
        }

        private static void AppendVariables(StringBuilder sb, JToken items, string title, bool inputs)
        {
            if (!(items is JArray itemArray))
            {
                return;
            }

            sb.Append(title).Append(":\n");
            foreach (var item in itemArray)
            {
                string name = GetString(item, "name");
                string type = GetString(item, "type");
                string description = GetString(item, "description");
                if (string.IsNullOrEmpty(type))
                {
                    type = "String";
                }

                if (!inputs)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "    {0,-20} ({1}{2}) : {3}\n", name, type,
                        GetBool(item, "conditional") ? ", not always set" : string.Empty, description);
                }
                else if (GetBool(item, "required"))
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "  * {0,-20} ({1}, required) : {2}\n", name, type, description);
                }
                else
                {
                    string defaultValue = GetString(item, "default_value");
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "    {0,-20} ({1}, default: {2}) : {3}\n", name, type, defaultValue, description);
                    }
                    else
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "    {0,-20} ({1}, optional) : {2}\n", name, type, description);
                    }
                }
            }
            if (itemArray.Count == 0)
            {
                sb.Append("  (None)\n");
            }
            sb.Append('\n');
        }

        private static JToken GetToken(JToken obj, string name)
        {
            return obj is JObject jObject ? jObject.GetValue(name, StringComparison.OrdinalIgnoreCase) : null;
        }

        private static string GetString(JToken obj, string name)
        {
            var token = GetToken(obj, name);
            return token == null || token.Type == JTokenType.Null ? null : token.ToString();
        }

        private static bool GetBool(JToken obj, string name)
        {
            var token = GetToken(obj, name);
            return token != null && token.Type == JTokenType.Boolean && token.Value<bool>();
        }

        private static string YesNo(bool value) => value ? "Yes" : "No";
    }
}
