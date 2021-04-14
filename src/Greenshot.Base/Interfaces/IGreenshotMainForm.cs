/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Forms;

namespace Greenshot.Base.Interfaces
{
    public interface IGreenshotMainForm : IWin32Window
    {
        /// <summary>
        /// Create the "capture window from list" list
        /// </summary>
        /// <param name="menuItem">ToolStripMenuItem</param>
        /// <param name="eventHandler">EventHandler</param>
        void AddCaptureWindowMenuItems(ToolStripMenuItem menuItem, EventHandler eventHandler);

        /// <summary>
        /// This is called indirectly from the context menu "Preferences"
        /// </summary>
        void ShowSetting();

        /// <summary>
        /// Show the about window
        /// </summary>
        void ShowAbout();
    }
}