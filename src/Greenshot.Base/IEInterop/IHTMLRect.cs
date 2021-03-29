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
    [ComImport, Guid("3050F4A3-98B5-11CF-BB82-00AA00BDCE0B"),
     TypeLibType(TypeLibTypeFlags.FDual),
     InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IHTMLRect
    {
        int bottom { [DispId(1004)] get; }

        int left { [DispId(1001)] get; }

        int right { [DispId(1003)] get; }

        int top { [DispId(1002)] get; }
    }
}