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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Common;

#endregion

namespace GreenshotPlugin.Core.Credentials
{
	/// <summary>
	///     The following code comes from: http://www.developerfusion.com/code/4693/using-the-credential-management-api/
	///     and is slightly modified so it works for us.
	///     As the "Stored usernames and passwords" which can be accessed by: Start-> Run and type "Control keymgr.dll"
	///     doesn't show all credentials use the tool here: http://www.microsoft.com/indonesia/msdn/credmgmt.aspx
	///     The following code is an example for a login, it will call the Authenticate with user/password
	///     which should return true if the login worked, false if not.
	///     private static bool Login(string system, string name) {
	///     try {
	///     CredentialsDialog dialog = new CredentialsDialog(system);
	///     dialog.Name = name;
	///     while (dialog.Show(dialog.Name) == DialogResult.OK) {
	///     if (Authenticate(dialog.Name, dialog.Password)) {
	///     if (dialog.SaveChecked) dialog.Confirm(true);
	///     return true;
	///     } else {
	///     try {
	///     dialog.Confirm(false);
	///     } catch (ApplicationException) {
	///     // exception handling ...
	///     }
	///     dialog.IncorrectPassword = true;
	///     }
	///     }
	///     } catch (ApplicationException) {
	///     // exception handling ...
	///     }
	///     return false;
	///     }
	/// </summary>
	/// <summary>Encapsulates dialog functionality from the Credential Management API.</summary>
	public sealed class CredentialsDialog
	{
		/// <summary>The only valid bitmap height (in pixels) of a user-defined banner.</summary>
		private const int ValidBannerHeight = 60;

		/// <summary>The only valid bitmap width (in pixels) of a user-defined banner.</summary>
		private const int ValidBannerWidth = 320;

		private Image _banner;

		private string _caption = string.Empty;

		private string _message = string.Empty;

		private string _name = string.Empty;

		private string _password = string.Empty;

		private string _target = string.Empty;

		/// <summary>
		///     Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog" /> class
		///     with the specified target.
		/// </summary>
		/// <param name="target">The name of the target for the credentials, typically a server name.</param>
		public CredentialsDialog(string target) : this(target, null)
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog" /> class
		///     with the specified target and caption.
		/// </summary>
		/// <param name="target">The name of the target for the credentials, typically a server name.</param>
		/// <param name="caption">The caption of the dialog (null will cause a system default title to be used).</param>
		public CredentialsDialog(string target, string caption) : this(target, caption, null)
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog" /> class
		///     with the specified target, caption and message.
		/// </summary>
		/// <param name="target">The name of the target for the credentials, typically a server name.</param>
		/// <param name="caption">The caption of the dialog (null will cause a system default title to be used).</param>
		/// <param name="message">The message of the dialog (null will cause a system default message to be used).</param>
		public CredentialsDialog(string target, string caption, string message) : this(target, caption, message, null)
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog" /> class
		///     with the specified target, caption, message and banner.
		/// </summary>
		/// <param name="target">The name of the target for the credentials, typically a server name.</param>
		/// <param name="caption">The caption of the dialog (null will cause a system default title to be used).</param>
		/// <param name="message">The message of the dialog (null will cause a system default message to be used).</param>
		/// <param name="banner">The image to display on the dialog (null will cause a system default image to be used).</param>
		public CredentialsDialog(string target, string caption, string message, Image banner)
		{
			Target = target;
			Caption = caption;
			Message = message;
			Banner = banner;
		}

		/// <summary>
		///     Gets or sets if the dialog will be shown even if the credentials
		///     can be returned from an existing credential in the credential manager.
		/// </summary>
		public bool AlwaysDisplay { get; set; }

		/// <summary>Gets or sets if the dialog is populated with name/password only.</summary>
		public bool ExcludeCertificates { get; set; } = true;

		/// <summary>Gets or sets if the credentials are to be persisted in the credential manager.</summary>
		public bool Persist { get; set; } = true;

		/// <summary>Gets or sets if the incorrect password balloontip needs to be shown. Introduced AFTER Windows XP</summary>
		public bool IncorrectPassword { get; set; }

		/// <summary>Gets or sets if the name is read-only.</summary>
		public bool KeepName { get; set; }

