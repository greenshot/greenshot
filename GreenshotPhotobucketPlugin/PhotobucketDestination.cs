/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Dapplo.Config.Ini;
using System.Threading.Tasks;
using System.Threading;
using GreenshotPlugin.Windows;
using System.IO;
using System;
using System.Windows;

namespace GreenshotPhotobucketPlugin  {
	/// <summary>
	/// Description of PhotobucketDestination.
	/// </summary>
	public class PhotobucketDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PhotobucketDestination));
		private static PhotobucketConfiguration _config = IniConfig.Get("Greenshot","greenshot").Get<PhotobucketConfiguration>();
		private string albumPath = null;

		public PhotobucketDestination()
		{
		}

		/// <summary>
		/// Create a Photobucket destination, which also has the path to the album in it
		/// </summary>
		/// <param name="plugin"></param>
		/// <param name="albumPath">path to the album, null for default</param>
		public PhotobucketDestination(string albumPath) {
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
				var resources = new ComponentResourceManager(typeof(PhotobucketPlugin));
				return (Image)resources.GetObject("Photobucket");
			}
		}
		
		public override bool IsDynamic {
			get {
				return true;
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			List<string> albums = null;
			// TODO: Get list of albums, as this is async it won't work here without eventually causing deadlocks
			//try {
			//	albums = PhotobucketUtils.RetrievePhotobucketAlbums().Result;
			//} catch {
			//}

			if (albums == null || albums.Count == 0) {
				yield break;
			}
			foreach (string album in albums) {
				yield return new PhotobucketDestination(album);
			}
		}

		/// <summary>
		/// Export the capture to Photobucket
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns></returns>
		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			var exportInformation = new ExportInformation {
				DestinationDesignation = Designation,
				DestinationDescription = Description
			};
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
			try {
				var photobucketInfo = await PleaseWaitWindow.CreateAndShowAsync(Designation, Language.GetString("photobucket", LangKey.communication_wait), async (progress, pleaseWaitToken) => {
					string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
					return await PhotobucketUtils.UploadToPhotobucket(surface, outputSettings, albumPath, captureDetails.Title, filename, progress);
				});

				// This causes an exeption if the upload failed :)
				LOG.DebugFormat("Uploaded to Photobucket page: " + photobucketInfo.Page);
				string uploadURL = null;
				try {
					if (_config.UsePageLink) {
						uploadURL = photobucketInfo.Page;
					} else {
						uploadURL = photobucketInfo.Original;
					}
				} catch (Exception ex) {
					LOG.Error("Can't write to clipboard: ", ex);
				}

				if (uploadURL != null) {
					exportInformation.ExportMade = true;
					exportInformation.ExportedToUri = new Uri(uploadURL);
					if (_config.AfterUploadLinkToClipBoard) {
						ClipboardHelper.SetClipboardData(uploadURL);
					}
				}
			} catch (Exception e) {
				LOG.Error("Error uploading.", e);
				MessageBox.Show(Language.GetString("photobucket", LangKey.upload_failure) + " " + e.Message);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
