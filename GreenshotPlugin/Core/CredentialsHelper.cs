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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

/// <summary>
/// The following code comes from: http://www.developerfusion.com/code/4693/using-the-credential-management-api/
/// and is slightly modified so it works for us.
/// As the "Stored usernames and passwords" which can be accessed by: Start-> Run and type "Control keymgr.dll"
/// doesn't show all credentials use the tool here: http://www.microsoft.com/indonesia/msdn/credmgmt.aspx
/// The following code is an example for a login, it will call the Authenticate with user/password
/// which should return true if the login worked, false if not.
///		private static bool Login(string system, string name) {
///			try {
///				CredentialsDialog dialog = new CredentialsDialog(system);
///				dialog.Name = name;
///				while (dialog.Show(dialog.Name) == DialogResult.OK) {
///					if (Authenticate(dialog.Name, dialog.Password)) {
///						if (dialog.SaveChecked) dialog.Confirm(true);
///						return true;
///					} else {
///						try {
///							dialog.Confirm(false);
///						} catch (ApplicationException) {
///							// exception handling ...
///						}
///						dialog.IncorrectPassword = true;
///					}
///				}
///			} catch (ApplicationException) {
///				// exception handling ...
///			}
///			return false;
///		}
/// </summary>
namespace GreenshotPlugin.Core {
	/// <summary>Encapsulates dialog functionality from the Credential Management API.</summary>
	public sealed class CredentialsDialog {
		[DllImport("gdi32.dll", SetLastError=true)]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>The only valid bitmap height (in pixels) of a user-defined banner.</summary>
		private const int ValidBannerHeight = 60;

		/// <summary>The only valid bitmap width (in pixels) of a user-defined banner.</summary>
		private const int ValidBannerWidth = 320;

		/// <summary>Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog"/> class
		/// with the specified target.</summary>
		/// <param name="target">The name of the target for the credentials, typically a server name.</param>
		public CredentialsDialog(string target) : this(target, null) {
		}

		/// <summary>Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog"/> class
		/// with the specified target and caption.</summary>
		/// <param name="target">The name of the target for the credentials, typically a server name.</param>
		/// <param name="caption">The caption of the dialog (null will cause a system default title to be used).</param>
		public CredentialsDialog(string target, string caption) : this(target, caption, null) {
		}

		/// <summary>Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog"/> class
		/// with the specified target, caption and message.</summary>
		/// <param name="target">The name of the target for the credentials, typically a server name.</param>
		/// <param name="caption">The caption of the dialog (null will cause a system default title to be used).</param>
		/// <param name="message">The message of the dialog (null will cause a system default message to be used).</param>
		public CredentialsDialog(string target, string caption, string message) : this(target, caption, message, null) {
		}

		/// <summary>Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog"/> class
		/// with the specified target, caption, message and banner.</summary>
		/// <param name="target">The name of the target for the credentials, typically a server name.</param>
		/// <param name="caption">The caption of the dialog (null will cause a system default title to be used).</param>
		/// <param name="message">The message of the dialog (null will cause a system default message to be used).</param>
		/// <param name="banner">The image to display on the dialog (null will cause a system default image to be used).</param>
		public CredentialsDialog(string target, string caption, string message, Image banner) {
			this.Target = target;
			this.Caption = caption;
			this.Message = message;
			this.Banner = banner;
		}

		private bool _alwaysDisplay = false;
		/// <summary>
		/// Gets or sets if the dialog will be shown even if the credentials
		/// can be returned from an existing credential in the credential manager.
		/// </summary>
		public bool AlwaysDisplay {
			get {
				return _alwaysDisplay;
			}
			set {
				_alwaysDisplay = value;
			}
		}

		private bool _excludeCertificates = true;
		/// <summary>Gets or sets if the dialog is populated with name/password only.</summary>
		public bool ExcludeCertificates {
			get {
				return _excludeCertificates;
			}
			set {
				_excludeCertificates = value;
			}
		}

