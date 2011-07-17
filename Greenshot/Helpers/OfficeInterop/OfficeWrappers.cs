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
using System.Collections;
using Greenshot.Interop;

namespace Greenshot.Helpers.OfficeInterop {
	/// <summary>
	/// Common properties that has appreared in almost all objects
	/// </summary>
	public interface Common : IDisposable {
	}

	public interface Collection : Common, IEnumerable
	{
		int Count { get; }
		void Remove( int index );
	}

	public interface Items : Collection, IEnumerable
	{
		object this[ object index ] { get; }
		object GetFirst();
		object GetLast();

		// Actual definition is "object Add( object )", just making it convenient
		object Add( OlItemType type );
	}

	public interface Item : Common {
		Attachments Attachments { get; }
		string Body { get; set; }
		DateTime CreationTime { get; }
		string EntryID { get; }
		DateTime LastModificationTime { get; }
		string MessageClass { get; set; }
		bool NoAging { get; set; }
		int OutlookInternalVersion { get; }
		string OutlookVersion { get; }
		bool Saved { get; }
		OlSensitivity Sensitivity { get; set; }
		int Size { get; }
		string Subject { get; set; }
		bool UnRead { get; set; }
		object Copy();
		void Display(object Modal);
		void Save();
		DateTime ExpiryTime { get; set; }
		string HTMLBody { get; set; }
		DateTime ReceivedTime { get; }
		string SenderName { get; }
		DateTime SentOn { get; }
		OlBodyFormat BodyFormat { get; set; }
	}

	public interface Attachments : Collection {
		Attachment Add( object source, object type, object position, object displayName );
	}

	public interface Attachment : Common {
		string DisplayName { get;set; }
		string FileName { get;}
		OlAttachmentType Type { get;}
	}

	// See: http://msdn.microsoft.com/en-us/library/ff861252.aspx
	public interface MailItem : Item {
		MailItem Forward();
		MailItem Reply();
		bool Sent { get; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.inspector_members.aspx
	public interface Inspector : Common {
		object CurrentItem { get;  }
		OlEditorType EditorType { get;  }
		object ModifiedFormPages { get;  }
		void Close(OlInspectorClose SaveMode);
		void Display(object Modal);
		void HideFormPage(string PageName);
		bool IsWordMail();
		void SetCurrentFormPage(string PageName);
		void ShowFormPage(string PageName);
		object HTMLEditor { get;  }
		IWordDocument WordEditor { get;  }
		string Caption { get;  }
		int Height { get; set; }
		int Left { get; set; }
		int Top { get; set; }
		int Width { get; set; }
		OlWindowState WindowState { get; set; }
		void Activate();
		void SetControlItemProperty(object Control, string PropertyName);
	}

	public enum OlBodyFormat {
		// Fields
		olFormatHTML = 2,
		olFormatPlain = 1,
		olFormatRichText = 3,
		olFormatUnspecified = 0
	}

	public enum OlAttachmentType {
		// Fields
		olByReference = 4,
		olByValue = 1,
		olEmbeddeditem = 5,
		olOLE = 6
	}

	public enum OlSensitivity {
		// Fields
		olConfidential = 3,
		olNormal = 0,
		olPersonal = 1,
		olPrivate = 2
	}

	public enum OlItemType {
		// Fields
		olAppointmentItem = 1,
		olContactItem = 2,
		olDistributionListItem = 7,
		olJournalItem = 4,
		olMailItem = 0,
		olNoteItem = 5,
		olPostItem = 6,
		olTaskItem = 3
	}
	
	public enum OlEditorType {
		// Fields
		olEditorHTML = 2,
		olEditorRTF = 3,
		olEditorText = 1,
		olEditorWord = 4
	}
	
	public enum OlWindowState {
		// Fields
		olMaximized = 0,
		olMinimized = 1,
		olNormalWindow = 2
	}
	
	public enum OlInspectorClose {
		// Fields
		olDiscard = 1,
		olPromptForSave = 2,
		olSave = 0
	}
	
	public enum MsoTriState {
		msoTrue = -1,
		msoFalse = 0,
		msoCTrue = 1,
		msoTriStateToggle = -3,
		msoTriStateMixed = -2
	}
	
	public enum MsoScaleFrom {
		msoScaleFromTopLeft = 0,
		msoScaleFromMiddle = 1,
		msoScaleFromBottomRight = 2
	}
}
