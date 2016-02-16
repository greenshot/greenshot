/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Runtime.InteropServices;
using Greenshot.Addon.Core;
using Microsoft.Office.Core;
using Excel = Microsoft.Office.Interop.Excel;

namespace Greenshot.Addon.Office.OfficeExport
{
	/// <summary>
	/// Excel exporter
	/// </summary>
	public class ExcelExporter
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(ExcelExporter));
		private static Version _excelVersion;

		/// <summary>
		/// Get all currently opened workbooks
		/// </summary>
		/// <returns>IEnumerable with names of the workbooks</returns>
		public static IEnumerable<string> GetWorkbooks()
		{
			using (var excelApplication = GetExcelApplication())
			{
				if (excelApplication == null || excelApplication.ComObject == null)
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
		/// Insert image from supplied tmp file into the give excel workbook
		/// </summary>
		/// <param name="workbookName"></param>
		/// <param name="tmpFile"></param>
		/// <param name="imageSize"></param>
		public static void InsertIntoExistingWorkbook(string workbookName, string tmpFile, Size imageSize)
		{
			using (var excelApplication = GetExcelApplication())
			{
				if (excelApplication == null || excelApplication.ComObject == null)
				{
					return;
				}
				using (var workbooks = DisposableCom.Create(excelApplication.ComObject.Workbooks))
				{
					for (int i = 1; i <= workbooks.ComObject.Count; i++)
					{
						using (var workbook = DisposableCom.Create((Excel._Workbook) workbooks.ComObject[i]))
						{
							if (workbook != null && workbook.ComObject.Name.StartsWith(workbookName))
							{
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
		private static void InsertIntoExistingWorkbook(IDisposableCom<Excel._Workbook> workbook, string tmpFile, Size imageSize)
		{
			using (var workSheet = DisposableCom.Create(workbook.ComObject.ActiveSheet))
			{
				if (workSheet == null)
				{
					return;
				}
				using (var shapes = DisposableCom.Create(workSheet.ComObject.Shapes))
				{
					if (shapes != null)
					{
						using (var shape = DisposableCom.Create(shapes.ComObject.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, 0, 0, imageSize.Width, imageSize.Height)))
						{
							if (shape != null)
							{
								shape.ComObject.Top = 40;
								shape.ComObject.Left = 40;
								shape.ComObject.LockAspectRatio = MsoTriState.msoTrue;
								shape.ComObject.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
								shape.ComObject.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromTopLeft);
								workbook.ComObject.Activate();
								using (var application = DisposableCom.Create(workbook.ComObject.Application))
								{
									WindowDetails.ToForeground((IntPtr) application.ComObject.Hwnd);
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
		public static void InsertIntoNewWorkbook(string tmpFile, Size imageSize)
		{
			using (var excelApplication = GetOrCreateExcelApplication())
			{
				if (excelApplication != null)
				{
					excelApplication.ComObject.Visible = true;
					using (var workbooks = DisposableCom.Create(excelApplication.ComObject.Workbooks))
					{
						using (var workbook = DisposableCom.Create((Excel._Workbook) workbooks.ComObject.Add()))
						{
							InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
						}
					}
				}
			}
		}

		/// <summary>
		/// Call this to get the running Excel application, returns null if there isn't any.
		/// </summary>
		/// <returns>ComDisposable for Excel.Application or null</returns>
		private static IDisposableCom<Excel.Application> GetExcelApplication()
		{
			IDisposableCom<Excel.Application> excelApplication;
			try
			{
				excelApplication = DisposableCom.Create((Excel.Application) Marshal.GetActiveObject("Excel.Application"));
			}
			catch
			{
				// Ignore, probably no excel running
				return null;
			}
			if (excelApplication != null && excelApplication.ComObject != null)
			{
				InitializeVariables(excelApplication);
			}
			return excelApplication;
		}

		/// <summary>
		/// Call this to get the running Excel application, or create a new instance
		/// </summary>
		/// <returns>ComDisposable for Excel.Application</returns>
		private static IDisposableCom<Excel.Application> GetOrCreateExcelApplication()
		{
			IDisposableCom<Excel.Application> excelApplication = GetExcelApplication();
			if (excelApplication == null)
			{
				excelApplication = DisposableCom.Create(new Excel.Application());
			}
			InitializeVariables(excelApplication);
			return excelApplication;
		}

		/// <summary>
		/// Initialize static excel variables like version and currentuser
		/// </summary>
		/// <param name="excelApplication"></param>
		private static void InitializeVariables(IDisposableCom<Excel.Application> excelApplication)
		{
			if (excelApplication == null || excelApplication.ComObject == null || _excelVersion != null)
			{
				return;
			}
			if (!Version.TryParse(excelApplication.ComObject.Version, out _excelVersion))
			{
				Log.Warning("Assuming Excel version 1997.");
				_excelVersion = new Version((int) OfficeVersion.OFFICE_97, 0, 0, 0);
			}
		}
	}
}