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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using log4net;

namespace Greenshot.Base.Core;

/// <summary>
/// HotkeyManager handles hotkey registration and execution.
/// Supports single chords, multi-chord sequences, modifier sides (Left/Right/Any), and dynamic registration.
/// </summary>
public static class HotkeyManager
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeyManager));

    private static readonly List<HotkeyInfo> RegisteredHotkeys = new List<HotkeyInfo>();
    private static IDisposable _keyboardSubscription;
    private static int _hotKeyCounter = 1;

    /// <summary>
    /// When true, hotkey handling is paused (e.g. while an in-window modal editor is recording keys).
    /// </summary>
    public static bool IsPaused { get; set; }

    // Multi-chord sequence tracking
    private static List<HotkeyInfo> _candidateSequences;
    private static int _activeChordIndex;
    private static DateTime _lastChordTime;
    private static readonly TimeSpan ChordTimeout = TimeSpan.FromSeconds(5.0);

    internal static int CandidateSequenceCount => _candidateSequences?.Count ?? 0;
    internal static int ActiveChordIndex => _activeChordIndex;

    private class HotkeyInfo
    {
        public HotkeySequence Sequence { get; set; }
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

    private static bool IsModifierKey(VirtualKeyCode key)
    {
        switch (key)
        {
            case VirtualKeyCode.LeftShift:
            case VirtualKeyCode.RightShift:
            case VirtualKeyCode.Shift:
            case VirtualKeyCode.LeftControl:
            case VirtualKeyCode.RightControl:
            case VirtualKeyCode.Control:
            case VirtualKeyCode.LeftMenu:
            case VirtualKeyCode.RightMenu:
            case VirtualKeyCode.Menu:
            case VirtualKeyCode.LeftWin:
            case VirtualKeyCode.RightWin:
                return true;
            default:
                return false;
        }
    }

    internal static void HandleKeyboardEvent(KeyboardHookEventArgs e)
    {
        if (IsPaused)
        {
            ResetChordState();
            return;
        }

        if (!e.IsKeyDown)
        {
            return;
        }

        // Ignore pure modifier keys (Ctrl, Alt, Shift, Win) when pressed alone without a trigger key.
        // We do not rely on e.IsModifier because Dapplo also classifies toggle/lock keys (ScrollLock, CapsLock, NumLock) as modifiers.
        if (IsModifierKey(e.Key))
        {
            return;
        }

        Log.DebugFormat("HotkeyManager: Processing keydown Key={0}, Injected={1}, Ctrl={2}, Alt={3}, Shift={4}, Win={5}, CandidateCount={6}",
            e.Key, e.IsInjectedByProcess, e.IsControl, e.IsAlt, e.IsShift, e.IsWindows, _candidateSequences?.Count ?? 0);

        // Timeout check for multi-chord sequences
        if (_candidateSequences != null && (DateTime.UtcNow - _lastChordTime) > ChordTimeout)
        {
            Log.DebugFormat("HotkeyManager: Sequence timed out after {0}s. Resetting candidate sequences.", ChordTimeout.TotalSeconds);
            ResetChordState();
        }

        // Allow Escape to cancel any pending multi-chord sequence
        if (_candidateSequences != null && e.Key == VirtualKeyCode.Escape)
        {
            Log.Info("HotkeyManager: Multi-chord sequence cancelled by Escape key.");
            ResetChordState();
            e.Handled = true;
            return;
        }

        List<HotkeyInfo> hotkeys;
        lock (RegisteredHotkeys)
        {
            hotkeys = RegisteredHotkeys.ToList();
        }

        // 1. If currently in the middle of a multi-chord sequence, check if this key matches the next chord of any candidate
        if (_candidateSequences != null)
        {
            var nextCandidates = _candidateSequences
                .Where(c => _activeChordIndex < c.Sequence.Chords.Count && MatchChord(e, c.Sequence.Chords[_activeChordIndex], c.Sequence.Chords[_activeChordIndex - 1]))
                .ToList();

            if (nextCandidates.Count > 0)
            {
                _candidateSequences = nextCandidates;
                _activeChordIndex++;
                _lastChordTime = DateTime.UtcNow;

                // Check if any candidate has fully completed
                var completed = _candidateSequences.FirstOrDefault(c => _activeChordIndex >= c.Sequence.Chords.Count);
                if (completed != null)
                {
                    // Full sequence matched! Mark handled on the final chord and trigger handler
                    e.Handled = true;
                    var handler = completed.Handler;
                    Log.InfoFormat("HotkeyManager: Completed multi-chord sequence '{0}'. Triggering action.", completed.Sequence);
                    ResetChordState();
                    handler();
                }
                else
                {
                    // Intermediate chord matched; consume the key event
                    e.Handled = true;
                    Log.InfoFormat("HotkeyManager: Chord step {0} matched for sequence '{1}'. Waiting for next chord...", _activeChordIndex, nextCandidates[0].Sequence);
                }
                return;
            }
            else
            {
                // Key didn't match any active candidate sequence; reset chord state and fall through
                // to check if this key starts a new sequence
                Log.DebugFormat("HotkeyManager: Key '{0}' did not match expected chord in candidate sequences. Resetting sequence.", e.Key);
                ResetChordState();
            }
        }

        // 2. Check registered hotkeys for starting chord matches
        // First, check if any single-chord hotkey matches exactly
        var singleMatch = hotkeys.FirstOrDefault(h => h.Sequence?.Chords.Count == 1 && MatchChord(e, h.Sequence.Chords[0]));
        if (singleMatch != null)
        {
            e.Handled = true;
            Log.InfoFormat("HotkeyManager: Single-chord hotkey '{0}' matched. Triggering action.", singleMatch.Sequence);
            singleMatch.Handler();
            return;
        }

        // Next, check for multi-chord sequences whose first chord matches
        var matchingMulti = hotkeys
            .Where(h => h.Sequence?.Chords.Count > 1 && MatchChord(e, h.Sequence.Chords[0]))
            .ToList();

        if (matchingMulti.Count > 0)
        {
            _candidateSequences = matchingMulti;
            _activeChordIndex = 1;
            _lastChordTime = DateTime.UtcNow;
            e.Handled = true;
            Log.InfoFormat("HotkeyManager: First chord matched for '{0}'. Waiting for next chord (timeout {1}s)...", matchingMulti[0].Sequence, ChordTimeout.TotalSeconds);
            return;
        }
    }

    private static void ResetChordState()
    {
        _candidateSequences = null;
        _activeChordIndex = 0;
    }

    private static bool MatchChord(KeyboardHookEventArgs e, KeyChord chord, KeyChord previousChord = null)
    {
        if (chord == null) return false;

        // Compare virtual key code
        if (e.Key != chord.Key)
        {
            return false;
        }

        // Check Ctrl and modifier side
        if (chord.Ctrl)
        {
            if (!e.IsControl) return false;
            if (chord.CtrlLocation == ModifierLocation.Left && !e.IsLeftControl) return false;
            if (chord.CtrlLocation == ModifierLocation.Right && !e.IsRightControl) return false;
        }
        else if (e.IsControl && (previousChord == null || !previousChord.Ctrl))
        {
            return false;
        }

        // Check Alt and modifier side
        if (chord.Alt)
        {
            if (!e.IsAlt) return false;
            if (chord.AltLocation == ModifierLocation.Left && !e.IsLeftAlt) return false;
            if (chord.AltLocation == ModifierLocation.Right && !e.IsRightAlt) return false;
        }
        else if (e.IsAlt && (previousChord == null || !previousChord.Alt))
        {
            return false;
        }

        // Check Shift and modifier side
        if (chord.Shift)
        {
            if (!e.IsShift) return false;
            if (chord.ShiftLocation == ModifierLocation.Left && !e.IsLeftShift) return false;
            if (chord.ShiftLocation == ModifierLocation.Right && !e.IsRightShift) return false;
        }
        else if (e.IsShift && (previousChord == null || !previousChord.Shift))
        {
            return false;
        }

        // Check Windows and modifier side
        if (chord.Win)
        {
            if (!e.IsWindows) return false;
            if (chord.WinLocation == ModifierLocation.Left && !e.IsLeftWindows) return false;
            if (chord.WinLocation == ModifierLocation.Right && !e.IsRightWindows) return false;
        }
        else if (e.IsWindows && (previousChord == null || !previousChord.Win))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Register a hotkey sequence with an action.
    /// Does not re-create the underlying KeyboardHook subscription if already active.
    /// </summary>
    public static int RegisterHotKey(HotkeySequence sequence, Action handler)
    {
        if (sequence == null || sequence.IsEmpty)
        {
            Log.Warn("Trying to register an empty hotkey sequence, ignoring");
            return 0;
        }

        _keyboardSubscription ??= KeyboardHook.KeyboardEvents.Subscribe(HandleKeyboardEvent);

        lock (RegisteredHotkeys)
        {
            var existing = RegisteredHotkeys.FirstOrDefault(x => x.Sequence.Equals(sequence));
            if (existing != null)
            {
                Log.WarnFormat("Hotkey {0} already registered (ID {1}). Replacing previous handler to prevent duplicate triggers.", sequence, existing.Id);
                existing.Handler = handler;
                return existing.Id;
            }

            var hotkeyInfo = new HotkeyInfo
            {
                Sequence = sequence,
                Handler = handler,
                Id = _hotKeyCounter++
            };
            RegisteredHotkeys.Add(hotkeyInfo);
            return hotkeyInfo.Id;
        }
    }

    /// <summary>
    /// Register a hotkey string (supports chords, side specifications, or legacy strings) with an action.
    /// </summary>
    public static int RegisterHotKey(string hotkeyString, Action handler)
    {
        var sequence = HotkeySequence.Parse(hotkeyString);
        return RegisterHotKey(sequence, handler);
    }

    /// <summary>
    /// Register a hotkey with an action (legacy overload for Keys modifier and Keys virtualKey).
    /// </summary>
    public static int RegisterHotKey(Keys modifierKeyCode, Keys virtualKeyCode, Action handler)
    {
        if (virtualKeyCode == Keys.None)
        {
            Log.Warn("Trying to register a Keys.none hotkey, ignoring");
            return 0;
        }

        var chord = new KeyChord((VirtualKeyCode)(int)virtualKeyCode)
        {
            Ctrl = (modifierKeyCode & Keys.Control) != 0,
            Alt = (modifierKeyCode & Keys.Alt) != 0,
            Shift = (modifierKeyCode & Keys.Shift) != 0,
            Win = (modifierKeyCode & Keys.LWin) != 0 || (modifierKeyCode & Keys.RWin) != 0
        };

        var sequence = HotkeySequence.FromChord(chord);
        return RegisterHotKey(sequence, handler);
    }

    /// <summary>
    /// Unregisters all currently registered global hotkeys and releases associated resources.
    /// </summary>
    public static void UnregisterHotkeys()
    {
        lock (RegisteredHotkeys)
        {
            _keyboardSubscription?.Dispose();
            _keyboardSubscription = null;
            RegisteredHotkeys.Clear();
            ResetChordState();
        }
    }

    /// <summary>
    /// Clears registered hotkeys without disposing the underlying keyboard subscription.
    /// </summary>
    public static void ClearRegisteredHotkeys()
    {
        lock (RegisteredHotkeys)
        {
            RegisteredHotkeys.Clear();
            ResetChordState();
        }
    }

    /// <summary>
    /// Unregisters a specific hotkey by its registration ID.
    /// </summary>
    public static bool UnregisterHotKey(int id)
    {
        lock (RegisteredHotkeys)
        {
            int index = RegisteredHotkeys.FindIndex(h => h.Id == id);
            if (index >= 0)
            {
                RegisteredHotkeys.RemoveAt(index);
                if (RegisteredHotkeys.Count == 0)
                {
                    _keyboardSubscription?.Dispose();
                    _keyboardSubscription = null;
                }
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts a hotkey string to its localized display representation.
    /// </summary>
    public static string GetLocalizedHotkeyStringFromString(string hotkeyString)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString) || string.Equals(hotkeyString.Trim(), "None", StringComparison.OrdinalIgnoreCase))
        {
            return "None";
        }

        var sequence = HotkeySequence.Parse(hotkeyString);
        if (sequence.IsEmpty)
        {
            return "None";
        }

        return sequence.ToString();
    }

    /// <summary>
    /// Converts the specified modifier and virtual key codes to a human-readable hotkey string representation.
    /// </summary>
    public static string HotkeyToString(Keys modifierKeyCode, Keys virtualKeyCode)
    {
        return HotkeyModifiersToString(modifierKeyCode) + virtualKeyCode;
    }

    /// <summary>
    /// Converts the specified modifier key code to a human-readable string representing the combination of modifier keys.
    /// </summary>
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
    public static string HotkeyToLocalizedString(Keys modifierKeyCode, Keys virtualKeyCode)
    {
        return HotkeyModifiersToLocalizedString(modifierKeyCode) + GetKeyName(virtualKeyCode);
    }

    /// <summary>
    /// Converts a set of hotkey modifier keys to a localized string representation suitable for display.
    /// </summary>
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
    public static Keys HotkeyFromString(string hotkey)
    {
        Keys key = Keys.None;
        if (!string.IsNullOrEmpty(hotkey))
        {
            // If it's a sequence, take the first chord's trigger key
            if (hotkey.Contains(","))
            {
                hotkey = hotkey.Split(',')[0];
            }

            if (hotkey.LastIndexOf('+') > 0)
            {
                hotkey = hotkey.Remove(0, hotkey.LastIndexOf('+') + 1).Trim();
            }

            // Clean any trailing or leading whitespace
            hotkey = hotkey.Trim();

            // Handle common name mapping
            if (string.Equals(hotkey, "Snapshot", StringComparison.OrdinalIgnoreCase))
            {
                return Keys.PrintScreen;
            }

            if (Enum.TryParse<Keys>(hotkey, true, out var parsed))
            {
                return parsed;
            }
        }

        return key;
    }

    /// <summary>
    /// Returns a user-friendly display name for the specified keyboard key.
    /// </summary>
    public static string GetKeyName(Keys givenKey)
    {
        StringBuilder keyName = new StringBuilder();
        const uint numpad = 55;

        Keys virtualKey = givenKey;
        string keyString;
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

        switch (virtualKey)
        {
            case Keys.Left:
            case Keys.Up:
            case Keys.Right:
            case Keys.Down:
            case Keys.Prior:
            case Keys.Next:
            case Keys.End:
            case Keys.Home:
            case Keys.Insert:
            case Keys.Delete:
            case Keys.NumLock:
                scanCode |= 0x100;
                break;
            case Keys.PrintScreen:
                scanCode = 311;
                break;
            case Keys.Pause:
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
