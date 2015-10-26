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

using System.ComponentModel;
using System.Drawing;
using GreenshotPlugin.Core;
using log4net;
using System.IO;
using System;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using GreenshotPlugin.Windows;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotFlickrPlugin
{
	public class FlickrLegacyDestination : AbstractLegacyDestination
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (FlickrLegacyDestination));
		private static IFlickrConfiguration _config = IniConfig.Current.Get<IFlickrConfiguration>();
		private static IFlickrLanguage language = LanguageLoader.Current.Get<IFlickrLanguage>();

		public override string Designation
		{
			get
			{
				return "Flickr";
			}
		}

		public override string Description
		{
			get
			{
				return language.UploadMenuItem;
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				ComponentResourceManager resources = new ComponentResourceManager(typeof (FlickrPlugin));
				return (Image) resources.GetObject("flickr");
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
				var url = await PleaseWaitWindow.CreateAndShowAsync(Designation, language.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
					return await FlickrUtils.UploadToFlickrAsync(surface, outputSettings, captureDetails.Title, filename, progress, token);
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
				MessageBox.Show(language.UploadFailure + " " + e.Message);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}