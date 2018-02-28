#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using Dapplo.Windows.Com;

#endregion

namespace GreenshotOfficePlugin.OfficeInterop
{
	/// <summary>
	///     More details about OneNote: http://msdn.microsoft.com/en-us/magazine/ff796230.aspx
	///     See
	///     http://msdn.microsoft.com/en-US/library/microsoft.office.interop.word.applicationclass_members%28v=Office.11%29.aspx
	/// </summary>
	[ComProgId("OneNote.Application")]
	public interface IOneNoteApplication : IComCommon
	{
		/// <summary>
		///     Make sure that the out variables are filled with a string, e.g. "", otherwise a type error occurs.
		///     For more info on the methods: http://msdn.microsoft.com/en-us/library/gg649853.aspx
		/// </summary>
		void GetHierarchy(string startNode, HierarchyScope scope, out string notebookXml, XMLSchema schema);

		void GetSpecialLocation(SpecialLocation specialLocation, out string specialLocationPath);
		void UpdatePageContent(string pageChangesXml, DateTime dateExpectedLastModified, XMLSchema schema, bool force);
		void GetPageContent(string pageId, out string pageXml, PageInfo pageInfoToExport, XMLSchema schema);
		void NavigateTo(string hierarchyObjectID, string objectId, bool newWindow);
		void CreateNewPage(string sectionID, out string pageID, NewPageStyle newPageStyle);
	}
}