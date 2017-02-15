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
using System.Collections.Generic;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     Code for handling with "windows"
	///     Main code is taken from vbAccelerator, location:
	///     http://www.vbaccelerator.com/home/NET/Code/Libraries/Windows/Enumerating_Windows/article.asp
	///     but a LOT of changes/enhancements were made to adapt it for Greenshot.
	///     Provides details about a Window returned by the  enumeration
	/// </summary>
	public class WindowDetails
	{
		private static readonly IList<IntPtr> IgnoreHandles = new List<IntPtr>();

		/// <summary>
		///     Use this to make remove internal windows, like the mainform and the captureforms, invisible
		/// </summary>
		/// <param name="ignoreHandle"></param>
		public static void RegisterIgnoreHandle(IntPtr ignoreHandle)
		{
			IgnoreHandles.Add(ignoreHandle);
		}

		/// <summary>
		///     Use this to remove the with RegisterIgnoreHandle registered handle
		/// </summary>
		/// <param name="ignoreHandle"></param>
		public static void UnregisterIgnoreHandle(IntPtr ignoreHandle)
		{
			IgnoreHandles.Remove(ignoreHandle);
		}
	}
}