/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Greenshot.Helpers {
	public class Objects {
		private Objects() {
		}
		
		/// <param name="source">object to clone</param>
		/// <returns>a deep clone of source object</returns>
		public static object DeepClone(object source) {
			MemoryStream m = new MemoryStream();
			BinaryFormatter b = new BinaryFormatter();
			b.Serialize(m, source);
			m.Position = 0;
			return b.Deserialize(m);
		}
	}
}
