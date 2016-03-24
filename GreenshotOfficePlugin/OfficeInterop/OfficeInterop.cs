/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using System.Collections;

namespace Greenshot.Interop.Office {
	public enum OfficeVersion : int {
		OFFICE_97 = 8,
		OFFICE_2000 = 9,
		OFFICE_2002 = 10,
		OFFICE_2003 = 11,
		OFFICE_2007 = 12,
		OFFICE_2010 = 14,
		OFFICE_2013 = 15
	}

	/// <summary>
	/// If the "type" this[object index] { get; } is implemented, use index + 1!!! (starts at 1)
	/// </summary>
	public interface ICollection : ICommon, IEnumerable {
		int Count {
			get;
		}
		void Remove(int index);
	}
}
