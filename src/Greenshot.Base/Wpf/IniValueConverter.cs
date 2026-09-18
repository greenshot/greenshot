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
using System.Globalization;
using System.Windows.Data;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Converter for IniValue to handle visibility based on IsExpert and expert mode setting
    /// </summary>
    public class ExpertVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue 
                    ? System.Windows.Visibility.Visible 
                    : System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to determine if control should be enabled (based on IsFixed/IsConstant in Dapplo.Ini).
    /// Disables controls (returns false) if the bound INI property is marked as constant.
    /// Supports:
    /// - Binding to an IIniSection with ConverterParameter="PropertyName"
    /// - MultiBinding with [0] = IIniSection, [1] = PropertyName
    /// - Direct boolean input (true => false, false => true)
    /// </summary>
    public class FixedToEnabledConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Dapplo.Ini.Interfaces.IIniSection section && parameter is string propertyName)
            {
                return !section.IsConstant(propertyName);
            }

            if (value is bool isFixed)
            {
                return !isFixed; // Enabled when not fixed
            }

            if (value != null && parameter is string propName)
            {
                // In case a ViewModel or wrapper object was passed
                var dotIndex = propName.IndexOf('.');
                if (dotIndex > 0)
                {
                    var sectionPropName = propName.Substring(0, dotIndex);
                    var actualPropName = propName.Substring(dotIndex + 1);
                    var sectionObj = value.GetType().GetProperty(sectionPropName)?.GetValue(value) as Dapplo.Ini.Interfaces.IIniSection;
                    if (sectionObj != null)
                    {
                        return !sectionObj.IsConstant(actualPropName);
                    }
                }
                else
                {
                    var coreProp = value.GetType().GetProperty("CoreConfiguration")?.GetValue(value) as Dapplo.Ini.Interfaces.IIniSection;
                    if (coreProp != null)
                    {
                        return !coreProp.IsConstant(propName);
                    }
                }
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
            {
                return true;
            }

            foreach (var val in values)
            {
                if (val is bool b && !b)
                {
                    return false;
                }
            }

            string propertyName = parameter as string;
            Dapplo.Ini.Interfaces.IIniSection section = null;

            foreach (var val in values)
            {
                if (val is Dapplo.Ini.Interfaces.IIniSection s)
                {
                    section = s;
                }
                else if (val is string str && propertyName == null)
                {
                    propertyName = str;
                }
            }

            if (section != null && !string.IsNullOrEmpty(propertyName))
            {
                return !section.IsConstant(propertyName);
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverse boolean converter
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}
