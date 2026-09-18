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
using System.Linq;
using System.Text;
using Dapplo.Windows.Input.Enums;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Represents a complete hotkey shortcut consisting of one or more key chords.
    /// </summary>
    public sealed class HotkeySequence : IEquatable<HotkeySequence>
    {
        public List<KeyChord> Chords { get; } = new List<KeyChord>();

        public bool IsEmpty => Chords.Count == 0 || Chords.All(c => c.Key == VirtualKeyCode.None);

        public HotkeySequence()
        {
        }

        public HotkeySequence(IEnumerable<KeyChord> chords)
        {
            if (chords != null)
            {
                Chords.AddRange(chords);
            }
        }

        public HotkeySequence(params KeyChord[] chords)
        {
            if (chords != null)
            {
                Chords.AddRange(chords);
            }
        }

        public static HotkeySequence FromChord(KeyChord chord)
        {
            var seq = new HotkeySequence();
            if (chord != null)
            {
                seq.Chords.Add(chord);
            }
            return seq;
        }

        public static HotkeySequence Parse(string sequenceString)
        {
            var sequence = new HotkeySequence();
            if (string.IsNullOrWhiteSpace(sequenceString) || string.Equals(sequenceString.Trim(), "None", StringComparison.OrdinalIgnoreCase))
            {
                return sequence;
            }

            // Chords are separated by comma (or semicolon or ' then ')
            string normalized = sequenceString.Replace(" then ", ",");
            string[] chordParts = normalized.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in chordParts)
            {
                var chord = KeyChord.Parse(part);
                if (chord.Key != VirtualKeyCode.None || chord.HasModifiers)
                {
                    sequence.Chords.Add(chord);
                }
            }

            return sequence;
        }

        /// <summary>
        /// Validates this hotkey sequence against the Greenshot hotkey rules.
        /// </summary>
        /// <param name="errorMessage">Output error message if invalid</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool Validate(out string errorMessage)
        {
            if (IsEmpty)
            {
                errorMessage = "No hotkey set.";
                return false;
            }

            for (int i = 0; i < Chords.Count; i++)
            {
                var chord = Chords[i];
                string chordLabel = Chords.Count > 1 ? $"Chord {i + 1}: " : "";

                if (chord.Key == VirtualKeyCode.None)
                {
                    errorMessage = $"{chordLabel}Please choose a trigger key.";
                    return false;
                }

                // Disallowed reserved keys
                if (chord.Key == VirtualKeyCode.Escape ||
                    chord.Key == VirtualKeyCode.Return ||
                    chord.Key == VirtualKeyCode.Tab ||
                    chord.Key == VirtualKeyCode.Capital)
                {
                    errorMessage = $"{chordLabel}{KeyChord.FormatKeyName(chord.Key)} is a reserved system key and cannot be used as a trigger.";
                    return false;
                }

                // Allowed standalone keys without modifiers: Function keys F1-F24, Snapshot, Scroll, Pause
                bool isAllowedStandalone = IsFunctionKey(chord.Key) ||
                                           chord.Key == VirtualKeyCode.Snapshot ||
                                           chord.Key == VirtualKeyCode.Scroll ||
                                           chord.Key == VirtualKeyCode.Pause;

                if (!chord.HasModifiers && !isAllowedStandalone)
                {
                    errorMessage = $"{chordLabel}Common keys require at least one modifier key (Ctrl, Alt, Shift, or Win).";
                    return false;
                }

                // Disallow Shift as sole modifier for alphanumeric keys
                if (chord.Shift && !chord.Ctrl && !chord.Alt && !chord.Win && IsAlphanumeric(chord.Key))
                {
                    errorMessage = $"{chordLabel}Shift cannot be the only modifier for letters or numbers.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        public static bool IsFunctionKey(VirtualKeyCode key)
        {
            int code = (int)key;
            return (code >= (int)VirtualKeyCode.F1 && code <= (int)VirtualKeyCode.F24);
        }

        public static bool IsAlphanumeric(VirtualKeyCode key)
        {
            int code = (int)key;
            bool isDigit = (code >= (int)VirtualKeyCode.Key0 && code <= (int)VirtualKeyCode.Key9);
            bool isAlpha = (code >= (int)VirtualKeyCode.KeyA && code <= (int)VirtualKeyCode.KeyZ);
            bool isNumpad = (code >= (int)VirtualKeyCode.Numpad0 && code <= (int)VirtualKeyCode.Numpad9);
            return isDigit || isAlpha || isNumpad;
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "None";
            }

            return string.Join(", ", Chords.Select(c => c.ToString()));
        }

        public bool Equals(HotkeySequence other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Chords.Count != other.Chords.Count) return false;
            for (int i = 0; i < Chords.Count; i++)
            {
                if (!Chords[i].Equals(other.Chords[i])) return false;
            }
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as HotkeySequence);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var chord in Chords)
                {
                    hash = (hash * 397) ^ (chord != null ? chord.GetHashCode() : 0);
                }
                return hash;
            }
        }
    }
}
