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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// One option in the settings, the template is picked by its type
    /// </summary>
    public class RecipeOptionViewModel : INotifyPropertyChanged
    {
        private object _value;
        private RecipeOptionViewModel _switch;

        public RecipeOptionViewModel(RecipeOption option, object value)
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
        public RecipeOptionViewModel Switch
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
}
