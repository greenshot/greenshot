#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.Addon.Office.OfficeInterop;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;

#endregion

namespace Greenshot.Addon.Office.Destinations
{
    [Destination("OneNote", DestinationOrder.OneNote)]
    public class OneNoteDestination : AbstractDestination
	{
	    private readonly ExportNotification _exportNotification;
	    private const int IconApplication = 0;
		private static readonly LogSource Log = new LogSource();
		private readonly string _exePath;
		private readonly OneNotePage _page;

		public OneNoteDestination(
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification
        ) : base(coreConfiguration, greenshotLanguage, exportNotification)
        {
            _exportNotification = exportNotification;
            _exePath = PluginUtils.GetExePath("ONENOTE.EXE");
		    if (_exePath != null && !File.Exists(_exePath))
		    {
		        _exePath = null;
		    }
        }

		protected OneNoteDestination(OneNotePage page,
		    ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
		    ExportNotification exportNotification
        ) : this(coreConfiguration, greenshotLanguage, exportNotification)
        {
			_page = page;
		}

	    public override string Description
		{
			get
			{
				if (_page == null)
				{
					return "Microsoft OneNote";
				}
				return _page.DisplayName;
			}
		}

		public override bool IsDynamic => true;

		public override bool IsActive => base.IsActive && _exePath != null;

		public override Bitmap GetDisplayIcon(double dpi)
		{
			return PluginUtils.GetCachedExeIcon(_exePath, IconApplication, dpi > 100);
		}

		public override IEnumerable<IDestination> DynamicDestinations()
		{
			try
			{
				return OneNoteExporter.GetPages().Where(currentPage => currentPage.IsCurrentlyViewed).Select(currentPage => new OneNoteDestination(currentPage, CoreConfiguration, GreenshotLanguage, _exportNotification));
			}
			catch (COMException cEx)
			{
				if (cEx.ErrorCode == unchecked((int) 0x8002801D))
				{
					Log.Warn().WriteLine("Wrong registry keys, to solve this remove the OneNote key as described here: http://microsoftmercenary.com/wp/outlook-excel-interop-calls-breaking-solved/");
				}
				Log.Warn().WriteLine(cEx, "Problem retrieving onenote destinations, ignoring: ");
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine(ex, "Problem retrieving onenote destinations, ignoring: ");
			}
			return Enumerable.Empty<IDestination>();
		}

	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);

			if (_page == null)
			{
				try
				{
					exportInformation.ExportMade = OneNoteExporter.ExportToNewPage(surface);
				}
				catch (Exception ex)
				{
					exportInformation.ErrorMessage = ex.Message;
					Log.Error().WriteLine(ex);
				}
			}
			else
			{
				try
				{
					exportInformation.ExportMade = OneNoteExporter.ExportToPage(surface, _page);
				}
				catch (Exception ex)
				{
					exportInformation.ErrorMessage = ex.Message;
					Log.Error().WriteLine(ex);
				}
			}
			return exportInformation;
		}
	}
}