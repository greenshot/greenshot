/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Interop.Office {
	public class ExcelExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExcelExporter));

		public static List<string> GetWorkbooks() {
			List<string> currentWorkbooks = new List<string>();
			using (IExcelApplication excelApplication = COMWrapper.GetInstance<IExcelApplication>()) {
				if (excelApplication != null) {
					for (int i = 1; i <= excelApplication.Workbooks.Count; i++) {
						IWorkbook workbook = excelApplication.Workbooks[i];
						if (workbook != null) {
							currentWorkbooks.Add(workbook.Name);
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
		public static void InsertIntoExistingWorkbook(string workbookName, string tmpFile) {
			using (IExcelApplication excelApplication = COMWrapper.GetInstance<IExcelApplication>()) {
				if (excelApplication != null) {
					for (int i = 1; i <= excelApplication.Workbooks.Count; i++) {
						IWorkbook workbook = excelApplication.Workbooks[i];
						if (workbook != null && workbook.Name.StartsWith(workbookName)) {
							InsertIntoExistingWorkbook(workbook, tmpFile);
						}
					}
				}
			}
		}

		private static void InsertIntoExistingWorkbook(IWorkbook workbook, string tmpFile) {
			IWorksheet sheet = workbook.ActiveSheet;
			if (sheet != null) {
				if (sheet.Pictures != null) {
					sheet.Pictures.Insert(tmpFile);
				}
			} else {
				LOG.Error("No pictures found");
			}
		}

		public static void InsertIntoNewWorkbook(string tmpFile) {
			using (IExcelApplication excelApplication = COMWrapper.GetOrCreateInstance<IExcelApplication>()) {
				if (excelApplication != null) {
					excelApplication.Visible = true;
					object template = Missing.Value;
					IWorkbook workbook = excelApplication.Workbooks.Add(template);
					InsertIntoExistingWorkbook(workbook, tmpFile);
				}
			}
		}
	}

}
