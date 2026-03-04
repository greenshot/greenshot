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
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Configuration;
using Greenshot.Editor.Destinations;
using Greenshot.Forms;
using log4net;

namespace Greenshot.Helpers;

/// <summary>
/// Very specific code mapping the configuration and the capturehelper for hotkeys
/// </summary>
internal static class HotkeyHelper
{
    private static readonly ILog LOG = LogManager.GetLogger(typeof(ApplicationStartupHelper));
    private static readonly CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();
    
    /// <summary>
    /// Registers all hotkeys as configured, displaying a dialog in case of hotkey conflicts with other tools.
    /// </summary>
    /// <param name="ignoreFailedRegistration">if true, a failed hotkey registration will not be reported to the user - the hotkey will simply not be registered</param>
    /// <returns>Whether the hotkeys could be registered to the users content. This also applies if conflicts arise and the user decides to ignore these (i.e. not to register the conflicting hotkey).</returns>
    public static bool RegisterHotkeys(bool ignoreFailedRegistration = false)
    {
        bool success = true;
        StringBuilder failedKeys = new StringBuilder();

        if (!RegisterWrapper(failedKeys, "CaptureRegion", "RegionHotkey", () => CaptureHelper.CaptureRegion(true), ignoreFailedRegistration))
        {
            success = false;
        }

        if (!RegisterWrapper(failedKeys, "CaptureWindow", "WindowHotkey", () =>
        {
            if (config.CaptureWindowsInteractive)
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

        if (!RegisterWrapper(failedKeys, "CaptureFullScreen", "FullscreenHotkey", () => CaptureHelper.CaptureFullscreen(true, config.ScreenCaptureMode), ignoreFailedRegistration))
        {
            success = false;
        }

        if (!RegisterWrapper(failedKeys, "CaptureLastRegion", "LastregionHotkey", () => CaptureHelper.CaptureLastRegion(true), ignoreFailedRegistration))
        {
            success = false;
        }

        if (!RegisterWrapper(failedKeys, "CaptureClipboard", "ClipboardHotkey", () => CaptureHelper.CaptureClipboard(DestinationHelper.GetDestination(EditorDestination.DESIGNATION)), true))
        {
            success = false;
        }

        if (!success)
        {
            if (!ignoreFailedRegistration)
            {
                success = HandleFailedHotkeyRegistration(failedKeys.ToString());
            }
            else
            {
                // if failures have been ignored, the config has probably been updated
                if (config.IsDirty)
                {
                    IniConfig.Save();
                }
            }
        }

        return success || ignoreFailedRegistration;
    }

    private static bool RegisterWrapper(StringBuilder failedKeys, string functionName, string configurationKey, Action handler, bool ignoreFailedRegistration)
    {
        IniValue hotkeyValue = config.Values[configurationKey];
        var hotkeyStringValue = hotkeyValue.Value?.ToString();
        if (string.IsNullOrEmpty(hotkeyStringValue))
        {
            return true;
        }

        try
        {
            bool success = RegisterHotkey(failedKeys, functionName, hotkeyStringValue, handler);
            if (!success && ignoreFailedRegistration)
            {
                LOG.DebugFormat("Ignoring failed hotkey registration for {0}, with value '{1}', resetting to 'None'.", functionName, hotkeyStringValue);
                config.Values[configurationKey].Value = Keys.None.ToString();
                config.IsDirty = true;
            }

            return success;
        }
        catch (Exception ex)
        {
            LOG.Warn(ex);
            LOG.WarnFormat("Restoring default hotkey for {0}, stored under {1} from '{2}' to '{3}'", functionName, configurationKey, hotkeyStringValue, hotkeyValue.Attributes.DefaultValue);
            // when getting an exception the key wasn't found: reset the hotkey value
            hotkeyValue.UseValueOrDefault(null);
            hotkeyValue.ContainingIniSection.IsDirty = true;
            return RegisterHotkey(failedKeys, functionName, hotkeyStringValue, handler);
        }
    }

    /// <summary>
    /// Displays a dialog for the user to choose how to handle hotkey registration failures:
    /// retry (allowing to shut down the conflicting application before),
    /// ignore (not registering the conflicting hotkey and resetting the respective config to "None", i.e. not trying to register it again on next startup)
    /// abort (do nothing about it)
    /// </summary>
    /// <param name="failedKeys">comma separated list of the hotkeys that could not be registered, for display in dialog text</param>
    /// <returns></returns>
    private static bool HandleFailedHotkeyRegistration(string failedKeys)
    {
        bool success = false;
        var warningTitle = Language.GetString(LangKey.warning);
        var message = string.Format(Language.GetString(LangKey.warning_hotkeys), failedKeys, IsOneDriveBlockingHotkey() ? " (OneDrive)" : "");
        var mainForm = SimpleServiceProvider.Current.GetInstance<MainForm>();
        DialogResult dr = MessageBox.Show(message, warningTitle, MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Exclamation);
        if (dr == DialogResult.Retry)
        {
            LOG.DebugFormat("Re-trying to register hotkeys");
            HotkeyManager.UnregisterHotkeys();
            success = RegisterHotkeys(false);
        }
        else if (dr == DialogResult.Ignore)
        {
            LOG.DebugFormat("Ignoring failed hotkey registration");
            HotkeyManager.UnregisterHotkeys();
            success = RegisterHotkeys(true);
        }

        return success;
    }

    /// <summary>
    /// Check if OneDrive is blocking hotkeys
    /// </summary>
    /// <returns>true if one-drive has hotkeys turned on</returns>
    private static bool IsOneDriveBlockingHotkey()
    {
        if (!WindowsVersion.IsWindows10OrLater)
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

        var lines = File.ReadAllLines(oneDriveSettingsFile);
        if (lines.Length < 2)
        {
            return false;
        }

        return "2".Equals(lines[1]);
    }

    /// <summary>
    /// Helper method to cleanly register a hotkey
    /// </summary>
    /// <param name="failedKeys">StringBuilder</param>
    /// <param name="functionName">string</param>
    /// <param name="hotkeyString">string</param>
    /// <param name="handler">HotKeyHandler</param>
    /// <returns>bool</returns>
    private static bool RegisterHotkey(StringBuilder failedKeys, string functionName, string hotkeyString, Action handler)
    {
        Keys modifierKeyCode = HotkeyManager.HotkeyModifiersFromString(hotkeyString);
        Keys virtualKeyCode = HotkeyManager.HotkeyFromString(hotkeyString);
        if (!Keys.None.Equals(virtualKeyCode))
        {
            if (HotkeyManager.RegisterHotKey(modifierKeyCode, virtualKeyCode, handler) < 0)
            {
                LOG.DebugFormat("Failed to register {0} to hotkey: {1}", functionName, hotkeyString);
                if (failedKeys.Length > 0)
                {
                    failedKeys.Append(", ");
                }

                failedKeys.Append(hotkeyString);
                return false;
            }

            LOG.DebugFormat("Registered {0} to hotkey: {1}", functionName, hotkeyString);
        }
        else
        {
            LOG.InfoFormat("Skipping hotkey registration for {0}, no hotkey set!", functionName);
        }

        return true;
    }
}
