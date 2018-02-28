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

using System.ComponentModel;
using System.Drawing;
using GreenshotPlugin.Core;
using Dapplo.Ini;
using GreenshotPlugin.Interfaces;

#endregion

namespace GreenshotDropboxPlugin
{
	internal class DropboxDestination : AbstractDestination
	{
		private static readonly IDropboxPluginConfiguration DropboxConfig = IniConfig.Current.Get<IDropboxPluginConfiguration>();

		private readonly DropboxPlugin _plugin;

		public DropboxDestination(DropboxPlugin plugin)
		{
			_plugin = plugin;
		}
		public override Bitmap DisplayIcon
		{
			get
			{
				var resources = new ComponentResourceManager(typeof(DropboxPlugin));
				return (Bitmap)resources.GetObject("Dropbox");
			}
		}

		public override string Designation => "Dropbox";

		public override string Description => Language.GetString("dropbox", LangKey.upload_menu_item);

		public override ExportInformation ExportCapture(bool manually, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
		    var uploaded = _plugin.Upload(captureDetails, surface, out var uploadUrl);
			if (uploaded)
			{
				exportInformation.Uri = uploadUrl;
				exportInformation.ExportMade = true;
				if (DropboxConfig.AfterUploadLinkToClipBoard)
				{
					ClipboardHelper.SetClipboardData(uploadUrl);
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}