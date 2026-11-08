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
using Greenshot.Base.Languages;
using Greenshot.Recipes;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The recipes a click and a double-click on the tray icon start, on the General tab
    /// </summary>
    public partial class SettingsViewModel
    {
        private IList<TrayClickChoice> _trayClickChoices;
        private IList<TrayClickChoice> _trayDoubleClickChoices;

        /// <summary>
        /// The choices for a click: nothing, and the recipes a click can start
        /// </summary>
        public IList<TrayClickChoice> TrayClickChoices => _trayClickChoices ??= CreateTrayClickChoices(CoreConfiguration.TrayClickRecipe);

        /// <summary>
        /// The choices for a double-click: nothing, and the recipes a click can start
        /// </summary>
        public IList<TrayClickChoice> TrayDoubleClickChoices => _trayDoubleClickChoices ??= CreateTrayClickChoices(CoreConfiguration.TrayDoubleClickRecipe);

        public string SelectedTrayClickRecipe
        {
            get => ChoiceId(TrayClickChoices, CoreConfiguration.TrayClickRecipe);
            set
            {
                if (string.Equals(SelectedTrayClickRecipe, value ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                CoreConfiguration.TrayClickRecipe = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string SelectedTrayDoubleClickRecipe
        {
            get => ChoiceId(TrayDoubleClickChoices, CoreConfiguration.TrayDoubleClickRecipe);
            set
            {
                if (string.Equals(SelectedTrayDoubleClickRecipe, value ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                CoreConfiguration.TrayDoubleClickRecipe = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The id as the choice spells it (the setting may differ in case)
        /// </summary>
        private static string ChoiceId(IEnumerable<TrayClickChoice> choices, string recipeId)
        {
            string id = recipeId?.Trim() ?? string.Empty;
            return choices.FirstOrDefault(choice => string.Equals(choice.RecipeId, id, StringComparison.OrdinalIgnoreCase))?.RecipeId ?? id;
        }

        /// <summary>
        /// "Do nothing" and the recipes a click can start; the chosen recipe stays in the list as "(not available)" when it is
        /// gone, switched off or can't run from a click, so the setting is visible
        /// </summary>
        private static IList<TrayClickChoice> CreateTrayClickChoices(string selectedRecipeId)
        {
            var choices = new List<TrayClickChoice> { new TrayClickChoice(string.Empty, Texts.Settings.TrayiconNothing) };
            try
            {
                choices.AddRange(TrayClickRecipes.GetSuitable().Select(recipe => new TrayClickChoice(recipe.Id, TrayClickRecipes.GetDisplayName(recipe))));
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't read the recipes for the tray icon.", ex);
            }

            string selected = selectedRecipeId?.Trim();
            if (!string.IsNullOrEmpty(selected) && !choices.Any(choice => string.Equals(choice.RecipeId, selected, StringComparison.OrdinalIgnoreCase)))
            {
                string name = TrayClickRecipes.GetDisplayName(RecipeManager.Instance.GetRecipeById(selected)) ?? selected;
                choices.Add(new TrayClickChoice(selected, string.Format(Texts.Settings.TrayiconNotavailable, name)));
            }

            return choices;
        }
    }

    /// <summary>
    /// One choice of the tray icon drop-downs
    /// </summary>
    public sealed class TrayClickChoice
    {
        public TrayClickChoice(string recipeId, string displayName)
        {
            RecipeId = recipeId;
            DisplayName = displayName;
        }

        /// <summary>
        /// The id of the recipe, empty for nothing
        /// </summary>
        public string RecipeId { get; }

        public string DisplayName { get; }
    }
}
