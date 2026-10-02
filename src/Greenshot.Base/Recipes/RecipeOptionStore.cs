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
using Dapplo.Ini;
using log4net;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Reads and writes the values of recipe options. They are stored in the [RecipeOptions] section of greenshot.ini as
    /// "&lt;recipe id&gt;.&lt;option key&gt;", only when they differ from the default, and never in the recipe file:
    /// changing a value doesn't change the approved content of the file.
    /// Only the user sets them (settings, quick settings, greenshot.ini); triggers, the command line and AI tools can't.
    /// </summary>
    public static class RecipeOptionStore
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeOptionStore));
        private static readonly object SyncLock = new object();

        /// <summary>
        /// Used when the ini section isn't registered (tests, design time)
        /// </summary>
        private static readonly Dictionary<string, string> Fallback = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Raised after a value was changed or reset, the sender is the recipe id
        /// </summary>
        public static event EventHandler ValuesChanged;

        public static string StorageKey(string recipeId, string optionKey) => $"{recipeId}.{optionKey}";

        /// <summary>
        /// The current value of the option of the recipe: the stored value, or the default of the option
        /// </summary>
        public static object GetValue(CaptureRecipe recipe, RecipeOption option)
        {
            if (option == null) return null;
            if (recipe?.Id == null) return option.GetDefaultValue();

            string raw;
            lock (SyncLock)
            {
                var values = ReadValues();
                if (values == null || !values.TryGetValue(StorageKey(recipe.Id, option.Key), out raw))
                {
                    return option.GetDefaultValue();
                }
            }

            if (option.TryConvert(raw, out var value))
            {
                return value;
            }

            Log.WarnFormat("The value '{0}' of option '{1}' of recipe '{2}' doesn't fit the option, using the default.", raw, option.Key, recipe.Id);
            return option.GetDefaultValue();
        }

        /// <summary>
        /// The current value of the option with the key, false when the recipe doesn't declare it
        /// </summary>
        public static bool TryGetValue(CaptureRecipe recipe, string optionKey, out object value)
        {
            value = null;
            var option = recipe?.FindOption(optionKey);
            if (option == null) return false;
            value = GetValue(recipe, option);
            return true;
        }

        /// <summary>
        /// Stores the value, removes it when it is the default. A value which doesn't fit the option is ignored.
        /// </summary>
        public static void SetValue(string recipeId, RecipeOption option, object value)
        {
            if (string.IsNullOrEmpty(recipeId) || option == null) return;
            if (!option.TryConvert(value, out var converted))
            {
                Log.WarnFormat("Ignoring value '{0}' for option '{1}' of recipe '{2}', it doesn't fit the option.", value, option.Key, recipeId);
                return;
            }

            string key = StorageKey(recipeId, option.Key);
            string stored = option.ToStorageString(converted);
            bool isDefault = string.Equals(stored, option.ToStorageString(option.GetDefaultValue()), StringComparison.Ordinal);
            bool changed;
            lock (SyncLock)
            {
                var values = new Dictionary<string, string>(ReadValues() ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
                values.TryGetValue(key, out var previous);
                if (isDefault)
                {
                    changed = values.Remove(key);
                }
                else
                {
                    changed = !string.Equals(previous, stored, StringComparison.Ordinal);
                    values[key] = stored;
                }
                if (changed)
                {
                    WriteValues(values);
                }
            }

            if (changed)
            {
                ValuesChanged?.Invoke(recipeId, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Removes all stored values of the recipe, its options are back at their defaults
        /// </summary>
        public static void Reset(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return;
            string prefix = recipeId + ".";
            bool changed;
            lock (SyncLock)
            {
                var values = new Dictionary<string, string>(ReadValues() ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
                var keys = values.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var key in keys)
                {
                    values.Remove(key);
                }
                changed = keys.Count > 0;
                if (changed)
                {
                    WriteValues(values);
                }
            }

            if (changed)
            {
                ValuesChanged?.Invoke(recipeId, EventArgs.Empty);
            }
        }

        private static IRecipeOptionsConfiguration Section =>
            IniConfigRegistry.TryGetSection<IRecipeOptionsConfiguration>(out var section) ? section : null;

        private static IDictionary<string, string> ReadValues()
        {
            var section = Section;
            return section != null ? section.Values : Fallback;
        }

        private static void WriteValues(Dictionary<string, string> values)
        {
            var section = Section;
            if (section != null)
            {
                // A new instance, so the section notices the change and saves it
                section.Values = values;
                return;
            }

            Fallback.Clear();
            foreach (var pair in values)
            {
                Fallback[pair.Key] = pair.Value;
            }
        }
    }
}
