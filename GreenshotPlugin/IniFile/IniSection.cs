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
using System.Collections.Generic;

namespace Greenshot.IniFile {
	/// <summary>
	/// Base class for all IniSections
	/// </summary>
	[Serializable]
	public abstract class IniSection {
		[NonSerialized]
		private IDictionary<string, IniValue> values = new Dictionary<string, IniValue>();

		public IDictionary<string, IniValue> Values {
			get {
				return values;
			}
		}

		/// Flag to specify if values have been changed
		public bool IsDirty = false;

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public virtual object GetDefault(string property) {
			return null;
		}

		/// <summary>
		/// This method will be called before converting the property, making to possible to correct a certain value
		/// Can be used when migration is needed
		/// </summary>
		/// <param name="propertyName">The name of the property</param>
		/// <param name="propertyValue">The string value of the property</param>
		/// <returns>string with the propertyValue, modified or not...</returns>
		public virtual string PreCheckValue(string propertyName, string propertyValue) {
			return propertyValue;
		}

		/// <summary>
		/// This method will be called after reading the configuration, so eventually some corrections can be made
		/// </summary>
		public virtual void AfterLoad() {
		}

		public virtual void BeforeSave() {
		}

		public virtual void AfterSave() {
		}
	}
}
