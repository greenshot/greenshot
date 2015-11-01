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
using GreenshotPlugin.Core;
using GreenshotPlugin.Windows;
using log4net;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dapplo.Config.Language;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Interfaces;

namespace GreenshotBoxPlugin
{
	public class BoxLegacyDestination : AbstractLegacyDestination
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (BoxLegacyDestination));
		private static readonly IBoxConfiguration _config = IniConfig.Current.Get<IBoxConfiguration>();
		private static readonly IBoxLanguage language = LanguageLoader.Current.Get<IBoxLanguage>();

		private readonly BoxPlugin _plugin;

		public BoxLegacyDestination(BoxPlugin plugin)
		{
			_plugin = plugin;
		}

		public override string Designation
		{
			get
			{
				return "Box";
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
				ComponentResourceManager resources = new ComponentResourceManager(typeof (BoxPlugin));
				return (Image) resources.GetObject("Box");
			}
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			try
			{
				var url = await PleaseWaitWindow.CreateAndShowAsync(Designation, language.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					return await BoxUtils.UploadToBoxAsync(surface, progress, token);
				}, token);

				if (url != null)
				{
					exportInformation.ExportedToUri = new Uri(url);
					if (_config.AfterUploadLinkToClipBoard)
					{
						ClipboardHelper.SetClipboardData(url);
					}
				}

				exportInformation.ExportMade = true;
			}
			catch (TaskCanceledException tcEx)
			{
				exportInformation.ErrorMessage = tcEx.Message;
				LOG.Info(tcEx.Message);
			}
			catch (Exception e)
			{
				exportInformation.ErrorMessage = e.Message;
				LOG.Warn(e);
				MessageBox.Show(language.UploadFailure + " " + e.Message, Designation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}