		/// <summary>Gets or sets the name for the credentials.</summary>
		public string Name
		{
			get { return _name; }
			set
			{
				if (value?.Length > CredUi.MAX_USERNAME_LENGTH)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The name has a maximum length of {0} characters.",
						CredUi.MAX_USERNAME_LENGTH);
					throw new ArgumentException(message, nameof(Name));
				}
				_name = value;
			}
		}

		/// <summary>Gets or sets the password for the credentials.</summary>
		public string Password
		{
			get { return _password; }
			set
			{
				if (value?.Length > CredUi.MAX_PASSWORD_LENGTH)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The password has a maximum length of {0} characters.",
						CredUi.MAX_PASSWORD_LENGTH);
					throw new ArgumentException(message, nameof(Password));
				}
				_password = value;
			}
		}

		/// <summary>Gets or sets if the save checkbox status.</summary>
		public bool SaveChecked { get; set; }

		/// <summary>Gets or sets if the save checkbox is displayed.</summary>
		/// <remarks>This value only has effect if Persist is true.</remarks>
		public bool SaveDisplayed { get; set; } = true;

		/// <summary>Gets or sets the name of the target for the credentials, typically a server name.</summary>
		public string Target
		{
			get { return _target; }
			set
			{
				if (value == null)
				{
					throw new ArgumentException("The target cannot be a null value.", nameof(Target));
				}
				if (value.Length > CredUi.MAX_GENERIC_TARGET_LENGTH)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The target has a maximum length of {0} characters.",
						CredUi.MAX_GENERIC_TARGET_LENGTH);
					throw new ArgumentException(message, nameof(Target));
				}
				_target = value;
			}
		}

		/// <summary>Gets or sets the caption of the dialog.</summary>
		/// <remarks>A null value will cause a system default caption to be used.</remarks>
		public string Caption
		{
			get { return _caption; }
			set
			{
				if (value?.Length > CredUi.MAX_CAPTION_LENGTH)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The caption has a maximum length of {0} characters.",
						CredUi.MAX_CAPTION_LENGTH);
					throw new ArgumentException(message, nameof(Caption));
				}
				_caption = value;
			}
		}

		/// <summary>Gets or sets the message of the dialog.</summary>
		/// <remarks>A null value will cause a system default message to be used.</remarks>
		public string Message
		{
			get { return _message; }
			set
			{
				if (value?.Length > CredUi.MAX_MESSAGE_LENGTH)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The message has a maximum length of {0} characters.",
						CredUi.MAX_MESSAGE_LENGTH);
					throw new ArgumentException(message, nameof(Message));
				}
				_message = value;
			}
		}

		/// <summary>Gets or sets the image to display on the dialog.</summary>
		/// <remarks>A null value will cause a system default image to be used.</remarks>
		public Image Banner
		{
			get { return _banner; }
			set
			{
				if (value != null)
				{
					if (value.Width != ValidBannerWidth)
					{
						throw new ArgumentException("The banner image width must be 320 pixels.", nameof(Banner));
					}
					if (value.Height != ValidBannerHeight)
					{
						throw new ArgumentException("The banner image height must be 60 pixels.", nameof(Banner));
					}
				}
				_banner = value;
			}
		}

		[DllImport("gdi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteObject(IntPtr hObject);

		/// <summary>Shows the credentials dialog.</summary>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show()
		{
			return Show(null, Name, Password, SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified save checkbox status.</summary>
		/// <param name="saveChecked">True if the save checkbox is checked.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(bool saveChecked)
		{
			return Show(null, Name, Password, saveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified name.</summary>
		/// <param name="name">The name for the credentials.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(string name)
		{
			return Show(null, name, Password, SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified name and password.</summary>
		/// <param name="name">The name for the credentials.</param>
		/// <param name="password">The password for the credentials.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(string name, string password)
		{
			return Show(null, name, password, SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified name, password and save checkbox status.</summary>
		/// <param name="name">The name for the credentials.</param>
		/// <param name="password">The password for the credentials.</param>
		/// <param name="saveChecked">True if the save checkbox is checked.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(string name, string password, bool saveChecked)
		{
			return Show(null, name, password, saveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified owner.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(IWin32Window owner)
		{
			return Show(owner, Name, Password, SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified owner and save checkbox status.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <param name="saveChecked">True if the save checkbox is checked.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(IWin32Window owner, bool saveChecked)
		{
			return Show(owner, Name, Password, saveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified owner, name and password.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <param name="name">The name for the credentials.</param>
		/// <param name="password">The password for the credentials.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(IWin32Window owner, string name, string password)
		{
			return Show(owner, name, password, SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified owner, name, password and save checkbox status.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <param name="name">The name for the credentials.</param>
		/// <param name="password">The password for the credentials.</param>
		/// <param name="saveChecked">True if the save checkbox is checked.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(IWin32Window owner, string name, string password, bool saveChecked)
		{
			if (WindowsVersion.IsWindowsXpOrLater)
			{
				throw new ApplicationException("The Credential Management API requires Windows XP / Windows Server 2003 or later.");
			}
			Name = name;
			Password = password;
			SaveChecked = saveChecked;

			return ShowDialog(owner);
		}

		/// <summary>Confirmation action to be applied.</summary>
		/// <param name="value">True if the credentials should be persisted.</param>
		public void Confirm(bool value)
		{
			var confirmResult = CredUi.CredUIConfirmCredentials(Target, value);
			switch (confirmResult)
			{
				case CredUIReturnCodes.NO_ERROR:
					break;
				case CredUIReturnCodes.ERROR_INVALID_PARAMETER:
					// for some reason, this is encountered when credentials are overwritten
					break;
				default:
					throw new ApplicationException($"Credential confirmation failed: {confirmResult}");
			}
		}

		/// <summary>Returns a DialogResult indicating the user action.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <remarks>
		///     Sets the name, password and SaveChecked accessors to the state of the dialog as it was dismissed by the user.
		/// </remarks>
		private DialogResult ShowDialog(IWin32Window owner)
		{
			// set the api call parameters
			var name = new StringBuilder(CredUi.MAX_USERNAME_LENGTH);
			name.Append(Name);

			var password = new StringBuilder(CredUi.MAX_PASSWORD_LENGTH);
			password.Append(Password);

			var saveChecked = Convert.ToInt32(SaveChecked);

			var info = GetInfo(owner);
			var credFlags = GetFlags();

			// make the api call
			var code = CredUi.CredUIPromptForCredentials(
				ref info,
				Target,
				IntPtr.Zero, 0,
				name, CredUi.MAX_USERNAME_LENGTH,
				password, CredUi.MAX_PASSWORD_LENGTH,
				ref saveChecked,
				credFlags
			);

			// clean up resources
			if (Banner != null)
			{
				DeleteObject(info.hbmBanner);
			}

			// set the accessors from the api call parameters
			Name = name.ToString();
			Password = password.ToString();
			SaveChecked = Convert.ToBoolean(saveChecked);

			return GetDialogResult(code);
		}

		/// <summary>Returns the info structure for dialog display settings.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		private CredUiInfo GetInfo(IWin32Window owner)
		{
			var info = new CredUiInfo();
			if (owner != null)
			{
				info.hwndParent = owner.Handle;
			}
			info.pszCaptionText = Caption;
			info.pszMessageText = Message;
			if (Banner != null)
			{
				info.hbmBanner = new Bitmap(Banner, ValidBannerWidth, ValidBannerHeight).GetHbitmap();
			}
			info.cbSize = Marshal.SizeOf(info);
			return info;
		}

		/// <summary>Returns the flags for dialog display options.</summary>
		private CredFlags GetFlags()
		{
			var credFlags = CredFlags.GENERIC_CREDENTIALS;

			if (IncorrectPassword)
			{
				credFlags = credFlags | CredFlags.INCORRECT_PASSWORD;
			}

			if (AlwaysDisplay)
			{
				credFlags = credFlags | CredFlags.ALWAYS_SHOW_UI;
			}

			if (ExcludeCertificates)
			{
				credFlags = credFlags | CredFlags.EXCLUDE_CERTIFICATES;
			}

			if (Persist)
			{
				credFlags = credFlags | CredFlags.EXPECT_CONFIRMATION;
				if (SaveDisplayed)
				{
					credFlags = credFlags | CredFlags.SHOW_SAVE_CHECK_BOX;
				}
				else
				{
					credFlags = credFlags | CredFlags.PERSIST;
				}
			}
			else
			{
				credFlags = credFlags | CredFlags.DO_NOT_PERSIST;
			}

			if (KeepName)
			{
				credFlags = credFlags | CredFlags.KEEP_USERNAME;
			}

			return credFlags;
		}

		/// <summary>Returns a DialogResult from the specified code.</summary>
		/// <param name="code">The credential return code.</param>
		private DialogResult GetDialogResult(CredUIReturnCodes code)
		{
			DialogResult result;
			switch (code)
			{
				case CredUIReturnCodes.NO_ERROR:
					result = DialogResult.OK;
					break;
				case CredUIReturnCodes.ERROR_CANCELLED:
					result = DialogResult.Cancel;
					break;
				case CredUIReturnCodes.ERROR_NO_SUCH_LOGON_SESSION:
					throw new ApplicationException("No such logon session.");
				case CredUIReturnCodes.ERROR_NOT_FOUND:
					throw new ApplicationException("Not found.");
				case CredUIReturnCodes.ERROR_INVALID_ACCOUNT_NAME:
					throw new ApplicationException("Invalid account name.");
				case CredUIReturnCodes.ERROR_INSUFFICIENT_BUFFER:
					throw new ApplicationException("Insufficient buffer.");
				case CredUIReturnCodes.ERROR_INVALID_PARAMETER:
					throw new ApplicationException("Invalid parameter.");
				case CredUIReturnCodes.ERROR_INVALID_FLAGS:
					throw new ApplicationException("Invalid flags.");
				default:
					throw new ApplicationException("Unknown credential result encountered.");
			}
			return result;
		}
	}
}