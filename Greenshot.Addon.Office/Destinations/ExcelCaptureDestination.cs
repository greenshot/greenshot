//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core;
using Greenshot.Core.Interfaces;
using Greenshot.Legacy.Extensions;

#endregion

namespace Greenshot.Addon.Office.Destinations
{
	/// <summary>
	///     ExcelCaptureDestination implements ICaptureDestination for Excel
	/// </summary>
	public sealed class ExcelCaptureDestination : ICaptureDestination
	{
		public string Name { get; } = nameof(ExcelCaptureDestination);

		/// <summary>
		/// Workbook to export to
		/// </summary>
		public string Workbook { get; set; }


		public Task ExportCaptureAsync(ICaptureFlow captureFlow, CancellationToken cancellationToken = new CancellationToken())
		{
			bool createdFile = false;
			var capture = captureFlow.Capture;
			string imageFile = capture.CaptureDetails.Filename;
			try
			{
				if ((imageFile == null) || capture.Modified || !Regex.IsMatch(imageFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
				{
					imageFile = capture.SaveNamedTmpFile(capture.CaptureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
					createdFile = true;
				}
				if (string.IsNullOrWhiteSpace(Workbook))
				{
					ExcelExporter.InsertIntoNewWorkbook(imageFile, capture.Image.Size);
				}
				else
				{
					ExcelExporter.InsertIntoExistingWorkbook(Workbook, imageFile, capture.Image.Size);
				}
			}
			finally
			{
				if (createdFile)
				{
					ImageOutput.DeleteNamedTmpFile(imageFile);
				}
			}
			return Task.FromResult(true);
		}
	}
}