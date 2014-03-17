/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Interop.Office {
	// See http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.applicationclass_members%28v=Office.11%29.aspx
	[ComProgId("Word.Application")]
	public interface IWordApplication : ICommon {
		IWordDocument ActiveDocument { get; }
		ISelection Selection { get; }
		IDocuments Documents { get; }
		bool Visible { get; set; }
		void Activate();
		string Version { get; }
	}

	// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.documents_members(v=office.11).aspx
	public interface IDocuments : ICommon, ICollection {
		IWordDocument Add(ref object Template, ref object NewTemplate, ref object DocumentType, ref object Visible);
		IWordDocument item(int index);
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.document%28v=office.14%29.aspx 
	/// </summary>
	public interface IWordDocument : ICommon {
		void Activate();
		IWordApplication Application { get; }
		IWordWindow ActiveWindow { get; }
		bool ReadOnly { get; }
		IHyperlinks Hyperlinks { get; }

		// Only 2007 and later!
		bool Final { get; set; }
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.window_members.aspx
	/// </summary>
	public interface IWordWindow : ICommon {
		IPane ActivePane { get; }
		void Activate();
		string Caption {
			get;
		}
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.pane_members.aspx
	/// </summary>
	public interface IPane : ICommon {
		IWordView View { get; }
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.view_members.aspx
	/// </summary>
	public interface IWordView : ICommon {
		IZoom Zoom { get; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.zoom_members.aspx
	public interface IZoom : ICommon {
		int Percentage { get; set; }
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.selection_members(v=office.11).aspx 
	/// </summary>
	public interface ISelection : ICommon {
		IInlineShapes InlineShapes { get; }
		void InsertAfter(string text);
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/ms263866%28v=office.14%29.aspx
	/// </summary>
	public interface IInlineShapes : ICommon {
		IInlineShape AddPicture(string FileName, object LinkToFile, object SaveWithDocument, object Range);
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.inlineshape_members%28v=office.14%29.aspx
	/// </summary>
	public interface IInlineShape : ICommon {
		IHyperlink Hyperlink { get; }
		MsoTriState LockAspectRatio {
			get;
			set;
		}
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.hyperlink_members%28v=office.14%29.aspx
	/// </summary>
	public interface IHyperlink : ICommon {
		string Address {
			get;
			set;
		}
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.hyperlinks%28v=office.14%29.aspx
	/// </summary>
	public interface IHyperlinks : ICommon, ICollection {
		IHyperlink Add(object Anchor, object Address, object SubAddress, object ScreenTip, object TextToDisplay, object Target);
	}
}
