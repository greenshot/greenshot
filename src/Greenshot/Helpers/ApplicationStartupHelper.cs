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

using System;
using System.IO;
using System.Windows.Threading;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;
using log4net;

namespace Greenshot.Helpers
{
    internal static class ApplicationStartupHelper
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ApplicationStartupHelper));
        private static readonly CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();

        /// <summary>
        /// Performs initialization tasks for the application's first launch and displays an informational message to the user.
        /// </summary>
        /// <remarks>This method marks the application as no longer in its first launch state and shows a
        /// balloon notification with a localized message. The notification includes a callback that opens the settings
        /// dialog when triggered. Typically called during application startup to provide guidance to new
        /// users.</remarks>
        public static void FirstLaunch()
        {
            config.IsFirstLaunch = false;
            LOG.Info("FirstLaunch: Created new configuration, showing balloon.");
            var notifyIconClassicMessageHandler = SimpleServiceProvider.Current.GetInstance<INotificationService>();

            notifyIconClassicMessageHandler.ShowInfoMessage(
                Language.GetFormattedString(LangKey.tooltip_firststart, HotkeyManager.GetLocalizedHotkeyStringFromString(config.RegionHotkey)),
                TimeSpan.FromMinutes(10),
                () =>
                {
                    var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>();
                    mainForm.ShowSetting();
                });
        }

        /// <summary>
        /// Reloads the application configuration and updates the user interface and hotkey settings accordingly.
        /// </summary>
        /// <remarks>This method refreshes the configuration from persistent storage, updates UI elements
        /// to reflect any changes, and re-registers hotkeys. It should be called when configuration settings are
        /// modified at runtime to ensure the application state remains consistent.</remarks>
        public static void ReloadConfig()
        {
            try
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    // Make sure the current hotkeys are disabled
                    HotkeyManager.UnregisterHotkeys();
                    IniConfig.Reload();
                    var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>();
                    // Even update language when needed
                    mainForm.UpdateUi();
                    // Update the hotkey
                    HotkeyHelper.RegisterHotkeys();
                });
            }
            catch (Exception ex)
            {
                LOG.Warn("Exception while reloading configuration: ", ex);
            }
        }

        /// <summary>
        /// Opens the specified file and initiates a capture operation if the file exists.
        /// </summary>
        /// <remarks>If the file does not exist at the specified path, the operation is not performed and
        /// a warning is logged. The capture operation is executed asynchronously on the current dispatcher.</remarks>
        /// <param name="filePath">The full path to the file to be opened. Must refer to an existing file.</param>
        public static void OpenFile(string filePath)
        {
            LOG.InfoFormat("Open file requested: {0}", filePath);
            if (File.Exists(filePath))
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    ()=> {
                        CaptureHelper.CaptureFile(filePath);
                    });
            }
            else
            {
                LOG.Warn("No such file: " + filePath);
            }
        }
    }
}
