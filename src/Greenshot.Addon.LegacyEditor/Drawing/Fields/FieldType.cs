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
using Greenshot.Addons.Interfaces.Drawing;

namespace Greenshot.Addon.LegacyEditor.Drawing.Fields
{
	/// <summary>
	///     Defines all FieldTypes + their default value.
	///     (The additional value is why this is not an enum)
	/// </summary>
	[Serializable]
	public class FieldType<T> : IFieldType
	{
	    internal FieldType(string name)
		{
		    Name = name;
		}

		public string Name { get; }

	    /// <summary>
	    /// This returns the type which the value has
	    /// </summary>
	    public Type ValueType { get; }= typeof(T);

        /// <inheritdoc />
        public override string ToString()
		{
			return Name;
		}

        /// <inheritdoc />
        public override int GetHashCode()
		{
			var hashCode = 0;
			unchecked
			{
				if (Name != null)
				{
					hashCode += 1000000009 * Name.GetHashCode();
				}
			}
			return hashCode;
		}

        /// <inheritdoc />
        public override bool Equals(object obj)
		{
			var other = obj as FieldType<T>;
			if (other == null)
			{
				return false;
			}
			return Equals(Name, other.Name);
		}

        /// <summary>
        /// Implements a equals operator
        /// </summary>
        /// <param name="a">FieldType</param>
        /// <param name="b">FieldType</param>
        /// <returns>bool</returns>
        public static bool operator ==(FieldType<T> a, FieldType<T> b)
		{
			return Equals(a, b);
		}

        /// <summary>
        /// Implements a not equals operator
        /// </summary>
        /// <param name="a">FieldType</param>
        /// <param name="b">FieldType</param>
        /// <returns>bool</returns>
        public static bool operator !=(FieldType<T> a, FieldType<T> b)
		{
			return !Equals(a, b);
		}
	}
}