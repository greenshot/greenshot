#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GreenshotOfficePlugin.OfficeExport;
using GreenshotOfficePlugin.OfficeInterop;
using GreenshotPlugin.Core;
using GreenshotPlugin.Gfx;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;

#endregion

namespace GreenshotOfficePlugin.Destinations
{
	/// <summary>
	///     Description of OutlookDestination.
	/// </summary>
	public class OutlookDestination : AbstractDestination
	{
		private const int IconApplication = 0;
		private const int IconMeeting = 2;
		private const string MapiClient = "Microsoft Outlook";
		private static readonly LogSource Log = new LogSource();

		private static readonly Image MailIcon = GreenshotResources.GetImage("Email.Image");
		private static readonly OfficeConfiguration OfficeConfig = IniConfig.GetIniSection<OfficeConfiguration>();
		private static readonly string ExePath;
		private static readonly bool IsActiveFlag;
		private readonly string _outlookInspectorCaption;
		private readonly OlObjectClass _outlookInspectorType;

		static OutlookDestination()
		{
			if (EmailConfigHelper.HasOutlook())
			{
				IsActiveFlag = true;
			}
			ExePath = PluginUtils.GetExePath("OUTLOOK.EXE");
			if (ExePath != null && !File.Exists(ExePath))
			{
				ExePath = null;
			}
			if (ExePath == null)
			{
				IsActiveFlag = false;
			}
		}

		public OutlookDestination()
		{
		}

		public OutlookDestination(string outlookInspectorCaption, OlObjectClass outlookInspectorType)
		{
			_outlookInspectorCaption = outlookInspectorCaption;
			_outlookInspectorType = outlookInspectorType;
		}

		public override string Designation => "Outlook";

		public override string Description => _outlookInspectorCaption ?? MapiClient;

		public override int Priority => 3;

		public override bool IsActive => base.IsActive && IsActiveFlag;

		public override bool IsDynamic => true;

		public override Keys EditorShortcutKeys => Keys.Control | Keys.E;

		public override Image GetDisplayIcon(double dpi)
		{
			if (_outlookInspectorCaption == null)
			{
				return PluginUtils.GetCachedExeIcon(ExePath, IconApplication, dpi > 100);
			}
			if (OlObjectClass.olAppointment.Equals(_outlookInspectorType))
			{
				// Make sure we loaded the icon, maybe the configuration has been changed!
				return PluginUtils.GetCachedExeIcon(ExePath, IconMeeting, dpi > 100);
			}
			return MailIcon;
		}

		public override IEnumerable<IDestination> DynamicDestinations()
		{
			var inspectorCaptions = OutlookEmailExporter.RetrievePossibleTargets();
			if (inspectorCaptions == null)
			{
				yield break;
			}
			foreach (var inspectorCaption in inspectorCaptions.Keys)
			{
				yield return new OutlookDestination(inspectorCaption, inspectorCaptions[inspectorCaption]);
			}
		}

		/// <summary>
		///     Export the capture to outlook
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns></returns>
		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			// Outlook logic
			var tmpFile = captureDetails.Filename;
			if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
			}
			else
			{
				Log.Info().WriteLine("Using already available file: {0}", tmpFile);
			}

			// Create a attachment name for the image
			var attachmentName = captureDetails.Title;
			if (!string.IsNullOrEmpty(attachmentName))
			{
				attachmentName = attachmentName.Trim();
			}
			// Set default if non is set
			if (string.IsNullOrEmpty(attachmentName))
			{
				attachmentName = "Greenshot Capture";
			}
			// Make sure it's "clean" so it doesn't corrupt the header
			attachmentName = Regex.Replace(attachmentName, @"[^\x20\d\w]", "");

			if (_outlookInspectorCaption != null)
			{
				OutlookEmailExporter.ExportToInspector(_outlookInspectorCaption, tmpFile, attachmentName);
				exportInformation.ExportMade = true;
			}
			else
			{
				if (!manuallyInitiated)
				{
					var inspectorCaptions = OutlookEmailExporter.RetrievePossibleTargets();
					if (inspectorCaptions != null && inspectorCaptions.Count > 0)
					{
						var destinations = new List<IDestination>
						{
							new OutlookDestination()
						};
						foreach (var inspectorCaption in inspectorCaptions.Keys)
						{
							destinations.Add(new OutlookDestination(inspectorCaption, inspectorCaptions[inspectorCaption]));
						}
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return ShowPickerMenu(false, surface, captureDetails, destinations);
					}
				}
				else
				{
					exportInformation.ExportMade = OutlookEmailExporter.ExportToOutlook(OfficeConfig.OutlookEmailFormat, tmpFile,
						FilenameHelper.FillPattern(OfficeConfig.EmailSubjectPattern, captureDetails, false), attachmentName, OfficeConfig.EmailTo, OfficeConfig.EmailCC,
						OfficeConfig.EmailBCC, null);
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}