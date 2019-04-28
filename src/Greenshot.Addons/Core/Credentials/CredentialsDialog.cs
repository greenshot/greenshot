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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Common;

namespace Greenshot.Addons.Core.Credentials
{
	/// <summary>
	///     The following code comes from: http://www.developerfusion.com/code/4693/using-the-credential-management-api/
	///     and is slightly modified so it works for us.
	///     As the "Stored usernames and passwords" which can be accessed by: Start-> Run and type "Control keymgr.dll"
	///     doesn't show all credentials use the tool here: http://www.microsoft.com/indonesia/msdn/credmgmt.aspx
	///     The following code is an example for a login, it will call the Authenticate with user/password
	///     which should return true if the login worked, false if not.
	/// </summary>
	/// <summary>Encapsulates dialog functionality from the Credential Management API.</summary>
	public sealed class CredentialsDialog
	{
		/// <summary>The only valid bitmap height (in pixels) of a user-defined banner.</summary>
		private const int ValidBannerHeight = 60;
		/// <summary>The only valid bitmap width (in pixels) of a user-defined banner.</summary>
		private const int ValidBannerWidth = 320;

	    /// <summary>http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/authentication_constants.asp</summary>
	    private const int MaxMessageLength = 100;
	    private const int MaxCaptionLength = 100;
	    private const int MaxGenericTargetLength = 100;
	    private const int MaxUsernameLength = 100;
	    private const int MaxPasswordLength = 100;

        private Image _banner;
		private string _caption = string.Empty;
		private string _message = string.Empty;
		private string _name = string.Empty;
		private string _password = string.Empty;
		private string _target = string.Empty;

		/// <summary>
		///     Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog" /> class
		///     with the specified target, caption, message and banner.
		/// </summary>
		/// <param name="target">The name of the target for the credentials, typically a server name.</param>
		/// <param name="caption">The caption of the dialog (null will cause a system default title to be used).</param>
		/// <param name="message">The message of the dialog (null will cause a system default message to be used).</param>
		/// <param name="banner">The image to display on the dialog (null will cause a system default image to be used).</param>
		public CredentialsDialog(string target, string caption = null, string message = null, Image banner = null)
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
	    public bool IncorrectPassword { get; set; } = true;

		/// <summary>Gets or sets if the name is read-only.</summary>
		public bool KeepName { get; set; }

