/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Interop.Office;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using GreenshotOfficePlugin.OfficeExport;
using GreenshotPlugin.Interfaces;

namespace GreenshotOfficePlugin {
	public class OneNoteDestination : AbstractDestination {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordDestination));
		private const int IconApplication = 0;
		public const string DESIGNATION = "OneNote";
		private static readonly string ExePath;
		private readonly OneNotePage _page;

		static OneNoteDestination() {
			ExePath = PluginUtils.GetExePath("ONENOTE.EXE");
			if (ExePath != null && !File.Exists(ExePath)) {
				ExePath = null;
			}
		}
		
		public OneNoteDestination() {
			
		}

		public OneNoteDestination(OneNotePage page) {
			_page = page;
		}

		public override string Designation {
			get {
				return DESIGNATION;
			}
		}

		public override string Description {
			get
			{
				if (_page == null) {
					return "Microsoft OneNote";
				}
				return _page.DisplayName;
			}
		}

		public override int Priority => 4;

		public override bool IsDynamic => true;

		public override bool IsActive => base.IsActive && ExePath != null;

		public override Image DisplayIcon => PluginUtils.GetCachedExeIcon(ExePath, IconApplication);

		public override IEnumerable<IDestination> DynamicDestinations() {
			try
			{
				return OneNoteExporter.GetPages().Where(currentPage => currentPage.IsCurrentlyViewed).Select(currentPage => new OneNoteDestination(currentPage)).Cast<IDestination>();
			}
			catch (COMException cEx)
			{
				if (cEx.ErrorCode == unchecked((int)0x8002801D))
				{
					LOG.Warn("Wrong registry keys, to solve this remove the OneNote key as described here: http://microsoftmercenary.com/wp/outlook-excel-interop-calls-breaking-solved/");
				}
				LOG.Warn("Problem retrieving onenote destinations, ignoring: ", cEx);
			}
			catch (Exception ex)
			{
				LOG.Warn("Problem retrieving onenote destinations, ignoring: ", ex);
			}
			return Enumerable.Empty<IDestination>();
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);

			if (_page == null) {
				try {
					exportInformation.ExportMade = OneNoteExporter.ExportToNewPage(surface);
				} catch(Exception ex) {
					exportInformation.ErrorMessage = ex.Message;
					LOG.Error(ex);
				}
			} else {
				try {
					exportInformation.ExportMade = OneNoteExporter.ExportToPage(surface, _page);
				} catch(Exception ex) {
					exportInformation.ErrorMessage = ex.Message;
					LOG.Error(ex);
				}
			}
			return exportInformation;
		}
	}
}
