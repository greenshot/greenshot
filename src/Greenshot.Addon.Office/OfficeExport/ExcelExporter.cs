// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
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
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using Dapplo.Log;
using Dapplo.Windows.Com;
using Dapplo.Windows.Desktop;
using Greenshot.Addon.Office.OfficeInterop;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using Version = System.Version;

namespace Greenshot.Addon.Office.OfficeExport
{
    /// <summary>
    ///     Excel exporter
    /// </summary>
    public static class ExcelExporter
    {
        private static readonly LogSource Log = new LogSource();
        private static Version _excelVersion;

        /// <summary>
        ///     Call this to get the running Excel application, returns null if there isn't any.
        /// </summary>
        /// <returns>ComDisposable for Excel.Application or null</returns>
        private static IDisposableCom<Application> GetExcelApplication()
        {
            IDisposableCom<Application> excelApplication;
            try
            {
                excelApplication = OleAut32Api.GetActiveObject<Application>("Excel.Application");
            }
            catch
            {
                // Ignore, probably no excel running
                return null;
            }
            if (excelApplication?.ComObject != null)
            {
                InitializeVariables(excelApplication);
            }
            return excelApplication;
        }

        /// <summary>
        ///     Call this to get the running Excel application, or create a new instance
        /// </summary>
        /// <returns>ComDisposable for Excel.Application</returns>
        private static IDisposableCom<Application> GetOrCreateExcelApplication()
        {
            var excelApplication = GetExcelApplication();
            if (excelApplication == null)
            {
                excelApplication = DisposableCom.Create(new Application());
            }
            InitializeVariables(excelApplication);
            return excelApplication;
        }

        /// <summary>
        ///     Get all currently opened workbooks
        /// </summary>
        /// <returns>IEnumerable with names of the workbooks</returns>
        public static IEnumerable<string> GetWorkbooks()
        {
            using (var excelApplication = GetExcelApplication())
            {
                if ((excelApplication == null) || (excelApplication.ComObject == null))
                {
                    yield break;
                }
                using (var workbooks = DisposableCom.Create(excelApplication.ComObject.Workbooks))
                {
                    for (int i = 1; i <= workbooks.ComObject.Count; i++)
                    {
                        using (var workbook = DisposableCom.Create(workbooks.ComObject[i]))
                        {
                            if (workbook != null)
                            {
                                yield return workbook.ComObject.Name;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Initialize static excel variables like version and currentuser
        /// </summary>
        /// <param name="excelApplication"></param>
        private static void InitializeVariables(IDisposableCom<Application> excelApplication)
        {
            if ((excelApplication == null) || (excelApplication.ComObject == null) || (_excelVersion != null))
            {
                return;
            }
            if (!Version.TryParse(excelApplication.ComObject.Version, out _excelVersion))
            {
                Log.Warn().WriteLine("Assuming Excel version 1997.");
                _excelVersion = new Version((int)OfficeVersions.Office97, 0, 0, 0);
            }
        }

        /// <summary>
        ///     Insert image from supplied tmp file into the give excel workbook
        /// </summary>
        /// <param name="workbookName"></param>
        /// <param name="tmpFile"></param>
        /// <param name="imageSize"></param>
        public static void InsertIntoExistingWorkbook(string workbookName, string tmpFile, Size imageSize)
        {
            using (var excelApplication = GetExcelApplication())
            {
                if ((excelApplication == null) || (excelApplication.ComObject == null))
                {
                    return;
                }
                using (var workbooks = DisposableCom.Create(excelApplication.ComObject.Workbooks))
                {
                    for (int i = 1; i <= workbooks.ComObject.Count; i++)
                    {
                        using (var workbook = DisposableCom.Create((_Workbook)workbooks.ComObject[i]))
                        {
                            if ((workbook != null) && workbook.ComObject.Name.StartsWith(workbookName))
                            {
                                InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Insert a file into an already created workbook
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="tmpFile"></param>
        /// <param name="imageSize"></param>
        private static void InsertIntoExistingWorkbook(IDisposableCom<_Workbook> workbook, string tmpFile, Size imageSize)
        {
            using (var workSheet = DisposableCom.Create(workbook.ComObject.ActiveSheet as Worksheet))
            {
                if (workSheet == null)
                {
                    return;
                }
                using (var shapes = DisposableCom.Create(workSheet.ComObject.Shapes))
                {
                    if (shapes == null)
                    {
                        return;
                    }

                    using (var shape = DisposableCom.Create(shapes.ComObject.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, 0, 0, imageSize.Width, imageSize.Height)))
                    {
                        if (shape == null)
                        {
                            return;
                        }

                        shape.ComObject.Top = 40;
                        shape.ComObject.Left = 40;
                        shape.ComObject.LockAspectRatio = MsoTriState.msoTrue;
                        shape.ComObject.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
                        shape.ComObject.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
                        workbook.ComObject.Activate();
                        using (var application = DisposableCom.Create(workbook.ComObject.Application))
                        {
                            var excelWindow = InteropWindowFactory.CreateFor((IntPtr) application.ComObject.Hwnd);
                            excelWindow.ToForegroundAsync();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Add an image-file to a newly created workbook
        /// </summary>
        /// <param name="tmpFile"></param>
        /// <param name="imageSize"></param>
        public static void InsertIntoNewWorkbook(string tmpFile, Size imageSize)
        {
            using (var excelApplication = GetOrCreateExcelApplication())
            {
                if (excelApplication == null)
                {
                    return;
                }

                excelApplication.ComObject.Visible = true;
                using (var workbooks = DisposableCom.Create(excelApplication.ComObject.Workbooks))
                {
                    using (var workbook = DisposableCom.Create((_Workbook)workbooks.ComObject.Add()))
                    {
                        InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
                    }
                }
            }
        }
    }

}