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
using Dapplo.Config.Language;

namespace GreenshotPhotobucketPlugin
{
	/// <summary>
	/// Description of PhotobucketDestination.
	/// </summary>
	public class PhotobucketDestination : AbstractDestination
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (PhotobucketDestination));
		private static readonly IPhotobucketConfiguration Config = IniConfig.Current.Get<IPhotobucketConfiguration>();
		private static readonly IPhotobucketLanguage Language = LanguageLoader.Current.Get<IPhotobucketLanguage>();
		private readonly string _albumPath;

		public PhotobucketDestination()
		{
		}

		/// <summary>
		/// Create a Photobucket destination, which also has the path to the album in it
		/// </summary>
		/// <param name="albumPath">path to the album, null for default</param>
		public PhotobucketDestination(string albumPath)
		{
			_albumPath = albumPath;
		}

		public override string Designation
		{
			get
			{
				return "Photobucket";
			}
		}

		public override string Description
		{
			get
			{
				if (_albumPath != null)
				{
					return _albumPath;
				}
				return Language.UploadMenuItem;
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				var resources = new ComponentResourceManager(typeof (PhotobucketPlugin));
				return (Image) resources.GetObject("Photobucket");
			}
		}

		public override bool IsDynamic
		{
			get
			{
				return true;
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations()
		{
			var albums = Task.Run(async () => await PhotobucketUtils.RetrievePhotobucketAlbums()).GetAwaiter().GetResult();

			if (albums == null || albums.Count == 0)
			{
				yield break;
			}
			foreach (string album in albums)
			{
				yield return new PhotobucketDestination(album);
			}
		}

		/// <summary>
		/// Export the capture to Photobucket
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(Config.UploadFormat, Config.UploadJpegQuality, false);
			try
			{
				var photobucketInfo = await PleaseWaitWindow.CreateAndShowAsync(Designation, Language.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					string filename = Path.GetFileName(FilenameHelper.GetFilename(Config.UploadFormat, captureDetails));
					return await PhotobucketUtils.UploadToPhotobucket(surface, outputSettings, _albumPath, captureDetails.Title, filename, progress);
				}, token);

				// This causes an exeption if the upload failed :)
				LOG.DebugFormat("Uploaded to Photobucket page: " + photobucketInfo.Page);
				string uploadUrl = null;
				try
				{
					uploadUrl = Config.UsePageLink ? photobucketInfo.Page : photobucketInfo.Original;
				}
				catch (Exception ex)
				{
					LOG.Error("Can't write to clipboard: ", ex);
				}

				if (uploadUrl != null)
				{
					exportInformation.ExportMade = true;
					exportInformation.ExportedToUri = new Uri(uploadUrl);
					if (Config.AfterUploadLinkToClipBoard)
					{
						ClipboardHelper.SetClipboardData(uploadUrl);
					}
				}
			}
			catch (Exception e)
			{
				LOG.Error("Error uploading.", e);
				MessageBox.Show(Language.UploadFailure + " " + e.Message);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}