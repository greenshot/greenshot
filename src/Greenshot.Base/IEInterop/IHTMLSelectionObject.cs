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
    /// <summary>
    /// See <a href="https://msdn.microsoft.com/en-us/library/aa768849%28v=vs.85%29.aspx">here</a>
    /// </summary>
    [ComImport, Guid("3050f25A-98b5-11cf-bb82-00aa00bdce0b"),
     TypeLibType(TypeLibTypeFlags.FDual),
     InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IHTMLSelectionObject
    {
        [return: MarshalAs(UnmanagedType.IDispatch)]
        [DispId(1001)]
        IHTMLTxtRange createRange();

        [DispId(1002)]
        void empty();

        [DispId(1003)]
        void clear();

        [DispId(1004)]
        string EventType
        {
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
}