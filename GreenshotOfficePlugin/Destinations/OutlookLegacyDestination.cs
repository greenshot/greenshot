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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using GreenshotOfficePlugin.OfficeExport;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Microsoft.Win32;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace GreenshotOfficePlugin.Destinations
{
	/// <summary>
	/// Description of OutlookDestination.
	/// </summary>
	public class OutlookLegacyDestination : AbstractLegacyDestination
	{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (OutlookLegacyDestination));
		private const int IconApplication = 0;
		private const int IconMeeting = 2;
		private const string OutlookPathKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE";

		private static readonly Image MailIcon = GreenshotResources.GetImage("Email.Image");
		private static readonly IOfficeConfiguration OfficeConfiguration = IniConfig.Current.Get<IOfficeConfiguration>();
		private static readonly string ExePath;
		private static readonly bool IsActiveFlag;
		private const string MapiClient = "Microsoft Outlook";
		private const string DESIGNATION = "Outlook";
		private readonly string _outlookInspectorCaption;
		private readonly Outlook.OlObjectClass _outlookInspectorType;

		static OutlookLegacyDestination()
		{
			if (HasOutlook())
			{
				IsActiveFlag = true;
			}
			ExePath = PluginUtils.GetExePath("OUTLOOK.EXE");
			if (ExePath != null && File.Exists(ExePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("outlook");
			}
			else
			{
				ExePath = null;
			}
			if (ExePath == null)
			{
				IsActiveFlag = false;
			}
		}

		public OutlookLegacyDestination()
		{
			// Destination new message
		}

		public OutlookLegacyDestination(string outlookInspectorCaption, Outlook.OlObjectClass outlookInspectorType)
		{
			_outlookInspectorCaption = outlookInspectorCaption;
			_outlookInspectorType = outlookInspectorType;
		}

		public override string Designation
		{
			get
			{
				return DESIGNATION;
			}
		}

		public override string Description
		{
			get
			{
				if (_outlookInspectorCaption == null)
				{
					return MapiClient;
				}
				return _outlookInspectorCaption;
			}
		}

		public override int Priority
		{
			get
			{
				return 3;
			}
		}

		public override bool IsActive
		{
			get
			{
				return base.IsActive && IsActiveFlag;
			}
		}

		public override bool IsDynamic
		{
			get
			{
				return true;
			}
		}

		public override Keys EditorShortcutKeys
		{
			get
			{
				return Keys.Control | Keys.E;
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				if (_outlookInspectorCaption != null)
				{
					if (Outlook.OlObjectClass.olAppointment.Equals(_outlookInspectorType))
					{
						// Make sure we loaded the icon, maybe the configuration has been changed!
						return PluginUtils.GetCachedExeIcon(ExePath, IconMeeting);
					}
					return MailIcon;
				}
				return PluginUtils.GetCachedExeIcon(ExePath, IconApplication);
			}
		}

		public override IEnumerable<ILegacyDestination> DynamicDestinations()
		{
			var inspectorCaptions = OutlookExporter.RetrievePossibleTargets();
			if (inspectorCaptions == null)
			{
				yield break;
			}
			foreach (string inspectorCaption in inspectorCaptions.Keys)
			{
				yield return new OutlookLegacyDestination(inspectorCaption, inspectorCaptions[inspectorCaption]);
			}
		}

		/// <summary>
		/// Export the capture to outlook
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <param name="token"></param>
		/// <returns>ExportInformation</returns>
		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			// Outlook logic
			string tmpFile = captureDetails.Filename;
			if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
			}
			else
			{
				LOG.InfoFormat("Using already available file: {0}", tmpFile);
			}

			// Create a attachment name for the image
			string attachmentName = captureDetails.Title;
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
				await Task.Factory.StartNew(() =>
				{
					OutlookExporter.ExportToInspector(_outlookInspectorCaption, tmpFile, attachmentName);
					exportInformation.ExportMade = true;
				}, token, TaskCreationOptions.None, scheduler);
			}
			else
			{
				var inspectorCaptions = OutlookExporter.RetrievePossibleTargets();
				if (!manuallyInitiated && inspectorCaptions != null && inspectorCaptions.Count > 0)
				{
					var destinations = new List<ILegacyDestination>
					{
						new OutlookLegacyDestination()
					};
					foreach (string inspectorCaption in inspectorCaptions.Keys)
					{
						destinations.Add(new OutlookLegacyDestination(inspectorCaption, inspectorCaptions[inspectorCaption]));
					}
					// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
					return await ShowPickerMenuAsync(false, surface, captureDetails, destinations, token).ConfigureAwait(false);
				}
				await Task.Factory.StartNew(() =>
				{
					exportInformation.ExportMade = OutlookExporter.ExportToOutlook(OfficeConfiguration.OutlookEmailFormat, tmpFile, FilenameHelper.FillPattern(OfficeConfiguration.EmailSubjectPattern, captureDetails, false), attachmentName, OfficeConfiguration.EmailTo, OfficeConfiguration.EmailCC, OfficeConfiguration.EmailBCC, null);
				}, token, TaskCreationOptions.None, scheduler);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

		private static string GetOutlookExePath()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(OutlookPathKey, false))
			{
				// "" is the default key, which should point to the outlook location
				return (string) key?.GetValue("");
			}
		}

		/// <summary>
		/// Check if Outlook is installed
		/// </summary>
		/// <returns>Returns true if outlook is installed</returns>
		private static bool HasOutlook()
		{
			string outlookPath = GetOutlookExePath();
			if (outlookPath != null)
			{
				if (File.Exists(outlookPath))
				{
					return true;
				}
			}
			return false;
		}
	}
}