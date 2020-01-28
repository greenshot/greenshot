// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Dapplo.Windows.Common.Enums;

namespace Greenshot.Addon.Win10.Native
{
	/// <summary>
	/// The IDataTransferManagerInterOp is documented here: https://msdn.microsoft.com/en-us/library/windows/desktop/jj542488(v=vs.85).aspx.
	/// This interface allows an app to tie the share context to a specific
	/// window using a window handle. Useful for Win32 apps.
	/// </summary>
	[ComImport, Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IDataTransferManagerInterOp
	{
		/// <summary>
		/// Get an instance of Windows.ApplicationModel.DataTransfer.DataTransferManager
		/// for the window identified by a window handle
		/// </summary>
		/// <param name="appWindow">The window handle</param>
		/// <param name="riid">ID of the DataTransferManager interface</param>
		/// <param name="pDataTransferManager">The DataTransferManager instance for this window handle</param>
		/// <returns>HRESULT</returns>
		[PreserveSig]
		HResult GetForWindow([In] IntPtr appWindow, [In] ref Guid riid, [Out] out DataTransferManager pDataTransferManager);

		/// <summary>
		/// Show the share flyout for the window identified by a window handle
		/// </summary>
		/// <param name="appWindow">The window handle</param>
		/// <returns>HRESULT</returns>
		[PreserveSig]
		HResult ShowShareUIForWindow(IntPtr appWindow);
	}

}
