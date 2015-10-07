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

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using GreenshotOfficePlugin.OfficeExport;
using GreenshotPlugin.Core;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace GreenshotOfficePlugin
{
	/// <summary>
	/// Description of OutlookDestination.
	/// </summary>
	public class OutlookDestination : AbstractDestination
	{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (OutlookDestination));
		private const int ICON_APPLICATION = 0;
		private const int ICON_MEETING = 2;
		private const string OUTLOOK_PATH_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE";

		private static Image mailIcon = GreenshotResources.GetImage("Email.Image");
		private static IOfficeConfiguration conf = IniConfig.Current.Get<IOfficeConfiguration>();
		private static string exePath = null;
		private static bool isActiveFlag = false;
		private static string mapiClient = "Microsoft Outlook";
		public const string DESIGNATION = "Outlook";
		private string outlookInspectorCaption;
		private Outlook.OlObjectClass outlookInspectorType;

		static OutlookDestination()
		{
			if (HasOutlook())
			{
				isActiveFlag = true;
			}
			exePath = PluginUtils.GetExePath("OUTLOOK.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("outlook");
			}
			else
			{
				exePath = null;
			}
			if (exePath == null)
			{
				isActiveFlag = false;
			}
		}

		public OutlookDestination()
		{
			// Destination new message
		}

		public OutlookDestination(string outlookInspectorCaption, Outlook.OlObjectClass outlookInspectorType)
		{
			this.outlookInspectorCaption = outlookInspectorCaption;
			this.outlookInspectorType = outlookInspectorType;
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
				if (outlookInspectorCaption == null)
				{
					return mapiClient;
				}
				else
				{
					return outlookInspectorCaption;
				}
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
				return base.IsActive && isActiveFlag;
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
				if (outlookInspectorCaption != null)
				{
					if (Outlook.OlObjectClass.olAppointment.Equals(outlookInspectorType))
					{
						// Make sure we loaded the icon, maybe the configuration has been changed!
						return PluginUtils.GetCachedExeIcon(exePath, ICON_MEETING);
					}
					else
					{
						return mailIcon;
					}
				}
				else
				{
					return PluginUtils.GetCachedExeIcon(exePath, ICON_APPLICATION);
				}
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations()
		{
			IDictionary<string, Outlook.OlObjectClass> inspectorCaptions = OutlookExporter.RetrievePossibleTargets();
			if (inspectorCaptions != null)
			{
				foreach (string inspectorCaption in inspectorCaptions.Keys)
				{
					yield return new OutlookDestination(inspectorCaption, inspectorCaptions[inspectorCaption]);
				}
			}
		}

		/// <summary>
		/// Export the capture to outlook
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns></returns>
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

			if (outlookInspectorCaption != null)
			{
				await Task.Factory.StartNew(() =>
				{
					OutlookExporter.ExportToInspector(outlookInspectorCaption, tmpFile, attachmentName);
					exportInformation.ExportMade = true;
				}, token, TaskCreationOptions.None, scheduler);
			}
			else
			{
				IDictionary<string, Outlook.OlObjectClass> inspectorCaptions = OutlookExporter.RetrievePossibleTargets();
				if (!manuallyInitiated && inspectorCaptions != null && inspectorCaptions.Count > 0)
				{
					List<IDestination> destinations = new List<IDestination>();
					destinations.Add(new OutlookDestination());
					foreach (string inspectorCaption in inspectorCaptions.Keys)
					{
						destinations.Add(new OutlookDestination(inspectorCaption, inspectorCaptions[inspectorCaption]));
					}
					// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
					return await ShowPickerMenuAsync(false, surface, captureDetails, destinations, token).ConfigureAwait(false);
				}
				else
				{
					await Task.Factory.StartNew(() =>
					{
						exportInformation.ExportMade = OutlookExporter.ExportToOutlook(conf.OutlookEmailFormat, tmpFile, FilenameHelper.FillPattern(conf.EmailSubjectPattern, captureDetails, false), attachmentName, conf.EmailTo, conf.EmailCC, conf.EmailBCC, null);
					}, token, TaskCreationOptions.None, scheduler);
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

		private static string GetOutlookExePath()
		{
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(OUTLOOK_PATH_KEY, false))
			{
				if (key != null)
				{
					// "" is the default key, which should point to the outlook location
					return (string) key.GetValue("");
				}
			}
			return null;
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