		/// <summary>Gets or sets the name for the credentials.</summary>
		public string Name
		{
			get { return _name; }
			set
			{
				if (value?.Length > MaxUsernameLength)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The name has a maximum length of {0} characters.",
						MaxUsernameLength);
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
				if (value?.Length > MaxPasswordLength)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The password has a maximum length of {0} characters.",
						MaxPasswordLength);
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
				if (value.Length > MaxGenericTargetLength)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The target has a maximum length of {0} characters.",
						MaxGenericTargetLength);
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
				if (value?.Length > MaxCaptionLength)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The caption has a maximum length of {0} characters.",
						MaxCaptionLength);
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
				if (value?.Length > MaxMessageLength)
				{
					var message = string.Format(
						Thread.CurrentThread.CurrentUICulture,
						"The message has a maximum length of {0} characters.",
						MaxMessageLength);
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

		/// <summary>Shows the credentials dialog with the specified owner, name, password and save checkbox status.</summary>
		/// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
		/// <param name="name">The name for the credentials.</param>
		/// <param name="password">The password for the credentials.</param>
		/// <param name="saveChecked">True if the save checkbox is checked.</param>
		/// <returns>Returns a DialogResult indicating the user action.</returns>
		public DialogResult Show(IWin32Window owner, string name = null, string password = null, bool? saveChecked = null)
		{
			if (!WindowsVersion.IsWindowsXpOrLater)
			{
				throw new ApplicationException("The Credential Management API requires Windows XP / Windows Server 2003 or later.");
			}

		    if (name != null)
		    {
		        Name = name;
            }
		    if (password != null)
		    {
		        Password = password;
		    }
		    if (saveChecked.HasValue)
		    {
		        SaveChecked = saveChecked.Value;
            }

            return ShowDialog(owner);
		}

		/// <summary>Confirmation action to be applied.</summary>
		/// <param name="value">True if the credentials should be persisted.</param>
		public void Confirm(bool value)
		{
			var confirmResult = CredUIConfirmCredentials(Target, value);
			switch (confirmResult)
			{
				case CredUiReturnCodes.NoError:
					break;
				case CredUiReturnCodes.ErrorInvalidParameter:
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
			var name = new StringBuilder(MaxUsernameLength);
			name.Append(Name);

			var password = new StringBuilder(MaxPasswordLength);
			password.Append(Password);

			var saveChecked = Convert.ToInt32(SaveChecked);

			var info = GetInfo(owner);
		    try
		    {
		        var credFlags = GetFlags();

		        // make the api call
		        var code = CredUIPromptForCredentials(
		            ref info,
		            Target,
		            IntPtr.Zero, 0,
		            name, MaxUsernameLength,
		            password, MaxPasswordLength,
		            ref saveChecked,
		            credFlags
		        );
		        // set the accessors from the api call parameters
		        Name = name.ToString();
		        Password = password.ToString();
		        SaveChecked = Convert.ToBoolean(saveChecked);

		        return GetDialogResult(code);
		    }
            finally
		    {
		        // clean up resources
		        if (Banner != null)
		        {
		            DeleteObject(info.hbmBanner);
		        }
            }
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
			var credFlags = CredFlags.GenericCredentials;

			if (IncorrectPassword)
			{
				credFlags = credFlags | CredFlags.IncorrectPassword;
			}

			if (AlwaysDisplay)
			{
				credFlags = credFlags | CredFlags.AlwaysShowUi;
			}

			if (ExcludeCertificates)
			{
				credFlags = credFlags | CredFlags.ExcludeCertificates;
			}

			if (Persist)
			{
				credFlags = credFlags | CredFlags.ExpectConfirmation;
				if (SaveDisplayed)
				{
					credFlags = credFlags | CredFlags.ShowSaveCheckBox;
				}
				else
				{
					credFlags = credFlags | CredFlags.Persist;
				}
			}
			else
			{
				credFlags = credFlags | CredFlags.DoNotPersist;
			}

			if (KeepName)
			{
				credFlags = credFlags | CredFlags.KeepUsername;
			}

			return credFlags;
		}

		/// <summary>Returns a DialogResult from the specified code.</summary>
		/// <param name="code">The credential return code.</param>
		private DialogResult GetDialogResult(CredUiReturnCodes code)
		{
			DialogResult result;
			switch (code)
			{
				case CredUiReturnCodes.NoError:
					result = DialogResult.OK;
					break;
				case CredUiReturnCodes.ErrorCancelled:
					result = DialogResult.Cancel;
					break;
				case CredUiReturnCodes.ErrorNoSuchLogonSession:
					throw new ApplicationException("No such logon session.");
				case CredUiReturnCodes.ErrorNotFound:
					throw new ApplicationException("Not found.");
				case CredUiReturnCodes.ErrorInvalidAccountName:
					throw new ApplicationException("Invalid account name.");
				case CredUiReturnCodes.ErrorInsufficientBuffer:
					throw new ApplicationException("Insufficient buffer.");
				case CredUiReturnCodes.ErrorInvalidParameter:
					throw new ApplicationException("Invalid parameter.");
				case CredUiReturnCodes.ErrorInvalidFlags:
					throw new ApplicationException("Invalid flags.");
				default:
					throw new ApplicationException("Unknown credential result encountered.");
			}
			return result;
		}

	    /// <summary>
	    ///     http://www.pinvoke.net/default.aspx/credui.CredUIPromptForCredentialsW
	    ///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduipromptforcredentials.asp
	    /// </summary>
	    [DllImport("credui", CharSet = CharSet.Unicode)]
	    private static extern CredUiReturnCodes CredUIPromptForCredentials(
	        ref CredUiInfo credUiInfo,
	        string targetName,
	        IntPtr reserved1,
	        int iError,
	        StringBuilder userName,
	        int maxUserName,
	        StringBuilder password,
	        int maxPassword,
	        ref int iSave,
	        CredFlags credFlags
	    );

	    /// <summary>
	    ///     http://www.pinvoke.net/default.aspx/credui.CredUIConfirmCredentials
	    ///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduiconfirmcredentials.asp
	    /// </summary>
	    [DllImport("credui.dll", CharSet = CharSet.Unicode)]
	    private static extern CredUiReturnCodes CredUIConfirmCredentials(string targetName, [MarshalAs(UnmanagedType.Bool)] bool confirm);
    }
}