		private bool _persist = true;
		/// <summary>Gets or sets if the credentials are to be persisted in the credential manager.</summary>
		public bool Persist {
			get {
				return _persist;
			}
			set {
				_persist = value;
			}
		}

		private bool _incorrectPassword = false;
		/// <summary>Gets or sets if the incorrect password balloontip needs to be shown. Introduced AFTER Windows XP</summary>Gets></summary>
		public bool IncorrectPassword {
			get {
				return _incorrectPassword;
			}
			set {
				_incorrectPassword = value;
			}
		}

		private bool _keepName = false;
		/// <summary>Gets or sets if the name is read-only.</summary>
		public bool KeepName {
			get {
				return _keepName;
			}
			set {
				_keepName = value;
			}
		}

		private string _name = String.Empty;
		/// <summary>Gets or sets the name for the credentials.</summary>
		public string Name {
			get {
				return _name;
			}
			set {
				if (value != null) {
					if (value.Length > CREDUI.MAX_USERNAME_LENGTH) {
						string message = String.Format(
							Thread.CurrentThread.CurrentUICulture,
							"The name has a maximum length of {0} characters.",
							CREDUI.MAX_USERNAME_LENGTH);
						throw new ArgumentException(message, "Name");
					}
				}
				_name = value;
			}
		}

		private string _password = String.Empty;
		/// <summary>Gets or sets the password for the credentials.</summary>
		public string Password {
			get {
				return _password;
			}
			set {
				if (value != null) {
					if (value.Length > CREDUI.MAX_PASSWORD_LENGTH) {
						string message = String.Format(
							Thread.CurrentThread.CurrentUICulture,
							"The password has a maximum length of {0} characters.",
							CREDUI.MAX_PASSWORD_LENGTH);
						throw new ArgumentException(message, "Password");
					}
				}
				_password = value;
			}
		}

		private bool _saveChecked = false;
		/// <summary>Gets or sets if the save checkbox status.</summary>
		public bool SaveChecked {
			get {
				return _saveChecked;
			}
			set {
				_saveChecked = value;
			}
		}

		private bool _saveDisplayed = true;
		/// <summary>Gets or sets if the save checkbox is displayed.</summary>
		/// <remarks>This value only has effect if Persist is true.</remarks>
		public bool SaveDisplayed {
			get {
				return _saveDisplayed;
			}
			set {
				_saveDisplayed = value;
			}
		}

		private string _target = String.Empty;
		/// <summary>Gets or sets the name of the target for the credentials, typically a server name.</summary>
		public string Target {
			get {
				return _target;
			}
			set {
				if (value == null) {
					throw new ArgumentException("The target cannot be a null value.", "Target");
				} else if (value.Length > CREDUI.MAX_GENERIC_TARGET_LENGTH) {
					string message = String.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The target has a maximum length of {0} characters.",
						CREDUI.MAX_GENERIC_TARGET_LENGTH);
					throw new ArgumentException(message, "Target");
				}
				_target = value;
			}
		}

		private string _caption = String.Empty;
		/// <summary>Gets or sets the caption of the dialog.</summary>
		/// <remarks>A null value will cause a system default caption to be used.</remarks>
		public string Caption {
			get {
				return _caption;
			}
			set {
				if (value != null) {
					if (value.Length > CREDUI.MAX_CAPTION_LENGTH) {
						string message = String.Format(
							Thread.CurrentThread.CurrentUICulture,
							"The caption has a maximum length of {0} characters.",
							CREDUI.MAX_CAPTION_LENGTH);
						throw new ArgumentException(message, "Caption");
					}
				}
				_caption = value;
			}
		}

		private string _message = String.Empty;
		/// <summary>Gets or sets the message of the dialog.</summary>
		/// <remarks>A null value will cause a system default message to be used.</remarks>
		public string Message {
			get {
				return _message;
			}
			set {
				if (value != null) {
					if (value.Length > CREDUI.MAX_MESSAGE_LENGTH) {
						string message = String.Format(
							Thread.CurrentThread.CurrentUICulture,
							"The message has a maximum length of {0} characters.",
							CREDUI.MAX_MESSAGE_LENGTH);
						throw new ArgumentException(message, "Message");
					}
				}
				_message = value;
			}
		}

