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
using log4net;
using Newtonsoft.Json;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// A single user-configured entry mapping one hotkey to a capture function and one or more export destinations.
    /// A whole list of these is persisted as JSON in <see cref="ICoreConfiguration.HotkeyShortcutsJson"/>, replacing the
    /// legacy single RegionHotkey/WindowHotkey/FullscreenHotkey/LastregionHotkey/ClipboardHotkey fields (see
    /// CoreConfigurationImpl.OnAfterLoad for the one-time migration).
    /// </summary>
    public class HotkeyShortcutConfig
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeyShortcutConfig));

        /// <summary>Stable technical identifier of this entry, independent from any translatable text. Never reused.</summary>
        public string Id { get; set; }

        /// <summary>The hotkey sequence string, e.g. "Ctrl + PrintScreen" (parsed with HotkeySequence.Parse).</summary>
        public string Hotkey { get; set; }

        /// <summary>Stable technical id of the capture function, one of the constants in <see cref="HotkeyCaptureFunctions"/>.</summary>
        public string CaptureFunctionId { get; set; }

        /// <summary>Stable technical destination designations (IDestination.Designation), one or more.</summary>
        public List<string> Destinations { get; set; } = new List<string>();

        public HotkeyShortcutConfig()
        {
        }

        /// <summary>
        /// Creates a new entry with a freshly generated stable id.
        /// </summary>
        public static HotkeyShortcutConfig Create(string hotkey, string captureFunctionId, IEnumerable<string> destinations)
        {
            return new HotkeyShortcutConfig
            {
                Id = Guid.NewGuid().ToString("N"),
                Hotkey = hotkey,
                CaptureFunctionId = captureFunctionId,
                Destinations = destinations != null ? new List<string>(destinations) : new List<string>()
            };
        }

        /// <summary>
        /// Deserializes a JSON array of entries; never throws, returns an empty list for null/empty/invalid JSON so a
        /// corrupted or hand-edited ini value can never prevent the application from starting.
        /// </summary>
        public static List<HotkeyShortcutConfig> ParseList(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<HotkeyShortcutConfig>();
            }

            try
            {
                var list = JsonConvert.DeserializeObject<List<HotkeyShortcutConfig>>(json);
                return list ?? new List<HotkeyShortcutConfig>();
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not parse the hotkey shortcuts configuration, ignoring it: {ex.Message}");
                return new List<HotkeyShortcutConfig>();
            }
        }

        /// <summary>
        /// Serializes a list of entries to JSON; null is treated as an empty list.
        /// </summary>
        public static string ToJson(List<HotkeyShortcutConfig> entries)
        {
            return JsonConvert.SerializeObject(entries ?? new List<HotkeyShortcutConfig>());
        }

        public override string ToString() => $"{Hotkey} -> {CaptureFunctionId} -> [{string.Join(", ", Destinations ?? new List<string>())}]";
    }
}
