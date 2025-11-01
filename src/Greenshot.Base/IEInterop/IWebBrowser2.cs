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
    // IWebBrowser: EAB22AC1-30C1-11CF-A7EB-0000C05BAE0B
//    [ComVisible(true), ComImport(), Guid("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E"),
//     TypeLibType(TypeLibTypeFlags.FDual),
//     InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
//    public interface IWebBrowser2  {
//        [DispId(203)]
//        object Document {
//            [return: MarshalAs(UnmanagedType.IDispatch)]
//            get;
//        }
//    }
    [ComImport, /*SuppressUnmanagedCodeSecurity,*/
     TypeLibType(TypeLibTypeFlags.FOleAutomation |
                 TypeLibTypeFlags.FDual |
                 TypeLibTypeFlags.FHidden),
     Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E")]
    public interface IWebBrowser2
    {
        [DispId(100)]
        void GoBack();

        [DispId(0x65)]
        void GoForward();

        [DispId(0x66)]
        void GoHome();

        [DispId(0x67)]
        void GoSearch();

        [DispId(0x68)]
        void Navigate([In] string Url,
            [In] ref object flags,
            [In] ref object targetFrameName,
            [In] ref object postData,
            [In] ref object headers);

        [DispId(-550)]
        void Refresh();

        [DispId(0x69)]
        void Refresh2([In] ref object level);

        [DispId(0x6a)]
        void Stop();

        [DispId(200)]
        object Application
        {
            [return:
                MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        [DispId(0xc9)]
        object Parent
        {
            [return:
                MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        [DispId(0xca)]
        object Container
        {
            [return:
                MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        [DispId(0xcb)]
        object Document
        {
            [return:
                MarshalAs(UnmanagedType.IDispatch)]
            get;
        }

        [DispId(0xcc)] bool TopLevelContainer { get; }
        [DispId(0xcd)] string Type { get; }
        [DispId(0xce)] int Left { get; set; }
        [DispId(0xcf)] int Top { get; set; }
        [DispId(0xd0)] int Width { get; set; }
        [DispId(0xd1)] int Height { get; set; }
        [DispId(210)] string LocationName { get; }
        [DispId(0xd3)] string LocationURL { get; }
        [DispId(0xd4)] bool Busy { get; }

        [DispId(300)]
        void Quit();

        [DispId(0x12d)]
        void ClientToWindow(out int pcx, out int pcy);

        [DispId(0x12e)]
        void PutProperty([In] string property, [In] object vtValue);

        [DispId(0x12f)]
        object GetProperty([In] string property);

        [DispId(0)] string Name { get; }
        [DispId(-515)] int HWND { get; }
        [DispId(400)] string FullName { get; }
        [DispId(0x191)] string Path { get; }
        [DispId(0x192)] bool Visible { get; set; }
        [DispId(0x193)] bool StatusBar { get; set; }
        [DispId(0x194)] string StatusText { get; set; }
        [DispId(0x195)] int ToolBar { get; set; }
        [DispId(0x196)] bool MenuBar { get; set; }
        [DispId(0x197)] bool FullScreen { get; set; }

        [DispId(500)]
        void Navigate2([In] ref object URL,
            [In] ref object flags,
            [In] ref object targetFrameName,
            [In] ref object postData,
            [In] ref object headers);

        [DispId(550)] bool Offline { get; set; }
        [DispId(0x227)] bool Silent { get; set; }
        [DispId(0x228)] bool RegisterAsBrowser { get; set; }
        [DispId(0x229)] bool RegisterAsDropTarget { get; set; }
        [DispId(0x22a)] bool TheaterMode { get; set; }
        [DispId(0x22b)] bool AddressBar { get; set; }
        [DispId(0x22c)] bool Resizable { get; set; }
    }
}