/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The following code comes from: https://www.developerfusion.com/code/4693/using-the-credential-management-api/
    /// and is slightly modified so it works for us.
    /// As the "Stored usernames and passwords" which can be accessed by: Start-> Run and type "Control keymgr.dll"
    /// doesn't show all credentials use the tool here: https://www.microsoft.com/indonesia/msdn/credmgmt.aspx
    /// The following code is an example for a login, it will call the Authenticate with user/password
    /// which should return true if the login worked, false if not.
    ///        private static bool Login(string system, string name) {
    ///            try {
    ///                CredentialsDialog dialog = new CredentialsDialog(system);
    ///                dialog.Name = name;
    ///                while (dialog.Show(dialog.Name) == DialogResult.OK) {
    ///                    if (Authenticate(dialog.Name, dialog.Password)) {
    ///                        if (dialog.SaveChecked) dialog.Confirm(true);
    ///                        return true;
    ///                    } else {
    ///                        try {
    ///                            dialog.Confirm(false);
    ///                        } catch (ApplicationException) {
    ///                            // exception handling ...
    ///                        }
    ///                        dialog.IncorrectPassword = true;
    ///                    }
    ///                }
    ///            } catch (ApplicationException) {
    ///                // exception handling ...
    ///            }
    ///            return false;
    ///        }
    /// </summary>
    /// <summary>Encapsulates dialog functionality from the Credential Management API.</summary>
    public sealed class CredentialsDialog
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>The only valid bitmap height (in pixels) of a user-defined banner.</summary>
        private const int ValidBannerHeight = 60;

        /// <summary>The only valid bitmap width (in pixels) of a user-defined banner.</summary>
        private const int ValidBannerWidth = 320;

        /// <summary>Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog"/> class
        /// with the specified target.</summary>
        /// <param name="target">The name of the target for the credentials, typically a server name.</param>
        public CredentialsDialog(string target) : this(target, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog"/> class
        /// with the specified target and caption.</summary>
        /// <param name="target">The name of the target for the credentials, typically a server name.</param>
        /// <param name="caption">The caption of the dialog (null will cause a system default title to be used).</param>
        public CredentialsDialog(string target, string caption) : this(target, caption, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog"/> class
        /// with the specified target, caption and message.</summary>
        /// <param name="target">The name of the target for the credentials, typically a server name.</param>
        /// <param name="caption">The caption of the dialog (null will cause a system default title to be used).</param>
        /// <param name="message">The message of the dialog (null will cause a system default message to be used).</param>
        public CredentialsDialog(string target, string caption, string message) : this(target, caption, message, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:SecureCredentialsLibrary.CredentialsDialog"/> class
        /// with the specified target, caption, message and banner.</summary>
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
        /// Gets or sets if the dialog will be shown even if the credentials
        /// can be returned from an existing credential in the credential manager.
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

        private string _name = string.Empty;

        /// <summary>Gets or sets the name for the credentials.</summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (value?.Length > CredUi.MAX_USERNAME_LENGTH)
                {
                    string message = string.Format(
                        Thread.CurrentThread.CurrentUICulture,
                        "The name has a maximum length of {0} characters.",
                        CredUi.MAX_USERNAME_LENGTH);
                    throw new ArgumentException(message, nameof(Name));
                }

                _name = value;
            }
        }

        private string _password = string.Empty;

        /// <summary>Gets or sets the password for the credentials.</summary>
        public string Password
        {
            get { return _password; }
            set
            {
                if (value?.Length > CredUi.MAX_PASSWORD_LENGTH)
                {
                    string message = string.Format(
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

        private string _target = string.Empty;

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
                    string message = string.Format(
                        Thread.CurrentThread.CurrentUICulture,
                        "The target has a maximum length of {0} characters.",
                        CredUi.MAX_GENERIC_TARGET_LENGTH);
                    throw new ArgumentException(message, nameof(Target));
                }

                _target = value;
            }
        }

        private string _caption = string.Empty;

        /// <summary>Gets or sets the caption of the dialog.</summary>
        /// <remarks>A null value will cause a system default caption to be used.</remarks>
        public string Caption
        {
            get { return _caption; }
            set
            {
                if (value?.Length > CredUi.MAX_CAPTION_LENGTH)
                {
                    string message = string.Format(
                        Thread.CurrentThread.CurrentUICulture,
                        "The caption has a maximum length of {0} characters.",
                        CredUi.MAX_CAPTION_LENGTH);
                    throw new ArgumentException(message, nameof(Caption));
                }

                _caption = value;
            }
        }

        private string _message = string.Empty;

        /// <summary>Gets or sets the message of the dialog.</summary>
        /// <remarks>A null value will cause a system default message to be used.</remarks>
        public string Message
        {
            get { return _message; }
            set
            {
                if (value?.Length > CredUi.MAX_MESSAGE_LENGTH)
                {
                    string message = string.Format(
                        Thread.CurrentThread.CurrentUICulture,
                        "The message has a maximum length of {0} characters.",
                        CredUi.MAX_MESSAGE_LENGTH);
                    throw new ArgumentException(message, nameof(Message));
                }

                _message = value;
            }
        }

        private Image _banner;

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

        /// <summary>Shows the credentials dialog with the specified name.</summary>
        /// <param name="name">The name for the credentials.</param>
        /// <returns>Returns a DialogResult indicating the user action.</returns>
        public DialogResult Show(string name)
        {
            return Show(null, name, Password, SaveChecked);
        }

        /// <summary>Shows the credentials dialog with the specified owner, name, password and save checkbox status.</summary>
        /// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
        /// <param name="name">The name for the credentials.</param>
        /// <param name="password">The password for the credentials.</param>
        /// <param name="saveChecked">True if the save checkbox is checked.</param>
        /// <returns>Returns a DialogResult indicating the user action.</returns>
        public DialogResult Show(IWin32Window owner, string name, string password, bool saveChecked)
        {
            if ((Environment.OSVersion.Version.Major < 5) || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor < 1)))
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
                case CredUi.ReturnCodes.NO_ERROR:
                    break;
                case CredUi.ReturnCodes.ERROR_INVALID_PARAMETER:
                    // for some reason, this is encountered when credentials are overwritten
                    break;
                default:
                    throw new ApplicationException($"Credential confirmation failed: {confirmResult}");
            }
        }

        /// <summary>Returns a DialogResult indicating the user action.</summary>
        /// <param name="owner">The System.Windows.Forms.IWin32Window the dialog will display in front of.</param>
        /// <remarks>
        /// Sets the name, password and SaveChecked accessors to the state of the dialog as it was dismissed by the user.
        /// </remarks>
        private DialogResult ShowDialog(IWin32Window owner)
        {
            // set the api call parameters
            StringBuilder name = new StringBuilder(CredUi.MAX_USERNAME_LENGTH);
            name.Append(Name);

            StringBuilder password = new StringBuilder(CredUi.MAX_PASSWORD_LENGTH);
            password.Append(Password);

            int saveChecked = Convert.ToInt32(SaveChecked);

            CredUi.INFO info = GetInfo(owner);
            CredUi.CredFlags credFlags = GetFlags();

            // make the api call
            CredUi.ReturnCodes code = CredUi.CredUIPromptForCredentials(
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
        private CredUi.INFO GetInfo(IWin32Window owner)
        {
            CredUi.INFO info = new CredUi.INFO();
            if (owner != null) info.hWndParent = owner.Handle;
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
        private CredUi.CredFlags GetFlags()
        {
            CredUi.CredFlags credFlags = CredUi.CredFlags.GENERIC_CREDENTIALS;

            if (IncorrectPassword)
            {
                credFlags |= CredUi.CredFlags.INCORRECT_PASSWORD;
            }

            if (AlwaysDisplay)
            {
                credFlags |= CredUi.CredFlags.ALWAYS_SHOW_UI;
            }

            if (ExcludeCertificates)
            {
                credFlags |= CredUi.CredFlags.EXCLUDE_CERTIFICATES;
            }

            if (Persist)
            {
                credFlags |= CredUi.CredFlags.EXPECT_CONFIRMATION;
                if (SaveDisplayed)
                {
                    credFlags |= CredUi.CredFlags.SHOW_SAVE_CHECK_BOX;
                }
                else
                {
                    credFlags |= CredUi.CredFlags.PERSIST;
                }
            }
            else
            {
                credFlags |= CredUi.CredFlags.DO_NOT_PERSIST;
            }

            if (KeepName)
            {
                credFlags |= CredUi.CredFlags.KEEP_USERNAME;
            }

            return credFlags;
        }

        /// <summary>Returns a DialogResult from the specified code.</summary>
        /// <param name="code">The credential return code.</param>
        private DialogResult GetDialogResult(CredUi.ReturnCodes code) =>
            code switch
            {
                CredUi.ReturnCodes.NO_ERROR => DialogResult.OK,
                CredUi.ReturnCodes.ERROR_CANCELLED => DialogResult.Cancel,
                CredUi.ReturnCodes.ERROR_NO_SUCH_LOGON_SESSION => throw new ApplicationException(
                    "No such logon session."),
                CredUi.ReturnCodes.ERROR_NOT_FOUND => throw new ApplicationException("Not found."),
                CredUi.ReturnCodes.ERROR_INVALID_ACCOUNT_NAME =>
                    throw new ApplicationException("Invalid account name."),
                CredUi.ReturnCodes.ERROR_INSUFFICIENT_BUFFER => throw new ApplicationException("Insufficient buffer."),
                CredUi.ReturnCodes.ERROR_INVALID_PARAMETER => throw new ApplicationException("Invalid parameter."),
                CredUi.ReturnCodes.ERROR_INVALID_FLAGS => throw new ApplicationException("Invalid flags."),
                _ => throw new ApplicationException("Unknown credential result encountered.")
            };
    }

    internal static class CredUi
    {
        /// <summary>https://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/authentication_constants.asp</summary>
        public const int MAX_MESSAGE_LENGTH = 100;

        public const int MAX_CAPTION_LENGTH = 100;
        public const int MAX_GENERIC_TARGET_LENGTH = 100;
        public const int MAX_USERNAME_LENGTH = 100;
        public const int MAX_PASSWORD_LENGTH = 100;

        /// <summary>
        /// https://www.pinvoke.net/default.aspx/Enums.CREDUI_FLAGS
        /// https://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnnetsec/html/dpapiusercredentials.asp
        /// https://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduipromptforcredentials.asp
        /// </summary>
        [Flags]
        public enum CredFlags
        {
            INCORRECT_PASSWORD = 0x1,
            DO_NOT_PERSIST = 0x2,
            EXCLUDE_CERTIFICATES = 0x8,
            SHOW_SAVE_CHECK_BOX = 0x40,
            ALWAYS_SHOW_UI = 0x80,
            PERSIST = 0x1000,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            KEEP_USERNAME = 0x100000,
        }

        /// <summary>https://www.pinvoke.net/default.aspx/Enums.CredUIReturnCodes</summary>
        public enum ReturnCodes
        {
            NO_ERROR = 0,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_FLAGS = 1004,
            ERROR_NOT_FOUND = 1168,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_INVALID_ACCOUNT_NAME = 1315
        }

        /// <summary>
        /// https://www.pinvoke.net/default.aspx/Structures.CREDUI_INFO
        /// https://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/credui_info.asp
        /// </summary>
        public struct INFO
        {
            public int cbSize;
            public IntPtr hWndParent;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszMessageText;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        /// <summary>
        /// https://www.pinvoke.net/default.aspx/credui.CredUIPromptForCredentialsW
        /// https://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduipromptforcredentials.asp
        /// </summary>
        [DllImport("credui", CharSet = CharSet.Unicode)]
        public static extern ReturnCodes CredUIPromptForCredentials(
            ref INFO creditUR,
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
        /// https://www.pinvoke.net/default.aspx/credui.CredUIConfirmCredentials
        /// https://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduiconfirmcredentials.asp
        /// </summary>
        [DllImport("credui.dll", CharSet = CharSet.Unicode)]
        public static extern ReturnCodes CredUIConfirmCredentials(string targetName, [MarshalAs(UnmanagedType.Bool)] bool confirm);
    }
}