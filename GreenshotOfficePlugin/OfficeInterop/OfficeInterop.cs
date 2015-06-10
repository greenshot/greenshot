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

using System.Collections;
using Marshal = System.Runtime.InteropServices.Marshal;

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

	public static class ComHelper {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		public static void ReleaseObject<T>(ref T obj)
		{
			// Do not catch an exception from this.
			// You may want to remove these guards depending on
			// what you think the semantics should be.
			if (obj != null && Marshal.IsComObject(obj)) {
				Marshal.ReleaseComObject(obj);
			}
			// Since passed "by ref" this assingment will be useful
			// (It was not useful in the original, and neither was the GC.Collect.)
			obj = default(T);
		}
	}
}
