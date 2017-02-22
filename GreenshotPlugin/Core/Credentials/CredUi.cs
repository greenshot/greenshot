#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Runtime.InteropServices;
using System.Text;

#endregion

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