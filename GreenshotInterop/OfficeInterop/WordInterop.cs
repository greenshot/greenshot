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
using System.Collections;
using Greenshot.Interop;

namespace Greenshot.Interop.Office {
	// See http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.applicationclass_members%28v=Office.11%29.aspx
	[ComProgId("Word.Application")]
	public interface IWordApplication : Common {
		IWordDocument ActiveDocument { get; }
		ISelection Selection { get; }
		IDocuments Documents { get; }
		bool Visible { get; set; }
		void Activate();
		string Version { get; }
	}

	// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.documents_members(v=office.11).aspx
	public interface IDocuments : Common, Collection {
		IWordDocument Add(ref object Template, ref object NewTemplate, ref object DocumentType, ref object Visible);
		IWordDocument item(int index);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.document.aspx
	public interface IWordDocument : Common {
		void Activate();
		IWordApplication Application { get; }
		IWordWindow ActiveWindow { get; }
		bool ReadOnly { get; }

		// Only 2007 and later!
		bool Final { get; set; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.window_members.aspx
	public interface IWordWindow : Common {
		IPane ActivePane { get; }
		void Activate();
		string Caption {
			get;
		}
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.pane_members.aspx
	public interface IPane : Common {
		IWordView View { get; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.view_members.aspx
	public interface IWordView : Common {
		IZoom Zoom { get; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.zoom_members.aspx
	public interface IZoom : Common {
		int Percentage { get; set; }
	}

	// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.selection_members(v=office.11).aspx
	public interface ISelection : Common {
		IInlineShapes InlineShapes { get; }
		void InsertAfter(string text);
	}

	public interface IInlineShapes : Common {
		object AddPicture(string FileName, object LinkToFile, object SaveWithDocument, object Range);
	}
}
