/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using log4net;

namespace GreenshotFlickrPlugin {
	public class FlickrDestination : AbstractDestination {
		private static ILog LOG = LogManager.GetLogger(typeof(FlickrDestination));
		private readonly FlickrPlugin plugin;
		public FlickrDestination(FlickrPlugin plugin) {
			this.plugin = plugin;
		}

		public override string Designation {
			get {
				return "Flickr";
			}
		}

		public override string Description {
			get {
				return Language.GetString("flickr", LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(FlickrPlugin));
				return (Image)resources.GetObject("flickr");
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);
			string uploadURL;
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
