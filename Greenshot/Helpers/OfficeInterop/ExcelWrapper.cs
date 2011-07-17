/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Reflection;
using Greenshot.Interop;
using Greenshot.Plugin;

namespace Greenshot.Helpers.OfficeInterop {
	// See http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel.application.aspx
	[ComProgId("Excel.Application")]
	public interface IExcelApplication : Common {
		IWorkbook ActiveWorkbook { get; }
		//ISelection Selection {get;}
		IWorkbooks Workbooks {get;}
		bool Visible {get; set;}
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel.workbooks.aspx
	public interface IWorkbooks : Common {
		IWorkbook Add(object template);
	}
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.document.aspx
	public interface IWorkbook : Common {
		IWorksheet ActiveSheet {get;}
	}
	
	public interface IWorksheet : Common {
		IPictures Pictures {get;}
	}
	public interface IPictures : Common {
		void Insert(string file);
	}
	
	public class ExcelExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExcelExporter));

		private static IExcelApplication ExcelApplication() {
			return (IExcelApplication)COMWrapper.GetOrCreateInstance(typeof(IExcelApplication));
		}

		public static void InsertIntoExistingWorkbook(IWorkbook workbook, string tmpFile) {
			workbook.ActiveSheet.Pictures.Insert(tmpFile);
		}
		
		private static void InsertIntoNewWorkbook(IExcelApplication excelApplication, string tmpFile) {
			LOG.Debug("No workbook, creating a new workbook");
			object template = Missing.Value;
			IWorkbook workbook = excelApplication.Workbooks.Add(template);
			InsertIntoExistingWorkbook(workbook, tmpFile);
		}

		public static void ExportToExcel(string tmpFile) {
			using( IExcelApplication excelApplication = ExcelApplication() ) {
				if (excelApplication != null) {
					excelApplication.Visible = true;
					if (excelApplication.ActiveWorkbook != null) {
						InsertIntoExistingWorkbook(excelApplication.ActiveWorkbook, tmpFile);
					} else {
						InsertIntoNewWorkbook(excelApplication, tmpFile);
					}
					return;
				}
			}
		}
	}
}
