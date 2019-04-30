// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Dapplo.Config.Language;

namespace Greenshot.Addons.Controls
{
    /// <summary>
    /// A special combox box which can show a list of translated enum values
    /// </summary>
	public class GreenshotComboBox : ComboBox, IGreenshotConfigBindable
	{
	    private readonly ILanguage _language;
	    private Type _enumType;
		private Enum _selectedEnum;

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="language"></param>
		public GreenshotComboBox(ILanguage language)
		{
		    _language = language;
		    SelectedIndexChanged += (sender, args) => StoreSelectedEnum();
		}

        /// <inheritdoc />
        [Category("Greenshot")]
		[DefaultValue("Core")]
		[Description("Specifies the Ini-Section to map this control with.")]
		public string SectionName { get; set; } = "Core";

        /// <inheritdoc />
        [Category("Greenshot")]
		[DefaultValue(null)]
		[Description("Specifies the property name to map the configuration.")]
		public string PropertyName { get; set; }

        /// <summary>
        /// Set the value
        /// </summary>
        /// <param name="currentValue">Enum</param>
        public void SetValue(Enum currentValue)
		{
		    if (currentValue == null)
		    {
		        return;
		    }

		    _selectedEnum = currentValue;

		    var key = ToKey(currentValue);
		    if (_language.Keys().Contains(key))
		    {
		        SelectedItem = _language[key];
		    }
		    else
		    {
		        SelectedItem = currentValue.ToString();
		    }
		}

		/// <summary>
		///     This is a method to populate the ComboBox
		///     with the items from the enumeration
		/// </summary>
		/// <param name="enumType">TEnum to populate with</param>
		public void Populate(Type enumType)
		{
			// Store the enum-type, so we can work with it
			_enumType = enumType;

			var availableValues = Enum.GetValues(enumType);
			Items.Clear();
			foreach (var enumValue in availableValues)
			{
			    var key = ToKey(enumValue);
			    if (_language.Keys().Contains(key))
			    {
			        Items.Add(_language[key]);
                }
			    else
			    {
			        Items.Add(key);
                }
			}
		}

	    private string ToKey(object enumValue)
	    {
	        var typename = enumValue.GetType().Name;
	        return typename + "." + enumValue;
        }

		/// <summary>
		///     Store the selected value internally
		/// </summary>
		private void StoreSelectedEnum()
		{
			var selectedValue = SelectedItem as string;
			var availableValues = Enum.GetValues(_enumType);
			object returnValue = null;

			try
			{
				returnValue = Enum.Parse(_enumType, selectedValue);
			}
			catch (Exception)
			{
				// Ignore
			}

			foreach (Enum enumValue in availableValues)
			{
			    var key = enumValue.ToString();

                if (!_language.Keys().Contains(key))
			    {
			        continue;
			    }

			    var translation = _language[key];
			    if (translation.Equals(selectedValue))
			    {
			        returnValue = enumValue;
			    }
			}
			_selectedEnum = (Enum) returnValue;
		}

		/// <summary>
		///     Get the selected enum value from the combobox, uses generics
		/// </summary>
		/// <returns>The enum value of the combobox</returns>
		public Enum GetSelectedEnum()
		{
			return _selectedEnum;
		}
	}
}