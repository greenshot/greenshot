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

using Dapplo.Ini;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Helper to safely determine whether the Recipe Editor plugin is installed and enabled.
    /// </summary>
    public static class RecipeConfigHelper
    {
        /// <summary>
        /// Returns true if the specified configuration is non-null and has Enabled = true.
        /// </summary>
        public static bool IsRecipeFeatureEnabled(IRecipeConfiguration recipeConfig)
        {
            return recipeConfig != null && recipeConfig.Enabled;
        }

        /// <summary>
        /// Returns true if the Recipe Editor plugin is registered in configuration and enabled;
        /// otherwise false (e.g. plugin not installed or disabled in settings).
        /// </summary>
        public static bool IsRecipeFeatureEnabled() => IsRecipeFeatureEnabled(TryGetRecipeConfiguration());

        /// <summary>
        /// Returns the <see cref="IRecipeConfiguration"/> section if it is registered, otherwise null.
        /// The section is registered by the Recipe Editor plugin, so it is missing when the plugin isn't installed
        /// or when this is called before the plugins are loaded.
        /// Use this instead of <c>IniConfigRegistry.GetSection&lt;IRecipeConfiguration&gt;()</c>, which throws for a missing section.
        /// </summary>
        public static IRecipeConfiguration TryGetRecipeConfiguration()
        {
            return IniConfigRegistry.TryGetSection<IRecipeConfiguration>(out var recipeConfiguration) ? recipeConfiguration : null;
        }
    }
}
