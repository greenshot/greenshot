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

using Greenshot.Plugin;
using GreenshotOfficePlugin.OfficeExport;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotOfficePlugin
{
	/// <summary>
	/// ExcelDestination
	/// </summary>
	public class ExcelLegacyDestination : AbstractLegacyDestination
	{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (ExcelLegacyDestination));
		private const int ICON_APPLICATION = 0;
		private const int ICON_WORKBOOK = 1;
		private static string exePath = null;
		private string _workbookName = null;

		static ExcelLegacyDestination()
		{
			exePath = PluginUtils.GetExePath("EXCEL.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("excel");
			}
			else
			{
				exePath = null;
			}
		}

		public ExcelLegacyDestination()
		{
		}

		public ExcelLegacyDestination(string workbookName)
		{
			_workbookName = workbookName;
		}

		public override string Designation
		{
			get
			{
				return "Excel";
			}
		}

		public override string Description
		{
			get
			{
				if (_workbookName == null)
				{
					return "Microsoft Excel";
				}
				else
				{
					return _workbookName;
				}
			}
		}

		public override int Priority
		{
			get
			{
				return 5;
			}
		}

		public override bool IsDynamic
		{
			get
			{
				return true;
			}
		}

		public override bool IsActive
		{
			get
			{
				return base.IsActive && exePath != null;
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				if (!string.IsNullOrEmpty(_workbookName))
				{
					return PluginUtils.GetCachedExeIcon(exePath, ICON_WORKBOOK);
				}
				return PluginUtils.GetCachedExeIcon(exePath, ICON_APPLICATION);
			}
		}

		public override IEnumerable<ILegacyDestination> DynamicDestinations()
		{
			return from workbookname in ExcelExporter.GetWorkbooks()
				orderby workbookname
				select new ExcelLegacyDestination(workbookname);
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			bool createdFile = false;
			string imageFile = captureDetails.Filename;
			try
			{
				await Task.Factory.StartNew(() =>
				{
					if (imageFile == null || surface.Modified || !Regex.IsMatch(imageFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
					{
						imageFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
						createdFile = true;
					}
					if (_workbookName != null)
					{
						ExcelExporter.InsertIntoExistingWorkbook(_workbookName, imageFile, surface.Image.Size);
					}
					else
					{
						ExcelExporter.InsertIntoNewWorkbook(imageFile, surface.Image.Size);
					}
				}, token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
				exportInformation.ExportMade = true;
			}
			catch (Exception ex)
			{
				exportInformation.ErrorMessage = ex.Message;
			}
			ProcessExport(exportInformation, surface);

			// Cleanup imageFile if we created it here, so less tmp-files are generated and left
			if (createdFile)
			{
				ImageOutput.DeleteNamedTmpFile(imageFile);
			}
			return exportInformation;
		}
	}
}