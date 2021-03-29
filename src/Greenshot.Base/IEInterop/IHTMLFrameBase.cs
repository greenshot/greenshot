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
    [ComVisible(true), ComImport(), Guid("3050f311-98b5-11cf-bb82-00aa00bdce0b"),
     TypeLibType(TypeLibTypeFlags.FDual),
     InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IHTMLFrameBase
    {
//dispinterface IHTMLFrameBase {
//    properties:
//    methods:
//        [id(0x80010bb8), propput]
//        void src([in] BSTR rhs);
//        [id(0x80010bb8), propget]
//        BSTR src();
//        [id(0x80010000), propput]
//        void name([in] BSTR rhs);
//        [id(0x80010000), propget]
//        BSTR name();
//        [id(0x80010bba), propput]
//        void border([in] VARIANT rhs);
//        [id(0x80010bba), propget]
//        VARIANT border();
//        [id(0x80010bbb), propput]
//        void frameBorder([in] BSTR rhs);
//        [id(0x80010bbb), propget]
//        BSTR frameBorder();
//        [id(0x80010bbc), propput]
//        void frameSpacing([in] VARIANT rhs);
//        [id(0x80010bbc), propget]
//        VARIANT frameSpacing();
//        [id(0x80010bbd), propput]
//        void marginWidth([in] VARIANT rhs);
//        [id(0x80010bbd), propget]
//        VARIANT marginWidth();
//        [id(0x80010bbe), propput]
//        void marginHeight([in] VARIANT rhs);
//        [id(0x80010bbe), propget]
//        VARIANT marginHeight();
//        [id(0x80010bbf), propput]
//        void noResize([in] VARIANT_BOOL rhs);
//        [id(0x80010bbf), propget]
//        VARIANT_BOOL noResize();
//        [id(0x80010bc0), propput]
//        void scrolling([in] BSTR rhs);
//        [id(0x80010bc0), propget]
//        BSTR scrolling();
//};
//        [DispId(HTMLDispIDs.DISPID_IHTMLFRAMEBASE_SRC)]
        string src
        {
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string name
        {
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        object border { get; }

        string frameBorder
        {
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        object frameSpacing { get; }
        object marginWidth { get; }
        object marginHeight { get; }

        bool noResize
        {
            [return: MarshalAs(UnmanagedType.VariantBool)]
            get;
        }

        string scrolling
        {
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
}