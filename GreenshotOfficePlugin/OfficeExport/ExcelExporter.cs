#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Dapplo.Windows.Desktop;
using GreenshotOfficePlugin.OfficeInterop;
using GreenshotPlugin.Interop;
using Dapplo.Log;

#endregion

namespace GreenshotOfficePlugin.OfficeExport
{
	public class ExcelExporter
	{
		private static readonly LogSource Log = new LogSource();
		private static Version _excelVersion;

		/// <summary>
		///     Get all currently opened workbooks
		/// </summary>
		/// <returns>List of string with names of the workbooks</returns>
		public static List<string> GetWorkbooks()
		{
			var currentWorkbooks = new List<string>();
			using (var excelApplication = GetExcelApplication())
			{
				if (excelApplication == null)
				{
					return currentWorkbooks;
				}
				using (var workbooks = excelApplication.Workbooks)
				{
					for (var i = 1; i <= workbooks.Count; i++)
					{
						using (var workbook = workbooks[i])
						{
							if (workbook != null)
							{
								currentWorkbooks.Add(workbook.Name);
							}
						}
					}
				}
			}
			currentWorkbooks.Sort();
			return currentWorkbooks;
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
				if (excelApplication == null)
				{
					return;
				}
				using (var workbooks = excelApplication.Workbooks)
				{
					for (var i = 1; i <= workbooks.Count; i++)
					{
						using (var workbook = workbooks[i])
						{
							if (workbook != null && workbook.Name.StartsWith(workbookName))
							{
								InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
							}
						}
					}
				}
				var hWnd = excelApplication.Hwnd;
				if (hWnd > 0)
				{
					// TODO: Await
					InteropWindowFactory.CreateFor(new IntPtr(hWnd)).ToForegroundAsync();
				}
			}
		}

		/// <summary>
		///     Insert a file into an already created workbook
		/// </summary>
		/// <param name="workbook"></param>
		/// <param name="tmpFile"></param>
		/// <param name="imageSize"></param>
		private static void InsertIntoExistingWorkbook(IWorkbook workbook, string tmpFile, Size imageSize)
		{
			var workSheet = workbook.ActiveSheet;
			if (workSheet == null)
			{
				return;
			}
			using (var shapes = workSheet.Shapes)
			{
				if (shapes != null)
				{
					using (var shape = shapes.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, 0, 0, imageSize.Width, imageSize.Height))
					{
						if (shape != null)
						{
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
		///     Add an image-file to a newly created workbook
		/// </summary>
		/// <param name="tmpFile"></param>
		/// <param name="imageSize"></param>
		public static void InsertIntoNewWorkbook(string tmpFile, Size imageSize)
		{
			using (var excelApplication = GetOrCreateExcelApplication())
			{
				if (excelApplication != null)
				{
					excelApplication.Visible = true;
					object template = Missing.Value;
					using (var workbooks = excelApplication.Workbooks)
					{
						var workbook = workbooks.Add(template);
						InsertIntoExistingWorkbook(workbook, tmpFile, imageSize);
					}
				}
			}
		}


		/// <summary>
		///     Call this to get the running Excel application, returns null if there isn't any.
		/// </summary>
		/// <returns>IExcelApplication or null</returns>
		private static IExcelApplication GetExcelApplication()
		{
			var excelApplication = COMWrapper.GetInstance<IExcelApplication>();
			InitializeVariables(excelApplication);
			return excelApplication;
		}

		/// <summary>
		///     Call this to get the running Excel application, or create a new instance
		/// </summary>
		/// <returns>IExcelApplication</returns>
		private static IExcelApplication GetOrCreateExcelApplication()
		{
			var excelApplication = COMWrapper.GetOrCreateInstance<IExcelApplication>();
			InitializeVariables(excelApplication);
			return excelApplication;
		}

		/// <summary>
		///     Initialize static outlook variables like version and currentuser
		/// </summary>
		/// <param name="excelApplication"></param>
		private static void InitializeVariables(IExcelApplication excelApplication)
		{
			if (excelApplication == null || _excelVersion != null)
			{
				return;
			}
			try
			{
				_excelVersion = new Version(excelApplication.Version);
				Log.Info().WriteLine("Using Excel {0}", _excelVersion);
			}
			catch (Exception exVersion)
			{
				Log.Error().WriteLine(exVersion);
				Log.Warn().WriteLine("Assuming Excel version 1997.");
				_excelVersion = new Version((int) OfficeVersions.Office97, 0, 0, 0);
			}
		}
	}
}