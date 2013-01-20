/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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

using GreenshotPlugin.Core;

namespace GreenshotConfluencePlugin {
	public class EnumDisplayer : IValueConverter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(EnumDisplayer));

		private Type type;
		private IDictionary displayValues;
		private IDictionary reverseValues;
		
		public EnumDisplayer() {
		}
	
		public EnumDisplayer(Type type) {
			this.Type = type;
		}
	
		public Type Type {
			get { return type; }
			set {
				if (!value.IsEnum) {
					throw new ArgumentException("parameter is not an Enumerated type", "value");
				}
				this.type = value;
			}
		}
		
		public ReadOnlyCollection<string> DisplayNames {
			get {
				this.reverseValues = (IDictionary) Activator.CreateInstance(typeof(Dictionary<,>).GetGenericTypeDefinition().MakeGenericType(typeof(string),type));
				
				this.displayValues = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).GetGenericTypeDefinition().MakeGenericType(type, typeof(string)));
				
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
				foreach (var field in fields) {
					DisplayKeyAttribute[] a = (DisplayKeyAttribute[])field.GetCustomAttributes(typeof(DisplayKeyAttribute), false);
				
					string displayKey = GetDisplayKeyValue(a);
					object enumValue = field.GetValue(null);
					
					string displayString = null;
					if (displayKey != null && Language.hasKey(displayKey)) {
						displayString = Language.GetString(displayKey);
					} if (displayKey != null) {
						displayString = displayKey;
					} else {
						displayString = enumValue.ToString();
					}
				
					if (displayString != null) {
						displayValues.Add(enumValue, displayString);
						reverseValues.Add(displayString, enumValue);
					}
				}
				return new List<string>((IEnumerable<string>)displayValues.Values).AsReadOnly();
			}
		}
		
		private string GetDisplayKeyValue(DisplayKeyAttribute[] a) {
			if (a == null || a.Length == 0) {
				return null;
			}
			DisplayKeyAttribute dka = a[0];
			return dka.Value;
		}
		
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return displayValues[value];
		}
		
		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return reverseValues[value];
		}
	}
}
