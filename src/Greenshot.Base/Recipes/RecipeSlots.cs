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
using Newtonsoft.Json.Linq;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// The named places in a recipe where extensions put their steps. A recipe has a slot where it has a Slot node;
    /// without one, no extension changes it (nothing is inferred).
    /// </summary>
    public static class RecipeSlots
    {
        /// <summary>
        /// After the capture and the selection, before the processors
        /// </summary>
        public const string AfterCapture = "AfterCapture";

        /// <summary>
        /// After the processors, before the destinations: the image is final
        /// </summary>
        public const string BeforeExport = "BeforeExport";

        /// <summary>
        /// After the destinations
        /// </summary>
        public const string AfterExport = "AfterExport";

        /// <summary>
        /// Run by the export steps (Destinations and the pickers) for each destination, on its own copy of the capture
        /// </summary>
        public const string BeforeDestination = "BeforeDestination";

        public static readonly IReadOnlyList<string> All = new[] { AfterCapture, BeforeExport, AfterExport, BeforeDestination };

        /// <summary>
        /// The value of a slot's "Accept" for all extensions (the default)
        /// </summary>
        public const string AcceptAll = "all";

        /// <summary>
        /// The value of a slot's "Accept" for no extension
        /// </summary>
        public const string AcceptNone = "none";

        public static bool IsKnown(string slotName) => Normalize(slotName) != null;

        /// <summary>
        /// The slot name as declared here, null for an unknown name
        /// </summary>
        public static string Normalize(string slotName) => All.FirstOrDefault(s => string.Equals(s, slotName?.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// The name of a Slot node, null when it isn't one
        /// </summary>
        public static string GetSlotName(RecipeNodeConfig node)
        {
            if (node == null || !string.Equals(node.StepType, WellKnownStepTypes.Slot, StringComparison.OrdinalIgnoreCase)) return null;
            return Normalize(node.GetParameter<string>("Name"));
        }

        /// <summary>
        /// Whether the Slot node accepts the extension: "Accept" is "all" (or missing), "none" or a list of extension ids
        /// </summary>
        public static bool Accepts(RecipeNodeConfig slotNode, string extensionId)
        {
            if (slotNode == null || !slotNode.Parameters.TryGetValue("Accept", out var accept) || accept == null)
            {
                return true;
            }
            if (accept is JValue jValue)
            {
                accept = jValue.Value;
            }
            switch (accept)
            {
                case string text when string.IsNullOrWhiteSpace(text) || string.Equals(text.Trim(), AcceptAll, StringComparison.OrdinalIgnoreCase):
                    return true;
                case string text when string.Equals(text.Trim(), AcceptNone, StringComparison.OrdinalIgnoreCase):
                    return false;
                case string text:
                    return SplitList(text).Contains(extensionId, StringComparer.OrdinalIgnoreCase);
                case System.Collections.IEnumerable list:
                    return list.Cast<object>().Select(o => (o as JValue)?.Value?.ToString() ?? o?.ToString()).Contains(extensionId, StringComparer.OrdinalIgnoreCase);
                default:
                    return true;
            }
        }

        /// <summary>
        /// Splits a comma separated list (of recipe, extension or destination ids)
        /// </summary>
        public static IReadOnlyList<string> SplitList(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            return text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        }
    }
}
