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
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Recipes;
using log4net;

namespace Greenshot.Triggers
{
    /// <summary>
    /// Default implementation of ITriggerManager managing hotkeys, clipboard watchers, and manual triggers.
    /// </summary>
    public class TriggerManager : ITriggerManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TriggerManager));
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

        private readonly Dictionary<string, ITrigger> _triggers = new Dictionary<string, ITrigger>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        public event EventHandler<TriggerEventArgs> TriggerFired;

        private static TriggerManager _instance;
        public static TriggerManager Instance => _instance ??= new TriggerManager();

        public TriggerManager()
        {
            InitializeDefaultTriggers();
        }

        public void InitializeDefaultTriggers()
        {
            if (CoreConfig == null) return;

            // Register hotkeys from legacy config as default hotkey triggers
            if (!string.IsNullOrEmpty(CoreConfig.RegionHotkey))
            {
                RegisterTrigger(new HotkeyTrigger("trigger_hotkey_region", "Region Hotkey", CoreConfig.RegionHotkey, RecipeManager.RecipeIdRegion));
            }

            if (!string.IsNullOrEmpty(CoreConfig.WindowHotkey))
            {
                string targetRecipe = CoreConfig.CaptureWindowsInteractive ? RecipeManager.RecipeIdWindow : RecipeManager.RecipeIdActiveWindow;
                RegisterTrigger(new HotkeyTrigger("trigger_hotkey_window", "Window Hotkey", CoreConfig.WindowHotkey, targetRecipe));
            }

            if (!string.IsNullOrEmpty(CoreConfig.FullscreenHotkey))
            {
                RegisterTrigger(new HotkeyTrigger("trigger_hotkey_fullscreen", "Fullscreen Hotkey", CoreConfig.FullscreenHotkey, RecipeManager.RecipeIdFullScreen));
            }

            if (!string.IsNullOrEmpty(CoreConfig.LastregionHotkey))
            {
                RegisterTrigger(new HotkeyTrigger("trigger_hotkey_lastregion", "Last Region Hotkey", CoreConfig.LastregionHotkey, RecipeManager.RecipeIdLastRegion));
            }

            if (!string.IsNullOrEmpty(CoreConfig.ClipboardHotkey))
            {
                RegisterTrigger(new HotkeyTrigger("trigger_hotkey_clipboard", "Clipboard Hotkey", CoreConfig.ClipboardHotkey, RecipeManager.RecipeIdClipboard));
            }
        }

        public IReadOnlyList<ITrigger> GetAllTriggers()
        {
            lock (_triggers)
            {
                return _triggers.Values.ToList();
            }
        }

        public IReadOnlyList<ContextMenuTrigger> GetContextMenuTriggers()
        {
            lock (_triggers)
            {
                return _triggers.Values.OfType<ContextMenuTrigger>().ToList();
            }
        }

        public HotkeyTrigger FindHotkeyTriggerForRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;
            lock (_triggers)
            {
                return _triggers.Values.OfType<HotkeyTrigger>()
                    .FirstOrDefault(h => string.Equals(h.TargetRecipeId, recipeId, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Synchronizes modular triggers attached to registered recipes.
        /// Unregisters any previous recipe-attached triggers and registers new ones.
        /// </summary>
        public void SyncRecipeTriggers(IEnumerable<CaptureRecipe> recipes)
        {
            if (recipes == null) return;

            lock (_triggers)
            {
                // Remove all triggers previously registered for recipes
                var recipeTriggerIds = _triggers.Keys
                    .Where(k => k.StartsWith("trigger_recipe_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var tid in recipeTriggerIds)
                {
                    UnregisterTrigger(tid);
                }

                foreach (var recipe in recipes)
                {
                    if (recipe.Triggers == null || recipe.Triggers.Count == 0)
                    {
                        if (!recipe.IsBuiltIn && recipe.ShowInContextMenu)
                        {
                            string triggerId = $"trigger_recipe_{recipe.Id}_menu_default";
                            RegisterTrigger(new ContextMenuTrigger(triggerId, recipe.Name, recipe.Name, recipe.Id, "Recipes", 0));
                        }
                        continue;
                    }

                    bool hasMenuTrigger = false;
                    for (int i = 0; i < recipe.Triggers.Count; i++)
                    {
                        var tc = recipe.Triggers[i];
                        if (!tc.Enabled) continue;

                        if (string.Equals(tc.TriggerType, TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase))
                        {
                            string hotkey = tc.GetParameter<string>("Hotkey");
                            if (!string.IsNullOrWhiteSpace(hotkey))
                            {
                                string triggerId = $"trigger_recipe_{recipe.Id}_hotkey_{i}";
                                RegisterTrigger(new HotkeyTrigger(triggerId, tc.Name ?? $"{recipe.Name} Hotkey", hotkey, recipe.Id));
                            }
                        }
                        else if (string.Equals(tc.TriggerType, TriggerConfig.TypeContextMenu, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(tc.TriggerType, TriggerConfig.TypeSystray, StringComparison.OrdinalIgnoreCase))
                        {
                            hasMenuTrigger = true;
                            string menuText = tc.GetParameter<string>("MenuItemText") ?? recipe.Name;
                            string group = tc.GetParameter<string>("Group", "Recipes");
                            int order = tc.GetParameter<int>("Order", 0);
                            string triggerId = $"trigger_recipe_{recipe.Id}_menu_{i}";
                            RegisterTrigger(new ContextMenuTrigger(triggerId, tc.Name ?? menuText, menuText, recipe.Id, group, order));
                        }
                    }

                    if (!hasMenuTrigger && !recipe.IsBuiltIn && recipe.ShowInContextMenu)
                    {
                        string triggerId = $"trigger_recipe_{recipe.Id}_menu_default";
                        RegisterTrigger(new ContextMenuTrigger(triggerId, recipe.Name, recipe.Name, recipe.Id, "Recipes", 0));
                    }
                }
            }
        }

        public void RegisterTrigger(ITrigger trigger)
        {
            if (trigger == null || string.IsNullOrEmpty(trigger.Id)) throw new ArgumentException("Trigger or Trigger.Id cannot be null/empty");

            lock (_triggers)
            {
                if (_triggers.TryGetValue(trigger.Id, out var existing))
                {
                    existing.Stop();
                    existing.Triggered -= OnTriggerFired;
                    existing.Dispose();
                }

                _triggers[trigger.Id] = trigger;
                trigger.Triggered += OnTriggerFired;
                if (trigger.IsEnabled)
                {
                    trigger.Start();
                }
            }

            Log.InfoFormat("Registered trigger: {0} ({1}) -> Recipe: {2}", trigger.Name, trigger.Id, trigger.TargetRecipeId);
        }

        public bool UnregisterTrigger(string triggerId)
        {
            if (string.IsNullOrEmpty(triggerId)) return false;

            lock (_triggers)
            {
                if (_triggers.TryGetValue(triggerId, out var trigger))
                {
                    trigger.Stop();
                    trigger.Triggered -= OnTriggerFired;
                    trigger.Dispose();
                    _triggers.Remove(triggerId);
                    Log.InfoFormat("Unregistered trigger: {0}", triggerId);
                    return true;
                }
            }

            return false;
        }

        public void StartAll()
        {
            lock (_triggers)
            {
                foreach (var trigger in _triggers.Values)
                {
                    if (trigger.IsEnabled)
                    {
                        trigger.Start();
                    }
                }
            }
        }

        public void StopAll()
        {
            lock (_triggers)
            {
                foreach (var trigger in _triggers.Values)
                {
                    trigger.Stop();
                }
            }
        }

        private void OnTriggerFired(object sender, TriggerEventArgs e)
        {
            TriggerFired?.Invoke(sender, e);

            var recipeManager = SimpleServiceProvider.Current.GetInstance<IRecipeManager>() ?? RecipeManager.Instance;
            var recipe = recipeManager.GetRecipeById(e.TargetRecipeId);

            if (recipe != null)
            {
                var pipeline = SimpleServiceProvider.Current.GetInstance<ICapturePipeline>();
                if (pipeline != null)
                {
                    var trigger = sender as ITrigger;
                    pipeline.ExecuteAsync(recipe, trigger, ctx =>
                    {
                        if (e.Parameters != null)
                        {
                            foreach (var kvp in e.Parameters)
                            {
                                ctx.Properties[kvp.Key] = kvp.Value;
                            }
                        }
                    });
                }
                else
                {
                    Log.WarnFormat("No ICapturePipeline registered to handle trigger {0} for recipe {1}", (sender as ITrigger)?.Name, e.TargetRecipeId);
                }
            }
            else
            {
                Log.WarnFormat("Target recipe '{0}' not found for trigger '{1}'", e.TargetRecipeId, (sender as ITrigger)?.Name);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                StopAll();
                lock (_triggers)
                {
                    foreach (var trigger in _triggers.Values)
                    {
                        trigger.Triggered -= OnTriggerFired;
                        trigger.Dispose();
                    }
                    _triggers.Clear();
                }
            }
        }
    }
}
