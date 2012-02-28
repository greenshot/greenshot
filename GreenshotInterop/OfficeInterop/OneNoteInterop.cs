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
using System.Xml.Schema;
using Greenshot.Interop;

namespace Greenshot.Interop.Office {
	// More details about OneNote: http://msdn.microsoft.com/en-us/magazine/ff796230.aspx

	// See http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.applicationclass_members%28v=Office.11%29.aspx
	[ComProgId("OneNote.Application")]
	public interface IOneNoteApplication : Common {
		/// <summary>
		/// Make sure that the notebookXml is of type string, e.g. "", otherwise a type error occurs.
		/// For more info on the methods: http://msdn.microsoft.com/en-us/library/gg649853.aspx
		/// </summary>
		void GetHierarchy(string startNode, HierarchyScope scope, out string notebookXml, XMLSchema schema);
		void UpdatePageContent(string pageChangesXml, DateTime dateExpectedLastModified, XMLSchema schema, bool force);
		void GetPageContent(string pageId, out string pageXml, PageInfo pageInfoToExport, XMLSchema schema);
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
}
