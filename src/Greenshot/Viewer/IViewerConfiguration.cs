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

namespace Greenshot.Viewer
{
    /// <summary>
    /// The settings of the viewer windows, a change in a viewer becomes the default for the next viewers
    /// </summary>
    [IniSection("Viewer")]
    [Description("Greenshot viewer configuration")]
    public interface IViewerConfiguration : IIniSection
    {
        [Description("Maximum width of a new viewer window in pixels, a bigger capture is shown smaller at first")]
        [DefaultValue(800)]
        int MaxWidth { get; set; }

        [Description("Maximum height of a new viewer window in pixels, a bigger capture is shown smaller at first")]
        [DefaultValue(800)]
        int MaxHeight { get; set; }

        [Description("Keep the viewer windows on top of the other windows")]
        [DefaultValue(false)]
        bool AlwaysOnTop { get; set; }

        [Description("Show the title bar and the border of the viewer windows, without them only the capture is shown")]
        [DefaultValue(true)]
        bool ShowTitleBar { get; set; }

        [Description("Show the captured mouse pointer in the viewer windows")]
        [DefaultValue(true)]
        bool ShowCursor { get; set; }

        [Description("Was the hint shown that the viewer doesn't save the capture? Greenshot shows it once, with the first viewer")]
        bool UnsavedHintShown { get; set; }
    }
}
