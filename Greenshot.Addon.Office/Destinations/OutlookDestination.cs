/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Office.OfficeExport;

namespace Greenshot.Addon.Office.Destinations
{
	/// <summary>
	/// Description of OutlookDestination.
	/// </summary>
	[Destination(OutlookDesignation), PartNotDiscoverable]
	public sealed class OutlookDestination : AbstractDestination
	{
		public const string OutlookDesignation = "Outlook";
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(OutlookDestination));
		private static readonly BitmapSource MeetingIcon;
		private static readonly BitmapSource MailIcon;
		private static readonly BitmapSource ApplicationIcon;
		
		static OutlookDestination()
		{
			var exePath = PluginUtils.GetExePath("Outlook.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("outlook");
				ApplicationIcon = PluginUtils.GetCachedExeIcon(exePath, 0).ToBitmapSource();
				MeetingIcon = PluginUtils.GetCachedExeIcon(exePath, 2).ToBitmapSource();
				using (var mailIcon = GreenshotResources.GetImage("Email.Image"))
				{
					MailIcon = mailIcon.ToBitmapSource();
				}
				IsActive = true;
			}
		}

		/// <summary>
		/// Tells if the destination can be used
		/// </summary>
		public static bool IsActive
		{
			get;
			private set;
		}

		[Import]
		private IOfficeConfiguration OfficeConfiguration
		{
			get;
			set;
		}

		[Import]
		private IGreenshotLanguage GreenshotLanguage
		{
			get;
			set;
		}

		protected override void Initialize()
		{
			base.Initialize();
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, null);
			Text = Text = $"Export to {OutlookDesignation}";
			Designation = OutlookDesignation;
			Icon = ApplicationIcon;
		}

		/// <summary>
		/// Load the current documents to export to
		/// </summary>
		/// <param name="caller1"></param>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public override async Task RefreshAsync(IExportContext caller1, CancellationToken token = default(CancellationToken))
		{
			await Task.Factory.StartNew(
				// this will use current synchronization context
				() =>
				{
					Children.Clear();
					var inspectorCaptions = OutlookExporter.RetrievePossibleTargets();
					if (inspectorCaptions == null)
					{
						return;
					}
					foreach (string inspectorCaption in inspectorCaptions.Keys)
					{
						var outlookDestination = new OutlookDestination
						{
							Icon = Microsoft.Office.Interop.Outlook.OlObjectClass.olAppointment.Equals(inspectorCaptions[inspectorCaption]) ? MeetingIcon : MailIcon,
							Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, inspectorCaption),
							Text = inspectorCaption,
							OfficeConfiguration = OfficeConfiguration,
							GreenshotLanguage = GreenshotLanguage
						};
						Children.Add(outlookDestination);
					}
				},
				token,
				TaskCreationOptions.None,
				TaskScheduler.FromCurrentSynchronizationContext()
			);
		}

		private Task<INotification> ExportCaptureAsync(ICapture capture, string inspectorCaption)
		{
			INotification returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = OutlookDesignation,
				SourceType = SourceTypes.Destination,
				Text = $"Exported to {OutlookDesignation}"
			};
			// Outlook logic
			string tmpFile = capture.CaptureDetails.Filename;
			if (tmpFile == null || capture.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(capture, capture.CaptureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
			}
			else
			{
				LOG.Information("Using already available file: {0}", tmpFile);
			}

			// Create a attachment name for the image
			string attachmentName = capture.CaptureDetails.Title;
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

			try
			{
				if (inspectorCaption != null)
				{
					OutlookExporter.ExportToInspector(inspectorCaption, tmpFile, attachmentName);
				}
				else
				{
					OutlookExporter.ExportToOutlook(OfficeConfiguration.OutlookEmailFormat, tmpFile, FilenameHelper.FillPattern(OfficeConfiguration.EmailSubjectPattern, capture.CaptureDetails, false), attachmentName, OfficeConfiguration.EmailTo, OfficeConfiguration.EmailCC, OfficeConfiguration.EmailBCC, null);
				}
			}
			catch (Exception ex)
			{
				LOG.Error(ex, "Outlook export failed");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = ex.Message;
				returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, OutlookDesignation);
				return Task.FromResult(returnValue);
			}
			return Task.FromResult(returnValue);
		}
	}
}