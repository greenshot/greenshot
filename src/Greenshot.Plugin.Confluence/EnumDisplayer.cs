/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using Greenshot.Base.Core;

namespace Greenshot.Plugin.Confluence
{
    public class EnumDisplayer : IValueConverter
    {
        private Type _type;
        private IDictionary _displayValues;
        private IDictionary _reverseValues;

        public Type Type
        {
            get { return _type; }
            set
            {
                if (!value.IsEnum)
                {
                    throw new ArgumentException("parameter is not an Enumerated type", nameof(value));
                }

                _type = value;
            }
        }

        public ReadOnlyCollection<string> DisplayNames
        {
            get
            {
                var genericTypeDefinition = typeof(Dictionary<,>).GetGenericTypeDefinition();
                if (genericTypeDefinition != null)
                {
                    _reverseValues = (IDictionary) Activator.CreateInstance(genericTypeDefinition.MakeGenericType(typeof(string), _type));
                }

                var typeDefinition = typeof(Dictionary<,>).GetGenericTypeDefinition();
                if (typeDefinition != null)
                {
                    _displayValues = (IDictionary) Activator.CreateInstance(typeDefinition.MakeGenericType(_type, typeof(string)));
                }

                var fields = _type.GetFields(BindingFlags.Public | BindingFlags.Static);
                foreach (var fieldInfo in fields)
                {
                    DisplayKeyAttribute[] a = (DisplayKeyAttribute[])fieldInfo.GetCustomAttributes(typeof(DisplayKeyAttribute), false);

                    string displayKey = GetDisplayKeyValue(a);
                    object enumValue = fieldInfo.GetValue(null);

                    string displayString;
                    if (displayKey != null && Language.HasKey(displayKey))
                    {
                        displayString = Language.GetString(displayKey);
                    }

                    displayString = displayKey ?? enumValue.ToString();

                    _displayValues.Add(enumValue, displayString);
                    _reverseValues.Add(displayString, enumValue);
                }

                return new List<string>((IEnumerable<string>) _displayValues.Values).AsReadOnly();
            }
        }

        private static string GetDisplayKeyValue(DisplayKeyAttribute[] a)
        {
            if (a == null || a.Length == 0)
            {
                return null;
            }

            DisplayKeyAttribute dka = a[0];
            return dka.Value;
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _displayValues[value];
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _reverseValues[value];
        }
    }
}