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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// A simple control that allows the user to select pretty much any valid hotkey combination
    /// See: https://www.codeproject.com/KB/buttons/hotkeycontrol.aspx
    /// But is modified to fit in Greenshot, and have localized support
    /// </summary>
    public sealed class HotkeyControl : GreenshotTextBox
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeyControl));

        private static readonly EventDelay EventDelay = new EventDelay(TimeSpan.FromMilliseconds(600).Ticks);
        private static readonly bool IsWindows7OrOlder = Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 1;

        // Holds the list of hotkeys
        private static readonly IDictionary<int, HotKeyHandler> KeyHandlers = new Dictionary<int, HotKeyHandler>();
        private static int _hotKeyCounter = 1;
        private const uint WM_HOTKEY = 0x312;
        private static IntPtr _hotkeyHwnd;

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum Modifiers : uint
        {
            NONE = 0,
            ALT = 1,
            CTRL = 2,
            SHIFT = 4,
            WIN = 8,
            NO_REPEAT = 0x4000
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum MapType : uint
        {
            MAPVK_VK_TO_VSC =
                0, //The uCode parameter is a virtual-key code and is translated into a scan code. If it is a virtual-key code that does not distinguish between left- and right-hand keys, the left-hand scan code is returned. If there is no translation, the function returns 0.

            MAPVK_VSC_TO_VK =
                1, //The uCode parameter is a scan code and is translated into a virtual-key code that does not distinguish between left- and right-hand keys. If there is no translation, the function returns 0.

            MAPVK_VK_TO_CHAR =
                2, //The uCode parameter is a virtual-key code and is translated into an unshifted character value in the low order word of the return value. Dead keys (diacritics) are indicated by setting the top bit of the return value. If there is no translation, the function returns 0.

            MAPVK_VSC_TO_VK_EX =
                3, //The uCode parameter is a scan code and is translated into a virtual-key code that distinguishes between left- and right-hand keys. If there is no translation, the function returns 0.

            MAPVK_VK_TO_VSC_EX =
                4 //The uCode parameter is a virtual-key code and is translated into a scan code. If it is a virtual-key code that does not distinguish between left- and right-hand keys, the left-hand scan code is returned. If the scan code is an extended scan code, the high byte of the uCode value can contain either 0xe0 or 0xe1 to specify the extended scan code. If there is no translation, the function returns 0.
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint virtualKeyCode);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetKeyNameText(uint lParam, [Out] StringBuilder lpString, int nSize);

        // These variables store the current hotkey and modifier(s)
        private Keys _hotkey = Keys.None;
        private Keys _modifiers = Keys.None;

        // ArrayLists used to enforce the use of proper modifiers.
        // Shift+A isn't a valid hotkey, for instance, as it would screw up when the user is typing.
        private readonly IList<int> _needNonShiftModifier = new List<int>();
        private readonly IList<int> _needNonAltGrModifier = new List<int>();

        private readonly ContextMenuStrip _dummy = new ContextMenuStrip();

        /// <summary>
        /// Used to make sure that there is no right-click menu available
        /// </summary>
        public override ContextMenuStrip ContextMenuStrip
        {
            get { return _dummy; }
            set { base.ContextMenuStrip = _dummy; }
        }

        /// <summary>
        /// Forces the control to be non-multiline
        /// </summary>
        public override bool Multiline
        {
            get { return base.Multiline; }
            set
            {
                // Ignore what the user wants; force Multiline to false
                base.Multiline = false;
            }
        }

        /// <summary>
        /// Creates a new HotkeyControl
        /// </summary>
        public HotkeyControl()
        {
            ContextMenuStrip = _dummy; // Disable right-clicking
            Text = "None";

            // Handle events that occurs when keys are pressed
            KeyPress += HotkeyControl_KeyPress;
            KeyUp += HotkeyControl_KeyUp;
            KeyDown += HotkeyControl_KeyDown;

            PopulateModifierLists();
        }

        /// <summary>
        /// Populates the ArrayLists specifying disallowed hotkeys
        /// such as Shift+A, Ctrl+Alt+4 (would produce a dollar sign) etc
        /// </summary>
        private void PopulateModifierLists()
        {
            // Shift + 0 - 9, A - Z
            for (Keys k = Keys.D0; k <= Keys.Z; k++)
            {
                _needNonShiftModifier.Add((int) k);
            }

            // Shift + Numpad keys
            for (Keys k = Keys.NumPad0; k <= Keys.NumPad9; k++)
            {
                _needNonShiftModifier.Add((int) k);
            }

            // Shift + Misc (,;<./ etc)
            for (Keys k = Keys.Oem1; k <= Keys.OemBackslash; k++)
            {
                _needNonShiftModifier.Add((int) k);
            }

            // Shift + Space, PgUp, PgDn, End, Home
            for (Keys k = Keys.Space; k <= Keys.Home; k++)
            {
                _needNonShiftModifier.Add((int) k);
            }

            // Misc keys that we can't loop through
            _needNonShiftModifier.Add((int) Keys.Insert);
            _needNonShiftModifier.Add((int) Keys.Help);
            _needNonShiftModifier.Add((int) Keys.Multiply);
            _needNonShiftModifier.Add((int) Keys.Add);
            _needNonShiftModifier.Add((int) Keys.Subtract);
            _needNonShiftModifier.Add((int) Keys.Divide);
            _needNonShiftModifier.Add((int) Keys.Decimal);
            _needNonShiftModifier.Add((int) Keys.Return);
            _needNonShiftModifier.Add((int) Keys.Escape);
            _needNonShiftModifier.Add((int) Keys.NumLock);

            // Ctrl+Alt + 0 - 9
            for (Keys k = Keys.D0; k <= Keys.D9; k++)
            {
                _needNonAltGrModifier.Add((int) k);
            }
        }

        /// <summary>
        /// Resets this hotkey control to None
        /// </summary>
        public new void Clear()
        {
            Hotkey = Keys.None;
            HotkeyModifiers = Keys.None;
        }

        /// <summary>
        /// Fires when a key is pushed down. Here, we'll want to update the text in the box
        /// to notify the user what combination is currently pressed.
        /// </summary>
        private void HotkeyControl_KeyDown(object sender, KeyEventArgs e)
        {
            // Clear the current hotkey
            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                ResetHotkey();
            }
            else
            {
                _modifiers = e.Modifiers;
                _hotkey = e.KeyCode;
                Redraw();
            }
        }

        /// <summary>
        /// Fires when all keys are released. If the current hotkey isn't valid, reset it.
        /// Otherwise, do nothing and keep the text and hotkey as it was.
        /// </summary>
        private void HotkeyControl_KeyUp(object sender, KeyEventArgs e)
        {
            // Somehow the PrintScreen only comes as a keyup, therefore we handle it here.
            if (e.KeyCode == Keys.PrintScreen)
            {
                _modifiers = e.Modifiers;
                _hotkey = e.KeyCode;
                Redraw();
            }

            if (_hotkey == Keys.None && ModifierKeys == Keys.None)
            {
                ResetHotkey();
            }
        }

        /// <summary>
        /// Prevents the letter/whatever entered to show up in the TextBox
        /// Without this, a "A" key press would appear as "aControl, Alt + A"
        /// </summary>
        private void HotkeyControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Handles some misc keys, such as Ctrl+Delete and Shift+Insert
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete || keyData == (Keys.Control | Keys.Delete))
            {
                ResetHotkey();
                return true;
            }

            // Paste
            if (keyData == (Keys.Shift | Keys.Insert))
            {
                return true; // Don't allow
            }

            // Allow the rest
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Clears the current hotkey and resets the TextBox
        /// </summary>
        public void ResetHotkey()
        {
            _hotkey = Keys.None;
            _modifiers = Keys.None;
            Redraw();
        }

        /// <summary>
        /// Used to get/set the hotkey (e.g. Keys.A)
        /// </summary>
        public Keys Hotkey
        {
            get { return _hotkey; }
            set
            {
                _hotkey = value;
                Redraw(true);
            }
        }

        /// <summary>
        /// Used to get/set the hotkey (e.g. Keys.A)
        /// </summary>
        public void SetHotkey(string hotkey)
        {
            _hotkey = HotkeyFromString(hotkey);
            _modifiers = HotkeyModifiersFromString(hotkey);
            Redraw(true);
        }

        /// <summary>
        /// Used to get/set the modifier keys (e.g. Keys.Alt | Keys.Control)
        /// </summary>
        public Keys HotkeyModifiers
        {
            get { return _modifiers; }
            set
            {
                _modifiers = value;
                Redraw(true);
            }
        }

        /// <summary>
        /// Redraws the TextBox when necessary.
        /// </summary>
        /// <param name="bCalledProgramatically">Specifies whether this function was called by the Hotkey/HotkeyModifiers properties or by the user.</param>
        private void Redraw(bool bCalledProgramatically = false)
        {
            // No hotkey set
            if (_hotkey == Keys.None)
            {
                Text = string.Empty;
                return;
            }

            // LWin/RWin doesn't work as hotkeys (neither do they work as modifier keys in .NET 2.0)
            if (_hotkey == Keys.LWin || _hotkey == Keys.RWin)
            {
                Text = string.Empty;
                return;
            }

            // Only validate input if it comes from the user
            if (bCalledProgramatically == false)
            {
                // No modifier or shift only, AND a hotkey that needs another modifier
                if ((_modifiers == Keys.Shift || _modifiers == Keys.None) && _needNonShiftModifier.Contains((int) _hotkey))
                {
                    if (_modifiers == Keys.None)
                    {
                        // Set Ctrl+Alt as the modifier unless Ctrl+Alt+<key> won't work...
                        if (_needNonAltGrModifier.Contains((int) _hotkey) == false)
                        {
                            _modifiers = Keys.Alt | Keys.Control;
                        }
                        else
                        {
                            // ... in that case, use Shift+Alt instead.
                            _modifiers = Keys.Alt | Keys.Shift;
                        }
                    }
                    else
                    {
                        // User pressed Shift and an invalid key (e.g. a letter or a number),
                        // that needs another set of modifier keys
                        _hotkey = Keys.None;
                        Text = string.Empty;
                        return;
                    }
                }

                // Check all Ctrl+Alt keys
                if ((_modifiers == (Keys.Alt | Keys.Control)) && _needNonAltGrModifier.Contains((int) _hotkey))
                {
                    // Ctrl+Alt+4 etc won't work; reset hotkey and tell the user
                    _hotkey = Keys.None;
                    Text = string.Empty;
                    return;
                }
            }

            // I have no idea why this is needed, but it is. Without this code, pressing only Ctrl
            // will show up as "Control + ControlKey", etc.
            if (_hotkey == Keys.Menu /* Alt */ || _hotkey == Keys.ShiftKey || _hotkey == Keys.ControlKey)
            {
                _hotkey = Keys.None;
            }

            Text = HotkeyToLocalizedString(_modifiers, _hotkey);
        }

        public override string ToString()
        {
            return HotkeyToString(HotkeyModifiers, Hotkey);
        }

        public static string GetLocalizedHotkeyStringFromString(string hotkeyString)
        {
            Keys virtualKeyCode = HotkeyFromString(hotkeyString);
            Keys modifiers = HotkeyModifiersFromString(hotkeyString);
            return HotkeyToLocalizedString(modifiers, virtualKeyCode);
        }

        public static string HotkeyToString(Keys modifierKeyCode, Keys virtualKeyCode)
        {
            return HotkeyModifiersToString(modifierKeyCode) + virtualKeyCode;
        }

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

            if (modifierKeyCode == Keys.LWin || modifierKeyCode == Keys.RWin)
            {
                hotkeyString.Append("Win").Append(" + ");
            }

            return hotkeyString.ToString();
        }


        public static string HotkeyToLocalizedString(Keys modifierKeyCode, Keys virtualKeyCode)
        {
            return HotkeyModifiersToLocalizedString(modifierKeyCode) + GetKeyName(virtualKeyCode);
        }

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

            if (modifierKeyCode == Keys.LWin || modifierKeyCode == Keys.RWin)
            {
                hotkeyString.Append("Win").Append(" + ");
            }

            return hotkeyString.ToString();
        }


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

        public static Keys HotkeyFromString(string hotkey)
        {
            Keys key = Keys.None;
            if (!string.IsNullOrEmpty(hotkey))
            {
                if (hotkey.LastIndexOf('+') > 0)
                {
                    hotkey = hotkey.Remove(0, hotkey.LastIndexOf('+') + 1).Trim();
                }

                key = (Keys) Enum.Parse(typeof(Keys), hotkey);
            }

            return key;
        }

        public static void RegisterHotkeyHwnd(IntPtr hWnd)
        {
            _hotkeyHwnd = hWnd;
        }

        /// <summary>
        /// Register a hotkey
        /// </summary>
        /// <param name="modifierKeyCode">The modifier, e.g.: Modifiers.CTRL, Modifiers.NONE or Modifiers.ALT</param>
        /// <param name="virtualKeyCode">The virtual key code</param>
        /// <param name="handler">A HotKeyHandler, this will be called to handle the hotkey press</param>
        /// <returns>the hotkey number, -1 if failed</returns>
        public static int RegisterHotKey(Keys modifierKeyCode, Keys virtualKeyCode, HotKeyHandler handler)
        {
            if (virtualKeyCode == Keys.None)
            {
                Log.Warn("Trying to register a Keys.none hotkey, ignoring");
                return 0;
            }

            // Convert Modifiers to fit HKM_SETHOTKEY
            uint modifiers = 0;
            if ((modifierKeyCode & Keys.Alt) > 0)
            {
                modifiers |= (uint) Modifiers.ALT;
            }

            if ((modifierKeyCode & Keys.Control) > 0)
            {
                modifiers |= (uint) Modifiers.CTRL;
            }

            if ((modifierKeyCode & Keys.Shift) > 0)
            {
                modifiers |= (uint) Modifiers.SHIFT;
            }

            if (modifierKeyCode == Keys.LWin || modifierKeyCode == Keys.RWin)
            {
                modifiers |= (uint) Modifiers.WIN;
            }

            // Disable repeating hotkey for Windows 7 and beyond, as described in #1559
            if (IsWindows7OrOlder)
            {
                modifiers |= (uint) Modifiers.NO_REPEAT;
            }

            if (RegisterHotKey(_hotkeyHwnd, _hotKeyCounter, modifiers, (uint) virtualKeyCode))
            {
                KeyHandlers.Add(_hotKeyCounter, handler);
                return _hotKeyCounter++;
            }

            Log.Warn($"Couldn't register hotkey modifier {modifierKeyCode} virtualKeyCode {virtualKeyCode}");
            return -1;
        }

        public static void UnregisterHotkeys()
        {
            foreach (int hotkey in KeyHandlers.Keys)
            {
                UnregisterHotKey(_hotkeyHwnd, hotkey);
            }

            // Remove all key handlers
            KeyHandlers.Clear();
        }

        /// <summary>
        /// Handle WndProc messages for the hotkey
        /// </summary>
        /// <param name="m"></param>
        /// <returns>true if the message was handled</returns>
        public static bool HandleMessages(ref Message m)
        {
            if (m.Msg != WM_HOTKEY)
            {
                return false;
            }

            // Call handler
            if (!IsWindows7OrOlder && !EventDelay.Check())
            {
                return true;
            }

            if (KeyHandlers.TryGetValue((int) m.WParam, out var handler))
            {
                handler();
            }

            return true;
        }

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

            uint scanCode = MapVirtualKey((uint) virtualKey, (uint) MapType.MAPVK_VK_TO_VSC);

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
}