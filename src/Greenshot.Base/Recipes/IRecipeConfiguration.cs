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
using System.Runtime.Serialization;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Configuration for the Recipe Editor plugin.
    /// </summary>
    [IniSection("Recipe")]
    [Description("Greenshot Recipe Editor configuration")]
    public interface IRecipeConfiguration : IIniSection, IAfterLoad, IBeforeSave
    {
        [DataMember(Name = "EnableRecipeFeature")]
        [Description("Whether to enable the recipe editor extension.")]
        [DefaultValue(true)]
        bool Enabled { get; set; }

        [Description("Whether to show a quicklink in the tray context menu for configuring or opening this plugin.")]
        [DefaultValue(false)]
        bool QuicklinkEnabled { get; set; }

        [Description("Semicolon-separated list of explicit recipe file paths to load. Automatic directory scanning is disabled for security.")]
        [DefaultValue(null)]
        string RecipeFiles { get; set; }
    }
}
