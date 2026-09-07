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
using System.Text;
using System.Windows.Forms;
using Greenshot.Base.Core;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// A simple control that allows the user to select pretty much any valid hotkey combination
    /// See: https://www.codeproject.com/KB/buttons/hotkeycontrol.aspx
    /// But is modified to fit in Greenshot, and have localized support
    /// </summary>
    public sealed class HotkeyControl : GreenshotTextBox
    {
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
            _hotkey = HotkeyManager.HotkeyFromString(hotkey);
            _modifiers = HotkeyManager.HotkeyModifiersFromString(hotkey);
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

            Text = HotkeyManager.HotkeyToLocalizedString(_modifiers, _hotkey);
        }

        public override string ToString()
        {
            return HotkeyManager.HotkeyToString(HotkeyModifiers, Hotkey);
        }
    }
}
