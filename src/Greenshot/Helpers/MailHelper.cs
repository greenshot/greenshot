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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addons.Config.Impl;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

#endregion

namespace Greenshot.Helpers
{
	/// <summary>
	///     Author: Andrew Baker
	///     Datum: 10.03.2006
	///     Available from <a href="http://www.vbusers.com/codecsharp/codeget.asp?ThreadID=71&PostID=1">here</a>
	/// </summary>
	/// <summary>
	///     Represents an email message to be sent through MAPI.
	/// </summary>
	public class MapiMailMessage : IDisposable
	{
		#region Enums

		/// <summary>
		///     Specifies the valid RecipientTypes for a Recipient.
		/// </summary>
		public enum RecipientType
		{
			/// <summary>
			///     Recipient will be in the TO list.
			/// </summary>
			To = 1,

			/// <summary>
			///     Recipient will be in the CC list.
			/// </summary>
			CC = 2,

			/// <summary>
			///     Recipient will be in the BCC list.
			/// </summary>
			BCC = 3
		}

		#endregion Enums

		private static readonly LogSource Log = new LogSource();
        // TODO: Solve, was static reference!
        private static readonly ICoreConfiguration CoreConfig = new CoreConfigurationImpl();

        #region Member Variables

        private readonly ManualResetEvent _manualResetEvent;

		#endregion Member Variables

		/// <summary>
		///     Helper Method for creating an Email with Attachment
		/// </summary>
		/// <param name="fullPath">Path to file</param>
		/// <param name="title"></param>
		public static void SendImage(string fullPath, string title)
		{
			using (var message = new MapiMailMessage(title, null))
			{
				message.Files.Add(fullPath);
				if (!string.IsNullOrEmpty(CoreConfig.MailApiTo))
				{
					message.Recipients.Add(new Recipient(CoreConfig.MailApiTo, RecipientType.To));
				}
				if (!string.IsNullOrEmpty(CoreConfig.MailApiCC))
				{
					message.Recipients.Add(new Recipient(CoreConfig.MailApiCC, RecipientType.CC));
				}
				if (!string.IsNullOrEmpty(CoreConfig.MailApiBCC))
				{
					message.Recipients.Add(new Recipient(CoreConfig.MailApiBCC, RecipientType.BCC));
				}
				message.ShowDialog();
			}
		}


		/// <summary>
		///     Helper Method for creating an Email with Image Attachment
		/// </summary>
		/// <param name="surface">The image to send</param>
		/// <param name="captureDetails">ICaptureDetails</param>
		public static void SendImage(ISurface surface, ICaptureDetails captureDetails)
		{
			var tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings());

			if (tmpFile != null)
			{
				// Store the list of currently active windows, so we can make sure we show the email window later!
				//bool isEmailSend = false;
				//if (EmailConfigHelper.HasOutlook() && (CoreConfig.OutputEMailFormat == EmailFormats.Html || CoreConfig.OutputEMailFormat == EmailFormats.Text)) {
				//	isEmailSend = OutlookExporter.ExportToOutlook(tmpFile, captureDetails);
				//}
				if ( /*!isEmailSend &&*/ EmailConfigHelper.HasMapi())
				{
					// Fallback to MAPI
					// Send the email
					SendImage(tmpFile, captureDetails.Title);
				}
			}
		}

		#region Private MapiFileDescriptor Class

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		private class MapiFileDescriptor
		{
			public int flags = 0;
			public string name;
			public string path;
			public int position;
			public int reserved = 0;
			public IntPtr type = IntPtr.Zero;
		}

		#endregion Private MapiFileDescriptor Class

		#region Private MAPIHelperInterop Class

		/// <summary>
		///     Internal class for calling MAPI APIs
		/// </summary>
		internal class MapiHelperInterop
		{
			#region Constructors

			/// <summary>
			///     Private constructor.
			/// </summary>
			private MapiHelperInterop()
			{
				// Intenationally blank
			}

			#endregion Constructors

			#region Structs

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
			public class MapiMessage
			{
				public string ConversationID = null;
				public string DateReceived = null;
				public int FileCount;
				public IntPtr Files = IntPtr.Zero;
				public int Flags = 0;
				public string MessageType = null;
				public string NoteText;
				public IntPtr Originator = IntPtr.Zero;
				public int RecipientCount;
				public IntPtr Recipients = IntPtr.Zero;
				public int Reserved = 0;
				public string Subject;
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
			public class MapiRecipDesc
			{
				public string Address;
				public int eIDSize = 0;
				public IntPtr EntryID = IntPtr.Zero;
				public string Name;
				public int RecipientClass;
				public int Reserved = 0;
			}

			[DllImport("MAPI32.DLL", SetLastError = true, CharSet = CharSet.Ansi)]
			public static extern int MAPISendMail(IntPtr session, IntPtr hwnd, MapiMessage message, int flg, int rsv);

