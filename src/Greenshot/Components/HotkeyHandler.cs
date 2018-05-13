#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dapplo.Addons;
using Dapplo.CaliburnMicro;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Configuration;
using Greenshot.Forms;
using Greenshot.Helpers;

namespace Greenshot.Components
{
    /// <summary>
    /// This startup action registers the hotkeys
    /// </summary>
    [StartupOrder((int)GreenshotUiStartupOrder.Hotkeys), ShutdownOrder(int.MinValue)]
    public class HotkeyHandler : IUiStartup, IUiShutdown
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly WindowHandle _windowHandle;
        private static HotkeyHandler _instance;

        public HotkeyHandler(ICoreConfiguration coreConfiguration, WindowHandle windowHandle)
        {
            _instance = this;
            _coreConfiguration = coreConfiguration;
            _windowHandle = windowHandle;
        }

        public void Start()
        {
            Log.Debug().WriteLine("Registering hotkeys");
            // Make sure all hotkeys pass this window!
            HotkeyControl.RegisterHotkeyHwnd(_windowHandle.Handle);
            
            RegisterHotkeys(false);

            Log.Debug().WriteLine("Started hotkeys");
        }

        public void Shutdown()
        {
            Log.Debug().WriteLine("Stopping hotkeys");

            // Make sure hotkeys are disabled
            try
            {
                HotkeyControl.UnregisterHotkeys();
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Error unregistering hotkeys!");
            }

        }

        #region hotkeys

        /// <summary>
        ///     Helper method to cleanly register a hotkey
        /// </summary>
        /// <param name="failedKeys"></param>
        /// <param name="functionName"></param>
        /// <param name="hotkeyString"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        private static bool RegisterHotkey(StringBuilder failedKeys, string functionName, string hotkeyString, HotKeyHandler handler)
        {
            var modifierKeyCode = HotkeyControl.HotkeyModifiersFromString(hotkeyString);
            var virtualKeyCode = HotkeyControl.HotkeyFromString(hotkeyString);
            if (!Keys.None.Equals(virtualKeyCode))
            {
                if (HotkeyControl.RegisterHotKey(modifierKeyCode, virtualKeyCode, handler) < 0)
                {
                    Log.Debug().WriteLine("Failed to register {0} to hotkey: {1}", functionName, hotkeyString);
                    if (failedKeys.Length > 0)
                    {
                        failedKeys.Append(", ");
                    }
                    failedKeys.Append(hotkeyString);
                    return false;
                }
                Log.Debug().WriteLine("Registered {0} to hotkey: {1}", functionName, hotkeyString);
            }
            else
            {
                Log.Info().WriteLine("Skipping hotkey registration for {0}, no hotkey set!", functionName);
            }
            return true;
        }

        private bool RegisterWrapper(StringBuilder failedKeys, string functionName, string configurationKey, HotKeyHandler handler, bool ignoreFailedRegistration)
        {
            var hotkeyValue = _coreConfiguration[configurationKey];
            try
            {
                var success = RegisterHotkey(failedKeys, functionName, hotkeyValue.Value.ToString(), handler);
                if (!success && ignoreFailedRegistration)
                {
                    Log.Debug().WriteLine("Ignoring failed hotkey registration for {0}, with value '{1}', resetting to 'None'.", functionName, hotkeyValue);
                    _coreConfiguration[configurationKey].Value = Keys.None.ToString();
                }
                return success;
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine(ex);
                Log.Warn().WriteLine("Restoring default hotkey for {0}, stored under {1} from '{2}' to '{3}'", functionName, configurationKey, hotkeyValue.Value,
                    hotkeyValue.DefaultValue);
                // when getting an exception the key wasn't found: reset the hotkey value
                hotkeyValue.ResetToDefault();
                return RegisterHotkey(failedKeys, functionName, hotkeyValue.Value.ToString(), handler);
            }
        }

        /// <summary>
        ///     Registers all hotkeys as configured, displaying a dialog in case of hotkey conflicts with other tools.
        /// </summary>
        /// <returns>
        ///     Whether the hotkeys could be registered to the users content. This also applies if conflicts arise and the
        ///     user decides to ignore these (i.e. not to register the conflicting hotkey).
        /// </returns>
        public static bool RegisterHotkeys()
        {
            return _instance.RegisterHotkeys(false);
        }

