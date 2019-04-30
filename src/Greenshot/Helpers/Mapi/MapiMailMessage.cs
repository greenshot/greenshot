// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

namespace Greenshot.Helpers.Mapi
{
    /// <summary>
    ///     Author: Andrew Baker
    ///     Datum: 10.03.2006
    ///     Available from <a href="http://www.vbusers.com/codecsharp/codeget.asp?ThreadID=71">here</a>
    /// </summary>
    /// <summary>
    ///     Represents an email message to be sent through MAPI.
    /// </summary>
    public class MapiMailMessage : IDisposable
	{
		private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// Set from DI via AddonsModule
        /// </summary>
        internal static ICoreConfiguration CoreConfiguration { get; set; }

        private readonly ManualResetEvent _manualResetEvent;

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
				if (!string.IsNullOrEmpty(CoreConfiguration.MailApiTo))
				{
					message.Recipients.Add(new Recipient(CoreConfiguration.MailApiTo, RecipientType.To));
				}
				if (!string.IsNullOrEmpty(CoreConfiguration.MailApiCC))
				{
					message.Recipients.Add(new Recipient(CoreConfiguration.MailApiCC, RecipientType.CC));
				}
				if (!string.IsNullOrEmpty(CoreConfiguration.MailApiBCC))
				{
					message.Recipients.Add(new Recipient(CoreConfiguration.MailApiBCC, RecipientType.BCC));
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
			var tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings(CoreConfiguration));

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

        /// <inheritdoc />
        public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

        /// <summary>
        /// Actual dispose implementation
        /// </summary>
        /// <param name="disposing">bool</param>
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
			var message = new MapiMessage();

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

				const int mapiDialog = 0x8;
				//const int MAPI_LOGON_UI = 0x1;
				var error = MapiHelperInterop.MAPISendMail(IntPtr.Zero, IntPtr.Zero, message, mapiDialog, 0);

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
		private void _DeallocFiles(MapiMessage message)
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
    }
}