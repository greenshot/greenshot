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

namespace Greenshot.Addons.Core.Credentials
{
    /// <summary>
    ///     http://www.pinvoke.net/default.aspx/Enums.CREDUI_FLAGS
    ///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnnetsec/html/dpapiusercredentials.asp
    ///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduipromptforcredentials.asp
    /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Flags]
	public enum CredFlags
	{
        IncorrectPassword = 0x1,
        DoNotPersist = 0x2,
		RequestAdministrator = 0x4,
		ExcludeCertificates = 0x8,
		RequireCertificate = 0x10,
		ShowSaveCheckBox = 0x40,
		AlwaysShowUi = 0x80,
		RequireSmartcard = 0x100,
		PasswordOnlyOk = 0x200,
		ValidateUsername = 0x400,
		CompleteUsername = 0x800,
		Persist = 0x1000,
		ServerCredential = 0x4000,
		ExpectConfirmation = 0x20000,
		GenericCredentials = 0x40000,
		UsernameTargetCredentials = 0x80000,
		KeepUsername = 0x100000
	}
}