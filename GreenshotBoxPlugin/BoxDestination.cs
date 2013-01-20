/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.ComponentModel;
using System.Drawing;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotBoxPlugin {
	public class BoxDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BoxDestination));
		private static BoxConfiguration config = IniConfig.GetIniSection<BoxConfiguration>();

		private BoxPlugin plugin = null;
		public BoxDestination(BoxPlugin plugin) {
			this.plugin = plugin;
		}
		
		public override string Designation {
			get {
				return "Box";
			}
		}

		public override string Description {
			get {
				return Language.GetString("box", LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(BoxPlugin));
				return (Image)resources.GetObject("Box");
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			string uploadUrl = plugin.Upload(captureDetails, surface);
			if (uploadUrl != null) {
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadUrl;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
