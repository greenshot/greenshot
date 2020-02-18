/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.ComponentModel;
using System.Drawing;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;

namespace GreenshotDropboxPlugin {
	internal class DropboxDestination : AbstractDestination {
		private static readonly DropboxPluginConfiguration DropboxConfig = IniConfig.GetIniSection<DropboxPluginConfiguration>();

		private readonly DropboxPlugin _plugin;
		public DropboxDestination(DropboxPlugin plugin) {
			_plugin = plugin;
		}
		
		public override string Designation => "Dropbox";

		public override string Description => Language.GetString("dropbox", LangKey.upload_menu_item);

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(DropboxPlugin));
				return (Image)resources.GetObject("Dropbox");
			}
		}
		
		public override ExportInformation ExportCapture(bool manually, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);
            bool uploaded = _plugin.Upload(captureDetails, surface, out var uploadUrl);
			if (uploaded) {
				exportInformation.Uri = uploadUrl;
				exportInformation.ExportMade = true;
				if (DropboxConfig.AfterUploadLinkToClipBoard) {
					ClipboardHelper.SetClipboardData(uploadUrl);
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
