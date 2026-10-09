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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;
using Greenshot.Base.Languages;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The options of one recipe or extension. An extension with an on/off option has it as checkbox in front of its name,
    /// its other options are only shown while it is on; it also has the recipes it is used in (and the destinations).
    /// </summary>
    public class RecipeOptionGroupViewModel : INotifyPropertyChanged
    {
        private readonly FlowDefinition _definition;

        /// <param name="definition">The recipe or extension</param>
        /// <param name="extensibleRecipes">Extension only: the recipes it can change, for "Use in"</param>
        public RecipeOptionGroupViewModel(FlowDefinition definition, IReadOnlyList<CaptureRecipe> extensibleRecipes)
        {
            _definition = definition;
            Items = new ObservableCollection<RecipeOptionViewModel>((definition.Options ?? new List<RecipeOption>()).Where(o => o != null).Select(o => new RecipeOptionViewModel(o, RecipeOptionStore.GetValue(definition, o))));
            foreach (var item in Items)
            {
                if (!string.IsNullOrWhiteSpace(item.Option.EnabledWhen))
                {
                    item.Switch = Items.FirstOrDefault(i => string.Equals(i.Option.Key, item.Option.EnabledWhen, StringComparison.OrdinalIgnoreCase) && i.IsBoolean);
                }
                item.PropertyChanged += (sender, args) => OnChanged();
            }

            if (Extension != null)
            {
                SwitchItem = Items.FirstOrDefault(i => i.IsBoolean && string.Equals(i.Option.Key, RecipeExtension.EnabledOptionKey, StringComparison.OrdinalIgnoreCase));
                if (SwitchItem != null)
                {
                    SwitchItem.PropertyChanged += (sender, args) => OnPropertyChanged(nameof(IsOn));
                }

                // Checked: the extension changes the recipe. New recipes get it too, so only the unchecked ones are stored.
                var stored = RecipeExtensionSettings.FromStore(Extension);
                UseIn = new ObservableCollection<RecipeScopeViewModel>((extensibleRecipes ?? Array.Empty<CaptureRecipe>())
                    .Select(r => new RecipeScopeViewModel(r.Id, r.Name ?? r.Id, (stored.ApplyToAll || stored.OnlyRecipes.Contains(r.Id)) && !stored.ExceptRecipes.Contains(r.Id))));
                OnlyDestinations = new ObservableCollection<RecipeScopeViewModel>();
                if (IsDestinationSlot)
                {
                    foreach (var destination in DestinationHelper.GetAllDestinations().Where(d => !string.Equals(d.Designation, "Picker", StringComparison.OrdinalIgnoreCase)))
                    {
                        OnlyDestinations.Add(new RecipeScopeViewModel(destination.Designation, destination.Descriptor?.DisplayName ?? destination.Designation,
                            stored.OnlyDestinations.Count == 0 || stored.OnlyDestinations.Contains(destination.Designation)));
                    }
                }
                foreach (var scopeItem in UseIn.Concat(OnlyDestinations))
                {
                    scopeItem.PropertyChanged += (sender, args) =>
                    {
                        OnPropertyChanged(nameof(ScopeSummary));
                        OnChanged();
                    };
                }
            }
            BodyItems = new ObservableCollection<RecipeOptionViewModel>(Items.Where(i => i != SwitchItem));
            ResetCommand = new RelayCommand(Reset);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// A value changed, which may change which recipes the extension changes
        /// </summary>
        public event EventHandler Changed;

        public RecipeExtension Extension => _definition as RecipeExtension;

        public string Id => _definition.Id;

        public string Name => RecipeText.Translate(_definition.Name ?? _definition.Id);

        public string Description => RecipeText.Translate(_definition.Description);

        /// <summary>
        /// All options, the on/off option of an extension included
        /// </summary>
        public ObservableCollection<RecipeOptionViewModel> Items { get; }

        /// <summary>
        /// The options shown under the name: all but the on/off option, which is the checkbox in front of the name
        /// </summary>
        public ObservableCollection<RecipeOptionViewModel> BodyItems { get; }

        /// <summary>
        /// The on/off option of an extension, null when there is none (recipes, extensions which are always on)
        /// </summary>
        public RecipeOptionViewModel SwitchItem { get; }

        public bool HasSwitch => SwitchItem != null;

        public bool HasNoSwitch => SwitchItem == null;

        /// <summary>
        /// Switched on (always for a group without switch): the options are shown
        /// </summary>
        public bool IsOn
        {
            get => SwitchItem == null || SwitchItem.BoolValue;
            set
            {
                if (SwitchItem != null) SwitchItem.BoolValue = value;
            }
        }

        /// <summary>
        /// Extension only: the recipes it can change, checked when it changes them
        /// </summary>
        public ObservableCollection<RecipeScopeViewModel> UseIn { get; }

        public bool HasUseIn => UseIn != null && UseIn.Count > 0;

        /// <summary>
        /// Where the extension is used, in one line: "all captures → all destinations", "3 of 7 captures → Email", ...
        /// </summary>
        public string ScopeSummary
        {
            get
            {
                if (UseIn == null) return null;
                int checkedCaptures = UseIn.Count(i => i.IsChecked);
                string captures = checkedCaptures == UseIn.Count
                    ? Texts.Settings.RecipesScopeAllcaptures
                    : checkedCaptures == 0
                        ? Texts.Settings.RecipesScopeNocaptures
                        : string.Format(Texts.Settings.RecipesScopeSomecaptures, checkedCaptures, UseIn.Count);
                if (OnlyDestinations == null || OnlyDestinations.Count == 0) return captures;
                var checkedDestinations = OnlyDestinations.Where(i => i.IsChecked).ToList();
                string destinations = checkedDestinations.Count == OnlyDestinations.Count
                    ? Texts.Settings.RecipesScopeAlldestinations
                    : string.Join(", ", checkedDestinations.Select(i => i.Name));
                return $"{captures} → {destinations}";
            }
        }

        public ObservableCollection<RecipeScopeViewModel> OnlyDestinations { get; }

        /// <summary>
        /// The extension runs per destination (BeforeDestination): it gets "only for these destinations"
        /// </summary>
        public bool IsDestinationSlot => Extension?.SlotName == RecipeSlots.BeforeDestination;

        /// <summary>
        /// The settings of the extension as they are on the tab
        /// </summary>
        public RecipeExtensionSettings CurrentSettings()
        {
            return new RecipeExtensionSettings
            {
                Enabled = IsOn,
                ApplyToAll = true,
                ExceptRecipes = new HashSet<string>(UseIn?.Where(i => !i.IsChecked).Select(i => i.Id) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
                // All destinations checked: stored as "all", so destinations added later get it too
                OnlyDestinations = OnlyDestinations == null || OnlyDestinations.All(i => i.IsChecked)
                    ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(OnlyDestinations.Where(i => i.IsChecked).Select(i => i.Id), StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Back to the defaults (stored on OK)
        /// </summary>
        public System.Windows.Input.ICommand ResetCommand { get; }

        public void Reset()
        {
            foreach (var item in Items.Where(i => i != SwitchItem))
            {
                item.Value = item.Option.GetDefaultValue();
            }
            if (Extension == null) return;
            foreach (var scopeItem in UseIn)
            {
                scopeItem.IsChecked = true;
            }
            foreach (var scopeItem in OnlyDestinations)
            {
                scopeItem.IsChecked = true;
            }
        }

        public void Save()
        {
            foreach (var item in Items)
            {
                RecipeOptionStore.SetValue(_definition.Id, item.Option, item.Value);
            }
            if (Extension == null) return;
            var settings = CurrentSettings();
            RecipeOptionStore.SetValue(Extension.Id, RecipeExtension.ApplyToOption, RecipeExtension.ApplyToAll);
            RecipeOptionStore.SetValue(Extension.Id, RecipeExtension.OnlyRecipesOption, string.Empty);
            RecipeOptionStore.SetValue(Extension.Id, RecipeExtension.ExceptRecipesOption, string.Join(",", settings.ExceptRecipes.OrderBy(i => i, StringComparer.OrdinalIgnoreCase)));
            RecipeOptionStore.SetValue(Extension.Id, RecipeExtension.OnlyDestinationsOption, string.Join(",", settings.OnlyDestinations.OrderBy(i => i, StringComparer.OrdinalIgnoreCase)));
        }

        private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
