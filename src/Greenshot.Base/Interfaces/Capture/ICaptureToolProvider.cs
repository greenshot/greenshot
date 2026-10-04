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

using System.Collections.Generic;

namespace Greenshot.Base.Interfaces.Capture
{
    /// <summary>
    /// Adds capture tools to the CaptureWindow, e.g. from a plugin.
    /// Register an implementation with SimpleServiceProvider.Current.AddService&lt;ICaptureToolProvider&gt;(...) in the plugin's Initialize.
    /// </summary>
    public interface ICaptureToolProvider
    {
        /// <summary>
        /// New instances of the tools, called every time a CaptureWindow opens.
        /// The tools come after the built-in ones (region, window, text): a mode or shortcut key which is already taken does not switch to them.
        /// </summary>
        IEnumerable<ICaptureTool> CreateTools();
    }
}
