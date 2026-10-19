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


using System.Runtime.InteropServices;
using Dapplo.Windows.User32.Enums;

namespace Greenshot.Base.Native
{
    /// <summary>
    /// Narrator, NVDA, JAWS and other screen readers tell Windows that they run (SPI_SETSCREENREADER)
    /// </summary>
    public static class ScreenReader
    {
        // Dapplo.Windows has SPI_GETSCREENREADER, but no SystemParametersInfo overload which reads a value into a uint (yet)
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(SystemParametersInfoActions uiAction, uint uiParam, out int pvParam, SystemParametersInfoBehaviors fWinIni);

        /// <summary>
        /// True while a screen reader runs, asked each time: it can be started and stopped at any moment.
        /// Any application can set the flag and leave it on, so it only decides a default the user can change at once (Space in the capture window).
        /// </summary>
        public static bool IsRunning => SystemParametersInfo(SystemParametersInfoActions.SPI_GETSCREENREADER, 0, out int running, SystemParametersInfoBehaviors.None) && running != 0;
    }
}
