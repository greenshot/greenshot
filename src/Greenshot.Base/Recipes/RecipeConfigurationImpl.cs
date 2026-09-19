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
using Dapplo.Ini;

namespace Greenshot.Base.Recipes
{
    public partial class RecipeConfigurationImpl : IRecipeConfiguration
    {
        public void OnAfterLoad()
        {
            // Backward-compatibility: migrate legacy [Core] EnableRecipeFeature if present
            try
            {
                var iniConfig = IniConfigRegistry.Get();
                if (iniConfig != null)
                {
                    var coreSection = iniConfig.GetSection("Core");
                    var legacyVal = coreSection?.GetRawValue("EnableRecipeFeature");
                    if (!string.IsNullOrEmpty(legacyVal) && bool.TryParse(legacyVal, out bool parsed))
                    {
                        Enabled = parsed;
                    }
                }
            }
            catch
            {
                // Ignore migration failure
            }
        }

        public bool OnBeforeSave()
        {
            return true;
        }
    }
}
