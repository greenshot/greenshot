/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GreenshotPicasaPlugin {
	public class PicasaDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PicasaDestination));
		private static PicasaConfiguration _config = IniConfig.Current.Get<PicasaConfiguration>();

		public override string Designation {
			get {
				return "Picasa";
			}
		}

		public override string Description {
			get {
				return Language.GetString("picasa", LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				var resources = new ComponentResourceManager(typeof(PicasaPlugin));
				return (Image)resources.GetObject("Picasa");
			}
		}

		/// <summary>
		/// export the capture to Picasa
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			var exportInformation = new ExportInformation {
				DestinationDesignation = Designation,
				DestinationDescription = Description
			};

			try {
				var uploadURL = await PleaseWaitWindow.CreateAndShowAsync(Designation, Language.GetString("box", LangKey.communication_wait), async (progress, pleaseWaitToken) => {
					return await PicasaUtils.UploadToPicasa(surface, captureDetails, progress, token).ConfigureAwait(false);
				});

				if (!string.IsNullOrEmpty(uploadURL)) {
					exportInformation.ExportMade = true;
					exportInformation.ExportedToUri = new Uri(uploadURL);
				}
			} catch (TaskCanceledException tcEx) {
				exportInformation.ErrorMessage = tcEx.Message;
				LOG.Info(tcEx.Message);
			} catch (Exception e) {
				exportInformation.ErrorMessage = e.Message;
				LOG.Warn(e);
				MessageBox.Show(Designation, Language.GetString("picasa", LangKey.upload_failure) + " " + e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
