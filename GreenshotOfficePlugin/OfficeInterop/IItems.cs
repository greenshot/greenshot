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

#region Usings

using System.Collections;

#endregion

namespace GreenshotOfficePlugin.OfficeInterop
{
	/// <summary>
	///     See: http://msdn.microsoft.com/en-us/library/bb208387%28v=office.12%29.aspx
	/// </summary>
	public interface IItems : IComCollection, IEnumerable
	{
		IItem this[object index] { get; }

		bool IncludeRecurrences { get; set; }

		IItem GetFirst();
		IItem GetNext();
		IItem GetLast();
		IItem GetPrevious();

		IItems Restrict(string filter);
		void Sort(string property, object descending);

		// Actual definition is "object Add( object )", just making it convenient
		object Add(OlItemType type);
	}
}