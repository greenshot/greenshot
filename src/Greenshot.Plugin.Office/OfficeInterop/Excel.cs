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
    // The members of Excel we use, see OfficeApplication for how this works

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IExcelApplication
    {
        IExcelWorkbooks Workbooks { get; }
        bool Visible { get; set; }
        int Hwnd { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IExcelWorkbooks
    {
        int Count { get; }
        IExcelWorkbook Item(object index);
        IExcelWorkbook Add();
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IExcelWorkbook
    {
        IExcelApplication Application { get; }
        string Name { get; }
        IExcelWorksheet ActiveSheet { get; }
        void Activate();
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IExcelWorksheet
    {
        IExcelShapes Shapes { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IExcelShapes
    {
        IExcelShape AddPicture(string fileName, MsoTriState linkToFile, MsoTriState saveWithDocument, float left, float top, float width, float height);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IExcelShape
    {
        float Top { get; set; }
        float Left { get; set; }
        MsoTriState LockAspectRatio { set; }
        void ScaleHeight(float factor, MsoTriState relativeToOriginalSize, MsoScaleFrom scale);
        void ScaleWidth(float factor, MsoTriState relativeToOriginalSize, MsoScaleFrom scale);
    }
}
