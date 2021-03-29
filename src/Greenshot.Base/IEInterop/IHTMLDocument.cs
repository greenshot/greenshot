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
    /// <summary><para><c>IHTMLDocument</c> interface.</para></summary>
    [Guid("626FC520-A41E-11CF-A731-00A0C9082637")]
    [ComImport]
    [TypeLibType(TypeLibTypeFlags.FDual)]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IHTMLDocument
    {
        object Script
        {
            [return: MarshalAs(UnmanagedType.IDispatch)]
            get;
        }
    }
}