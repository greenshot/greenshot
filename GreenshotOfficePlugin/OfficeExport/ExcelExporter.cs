/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Greenshot.IniFile;
using Greenshot.Interop;
using GreenshotOfficePlugin;
using Microsoft.Office.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace GreenshotOfficePlugin.OfficeExport {
	public class ExcelExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExcelExporter));
		private static readonly OfficeConfiguration officeConfiguration = IniConfig.GetIniSection<OfficeConfiguration>();
		private static Version excelVersion;

		/// <summary>
		/// Get all currently opened workbooks
		/// </summary>
		/// <returns>IEnumerable with names of the workbooks</returns>
		public static IEnumerable<string> GetWorkbooks() {
			using (var excelApplication = GetExcelApplication()) {
				if (excelApplication == null || excelApplication.ComObject == null) {
					yield break;
				}
				using (var workbooks = ComDisposableFactory.Create(excelApplication.ComObject.Workbooks)) {
					for (int i = 1; i <= workbooks.ComObject.Count; i++) {
						using (var workbook = ComDisposableFactory.Create(workbooks.ComObject[i])) {
							if (workbook != null) {
								yield return workbook.ComObject.Name;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Insert image from supplied tmp file into the give excel workbook
		/// </summary>
		/// <param name="workbookName"></param>
		/// <param name="tmpFile"></param>
		public static void InsertIntoExistingWorkbook(string workbookName, string tmpFile, Size imageSize) {
			using (var excelApplication = GetExcelApplication()) {
				if (excelApplication == null || excelApplication.ComObject == null) {
					return;
				}
				using (var workbooks = ComDisposableFactory.Create(excelApplication.ComObject.Workbooks)) {
					for (int i = 1; i <= workbooks.ComObject.Count; i++) {
						using (var workbook = ComDisposableFactory.Create((Excel._Workbook)workbooks.ComObject[i])) {
							if (workbook != null && workbook.ComObject.Name.StartsWith(workbookName)) {
								InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Insert a file into an already created workbook
		/// </summary>
		/// <param name="workbook"></param>
		/// <param name="tmpFile"></param>
		/// <param name="imageSize"></param>
		private static void InsertIntoExistingWorkbook(ComDisposable<Excel._Workbook> workbook, string tmpFile, Size imageSize) {
			using (var workSheet = ComDisposableFactory.Create(workbook.ComObject.ActiveSheet)) {
				if (workSheet == null) {
					return;
				}
				using (var shapes = ComDisposableFactory.Create(workSheet.ComObject.Shapes)) {
					if (shapes != null) {
						using (var shape = ComDisposableFactory.Create(shapes.ComObject.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, 0, 0, imageSize.Width, imageSize.Height))) {
							if (shape != null) {
								shape.ComObject.Top = 40;
								shape.ComObject.Left = 40;
								shape.ComObject.LockAspectRatio = MsoTriState.msoTrue;
								shape.ComObject.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
								shape.ComObject.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
								workbook.ComObject.Activate();
								using (var application = ComDisposableFactory.Create(workbook.ComObject.Application)) {
									GreenshotPlugin.Core.WindowDetails.ToForeground((IntPtr)application.ComObject.Hwnd);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Add an image-file to a newly created workbook
		/// </summary>
		/// <param name="tmpFile"></param>
		/// <param name="imageSize"></param>
		public static void InsertIntoNewWorkbook(string tmpFile, Size imageSize) {
			using (var excelApplication = GetOrCreateExcelApplication()) {
				if (excelApplication != null) {
					excelApplication.ComObject.Visible = true;
					using (var workbooks = ComDisposableFactory.Create(excelApplication.ComObject.Workbooks))
					using (var workbook = ComDisposableFactory.Create((Excel._Workbook)workbooks.ComObject.Add())) {
						InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
					}
				}
			}
		}

		/// <summary>
		/// Call this to get the running Excel application, returns null if there isn't any.
		/// </summary>
		/// <returns>ComDisposable for Excel.Application or null</returns>
		private static ComDisposable<Excel.Application> GetExcelApplication() {
			ComDisposable<Excel.Application> excelApplication = null;
			try {
				excelApplication = ComDisposableFactory.Create((Excel.Application)Marshal.GetActiveObject("Excel.Application"));
			} catch (Exception) {
				// Ignore, probably no excel running
				return null;
			}
			if (excelApplication != null && excelApplication.ComObject != null) {
				InitializeVariables(excelApplication);
			}
			return excelApplication;
		}

		/// <summary>
		/// Call this to get the running Excel application, or create a new instance
		/// </summary>
		/// <returns>ComDisposable for Excel.Application</returns>
		private static ComDisposable<Excel.Application> GetOrCreateExcelApplication() {
			ComDisposable<Excel.Application> excelApplication = GetExcelApplication();
			if (excelApplication == null) {
				excelApplication = ComDisposableFactory.Create(new Excel.Application());
			}
			InitializeVariables(excelApplication);
			return excelApplication;
		}

		/// <summary>
		/// Initialize static outlook variables like version and currentuser
		/// </summary>
		/// <param name="excelApplication"></param>
		private static void InitializeVariables(ComDisposable<Excel.Application> excelApplication) {
			if (excelApplication == null || excelApplication.ComObject == null || excelVersion != null) {
				return;
			}
			try {
				excelVersion = new Version(excelApplication.ComObject.Version);
				LOG.InfoFormat("Using Excel {0}", excelVersion);
			} catch (Exception exVersion) {
				LOG.Error(exVersion);
				LOG.Warn("Assuming Excel version 1997.");
				excelVersion = new Version((int)Greenshot.Interop.Office.OfficeVersion.OFFICE_97, 0, 0, 0);
			}
		}
	}
}
