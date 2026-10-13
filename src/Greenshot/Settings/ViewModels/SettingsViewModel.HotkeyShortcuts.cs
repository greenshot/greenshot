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
using System.Collections.ObjectModel;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Helpers;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The "Shortcuts" settings tab: freely configurable hotkeys, each with a capture function and one or more
    /// destinations. Reuses HotkeyShortcutManager for the available capture functions/destinations and for
    /// validation; persistence happens through HotkeyShortcutManager.SaveShortcuts on save (OK), exactly like every
    /// other setting - Cancel simply discards this ViewModel without ever calling it.
    /// </summary>
    public partial class SettingsViewModel
    {
        public ObservableCollection<HotkeyShortcutItemViewModel> HotkeyShortcuts { get; private set; }

        public ObservableCollection<CaptureFunctionOption> AvailableCaptureFunctions { get; private set; }

        public ObservableCollection<DestinationOption> AvailableDestinations { get; private set; }

        private void InitializeHotkeyShortcuts()
        {
            AvailableCaptureFunctions = new ObservableCollection<CaptureFunctionOption>(HotkeyShortcutManager.GetAvailableCaptureFunctions());
            AvailableDestinations = new ObservableCollection<DestinationOption>(HotkeyShortcutManager.GetAvailableDestinations());

            HotkeyShortcuts = new ObservableCollection<HotkeyShortcutItemViewModel>();
            foreach (var config in HotkeyShortcutManager.GetShortcuts())
            {
                HotkeyShortcuts.Add(HotkeyShortcutItemViewModel.FromConfig(config));
            }
        }

        /// <summary>
        /// Adds a new, not-yet-configured shortcut row (no hotkey set yet, first available capture function, the
        /// user's current default destination). The user still has to set a hotkey via the hotkey badge.
        /// </summary>
        public HotkeyShortcutItemViewModel AddHotkeyShortcut()
        {
            string defaultDestination = (CoreConfiguration?.OutputDestinations != null && CoreConfiguration.OutputDestinations.Count > 0)
                ? CoreConfiguration.OutputDestinations[0]
                : "Picker";

            var item = new HotkeyShortcutItemViewModel
            {
                Id = Guid.NewGuid().ToString("N"),
                Hotkey = null,
                CaptureFunctionId = AvailableCaptureFunctions?.FirstOrDefault()?.Id,
                DestinationId = defaultDestination
            };
            HotkeyShortcuts.Add(item);
            return item;
        }

        public void RemoveHotkeyShortcut(HotkeyShortcutItemViewModel item)
        {
            if (item != null)
            {
                HotkeyShortcuts.Remove(item);
            }
        }

        /// <summary>
        /// Validates one shortcut against all the others currently in the list (for immediate feedback while editing,
        /// e.g. right after the hotkey capture modal or the destinations picker closes).
        /// </summary>
        public bool ValidateHotkeyShortcut(HotkeyShortcutItemViewModel item, string candidateHotkey, out string errorMessage)
        {
            if (item == null)
            {
                errorMessage = null;
                return true;
            }

            var candidate = item.ToConfig();
            candidate.Hotkey = candidateHotkey;
            var others = HotkeyShortcuts.Where(h => h != item).Select(h => h.ToConfig());
            return HotkeyShortcutManager.Validate(candidate, others, out errorMessage);
        }

        /// <summary>
        /// Persists every currently valid shortcut and returns a human-readable description of each entry that was
        /// dropped because it was invalid (empty hotkey, unknown capture function, no destination, or a duplicate
        /// hotkey) - validation never blocks saving the rest of the Settings window, it only skips the bad entries.
        /// </summary>
        public IReadOnlyList<string> SaveHotkeyShortcuts()
        {
            var allConfigs = HotkeyShortcuts.Select(h => h.ToConfig()).ToList();
            var validConfigs = new List<HotkeyShortcutConfig>();
            var skipped = new List<string>();

            foreach (var config in allConfigs)
            {
                var others = allConfigs.Where(c => c != config);
                if (HotkeyShortcutManager.Validate(config, others, out string error))
                {
                    validConfigs.Add(config);
                }
                else
                {
                    string label = string.IsNullOrWhiteSpace(config.Hotkey) ? HotkeyShortcutManager.GetCaptureFunctionDisplayName(config.CaptureFunctionId) : config.Hotkey;
                    skipped.Add($"{label}: {error}");
                }
            }

            HotkeyShortcutManager.SaveShortcuts(validConfigs);
            return skipped;
        }
    }
}
