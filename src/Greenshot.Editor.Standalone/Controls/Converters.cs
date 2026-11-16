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
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Greenshot.Editor.Standalone.Controls
{
    /// <summary>
    /// The translated name of an enum value the panel offers (arrow heads, modes, edges)
    /// </summary>
    public sealed class EnumNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => EditorTexts.NameOf(value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => BindingOperations.DoNothing;
    }

    /// <summary>
    /// True when the value is the parameter: for a group of radio buttons bound to one enum property
    /// </summary>
    public sealed class EnumEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value != null && parameter != null && string.Equals(value.ToString(), parameter.ToString(), StringComparison.Ordinal);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is true && parameter != null && targetType.IsEnum ? Enum.Parse(targetType, parameter.ToString()) : BindingOperations.DoNothing;
    }

    /// <summary>
    /// A number of a view model (double or int) for a NumericUpDown, which works with decimal
    /// </summary>
    public sealed class NumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is IConvertible number ? Math.Round(number.ToDecimal(CultureInfo.InvariantCulture), 2) : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not decimal number)
            {
                return BindingOperations.DoNothing;
            }

            var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return type == typeof(int) ? (int)Math.Round(number) : System.Convert.ChangeType(number, type, CultureInfo.InvariantCulture);
        }
    }
}
