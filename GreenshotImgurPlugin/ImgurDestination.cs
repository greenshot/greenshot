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

using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.Windows;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GreenshotImgurPlugin
{
	/// <summary>
	/// Implementation of the Imgur destination.
	/// </summary>
	public class ImgurDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurDestination));
		private static readonly IImgurConfiguration config = IniConfig.Current.Get<IImgurConfiguration>();
		private static readonly IImgurLanguage imgurLanguage = LanguageLoader.Current.Get<IImgurLanguage>();
		private ImgurPlugin plugin = null;

		public ImgurDestination(ImgurPlugin plugin) {
			this.plugin = plugin;
		}
		
		public override string Designation {
			get {
				return "Imgur";
			}
		}

		public override string Description {
			get {
				return imgurLanguage.UploadMenuItem;
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(ImgurPlugin));
				return (Image)resources.GetObject("Imgur");
			}
		}

		/// <summary>
		/// Implementation of the export capture functionality
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <param name="token">CancellationToken</param>
		/// <returns>Task with ExportInformation</returns>
		public async override Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			var exportInformation = new ExportInformation {
				DestinationDesignation = Designation,
				DestinationDescription = Description
			};
			var outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			string uploadURL = null;
			try {
				string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(config.FilenamePattern, config.UploadFormat, captureDetails));
				var imgurInfo = await PleaseWaitWindow.CreateAndShowAsync(Designation, imgurLanguage.CommunicationWait, async (progress, pleaseWaitToken) => {
					return await ImgurUtils.UploadToImgurAsync(surface, outputSettings, captureDetails.Title, filename, progress, pleaseWaitToken);
				});

				if (imgurInfo != null) {
					exportInformation.ExportMade = true;

					if (config.UsePageLink) {
						if (imgurInfo.Page.AbsoluteUri != null) {
							exportInformation.ExportedToUri = new Uri(imgurInfo.Page.AbsoluteUri);
						}
					} else if (imgurInfo.Original.AbsoluteUri != null) {
						exportInformation.ExportedToUri = new Uri(imgurInfo.Original.AbsoluteUri);
					}
					try {
						if (config.CopyUrlToClipboard) {
							ClipboardHelper.SetClipboardData(uploadURL);
						}
					} catch (Exception ex) {
						LOG.Error("Can't write to clipboard: ", ex);
					}
				}
			} catch (TaskCanceledException tcEx) {
				exportInformation.ErrorMessage = tcEx.Message;
				LOG.Info(tcEx.Message);
			} catch (Exception e) {
				exportInformation.ErrorMessage = e.Message;
				LOG.Warn(e);
				MessageBox.Show(Designation, imgurLanguage.UploadFailure + " " + e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
