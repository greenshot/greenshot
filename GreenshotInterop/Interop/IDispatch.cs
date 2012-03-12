using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.CustomMarshalers;

namespace Greenshot.Interop {
	[ComImport, Guid("00020400-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IDispatch {
		void Reserved();
		[PreserveSig]
		int GetTypeInfo(uint nInfo, int lcid, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TypeToTypeInfoMarshaler))] out Type typeInfo);
	}
}
