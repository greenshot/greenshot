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
namespace GreenshotDropboxPlugin {
	class DropboxDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(DropboxDestination));
		private static DropboxPluginConfiguration config = IniConfig.GetIniSection<DropboxPluginConfiguration>();

		private DropboxPlugin plugin = null;
		public DropboxDestination(DropboxPlugin plugin) {
			this.plugin = plugin;
		}
		
		public override string Designation {
			get {
				return "Dropbox";
			}
		}

		public override string Description {
			get {
				return Language.GetString("dropbox", LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(DropboxPlugin));
				return (Image)resources.GetObject("Dropbox");
			}
		}
		
		public override ExportInformation ExportCapture(bool manually, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			string uploadURL = null;
			bool uploaded = plugin.Upload(captureDetails, surface, out uploadURL);
			if (uploaded) {
				exportInformation.Uri = uploadURL;
				exportInformation.ExportMade = true;
				if (config.AfterUploadLinkToClipBoard) {
					ClipboardHelper.SetClipboardData(uploadURL);
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
