/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;

using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotPhotobucketPlugin  {
	/// <summary>
	/// Description of PhotobucketDestination.
	/// </summary>
	public class PhotobucketDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PhotobucketDestination));
		private static PhotobucketConfiguration config = IniConfig.GetIniSection<PhotobucketConfiguration>();
		private PhotobucketPlugin plugin = null;
		private string albumPath = null;

		/// <summary>
		/// Create a Photobucket destination, which also has the path to the album in it
		/// </summary>
		/// <param name="plugin"></param>
		/// <param name="albumPath">path to the album, null for default</param>
		public PhotobucketDestination(PhotobucketPlugin plugin, string albumPath) {
			this.plugin = plugin;
			this.albumPath = albumPath;
		}
		
		public override string Designation {
			get {
				return "Photobucket";
			}
		}

		public override string Description {
			get {
				if (albumPath != null) {
					return albumPath;
				}
				return Language.GetString("photobucket", LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(PhotobucketPlugin));
				return (Image)resources.GetObject("Photobucket");
			}
		}
		
		public override bool isDynamic {
			get {
				return true;
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			List<string> albums = null;
			try {
				albums = PhotobucketUtils.RetrievePhotobucketAlbums();
			} catch {
			}

			if (albums == null || albums.Count == 0) {
				yield break;
			}
			foreach (string album in albums) {
				yield return new PhotobucketDestination(plugin, album);
			}
		}

		/// <summary>
		/// Export the capture to Photobucket
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns></returns>
		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			string uploadURL = null;
			bool uploaded = plugin.Upload(captureDetails, surface, albumPath, out uploadURL);
			if (uploaded) {
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadURL;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
