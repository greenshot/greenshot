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
    [ComImport, Guid("3050F1FF-98B5-11CF-BB82-00AA00BDCE0B"),
     TypeLibType(TypeLibTypeFlags.FDual),
     InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IHTMLElement
    {
        [DispId(-2147417611)]
        void setAttribute([MarshalAs(UnmanagedType.BStr)] string strAttributeName, object AttributeValue, int lFlags);

        [DispId(-2147417610)]
        object getAttribute([MarshalAs(UnmanagedType.BStr)] string strAttributeName, int lFlags);

        long offsetLeft { [DispId(-2147417104)] get; }

        long offsetTop { [DispId(-2147417103)] get; }

        long offsetWidth { [DispId(-2147417102)] get; }

        long offsetHeight { [DispId(-2147417101)] get; }

        IHTMLElement offsetParent { [DispId(-2147417100)] get; }

        string className
        {
            [DispId(-2147417111)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        IHTMLDocument2 document
        {
            [DispId(-2147417094)]
            [return: MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        string id
        {
            [DispId(-2147417110)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string innerHTML
        {
            [DispId(-2147417086)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string innerText
        {
            [DispId(-2147417085)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        IHTMLStyle style
        {
            [DispId(-2147418038)]
            [return: MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        string tagName
        {
            [DispId(-2147417108)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string title
        {
            [DispId(-2147418043)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        [DispId(-2147417093)]
        void scrollIntoView(bool varargStart);

        IHTMLElementCollection children
        {
            [DispId(-2147417075)]
            [return: MarshalAs(UnmanagedType.IDispatch)]
            get;
        }
    }
}