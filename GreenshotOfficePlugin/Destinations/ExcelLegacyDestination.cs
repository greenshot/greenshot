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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GreenshotOfficePlugin.OfficeExport;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotOfficePlugin.Destinations
{
	/// <summary>
	/// ExcelDestination
	/// </summary>
	public class ExcelLegacyDestination : AbstractLegacyDestination
	{
		private const int IconApplication = 0;
		private const int IconWorkbook = 1;
		private static readonly string ExePath;
		private readonly string _workbookName;

		static ExcelLegacyDestination()
		{
			ExePath = PluginUtils.GetExePath("EXCEL.EXE");
			if (ExePath != null && File.Exists(ExePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("excel");
			}
			else
			{
				ExePath = null;
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
				return _workbookName;
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
				return base.IsActive && ExePath != null;
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				if (!string.IsNullOrEmpty(_workbookName))
				{
					return PluginUtils.GetCachedExeIcon(ExePath, IconWorkbook);
				}
				return PluginUtils.GetCachedExeIcon(ExePath, IconApplication);
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