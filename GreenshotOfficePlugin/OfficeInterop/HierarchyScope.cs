#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

namespace GreenshotOfficePlugin.OfficeInterop
{
	public enum HierarchyScope
	{
		hsSelf = 0, // Gets just the start node specified and no descendants.
		hsChildren = 1, //Gets the immediate child nodes of the start node, and no descendants in higher or lower subsection groups.
		hsNotebooks = 2, // Gets all notebooks below the start node, or root.
		hsSections = 3, //Gets all sections below the start node, including sections in section groups and subsection groups.
		hsPages = 4 //Gets all pages below the start node, including all pages in section groups and subsection groups.
	}
}