			#endregion Structs
		}

		#endregion Private MAPIHelperInterop Class

		#region Constructors

		/// <summary>
		///     Creates a blank mail message.
		/// </summary>
		public MapiMailMessage()
		{
			Files = new List<string>();
			Recipients = new RecipientCollection();
			_manualResetEvent = new ManualResetEvent(false);
		}

		/// <summary>
		///     Creates a new mail message with the specified subject.
		/// </summary>
		public MapiMailMessage(string subject) : this()
		{
			Subject = subject;
		}

		/// <summary>
		///     Creates a new mail message with the specified subject and body.
		/// </summary>
		public MapiMailMessage(string subject, string body) : this()
		{
			Subject = subject;
			Body = body;
		}

		#endregion Constructors

		#region Public Properties

		/// <summary>
		///     Gets or sets the subject of this mail message.
		/// </summary>
		public string Subject { get; set; }

		/// <summary>
		///     Gets or sets the body of this mail message.
		/// </summary>
		public string Body { get; set; }

		/// <summary>
		///     Gets the recipient list for this mail message.
		/// </summary>
		public RecipientCollection Recipients { get; private set; }

		/// <summary>
		///     Gets the file list for this mail message.
		/// </summary>
		public List<string> Files { get; }

		#endregion Public Properties

		#region Public Methods

		/// <summary>
		///     Displays the mail message dialog asynchronously.
		/// </summary>
		public void ShowDialog()
		{
			// Create the mail message in an STA thread
			var thread = new Thread(_ShowMail)
			{
				IsBackground = true,
				Name = "Create MAPI mail"
			};
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			// only return when the new thread has built it's interop representation
			_manualResetEvent.WaitOne();
			_manualResetEvent.Reset();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion Public Methods

		#region Private Methods

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			_manualResetEvent?.Close();
		}

		/// <summary>
		///     Sends the mail message.
		/// </summary>
		private void _ShowMail()
		{
			var message = new MapiHelperInterop.MapiMessage();

			using (var interopRecipients = Recipients.GetInteropRepresentation())
			{
				message.Subject = Subject;
				message.NoteText = Body;

				message.Recipients = interopRecipients.Handle;
				message.RecipientCount = Recipients.Count;

				// Check if we need to add attachments
				if (Files.Count > 0)
				{
					// Add attachments
					message.Files = _AllocAttachments(out message.FileCount);
				}

				// Signal the creating thread (make the remaining code async)
				_manualResetEvent.Set();

				const int MAPI_DIALOG = 0x8;
				//const int MAPI_LOGON_UI = 0x1;
				var error = MapiHelperInterop.MAPISendMail(IntPtr.Zero, IntPtr.Zero, message, MAPI_DIALOG, 0);

				if (Files.Count > 0)
				{
					// Deallocate the files
					_DeallocFiles(message);
				}
				var errorCode = (MapiCodes) Enum.ToObject(typeof(MapiCodes), error);

				// Check for error
				if (errorCode == MapiCodes.SUCCESS || errorCode == MapiCodes.USER_ABORT)
				{
					return;
				}
				var errorText = GetMapiError(errorCode);
				Log.Error().WriteLine(null, "Error sending MAPI Email. Error: " + errorText + " (code = " + errorCode + ").");
				MessageBox.Show(errorText, "Mail (MAPI) destination", MessageBoxButtons.OK, MessageBoxIcon.Error);
				// Recover from bad settings, show again
				if (errorCode != MapiCodes.INVALID_RECIPS)
				{
					return;
				}
				Recipients = new RecipientCollection();
				_ShowMail();
			}
		}

		/// <summary>
		///     Deallocates the files in a message.
		/// </summary>
		/// <param name="message">The message to deallocate the files from.</param>
		private void _DeallocFiles(MapiHelperInterop.MapiMessage message)
		{
			if (message.Files != IntPtr.Zero)
			{
				var fileDescType = typeof(MapiFileDescriptor);
				var fsize = Marshal.SizeOf(fileDescType);

				// Get the ptr to the files
				var runptr = message.Files;
				// Release each file
				for (var i = 0; i < message.FileCount; i++)
				{
					Marshal.DestroyStructure(runptr, fileDescType);
					runptr = new IntPtr(runptr.ToInt64() + fsize);
				}
				// Release the file
				Marshal.FreeHGlobal(message.Files);
			}
		}

