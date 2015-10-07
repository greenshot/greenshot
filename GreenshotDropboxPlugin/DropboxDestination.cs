/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.Windows;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dapplo.Config.Language;
using log4net;

namespace GreenshotDropboxPlugin
{
	internal class DropboxDestination : AbstractDestination
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (DropboxDestination));
		private static readonly IDropboxConfiguration _config = IniConfig.Current.Get<IDropboxConfiguration>();
		private static readonly IDropboxLanguage _language = LanguageLoader.Current.Get<IDropboxLanguage>();

		private DropboxPlugin plugin = null;

		public DropboxDestination(DropboxPlugin plugin)
		{
			this.plugin = plugin;
		}

		public override string Designation
		{
			get
			{
				return "Dropbox";
			}
		}

		public override string Description
		{
			get
			{
				return _language.UploadMenuItem;
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				ComponentResourceManager resources = new ComponentResourceManager(typeof (DropboxPlugin));
				return (Image) resources.GetObject("Dropbox");
			}
		}


		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
			try
			{
				var url = await PleaseWaitWindow.CreateAndShowAsync(Designation, _language.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
					using (var stream = new MemoryStream())
					{
						ImageOutput.SaveToStream(surface, stream, outputSettings);
						stream.Position = 0;
						using (var uploadStream = new ProgressStream(stream, progress))
						{
							using (var streamContent = new StreamContent(uploadStream))
							{
								streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
								return await DropboxUtils.UploadToDropbox(streamContent, filename);
							}
						}
					}
				}, token);

				if (url != null)
				{
					exportInformation.ExportMade = true;
					exportInformation.ExportedToUri = new Uri(url);
					if (_config.AfterUploadLinkToClipBoard)
					{
						ClipboardHelper.SetClipboardData(url);
					}
				}
			}
			catch (Exception e)
			{
				LOG.Error("Error uploading.", e);
				MessageBox.Show(_language.UploadFailure + " " + e.Message);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}