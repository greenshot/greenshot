/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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

namespace Greenshot.Interop.Office {
	// See http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel.application.aspx
	[ComProgId("Excel.Application")]
	public interface IExcelApplication : ICommon {
		IWorkbook ActiveWorkbook {
			get;
		}
		//ISelection Selection {get;}
		IWorkbooks Workbooks {
			get;
		}
		bool Visible {
			get;
			set;
		}
		string Version {
			get;
		}

		/// <summary>
		/// Returns an Integer indicating the top-level window handle of the Microsoft Excel window.
		/// </summary>
		int Hwnd
		{
			get;
		}
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel.workbooks.aspx
	public interface IWorkbooks : ICommon, ICollection {
		IWorkbook Add(object template);
		// Use index + 1!!
		IWorkbook this[object index] {
			get;
		}
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel.workbook.aspx
	public interface IWorkbook : ICommon {
		IWorksheet ActiveSheet {
			get;
		}
		string Name {
			get;
		}
		void Activate();
		IWorksheets Worksheets {
			get;
		}
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel._worksheet_members.aspx
	public interface IWorksheet : ICommon {
		IPictures Pictures {
			get;
		}
		IShapes Shapes {
			get;
		}
		string Name {
			get;
		}
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel.iworksheets_members.aspx
	public interface IWorksheets : ICollection {
		// Use index + 1!!
		IWorksheet this[object index] {
			get;
		}
	}

	public interface IPictures : ICollection {
		// Use index + 1!!
		//IPicture this[object Index] { get; }
		void Insert(string file);
	}
}
