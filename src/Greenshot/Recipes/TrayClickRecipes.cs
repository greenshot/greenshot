/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Recipes.Triggers;
using log4net;

namespace Greenshot.Recipes
{
    /// <summary>
    /// The recipes a click or double-click on the tray icon can start (Settings > General), and starting them
    /// </summary>
    public static class TrayClickRecipes
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TrayClickRecipes));

        /// <summary>
        /// The triggers which hand something to the recipe (the editor, a file, a browser capture, arguments, the capture of a
        /// clicked notification):
        /// a recipe which only has these is meant for them, not for a click
        /// </summary>
        private static readonly HashSet<string> InputTriggerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            TriggerConfig.TypeEditor,
            TriggerConfig.TypeOpenFile,
            TriggerConfig.TypeExtension,
            TriggerConfig.TypeCommandline,
            TriggerConfig.TypeAiTool,
            TriggerConfig.TypeNotification
        };

        /// <summary>
        /// Whether a click can start the recipe: it is on, not only meant for the editor, a file, the browser extension, the
        /// command line or AI tools, and its steps need no variable which only a caller can give
        /// </summary>
        /// <param name="recipe">The recipe</param>
        /// <param name="registry">The contracts of the steps, default <see cref="StepRegistry.Instance"/></param>
        public static bool IsSuitable(CaptureRecipe recipe, IStepRegistry registry = null)
        {
            if (recipe == null || !recipe.IsEnabled)
            {
                return false;
            }

            var triggers = recipe.Triggers?.Where(t => t != null && t.IsActive).ToList();
            if (triggers?.Count > 0 && triggers.All(t => InputTriggerTypes.Contains(t.TriggerType ?? string.Empty)))
            {
                return false;
            }

            try
            {
                // Without the triggers, what the steps need from outside
                var flowOnly = recipe.Clone();
                flowOnly.Triggers = new List<TriggerConfig>();
                var contract = RecipeContract.Analyze(flowOnly, registry);
                return contract != null && !contract.Inputs.Any(input => input.Required && !input.Conditional);
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't check whether the recipe '{recipe.Id}' can run from a click.", ex);
                return false;
            }
        }

        /// <summary>
        /// The recipes a click can start, sorted by their name
        /// </summary>
        public static IReadOnlyList<CaptureRecipe> GetSuitable(IRecipeManager recipeManager = null, IStepRegistry registry = null)
        {
            recipeManager ??= RecipeManager.Instance;
            return (recipeManager.GetAllRecipes() ?? Array.Empty<CaptureRecipe>())
                .Where(recipe => IsSuitable(recipe, registry))
                .OrderBy(GetDisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The name of the recipe in the list: the text of its item in the tray menu (translated for the built-in recipes),
        /// else its name
        /// </summary>
        public static string GetDisplayName(CaptureRecipe recipe)
        {
            if (recipe == null)
            {
                return null;
            }

            for (int i = 0; i < (recipe.Triggers?.Count ?? 0); i++)
            {
                var trigger = recipe.Triggers[i];
                // An item with a sub menu (e.g. "Capture window from list") doesn't say what the recipe does alone
                if (trigger != null && string.Equals(trigger.TriggerType, TriggerConfig.TypeContextMenu, StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrEmpty(trigger.GetParameter<string>(TriggerConfig.ExpandParameter)))
                {
                    string menuText = recipe.GetTriggerMenuText(i);
                    if (!string.IsNullOrWhiteSpace(menuText))
                    {
                        return menuText;
                    }
                }
            }

            return recipe.DisplayName;
        }

        /// <summary>
        /// Start the recipe for a click on the tray icon; an empty id does nothing, a recipe which is gone or off is logged
        /// </summary>
        /// <param name="recipeId">The id from TrayClickRecipe or TrayDoubleClickRecipe</param>
        /// <param name="clickName">"Tray icon click" or "Tray icon double-click", for the log</param>
        public static void Start(string recipeId, string clickName)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return;
            }

            var triggerManager = SimpleServiceProvider.Current.GetInstance<ITriggerManager>(isOptional: true) as TriggerManager ?? TriggerManager.Instance;
            triggerManager.Run(recipeId.Trim(), new ManualTrigger(clickName, recipeId.Trim()));
        }
    }
}
