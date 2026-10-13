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
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Recipes;
using Greenshot.Recipes.Triggers;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// A capture function offered in the hotkey shortcuts settings tab: a stable technical id (see
    /// <see cref="HotkeyCaptureFunctions"/>) paired with its current translated display name.
    /// </summary>
    public sealed class CaptureFunctionOption
    {
        public CaptureFunctionOption(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public string Id { get; }
        public string DisplayName { get; }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// A destination offered in the hotkey shortcuts settings tab: a stable technical designation (see
    /// IDestination.Designation) paired with its current translated display name.
    /// </summary>
    public sealed class DestinationOption
    {
        public DestinationOption(string designation, string displayName)
        {
            Designation = designation;
            DisplayName = displayName;
        }

        public string Designation { get; }
        public string DisplayName { get; }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Business logic for the freely configurable hotkey shortcuts (hotkey + capture function + one or more
    /// destinations). Reuses the existing hotkey capture/registration logic (HotkeySequence, HotkeyManager,
    /// HotkeyTrigger) and the existing capture/destination pipeline (RecipeManager, DestinationHelper,
    /// DestinationExportStep's "OverrideDestinations" context property) instead of introducing a parallel engine.
    /// </summary>
    public static class HotkeyShortcutManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeyShortcutManager));

        /// <summary>Prefix of the trigger ids this class registers with the ITriggerManager, so they can be told apart.</summary>
        internal const string TriggerIdPrefix = "trigger_hotkey_shortcut_";

        /// <summary>Context property key read by DestinationExportStep.ResolveDestinationDesignations (priority 2).</summary>
        internal const string OverrideDestinationsKey = "OverrideDestinations";

        private static ICoreConfiguration CoreConfig
        {
            get
            {
                try
                {
                    return IniConfigRegistry.GetSection<ICoreConfiguration>();
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Loads the currently configured hotkey shortcuts. Never throws: a corrupted or missing configuration
        /// results in an empty list instead of blocking the caller (e.g. application startup).
        /// </summary>
        public static List<HotkeyShortcutConfig> GetShortcuts()
        {
            var config = CoreConfig;
            if (config == null)
            {
                return new List<HotkeyShortcutConfig>();
            }

            return HotkeyShortcutConfig.ParseList(config.HotkeyShortcutsJson);
        }

        /// <summary>
        /// Persists the given list of hotkey shortcuts to the configuration (does not save the ini file itself,
        /// that happens together with the rest of the Settings window, like every other setting).
        /// </summary>
        public static void SaveShortcuts(List<HotkeyShortcutConfig> shortcuts)
        {
            var config = CoreConfig;
            if (config == null)
            {
                return;
            }

            config.HotkeyShortcutsJson = HotkeyShortcutConfig.ToJson(shortcuts ?? new List<HotkeyShortcutConfig>());
        }

        /// <summary>
        /// Resolves a stable capture function id to the id of the built-in capture recipe it currently maps to.
        /// Returns null for an unknown id. "Window" resolves dynamically to the interactive or the active-window
        /// recipe, following the same CaptureWindowsInteractive setting the legacy WindowHotkey used to follow.
        /// </summary>
        public static string ResolveRecipeId(string captureFunctionId)
        {
            if (string.IsNullOrWhiteSpace(captureFunctionId))
            {
                return null;
            }

            switch (captureFunctionId)
            {
                case HotkeyCaptureFunctions.Region:
                    return RecipeManager.RecipeIdRegion;
                case HotkeyCaptureFunctions.Window:
                    return (CoreConfig?.CaptureWindowsInteractive ?? false) ? RecipeManager.RecipeIdWindow : RecipeManager.RecipeIdActiveWindow;
                case HotkeyCaptureFunctions.Fullscreen:
                    return RecipeManager.RecipeIdFullScreen;
                case HotkeyCaptureFunctions.LastRegion:
                    return RecipeManager.RecipeIdLastRegion;
                case HotkeyCaptureFunctions.Clipboard:
                    return RecipeManager.RecipeIdClipboard;
                default:
                    return null;
            }
        }

        /// <summary>
        /// The current translated display name of a stable capture function id, falling back to the id itself when
        /// no translation is available (e.g. in tests without the language files loaded).
        /// </summary>
        public static string GetCaptureFunctionDisplayName(string captureFunctionId)
        {
            switch (captureFunctionId)
            {
                case HotkeyCaptureFunctions.Region:
                    return Texts.Core.ContextmenuCapturearea ?? captureFunctionId;
                case HotkeyCaptureFunctions.Window:
                    return Texts.Core.ContextmenuCapturewindow ?? captureFunctionId;
                case HotkeyCaptureFunctions.Fullscreen:
                    return Texts.Core.ContextmenuCapturefullscreen ?? captureFunctionId;
                case HotkeyCaptureFunctions.LastRegion:
                    return Texts.Core.ContextmenuCapturelastregion ?? captureFunctionId;
                case HotkeyCaptureFunctions.Clipboard:
                    return Texts.Core.ContextmenuCaptureclipboard ?? captureFunctionId;
                default:
                    return captureFunctionId;
            }
        }

        /// <summary>
        /// The capture functions that can currently be picked for a hotkey shortcut: every id in
        /// <see cref="HotkeyCaptureFunctions.All"/> whose underlying built-in recipe actually exists right now
        /// (robust against e.g. a stripped build or a built-in recipe that was disabled).
        /// </summary>
        public static IReadOnlyList<CaptureFunctionOption> GetAvailableCaptureFunctions()
        {
            var result = new List<CaptureFunctionOption>();
            foreach (var id in HotkeyCaptureFunctions.All)
            {
                string recipeId = ResolveRecipeId(id);
                if (recipeId == null)
                {
                    continue;
                }

                try
                {
                    if (RecipeManager.Instance.GetRecipeById(recipeId) == null)
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Could not look up capture recipe '{recipeId}' for capture function '{id}', skipping it.", ex);
                    continue;
                }

                result.Add(new CaptureFunctionOption(id, GetCaptureFunctionDisplayName(id)));
            }

            return result;
        }

        /// <summary>
        /// All destinations currently available to pick for a hotkey shortcut: the same dynamic, plugin-aware list
        /// the "Destinations" settings tab and the tray menu use (DestinationHelper.GetAllDestinations()).
        /// </summary>
        public static IReadOnlyList<DestinationOption> GetAvailableDestinations()
        {
            var result = new List<DestinationOption>();
            foreach (var destination in DestinationHelper.GetAllDestinations())
            {
                if (destination == null)
                {
                    continue;
                }

                string displayName;
                try
                {
                    displayName = destination.Descriptor?.DisplayName ?? destination.Designation;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Could not resolve the display name of destination '{destination.Designation}'.", ex);
                    displayName = destination.Designation;
                }

                result.Add(new DestinationOption(destination.Designation, displayName));
            }

            return result;
        }

        /// <summary>
        /// Validates a single hotkey shortcut entry: the hotkey itself must be a valid, registrable sequence (reusing
        /// <see cref="HotkeySequence.Validate"/>, the same rules the hotkey capture editor uses), it must reference a
        /// known capture function, have a destination, and must not duplicate the hotkey of another entry.
        /// </summary>
        /// <param name="entry">The entry to validate</param>
        /// <param name="otherEntries">The other configured entries (excluding <paramref name="entry"/>), to detect duplicate hotkeys</param>
        /// <param name="errorMessage">A human-readable (English, like the rest of the hotkey editor) description of the problem, or null when valid</param>
        public static bool Validate(HotkeyShortcutConfig entry, IEnumerable<HotkeyShortcutConfig> otherEntries, out string errorMessage)
        {
            if (entry == null)
            {
                errorMessage = "No shortcut provided.";
                return false;
            }

            var sequence = HotkeySequence.Parse(entry.Hotkey);
            if (!sequence.Validate(out errorMessage))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.CaptureFunctionId) || ResolveRecipeId(entry.CaptureFunctionId) == null)
            {
                errorMessage = "Please choose a capture function.";
                return false;
            }

            if (entry.Destinations == null || entry.Destinations.Count == 0)
            {
                errorMessage = "Please choose a destination.";
                return false;
            }

            if (otherEntries != null)
            {
                foreach (var other in otherEntries)
                {
                    if (other == null || ReferenceEquals(other, entry))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(entry.Id) && string.Equals(other.Id, entry.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var otherSequence = HotkeySequence.Parse(other.Hotkey);
                    if (!otherSequence.IsEmpty && otherSequence.Equals(sequence))
                    {
                        errorMessage = "This hotkey is already assigned to another shortcut.";
                        return false;
                    }
                }
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// The hotkey string of the first shortcut for a given capture function, or <paramref name="fallback"/>
        /// when there is none. Used so the tray menu, the first-start tooltip and the self-service diagnostics keep
        /// showing the live hotkey after it was changed in the shortcuts settings tab (several shortcuts may target
        /// the same capture function with different destinations; only one hint can be shown in those legacy spots).
        /// </summary>
        public static string GetDisplayHotkeyFor(string captureFunctionId, string fallback = null)
        {
            try
            {
                var match = GetShortcuts().FirstOrDefault(s =>
                    s != null && !string.IsNullOrWhiteSpace(s.Hotkey) &&
                    string.Equals(s.CaptureFunctionId, captureFunctionId, StringComparison.OrdinalIgnoreCase));
                return match?.Hotkey ?? fallback;
            }
            catch (Exception ex)
            {
                Log.Warn("Could not resolve the display hotkey for capture function " + captureFunctionId, ex);
                return fallback;
            }
        }

        internal static string BuildTriggerId(string shortcutId) => TriggerIdPrefix + shortcutId;

        /// <summary>
        /// Registers a HotkeyTrigger for every valid hotkey shortcut with the given trigger manager (unregisters
        /// the trigger of a removed entry). Each entry is handled independently: an invalid hotkey, an
        /// unknown capture function or an unresolvable destination only skips that one entry (logged as a warning) and
        /// never prevents the other shortcuts - or the application start - from working.
        /// </summary>
        public static void RegisterAll(ITriggerManager triggerManager)
        {
            if (triggerManager == null)
            {
                return;
            }

            List<HotkeyShortcutConfig> shortcuts;
            try
            {
                shortcuts = GetShortcuts();
            }
            catch (Exception ex)
            {
                Log.Error("Could not load the hotkey shortcuts, none will be registered this session.", ex);
                return;
            }

            foreach (var entry in shortcuts)
            {
                string triggerId = BuildTriggerId(entry?.Id ?? Guid.NewGuid().ToString("N"));
                try
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Hotkey))
                    {
                        // The hotkey was cleared in the editor: make sure a previous registration is gone
                        triggerManager.UnregisterTrigger(triggerId);
                        continue;
                    }

                    string recipeId = ResolveRecipeId(entry.CaptureFunctionId);
                    if (recipeId == null)
                    {
                        Log.WarnFormat("Hotkey shortcut '{0}': unknown capture function '{1}', skipping registration.", entry.Id, entry.CaptureFunctionId);
                        triggerManager.UnregisterTrigger(triggerId);
                        continue;
                    }

                    if (RecipeManager.Instance.GetRecipeById(recipeId) == null)
                    {
                        Log.WarnFormat("Hotkey shortcut '{0}': capture recipe '{1}' is not available, skipping registration.", entry.Id, recipeId);
                        triggerManager.UnregisterTrigger(triggerId);
                        continue;
                    }

                    IDictionary<string, object> extraParameters = null;
                    if (entry.Destinations != null && entry.Destinations.Count > 0)
                    {
                        // Destinations with no backing plugin/IDestination are filtered out gracefully later, by
                        // DestinationExportStep itself (it simply skips a designation it cannot resolve).
                        extraParameters = new Dictionary<string, object> { { OverrideDestinationsKey, new List<string>(entry.Destinations) } };
                    }

                    var name = $"Hotkey shortcut: {GetCaptureFunctionDisplayName(entry.CaptureFunctionId)}";
                    var trigger = new HotkeyTrigger(triggerId, name, entry.Hotkey, recipeId)
                    {
                        ExtraParameters = extraParameters
                    };
                    triggerManager.RegisterTrigger(trigger);
                }
                catch (Exception ex)
                {
                    // One broken entry must never stop the others from registering, nor the application from starting.
                    Log.Error($"Failed to register hotkey shortcut '{entry?.Id}', skipping it.", ex);
                }
            }
        }
    }
}
