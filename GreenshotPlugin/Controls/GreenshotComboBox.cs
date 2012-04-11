/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Windows.Forms;
using GreenshotPlugin.Core;

namespace GreenshotPlugin.Controls {
	public class GreenshotComboBox : ComboBox, IGreenshotConfigBindable {
		private string sectionName = "Core";
		[Category("Greenshot"), DefaultValue("Core"), Description("Specifies the Ini-Section to map this control with.")]
		public string SectionName {
			get {
				return sectionName;
			}
			set {
				sectionName = value;
			}
		}

		[Category("Greenshot"), DefaultValue(null), Description("Specifies the property name to map the configuration.")]
		public string PropertyName {
			get;
			set;
		}

		/// <summary>
		/// This is a method to popululate the ComboBox
		/// with the items from the enumeration
		/// </summary>
		/// <param name="enumType">TEnum to populate with</param>
		public void Populate(ILanguage language, Type enumType, object currentValue) {
			var availableValues = Enum.GetValues(enumType);
			this.Items.Clear();
			string enumTypeName = enumType.Name;
			foreach (var enumValue in availableValues) {
				string enumKey = enumTypeName + "." + enumValue.ToString();
				if (language.hasKey(enumKey)) {
					string translation = language.GetString(enumKey);
					this.Items.Add(translation);
				} else {
					this.Items.Add(enumValue.ToString());
				}
			}
			if (currentValue != null) {
				string selectedEnumKey = enumTypeName + "." + currentValue.ToString();
				if (language.hasKey(selectedEnumKey)) {
					this.SelectedItem = language.GetString(selectedEnumKey);
				} else {
					this.SelectedItem = currentValue.ToString();
				}
			}
		}


		/// <summary>
		/// Get the selected enum value from the combobox, uses generics
		/// </summary>
		/// <param name="comboBox">Combobox to get the value from</param>
		/// <returns>The generics value of the combobox</returns>
		public object GetSelectedEnum(ILanguage language, Type enumType) {
			string enumTypeName = enumType.Name;
			string selectedValue = this.SelectedItem as string;
			var availableValues = Enum.GetValues(enumType);
			object returnValue = null;

			try {
				returnValue = Enum.Parse(enumType, selectedValue);
				return returnValue;
			} catch (Exception) {
			}

			foreach (Enum enumValue in availableValues) {
				string enumKey = enumTypeName + "." + enumValue.ToString();
				if (language.hasKey(enumKey)) {
					string translation = language.GetString(enumTypeName + "." + enumValue.ToString());
					if (translation.Equals(selectedValue)) {
						return enumValue;
					}
				}
			}
			return returnValue;
		}
	}
}
