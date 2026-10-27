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

#nullable disable

using Greenshot.Linux.Core.Languages;

using System;
using Greenshot.Linux.Recipes;

namespace Greenshot.Linux.Recipes.ViewModels
{
    /// <summary>
    /// Represents a recipe validation failure item with an actionable diagnostic hint.
    /// </summary>
    public class RecipeValidationErrorViewModel
    {
        public string ErrorMessage { get; set; }
        public string DiagnosticHint { get; set; }
        public bool HasHint => !string.IsNullOrWhiteSpace(DiagnosticHint);

        public static RecipeValidationErrorViewModel Create(string rawError)
        {
            var item = new RecipeValidationErrorViewModel
            {
                ErrorMessage = rawError ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(rawError))
            {
                return item;
            }

            // Check if unknown or plugin stepType was used
            if (rawError.IndexOf("which is not available because the required extension/plugin is not installed or active", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = Texts.Get("Recipe", "approval_hint_step_type");
            }
            // Check for SourceType issues
            else if (rawError.IndexOf("Unknown SourceType", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = string.Format(Texts.Get("Recipe", "approval_hint_source_type"), string.Join(", ", Enum.GetNames(typeof(CaptureSourceType))));
            }
            // Check for Transition target issues
            else if (rawError.IndexOf("Transition targets node", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = Texts.Get("Recipe", "approval_hint_transition");
            }
            // Check for duplicate node ID
            else if (rawError.IndexOf("Duplicate node id", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = Texts.Get("Recipe", "approval_hint_duplicate_id");
            }
            // Check for circular transitions
            else if (rawError.IndexOf("Circular transition", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = Texts.Get("Recipe", "approval_hint_circular");
            }
            // Check for missing nodes
            else if (rawError.IndexOf("must contain at least one node", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = Texts.Get("Recipe", "approval_hint_no_nodes");
            }
            // Check for missing id or name
            else if (rawError.IndexOf("missing required 'id'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     rawError.IndexOf("missing required 'name'", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = Texts.Get("Recipe", "approval_hint_id_name");
            }
            // Check for JSON parser / deserializer syntax exceptions
            else if (rawError.IndexOf("JsonException", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     (rawError.IndexOf("line", StringComparison.OrdinalIgnoreCase) >= 0 && rawError.IndexOf("position", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                item.DiagnosticHint = Texts.Get("Recipe", "approval_hint_json_syntax");
            }

            return item;
        }
    }
}
