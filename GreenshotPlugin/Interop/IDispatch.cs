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
using System.Runtime.InteropServices.CustomMarshalers;
using DISPPARAMS = System.Runtime.InteropServices.ComTypes.DISPPARAMS;
using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

#endregion

namespace GreenshotPlugin.Interop
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