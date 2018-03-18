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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.CustomMarshalers;
using DISPPARAMS = System.Runtime.InteropServices.ComTypes.DISPPARAMS;
using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

#endregion

namespace Greenshot.Addons.Interop
{
	[ComImport]
	[Guid("00020400-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	public interface IDispatch : IUnknown
	{
		[PreserveSig]
		int GetTypeInfoCount(out int count);

		[PreserveSig]
		int GetTypeInfo([MarshalAs(UnmanagedType.U4)] int iTInfo, [MarshalAs(UnmanagedType.U4)] int lcid,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TypeToTypeInfoMarshaler))] out Type typeInfo);

		[PreserveSig]
		int GetIDsOfNames(ref Guid riid, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] rgsNames, int cNames, int lcid,
			[MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);

		[PreserveSig]
		int Invoke(int dispIdMember, ref Guid riid, uint lcid, ushort wFlags, ref DISPPARAMS pDispParams,
			out object pVarResult, ref EXCEPINFO pExcepInfo, IntPtr[] pArgErr);
	}
}