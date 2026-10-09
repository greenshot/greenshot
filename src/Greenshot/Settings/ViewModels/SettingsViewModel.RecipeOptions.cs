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
using Greenshot.Base.Recipes;
using Greenshot.Recipes;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The Recipes tab: the extensions (border, drop shadow, caption, ...) and the options of the recipes, each a group,
    /// and which extensions change which recipe. The values are written when the settings are saved (OK), Cancel keeps the stored values.
    /// </summary>
    public partial class SettingsViewModel
    {
        public ObservableCollection<RecipeOptionGroupViewModel> RecipeOptionGroups { get; } = new ObservableCollection<RecipeOptionGroupViewModel>();

        /// <summary>
        /// Per recipe, the extensions which change it with the values on the tab
        /// </summary>
        public ObservableCollection<RecipeChangeViewModel> RecipeChanges { get; } = new ObservableCollection<RecipeChangeViewModel>();

        public bool HasRecipeOptions => RecipeOptionGroups.Count > 0;

        public bool HasRecipeChanges => RecipeChanges.Count > 0;

        public bool HasNoRecipeChanges => !HasRecipeChanges;

        private IReadOnlyList<CaptureRecipe> _recipesForOptions = Array.Empty<CaptureRecipe>();

        private void InitializeRecipeOptions()
        {
            RecipeOptionGroups.Clear();
            try
            {
                var manager = RecipeManager.Instance;
                _recipesForOptions = manager.GetAllRecipes().Where(r => r != null).OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                foreach (var extension in manager.GetAllExtensions().Where(e => e != null).OrderBy(e => e.Extends?.Order ?? 0).ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var group = new RecipeOptionGroupViewModel(extension, _recipesForOptions.Where(r => RecipeComposer.CanExtend(extension, r)).ToList());
                    group.Changed += (sender, args) => UpdateRecipeChanges();
                    RecipeOptionGroups.Add(group);
                }
                foreach (var recipe in _recipesForOptions.Where(r => r.HasOptions))
                {
                    RecipeOptionGroups.Add(new RecipeOptionGroupViewModel(recipe, null));
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't read the options of the recipes.", ex);
            }
            UpdateRecipeChanges();
            OnPropertyChanged(nameof(HasRecipeOptions));
        }

        /// <summary>
        /// Which extensions change which recipe, with the values as they are on the tab (not yet saved)
        /// </summary>
        private void UpdateRecipeChanges()
        {
            var extensionGroups = RecipeOptionGroups.Where(g => g.Extension != null).ToList();
            var extensions = extensionGroups.Select(g => g.Extension).ToList();
            RecipeExtensionSettings SettingsOf(RecipeExtension extension) => extensionGroups.First(g => g.Extension == extension).CurrentSettings();

            // One line per combination of extensions, with the recipes they change
            RecipeChanges.Clear();
            var combinations = _recipesForOptions
                .Select(recipe => (Recipe: recipe, Extensions: string.Join(", ", RecipeComposer.FindApplicableExtensions(recipe, extensions, SettingsOf).Select(e => RecipeText.Translate(e.Name ?? e.Id)))))
                .Where(c => c.Extensions.Length > 0)
                .GroupBy(c => c.Extensions);
            foreach (var combination in combinations)
            {
                RecipeChanges.Add(new RecipeChangeViewModel(combination.Key, string.Join(", ", combination.Select(c => c.Recipe.Name ?? c.Recipe.Id))));
            }
            OnPropertyChanged(nameof(HasRecipeChanges));
            OnPropertyChanged(nameof(HasNoRecipeChanges));
        }

        /// <summary>
        /// Writes the values of the Recipes tab to greenshot.ini
        /// </summary>
        public void SaveRecipeOptions()
        {
            foreach (var group in RecipeOptionGroups)
            {
                group.Save();
            }
        }
    }
}
