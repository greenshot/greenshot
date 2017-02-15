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

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     A helper class which does the mashalling for structs
	/// </summary>
	public static class BinaryStructHelper
	{
		/// <summary>
		///     Get a struct from a byte array
		/// </summary>
		/// <typeparam name="T">typeof struct</typeparam>
		/// <param name="bytes">byte[]</param>
		/// <returns>struct</returns>
		public static T FromByteArray<T>(byte[] bytes) where T : struct
		{
			var ptr = IntPtr.Zero;
			try
			{
				var size = Marshal.SizeOf(typeof(T));
				ptr = Marshal.AllocHGlobal(size);
				Marshal.Copy(bytes, 0, ptr, size);
				return FromIntPtr<T>(ptr);
			}
			finally
			{
				if (ptr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(ptr);
				}
			}
		}

		/// <summary>
		///     Get a struct from a byte array
		/// </summary>
		/// <typeparam name="T">typeof struct</typeparam>
		/// <param name="intPtr">Pointer to the structor to return</param>
		/// <returns>struct</returns>
		public static T FromIntPtr<T>(IntPtr intPtr) where T : struct
		{
			var obj = Marshal.PtrToStructure(intPtr, typeof(T));
			return (T) obj;
		}

		/// <summary>
		///     copy a struct to a byte array
		/// </summary>
		/// <typeparam name="T">typeof struct</typeparam>
		/// <param name="obj">struct</param>
		/// <returns>byte[]</returns>
		public static byte[] ToByteArray<T>(T obj) where T : struct
		{
			var ptr = IntPtr.Zero;
			try
			{
				var size = Marshal.SizeOf(typeof(T));
				ptr = Marshal.AllocHGlobal(size);
				Marshal.StructureToPtr(obj, ptr, true);
				return FromPtrToByteArray<T>(ptr);
			}
			finally
			{
				if (ptr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(ptr);
				}
			}
		}

		/// <summary>
		///     copy a struct from a pointer to a byte array
		/// </summary>
		/// <typeparam name="T">typeof struct</typeparam>
		/// <param name="ptr">IntPtr to struct</param>
		/// <returns>byte[]</returns>
		public static byte[] FromPtrToByteArray<T>(IntPtr ptr) where T : struct
		{
			var size = Marshal.SizeOf(typeof(T));
			var bytes = new byte[size];
			Marshal.Copy(ptr, bytes, 0, size);
			return bytes;
		}
	}
}