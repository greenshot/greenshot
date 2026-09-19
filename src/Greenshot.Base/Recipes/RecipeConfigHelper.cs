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
using System.Linq;
using Dapplo.Ini;
using log4net;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Helper to safely determine whether the Recipe Editor plugin is installed and enabled.
    /// </summary>
    public static class RecipeConfigHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeConfigHelper));

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
        public static bool IsRecipeFeatureEnabled()
        {
            try
            {
                var iniConfig = IniConfigRegistry.Get();
                if (iniConfig == null) return false;

                var sections = iniConfig.GetSections();
                if (sections == null) return false;

                foreach (var section in sections)
                {
                    if (section is IRecipeConfiguration recipeConfig)
                    {
                        return recipeConfig.Enabled;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Debug("Error querying IRecipeConfiguration from IniConfigRegistry", ex);
                return false;
            }
        }
    }
}
