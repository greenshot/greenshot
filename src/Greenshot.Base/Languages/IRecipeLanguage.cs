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
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Base.Languages
{
    /// <summary>
    /// The texts of the [Recipe] section of greenshot.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Recipe")]
    public interface IRecipeLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Border
        /// </summary>
        string ExtensionBorder { get; }

        /// <summary>
        /// Color
        /// </summary>
        string ExtensionBorderColor { get; }

        /// <summary>
        /// Adds a border around the capture before it is exported.
        /// </summary>
        string ExtensionBorderDescription { get; }

        /// <summary>
        /// Add a border
        /// </summary>
        string ExtensionBorderEnabled { get; }

        /// <summary>
        /// Width (px)
        /// </summary>
        string ExtensionBorderWidth { get; }

        /// <summary>
        /// Caption
        /// </summary>
        string ExtensionCaption { get; }

        /// <summary>
        /// Bar color
        /// </summary>
        string ExtensionCaptionBarcolor { get; }

        /// <summary>
        /// Adds a bar with a text, for example the date and time, above or below the capture.
        /// </summary>
        string ExtensionCaptionDescription { get; }

        /// <summary>
        /// Add a caption
        /// </summary>
        string ExtensionCaptionEnabled { get; }

        /// <summary>
        /// Font size
        /// </summary>
        string ExtensionCaptionFontsize { get; }

        /// <summary>
        /// Position
        /// </summary>
        string ExtensionCaptionPosition { get; }

        /// <summary>
        /// Below the capture
        /// </summary>
        string ExtensionCaptionPositionBottom { get; }

        /// <summary>
        /// Above the capture
        /// </summary>
        string ExtensionCaptionPositionTop { get; }

        /// <summary>
        /// Text
        /// </summary>
        string ExtensionCaptionText { get; }

        /// <summary>
        /// Text color
        /// </summary>
        string ExtensionCaptionTextcolor { get; }

        /// <summary>
        /// The text of the caption: ${now:yyyy-MM-dd HH:mm:ss} is the date and time, ${user.username} your user name.
        /// </summary>
        string ExtensionCaptionTextDescription { get; }

        /// <summary>
        /// Drop shadow
        /// </summary>
        string ExtensionDropshadow { get; }

        /// <summary>
        /// Darkness
        /// </summary>
        string ExtensionDropshadowDarkness { get; }

        /// <summary>
        /// Adds a drop shadow to the capture before it is exported.
        /// </summary>
        string ExtensionDropshadowDescription { get; }

        /// <summary>
        /// Add a drop shadow
        /// </summary>
        string ExtensionDropshadowEnabled { get; }

        /// <summary>
        /// Offset (px)
        /// </summary>
        string ExtensionDropshadowOffset { get; }

        /// <summary>
        /// Size (px)
        /// </summary>
        string ExtensionDropshadowSize { get; }

        /// <summary>
        /// Custom Action
        /// </summary>
        string GateCustom { get; }

        /// <summary>
        /// External Command
        /// </summary>
        string GateExternalCommand { get; }

        /// <summary>
        /// File System Access
        /// </summary>
        string GateFileSystemAccess { get; }

        /// <summary>
        /// Network Access
        /// </summary>
        string GateNetworkAccess { get; }

        /// <summary>
        /// Recipe Import
        /// </summary>
        string Import { get; }

        /// <summary>
        /// Failed to load recipe:
        /// </summary>
        string ImportFailed { get; }

        /// <summary>
        /// Import Capture Recipe
        /// </summary>
        string ImportTitle { get; }
    }
}