		/// <summary>
		///     Allocates the file attachments
		/// </summary>
		/// <param name="fileCount"></param>
		/// <returns></returns>
		private IntPtr _AllocAttachments(out int fileCount)
		{
			fileCount = 0;
			if (Files == null)
			{
				return IntPtr.Zero;
			}
			if (Files.Count <= 0 || Files.Count > 100)
			{
				return IntPtr.Zero;
			}

			var atype = typeof(MapiFileDescriptor);
			var asize = Marshal.SizeOf(atype);
			var ptra = Marshal.AllocHGlobal(Files.Count * asize);

			var mfd = new MapiFileDescriptor
			{
				position = -1
			};
			var runptr = ptra;
			foreach (var path in Files)
			{
				mfd.name = Path.GetFileName(path);
				mfd.path = path;
				Marshal.StructureToPtr(mfd, runptr, false);
				runptr = new IntPtr(runptr.ToInt64() + asize);
			}

			fileCount = Files.Count;
			return ptra;
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private enum MapiCodes
		{
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
		///     Logs any Mapi errors.
		/// </summary>
		private string GetMapiError(MapiCodes errorCode)
		{
			var error = string.Empty;

			switch (errorCode)
			{
				case MapiCodes.USER_ABORT:
					error = "User Aborted.";
					break;
				case MapiCodes.FAILURE:
					error = "MAPI Failure.";
					break;
				case MapiCodes.LOGIN_FAILURE:
					error = "Login Failure.";
					break;
				case MapiCodes.DISK_FULL:
					error = "MAPI Disk full.";
					break;
				case MapiCodes.INSUFFICIENT_MEMORY:
					error = "MAPI Insufficient memory.";
					break;
				case MapiCodes.BLK_TOO_SMALL:
					error = "MAPI Block too small.";
					break;
				case MapiCodes.TOO_MANY_SESSIONS:
					error = "MAPI Too many sessions.";
					break;
				case MapiCodes.TOO_MANY_FILES:
					error = "MAPI too many files.";
					break;
				case MapiCodes.TOO_MANY_RECIPIENTS:
					error = "MAPI too many recipients.";
					break;
				case MapiCodes.ATTACHMENT_NOT_FOUND:
					error = "MAPI Attachment not found.";
					break;
				case MapiCodes.ATTACHMENT_OPEN_FAILURE:
					error = "MAPI Attachment open failure.";
					break;
				case MapiCodes.ATTACHMENT_WRITE_FAILURE:
					error = "MAPI Attachment Write Failure.";
					break;
				case MapiCodes.UNKNOWN_RECIPIENT:
					error = "MAPI Unknown recipient.";
					break;
				case MapiCodes.BAD_RECIPTYPE:
					error = "MAPI Bad recipient type.";
					break;
				case MapiCodes.NO_MESSAGES:
					error = "MAPI No messages.";
					break;
				case MapiCodes.INVALID_MESSAGE:
					error = "MAPI Invalid message.";
					break;
				case MapiCodes.TEXT_TOO_LARGE:
					error = "MAPI Text too large.";
					break;
				case MapiCodes.INVALID_SESSION:
					error = "MAPI Invalid session.";
					break;
				case MapiCodes.TYPE_NOT_SUPPORTED:
					error = "MAPI Type not supported.";
					break;
				case MapiCodes.AMBIGUOUS_RECIPIENT:
					error = "MAPI Ambiguous recipient.";
					break;
				case MapiCodes.MESSAGE_IN_USE:
					error = "MAPI Message in use.";
					break;
				case MapiCodes.NETWORK_FAILURE:
					error = "MAPI Network failure.";
					break;
				case MapiCodes.INVALID_EDITFIELDS:
					error = "MAPI Invalid edit fields.";
					break;
				case MapiCodes.INVALID_RECIPS:
					error = "MAPI Invalid Recipients.";
					break;
				case MapiCodes.NOT_SUPPORTED:
					error = "MAPI Not supported.";
					break;
				case MapiCodes.NO_LIBRARY:
					error = "MAPI No Library.";
					break;
				case MapiCodes.INVALID_PARAMETER:
					error = "MAPI Invalid parameter.";
					break;
			}
			return error;
		}

		#endregion Private Methods
	}

	#region Public Recipient Class

	/// <summary>
	///     Represents a Recipient for a MapiMailMessage.
	/// </summary>
	public class Recipient
	{
		#region Internal Methods

		/// <summary>
		///     Returns an interop representation of a recepient.
		/// </summary>
		/// <returns></returns>
		internal MapiMailMessage.MapiHelperInterop.MapiRecipDesc GetInteropRepresentation()
		{
			var interop = new MapiMailMessage.MapiHelperInterop.MapiRecipDesc();

			if (DisplayName == null)
			{
				interop.Name = Address;
			}
			else
			{
				interop.Name = DisplayName;
				interop.Address = Address;
			}

			interop.RecipientClass = (int) RecipientType;

			return interop;
		}

		#endregion Internal Methods

		#region Public Properties

		/// <summary>
		///     The email address of this recipient.
		/// </summary>
		public string Address;

		/// <summary>
		///     The display name of this recipient.
		/// </summary>
		public string DisplayName;

		/// <summary>
		///     How the recipient will receive this message (To, CC, BCC).
		/// </summary>
		public MapiMailMessage.RecipientType RecipientType = MapiMailMessage.RecipientType.To;

		#endregion Public Properties

		#region Constructors

		/// <summary>
		///     Creates a new recipient with the specified address.
		/// </summary>
		public Recipient(string address)
		{
			Address = address;
		}

		/// <summary>
		///     Creates a new recipient with the specified address and display name.
		/// </summary>
		public Recipient(string address, string displayName)
		{
			Address = address;
			DisplayName = displayName;
		}

		/// <summary>
		///     Creates a new recipient with the specified address and recipient type.
		/// </summary>
		public Recipient(string address, MapiMailMessage.RecipientType recipientType)
		{
			Address = address;
			RecipientType = recipientType;
		}

		/// <summary>
		///     Creates a new recipient with the specified address, display name and recipient type.
		/// </summary>
		public Recipient(string address, string displayName, MapiMailMessage.RecipientType recipientType)
		{
			Address = address;
			DisplayName = displayName;
			RecipientType = recipientType;
		}

		#endregion Constructors
	}

	#endregion Public Recipient Class

	#region Public RecipientCollection Class

	/// <summary>
	///     Represents a colleciton of recipients for a mail message.
	/// </summary>
	public class RecipientCollection : CollectionBase
	{
		/// <summary>
		///     Returns the recipient stored in this collection at the specified index.
		/// </summary>
		public Recipient this[int index] => (Recipient) List[index];

		/// <summary>
		///     Adds the specified recipient to this collection.
		/// </summary>
		public void Add(Recipient value)
		{
			List.Add(value);
		}

		/// <summary>
		///     Adds a new recipient with the specified address to this collection.
		/// </summary>
		public void Add(string address)
		{
			Add(new Recipient(address));
		}

		/// <summary>
		///     Adds a new recipient with the specified address and display name to this collection.
		/// </summary>
		public void Add(string address, string displayName)
		{
			Add(new Recipient(address, displayName));
		}

		/// <summary>
		///     Adds a new recipient with the specified address and recipient type to this collection.
		/// </summary>
		public void Add(string address, MapiMailMessage.RecipientType recipientType)
		{
			Add(new Recipient(address, recipientType));
		}

		/// <summary>
		///     Adds a new recipient with the specified address, display name and recipient type to this collection.
		/// </summary>
		public void Add(string address, string displayName, MapiMailMessage.RecipientType recipientType)
		{
			Add(new Recipient(address, displayName, recipientType));
		}

		internal InteropRecipientCollection GetInteropRepresentation()
		{
			return new InteropRecipientCollection(this);
		}

		/// <summary>
		///     Struct which contains an interop representation of a colleciton of recipients.
		/// </summary>
		internal struct InteropRecipientCollection : IDisposable
		{
			#region Member Variables

			private int _count;

			#endregion Member Variables

			#region Constructors

			/// <summary>
			///     Default constructor for creating InteropRecipientCollection.
			/// </summary>
			/// <param name="outer"></param>
			public InteropRecipientCollection(RecipientCollection outer)
			{
				_count = outer.Count;

				if (_count == 0)
				{
					Handle = IntPtr.Zero;
					return;
				}

				// allocate enough memory to hold all recipients
				var size = Marshal.SizeOf(typeof(MapiMailMessage.MapiHelperInterop.MapiRecipDesc));
				Handle = Marshal.AllocHGlobal(_count * size);

				// place all interop recipients into the memory just allocated
				var ptr = Handle;
				foreach (Recipient native in outer)
				{
					var interop = native.GetInteropRepresentation();

					// stick it in the memory block
					Marshal.StructureToPtr(interop, ptr, false);
					ptr = new IntPtr(ptr.ToInt64() + size);
				}
			}

			#endregion Costructors

			#region Public Properties

			public IntPtr Handle { get; private set; }

			#endregion Public Properties

			#region Public Methods

			/// <summary>
			///     Disposes of resources.
			/// </summary>
			public void Dispose()
			{
				if (Handle != IntPtr.Zero)
				{
					var type = typeof(MapiMailMessage.MapiHelperInterop.MapiRecipDesc);
					var size = Marshal.SizeOf(type);

					// destroy all the structures in the memory area
					var ptr = Handle;
					for (var i = 0; i < _count; i++)
					{
						Marshal.DestroyStructure(ptr, type);
						ptr = new IntPtr(ptr.ToInt64() + size);
					}

					// free the memory
					Marshal.FreeHGlobal(Handle);

					Handle = IntPtr.Zero;
					_count = 0;
				}
			}

			#endregion Public Methods
		}
	}

	#endregion Public RecipientCollection Class
}