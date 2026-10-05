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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Greenshot.Base.Recipes.Triggers
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
        public const string TypeEditor = "Editor";
        public const string TypeManual = "Manual";
        public const string TypeSchedule = "Schedule";
        public const string TypeCommandline = "Commandline";
        public const string TypeOpenFile = "OpenFile";
        public const string TypeExtension = "Extension";
        public const string TypeAiTool = "AiTool";

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
        /// False when the user didn't approve this trigger for a recipe from a file (the approval window lists each trigger,
        /// the decision is kept in the trust store, not in the recipe file). Not saved with the recipe.
        /// </summary>
        [JsonIgnore]
        public bool IsApproved { get; set; } = true;

        /// <summary>
        /// Whether the trigger may start the recipe: enabled and approved
        /// </summary>
        [JsonIgnore]
        public bool IsActive => Enabled && IsApproved;

        /// <summary>
        /// False when the user approved a Commandline trigger, but not its AllowBrowserInvocation (web pages and the browser
        /// extension). Not saved with the recipe.
        /// </summary>
        [JsonIgnore]
        public bool IsBrowserInvocationApproved { get; set; } = true;

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
                IsApproved = IsApproved,
                IsBrowserInvocationApproved = IsBrowserInvocationApproved,
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

        public static TriggerConfig CreateEditor(string menuItemText = null, string group = "Recipes", int order = 0)
        {
            var config = new TriggerConfig(TypeEditor, menuItemText ?? "Editor Menu");
            if (!string.IsNullOrEmpty(menuItemText))
            {
                config.SetParameter("MenuItemText", menuItemText);
            }
            config.SetParameter("Group", group ?? "Recipes");
            config.SetParameter("Order", order);
            return config;
        }

        public static TriggerConfig CreateClipboard(bool onImageCopied = true, string formatFilter = null, string name = null)
        {
            var config = new TriggerConfig(TypeClipboard, name ?? "Clipboard Monitor");
            config.SetParameter("OnImageCopied", onImageCopied);
            if (!string.IsNullOrEmpty(formatFilter))
            {
                config.SetParameter("FormatFilter", formatFilter);
            }
            return config;
        }

        public static TriggerConfig CreateCommandline(
            string command = null,
            string description = null,
            bool fireAndForget = false,
            string stdout = null,
            IEnumerable<CommandlineArgument> arguments = null,
            string name = null)
        {
            var config = new TriggerConfig(TypeCommandline, name ?? (command ?? "Commandline"));
            if (!string.IsNullOrEmpty(command))
            {
                config.SetParameter("Command", command);
            }
            if (!string.IsNullOrEmpty(description))
            {
                config.SetParameter("Description", description);
            }
            config.SetParameter("FireAndForget", fireAndForget);
            if (!string.IsNullOrEmpty(stdout))
            {
                config.SetParameter("Stdout", stdout);
            }
            if (arguments != null)
            {
                config.SetParameter("Arguments", new List<CommandlineArgument>(arguments));
            }
            return config;
        }

        public static TriggerConfig CreateOpenFile(string filter = null, bool fireAndForget = false, string name = null)
        {
            var config = new TriggerConfig(TypeOpenFile, name ?? "Open File");
            if (!string.IsNullOrEmpty(filter))
            {
                config.SetParameter("Filter", filter);
            }
            config.SetParameter("FireAndForget", fireAndForget);
            return config;
        }

        public static TriggerConfig CreateExtension(string browser = null, bool fireAndForget = false, string name = null)
        {
            var config = new TriggerConfig(TypeExtension, name ?? "Browser Extension");
            if (!string.IsNullOrEmpty(browser))
            {
                config.SetParameter("Browser", browser);
            }
            config.SetParameter("FireAndForget", fireAndForget);
            return config;
        }

        /// <summary>
        /// A trigger which offers the recipe as a tool to AI tools (MCP clients connected through greenshot-mcp.exe).
        /// </summary>
        /// <param name="toolName">The tool name the AI uses (letters, digits, _ and -)</param>
        /// <param name="description">What the tool does, for the AI</param>
        /// <param name="arguments">The tool's arguments; Window arguments take a window reference from list_windows</param>
        /// <param name="title">Name of the tool for people</param>
        /// <param name="readOnly">True when the tool doesn't change anything (e.g. only captures)</param>
        /// <param name="destructive">True when the tool can overwrite or delete something</param>
        /// <param name="name">Name of the trigger</param>
        public static TriggerConfig CreateAiTool(
            string toolName,
            string description,
            IEnumerable<CommandlineArgument> arguments = null,
            string title = null,
            bool readOnly = true,
            bool destructive = false,
            string name = null)
        {
            var config = new TriggerConfig(TypeAiTool, name ?? (title ?? toolName ?? "AI tool"));
            if (!string.IsNullOrEmpty(toolName))
            {
                config.SetParameter("ToolName", toolName);
            }
            if (!string.IsNullOrEmpty(title))
            {
                config.SetParameter("Title", title);
            }
            if (!string.IsNullOrEmpty(description))
            {
                config.SetParameter("Description", description);
            }
            config.SetParameter("ReadOnly", readOnly);
            config.SetParameter("Destructive", destructive);
            if (arguments != null)
            {
                config.SetParameter("Arguments", new List<CommandlineArgument>(arguments));
            }
            return config;
        }

        public override string ToString() => $"{TriggerType}: {Name} (Enabled={Enabled})";
    }
}
