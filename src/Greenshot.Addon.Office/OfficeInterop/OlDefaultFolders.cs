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

namespace Greenshot.Addon.Office.OfficeInterop
{
	public enum OlDefaultFolders
	{
		olFolderCalendar = 9, // The Calendar folder.
		olFolderConflicts = 19, // The Conflicts folder (subfolder of Sync Issues folder). Only available for an Exchange account.
		olFolderContacts = 10, // The Contacts folder.
		olFolderDeletedItems = 3, // The Deleted Items folder.
		olFolderDrafts = 16, // The Drafts folder.
		olFolderInbox = 6, // The Inbox folder.
		olFolderJournal = 11, // The Journal folder.
		olFolderJunk = 23, // The Junk E-Mail folder.
		olFolderLocalFailures = 21, // The Local Failures folder (subfolder of Sync Issues folder). Only available for an Exchange account.
		olFolderManagedEmail = 29,
		// The top-level folder in the Managed Folders group. For more information on Managed Folders, see Help in Microsoft Outlook. Only available for an Exchange account.
		olFolderNotes = 12, // The Notes folder.
		olFolderOutbox = 4, // The Outbox folder.
		olFolderSentMail = 5, // The Sent Mail folder.
		olFolderServerFailures = 22, // The Server Failures folder (subfolder of Sync Issues folder). Only available for an Exchange account.
		olFolderSyncIssues = 20, // The Sync Issues folder. Only available for an Exchange account.
		olFolderTasks = 13, // The Tasks folder.
		olFolderToDo = 28, // The To Do folder.
		olPublicFoldersAllPublicFolders = 18, // The All Public Folders folder in the Exchange Public Folders store. Only available for an Exchange account.
		olFolderRssFeeds = 25 // The RSS Feeds folder.
	}
}