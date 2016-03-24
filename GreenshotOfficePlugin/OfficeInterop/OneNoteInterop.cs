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

using System;

namespace Greenshot.Interop.Office {
	// More details about OneNote: http://msdn.microsoft.com/en-us/magazine/ff796230.aspx

	// See http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.applicationclass_members%28v=Office.11%29.aspx
	[ComProgId("OneNote.Application")]
	public interface IOneNoteApplication : ICommon {
		/// <summary>
		/// Make sure that the out variables are filled with a string, e.g. "", otherwise a type error occurs.
		/// For more info on the methods: http://msdn.microsoft.com/en-us/library/gg649853.aspx
		/// </summary>
		void GetHierarchy(string startNode, HierarchyScope scope, out string notebookXml, XMLSchema schema);
		void GetSpecialLocation(SpecialLocation specialLocation, out string specialLocationPath);
		void UpdatePageContent(string pageChangesXml, DateTime dateExpectedLastModified, XMLSchema schema, bool force);
		void GetPageContent(string pageId, out string pageXml, PageInfo pageInfoToExport, XMLSchema schema);
		void NavigateTo(string hierarchyObjectID, string objectId, bool newWindow);
		void CreateNewPage(string sectionID, out string pageID, NewPageStyle newPageStyle);
	}

	public enum PageInfo {
		piBasic = 0,				// Returns only basic page content, without selection markup and binary data objects. This is the standard value to pass.
		piBinaryData = 1,			// Returns page content with no selection markup, but with all binary data.
		piSelection = 2,			// Returns page content with selection markup, but no binary data.
		piBinaryDataSelection = 3,	// Returns page content with selection markup and all binary data.
		piAll = 3					// Returns all page content.
	}

	public enum HierarchyScope {
		hsSelf = 0, // Gets just the start node specified and no descendants.
		hsChildren = 1, //Gets the immediate child nodes of the start node, and no descendants in higher or lower subsection groups.
		hsNotebooks = 2, // Gets all notebooks below the start node, or root.
		hsSections = 3, //Gets all sections below the start node, including sections in section groups and subsection groups.
		hsPages = 4 //Gets all pages below the start node, including all pages in section groups and subsection groups.
	}

	public enum XMLSchema {
		xs2007	= 0,
		xs2010	= 1,
		xsCurrent= xs2010
	}

	public enum SpecialLocation : int {
		slBackupFolder = 0,
		slUnfiledNotesSection = 1,
		slDefaultNotebookFolder = 2
	}

	public enum NewPageStyle {
		npsDefault = 0,
		npsBlankPageWithTitle = 1,
		npsBlankPageNoTitle = 2
	}

	public class OneNotePage {
		public OneNoteSection Parent { get; set; }
		public string Name { get; set; }
		public string ID { get; set; }
		public bool IsCurrentlyViewed { get; set; }
		public string DisplayName {
			get {
				OneNoteNotebook notebook = Parent.Parent;
				if(string.IsNullOrEmpty(notebook.Name)) {
					return string.Format("{0} / {1}", Parent.Name, Name);
				} else {
					return string.Format("{0} / {1} / {2}", Parent.Parent.Name, Parent.Name, Name);
				}
			}
		}
	}

	public class OneNoteSection {
		public OneNoteNotebook Parent { get; set; }
		public string Name { get; set; }
		public string ID { get; set; }
	}

	public class OneNoteNotebook {
		public string Name { get; set; }
		public string ID { get; set; }
	}
}
