/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Collections.Generic;
using Greenshot.Configuration;

namespace Greenshot.Drawing.Fields {
	
	/// <summary>
	/// In charge for creating Fields for DrawableContainers or Filters.
	/// Add new FieldTypes in FieldType.cs
	/// </summary>
	public class FieldFactory {
		
		private FieldFactory() {}
		
		/// <param name="fieldType">FieldType of the field to construct</param>
		/// <returns>a new Field of the given fieldType</returns>
		public static Field CreateField(FieldType fieldType) {
			return CreateField(fieldType, null, null);
		}
		
		/// <param name="fieldType">FieldType of the field to construct</param>
		/// <param name="preferredDefaultValue">overwrites the original default value being defined in FieldType</param>
		/// <returns>a new Field of the given fieldType and initialValue</returns>
		public static Field CreateField(FieldType fieldType, object preferredDefaultValue) {
			return CreateField(fieldType, null, preferredDefaultValue);
		}
		
		/// <param name="fieldType">FieldType of the field to construct</param>
		/// <param name="forcedValue">the value to be inserted, regardless of last used value or default value for this field</param>
		/// <returns>a new Field of the given fieldType and initialValue</returns>
		public static Field CreateFieldWithValue(FieldType fieldType, object forcedValue) {
			Field ret = new Field(fieldType);
			ret.Value = forcedValue;
			return ret;
			
		}

		/// <param name="fieldType">FieldType of the field to construct</param>
		/// <param name="scope">FieldType of the field to construct</param>
		/// <returns>a new Field of the given fieldType, with the scope of it's value being restricted to the Type scope</returns>
		public static Field CreateField(FieldType fieldType, Type scope) {
			return CreateField(fieldType, scope, null);
		}
		
		/// <param name="fieldType">FieldType of the field to construct</param>
		/// <param name="preferredDefaultValue">overwrites the original default value being defined in FieldType</param>
		/// <returns>a new Field of the given fieldType and preferredDefaultValue, with the scope of it's value being restricted to the Type scope</returns>
		public static Field CreateField(FieldType fieldType, Type scope, object preferredDefaultValue) {
			Field ret = null;
			if (scope != null) {
				ret = new Field(fieldType, scope);
			} else {
				ret = new Field(fieldType);
			}
			AppConfig.GetInstance().GetLastUsedValueForField(ret, preferredDefaultValue);
			return ret;
		}
		
		/// <returns>a List of all available fields with their respective default value</returns>
		public static List<Field> GetDefaultFields() {
			List<Field> ret = new List<Field>();
			foreach(FieldType ft in FieldType.Values) {
				Field f = CreateField(ft);
				f.Value = ft.DefaultValue;
				ret.Add(f);
			}
			return ret;
		}
	}
	
}
