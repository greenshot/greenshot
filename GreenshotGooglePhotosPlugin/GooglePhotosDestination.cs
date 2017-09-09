#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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
using GreenshotPlugin.Interfaces;

#endregion

namespace GreenshotGooglePhotosPlugin
{
	public class GooglePhotosDestination : AbstractDestination
	{
		private readonly GooglePhotosPlugin _plugin;

		public GooglePhotosDestination(GooglePhotosPlugin plugin)
		{
			_plugin = plugin;
		}

		public override string Designation => "GooglePhotos";

		public override string Description => Language.GetString("googlephotos", LangKey.upload_menu_item);

		public override Bitmap DisplayIcon
		{
			get
			{
				var resources = new ComponentResourceManager(typeof(GooglePhotosPlugin));
				return (Bitmap) resources.GetObject("GooglePhotos");
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			string uploadUrl;
			var uploaded = _plugin.Upload(captureDetails, surface, out uploadUrl);
			if (uploaded)
			{
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadUrl;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}