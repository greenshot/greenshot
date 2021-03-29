/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Runtime.InteropServices;

namespace Greenshot.Base.IEInterop
{
    [ComImport, Guid("3050f3db-98b5-11cf-bb82-00aa00bdce0b"),
     TypeLibType(TypeLibTypeFlags.FDual),
     InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IHTMLCurrentStyle
    {
        /// <summary><para><c>styleFloat</c> property of <c>IHTMLStyle</c> interface.</para></summary>
        string styleFloat
        {
            [DispId(-2147413042)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string left
        {
            [DispId(-2147418112 + 3)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string top
        {
            [DispId(-2147418112 + 4)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string width
        {
            [DispId(-2147418112 + 5)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string height
        {
            [DispId(-2147418112 + 6)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string right
        {
            [DispId(-2147418112 + 0x4d)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string bottom
        {
            [DispId(-2147418112 + 0x4e)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
}