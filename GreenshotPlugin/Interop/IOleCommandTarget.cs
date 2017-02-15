#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Runtime.InteropServices;

#endregion

namespace GreenshotPlugin.Interop
{
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true)]
	[Guid("B722BCCB-4E68-101B-A2BC-00AA00404770")]
	public interface IOleCommandTarget
	{
		[return: MarshalAs(UnmanagedType.I4)]
		[PreserveSig]
		int QueryStatus([In] [MarshalAs(UnmanagedType.LPStruct)] Guid pguidCmdGroup, int cCmds, IntPtr prgCmds, IntPtr pCmdText);

		[return: MarshalAs(UnmanagedType.I4)]
		[PreserveSig]
		int Exec([In] [MarshalAs(UnmanagedType.LPStruct)] Guid pguidCmdGroup, int nCmdID, int nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut);
	}
}