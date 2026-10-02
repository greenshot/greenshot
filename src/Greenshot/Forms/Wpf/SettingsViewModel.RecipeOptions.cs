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
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Base.Core;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;
using Greenshot.Recipes;

namespace Greenshot.Forms.Wpf
{
    /// <summary>
    /// The Recipes tab: the extensions (border, drop shadow, caption, ...) and the options of the recipes, each a group,
    /// and which extensions change which recipe. The values are written when the settings are saved (OK), Cancel keeps the stored values.
    /// </summary>
    public partial class SettingsViewModel
    {
        public ObservableCollection<RecipeOptionGroup> RecipeOptionGroups { get; } = new ObservableCollection<RecipeOptionGroup>();

        /// <summary>
        /// Per recipe, the extensions which change it with the values on the tab
        /// </summary>
        public ObservableCollection<RecipeChangeItem> RecipeChanges { get; } = new ObservableCollection<RecipeChangeItem>();

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
                    var group = new RecipeOptionGroup(extension, _recipesForOptions.Where(r => RecipeComposer.CanExtend(extension, r)).ToList());
                    group.Changed += (sender, args) => UpdateRecipeChanges();
                    RecipeOptionGroups.Add(group);
                }
                foreach (var recipe in _recipesForOptions.Where(r => r.HasOptions))
                {
                    RecipeOptionGroups.Add(new RecipeOptionGroup(recipe, null));
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
                RecipeChanges.Add(new RecipeChangeItem(combination.Key, string.Join(", ", combination.Select(c => c.Recipe.Name ?? c.Recipe.Id))));
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

    /// <summary>
    /// Extensions and the recipes they change, shown at the end of the Recipes tab
    /// </summary>
    public class RecipeChangeItem
    {
        public RecipeChangeItem(string extensions, string recipes)
        {
            Extensions = extensions;
            Recipes = recipes;
        }

        public string Extensions { get; }

        public string Recipes { get; }
    }

    /// <summary>
    /// A recipe or extension in the list of the "only these recipes", "except these recipes" or "only for these destinations" choice
    /// </summary>
    public class RecipeScopeItem : INotifyPropertyChanged
    {
        private bool _isChecked;

        public RecipeScopeItem(string id, string name, bool isChecked)
        {
            Id = id;
            Name = name;
            _isChecked = isChecked;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Id { get; }

        public string Name { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }
    }

    /// <summary>
    /// The options of one recipe or extension. An extension with an on/off option has it as checkbox in front of its name,
    /// its other options are only shown while it is on; it also has the recipes it is used in (and the destinations).
    /// </summary>
    public class RecipeOptionGroup : INotifyPropertyChanged
    {
        private readonly FlowDefinition _definition;

        /// <param name="definition">The recipe or extension</param>
        /// <param name="extensibleRecipes">Extension only: the recipes it can change, for "Use in"</param>
        public RecipeOptionGroup(FlowDefinition definition, IReadOnlyList<CaptureRecipe> extensibleRecipes)
        {
            _definition = definition;
            Items = new ObservableCollection<RecipeOptionItem>((definition.Options ?? new List<RecipeOption>()).Where(o => o != null).Select(o => new RecipeOptionItem(o, RecipeOptionStore.GetValue(definition, o))));
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
                UseIn = new ObservableCollection<RecipeScopeItem>((extensibleRecipes ?? Array.Empty<CaptureRecipe>())
                    .Select(r => new RecipeScopeItem(r.Id, r.Name ?? r.Id, (stored.ApplyToAll || stored.OnlyRecipes.Contains(r.Id)) && !stored.ExceptRecipes.Contains(r.Id))));
                OnlyDestinations = new ObservableCollection<RecipeScopeItem>();
                if (IsDestinationSlot)
                {
                    foreach (var destination in DestinationHelper.GetAllDestinations().Where(d => !string.Equals(d.Designation, "Picker", StringComparison.OrdinalIgnoreCase)))
                    {
                        OnlyDestinations.Add(new RecipeScopeItem(destination.Designation, destination.Descriptor?.DisplayName ?? destination.Designation, stored.OnlyDestinations.Contains(destination.Designation)));
                    }
                }
                foreach (var scopeItem in UseIn.Concat(OnlyDestinations))
                {
                    scopeItem.PropertyChanged += (sender, args) => OnChanged();
                }
            }
            BodyItems = new ObservableCollection<RecipeOptionItem>(Items.Where(i => i != SwitchItem));
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
        public ObservableCollection<RecipeOptionItem> Items { get; }

        /// <summary>
        /// The options shown under the name: all but the on/off option, which is the checkbox in front of the name
        /// </summary>
        public ObservableCollection<RecipeOptionItem> BodyItems { get; }

        /// <summary>
        /// The on/off option of an extension, null when there is none (recipes, extensions which are always on)
        /// </summary>
        public RecipeOptionItem SwitchItem { get; }

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
        public ObservableCollection<RecipeScopeItem> UseIn { get; }

        public bool HasUseIn => UseIn != null && UseIn.Count > 0;

        public ObservableCollection<RecipeScopeItem> OnlyDestinations { get; }

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
                OnlyDestinations = new HashSet<string>(OnlyDestinations?.Where(i => i.IsChecked).Select(i => i.Id) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase)
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
                scopeItem.IsChecked = false;
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
    /// <summary>
    /// One option in the settings, the template is picked by its type
    /// </summary>
    public class RecipeOptionItem : INotifyPropertyChanged
    {
        private object _value;
        private RecipeOptionItem _switch;

        public RecipeOptionItem(RecipeOption option, object value)
        {
            Option = option;
            _value = option.TryConvert(value, out var converted) ? converted : option.GetDefaultValue();
            Choices = option.Choices?.Where(c => c != null).ToList() ?? new List<RecipeOptionChoice>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public RecipeOption Option { get; }

        public string Label => Option.DisplayLabel;

        public string Description => Option.DisplayDescription;

        public bool IsBoolean => Option.Type == ContractDataType.Boolean;
        public bool IsNumber => Option.Type == ContractDataType.Integer || Option.Type == ContractDataType.Decimal;
        public bool IsColor => Option.Type == ContractDataType.Color;
        public bool IsEnum => Option.Type == ContractDataType.Enum;
        public bool IsText => Option.Type == ContractDataType.String;

        /// <summary>
        /// All but a switch show their label in front of the value
        /// </summary>
        public bool HasLabelColumn => !IsBoolean;

        /// <summary>
        /// The range of a number, shown next to the box
        /// </summary>
        public string RangeText
        {
            get
            {
                if (!IsNumber || (!Option.Min.HasValue && !Option.Max.HasValue)) return null;
                string min = Option.Min?.ToString(CultureInfo.CurrentCulture) ?? "…";
                string max = Option.Max?.ToString(CultureInfo.CurrentCulture) ?? "…";
                return $"{min} – {max}";
            }
        }

        public IReadOnlyList<RecipeOptionChoice> Choices { get; }

        /// <summary>
        /// The Boolean option of the recipe this option depends on (enabledWhen)
        /// </summary>
        public RecipeOptionItem Switch
        {
            get => _switch;
            set
            {
                if (_switch != null) _switch.PropertyChanged -= OnSwitchChanged;
                _switch = value;
                if (_switch != null) _switch.PropertyChanged += OnSwitchChanged;
                OnPropertyChanged(nameof(IsEditable));
            }
        }

        public bool IsEditable => _switch == null || (_switch.Value is bool on && on);

        public object Value
        {
            get => _value;
            set
            {
                if (!Option.TryConvert(value, out var converted) || Equals(converted, _value)) return;
                _value = converted;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BoolValue));
                OnPropertyChanged(nameof(TextValue));
                OnPropertyChanged(nameof(SelectedChoice));
                OnPropertyChanged(nameof(ColorBrush));
            }
        }

        public bool BoolValue
        {
            get => _value is bool b && b;
            set => Value = value;
        }

        /// <summary>
        /// Numbers, texts and colors are edited as text, a value which doesn't fit the option is not taken
        /// </summary>
        public string TextValue
        {
            get => _value switch
            {
                int i => i.ToString(CultureInfo.CurrentCulture),
                double d => d.ToString(CultureInfo.CurrentCulture),
                _ => Convert.ToString(_value, CultureInfo.InvariantCulture)
            };
            set
            {
                if (IsNumber && decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var number))
                {
                    Value = number.ToString(CultureInfo.InvariantCulture);
                    // A clamped number shows the value which is used
                    OnPropertyChanged();
                    return;
                }
                Value = value;
            }
        }

        public RecipeOptionChoice SelectedChoice
        {
            get => Choices.FirstOrDefault(c => string.Equals(c.Value, _value as string, StringComparison.OrdinalIgnoreCase));
            set
            {
                if (value != null) Value = value.Value;
            }
        }

        public System.Windows.Media.Brush ColorBrush
        {
            get
            {
                if (!IsColor || !(_value is string text)) return null;
                var color = RecipeOptionColors.Parse(text);
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            }
        }

        private void OnSwitchChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Value))
            {
                OnPropertyChanged(nameof(IsEditable));
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Converts the "#RRGGBB" / "#AARRGGBB" text of a color option
    /// </summary>
    public static class RecipeOptionColors
    {
        public static System.Drawing.Color Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return System.Drawing.Color.Black;
            string hex = text.Trim().TrimStart('#');
            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb)) return System.Drawing.Color.Black;
            if (hex.Length == 6) argb |= 0xFF000000;
            return System.Drawing.Color.FromArgb(unchecked((int)argb));
        }

        public static string Format(System.Drawing.Color color)
        {
            return color.A == 255
                ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
