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
using System.Drawing;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.IO;
using GreenshotPlugin.Windows;
using System.Windows;

namespace GreenshotImgurPlugin  {
	/// <summary>
	/// Description of ImgurDestination.
	/// </summary>
	public class ImgurDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurDestination));
		private static ImgurConfiguration config = IniConfig.GetIniSection<ImgurConfiguration>();
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
				return Language.GetString("imgur", LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(ImgurPlugin));
				return (Image)resources.GetObject("Imgur");
			}
		}

		public async override Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			string uploadURL = await UploadAsync(captureDetails, surface, token).ConfigureAwait(false);
			exportInformation.ExportMade = !string.IsNullOrEmpty(uploadURL);
			exportInformation.Uri = uploadURL;
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

		/// <summary>
		/// Upload the capture to imgur
		/// </summary>
		/// <param name="captureDetails"></param>
		/// <param name="image"></param>
		/// <param name="uploadURL">out string for the url</param>
		/// <returns>URL if the upload succeeded</returns>
		private async Task<string> UploadAsync(ICaptureDetails captureDetails, ISurface surfaceToUpload, CancellationToken token = default(CancellationToken))
		{
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			string uploadURL = null;
			try
			{
				string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(config.FilenamePattern, config.UploadFormat, captureDetails));
				var imgurInfo = await PleaseWaitWindow.CreateAndShowAsync(Designation, Language.GetString("imgur", LangKey.communication_wait), (progress, pleaseWaitToken) => {
					return ImgurUtils.UploadToImgurAsync(surfaceToUpload, outputSettings, captureDetails.Title, filename, pleaseWaitToken);
				}).ConfigureAwait(false);

				if (imgurInfo != null)
				{
					await plugin.CheckHistory().ConfigureAwait(false);
					IniConfig.Save();
					try
					{
						if (config.UsePageLink)
						{
							if (imgurInfo.Page.AbsoluteUri != null)
							{
								uploadURL = imgurInfo.Page.AbsoluteUri;
								if (config.CopyUrlToClipboard)
								{
									ClipboardHelper.SetClipboardData(imgurInfo.Page.AbsoluteUri);
								}
							}
						}
						else
						{
							if (imgurInfo.Original.AbsoluteUri != null)
							{

								uploadURL = imgurInfo.Original.AbsoluteUri;
								if (config.CopyUrlToClipboard)
								{
									ClipboardHelper.SetClipboardData(imgurInfo.Original.AbsoluteUri);
								}
							}
						}
					}
					catch (Exception ex)
					{
						LOG.Error("Can't write to clipboard: ", ex);
						uploadURL = null;
					}
				}
			}
			catch (Exception e)
			{
				LOG.Error("Error uploading.", e);
				MessageBox.Show(Language.GetString("imgur", LangKey.upload_failure) + " " + e.Message);
				uploadURL = null;
			}
			return uploadURL;
		}
	}
}
