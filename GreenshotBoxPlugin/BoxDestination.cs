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
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using log4net;
using System.IO;
using GreenshotPlugin.Controls;
using Greenshot.IniFile;
using System;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using GreenshotPlugin.Windows;

namespace GreenshotBoxPlugin {
	public class BoxDestination : AbstractDestination {
		private static ILog LOG = LogManager.GetLogger(typeof(BoxDestination));
		private static BoxConfiguration _config = IniConfig.GetIniSection<BoxConfiguration>();

		private readonly BoxPlugin _plugin;
		public BoxDestination(BoxPlugin plugin) {
			_plugin = plugin;
		}
		
		public override string Designation {
			get {
				return "Box";
			}
		}

		public override string Description {
			get {
				return Language.GetString("box", LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(BoxPlugin));
				return (Image)resources.GetObject("Box");
			}
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);
			string uploadUrl = await UploadAsync(captureDetails, surface, token).ConfigureAwait(false);
			if (uploadUrl != null) {
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadUrl;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// </summary>
		private async Task<string> UploadAsync(ICaptureDetails captureDetails, ISurface surfaceToUpload, CancellationToken token = default(CancellationToken))
		{
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
			try
			{
				string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
				SurfaceContainer imageToUpload = new SurfaceContainer(surfaceToUpload, outputSettings, filename);

				var url = await PleaseWaitWindow.CreateAndShowAsync(Designation, Language.GetString("box", LangKey.communication_wait), (progress, pleaseWaitToken) => {
					// TODO: Change to async
					return Task.Run(() => BoxUtils.UploadToBox(imageToUpload, captureDetails.Title, filename));
                }).ConfigureAwait(false);

				if (url != null && _config.AfterUploadLinkToClipBoard)
				{
					ClipboardHelper.SetClipboardData(url);
				}

				return url;
			}
			catch (Exception ex)
			{
				LOG.Error("Error uploading.", ex);
				MessageBox.Show(Language.GetString("box", LangKey.upload_failure) + " " + ex.Message);
				return null;
			}
		}

	}
}
