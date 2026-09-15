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

namespace Greenshot.UI
{
    /// <summary>
    /// Represents a recipe validation failure item with an actionable diagnostic hint.
    /// </summary>
    public class RecipeValidationErrorItem
    {
        public string ErrorMessage { get; set; }
        public string DiagnosticHint { get; set; }
        public bool HasHint => !string.IsNullOrWhiteSpace(DiagnosticHint);

        public static RecipeValidationErrorItem Create(string rawError)
        {
            var item = new RecipeValidationErrorItem
            {
                ErrorMessage = rawError ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(rawError))
            {
                return item;
            }

            // Check if stepType 'Drawable' was used (legacy name replaced by 'Annotation')
            if (rawError.IndexOf("Drawable", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = "The stepType 'Drawable' was renamed to 'Annotation' in Greenshot. In your recipe file, rename 'stepType': 'Drawable' to 'stepType': 'Annotation', and rename the 'drawables' array to 'annotations'.";
            }
            // Check if unknown or plugin stepType was used
            else if (rawError.IndexOf("which is not available because the required extension/plugin is not installed or active", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = "The stepType specified is not registered in core Greenshot. If it is provided by a plugin or extension (e.g. Jira, Imgur, OCR, etc.), verify that the plugin is installed in the Plugins folder and enabled under Greenshot Settings > Plugins.";
            }
            // Check for SourceType issues
            else if (rawError.IndexOf("Unknown SourceType", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = "Valid SourceType values are: Screen, Window, Region, AllScreens, Clipboard, File, Context.";
            }
            // Check for Transition target issues
            else if (rawError.IndexOf("Transition targets node", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = "Verify that the target node ID matches an existing node's 'id' in the recipe's 'nodes' pipeline list.";
            }
            // Check for duplicate node ID
            else if (rawError.IndexOf("Duplicate node id", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = "Each node in the recipe pipeline must have a unique 'id' identifier.";
            }
            // Check for circular transitions
            else if (rawError.IndexOf("Circular transition", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = "Recipes must form a directed acyclic graph (DAG). Ensure transitions and fallbacks do not loop back to earlier nodes.";
            }
            // Check for missing nodes
            else if (rawError.IndexOf("must contain at least one node", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = "Add at least one execution node to the 'nodes' array of the recipe.";
            }
            // Check for missing id or name
            else if (rawError.IndexOf("missing required 'id'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     rawError.IndexOf("missing required 'name'", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.DiagnosticHint = "Every recipe requires a unique 'id' and a descriptive 'name' property.";
            }
            // Check for JSON parser / deserializer syntax exceptions
            else if (rawError.IndexOf("JsonReaderException", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     rawError.IndexOf("JsonSerializationException", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     (rawError.IndexOf("line", StringComparison.OrdinalIgnoreCase) >= 0 && rawError.IndexOf("position", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                item.DiagnosticHint = "Check the recipe JSON syntax for unescaped quotes, trailing commas, or mismatched brackets.";
            }

            return item;
        }
    }
}
