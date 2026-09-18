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
using System.Collections.Generic;
using System.Text;
using Dapplo.Windows.Input.Enums;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Represents a single key combination (modifiers + side preferences + trigger key).
    /// </summary>
    public sealed class KeyChord : IEquatable<KeyChord>
    {
        public bool Ctrl { get; set; }
        public ModifierLocation CtrlLocation { get; set; } = ModifierLocation.Any;

        public bool Alt { get; set; }
        public ModifierLocation AltLocation { get; set; } = ModifierLocation.Any;

        public bool Shift { get; set; }
        public ModifierLocation ShiftLocation { get; set; } = ModifierLocation.Any;

        public bool Win { get; set; }
        public ModifierLocation WinLocation { get; set; } = ModifierLocation.Any;

        public VirtualKeyCode Key { get; set; } = VirtualKeyCode.None;

        public bool HasModifiers => Ctrl || Alt || Shift || Win;

        public KeyChord()
        {
        }

        public KeyChord(VirtualKeyCode key, bool ctrl = false, bool alt = false, bool shift = false, bool win = false)
        {
            Key = key;
            Ctrl = ctrl;
            Alt = alt;
            Shift = shift;
            Win = win;
        }

        public static KeyChord Parse(string chordString)
        {
            if (string.IsNullOrWhiteSpace(chordString))
            {
                return new KeyChord();
            }

            var chord = new KeyChord();
            string[] parts = chordString.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                string rawPart = parts[i].Trim();
                if (string.IsNullOrEmpty(rawPart))
                {
                    continue;
                }

                string lower = rawPart.ToLowerInvariant();
                if (lower.StartsWith("ctrl") || lower.StartsWith("control"))
                {
                    chord.Ctrl = true;
                    chord.CtrlLocation = ParseLocation(lower);
                }
                else if (lower.StartsWith("alt"))
                {
                    chord.Alt = true;
                    chord.AltLocation = ParseLocation(lower);
                }
                else if (lower.StartsWith("shift"))
                {
                    chord.Shift = true;
                    chord.ShiftLocation = ParseLocation(lower);
                }
                else if (lower.StartsWith("win"))
                {
                    chord.Win = true;
                    chord.WinLocation = ParseLocation(lower);
                }
                else
                {
                    chord.Key = ParseVirtualKeyCode(rawPart);
                }
            }

            return chord;
        }

        private static ModifierLocation ParseLocation(string modifierStr)
        {
            if (modifierStr.Contains("(l)") || modifierStr.EndsWith("left") || modifierStr.StartsWith("l"))
            {
                if (modifierStr.Contains("(l)") || modifierStr.Contains("left"))
                {
                    return ModifierLocation.Left;
                }
            }
            if (modifierStr.Contains("(r)") || modifierStr.EndsWith("right") || modifierStr.StartsWith("r"))
            {
                if (modifierStr.Contains("(r)") || modifierStr.Contains("right"))
                {
                    return ModifierLocation.Right;
                }
            }
            return ModifierLocation.Any;
        }

        public static VirtualKeyCode ParseVirtualKeyCode(string keyStr)
        {
            if (string.IsNullOrWhiteSpace(keyStr))
            {
                return VirtualKeyCode.None;
            }

            string clean = keyStr.Trim();
            // Handle common aliases
            string lower = clean.ToLowerInvariant();
            switch (lower)
            {
                case "printscreen":
                case "prnt":
                case "print":
                case "prtscr":
                case "snapshot":
                    return VirtualKeyCode.Snapshot;
                case "scroll":
                case "scrolllock":
                    return VirtualKeyCode.Scroll;
                case "pause":
                case "break":
                    return VirtualKeyCode.Pause;
                case "esc":
                case "escape":
                    return VirtualKeyCode.Escape;
                case "enter":
                case "return":
                    return VirtualKeyCode.Return;
                case "tab":
                    return VirtualKeyCode.Tab;
                case "space":
                case "spacebar":
                    return VirtualKeyCode.Space;
                case "back":
                case "backspace":
                    return VirtualKeyCode.Back;
                case "del":
                case "delete":
                    return VirtualKeyCode.Delete;
                case "ins":
                case "insert":
                    return VirtualKeyCode.Insert;
                case "caps":
                case "capslock":
                    return VirtualKeyCode.Capital;
            }

            // Single alphanumeric character
            if (clean.Length == 1)
            {
                char c = char.ToUpperInvariant(clean[0]);
                if (c >= 'A' && c <= 'Z')
                {
                    if (Enum.TryParse<VirtualKeyCode>("Key" + c, true, out var alphaKey))
                    {
                        return alphaKey;
                    }
                }
                if (c >= '0' && c <= '9')
                {
                    if (Enum.TryParse<VirtualKeyCode>("Key" + c, true, out var numKey))
                    {
                        return numKey;
                    }
                }
            }

            // Try direct Enum parse
            if (Enum.TryParse<VirtualKeyCode>(clean, true, out var directKey))
            {
                return directKey;
            }

            // Try with "Key" prefix if digit or letter
            if (Enum.TryParse<VirtualKeyCode>("Key" + clean, true, out var prefixedKey))
            {
                return prefixedKey;
            }

            return VirtualKeyCode.None;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (Ctrl)
            {
                sb.Append("Ctrl");
                if (CtrlLocation == ModifierLocation.Left) sb.Append("(L)");
                else if (CtrlLocation == ModifierLocation.Right) sb.Append("(R)");
                sb.Append(" + ");
            }

            if (Alt)
            {
                sb.Append("Alt");
                if (AltLocation == ModifierLocation.Left) sb.Append("(L)");
                else if (AltLocation == ModifierLocation.Right) sb.Append("(R)");
                sb.Append(" + ");
            }

            if (Shift)
            {
                sb.Append("Shift");
                if (ShiftLocation == ModifierLocation.Left) sb.Append("(L)");
                else if (ShiftLocation == ModifierLocation.Right) sb.Append("(R)");
                sb.Append(" + ");
            }

            if (Win)
            {
                sb.Append("Win");
                if (WinLocation == ModifierLocation.Left) sb.Append("(L)");
                else if (WinLocation == ModifierLocation.Right) sb.Append("(R)");
                sb.Append(" + ");
            }

            if (Key != VirtualKeyCode.None)
            {
                sb.Append(FormatKeyName(Key));
            }
            else if (sb.Length >= 3)
            {
                // Remove trailing " + "
                sb.Length -= 3;
            }

            return sb.ToString();
        }

        public static string FormatKeyName(VirtualKeyCode key)
        {
            switch (key)
            {
                case VirtualKeyCode.Snapshot:
                    return "PrintScreen";
                case VirtualKeyCode.Scroll:
                    return "ScrollLock";
                case VirtualKeyCode.Pause:
                    return "Pause";
                case VirtualKeyCode.Return:
                    return "Enter";
                case VirtualKeyCode.Back:
                    return "Backspace";
                case VirtualKeyCode.Escape:
                    return "Esc";
                case VirtualKeyCode.Capital:
                    return "CapsLock";
                default:
                    string name = key.ToString();
                    if (name.StartsWith("Key") && name.Length == 4)
                    {
                        return name.Substring(3);
                    }
                    return name;
            }
        }

        public IReadOnlyList<string> GetBadges()
        {
            var list = new List<string>();
            if (Ctrl)
            {
                string s = "Ctrl";
                if (CtrlLocation == ModifierLocation.Left) s += " (L)";
                else if (CtrlLocation == ModifierLocation.Right) s += " (R)";
                list.Add(s);
            }
            if (Alt)
            {
                string s = "Alt";
                if (AltLocation == ModifierLocation.Left) s += " (L)";
                else if (AltLocation == ModifierLocation.Right) s += " (R)";
                list.Add(s);
            }
            if (Shift)
            {
                string s = "Shift";
                if (ShiftLocation == ModifierLocation.Left) s += " (L)";
                else if (ShiftLocation == ModifierLocation.Right) s += " (R)";
                list.Add(s);
            }
            if (Win)
            {
                string s = "Win";
                if (WinLocation == ModifierLocation.Left) s += " (L)";
                else if (WinLocation == ModifierLocation.Right) s += " (R)";
                list.Add(s);
            }
            if (Key != VirtualKeyCode.None)
            {
                list.Add(FormatKeyName(Key));
            }
            return list;
        }

        public bool Equals(KeyChord other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Ctrl == other.Ctrl && CtrlLocation == other.CtrlLocation &&
                   Alt == other.Alt && AltLocation == other.AltLocation &&
                   Shift == other.Shift && ShiftLocation == other.ShiftLocation &&
                   Win == other.Win && WinLocation == other.WinLocation &&
                   Key == other.Key;
        }

        public override bool Equals(object obj) => Equals(obj as KeyChord);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Key;
                hash = (hash * 397) ^ Ctrl.GetHashCode();
                hash = (hash * 397) ^ (int)CtrlLocation;
                hash = (hash * 397) ^ Alt.GetHashCode();
                hash = (hash * 397) ^ (int)AltLocation;
                hash = (hash * 397) ^ Shift.GetHashCode();
                hash = (hash * 397) ^ (int)ShiftLocation;
                hash = (hash * 397) ^ Win.GetHashCode();
                hash = (hash * 397) ^ (int)WinLocation;
                return hash;
            }
        }
    }
}
