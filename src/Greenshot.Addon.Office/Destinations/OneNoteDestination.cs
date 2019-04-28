// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.Addon.Office.OfficeExport.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addon.Office.Destinations
{
    /// <summary>
    /// This is the OneNote destination, taking care of exporting
    /// </summary>
    [Destination("OneNote", DestinationOrder.OneNote)]
    public class OneNoteDestination : AbstractDestination
	{
        private readonly OneNoteExporter _oneNoteExporter;
        private readonly ExportNotification _exportNotification;
	    private const int IconApplication = 0;
		private static readonly LogSource Log = new LogSource();
		private readonly string _exePath;
		private readonly OneNotePage _page;

        /// <summary>
        /// Constructor used for dependency wiring
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
		public OneNoteDestination(
            OneNoteExporter oneNoteExporter,
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification
        ) : base(coreConfiguration, greenshotLanguage)
        {
            this._oneNoteExporter = oneNoteExporter;
            _exportNotification = exportNotification;
            _exePath = PluginUtils.GetExePath("ONENOTE.EXE");
		    if (_exePath != null && !File.Exists(_exePath))
		    {
		        _exePath = null;
		    }
        }

        /// <summary>
        /// Constructor used for dependency wiring, and being able to specify a page
        /// </summary>
        /// <param name="page">OneNotePage</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
        protected OneNoteDestination(OneNoteExporter oneNoteExporter,
            OneNotePage page,
		    ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
		    ExportNotification exportNotification
        ) : this(oneNoteExporter, coreConfiguration, greenshotLanguage, exportNotification)
        {
			_page = page;
		}

        /// <inherit />
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

        /// <inherit />
		public override bool IsDynamic => true;

        /// <inherit />
		public override bool IsActive => base.IsActive && _exePath != null;

        /// <inherit />
		public override IBitmapWithNativeSupport GetDisplayIcon(double dpi)
		{
			return PluginUtils.GetCachedExeIcon(_exePath, IconApplication, dpi > 100);
		}

        /// <inherit />
		public override IEnumerable<IDestination> DynamicDestinations()
		{
			try
			{
				return _oneNoteExporter.GetPages().Where(currentPage => currentPage.IsCurrentlyViewed).Select(currentPage => new OneNoteDestination(_oneNoteExporter, currentPage, CoreConfiguration, GreenshotLanguage, _exportNotification));
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

        /// <inherit />
	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);

			if (_page == null)
			{
				try
				{
					exportInformation.ExportMade = _oneNoteExporter.ExportToNewPage(surface);
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
					exportInformation.ExportMade = _oneNoteExporter.ExportToPage(surface, _page);
				}
				catch (Exception ex)
				{
					exportInformation.ErrorMessage = ex.Message;
					Log.Error().WriteLine(ex);
				}
			}
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}
	}
}