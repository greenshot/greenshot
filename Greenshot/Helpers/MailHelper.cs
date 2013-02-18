/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

/// <summary>
/// Author: Andrew Baker
/// Datum: 10.03.2006
/// Available from: http://www.vbusers.com/codecsharp/codeget.asp?ThreadID=71&PostID=1
/// </summary>
namespace Greenshot.Helpers {
	#region Public MapiMailMessage Class

	/// <summary>
	/// Represents an email message to be sent through MAPI.
	/// </summary>
	public class MapiMailMessage {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(MapiMailMessage));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

		/// <summary>
		/// Helper Method for creating an Email with Attachment
		/// </summary>
		/// <param name="fullpath">Path to file</param>
		/// <param name="captureDetails"></param>
		public static void SendImage(string fullPath, string title) {
			MapiMailMessage message = new MapiMailMessage(title, null);
			message.Files.Add(fullPath);
			if (!string.IsNullOrEmpty(conf.MailApiTo)) {
				message._recipientCollection.Add(new Recipient(conf.MailApiTo, RecipientType.To));
			}
			if (!string.IsNullOrEmpty(conf.MailApiCC)) {
				message._recipientCollection.Add(new Recipient(conf.MailApiCC, RecipientType.CC));
			}
			if (!string.IsNullOrEmpty(conf.MailApiBCC)) {
				message._recipientCollection.Add(new Recipient(conf.MailApiBCC, RecipientType.BCC));
			}
			message.ShowDialog();
		}


		/// <summary>
		/// Helper Method for creating an Email with Image Attachment
		/// </summary>
		/// <param name="image">The image to send</param>
		/// <param name="captureDetails">ICaptureDetails</param>
		public static void SendImage(ISurface surface, ICaptureDetails captureDetails) {
			string tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings());