		private Image _banner = null;
		/// <summary>Gets or sets the image to display on the dialog.</summary>
		/// <remarks>A null value will cause a system default image to be used.</remarks>
		public Image Banner {
			get {
				return _banner;
			}
			set {
				if (value != null) {
					if (value.Width != ValidBannerWidth) {
						throw new ArgumentException("The banner image width must be 320 pixels.", "Banner");
					}
					if (value.Height != ValidBannerHeight) {
						throw new ArgumentException("The banner image height must be 60 pixels.", "Banner");
					}
				}
				_banner = value;
			}
		}

		/// <summary>Shows the credentials dialog.</summary>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show() {
			return Show(null, this.Name, this.Password, this.SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified save checkbox status.</summary>
		/// <param name="saveChecked">True if the save checkbox is checked.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(bool saveChecked) {
			return Show(null, this.Name, this.Password, saveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified name.</summary>
		/// <param name="name">The name for the credentials.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(string name) {
			return Show(null, name, this.Password, this.SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified name and password.</summary>
		/// <param name="name">The name for the credentials.</param>
		/// <param name="password">The password for the credentials.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(string name, string password) {
			return Show(null, name, password, this.SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified name, password and save checkbox status.</summary>
		/// <param name="name">The name for the credentials.</param>
		/// <param name="password">The password for the credentials.</param>
		/// <param name="saveChecked">True if the save checkbox is checked.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(string name, string password, bool saveChecked) {
			return Show(null, name, password, saveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified owner.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(IWin32Window owner) {
			return Show(owner, this.Name, this.Password, this.SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified owner and save checkbox status.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <param name="saveChecked">True if the save checkbox is checked.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(IWin32Window owner, bool saveChecked) {
			return Show(owner, this.Name, this.Password, saveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified owner, name and password.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <param name="name">The name for the credentials.</param>
		/// <param name="password">The password for the credentials.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(IWin32Window owner, string name, string password) {
			return Show(owner, name, password, this.SaveChecked);
		}

		/// <summary>Shows the credentials dialog with the specified owner, name, password and save checkbox status.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <param name="name">The name for the credentials.</param>
		/// <param name="password">The password for the credentials.</param>
		/// <param name="saveChecked">True if the save checkbox is checked.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(IWin32Window owner, string name, string password, bool saveChecked) {
			if ((Environment.OSVersion.Version.Major < 5) || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor < 1))) {
				throw new ApplicationException("The Credential Management API requires Windows XP / Windows Server 2003 or later.");
			}
			this.Name = name;
			this.Password = password;
			this.SaveChecked = saveChecked;
		   
			return ShowDialog(owner);
		}

		/// <summary>Confirmation action to be applied.</summary>
		/// <param name="value">True if the credentials should be persisted.</param>
		public void Confirm(bool value) {
			switch (CREDUI.ConfirmCredentials(this.Target, value)) {
				case CREDUI.ReturnCodes.NO_ERROR:
					break;
			   
				case CREDUI.ReturnCodes.ERROR_INVALID_PARAMETER:
					// for some reason, this is encountered when credentials are overwritten
					break;
			   
				default:
					throw new ApplicationException("Credential confirmation failed.");
			}
		}

		/// <summary>Returns a DialogResult indicating the user action.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <remarks>
		/// Sets the name, password and SaveChecked accessors to the state of the dialog as it was dismissed by the user.
		/// </remarks>
		private DialogResult ShowDialog(IWin32Window owner) {
			// set the api call parameters
			StringBuilder name = new StringBuilder(CREDUI.MAX_USERNAME_LENGTH);
			name.Append(this.Name);

			StringBuilder password = new StringBuilder(CREDUI.MAX_PASSWORD_LENGTH);
			password.Append(this.Password);

			int saveChecked = Convert.ToInt32(this.SaveChecked);

			CREDUI.INFO info = GetInfo(owner);
			CREDUI.FLAGS flags = GetFlags();

			// make the api call
			CREDUI.ReturnCodes code = CREDUI.PromptForCredentials(
				ref info,
				this.Target,
				IntPtr.Zero, 0,
				name, CREDUI.MAX_USERNAME_LENGTH,
				password, CREDUI.MAX_PASSWORD_LENGTH,
				ref saveChecked,
				flags
			);

			// clean up resources
			if (this.Banner != null) {
				DeleteObject(info.hbmBanner);
			}

			// set the accessors from the api call parameters
			this.Name = name.ToString();
			this.Password = password.ToString();
			this.SaveChecked = Convert.ToBoolean(saveChecked);

			return GetDialogResult(code);
		}

		/// <summary>Returns the info structure for dialog display settings.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		private CREDUI.INFO GetInfo(IWin32Window owner) {
			CREDUI.INFO info = new CREDUI.INFO();
			if (owner != null) info.hwndParent = owner.Handle;
			info.pszCaptionText = this.Caption;
			info.pszMessageText = this.Message;
			if (this.Banner != null) {
				info.hbmBanner = new Bitmap(this.Banner, ValidBannerWidth, ValidBannerHeight).GetHbitmap();
			}
			info.cbSize = Marshal.SizeOf(info);
			return info;
		}

		/// <summary>Returns the flags for dialog display options.</summary>
		private CREDUI.FLAGS GetFlags() {
			CREDUI.FLAGS flags = CREDUI.FLAGS.GENERIC_CREDENTIALS;

			if (this.IncorrectPassword) {
				flags = flags | CREDUI.FLAGS.INCORRECT_PASSWORD;
			}

			if (this.AlwaysDisplay) {
				flags = flags | CREDUI.FLAGS.ALWAYS_SHOW_UI;
			}

			if (this.ExcludeCertificates) {
				flags = flags | CREDUI.FLAGS.EXCLUDE_CERTIFICATES;
			}

			if (this.Persist) {
				flags = flags | CREDUI.FLAGS.EXPECT_CONFIRMATION;
				if (this.SaveDisplayed) {
					flags = flags | CREDUI.FLAGS.SHOW_SAVE_CHECK_BOX;
				} else {
					flags = flags | CREDUI.FLAGS.PERSIST;
				}
			} else {
				flags = flags | CREDUI.FLAGS.DO_NOT_PERSIST;
			}

			if (this.KeepName) {
				flags = flags | CREDUI.FLAGS.KEEP_USERNAME;
			}

			return flags;
		}
	   
		/// <summary>Returns a DialogResult from the specified code.</summary>
		/// <param name="code">The credential return code.</param>
		private DialogResult GetDialogResult(CREDUI.ReturnCodes code) {
			DialogResult result;
			switch (code) {
				case CREDUI.ReturnCodes.NO_ERROR:
					result = DialogResult.OK;
					break;
				case CREDUI.ReturnCodes.ERROR_CANCELLED:
					result = DialogResult.Cancel;
					break;
				case CREDUI.ReturnCodes.ERROR_NO_SUCH_LOGON_SESSION:
					throw new ApplicationException("No such logon session.");
				case CREDUI.ReturnCodes.ERROR_NOT_FOUND:
					throw new ApplicationException("Not found.");
				case CREDUI.ReturnCodes.ERROR_INVALID_ACCOUNT_NAME:
					throw new ApplicationException("Invalid account name.");
				case CREDUI.ReturnCodes.ERROR_INSUFFICIENT_BUFFER:
					throw new ApplicationException("Insufficient buffer.");
				case CREDUI.ReturnCodes.ERROR_INVALID_PARAMETER:
					throw new ApplicationException("Invalid parameter.");
				case CREDUI.ReturnCodes.ERROR_INVALID_FLAGS:
					throw new ApplicationException("Invalid flags.");
				default:
					throw new ApplicationException("Unknown credential result encountered.");
			}
			return result;
		}
	}

	public sealed class CREDUI {
		private CREDUI() {
		}

		/// <summary>http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/authentication_constants.asp</summary>
		public const int MAX_MESSAGE_LENGTH		= 100;
		public const int MAX_CAPTION_LENGTH		= 100;
		public const int MAX_GENERIC_TARGET_LENGTH = 100;
		public const int MAX_DOMAIN_TARGET_LENGTH  = 100;
		public const int MAX_USERNAME_LENGTH	  = 100;
		public const int MAX_PASSWORD_LENGTH	  = 100;

		/// <summary>
		/// http://www.pinvoke.net/default.aspx/Enums.CREDUI_FLAGS
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnnetsec/html/dpapiusercredentials.asp
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduipromptforcredentials.asp
		/// </summary>
		[Flags] public enum FLAGS {
			INCORRECT_PASSWORD		  = 0x1,
			DO_NOT_PERSIST			  = 0x2,
			REQUEST_ADMINISTRATOR	  = 0x4,
			EXCLUDE_CERTIFICATES		= 0x8,
			REQUIRE_CERTIFICATE		= 0x10,
			SHOW_SAVE_CHECK_BOX		= 0x40,
			ALWAYS_SHOW_UI			  = 0x80,
			REQUIRE_SMARTCARD		  = 0x100,
			PASSWORD_ONLY_OK			= 0x200,
			VALIDATE_USERNAME		  = 0x400,
			COMPLETE_USERNAME		  = 0x800,
			PERSIST					= 0x1000,
			SERVER_CREDENTIAL		  = 0x4000,
			EXPECT_CONFIRMATION		= 0x20000,
			GENERIC_CREDENTIALS		= 0x40000,
			USERNAME_TARGET_CREDENTIALS = 0x80000,
			KEEP_USERNAME			  = 0x100000,
		}

		/// <summary>http://www.pinvoke.net/default.aspx/Enums.CredUIReturnCodes</summary>
		public enum ReturnCodes {
			NO_ERROR					= 0,
			ERROR_INVALID_PARAMETER	= 87,
			ERROR_INSUFFICIENT_BUFFER  = 122,
			ERROR_INVALID_FLAGS		= 1004,
			ERROR_NOT_FOUND			= 1168,
			ERROR_CANCELLED			= 1223,
			ERROR_NO_SUCH_LOGON_SESSION = 1312,
			ERROR_INVALID_ACCOUNT_NAME  = 1315
		}

		/// <summary>
		/// http://www.pinvoke.net/default.aspx/Structures.CREDUI_INFO
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/credui_info.asp
		/// </summary>
		public struct INFO {
			public int cbSize;
			public IntPtr hwndParent;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszMessageText;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszCaptionText;
			public IntPtr hbmBanner;
		}

		/// <summary>
		/// http://www.pinvoke.net/default.aspx/credui.CredUIPromptForCredentialsW
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduipromptforcredentials.asp
		/// </summary>
		[DllImport("credui", EntryPoint="CredUIPromptForCredentialsW", CharSet=CharSet.Unicode)]
		public static extern ReturnCodes PromptForCredentials(
			ref INFO creditUR,
			string targetName,
			IntPtr reserved1,
			int iError,
			StringBuilder userName,
			int maxUserName,
			StringBuilder password,
			int maxPassword,
			ref int iSave,
			FLAGS flags
		);

		/// <summary>
		/// http://www.pinvoke.net/default.aspx/credui.CredUIConfirmCredentials
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduiconfirmcredentials.asp
		/// </summary>
		[DllImport("credui.dll", EntryPoint="CredUIConfirmCredentialsW", CharSet=CharSet.Unicode)]
		public static extern ReturnCodes ConfirmCredentials(string targetName, bool confirm);
	}
}