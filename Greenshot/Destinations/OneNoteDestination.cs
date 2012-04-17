/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Helpers;
using Greenshot.Interop.Office;
using Greenshot.IniFile;

namespace Greenshot.Destinations {
	public class OneNoteDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordDestination));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static string exePath = null;
		private static Image applicationIcon = null;
		private static Image notebookIcon = null;
		private OneNotePage page = null;

		static OneNoteDestination() {
			exePath = GetExePath("ONENOTE.EXE");
			if (exePath != null && File.Exists(exePath)) {
				applicationIcon = GetExeIcon(exePath, 0);
				notebookIcon = GetExeIcon(exePath, 0);
			} else {
				exePath = null;
			}
		}
		
		public OneNoteDestination() {
			
		}

		public OneNoteDestination(OneNotePage page) {
			this.page = page;
		}

		public override string Designation {
			get {
				return "OneNote";
			}
		}

		public override string Description {
			get {
				if (page == null) {
					return "Microsoft OneNote";
				} else {
					return page.PageName;
				}
			}
		}

		public override int Priority {
			get {
				return 4;
			}
		}
		
		public override bool isDynamic {
			get {
				return true;
			}
		}

		public override bool isActive {
			get {
				return exePath != null;
			}
		}

		public override Image DisplayIcon {
			get {
				if (page != null) {
					return notebookIcon;
				}
				return applicationIcon;
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			foreach (OneNotePage page in OneNoteExporter.GetPages()) {
				yield return new OneNoteDestination(page);
			}
		}

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			using (Image image = surface.GetImageForExport()) {
				if (page != null) {
					try {
						OneNoteExporter.ExportToPage((Bitmap)image, page);
					} catch (Exception ex) {
						LOG.Error(ex);
					}
				}
			}
			return true;
		}
	}
}
