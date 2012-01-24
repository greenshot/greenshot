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

namespace Greenshot.Helpers.OfficeInterop {
	/// <summary>
	/// Common properties that has appreared in almost all objects
	/// </summary>
	public interface Common : IDisposable {
	}

	public interface Collection : Common, IEnumerable {
		int Count { get; }
		void Remove( int index );
	}

	public interface Items : Collection, IEnumerable
	{
		Item this[ object index ] { get; }
		object GetFirst();
		object GetLast();

		// Actual definition is "object Add( object )", just making it convenient
		object Add( OlItemType type );
	}

	// See: http://msdn.microsoft.com/en-us/library/ff861252.aspx
	public interface Item : Common {
		Attachments Attachments { get; }
		string Body { get; set; }
		OlObjectClass Class { get; }
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
		PropertyAccessor PropertyAccessor { get; }
		bool Sent { get; }
		object MAPIOBJECT { get; }
	}

	public interface Attachments : Collection {
		Attachment Add( object source, object type, object position, object displayName );
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.attachment_members.aspx
	public interface Attachment : Common {
		string DisplayName { get;set; }
		string FileName { get; }
		OlAttachmentType Type { get; }
		PropertyAccessor PropertyAccessor { get; }
		object MAPIOBJECT { get; }
	}
	
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.propertyaccessor_members.aspx
	public interface PropertyAccessor : Common {
		void SetProperty(string SchemaName, Object Value);
		Object GetProperty(string SchemaName);
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
	
