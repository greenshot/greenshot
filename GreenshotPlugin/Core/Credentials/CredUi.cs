using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GreenshotPlugin.Core.Credentials
{
    internal static class CredUi
    {
        /// <summary>http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/authentication_constants.asp</summary>
        public const int MAX_MESSAGE_LENGTH = 100;

        public const int MAX_CAPTION_LENGTH = 100;
        public const int MAX_GENERIC_TARGET_LENGTH = 100;
        public const int MAX_DOMAIN_TARGET_LENGTH = 100;
        public const int MAX_USERNAME_LENGTH = 100;
        public const int MAX_PASSWORD_LENGTH = 100;

        /// <summary>
        ///     http://www.pinvoke.net/default.aspx/credui.CredUIPromptForCredentialsW
        ///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduipromptforcredentials.asp
        /// </summary>
        [DllImport("credui", CharSet = CharSet.Unicode)]
        public static extern CredUIReturnCodes CredUIPromptForCredentials(
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
        public static extern CredUIReturnCodes CredUIConfirmCredentials(string targetName, [MarshalAs(UnmanagedType.Bool)] bool confirm);
    }
}