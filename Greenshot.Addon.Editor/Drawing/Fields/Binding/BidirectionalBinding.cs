/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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

using System.ComponentModel;
using System.Reflection;
using Greenshot.Addon.Interfaces.Drawing;

namespace Greenshot.Addon.Editor.Drawing.Fields.Binding
{
	/// <summary>
	/// Bidirectional binding of properties of two INotifyPropertyChanged instances.
	/// This implementation synchronizes null values, too. If you do not want this
	/// behavior (e.g. when binding to a 
	/// </summary>
	public class BidirectionalBinding
	{
		private readonly INotifyPropertyChanged _controlObject;
		private readonly Surface _surface;
		private readonly string _controlPropertyName;
		private readonly FieldTypes _fieldType;
		private bool _updatingControl;
		private IBindingConverter _converter;
		private IBindingValidator validator;
		private readonly PropertyInfo _controlValuePropertyInfo;
		private readonly PropertyInfo _controlVisiblePropertyInfo;

		/// <summary>
		/// Whether or not null values are passed on to the other object.
		/// </summary>
		protected bool AllowSynchronizeNull = true;

		/// <summary>
		/// Bind properties of two objects bidirectionally
		/// </summary>
		/// <param name="controlObject">Object containing 1st property to bind</param>
		/// <param name="controlPropertyName">Property of 1st object to bind</param>
		/// <param name="surface"></param>
		/// <param name="fieldType"></param>
		public BidirectionalBinding(INotifyPropertyChanged controlObject, string controlPropertyName, Surface surface, FieldTypes fieldType)
		{
			_controlObject = controlObject;
			_controlPropertyName = controlPropertyName;
			_controlValuePropertyInfo = resolvePropertyInfo(controlObject, controlPropertyName);
			_controlVisiblePropertyInfo = resolvePropertyInfo(controlObject, "Visible");
			_surface = surface;
			_fieldType = fieldType;

			_controlObject.PropertyChanged += ControlPropertyChanged;
		}

		/// <summary>
		/// Bind properties of two objects bidirectionally, converting the values using a converter
		/// </summary>
		/// <param name="controlObject">Object containing 1st property to bind</param>
		/// <param name="controlPropertyName">Property of 1st object to bind</param>
		/// <param name="surface"></param>
		/// <param name="fieldType"></param>
		/// <param name="converter">taking care of converting the synchronzied value to the correct target format and back</param>
		public BidirectionalBinding(INotifyPropertyChanged controlObject, string controlPropertyName, Surface surface, FieldTypes fieldType, IBindingConverter converter) : this(controlObject, controlPropertyName, surface, fieldType)
		{
			_converter = converter;
		}

		/// <summary>
		/// Bind properties of two objects bidirectionally, converting the values using a converter.
		/// Synchronization can be intercepted by adding a validator.
		/// </summary>
		/// <param name="controlObject">Object containing 1st property to bind</param>
		/// <param name="controlPropertyName">Property of 1st object to bind</param>
		/// <param name="surface"></param>
		/// <param name="fieldType"></param>
		/// <param name="validator">validator to intercept synchronisation if the value does not match certain criteria</param>
		public BidirectionalBinding(INotifyPropertyChanged controlObject, string controlPropertyName, Surface surface, FieldTypes fieldType, IBindingValidator validator) : this(controlObject, controlPropertyName, surface, fieldType)
		{
			this.validator = validator;
		}

		/// <summary>
		/// Bind properties of two objects bidirectionally, converting the values using a converter.
		/// Synchronization can be intercepted by adding a validator.
		/// </summary>
		/// <param name="controlObject">Object containing 1st property to bind</param>
		/// <param name="controlPropertyName">Property of 1st object to bind</param>
		/// <param name="surface"></param>
		/// <param name="fieldType"></param>
		/// <param name="converter">taking care of converting the synchronzied value to the correct target format and back</param>
		/// <param name="validator">validator to intercept synchronisation if the value does not match certain criteria</param>
		public BidirectionalBinding(INotifyPropertyChanged controlObject, string controlPropertyName, Surface surface, FieldTypes fieldType, IBindingConverter converter, IBindingValidator validator) : this(controlObject, controlPropertyName, surface, fieldType, converter)
		{
			this.validator = validator;
		}

		public void ControlPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (!_updatingControl && e.PropertyName.Equals(_controlPropertyName))
			{
				UpdateFields();
			}
		}

		public void Refresh()
		{
			// Set control visibilty
			bool visible = _surface.IsElementWithFieldTypeSelected(_fieldType);
			_controlVisiblePropertyInfo.SetValue(_controlObject, visible, null);

			// No need to update when the control is not visible
			if (visible)
			{
				_updatingControl = true;
				foreach (IFieldHolder fieldHolder in _surface.ReturnSelectedElementsWithFieldType(_fieldType))
				{
					FieldAttribute attribute = fieldHolder.FieldAttributes[_fieldType];
					if (attribute != null)
					{
						object currentValue = attribute.GetValue(fieldHolder);
						if (_converter != null && currentValue != null)
						{
							currentValue = _converter.convert(currentValue);
						}
						_controlValuePropertyInfo.SetValue(_controlObject, currentValue, null);
					}
				}
				_updatingControl = false;
			}
		}

		private void UpdateFields()
		{
			object bValue = _controlValuePropertyInfo.GetValue(_controlObject, null);
			if (_converter != null && bValue != null)
			{
				bValue = _converter.convert(bValue);
			}
			_surface.ChangeFields(_fieldType, bValue);
		}

		private PropertyInfo resolvePropertyInfo(object obj, string property)
		{
			return obj.GetType().GetProperty(property);
		}

		public IBindingConverter Converter
		{
			get
			{
				return _converter;
			}
			set
			{
				_converter = value;
			}
		}
	}
}