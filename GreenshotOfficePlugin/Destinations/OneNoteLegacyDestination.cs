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
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotOfficePlugin
{
	public class OneNoteLegacyDestination : AbstractLegacyDestination
	{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (WordLegacyDestination));
		private const int ICON_APPLICATION = 0;
		public const string DESIGNATION = "OneNote";
		private static string exePath = null;
		private OneNotePage page = null;

		static OneNoteLegacyDestination()
		{
			exePath = PluginUtils.GetExePath("ONENOTE.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("onenote");
			}
			else
			{
				exePath = null;
			}
		}

		public OneNoteLegacyDestination()
		{
		}

		public OneNoteLegacyDestination(OneNotePage page)
		{
			this.page = page;
		}

		public override string Designation
		{
			get
			{
				return DESIGNATION;
			}
		}

		public override string Description
		{
			get
			{
				if (page == null)
				{
					return "Microsoft OneNote";
				}
				else
				{
					return page.DisplayName;
				}
			}
		}

		public override int Priority
		{
			get
			{
				return 4;
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
				return PluginUtils.GetCachedExeIcon(exePath, ICON_APPLICATION);
			}
		}

		public override IEnumerable<ILegacyDestination> DynamicDestinations()
		{
			foreach (var page in OneNoteExporter.GetPages())
			{
				yield return new OneNoteLegacyDestination(page);
			}
		}

		public override Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};

			if (page == null)
			{
				try
				{
					exportInformation.ExportMade = OneNoteExporter.ExportToNewPage(surface);
				}
				catch (Exception ex)
				{
					exportInformation.ErrorMessage = ex.Message;
					LOG.Error(ex);
				}
			}
			else
			{
				try
				{
					exportInformation.ExportMade = OneNoteExporter.ExportToPage(surface, page);
				}
				catch (Exception ex)
				{
					exportInformation.ErrorMessage = ex.Message;
					LOG.Error(ex);
				}
			}
			return Task.FromResult(exportInformation);
		}
	}
}