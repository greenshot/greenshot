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

using System;
using System.Drawing;
using System.Windows.Forms;

using Greenshot.Helpers;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Dapplo.Config.Ini;
using log4net;
using System.Threading.Tasks;
using System.Threading;
using Dapplo.Config.Language;
using GreenshotPlugin.Configuration;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of EmailDestination.
	/// </summary>
	public class EmailDestination : AbstractDestination {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(EmailDestination));
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();
		private static readonly Image mailIcon = GreenshotResources.GetImage("Email.Image");
		private static bool _isActiveFlag = false;
		private static string mapiClient = null;
		public const string DESIGNATION = "EMail";

		static EmailDestination() {
			// Logic to decide what email implementation we use
			if (EmailConfigHelper.HasMAPI()) {
				_isActiveFlag = true;
				mapiClient = EmailConfigHelper.GetMapiClient();
				if (!string.IsNullOrEmpty(mapiClient)) {
					// Active as we have a mapi client, can be disabled later
					_isActiveFlag = true;
				}
			}
		}

		public override string Designation {
			get {
				return DESIGNATION;
			}
		}

		public override string Description {
			get {
				// Make sure there is some kind of "mail" name
				if (mapiClient == null) {
					mapiClient = language.EditorEmail;
				}
				return mapiClient;
			}
		}

		public override int Priority {
			get {
				return 3;
			}
		}

		public override bool IsActive {
			get {
				if (_isActiveFlag) {
					// Disable if the office plugin is installed and the client is outlook
					// TODO: Change this! It always creates an exception, as the plugin has not been loaded the type is not there :(
					Type outlookdestination = Type.GetType("GreenshotOfficePlugin.OutlookDestination,GreenshotOfficePlugin");
					if (outlookdestination != null) {
						if (mapiClient == null || mapiClient.ToLower().Contains("microsoft outlook")) {
							_isActiveFlag = false;
						}
					}
				}
				return base.IsActive && _isActiveFlag;
			}
		}

		public override Keys EditorShortcutKeys {
			get {
				return Keys.Control | Keys.E;
			}
		}

		public override Image DisplayIcon {
			get {
				return mailIcon;
			}
		}
		
		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			var exportInformation = new ExportInformation {
				DestinationDesignation = Designation,
				DestinationDescription = Description
			};
			// There is not much that can work async for the MapiMailMessage
			await Task.Factory.StartNew(() => {
				MapiMailMessage.SendImage(surface, captureDetails);
			}, token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

			exportInformation.ExportMade = true;
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}