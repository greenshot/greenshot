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

using System;
using System.Collections.Generic;
using System.Drawing;
using Dapplo.Windows.Com;
using Dapplo.Windows.User32;
using Greenshot.Plugin.Office.OfficeInterop;

namespace Greenshot.Plugin.Office.OfficeExport
{
    /// <summary>
    ///     Excel exporter
    /// </summary>
    public static class ExcelExporter
    {
        /// <summary>
        ///     Call this to get the running Excel application, returns null if there isn't any.
        /// </summary>
        /// <returns>ComDisposable for Excel.Application or null</returns>
        private static IDisposableCom<IExcelApplication> GetExcelApplication() => OfficeApplication.GetActive<IExcelApplication>("Excel.Application");

        /// <summary>
        ///     Call this to get the running Excel application, or create a new instance
        /// </summary>
        /// <returns>ComDisposable for Excel.Application</returns>
        private static IDisposableCom<IExcelApplication> GetOrCreateExcelApplication() => OfficeApplication.GetOrCreate<IExcelApplication>("Excel.Application");

        /// <summary>
        ///     Get all currently opened workbooks
        /// </summary>
        /// <returns>IEnumerable with names of the workbooks</returns>
        public static IEnumerable<string> GetWorkbooks()
        {
            using var excelApplication = GetExcelApplication();
            if (excelApplication == null)
            {
                yield break;
            }

            using var workbooks = DisposableCom.Create(excelApplication.ComObject.Workbooks);
            for (int i = 1; i <= workbooks.ComObject.Count; i++)
            {
                using var workbook = DisposableCom.Create(OfficeApplication.GetItem<IExcelWorkbook>(workbooks.ComObject, i));
                if (workbook != null)
                {
                    yield return workbook.ComObject.Name;
                }
            }
        }

        /// <summary>
        ///     Insert image from supplied tmp file into the give excel workbook
        /// </summary>
        /// <param name="workbookName"></param>
        /// <param name="tmpFile"></param>
        /// <param name="imageSize"></param>
        /// <returns>true if it worked</returns>
        public static bool InsertIntoExistingWorkbook(string workbookName, string tmpFile, Size imageSize)
        {
            using var excelApplication = GetExcelApplication();
            if (excelApplication == null)
            {
                return false;
            }

            using var workbooks = DisposableCom.Create(excelApplication.ComObject.Workbooks);
            for (int i = 1; i <= workbooks.ComObject.Count; i++)
            {
                using var workbook = DisposableCom.Create(OfficeApplication.GetItem<IExcelWorkbook>(workbooks.ComObject, i));
                if (workbook != null && workbook.ComObject.Name == workbookName)
                {
                    return InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
                }
            }

            return false;
        }

        /// <summary>
        ///     Insert a file into an already created workbook
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="tmpFile"></param>
        /// <param name="imageSize"></param>
        private static bool InsertIntoExistingWorkbook(IDisposableCom<IExcelWorkbook> workbook, string tmpFile, Size imageSize)
        {
            using var workSheet = DisposableCom.Create(workbook.ComObject.ActiveSheet);
            if (workSheet == null)
            {
                return false;
            }

            using var shapes = DisposableCom.Create(workSheet.ComObject.Shapes);
            if (shapes == null)
            {
                return false;
            }

            using var shape = DisposableCom.Create(shapes.ComObject.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, 0, 0, imageSize.Width, imageSize.Height));
            if (shape == null)
            {
                return false;
            }

            shape.ComObject.Top = 40;
            shape.ComObject.Left = 40;
            shape.ComObject.LockAspectRatio = MsoTriState.msoTrue;
            shape.ComObject.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
            shape.ComObject.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
            workbook.ComObject.Activate();
            using var application = DisposableCom.Create(workbook.ComObject.Application);
            User32Api.SetForegroundWindow((IntPtr) application.ComObject.Hwnd);
            return true;
        }

        /// <summary>
        ///     Add an image-file to a newly created workbook
        /// </summary>
        /// <param name="tmpFile"></param>
        /// <param name="imageSize"></param>
        /// <returns>true if it worked</returns>
        public static bool InsertIntoNewWorkbook(string tmpFile, Size imageSize)
        {
            using var excelApplication = GetOrCreateExcelApplication();
            excelApplication.ComObject.Visible = true;
            using var workbooks = DisposableCom.Create(excelApplication.ComObject.Workbooks);
            using var workbook = DisposableCom.Create(workbooks.ComObject.Add());
            return InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
        }
    }
}