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

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// The application shell: the windows and commands of Greenshot itself, which are not tied to a capture.
    /// Available as a service while Greenshot runs (not in tests or headless).
    /// </summary>
    public interface IGreenshotShell
    {
        /// <summary>
        /// A hidden top level window which owns the windows that belong to no other window (e.g. the capture window),
        /// so they don't show up in the taskbar on their own.
        /// </summary>
        IntPtr OwnerHandle { get; }

        /// <summary>
        /// Show the settings, called from the tray menu "Preferences" or a plugin "Configure"
        /// </summary>
        /// <param name="pluginName">Optional name of plugin to activate in the plugins tab.</param>
        void ShowSetting(string pluginName = null);

        /// <summary>
        /// Show the settings with a tab and/or a plugin selected
        /// </summary>
        /// <param name="pluginName">Name of the plugin to select, or null</param>
        /// <param name="tabName">Name of the tab to select, or null</param>
        void ShowSetting(string pluginName, string tabName);

        /// <summary>
        /// Show the about window
        /// </summary>
        void ShowAbout();

        /// <summary>
        /// Refresh what depends on the configuration or the language (e.g. the tray icon tooltip)
        /// </summary>
        void UpdateUi();

        /// <summary>
        /// Exit Greenshot
        /// </summary>
        void Exit();
    }
}