        /// <summary>
        ///     Registers all hotkeys as configured, displaying a dialog in case of hotkey conflicts with other tools.
        /// </summary>
        /// <param name="ignoreFailedRegistration">
        ///     if true, a failed hotkey registration will not be reported to the user - the
        ///     hotkey will simply not be registered
        /// </param>
        /// <returns>
        ///     Whether the hotkeys could be registered to the users content. This also applies if conflicts arise and the
        ///     user decides to ignore these (i.e. not to register the conflicting hotkey).
        /// </returns>
        public bool RegisterHotkeys(bool ignoreFailedRegistration)
        {
            var success = true;
            var failedKeys = new StringBuilder();

            if (!RegisterWrapper(failedKeys, "CaptureRegion", "RegionHotkey",
                () =>
                {
                    CaptureHelper.CaptureRegion(true);

                }, ignoreFailedRegistration))
            {
                success = false;
            }
            if (!RegisterWrapper(failedKeys, "CaptureWindow", "WindowHotkey", () =>
            {
                if (_coreConfiguration.CaptureWindowsInteractive)
                {
                    CaptureHelper.CaptureWindowInteractive(true);
                }
                else
                {
                    CaptureHelper.CaptureWindow(true);
                }
            }, ignoreFailedRegistration))
            {
                success = false;
            }
            if (!RegisterWrapper(failedKeys, "CaptureFullScreen", "FullscreenHotkey",
                () =>  CaptureHelper.CaptureFullscreen(true, _coreConfiguration.ScreenCaptureMode), ignoreFailedRegistration))
            {
                success = false;
            }
            if (!RegisterWrapper(failedKeys, "CaptureLastRegion", "LastregionHotkey", () => CaptureHelper.CaptureLastRegion(true), ignoreFailedRegistration))
            {
                success = false;
            }
            if (_coreConfiguration.IECapture)
            {
                if (!RegisterWrapper(failedKeys, "CaptureIE", "IEHotkey", () =>
                {
                    if (_coreConfiguration.IECapture)
                    {
                        CaptureHelper.CaptureIe(true, null);
                    }
                }, ignoreFailedRegistration))
                {
                    success = false;
                }
            }

            if (!success && !ignoreFailedRegistration)
            {
                success = HandleFailedHotkeyRegistration(failedKeys.ToString());
            }

            return success || ignoreFailedRegistration;
        }

        /// <summary>
        ///     Check if OneDrive is blocking hotkeys
        /// </summary>
        /// <returns>true if onedrive has hotkeys turned on</returns>
        private static bool IsOneDriveBlockingHotkey()
        {
            if (!WindowsVersion.IsWindows10)
            {
                return false;
            }
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var oneDriveSettingsPath = Path.Combine(localAppData, @"Microsoft\OneDrive\settings\Personal");
            if (!Directory.Exists(oneDriveSettingsPath))
            {
                return false;
            }
            var oneDriveSettingsFile = Directory.GetFiles(oneDriveSettingsPath, "*_screenshot.dat").FirstOrDefault();
            if (oneDriveSettingsFile == null || !File.Exists(oneDriveSettingsFile))
            {
                return false;
            }
            var screenshotSetting = File.ReadAllLines(oneDriveSettingsFile).Skip(1).Take(1).First();
            return "2".Equals(screenshotSetting);
        }

        /// <summary>
        ///     Displays a dialog for the user to choose how to handle hotkey registration failures:
        ///     retry (allowing to shut down the conflicting application before),
        ///     ignore (not registering the conflicting hotkey and resetting the respective config to "None", i.e. not trying to
        ///     register it again on next startup)
        ///     abort (do nothing about it)
        /// </summary>
        /// <param name="failedKeys">comma separated list of the hotkeys that could not be registered, for display in dialog text</param>
        /// <returns></returns>
        private bool HandleFailedHotkeyRegistration(string failedKeys)
        {
            var success = false;
            var warningTitle = Language.GetString(LangKey.warning);
            var message = string.Format(Language.GetString(LangKey.warning_hotkeys), failedKeys, IsOneDriveBlockingHotkey() ? " (OneDrive)" : "");
            var dr = MessageBox.Show(MainForm.Instance, message, warningTitle, MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Exclamation);
            if (dr == DialogResult.Retry)
            {
                Log.Debug().WriteLine("Re-trying to register hotkeys");
                HotkeyControl.UnregisterHotkeys();
                success = RegisterHotkeys(false);
            }
            else if (dr == DialogResult.Ignore)
            {
                Log.Debug().WriteLine("Ignoring failed hotkey registration");
                HotkeyControl.UnregisterHotkeys();
                success = RegisterHotkeys(true);
            }
            return success;
        }

        #endregion

    }
}
