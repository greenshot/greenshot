/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums {
	/// <summary>
	/// Window Style Flags
	/// </summary>
	[Flags]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum WindowStyleFlags : long {
		//WS_OVERLAPPED       = 0x00000000,
		WS_POPUP            = 0x80000000,
		WS_CHILD            = 0x40000000,
		WS_MINIMIZE         = 0x20000000,
		WS_VISIBLE          = 0x10000000,
		WS_DISABLED         = 0x08000000,
		WS_CLIPSIBLINGS     = 0x04000000,
		WS_CLIPCHILDREN     = 0x02000000,
		WS_MAXIMIZE         = 0x01000000,
		WS_BORDER           = 0x00800000,
		WS_DLGFRAME         = 0x00400000,
		WS_VSCROLL          = 0x00200000,
		WS_HSCROLL          = 0x00100000,
		WS_SYSMENU          = 0x00080000,
		WS_THICKFRAME       = 0x00040000,
		WS_GROUP            = 0x00020000,
		WS_TABSTOP          = 0x00010000,

		WS_UNK8000			= 0x00008000,
		WS_UNK4000			= 0x00004000,
		WS_UNK2000          = 0x00002000,
		WS_UNK1000          = 0x00001000,
		WS_UNK800			= 0x00000800,
		WS_UNK400			= 0x00000400,
		WS_UNK200           = 0x00000200,
		WS_UNK100           = 0x00000100,
		WS_UNK80			= 0x00000080,
		WS_UNK40			= 0x00000040,
		WS_UNK20			= 0x00000020,
		WS_UNK10			= 0x00000010,
		WS_UNK8				= 0x00000008,
		WS_UNK4				= 0x00000004,
		WS_UNK2				= 0x00000002,
		WS_UNK1				= 0x00000001,

		//WS_MINIMIZEBOX      = 0x00020000,
		//WS_MAXIMIZEBOX      = 0x00010000,

		//WS_CAPTION          = WS_BORDER | WS_DLGFRAME,
		//WS_TILED            = WS_OVERLAPPED,
		//WS_ICONIC           = WS_MINIMIZE,
		//WS_SIZEBOX          = WS_THICKFRAME,
		//WS_TILEDWINDOW      = WS_OVERLAPPEDWINDOW,

		//WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
		//WS_POPUPWINDOW	    = WS_POPUP | WS_BORDER | WS_SYSMENU
		//WS_CHILDWINDOW      = WS_CHILD
	}

    // See http://msdn.microsoft.com/en-us/library/aa969530(v=vs.85).aspx

    // Get/Set WindowLong Enum See: http://msdn.microsoft.com/en-us/library/ms633591.aspx

    // See: http://msdn.microsoft.com/en-us/library/ms633545.aspx
}
