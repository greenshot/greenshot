//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using Dapplo.Config.Language;
using Dapplo.Log;
using Greenshot.Addon.Extensions;
using Greenshot.Core.Extensions;

#endregion

namespace Greenshot.Addon.Confluence
{
	/// <summary>
	///     Use this to make it possible to display translated enum values.
	///     Start with:
	///     <code>
	/// &lt;confluence:EnumDisplayer Type="{x:Type gsconfig:OutputFormat}" LanguageModule="Core" x:Key="outputFormats" /&gt;
	/// </code>
	///     A combobox could have:
	///     <code>
	/// &lt;ComboBox ItemsSource="{Binding Source={StaticResource outputFormats},Path=DisplayNames}" SelectedValue="{Binding UploadFormat, Converter={StaticResource outputFormats}}" /&gt;
	/// </code>
	/// </summary>
	public class EnumDisplayer : IValueConverter
	{
		private static readonly LogSource Log = new LogSource();
		private readonly IDictionary<object, string> _displayValues = new Dictionary<object, string>();
		private readonly IDictionary<string, object> _reverseValues = new Dictionary<string, object>();

		private Type _type;

		public EnumDisplayer()
		{
		}

		public EnumDisplayer(Type type)
		{
			if (!type.IsEnum)
			{
				throw new ArgumentException("Not an enum", nameof(type));
			}
			Type = type;
		}

		public ReadOnlyCollection<string> DisplayNames
		{
			get
			{
				var fields = _type.GetFields(BindingFlags.Public | BindingFlags.Static);
				foreach (var field in fields)
				{
					var displayAttributes = (DisplayAttribute[]) field.GetCustomAttributes(typeof(DisplayAttribute), false);

					string displayKey = GetDisplayKeyValue(displayAttributes);
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
				return new List<string>(_displayValues.Values).AsReadOnly();
			}
		}

		/// <summary>
		///     Specify from what language module the translation needs to be retrieved
		/// </summary>
		public string LanguageModule { get; set; } = "Core";

		/// <summary>
		///     Specify the Type of the enum
		/// </summary>
		public Type Type
		{
			get { return _type; }
			set
			{
				if (!value.IsEnum)
				{
					throw new ArgumentException("parameter is not an Enumerated type", "value");
				}
				_type = value;
			}
		}

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var key = value as string;
			return _displayValues.ContainsKey(key) ? _displayValues[key] : null;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var key = value as string;
			return _reverseValues.ContainsKey(key) ? _reverseValues[key] : null;
		}

		private string GetDisplayKeyValue(DisplayAttribute[] displayKeyAttributes)
		{
			if ((displayKeyAttributes == null) || (displayKeyAttributes.Length == 0))
			{
				return null;
			}
			return displayKeyAttributes[0].GetName();
		}
	}
}