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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Dapplo.Windows.Input.Keyboard;
using log4net;

namespace Greenshot.Base.Core;

/// <summary>
/// HotkeyManager handles hotkey registration and execution.
/// </summary>
public static class HotkeyManager
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeyManager));

    // Holds the list of hotkeys
    private static readonly List<HotkeyInfo> RegisteredHotkeys = new List<HotkeyInfo>();
    private static IDisposable _keyboardSubscription;
    private static int _hotKeyCounter = 1;

    private class HotkeyInfo
    {
        public Keys Modifiers { get; set; }
        public Keys Key { get; set; }
        public Action Handler { get; set; }
        public int Id { get; set; }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum MapType : uint
    {
        MAPVK_VK_TO_VSC = 0,
        MAPVK_VSC_TO_VK = 1,
        MAPVK_VK_TO_CHAR = 2,
        MAPVK_VSC_TO_VK_EX = 3,
        MAPVK_VK_TO_VSC_EX = 4
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetKeyNameText(uint lParam, [Out] StringBuilder lpString, int nSize);

    private static void HandleKeyboardEvent(KeyboardHookEventArgs e)
    {
        if (!e.IsKeyDown)
        {
            return;
        }

        foreach (var hotkey in RegisteredHotkeys.ToList())
        {
            if (Match(e, hotkey))
            {
                e.Handled = true;
                hotkey.Handler();
            }
        }
    }

    private static bool Match(KeyboardHookEventArgs e, HotkeyInfo hotkey)
    {
        if ((int)e.Key != (int)hotkey.Key)
        {
            return false;
        }

        bool alt = (hotkey.Modifiers & Keys.Alt) != 0;
        bool ctrl = (hotkey.Modifiers & Keys.Control) != 0;
        bool shift = (hotkey.Modifiers & Keys.Shift) != 0;
        bool win = (hotkey.Modifiers & Keys.LWin) != 0 || (hotkey.Modifiers & Keys.RWin) != 0;

        return e.IsAlt == alt &&
               e.IsControl == ctrl &&
               e.IsShift == shift &&
               e.IsWindows == win;
    }

    /// <summary>
    /// Register a hotkey with an action
    /// </summary>
    /// <param name="modifierKeyCode">The modifier, e.g.: Keys.Control, Keys.None or Keys.Alt</param>
    /// <param name="virtualKeyCode">The virtual key code</param>
    /// <param name="handler">A Action, this will be called to handle the hotkey press</param>
    /// <returns>the hotkey number, -1 if failed</returns>
    public static int RegisterHotKey(Keys modifierKeyCode, Keys virtualKeyCode, Action handler)
    {
        if (virtualKeyCode == Keys.None)
        {
            Log.Warn("Trying to register a Keys.none hotkey, ignoring");
            return 0;
        }

        _keyboardSubscription ??= KeyboardHook.KeyboardEvents.Subscribe(HandleKeyboardEvent);

        var hotkeyInfo = new HotkeyInfo
        {
            Modifiers = modifierKeyCode,
            Key = virtualKeyCode,
            Handler = handler,
            Id = _hotKeyCounter++
        };
        RegisteredHotkeys.Add(hotkeyInfo);
        return hotkeyInfo.Id;
    }

    /// <summary>
    /// Unregisters all currently registered global hotkeys and releases associated resources.
    /// </summary>
    /// <remarks>Call this method to remove all hotkey bindings and clean up keyboard event subscriptions.
    /// After calling this method, no hotkeys will be active until they are registered again.</remarks>
    public static void UnregisterHotkeys()
    {
        _keyboardSubscription?.Dispose();
        _keyboardSubscription = null;
        RegisteredHotkeys.Clear();
    }

    /// <summary>
    /// Converts a hotkey string to its localized display representation.
    /// </summary>
    /// <remarks>Use this method to present hotkey combinations in a user-friendly, localized format suitable
    /// for display in UI elements. The method parses the input string to extract modifiers and key codes, then formats
    /// them according to localization settings.</remarks>
    /// <param name="hotkeyString">A string representing the hotkey combination to be localized. The string should follow the expected format for
    /// hotkey definitions.</param>
    /// <returns>A localized string that describes the hotkey combination. The string reflects the current system language and
    /// keyboard layout.</returns>
    public static string GetLocalizedHotkeyStringFromString(string hotkeyString)
    {
        Keys virtualKeyCode = HotkeyFromString(hotkeyString);
        Keys modifiers = HotkeyModifiersFromString(hotkeyString);
        return HotkeyToLocalizedString(modifiers, virtualKeyCode);
    }

    /// <summary>
    /// Converts the specified modifier and virtual key codes to a human-readable hotkey string representation.
    /// </summary>
    /// <param name="modifierKeyCode">The modifier key or combination of modifier keys to include in the hotkey string. Typical values include
    /// Control, Alt, Shift, or combinations thereof.</param>
    /// <param name="virtualKeyCode">The virtual key code representing the main key of the hotkey. This is appended to the modifier keys in the
    /// resulting string.</param>
    /// <returns>A string that represents the combined modifier and virtual key as a hotkey, suitable for display to users.</returns>
    public static string HotkeyToString(Keys modifierKeyCode, Keys virtualKeyCode)
    {
        return HotkeyModifiersToString(modifierKeyCode) + virtualKeyCode;
    }

    /// <summary>
    /// Converts the specified modifier key code to a human-readable string representing the combination of modifier
    /// keys.
    /// </summary>
    /// <remarks>The returned string lists modifier keys in the order: Alt, Ctrl, Shift, Win. This method does
    /// not include non-modifier keys in the output.</remarks>
    /// <param name="modifierKeyCode">A bitwise combination of modifier keys from the <see cref="Keys"/> enumeration to be converted to a string.
    /// Typical values include <see cref="Keys.Alt"/>, <see cref="Keys.Control"/>, <see cref="Keys.Shift"/>, <see
    /// cref="Keys.LWin"/>, and <see cref="Keys.RWin"/>.</param>
    /// <returns>A string containing the names of all modifier keys present in <paramref name="modifierKeyCode"/>, separated by "
    /// + ". If no modifier keys are present, returns an empty string.</returns>
    public static string HotkeyModifiersToString(Keys modifierKeyCode)
    {
        StringBuilder hotkeyString = new StringBuilder();
        if ((modifierKeyCode & Keys.Alt) > 0)
        {
            hotkeyString.Append("Alt").Append(" + ");
        }

        if ((modifierKeyCode & Keys.Control) > 0)
        {
            hotkeyString.Append("Ctrl").Append(" + ");
        }

        if ((modifierKeyCode & Keys.Shift) > 0)
        {
            hotkeyString.Append("Shift").Append(" + ");
        }

        if ((modifierKeyCode & Keys.LWin) > 0 || (modifierKeyCode & Keys.RWin) > 0)
        {
            hotkeyString.Append("Win").Append(" + ");
        }

        return hotkeyString.ToString();
    }

    /// <summary>
    /// Converts the specified modifier and virtual key codes to a localized string representation of the hotkey.
    /// </summary>
    /// <remarks>The returned string is suitable for display in user interfaces and reflects the current
    /// localization settings. Use this method to present hotkey combinations in a way that is familiar to users in
    /// their language and region.</remarks>
    /// <param name="modifierKeyCode">The modifier key or combination of modifier keys to include in the hotkey string. Typical values include
    /// Control, Alt, Shift, or combinations thereof.</param>
    /// <param name="virtualKeyCode">The virtual key code representing the main key of the hotkey. This is combined with the modifier keys to form
    /// the complete hotkey string.</param>
    /// <returns>A localized string that represents the hotkey combination specified by the modifier and virtual key codes.</returns>
    public static string HotkeyToLocalizedString(Keys modifierKeyCode, Keys virtualKeyCode)
    {
        return HotkeyModifiersToLocalizedString(modifierKeyCode) + GetKeyName(virtualKeyCode);
    }

    /// <summary>
    /// Converts a set of hotkey modifier keys to a localized string representation suitable for display.
    /// </summary>
    /// <remarks>The returned string includes only the modifier keys present in the input. The order of
    /// modifiers in the string follows Alt, Control, Shift, and Windows keys.</remarks>
    /// <param name="modifierKeyCode">A bitwise combination of modifier keys from the <see cref="Keys"/> enumeration to be converted.</param>
    /// <returns>A string containing the localized names of the specified modifier keys, separated by " + ". Returns an empty
    /// string if no modifiers are specified.</returns>
    public static string HotkeyModifiersToLocalizedString(Keys modifierKeyCode)
    {
        StringBuilder hotkeyString = new StringBuilder();
        if ((modifierKeyCode & Keys.Alt) > 0)
        {
            hotkeyString.Append(GetKeyName(Keys.Alt)).Append(" + ");
        }

        if ((modifierKeyCode & Keys.Control) > 0)
        {
            hotkeyString.Append(GetKeyName(Keys.Control)).Append(" + ");
        }

        if ((modifierKeyCode & Keys.Shift) > 0)
        {
            hotkeyString.Append(GetKeyName(Keys.Shift)).Append(" + ");
        }

        if ((modifierKeyCode & Keys.LWin) > 0 || (modifierKeyCode & Keys.RWin) > 0)
        {
            hotkeyString.Append("Win").Append(" + ");
        }

        return hotkeyString.ToString();
    }

    /// <summary>
    /// Converts a string representation of keyboard modifier keys into a combination of corresponding <see cref="Keys"/> flags.
    /// </summary>
    /// <remarks>The method recognizes "Alt", "Ctrl", "Shift", and "Win" as valid modifier names. Multiple
    /// modifiers can be specified in the input string, and their corresponding flags will be combined. Unrecognized
    /// modifier names are ignored.</remarks>
    /// <param name="modifiersString">A string containing the names of modifier keys (such as "Alt", "Ctrl", "Shift", or "Win"). The string is
    /// case-insensitive and may include multiple modifiers.</param>
    /// <returns>A <see cref="Keys"/> value representing the combined modifier keys specified in <paramref
    /// name="modifiersString"/>. Returns <see cref="Keys.None"/> if no valid modifiers are found.</returns>
    public static Keys HotkeyModifiersFromString(string modifiersString)
    {
        Keys modifiers = Keys.None;
        if (!string.IsNullOrEmpty(modifiersString))
        {
            if (modifiersString.ToLower().Contains("alt"))
            {
                modifiers |= Keys.Alt;
            }

            if (modifiersString.ToLower().Contains("ctrl"))
            {
                modifiers |= Keys.Control;
            }

            if (modifiersString.ToLower().Contains("shift"))
            {
                modifiers |= Keys.Shift;
            }

            if (modifiersString.ToLower().Contains("win"))
            {
                modifiers |= Keys.LWin;
            }
        }

        return modifiers;
    }

    /// <summary>
    /// Converts a string representation of a hotkey to its corresponding <see cref="Keys"/> value.
    /// </summary>
    /// <remarks>Only the main key after the last '+' character is parsed. Modifier keys are ignored in the
    /// result. The input must match a valid <see cref="Keys"/> enumeration name.</remarks>
    /// <param name="hotkey">A string containing the hotkey name or combination. The string may include modifier keys separated by '+', with
    /// the main key at the end.</param>
    /// <returns>A <see cref="Keys"/> value representing the main key of the hotkey. Returns <see cref="Keys.None"/> if the input
    /// is null, empty, or invalid.</returns>
    public static Keys HotkeyFromString(string hotkey)
    {
        Keys key = Keys.None;
        if (!string.IsNullOrEmpty(hotkey))
        {
            if (hotkey.LastIndexOf('+') > 0)
            {
                hotkey = hotkey.Remove(0, hotkey.LastIndexOf('+') + 1).Trim();
            }

            key = (Keys)Enum.Parse(typeof(Keys), hotkey);
        }

        return key;
    }

    /// <summary>
    /// Returns a user-friendly display name for the specified keyboard key.
    /// </summary>
    /// <remarks>This method handles special cases for modifier and numpad keys to provide more readable
    /// names. The returned name is suitable for display in user interfaces where keyboard shortcuts or key names are
    /// shown.</remarks>
    /// <param name="givenKey">The keyboard key for which to retrieve the display name.</param>
    /// <returns>A string containing the display name of the specified key. If the key cannot be resolved to a display name, the
    /// method returns the string representation of the key.</returns>
    public static string GetKeyName(Keys givenKey)
    {
        StringBuilder keyName = new StringBuilder();
        const uint numpad = 55;

        Keys virtualKey = givenKey;
        string keyString;
        // Make VC's to real keys
        switch (virtualKey)
        {
            case Keys.Alt:
                virtualKey = Keys.LMenu;
                break;
            case Keys.Control:
                virtualKey = Keys.ControlKey;
                break;
            case Keys.Shift:
                virtualKey = Keys.LShiftKey;
                break;
            case Keys.Multiply:
                GetKeyNameText(numpad << 16, keyName, 100);
                keyString = keyName.ToString().Replace("*", string.Empty).Trim().ToLower();
                if (keyString.IndexOf("(", StringComparison.Ordinal) >= 0)
                {
                    return "* " + keyString;
                }

                keyString = keyString.Substring(0, 1).ToUpper() + keyString.Substring(1).ToLower();
                return keyString + " *";
            case Keys.Divide:
                GetKeyNameText(numpad << 16, keyName, 100);
                keyString = keyName.ToString().Replace("*", string.Empty).Trim().ToLower();
                if (keyString.IndexOf("(", StringComparison.Ordinal) >= 0)
                {
                    return "/ " + keyString;
                }

                keyString = keyString.Substring(0, 1).ToUpper() + keyString.Substring(1).ToLower();
                return keyString + " /";
        }

        uint scanCode = MapVirtualKey((uint)virtualKey, (uint)MapType.MAPVK_VK_TO_VSC);

        // because MapVirtualKey strips the extended bit for some keys
        switch (virtualKey)
        {
            case Keys.Left:
            case Keys.Up:
            case Keys.Right:
            case Keys.Down: // arrow keys
            case Keys.Prior:
            case Keys.Next: // page up and page down
            case Keys.End:
            case Keys.Home:
            case Keys.Insert:
            case Keys.Delete:
            case Keys.NumLock:
                Log.Debug("Modifying Extended bit");
                scanCode |= 0x100; // set extended bit
                break;
            case Keys.PrintScreen: // PrintScreen
                scanCode = 311;
                break;
            case Keys.Pause: // PrintScreen
                scanCode = 69;
                break;
        }

        scanCode |= 0x200;
        if (GetKeyNameText(scanCode << 16, keyName, 100) != 0)
        {
            string visibleName = keyName.ToString();
            if (visibleName.Length > 1)
            {
                visibleName = visibleName.Substring(0, 1) + visibleName.Substring(1).ToLower();
            }

            return visibleName;
        }

        return givenKey.ToString();
    }
}
