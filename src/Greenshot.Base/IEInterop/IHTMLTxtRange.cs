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
    /// See <a href="https://msdn.microsoft.com/en-us/library/aa741548%28v=vs.85%29.aspx">here</a>
    /// </summary>
    [ComImport, Guid("3050F220-98B5-11CF-BB82-00AA00BDCE0B"),
     TypeLibType(TypeLibTypeFlags.FDual),
     InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IHTMLTxtRange
    {
        [DispId(1006)]
        IHTMLElement parentElement();

        [DispId(1008)]
        IHTMLTxtRange duplicate();

        [DispId(1010)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool inRange(IHTMLTxtRange range);

        [DispId(1011)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool isEqual(IHTMLTxtRange range);

        [DispId(1012)]
        void scrollIntoView([MarshalAs(UnmanagedType.VariantBool)] bool fStart);

        [DispId(1013)]
        void collapse([MarshalAs(UnmanagedType.VariantBool)] bool Start);

        [DispId(1014)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool expand([MarshalAs(UnmanagedType.BStr)] string Unit);

        [DispId(1015)]
        int move([MarshalAs(UnmanagedType.BStr)] string Unit, int Count);

        [DispId(1016)]
        int moveStart([MarshalAs(UnmanagedType.BStr)] string Unit, int Count);

        [DispId(1017)]
        int moveEnd([MarshalAs(UnmanagedType.BStr)] string Unit, int Count);

        [DispId(1024)]
        void select();

        [DispId(1026)]
        void pasteHTML([MarshalAs(UnmanagedType.BStr)] string html);

        [DispId(1001)]
        void moveToElementText(IHTMLElement element);

        [DispId(1025)]
        void setEndPoint([MarshalAs(UnmanagedType.BStr)] string how, IHTMLTxtRange SourceRange);

        [DispId(1018)]
        int compareEndPoints([MarshalAs(UnmanagedType.BStr)] string how, IHTMLTxtRange SourceRange);

        [DispId(1019)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool findText([MarshalAs(UnmanagedType.BStr)] string String, int Count, int Flags);

        [DispId(1020)]
        void moveToPoint(int x, int y);

        [DispId(1021)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string getBookmark();

        [DispId(1009)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool moveToBookmark([MarshalAs(UnmanagedType.BStr)] string Bookmark);

        [DispId(1027)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool queryCommandSupported([MarshalAs(UnmanagedType.BStr)] string cmdID);

        [DispId(1028)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool queryCommandEnabled([MarshalAs(UnmanagedType.BStr)] string cmdID);

        [DispId(1029)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool queryCommandState([MarshalAs(UnmanagedType.BStr)] string cmdID);

        [DispId(1030)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool queryCommandIndeterm([MarshalAs(UnmanagedType.BStr)] string cmdID);

        [DispId(1031)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string queryCommandText([MarshalAs(UnmanagedType.BStr)] string cmdID);

        [DispId(1032)]
        object queryCommandValue([MarshalAs(UnmanagedType.BStr)] string cmdID);

        [DispId(1033)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool execCommand([MarshalAs(UnmanagedType.BStr)] string cmdID, [MarshalAs(UnmanagedType.VariantBool)] bool showUI, object value);

        [DispId(1034)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool execCommandShowHelp([MarshalAs(UnmanagedType.BStr)] string cmdID);

        string htmlText
        {
            [DispId(1003)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        string text
        {
            [DispId(1004)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            [DispId(1004)] set;
        }
    }
}