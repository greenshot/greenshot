/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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

namespace GreenshotPicasaPlugin {
	public class PicasaDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PicasaDestination));
		private static PicasaConfiguration config = IniConfig.GetIniSection<PicasaConfiguration>();

		private PicasaPlugin plugin = null;
		public PicasaDestination(PicasaPlugin plugin) {
			this.plugin = plugin;
		}
		
		public override string Designation {
			get {
				return "Picasa";
			}
		}

		public override string Description {
			get {
				return Language.GetString("picasa", LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(PicasaPlugin));
				return (Image)resources.GetObject("Picasa");
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			string uploadURL = null;
			bool uploaded = plugin.Upload(captureDetails, surface, out uploadURL);
			if (uploaded) {
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadURL;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
