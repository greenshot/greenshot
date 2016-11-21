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
using System.Reflection;

#endregion

namespace Greenshot.Addon.Editor.Interfaces.Drawing
{
	/// <summary>
	///     Attribute for telling that a property is a field-property, meaning it is linked to a changable value for the editor
	/// </summary>
	[Serializable]
	[AttributeUsage(AttributeTargets.Property)]
	public class FieldAttribute : Attribute
	{
		[NonSerialized] private PropertyInfo _linkedProperty;

		private string _scope;

		public FieldAttribute(FieldTypes fieldType)
		{
			FieldType = fieldType;
		}

		/// <summary>
		///     FieldType of this FieldAttribute
		/// </summary>
		public FieldTypes FieldType { get; }

		/// <summary>
		///     This is used for caching the Reflection and should only be set from the InitializeFields
		/// </summary>
		public PropertyInfo LinkedProperty
		{
			private get { return _linkedProperty; }
			set { _linkedProperty = value; }
		}

		/// <summary>
		///     Return the type of the property this attribute is linked to.
		/// </summary>
		public Type PropertyType => LinkedProperty.PropertyType;

		/// <summary>
		///     Scope of the field, default is set to the Type of the class
		/// </summary>
		public string Scope
		{
			get { return _scope; }
			set { _scope = value; }
		}

		/// <summary>
		///     Get the field value from the IFieldHolder, this is needed for serialization
		/// </summary>
		/// <param name="target">IFieldHolder</param>
		/// <returns>value</returns>
		public object GetValue(IFieldHolder target)
		{
			Reflect(target.GetType());
			if (LinkedProperty.CanRead)
			{
				return LinkedProperty.GetValue(target, null);
			}
			return null;
		}

		/// <summary>
		///     Helper method to get the property info for this attribute
		/// </summary>
		/// <param name="typeForAttribute"></param>
		private void Reflect(Type typeForAttribute)
		{
			if (_linkedProperty == null)
			{
				foreach (var propertyInfo in typeForAttribute.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					foreach (var attribute in propertyInfo.GetCustomAttributes(true))
					{
						var fieldAttribute = attribute as FieldAttribute;
						if ((fieldAttribute != null) && (fieldAttribute.FieldType == FieldType))
						{
							_linkedProperty = propertyInfo;
							return;
						}
					}
				}
			}
		}

		/// <summary>
		///     Change the field value on the IFieldHolder
		/// </summary>
		/// <param name="target">IFieldHolder</param>
		/// <param name="value">value to set</param>
		public void SetValue(IFieldHolder target, object value)
		{
			Reflect(target.GetType());
			if (LinkedProperty.CanWrite)
			{
				LinkedProperty.SetValue(target, value, null);
			}
		}
	}
}