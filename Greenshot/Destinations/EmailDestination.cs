/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Interop.Office;
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
		private static string exePath = null;
		private static Image applicationIcon = null;
		private static Image mailIcon = null;
		private static Image meetingIcon = null;
		private static bool isActiveFlag = false;
		private static bool isOutlookUsed = false;
		private static string mapiClient = null;
		public const string DESIGNATION = "EMail";
		private string outlookInspectorCaption;
		private OlObjectClass outlookInspectorType;

		static EmailDestination() {
			// Logic to decide what email implementation we use
			if (EmailConfigHelper.HasMAPI()) {
				isActiveFlag = true;
				mapiClient = EmailConfigHelper.GetMapiClient();
				if (!string.IsNullOrEmpty(mapiClient)) {
					if (mapiClient.ToLower().Contains("microsoft outlook")) {
						isOutlookUsed = true;
					}
				}
			} else if (EmailConfigHelper.HasOutlook()) {
				mapiClient = "Microsoft Outlook";
				isActiveFlag = true;
				isOutlookUsed = true;
			}
			// Use default email icon
			mailIcon = GreenshotPlugin.Core.GreenshotResources.getImage("Email.Image");

			if (isOutlookUsed) {
				exePath = GetExePath("OUTLOOK.EXE");
				if (exePath != null && File.Exists(exePath)) {
					applicationIcon = GetExeIcon(exePath, 0);
					if (conf.OutlookAllowExportInMeetings) {
						meetingIcon = GetExeIcon(exePath, 2);
					}
				} else {
					exePath = null;
				}
				if (exePath == null) {
					isOutlookUsed = false;
					if (!EmailConfigHelper.HasMAPI()) {
						isActiveFlag = false;
					}
				}
			}
			if (isActiveFlag && !isOutlookUsed) {
				// Use default email icon
				applicationIcon = mailIcon;
			}
		}

		public EmailDestination() {
			
		}
		public EmailDestination(string outlookInspectorCaption, OlObjectClass outlookInspectorType) {
			this.outlookInspectorCaption = outlookInspectorCaption;
			this.outlookInspectorType = outlookInspectorType;
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

				if (outlookInspectorCaption == null) {
					return mapiClient;
				} else {
					return outlookInspectorCaption;
				}
			}
		}

		public override int Priority {
			get {
				return 3;
			}
		}

		public override bool isActive {
			get {
				return isActiveFlag;
			}
		}

		public override bool isDynamic {
			get {
				return isOutlookUsed;
			}
		}

		public override Keys EditorShortcutKeys {
			get {
				return Keys.Control | Keys.E;
			}
		}

		public override Image DisplayIcon {
			get {
				if (isOutlookUsed && outlookInspectorCaption != null) {
					if (OlObjectClass.olAppointment.Equals(outlookInspectorType)) {
						// Make sure we loaded the icon, maybe the configuration has been changed!
						if (meetingIcon == null) {
							meetingIcon = GetExeIcon(exePath, 2);
						}
						return meetingIcon;
					} else {
						return mailIcon;
					}
				} else {
					return applicationIcon;
				}
			}
		}
		
		public override IEnumerable<IDestination> DynamicDestinations() {
			if (!isOutlookUsed) {
				yield break;
			}
			Dictionary<string, OlObjectClass> inspectorCaptions = OutlookEmailExporter.RetrievePossibleTargets(conf.OutlookAllowExportInMeetings);
			if (inspectorCaptions != null) {
				foreach (string inspectorCaption in inspectorCaptions.Keys) {
					yield return new EmailDestination(inspectorCaption, inspectorCaptions[inspectorCaption]);
				}
			}
		}

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			if (!isOutlookUsed) {
				using (Image image = surface.GetImageForExport()) {
					MapiMailMessage.SendImage(image, captureDetails);
					surface.Modified = false;
					surface.SendMessageEvent(this, SurfaceMessageTyp.Info, "Exported to " + mapiClient);
				}
				return true;
			}

			// Outlook logic
			string tmpFile = captureDetails.Filename;
			if (tmpFile == null || surface.Modified) {
				using (Image image = surface.GetImageForExport()) {
					tmpFile = ImageOutput.SaveNamedTmpFile(image, captureDetails, conf.OutputFileFormat, conf.OutputFileJpegQuality, conf.OutputFileReduceColors);
				}
			} else {
				LOG.InfoFormat("Using already available file: {0}", tmpFile);
			}

			// Create a attachment name for the image
			string attachmentName = captureDetails.Title;
			if (!string.IsNullOrEmpty(attachmentName)) {
				attachmentName = attachmentName.Trim();
			}
			// Set default if non is set
			if (string.IsNullOrEmpty(attachmentName)) {
				attachmentName = "Greenshot Capture";
			}
			// Make sure it's "clean" so it doesn't corrupt the header
			attachmentName = Regex.Replace(attachmentName, @"[^\x20\d\w]", "");

			if (outlookInspectorCaption != null) {
				OutlookEmailExporter.ExportToInspector(outlookInspectorCaption, tmpFile, attachmentName);
			} else {
				if (!manuallyInitiated) {
					Dictionary<string, OlObjectClass> inspectorCaptions = OutlookEmailExporter.RetrievePossibleTargets(conf.OutlookAllowExportInMeetings);
					if (inspectorCaptions != null && inspectorCaptions.Count > 0) {
						List<IDestination> destinations = new List<IDestination>();
						destinations.Add(new EmailDestination());
						foreach (string inspectorCaption in inspectorCaptions.Keys) {
							destinations.Add(new EmailDestination(inspectorCaption, inspectorCaptions[inspectorCaption]));
						}
						ContextMenuStrip menu = PickerDestination.CreatePickerMenu(false, surface, captureDetails, destinations);
						PickerDestination.ShowMenuAtCursor(menu);
						return false;
					}
				}
				OutlookEmailExporter.ExportToOutlook(conf.OutlookEmailFormat, tmpFile, captureDetails.Title, attachmentName);
			}
			surface.SendMessageEvent(this, SurfaceMessageTyp.Info, Language.GetFormattedString(LangKey.exported_to, Description));
			surface.Modified = false;

			// Don't know how to handle a cancel in the email

			return true;
		}
	}
}