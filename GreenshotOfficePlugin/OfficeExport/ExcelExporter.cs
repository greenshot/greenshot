/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Greenshot.Interop;
using System.Drawing;
using GreenshotOfficePlugin;
using Greenshot.IniFile;

namespace Greenshot.Interop.Office {
	public class ExcelExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExcelExporter));
		private static readonly OfficeConfiguration officeConfiguration = IniConfig.GetIniSection<OfficeConfiguration>();
		private static Version excelVersion;

		/// <summary>
		/// Get all currently opened workbooks
		/// </summary>
		/// <returns>List<string> with names of the workbooks</returns>
		public static List<string> GetWorkbooks() {
			List<string> currentWorkbooks = new List<string>();
			using (IExcelApplication excelApplication = GetExcelApplication()) {
				if (excelApplication == null) {
					return currentWorkbooks;
				}
				using (IWorkbooks workbooks = excelApplication.Workbooks) {
					for (int i = 1; i <= workbooks.Count; i++) {
						using (IWorkbook workbook = workbooks[i]) {
							if (workbook != null) {
								currentWorkbooks.Add(workbook.Name);
							}
						}
					}
				}
			}
			return currentWorkbooks;
		}

		/// <summary>
		/// Insert image from supplied tmp file into the give excel workbook
		/// </summary>
		/// <param name="workbookName"></param>
		/// <param name="tmpFile"></param>
		public static void InsertIntoExistingWorkbook(string workbookName, string tmpFile, Size imageSize) {
			using (IExcelApplication excelApplication = GetExcelApplication()) {
				if (excelApplication == null) {
					return;
				}
				using (IWorkbooks workbooks = excelApplication.Workbooks) {
					for (int i = 1; i <= workbooks.Count; i++) {
						using (IWorkbook workbook = workbooks[i]) {
							if (workbook != null && workbook.Name.StartsWith(workbookName)) {
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
		private static void InsertIntoExistingWorkbook(IWorkbook workbook, string tmpFile, Size imageSize) {
			IWorksheet workSheet = workbook.ActiveSheet;
			if (workSheet == null) {
				return;
			}
			using (IShapes shapes = workSheet.Shapes) {
				if (shapes != null) {
					using (IShape shape = shapes.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, 0, 0, imageSize.Width, imageSize.Height)) {
						if (shape != null) {
							shape.Top = 40;
							shape.Left = 40;
							shape.LockAspectRatio = MsoTriState.msoTrue;
							shape.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
							shape.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
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
			using (IExcelApplication excelApplication = GetOrCreateExcelApplication()) {
				if (excelApplication != null) {
					excelApplication.Visible = true;
					object template = Missing.Value;
					using (IWorkbooks workbooks = excelApplication.Workbooks) {
						IWorkbook workbook = workbooks.Add(template);
						InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
					}
				}
			}
		}


		/// <summary>
		/// Call this to get the running Excel application, returns null if there isn't any.
		/// </summary>
		/// <returns>IExcelApplication or null</returns>
		private static IExcelApplication GetExcelApplication() {
			IExcelApplication excelApplication = COMWrapper.GetInstance<IExcelApplication>();
			InitializeVariables(excelApplication);
			return excelApplication;
		}

		/// <summary>
		/// Call this to get the running Excel application, or create a new instance
		/// </summary>
		/// <returns>IExcelApplication</returns>
		private static IExcelApplication GetOrCreateExcelApplication() {
			IExcelApplication excelApplication = COMWrapper.GetOrCreateInstance<IExcelApplication>();
			InitializeVariables(excelApplication);
			return excelApplication;
		}

		/// <summary>
		/// Initialize static outlook variables like version and currentuser
		/// </summary>
		/// <param name="excelApplication"></param>
		private static void InitializeVariables(IExcelApplication excelApplication) {
			if (excelApplication == null || excelVersion != null) {
				return;
			}
			try {
				excelVersion = new Version(excelApplication.Version);
				LOG.InfoFormat("Using Excel {0}", excelVersion);
			} catch (Exception exVersion) {
				LOG.Error(exVersion);
				LOG.Warn("Assuming Excel version 1997.");
				excelVersion = new Version((int)OfficeVersion.OFFICE_97, 0, 0, 0);
			}
		}
	}
}
