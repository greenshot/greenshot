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
using System.Threading.Tasks;
using Greenshot.Base.Core;

namespace Greenshot.Pipeline
{
    /// <summary>
    /// An interactive selector which can prepare its window, and keep the windows to snap to, while the screen is captured
    /// </summary>
    public interface ICaptureWindowPreparer
    {
        /// <summary>
        /// Called from the thread pool right before the screen is captured for an interactive selection. Doesn't wait for the window.
        /// </summary>
        /// <param name="snapWindows">Task which gets the windows to snap to, next to the capture</param>
        void PrepareWindow(Task<List<WindowDetails>> snapWindows);

        /// <summary>
        /// The windows to snap to of the last PrepareWindow, when that was recent. Each one is taken once.
        /// </summary>
        bool TryTakeSnapWindows(out Task<List<WindowDetails>> snapWindows);
    }
}
