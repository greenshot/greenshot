/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using Dapplo.Config.Language;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;

namespace Greenshot.Addon.Confluence
{
	public class EnumDisplayer : IValueConverter
	{
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(EnumDisplayer));

		private Type _type;
		private IDictionary _displayValues;
		private IDictionary _reverseValues;

		public EnumDisplayer()
		{
		}

		public EnumDisplayer(Type type)
		{
			Type = type;
		}

		/// <summary>
		/// Specify the Type of the enum
		/// </summary>
		public Type Type
		{
			get
			{
				return _type;
			}
			set
			{
				if (!value.IsEnum)
				{
					throw new ArgumentException("parameter is not an Enumerated type", "value");
				}
				_type = value;
			}
		}

		/// <summary>
		/// Specify from what language module the translation needs to be retrieved
		/// </summary>
		public string LanguageModule
		{
			get;
			set;
		} = "Core";

		public ReadOnlyCollection<string> DisplayNames
		{
			get
			{
				_reverseValues = (IDictionary) Activator.CreateInstance(typeof (Dictionary<,>).GetGenericTypeDefinition().MakeGenericType(typeof (string), _type));

				_displayValues = (IDictionary) Activator.CreateInstance(typeof (Dictionary<,>).GetGenericTypeDefinition().MakeGenericType(_type, typeof (string)));

				var fields = _type.GetFields(BindingFlags.Public | BindingFlags.Static);
				foreach (var field in fields)
				{
					DisplayKeyAttribute[] displayKeyAttributes = (DisplayKeyAttribute[]) field.GetCustomAttributes(typeof (DisplayKeyAttribute), false);

					string displayKey = GetDisplayKeyValue(displayKeyAttributes);
					object enumValue = field.GetValue(null);

					string displayString = LanguageLoader.Current.Translate(displayKey, LanguageModule);
					if (string.IsNullOrEmpty(displayKey))
					{
						displayString = displayKey;
					}
					else
					{
						displayString = enumValue.ToString();
					}

					if (displayString != null)
					{
						_displayValues.Add(enumValue, displayString);
						_reverseValues.Add(displayString, enumValue);
					}
				}
				return new List<string>((IEnumerable<string>) _displayValues.Values).AsReadOnly();
			}
		}

		private string GetDisplayKeyValue(DisplayKeyAttribute[] displayKeyAttributes)
		{
			if (displayKeyAttributes == null || displayKeyAttributes.Length == 0)
			{
				return null;
			}
			return displayKeyAttributes[0].Value;
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