// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: https://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Runtime.InteropServices;

namespace Greenshot.Plugin.Office.OfficeInterop
{
    // The members of Word we use, see OfficeApplication for how this works

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordApplication
    {
        IWordDocuments Documents { get; }
        IWordSelection Selection { get; }
        bool Visible { get; set; }
        void Activate();
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordDocuments
    {
        int Count { get; }
        IWordDocument Item(object index);
        IWordDocument Add();
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordDocument
    {
        IWordApplication Application { get; }
        IWordWindow ActiveWindow { get; }
        bool ReadOnly { get; }
        bool Final { get; }
        void Activate();
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordWindow
    {
        string Caption { get; }
        IWordPane ActivePane { get; }
        void Activate();
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordPane
    {
        IWordView View { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordView
    {
        IWordZoom Zoom { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordZoom
    {
        int Percentage { get; set; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordSelection
    {
        IWordInlineShapes InlineShapes { get; }
        void InsertAfter(string text);
        int MoveDown(WdUnits unit, int count);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordInlineShapes
    {
        IWordInlineShape AddPicture(string fileName, bool linkToFile, bool saveWithDocument);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWordInlineShape
    {
        MsoTriState LockAspectRatio { set; }
    }

    /// <summary>
    /// WdUnits
    /// </summary>
    public enum WdUnits
    {
        wdLine = 5
    }
}
