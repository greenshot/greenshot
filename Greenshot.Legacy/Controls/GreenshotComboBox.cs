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
using System.ComponentModel;
using System.Windows.Forms;
using Dapplo.Config.Language;
using Greenshot.Core.Extensions;

#endregion

namespace Greenshot.Legacy.Controls
{
	public class GreenshotComboBox : ComboBox, IGreenshotConfigBindable, IGreenshotLanguageBindable
	{
		private Type _enumType;
		private Enum _selectedEnum;

		public GreenshotComboBox()
		{
			SelectedIndexChanged += delegate { StoreSelectedEnum(); };
		}

		[Category("Greenshot")]
		[DefaultValue("Core")]
		[Description("Specifies the Ini-Section to map this control with.")]
		public string SectionName { get; set; } = "Core";

		[Category("Greenshot")]
		[DefaultValue(null)]
		[Description("Specifies the property name to map the configuration.")]
		public string PropertyName { get; set; }

		[Category("Greenshot")]
		[DefaultValue("Core")]
		[Description("Specifies module for the language file to use when displaying the translation.")]
		public string LanguageModule { get; set; }


		public string LanguageKey { get; set; }

		/// <summary>
		///     Get the selected enum value from the combobox, uses generics
		/// </summary>
		/// <returns>The enum value of the combobox</returns>
		public Enum GetSelectedEnum()
		{
			return _selectedEnum;
		}

		/// <summary>
		///     This is a method to popululate the ComboBox
		///     with the items from the enumeration
		/// </summary>
		/// <param name="enumType">TEnum to populate with</param>
		public void Populate(Type enumType)
		{
			// Store the enum-type, so we can work with it
			_enumType = enumType;

			var availableValues = Enum.GetValues(enumType);
			Items.Clear();
			string enumTypeName = enumType.Name;
			foreach (var enumValue in availableValues)
			{
				Items.Add(LanguageLoader.Current.Translate(enumValue, LanguageModule));
			}
		}

		public void SetValue(Enum currentValue)
		{
			if (currentValue != null)
			{
				_selectedEnum = currentValue;
				SelectedItem = LanguageLoader.Current.Translate(currentValue, LanguageModule);
			}
		}

		/// <summary>
		///     Store the selected value internally
		/// </summary>
		private void StoreSelectedEnum()
		{
			string enumTypeName = _enumType.Name;
			string selectedValue = SelectedItem as string;
			var availableValues = Enum.GetValues(_enumType);
			object returnValue = null;

			try
			{
				returnValue = Enum.Parse(_enumType, selectedValue);
			}
			catch (Exception)
			{
			}

			foreach (var enumValue in availableValues)
			{
				string enumKey = enumTypeName + "." + enumValue;
				string translation = LanguageLoader.Current.Translate(enumValue, LanguageModule);
				if (translation.Equals(selectedValue))
				{
					returnValue = enumValue;
				}
			}
			_selectedEnum = (Enum) returnValue;
		}
	}
}