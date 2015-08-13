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
using log4net;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GreenshotBoxPlugin
{
	public class BoxDestination : AbstractDestination {
		private static ILog LOG = LogManager.GetLogger(typeof(BoxDestination));
		private static IBoxConfiguration _config = IniConfig.Get("Greenshot", "greenshot").Get<IBoxConfiguration>();

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
			var exportInformation = new ExportInformation {
				DestinationDesignation = Designation,
				DestinationDescription = Description
			};
			try {
				var url = await PleaseWaitWindow.CreateAndShowAsync(Designation, Language.GetString("box", LangKey.communication_wait), async (progress, pleaseWaitToken) => {
					return await BoxUtils.UploadToBoxAsync(surface, captureDetails, progress, token);
				});

				if (url != null && _config.AfterUploadLinkToClipBoard) {
					ClipboardHelper.SetClipboardData(url);
				}

				exportInformation.ExportMade = true;
				exportInformation.ExportedToUri = new Uri(url);
			} catch (TaskCanceledException tcEx) {
				exportInformation.ErrorMessage = tcEx.Message;
				LOG.Info(tcEx.Message);
			} catch (Exception e) {
				exportInformation.ErrorMessage = e.Message;
				LOG.Warn(e);
				MessageBox.Show(Designation, Language.GetString("box", LangKey.upload_failure) + " " + e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
