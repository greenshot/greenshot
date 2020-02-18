/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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

namespace GreenshotOfficePlugin.OfficeInterop.OneNote {
	// More details about OneNote: http://msdn.microsoft.com/en-us/magazine/ff796230.aspx

	// See http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.applicationclass_members%28v=Office.11%29.aspx

    public class OneNotePage {
		public OneNoteSection Parent { get; set; }
		public string Name { get; set; }
		public string ID { get; set; }
		public bool IsCurrentlyViewed { get; set; }
		public string DisplayName {
			get {
				OneNoteNotebook notebook = Parent.Parent;
				if(string.IsNullOrEmpty(notebook.Name)) {
					return $"{Parent.Name} / {Name}";
				}

                return $"{Parent.Parent.Name} / {Parent.Name} / {Name}";
            }
		}
	}
}
