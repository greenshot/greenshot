﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Runtime.InteropServices;

namespace GreenshotPlugin.IEInterop {
	/// <summary><para><c>IHTMLDocument3</c> interface.</para></summary>
	[Guid("3050F485-98B5-11CF-BB82-00AA00BDCE0B")]
	[ComImport]
	[TypeLibType(TypeLibTypeFlags.FDual)]
	[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
	public interface IHTMLDocument3 {
		IHTMLElement documentElement {
			[DispId(1075)]
			[return: MarshalAs(UnmanagedType.IDispatch)]
			get;
		}

		[DispId(1086)]
		[return: MarshalAs(UnmanagedType.IDispatch)]
		IHTMLElementCollection getElementsByName([MarshalAs(UnmanagedType.BStr)] string v);

		[DispId(1088)]
		IHTMLElement getElementById([MarshalAs(UnmanagedType.BStr)] string v);

		[DispId(1087)]
		IHTMLElementCollection getElementsByTagName([MarshalAs(UnmanagedType.BStr)] string v);
	}
}