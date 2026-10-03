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
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    /// <summary>
    /// The options a recipe or automatic step declares (Settings > Recipes shows them, the steps read them with ${option.key}).
    /// The items edit the options of <see cref="ActiveRecipe"/> directly, the unsaved state and undo see the changes.
    /// </summary>
    public partial class RecipeEditorViewModel
    {
        private ICommand _addOptionCommand;
        private ICommand _newExtensionCommand;

        public ObservableCollection<RecipeOptionItemViewModel> RecipeOptions { get; } = new ObservableCollection<RecipeOptionItemViewModel>();

        public bool HasRecipeOptions => RecipeOptions.Count > 0;

        public ICommand AddOptionCommand => _addOptionCommand ??= new RelayCommand(AddOption);

        public ICommand NewExtensionCommand => _newExtensionCommand ??= new RelayCommand(NewExtension);

        private void LoadOptions(CaptureRecipe recipe)
        {
            RecipeOptions.Clear();
            foreach (var option in recipe?.Options?.Where(o => o != null) ?? Enumerable.Empty<RecipeOption>())
            {
                RecipeOptions.Add(new RecipeOptionItemViewModel(option, RemoveOption));
            }
            OnPropertyChanged(nameof(HasRecipeOptions));
        }

        public void AddOption()
        {
            if (ActiveRecipe == null) return;
            ActiveRecipe.Options ??= new List<RecipeOption>();
            string key = "option";
            for (int i = 2; ActiveRecipe.Options.Any(o => string.Equals(o?.Key, key, StringComparison.OrdinalIgnoreCase)); i++)
            {
                key = $"option{i}";
            }
            var option = new RecipeOption { Key = key, Type = ContractDataType.Boolean, DefaultValue = false, Label = "New option" };
            ActiveRecipe.Options.Add(option);
            RecipeOptions.Add(new RecipeOptionItemViewModel(option, RemoveOption));
            OnPropertyChanged(nameof(HasRecipeOptions));
            IsDirty = true;
            StatusMessage = $"Added option '{key}': the steps read it with ${{option.{key}}}";
        }

        private void RemoveOption(RecipeOptionItemViewModel item)
        {
            if (item == null || ActiveRecipe?.Options == null) return;
            ActiveRecipe.Options.Remove(item.Option);
            if (ActiveRecipe.Options.Count == 0)
            {
                ActiveRecipe.Options = null;
            }
            RecipeOptions.Remove(item);
            OnPropertyChanged(nameof(HasRecipeOptions));
            IsDirty = true;
            StatusMessage = $"Removed option '{item.Key}'";
        }
    }

    /// <summary>
    /// One option of the recipe in the editor
    /// </summary>
    public class RecipeOptionItemViewModel : ViewModelBase
    {
        public RecipeOptionItemViewModel(RecipeOption option, Action<RecipeOptionItemViewModel> onRemove)
        {
            Option = option ?? throw new ArgumentNullException(nameof(option));
            RemoveCommand = new RelayCommand(() => onRemove?.Invoke(this));
        }

        public RecipeOption Option { get; }

        public ICommand RemoveCommand { get; }

        private static readonly IReadOnlyList<string> AllTypeNames = RecipeOption.SupportedTypes.Select(t => t.ToString()).ToList();

        public IReadOnlyList<string> TypeNames => AllTypeNames;

        public string Key
        {
            get => Option.Key;
            set
            {
                Option.Key = value?.Trim();
                OnPropertyChanged();
                OnPropertyChanged(nameof(UsageText));
            }
        }

        public string Type
        {
            get => Option.Type.ToString();
            set
            {
                if (!Enum.TryParse<ContractDataType>(value, out var type) || type == Option.Type) return;
                Option.Type = type;
                // The default fits the new type, or it is the type's neutral value
                if (!Option.TryConvert(Option.DefaultValue, out _))
                {
                    Option.DefaultValue = null;
                    Option.DefaultValue = Option.GetDefaultValue();
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(DefaultText));
                OnPropertyChanged(nameof(IsNumber));
                OnPropertyChanged(nameof(IsEnum));
                OnPropertyChanged(nameof(CanBeQuickSetting));
            }
        }

        public bool IsNumber => Option.Type == ContractDataType.Integer || Option.Type == ContractDataType.Decimal;

        public bool IsEnum => Option.Type == ContractDataType.Enum;

        public bool CanBeQuickSetting => RecipeOption.QuickSettingsTypes.Contains(Option.Type);

        /// <summary>
        /// How a step uses it
        /// </summary>
        public string UsageText => $"${{option.{Option.Key}}}";

        public string DefaultText
        {
            get => Convert.ToString(Option.DefaultValue is Newtonsoft.Json.Linq.JValue j ? j.Value : Option.DefaultValue, CultureInfo.InvariantCulture);
            set
            {
                // Stored as the type when it fits, as text otherwise (the check reports it)
                Option.DefaultValue = Option.TryConvert(value, out var converted) ? converted : value;
                OnPropertyChanged();
            }
        }

        public string Label
        {
            get => Option.Label;
            set
            {
                Option.Label = string.IsNullOrWhiteSpace(value) ? null : value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => Option.Description;
            set
            {
                Option.Description = string.IsNullOrWhiteSpace(value) ? null : value;
                OnPropertyChanged();
            }
        }

        public string MinText
        {
            get => Option.Min?.ToString(CultureInfo.InvariantCulture);
            set
            {
                Option.Min = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var min) ? min : (decimal?)null;
                OnPropertyChanged();
            }
        }

        public string MaxText
        {
            get => Option.Max?.ToString(CultureInfo.InvariantCulture);
            set
            {
                Option.Max = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var max) ? max : (decimal?)null;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The choices of an Enum option, one per line: "value" or "value = label"
        /// </summary>
        public string ChoicesText
        {
            get => Option.Choices == null ? null : string.Join("\n", Option.Choices.Where(c => c != null).Select(c => string.IsNullOrWhiteSpace(c.Label) ? c.Value : $"{c.Value} = {c.Label}"));
            set
            {
                var choices = (value ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Split(new[] { '=' }, 2))
                    .Where(parts => !string.IsNullOrWhiteSpace(parts[0]))
                    .Select(parts => new RecipeOptionChoice { Value = parts[0].Trim(), Label = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : null })
                    .ToList();
                Option.Choices = choices.Count > 0 ? choices : null;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The key of a Boolean option this one depends on
        /// </summary>
        public string EnabledWhen
        {
            get => Option.EnabledWhen;
            set
            {
                Option.EnabledWhen = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                OnPropertyChanged();
            }
        }

        public bool QuickSettings
        {
            get => Option.QuickSettings;
            set
            {
                Option.QuickSettings = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A String option whose ${...} is evaluated where it is used
        /// </summary>
        public bool IsTemplate
        {
            get => string.Equals(Option.Format, RecipeOption.FormatTemplate, StringComparison.OrdinalIgnoreCase);
            set
            {
                Option.Format = value ? RecipeOption.FormatTemplate : null;
                OnPropertyChanged();
            }
        }
    }
}
