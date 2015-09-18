/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Dapplo.Config.Ini;
using Greenshot.Plugin.Drawing;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace GreenshotEditorPlugin.Drawing.Fields
{
    /// <summary>
    /// Basic IFieldHolder implementation, providing access to a set of fields
    /// </summary>
    [Serializable]
	public abstract class AbstractFieldHolder : IFieldHolder {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(AbstractFieldHolder));
		private static IEditorConfiguration editorConfiguration = IniConfig.Get("Greenshot","greenshot").Get<IEditorConfiguration>();

		protected IDictionary<FieldTypes, FieldAttribute> fieldAttributes = new Dictionary<FieldTypes, FieldAttribute>();
		public IDictionary<FieldTypes, FieldAttribute> FieldAttributes {
			get {
				return fieldAttributes;
			}
		}

		[NonSerialized]
		private PropertyChangedEventHandler propertyChanged;
		public event PropertyChangedEventHandler PropertyChanged {
			add {
				propertyChanged += value;
			}
			remove {
				propertyChanged -= value;
			}
		}

		/// <summary>
		/// Initializes all the fields in "this", using the editor configuration for caching and writing default values
		/// </summary>
		protected void InitFieldAttributes() {
			// Build cache if needed
			if (fieldAttributes.Count == 0) {
				foreach (var propertyInfo in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
					foreach (Attribute attribute in propertyInfo.GetCustomAttributes(true)) {
						var fieldAttribute = attribute as FieldAttribute;
						if (fieldAttribute != null) {
							if (fieldAttributes.ContainsKey(fieldAttribute.FieldType)) {
								throw new NotSupportedException(string.Format("Can't have two fields of type {0}", fieldAttribute.FieldType));
							}
							// Cache value (this is not directly needed, as get/set value will solve this, but is quicker!)
							fieldAttribute.LinkedProperty = propertyInfo;
							// Overwrite scope if it's not set
							if (fieldAttribute.Scope == null) {
								fieldAttribute.Scope = GetType().Name;
							}
							fieldAttributes.Add(fieldAttribute.FieldType, fieldAttribute);
						}
					}
				}
			}

			// Fill the attributes with their default or cached value
			foreach (FieldAttribute fieldAttribute in fieldAttributes.Values) {
				object defaultValue = fieldAttribute.GetValue(this);

				// TODO: Fix this
				//object fieldValue = editorConfiguration.CreateCachedValue(fieldAttribute.Scope, fieldAttribute.FieldType, defaultValue);
				//fieldAttribute.SetValue(this, fieldValue);
			}
		}

		/// <summary>
		/// Call this to propegate the changed event and update the editor configuration
		/// </summary>
		/// <param name="fieldType"></param>
		protected void OnFieldPropertyChanged(FieldTypes fieldType) {
			FieldAttribute fieldAttribute = FieldAttributes[fieldType];
			if (fieldAttribute != null) {
                // TODO: Implement
                //editorConfiguration.UpdateCachedValue(fieldAttribute.Scope, fieldType, fieldAttribute.GetValue(this));
            }
            OnPropertyChanged(fieldType.ToString());
		}

		/// <summary>
		/// Call this to send an PropertyChanged event
		/// </summary>
		/// <param name="propertyName"></param>
		protected void OnPropertyChanged(string propertyName) {
			if (propertyChanged != null) {
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
