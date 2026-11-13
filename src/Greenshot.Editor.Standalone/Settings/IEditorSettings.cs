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
using System.ComponentModel;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;

namespace Greenshot.Editor.Standalone.Settings
{
    /// <summary>
    /// The settings of the editor, in its own greenshot-editor.ini. What Greenshot decides (e.g. the language) is read from
    /// greenshot.ini, which the editor never writes.
    /// </summary>
    [IniSection("Editor")]
    [Description("Greenshot editor settings")]
    public interface IEditorSettings : IIniSection
    {
        [Description("The language of the editor (e.g. en-US); empty for the language of Greenshot, or of the system when Greenshot has none")]
        [DefaultValue("")]
        string Language { get; set; }

        [Description("Light, Dark or empty to follow the system")]
        [DefaultValue("")]
        string Theme { get; set; }

        [Description("The folder with the stamps; empty for the folder Stamps next to the settings")]
        [DefaultValue("")]
        string StampsPath { get; set; }

        [Description("The folder which was last used to open or save a file")]
        [DefaultValue("")]
        string LastDirectory { get; set; }

        [Description("The emoji which were used last, separated by spaces")]
        [DefaultValue("")]
        string RecentEmojis { get; set; }

        [Description("The width of the window, 0 for the default")]
        [DefaultValue(0)]
        int WindowWidth { get; set; }

        [Description("The height of the window, 0 for the default")]
        [DefaultValue(0)]
        int WindowHeight { get; set; }

        [Description("Is the window maximized")]
        [DefaultValue(false)]
        bool WindowMaximized { get; set; }
    }
}
