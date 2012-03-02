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
using System.Collections.Generic;

/// <summary>
/// This utils class should help setting the content-id on the attachment for Outlook < 2007
/// But this somehow doesn't work yet
/// </summary>
namespace Greenshot.Interop.Office {
	/// <summary>
	/// Wrapper for Outlook.Application, see: http://msdn.microsoft.com/en-us/library/aa210897%28v=office.11%29.aspx
	/// </summary>
	[ComProgId("Outlook.Application")]
	public interface IOutlookApplication : Common {
		string Name { get; }
		string Version { get; }
		Item CreateItem(OlItemType ItemType);
		object CreateItemFromTemplate(string TemplatePath, object InFolder);
		object CreateObject(string ObjectName);
		Inspector ActiveInspector();
		Inspectors Inspectors { get; }
		INameSpace GetNameSpace(string type);
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/bb176693%28v=office.12%29.aspx
	/// </summary>
	public interface INameSpace : Common {
		IRecipient CurrentUser { get; }
		IFolder GetDefaultFolder(OlDefaultFolders defaultFolder);
	}

	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/bb176362%28v=office.12%29.aspx
	/// </summary>
	public interface IFolder : Common {
		Items Items {get;}
	}

	public interface IRecipient : Common {
		string Name { get; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.inspector_members.aspx
	public interface Inspector : Common {
		Item CurrentItem { get; }
		OlEditorType EditorType { get; }
		object ModifiedFormPages { get; }
		void Close(OlInspectorClose SaveMode);
		void Display(object Modal);
		void HideFormPage(string PageName);
		bool IsWordMail();
		void SetCurrentFormPage(string PageName);
		void ShowFormPage(string PageName);
		object HTMLEditor { get; }
		IWordDocument WordEditor { get; }
		string Caption { get; }
		int Height { get; set; }
		int Left { get; set; }
		int Top { get; set; }
		int Width { get; set; }
		OlWindowState WindowState { get; set; }
		void Activate();
		void SetControlItemProperty(object Control, string PropertyName);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook._application.inspectors.aspx
	public interface Inspectors : Common, Collection, IEnumerable {
		// Use index + 1!!
		Inspector this[Object Index] { get; }
	}

	/// <summary>
	/// Specifies which EmailFormat the email needs to use
	/// </summary>
	public enum EmailFormat {
		Text, HTML
	}
}