	// See: http://msdn.microsoft.com/en-us/library/ff863329.aspx
	public enum OlObjectClass {
		olAccount	=	105	, //	Represents an Account object.
		olAccountRuleCondition	=	135	, //	Represents an AccountRuleCondition object.
		olAccounts	=	106	, //	Represents an Accounts object.
		olAction	=	32	, //	Represents an Action object.
		olActions	=	33	, //	Represents an Actions object.
		olAddressEntries	=	21	, //	Represents an AddressEntries object.
		olAddressEntry	=	8	, //	Represents an AddressEntry object.
		olAddressList	=	7	, //	Represents an AddressList object.
		olAddressLists	=	20	, //	Represents an AddressLists object.
		olAddressRuleCondition	=	170	, //	Represents an AddressRuleCondition object.
		olApplication	=	0	, //	Represents an Application object.
		olAppointment	=	26	, //	Represents an AppointmentItem object.
		olAssignToCategoryRuleAction	=	122	, //	Represents an AssignToCategoryRuleAction object.
		olAttachment	=	5	, //	Represents an Attachment object.
		olAttachments	=	18	, //	Represents an Attachments object.
		olAttachmentSelection	=	169	, //	Represents an AttachmentSelection object.
		olAutoFormatRule	=	147	, //	Represents an AutoFormatRule object.
		olAutoFormatRules	=	148	, //	Represents an AutoFormatRules object.
		olCalendarModule	=	159	, //	Represents a CalendarModule object.
		olCalendarSharing	=	151	, //	Represents a CalendarSharing object.
		olCategories	=	153	, //	Represents a Categories object.
		olCategory	=	152	, //	Represents a Category object.
		olCategoryRuleCondition	=	130	, //	Represents a CategoryRuleCondition object.
		olClassBusinessCardView	=	168	, //	Represents a BusinessCardView object.
		olClassCalendarView	=	139	, //	Represents a CalendarView object.
		olClassCardView	=	138	, //	Represents a CardView object.
		olClassIconView	=	137	, //	Represents a IconView object.
		olClassNavigationPane	=	155	, //	Represents a NavigationPane object.
		olClassTableView	=	136	, //	Represents a TableView object.
		olClassTimeLineView	=	140	, //	Represents a TimelineView object.
		olClassTimeZone	=	174	, //	Represents a TimeZone object.
		olClassTimeZones	=	175	, //	Represents a TimeZones object.
		olColumn	=	154	, //	Represents a Column object.
		olColumnFormat	=	149	, //	Represents a ColumnFormat object.
		olColumns	=	150	, //	Represents a Columns object.
		olConflict	=	102	, //	Represents a Conflict object.
		olConflicts	=	103	, //	Represents a Conflicts object.
		olContact	=	40	, //	Represents a ContactItem object.
		olContactsModule	=	160	, //	Represents a ContactsModule object.
		olDistributionList	=	69	, //	Represents a ExchangeDistributionList object.
		olDocument	=	41	, //	Represents a DocumentItem object.
		olException	=	30	, //	Represents an Exception object.
		olExceptions	=	29	, //	Represents an Exceptions object.
		olExchangeDistributionList	=	111	, //	Represents an ExchangeDistributionList object.
		olExchangeUser	=	110	, //	Represents an ExchangeUser object.
		olExplorer	=	34	, //	Represents an Explorer object.
		olExplorers	=	60	, //	Represents an Explorers object.
		olFolder	=	2	, //	Represents a Folder object.
		olFolders	=	15	, //	Represents a Folders object.
		olFolderUserProperties	=	172	, //	Represents a UserDefinedProperties object.
		olFolderUserProperty	=	171	, //	Represents a UserDefinedProperty object.
		olFormDescription	=	37	, //	Represents a FormDescription object.
		olFormNameRuleCondition	=	131	, //	Represents a FormNameRuleCondition object.
		olFormRegion	=	129	, //	Represents a FormRegion object.
		olFromRssFeedRuleCondition	=	173	, //	Represents a FromRssFeedRuleCondition object.
		olFromRuleCondition	=	132	, //	Represents a ToOrFromRuleCondition object.
		olImportanceRuleCondition	=	128	, //	Represents an ImportanceRuleCondition object.
		olInspector	=	35	, //	Represents an Inspector object.
		olInspectors	=	61	, //	Represents an Inspectors object.
		olItemProperties	=	98	, //	Represents an ItemProperties object.
		olItemProperty	=	99	, //	Represents an ItemProperty object.
		olItems	=	16	, //	Represents an Items object.
		olJournal	=	42	, //	Represents a JournalItem object.
		olJournalModule	=	162	, //	Represents a JournalModule object.
		olLink	=	75	, //	Represents a Link object.
		olLinks	=	76	, //	Represents a Links object.
		olMail	=	43	, //	Represents a MailItem object.
		olMailModule	=	158	, //	Represents a MailModule object.
		olMarkAsTaskRuleAction	=	124	, //	Represents a MarkAsTaskRuleAction object.
		olMeetingCancellation	=	54	, //	Represents a MeetingItem object that is a meeting cancellation notice.
		olMeetingRequest	=	53	, //	Represents a MeetingItem object that is a meeting request.
		olMeetingResponseNegative	=	55	, //	Represents a MeetingItem object that is a refusal of a meeting request.
		olMeetingResponsePositive	=	56	, //	Represents a MeetingItem object that is an acceptance of a meeting request.
		olMeetingResponseTentative	=	57	, //	Represents a MeetingItem object that is a tentative acceptance of a meeting request.
		olMoveOrCopyRuleAction	=	118	, //	Represents a MoveOrCopyRuleAction object.
		olNamespace	=	1	, //	Represents a NameSpace object.
		olNavigationFolder	=	167	, //	Represents a NavigationFolder object.
		olNavigationFolders	=	166	, //	Represents a NavigationFolders object.
		olNavigationGroup	=	165	, //	Represents a NavigationGroup object.
		olNavigationGroups	=	164	, //	Represents a NavigationGroups object.
		olNavigationModule	=	157	, //	Represents a NavigationModule object.
		olNavigationModules	=	156	, //	Represents a NavigationModules object.
		olNewItemAlertRuleAction	=	125	, //	Represents a NewItemAlertRuleAction object.
		olNote	=	44	, //	Represents a NoteItem object.
		olNotesModule	=	163	, //	Represents a NotesModule object.
		olOrderField	=	144	, //	Represents an OrderField object.
		olOrderFields	=	145	, //	Represents an OrderFields object.
		olOutlookBarGroup	=	66	, //	Represents an OutlookBarGroup object.
		olOutlookBarGroups	=	65	, //	Represents an OutlookBarGroups object.
		olOutlookBarPane	=	63	, //	Represents an OutlookBarPane object.
		olOutlookBarShortcut	=	68	, //	Represents an OutlookBarShortcut object.
		olOutlookBarShortcuts	=	67	, //	Represents an OutlookBarShortcuts object.
		olOutlookBarStorage	=	64	, //	Represents an OutlookBarStorage object.
		olPages	=	36	, //	Represents a Pages object.
		olPanes	=	62	, //	Represents a Panes object.
		olPlaySoundRuleAction	=	123	, //	Represents a PlaySoundRuleAction object.
		olPost	=	45	, //	Represents a PostItem object.
		olPropertyAccessor	=	112	, //	Represents a PropertyAccessor object.
		olPropertyPages	=	71	, //	Represents a PropertyPages object.
		olPropertyPageSite	=	70	, //	Represents a PropertyPageSite object.
		olRecipient	=	4	, //	Represents a Recipient object.
		olRecipients	=	17	, //	Represents a Recipients object.
		olRecurrencePattern	=	28	, //	Represents a RecurrencePattern object.
		olReminder	=	101	, //	Represents a Reminder object.
		olReminders	=	100	, //	Represents a Reminders object.
		olRemote	=	47	, //	Represents a RemoteItem object.
		olReport	=	46	, //	Represents a ReportItem object.
		olResults	=	78	, //	Represents a Results object.
		olRow	=	121	, //	Represents a Row object.
		olRule	=	115	, //	Represents a Rule object.
		olRuleAction	=	117	, //	Represents a RuleAction object.
		olRuleActions	=	116	, //	Represents a RuleAction object.
		olRuleCondition	=	127	, //	Represents a RuleCondition object.
		olRuleConditions	=	126	, //	Represents a RuleConditions object.
		olRules	=	114	, //	Represents a Rules object.
		olSearch	=	77	, //	Represents a Search object.
		olSelection	=	74	, //	Represents a Selection object.
		olSelectNamesDialog	=	109	, //	Represents a SelectNamesDialog object.
		olSenderInAddressListRuleCondition	=	133	, //	Represents a SenderInAddressListRuleCondition object.
		olSendRuleAction	=	119	, //	Represents a SendRuleAction object.
		olSharing	=	104	, //	Represents a SharingItem object.
		olStorageItem	=	113	, //	Represents a StorageItem object.
		olStore	=	107	, //	Represents a Store object.
		olStores	=	108	, //	Represents a Stores object.
		olSyncObject	=	72	, //	Represents a SyncObject object.
		olSyncObjects	=	73	, //	Represents a SyncObject object.
		olTable	=	120	, //	Represents a Table object.
		olTask	=	48	, //	Represents a TaskItem object.
		olTaskRequest	=	49	, //	Represents a TaskRequestItem object.
		olTaskRequestAccept	=	51	, //	Represents a TaskRequestAcceptItem object.
		olTaskRequestDecline	=	52	, //	Represents a TaskRequestDeclineItem object.
		olTaskRequestUpdate	=	50	, //	Represents a TaskRequestUpdateItem object.
		olTasksModule	=	161	, //	Represents a TasksModule object.
		olTextRuleCondition	=	134	, //	Represents a TextRuleCondition object.
		olUserDefinedProperties	=	172	, //	Represents a UserDefinedProperties object.
		olUserDefinedProperty	=	171	, //	Represents a UserDefinedProperty object.
		olUserProperties	=	38	, //	Represents a UserProperties object.
		olUserProperty	=	39	, //	Represents a UserProperty object.
		olView	=	80	, //	Represents a View object.
		olViewField	=	142	, //	Represents a ViewField object.
		olViewFields	=	141	, //	Represents a ViewFields object.
		olViewFont	=	146	, //	Represents a ViewFont object.
		olViews	=	79	//	Represents a Views object.
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
	
	// Schema definitions for the MAPI properties
	// See: http://msdn.microsoft.com/en-us/library/aa454438.aspx
	// and see: http://msdn.microsoft.com/en-us/library/bb446117.aspx
	public static class PropTag {
		public const string ATTACHMENT_CONTENT_ID = @"http://schemas.microsoft.com/mapi/proptag/0x3712001E";
	}
}
