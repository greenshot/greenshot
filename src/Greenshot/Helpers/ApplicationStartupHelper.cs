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

using System;
using System.IO;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Recipes.Triggers;
using Greenshot.Views;
using log4net;
using Greenshot.Base.Threading;
using System.Threading;

using Greenshot.Base.Languages;

namespace Greenshot.Helpers
{
    internal static class ApplicationStartupHelper
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ApplicationStartupHelper));
        private static readonly ICoreConfiguration config = IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <summary>
        /// Show the welcome window: at the first start, or when greenshot-cli asks for it
        /// </summary>
        public static void ShowWelcome()
        {
            config.WelcomeShown = true;
            LOG.Info("Showing the welcome window.");
            WelcomeWindow.ShowWelcome();
        }

        /// <summary>
        /// Reads the configuration again (--reload) and applies it: language, tray tooltip and hotkeys.
        /// </summary>
        /// <remarks>greenshot.ini is locked while Greenshot runs (only readable for others), so what can change is greenshot-fixed.ini,
        /// e.g. an administrator who rolls out a new value. greenshot-defaults.ini only fills keys which are missing in greenshot.ini, and
        /// greenshot.ini holds every key once it was saved, so a changed default rarely shows.</remarks>
        public static void ReloadConfig()
        {
            // Hotkeys and the UI belong to the UI thread: runs inline there, otherwise posted to it
            UiDispatcher.Current.RunOnUiAsync(ReloadConfigOnUi, CancellationToken.None).FireAndLog("Reload the configuration", LOG);
        }

        private static void ReloadConfigOnUi()
        {
            try
            {
                // Make sure the current hotkeys are disabled
                HotkeyManager.UnregisterHotkeys();
                IniConfigRegistry.Get().Reload();
                // The language of the reloaded configuration (e.g. fixed in greenshot-fixed.ini), the texts and open windows follow (LanguageChanged)
                Texts.SetLanguage(IniConfigRegistry.GetSection<ICoreConfiguration>().Language);
                var shell = SimpleServiceProvider.Current.GetInstance<IGreenshotShell>(isOptional: true);
                shell?.UpdateUi();
                // Update the hotkey
                TriggerManager.Instance.RestartHotkeyTriggers();
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
                UiDispatcher.Current.InvokeAsync(() => CaptureHelper.CaptureFile(filePath)).FireAndLog("Open " + filePath, LOG);
            }
            else
            {
                LOG.Warn("No such file: " + filePath);
            }
        }
    }
}
