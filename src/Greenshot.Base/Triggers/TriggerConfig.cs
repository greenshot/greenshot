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
using Newtonsoft.Json.Linq;

namespace Greenshot.Base.Triggers
{
    /// <summary>
    /// Configuration entity for a modular trigger attached to a capture recipe.
    /// Supports hotkeys, systray/context menu entries, clipboard monitors, and manual triggers.
    /// </summary>
    public class TriggerConfig
    {
        public const string TypeHotkey = "Hotkey";
        public const string TypeContextMenu = "ContextMenu";
        public const string TypeSystray = "Systray";
        public const string TypeClipboard = "Clipboard";
        public const string TypeManual = "Manual";
        public const string TypeSchedule = "Schedule";

        /// <summary>
        /// The type of trigger (e.g. "Hotkey", "ContextMenu", "Clipboard", "Manual").
        /// </summary>
        public string TriggerType { get; set; }

        /// <summary>
        /// Optional human-readable name or label for this trigger.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether this trigger is active. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Trigger-specific parameters (e.g. Hotkey, MenuItemText, Group, Order).
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public TriggerConfig()
        {
        }

        public TriggerConfig(string triggerType, string name = null)
        {
            TriggerType = triggerType ?? throw new ArgumentNullException(nameof(triggerType));
            Name = name ?? triggerType;
        }

        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (Parameters == null || !Parameters.TryGetValue(key, out var rawValue) || rawValue == null)
            {
                return defaultValue;
            }

            try
            {
                if (rawValue is T typedVal)
                {
                    return typedVal;
                }

                if (rawValue is JToken jToken)
                {
                    return jToken.ToObject<T>();
                }

                Type targetType = typeof(T);
                if (targetType.IsEnum)
                {
                    if (rawValue is string strVal)
                    {
                        return (T)Enum.Parse(targetType, strVal, true);
                    }
                    return (T)Enum.ToObject(targetType, rawValue);
                }

                return (T)Convert.ChangeType(rawValue, targetType);
            }
            catch
            {
                return defaultValue;
            }
        }

        public TriggerConfig SetParameter(string key, object value)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            Parameters[key] = value;
            return this;
        }

        public TriggerConfig Clone()
        {
            var clone = new TriggerConfig
            {
                TriggerType = TriggerType,
                Name = Name,
                Enabled = Enabled,
                Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            };

            if (Parameters != null)
            {
                foreach (var kvp in Parameters)
                {
                    clone.Parameters[kvp.Key] = kvp.Value;
                }
            }

            return clone;
        }

        public static TriggerConfig CreateHotkey(string hotkey, string name = null)
        {
            var config = new TriggerConfig(TypeHotkey, name ?? "Hotkey");
            config.SetParameter("Hotkey", hotkey);
            return config;
        }

        public static TriggerConfig CreateContextMenu(string menuItemText = null, string group = "Recipes", int order = 0)
        {
            var config = new TriggerConfig(TypeContextMenu, menuItemText ?? "Context Menu");
            if (!string.IsNullOrEmpty(menuItemText))
            {
                config.SetParameter("MenuItemText", menuItemText);
            }
            config.SetParameter("Group", group ?? "Recipes");
            config.SetParameter("Order", order);
            return config;
        }

        public static TriggerConfig CreateClipboard(bool onImageCopied = true, string name = null)
        {
            var config = new TriggerConfig(TypeClipboard, name ?? "Clipboard Monitor");
            config.SetParameter("OnImageCopied", onImageCopied);
            return config;
        }

        public override string ToString() => $"{TriggerType}: {Name} (Enabled={Enabled})";
    }
}
