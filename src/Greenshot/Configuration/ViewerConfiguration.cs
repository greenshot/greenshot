/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Base.IniFile;

namespace Greenshot.Configuration
{
    /// <summary>
    /// Configuration for the Image Viewer
    /// </summary>
    [IniSection("Viewer", Description = "Greenshot Image Viewer configuration")]
    public class ViewerConfiguration : IniSection
    {
        [IniProperty("MaxWidth", Description = "Maximum width of the viewer window", DefaultValue = "800")]
        public int MaxWidth { get; set; }

        [IniProperty("MaxHeight", Description = "Maximum height of the viewer window", DefaultValue = "800")]
        public int MaxHeight { get; set; }

        [IniProperty("AlwaysOnTop", Description = "Whether the viewer window stays on top", DefaultValue = "False")]
        public bool AlwaysOnTop { get; set; }

        [IniProperty("ShowTitle", Description = "Whether to show the title bar", DefaultValue = "True")]
        public bool ShowTitle { get; set; }

        [IniProperty("ShowCursor", Description = "Whether to show the captured cursor", DefaultValue = "True")]
        public bool ShowCursor { get; set; }

        [IniProperty("FirstUsageShown", Description = "Whether the first usage message has been shown", DefaultValue = "False")]
        public bool FirstUsageShown { get; set; }
    }
}
