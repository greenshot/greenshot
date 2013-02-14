/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Helpers;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of EmailDestination.
	/// </summary>
	public class EmailDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(EmailDestination));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static Image mailIcon = GreenshotPlugin.Core.GreenshotResources.getImage("Email.Image");
		private static bool isActiveFlag = false;
		private static string mapiClient = null;
		public const string DESIGNATION = "EMail";

		static EmailDestination() {
			// Logic to decide what email implementation we use
			if (EmailConfigHelper.HasMAPI()) {
				isActiveFlag = true;
				mapiClient = EmailConfigHelper.GetMapiClient();
				if (!string.IsNullOrEmpty(mapiClient)) {
					// Active as we have a mapi client, can be disabled later
					isActiveFlag = true;
				}
			}
		}

		public EmailDestination() {
			
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
					mapiClient = Language.GetString(LangKey.editor_email);
				}
				return mapiClient;
			}
		}

		public override int Priority {
			get {
				return 3;
			}
		}

		public override bool isActive {
			get {
				if (isActiveFlag) {
					// Disable if the office plugin is installed and the client is outlook
					Type outlookdestination = Type.GetType("GreenshotOfficePlugin.OutlookDestination,GreenshotOfficePlugin");
					if (outlookdestination != null) {
						if (mapiClient.ToLower().Contains("microsoft outlook")) {
							isActiveFlag = false;
						}
					}
				}
				return base.isActive && isActiveFlag;
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
		
		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			MapiMailMessage.SendImage(surface, captureDetails);
			exportInformation.ExportMade = true;
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}