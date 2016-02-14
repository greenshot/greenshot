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
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Dapplo.Config.Ini;
using Greenshot.Addon.Interfaces.Drawing;

namespace Greenshot.Addon.Editor.Drawing.Fields
{
	/// <summary>
	/// Basic IFieldHolder implementation, providing access to a set of fields
	/// </summary>
	[Serializable]
	public abstract class AbstractFieldHolder : IFieldHolder
	{
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(AbstractFieldHolder));
		private static IEditorConfiguration editorConfiguration = IniConfig.Current.Get<IEditorConfiguration>();

		protected IDictionary<FieldTypes, FieldAttribute> fieldAttributes = new Dictionary<FieldTypes, FieldAttribute>();

		/// <summary>
		/// Store the field attributes for this element
		/// </summary>
		public IDictionary<FieldTypes, FieldAttribute> FieldAttributes
		{
			get
			{
				return fieldAttributes;
			}
		}

		/// <summary>
		/// Get the flag of this element
		/// </summary>
		public ElementFlag Flag
		{
			get;
			private set;
		}

		[NonSerialized]
		private PropertyChangedEventHandler propertyChanged;

		public event PropertyChangedEventHandler PropertyChanged
		{
			add
			{
				propertyChanged += value;
			}
			remove
			{
				propertyChanged -= value;
			}
		}

		/// <summary>
		/// Initializes all the fields in "this", using the editor configuration for caching and writing default values
		/// </summary>
		protected void InitFieldAttributes()
		{
			var flagAttribute = GetType().GetCustomAttribute<FlagAttribute>();
			if (flagAttribute != null)
			{
				Flag = flagAttribute.Flag;
			}
			// Build cache if needed
			if (fieldAttributes.Count == 0)
			{
				foreach (var propertyInfo in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					foreach (Attribute attribute in propertyInfo.GetCustomAttributes(true))
					{
						var fieldAttribute = attribute as FieldAttribute;
						if (fieldAttribute != null)
						{
							if (fieldAttributes.ContainsKey(fieldAttribute.FieldType))
							{
								throw new NotSupportedException(string.Format("Can't have two fields of type {0}", fieldAttribute.FieldType));
							}
							// Cache value (this is not directly needed, as get/set value will solve this, but is quicker!)
							fieldAttribute.LinkedProperty = propertyInfo;
							// Overwrite scope if it's not set
							if (fieldAttribute.Scope == null)
							{
								fieldAttribute.Scope = GetType().Name;
							}
							fieldAttributes.Add(fieldAttribute.FieldType, fieldAttribute);
						}
					}
				}
			}

			// Fill the attributes with their default or cached value
			foreach (FieldAttribute fieldAttribute in fieldAttributes.Values)
			{
				object defaultValue = fieldAttribute.GetValue(this);
				if (fieldAttribute.FieldType != FieldTypes.COUNTER_START)
				{
					defaultValue = CreateCachedValue(fieldAttribute, defaultValue);
					fieldAttribute.SetValue(this, defaultValue);
				}
			}
		}

		/// <summary>
		/// Get the default value from the field, check if we have cached it and if not store it in the cache
		/// </summary>
		/// <param name="fieldAttribute"></param>
		/// <returns>object</returns>
		private object CreateCachedValue(FieldAttribute fieldAttribute, object defaultValue)
		{
			string key = string.Format("{0}-{1}", fieldAttribute.Scope, fieldAttribute.FieldType);
			var converter = TypeDescriptor.GetConverter(fieldAttribute.PropertyType);
			if (editorConfiguration.LastUsedFieldValues.ContainsKey(key))
			{
				string cachedValue = editorConfiguration.LastUsedFieldValues[key];
				if (converter.CanConvertFrom(typeof (string)))
				{
					defaultValue = converter.ConvertFromInvariantString(cachedValue);
				}
			}
			else
			{
				editorConfiguration.LastUsedFieldValues.Add(key, converter.ConvertToInvariantString(defaultValue));
			}
			return defaultValue;
		}

		/// <summary>
		/// Update the value in the cache
		/// </summary>
		/// <param name="fieldAttribute"></param>
		private void UpdateCachedValue(FieldAttribute fieldAttribute)
		{
			string key = string.Format("{0}-{1}", fieldAttribute.Scope, fieldAttribute.FieldType);
			if (editorConfiguration.LastUsedFieldValues.ContainsKey(key))
			{
				var converter = TypeDescriptor.GetConverter(fieldAttribute.PropertyType);
				editorConfiguration.LastUsedFieldValues[key] = converter.ConvertToInvariantString(fieldAttribute.GetValue(this));
			}
		}

		/// <summary>
		/// Call this to propegate the changed event and update the editor configuration
		/// </summary>
		/// <param name="fieldType"></param>
		protected void OnFieldPropertyChanged(FieldTypes fieldType)
		{
			FieldAttribute fieldAttribute = FieldAttributes[fieldType];
			if (fieldAttribute != null && fieldAttribute.FieldType != FieldTypes.COUNTER_START)
			{
				UpdateCachedValue(fieldAttribute);
			}
			OnPropertyChanged(fieldType.ToString());
		}

		/// <summary>
		/// Call this to send an PropertyChanged event
		/// </summary>
		/// <param name="propertyName"></param>
		protected void OnPropertyChanged(string propertyName)
		{
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
			Invalidate();
		}

		/// <summary>
		/// Every field holder should invalidate when a PropertyChanged event is generated, therefor it needs to implement this
		/// </summary>
		public abstract void Invalidate();
	}
}