			if (tmpFile != null) {
				// Store the list of currently active windows, so we can make sure we show the email window later!
				List<WindowDetails> windowsBefore = WindowDetails.GetVisibleWindows();
				bool isEmailSend = false;
				//if (EmailConfigHelper.HasOutlook() && (conf.OutputEMailFormat == EmailFormat.OUTLOOK_HTML || conf.OutputEMailFormat == EmailFormat.OUTLOOK_TXT)) {
				//	isEmailSend = OutlookExporter.ExportToOutlook(tmpFile, captureDetails);
				//}
				if (!isEmailSend && EmailConfigHelper.HasMAPI()) {
					// Fallback to MAPI
					// Send the email
					SendImage(tmpFile, captureDetails.Title);
				}
				WindowDetails.ActiveNewerWindows(windowsBefore);
			}
		}
		#region Private MapiFileDescriptor Class

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		private class MapiFileDescriptor {
			public int reserved = 0;
			public int flags = 0;
			public int position = 0;
			public string path = null;
			public string name = null;
			public IntPtr type = IntPtr.Zero;
		}

		#endregion Private MapiFileDescriptor Class

		#region Enums

		/// <summary>
		/// Specifies the valid RecipientTypes for a Recipient.
		/// </summary>
		public enum RecipientType : int {
			/// <summary>
			/// Recipient will be in the TO list.
			/// </summary>
			To = 1,

			/// <summary>
			/// Recipient will be in the CC list.
			/// </summary>
			CC = 2,

			/// <summary>
			/// Recipient will be in the BCC list.
			/// </summary>
			BCC = 3
		};

		#endregion Enums

		#region Member Variables

		private string _subject;
		private string _body;
		private RecipientCollection _recipientCollection;
		private List<string> _files;
		private ManualResetEvent _manualResetEvent;

		#endregion Member Variables

		#region Constructors

		/// <summary>
		/// Creates a blank mail message.
		/// </summary>
		public MapiMailMessage() {
			_files = new List<string>();
			_recipientCollection = new RecipientCollection();
			_manualResetEvent = new ManualResetEvent(false);
		}

		/// <summary>
		/// Creates a new mail message with the specified subject.
		/// </summary>
		public MapiMailMessage(string subject) : this() {
			_subject = subject;
		}

		/// <summary>
		/// Creates a new mail message with the specified subject and body.
		/// </summary>
		public MapiMailMessage(string subject, string body) : this() {
			_subject = subject;
			_body = body;
		}

		#endregion Constructors

		#region Public Properties

		/// <summary>
		/// Gets or sets the subject of this mail message.
		/// </summary>
		public string Subject {
			get {
				return _subject;
			}
			set {
				_subject = value;
			}
		}

		/// <summary>
		/// Gets or sets the body of this mail message.
		/// </summary>
		public string Body {
			get {
				return _body;
			}
			set {
				_body = value;
			}
		}

		/// <summary>
		/// Gets the recipient list for this mail message.
		/// </summary>
		public RecipientCollection Recipients {
			get {
				return _recipientCollection;
			}
		}

		/// <summary>
		/// Gets the file list for this mail message.
		/// </summary>
		public List<string> Files {
			get {
				return _files;
			}
		}

		#endregion Public Properties

		#region Public Methods

		/// <summary>
		/// Displays the mail message dialog asynchronously.
		/// </summary>
		public void ShowDialog() {
			// Create the mail message in an STA thread
			Thread t = new Thread(new ThreadStart(_ShowMail));
			t.IsBackground = true;
			t.Name = "Create MAPI mail";
			t.SetApartmentState(ApartmentState.STA);
			t.Start();

			// only return when the new thread has built it's interop representation
			_manualResetEvent.WaitOne();
			_manualResetEvent.Reset();
		}

		#endregion Public Methods

		#region Private Methods

		/// <summary>
		/// Sends the mail message.
		/// </summary>
		private void _ShowMail() {
			MAPIHelperInterop.MapiMessage message = new MAPIHelperInterop.MapiMessage();

			using (RecipientCollection.InteropRecipientCollection interopRecipients = _recipientCollection.GetInteropRepresentation()) {
				message.Subject = _subject;
				message.NoteText = _body;

				message.Recipients = interopRecipients.Handle;
				message.RecipientCount = _recipientCollection.Count;

				// Check if we need to add attachments
				if (_files.Count > 0) {
					// Add attachments
					message.Files = _AllocAttachments(out message.FileCount);
				}

				// Signal the creating thread (make the remaining code async)
				_manualResetEvent.Set();

				const int MAPI_DIALOG = 0x8;
				//const int MAPI_LOGON_UI = 0x1;
				int error = MAPIHelperInterop.MAPISendMail(IntPtr.Zero, IntPtr.Zero, message, MAPI_DIALOG, 0);

				if (_files.Count > 0) {
					// Deallocate the files
					_DeallocFiles(message);
				}
				MAPI_CODES errorCode = (MAPI_CODES)Enum.ToObject(typeof(MAPI_CODES), error);

				// Check for error
				if (errorCode != MAPI_CODES.SUCCESS && errorCode != MAPI_CODES.USER_ABORT) {
					string errorText = GetMapiError(errorCode);
					LOG.Error("Error sending MAPI Email. Error: " + errorText + " (code = " + errorCode + ").");
					MessageBox.Show(errorText, "Mail (MAPI) destination", MessageBoxButtons.OK, MessageBoxIcon.Error);
					// Recover from bad settings, show again
					if (errorCode == MAPI_CODES.INVALID_RECIPS) {
						_recipientCollection = new RecipientCollection();
						_ShowMail();
					}
				}
			}
		}

		/// <summary>
		/// Deallocates the files in a message.
		/// </summary>
		/// <param name="message">The message to deallocate the files from.</param>
		private void _DeallocFiles(MAPIHelperInterop.MapiMessage message) {
			if (message.Files != IntPtr.Zero) {
				Type fileDescType = typeof(MapiFileDescriptor);
				int fsize = Marshal.SizeOf(fileDescType);

				// Get the ptr to the files
				IntPtr runptr = message.Files.Clone();
				// Release each file
				for (int i = 0; i < message.FileCount; i++) {
					Marshal.DestroyStructure(runptr, fileDescType);
					runptr = new IntPtr(runptr.ToInt64() + fsize);
				}
				// Release the file
				Marshal.FreeHGlobal(message.Files);
			}
		}

		/// <summary>
		/// Allocates the file attachments
		/// </summary>
		/// <param name="fileCount"></param>
		/// <returns></returns>
		private IntPtr _AllocAttachments(out int fileCount) {
			fileCount = 0;
			if (_files == null) {
				return IntPtr.Zero;
			}
			if ((_files.Count <= 0) || (_files.Count > 100)) {
				return IntPtr.Zero;
			}

			Type atype = typeof(MapiFileDescriptor);
			int asize = Marshal.SizeOf(atype);
			IntPtr ptra = Marshal.AllocHGlobal(_files.Count * asize);

			MapiFileDescriptor mfd = new MapiFileDescriptor();
			mfd.position = -1;
			IntPtr runptr = ptra.Clone();
			for (int i = 0; i < _files.Count; i++) {
				string path = _files[i] as string;
				mfd.name = Path.GetFileName(path);
				mfd.path = path;
				Marshal.StructureToPtr(mfd, runptr, false);
				runptr = new IntPtr(runptr.ToInt64() + asize);
			}

			fileCount = _files.Count;
			return ptra;
		}

		private enum MAPI_CODES {
			SUCCESS = 0,
			USER_ABORT = 1,
			FAILURE = 2,
			LOGIN_FAILURE = 3,
			DISK_FULL = 4,
			INSUFFICIENT_MEMORY = 5,
			BLK_TOO_SMALL = 6,
			TOO_MANY_SESSIONS = 8,
			TOO_MANY_FILES = 9,
			TOO_MANY_RECIPIENTS = 10,
			ATTACHMENT_NOT_FOUND = 11,
			ATTACHMENT_OPEN_FAILURE = 12,
			ATTACHMENT_WRITE_FAILURE = 13,
			UNKNOWN_RECIPIENT = 14,
			BAD_RECIPTYPE = 15,
			NO_MESSAGES = 16,
			INVALID_MESSAGE = 17,
			TEXT_TOO_LARGE = 18,
			INVALID_SESSION = 19,
			TYPE_NOT_SUPPORTED = 20,
			AMBIGUOUS_RECIPIENT = 21,
			MESSAGE_IN_USE = 22,
			NETWORK_FAILURE = 23,
			INVALID_EDITFIELDS = 24,
			INVALID_RECIPS = 25,
			NOT_SUPPORTED = 26,
			NO_LIBRARY = 999,
			INVALID_PARAMETER = 998
		}
		/// <summary>
		/// Logs any Mapi errors.
		/// </summary>
		private string GetMapiError(MAPI_CODES errorCode) {

			string error = string.Empty;

			switch (errorCode) {
				case MAPI_CODES.USER_ABORT:
					error = "User Aborted.";
					break;
				case MAPI_CODES.FAILURE:
					error = "MAPI Failure.";
					break;
				case MAPI_CODES.LOGIN_FAILURE:
					error = "Login Failure.";
					break;
				case MAPI_CODES.DISK_FULL:
					error = "MAPI Disk full.";
					break;
				case MAPI_CODES.INSUFFICIENT_MEMORY:
					error = "MAPI Insufficient memory.";
					break;
				case MAPI_CODES.BLK_TOO_SMALL:
					error = "MAPI Block too small.";
					break;
				case MAPI_CODES.TOO_MANY_SESSIONS:
					error = "MAPI Too many sessions.";
					break;
				case MAPI_CODES.TOO_MANY_FILES:
					error = "MAPI too many files.";
					break;
				case MAPI_CODES.TOO_MANY_RECIPIENTS:
					error = "MAPI too many recipients.";
					break;
				case MAPI_CODES.ATTACHMENT_NOT_FOUND:
					error = "MAPI Attachment not found.";
					break;
				case MAPI_CODES.ATTACHMENT_OPEN_FAILURE:
					error = "MAPI Attachment open failure.";
					break;
				case MAPI_CODES.ATTACHMENT_WRITE_FAILURE:
					error = "MAPI Attachment Write Failure.";
					break;
				case MAPI_CODES.UNKNOWN_RECIPIENT:
					error = "MAPI Unknown recipient.";
					break;
				case MAPI_CODES.BAD_RECIPTYPE:
					error = "MAPI Bad recipient type.";
					break;
				case MAPI_CODES.NO_MESSAGES:
					error = "MAPI No messages.";
					break;
				case MAPI_CODES.INVALID_MESSAGE:
					error = "MAPI Invalid message.";
					break;
				case MAPI_CODES.TEXT_TOO_LARGE:
					error = "MAPI Text too large.";
					break;
				case MAPI_CODES.INVALID_SESSION:
					error = "MAPI Invalid session.";
					break;
				case MAPI_CODES.TYPE_NOT_SUPPORTED:
					error = "MAPI Type not supported.";
					break;
				case MAPI_CODES.AMBIGUOUS_RECIPIENT:
					error = "MAPI Ambiguous recipient.";
					break;
				case MAPI_CODES.MESSAGE_IN_USE:
					error = "MAPI Message in use.";
					break;
				case MAPI_CODES.NETWORK_FAILURE:
					error = "MAPI Network failure.";
					break;
				case MAPI_CODES.INVALID_EDITFIELDS:
					error = "MAPI Invalid edit fields.";
					break;
				case MAPI_CODES.INVALID_RECIPS:
					error = "MAPI Invalid Recipients.";
					break;
				case MAPI_CODES.NOT_SUPPORTED:
					error = "MAPI Not supported.";
					break;
				case MAPI_CODES.NO_LIBRARY:
					error = "MAPI No Library.";
					break;
				case MAPI_CODES.INVALID_PARAMETER:
					error = "MAPI Invalid parameter.";
					break;
			}
			return error;
		}
		#endregion Private Methods

		#region Private MAPIHelperInterop Class

		/// <summary>
		/// Internal class for calling MAPI APIs
		/// </summary>
		internal class MAPIHelperInterop {
			#region Constructors

			/// <summary>
			/// Private constructor.
			/// </summary>
			private MAPIHelperInterop() {
				// Intenationally blank
			}

			#endregion Constructors


			#region Structs

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
			public class MapiMessage {
				public int Reserved = 0;
				public string Subject = null;
				public string NoteText = null;
				public string MessageType = null;
				public string DateReceived = null;
				public string ConversationID = null;
				public int Flags = 0;
				public IntPtr Originator = IntPtr.Zero;
				public int RecipientCount = 0;
				public IntPtr Recipients = IntPtr.Zero;
				public int FileCount = 0;
				public IntPtr Files = IntPtr.Zero;
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
			public class MapiRecipDesc {
				public int Reserved = 0;
				public int RecipientClass = 0;
				public string Name = null;
				public string Address = null;
				public int eIDSize = 0;
				public IntPtr EntryID = IntPtr.Zero;
			}

			[DllImport("MAPI32.DLL", SetLastError = true)]
			public static extern int MAPISendMail(IntPtr session, IntPtr hwnd, MapiMessage message, int flg, int rsv);

			#endregion Structs
		}

		#endregion Private MAPIHelperInterop Class
	}

	#endregion Public MapiMailMessage Class

	#region Public Recipient Class

	/// <summary>
	/// Represents a Recipient for a MapiMailMessage.
	/// </summary>
	public class Recipient {
		#region Public Properties

		/// <summary>
		/// The email address of this recipient.
		/// </summary>
		public string Address = null;

		/// <summary>
		/// The display name of this recipient.
		/// </summary>
		public string DisplayName = null;

		/// <summary>
		/// How the recipient will receive this message (To, CC, BCC).
		/// </summary>
		public MapiMailMessage.RecipientType RecipientType = MapiMailMessage.RecipientType.To;

		#endregion Public Properties

		#region Constructors

		/// <summary>
		/// Creates a new recipient with the specified address.
		/// </summary>
		public Recipient(string address) {
			Address = address;
		}

		/// <summary>
		/// Creates a new recipient with the specified address and display name.
		/// </summary>
		public Recipient(string address, string displayName) {
			Address = address;
			DisplayName = displayName;
		}

		/// <summary>
		/// Creates a new recipient with the specified address and recipient type.
		/// </summary>
		public Recipient(string address, MapiMailMessage.RecipientType recipientType) {
			Address = address;
			RecipientType = recipientType;
		}

		/// <summary>
		/// Creates a new recipient with the specified address, display name and recipient type.
		/// </summary>
		public Recipient(string address, string displayName, MapiMailMessage.RecipientType recipientType) {
			Address = address;
			DisplayName = displayName;
			RecipientType = recipientType;
		}

		#endregion Constructors

		#region Internal Methods

		/// <summary>
		/// Returns an interop representation of a recepient.
		/// </summary>
		/// <returns></returns>
		internal MapiMailMessage.MAPIHelperInterop.MapiRecipDesc GetInteropRepresentation() {
			MapiMailMessage.MAPIHelperInterop.MapiRecipDesc interop = new MapiMailMessage.MAPIHelperInterop.MapiRecipDesc();

			if (DisplayName == null) {
				interop.Name = Address;
			} else {
				interop.Name = DisplayName;
				interop.Address = Address;
			}

			interop.RecipientClass = (int)RecipientType;

			return interop;
		}

		#endregion Internal Methods
	}

	#endregion Public Recipient Class

	#region Public RecipientCollection Class

	/// <summary>
	/// Represents a colleciton of recipients for a mail message.
	/// </summary>
	public class RecipientCollection : CollectionBase {
		/// <summary>
		/// Adds the specified recipient to this collection.
		/// </summary>
		public void Add(Recipient value) {
			List.Add(value);
		}

		/// <summary>
		/// Adds a new recipient with the specified address to this collection.
		/// </summary>
		public void Add(string address) {
			this.Add(new Recipient(address));
		}

		/// <summary>
		/// Adds a new recipient with the specified address and display name to this collection.
		/// </summary>
		public void Add(string address, string displayName) {
			this.Add(new Recipient(address, displayName));
		}

		/// <summary>
		/// Adds a new recipient with the specified address and recipient type to this collection.
		/// </summary>
		public void Add(string address, MapiMailMessage.RecipientType recipientType) {
			this.Add(new Recipient(address, recipientType));
		}

		/// <summary>
		/// Adds a new recipient with the specified address, display name and recipient type to this collection.
		/// </summary>
		public void Add(string address, string displayName, MapiMailMessage.RecipientType recipientType) {
			this.Add(new Recipient(address, displayName, recipientType));
		}

		/// <summary>
		/// Returns the recipient stored in this collection at the specified index.
		/// </summary>
		public Recipient this[int index] {
			get {
				return (Recipient)List[index];
			}
		}

		internal InteropRecipientCollection GetInteropRepresentation() {
			return new InteropRecipientCollection(this);
		}

		/// <summary>
		/// Struct which contains an interop representation of a colleciton of recipients.
		/// </summary>
		internal struct InteropRecipientCollection : IDisposable {
			#region Member Variables

			private IntPtr _handle;
			private int _count;

			#endregion Member Variables

			#region Constructors

			/// <summary>
			/// Default constructor for creating InteropRecipientCollection.
			/// </summary>
			/// <param name="outer"></param>
			public InteropRecipientCollection(RecipientCollection outer) {
				_count = outer.Count;

				if (_count == 0) {
					_handle = IntPtr.Zero;
					return;
				}

				// allocate enough memory to hold all recipients
				int size = Marshal.SizeOf(typeof(MapiMailMessage.MAPIHelperInterop.MapiRecipDesc));
				_handle = Marshal.AllocHGlobal(_count * size);

				// place all interop recipients into the memory just allocated
				IntPtr ptr = _handle.Clone();
				foreach (Recipient native in outer) {
					MapiMailMessage.MAPIHelperInterop.MapiRecipDesc interop = native.GetInteropRepresentation();

					// stick it in the memory block
					Marshal.StructureToPtr(interop, ptr, false);
					ptr = new IntPtr(ptr.ToInt64() + size);
				}
			}

			#endregion Costructors

			#region Public Properties

			public IntPtr Handle {
				get {
					return _handle;
				}
			}

			#endregion Public Properties

			#region Public Methods

			/// <summary>
			/// Disposes of resources.
			/// </summary>
			public void Dispose() {
				if (_handle != IntPtr.Zero) {
					Type type = typeof(MapiMailMessage.MAPIHelperInterop.MapiRecipDesc);
					int size = Marshal.SizeOf(type);

					// destroy all the structures in the memory area
					IntPtr ptr = _handle.Clone();
					for (int i = 0; i < _count; i++) {
						Marshal.DestroyStructure(ptr, type);
						ptr = new IntPtr(ptr.ToInt64() + size);
					}

					// free the memory
					Marshal.FreeHGlobal(_handle);

					_handle = IntPtr.Zero;
					_count = 0;
				}
			}

			#endregion Public Methods
		}
	}

	#endregion Public RecipientCollection Class
}