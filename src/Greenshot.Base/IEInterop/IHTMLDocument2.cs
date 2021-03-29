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
    /// <summary><para><c>IHTMLDocument2</c> interface.</para></summary>
    [Guid("332C4425-26CB-11D0-B483-00C04FD90119")]
    [ComImport]
    [TypeLibType(TypeLibTypeFlags.FDual)]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IHTMLDocument2
    {
        IHTMLElement body
        {
            [DispId(1004)]
            [return: MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        string title
        {
            [DispId(1012)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        object frames
        {
            [DispId(1019)]
            [return: MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        string url
        {
            [DispId(1025)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        IHTMLWindow2 parentWindow
        {
            [DispId(1034)]
            [return: MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        object bgColor { [DispId(-501)] get; }

        IHTMLSelectionObject selection
        {
            [DispId(1017)]
            [return: MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        string designMode
        {
            [DispId(1014)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            [DispId(1014)] set;
        }
    }
}