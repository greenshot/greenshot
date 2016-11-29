//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.Core;
using MahApps.Metro.IconPacks;
using Microsoft.Office.Interop.Outlook;
using Exception = System.Exception;
using Greenshot.Addon.Extensions;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core.Implementations;
using Greenshot.Core.Interfaces;
using Greenshot.Legacy.Extensions;

#endregion

namespace Greenshot.Addon.Office.Destinations
{
	/// <summary>
	///     Description of OutlookDestination.
	/// </summary>
	[Destination(OutlookDesignation)]
	[PartNotDiscoverable]
	public sealed class OutlookDestination : AbstractDestination
	{
		public const string OutlookDesignation = "Outlook";
		private static readonly LogSource Log = new LogSource();

		static OutlookDestination()
		{
			var exePath = PathHelper.GetExePath("Outlook.EXE");
			if ((exePath != null) && File.Exists(exePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("outlook");
				IsActive = true;
			}
		}

		[Import]
		private IGreenshotLanguage GreenshotLanguage { get; set; }

		/// <summary>
		///     Tells if the destination can be used
		/// </summary>
		public static bool IsActive { get; private set; }

		[Import]
		private IOfficeConfiguration OfficeConfiguration { get; set; }

		private Task<INotification> ExportCaptureAsync(ICapture capture, string inspectorCaption)
		{
			INotification returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = OutlookDesignation,
				NotificationSourceType = NotificationSourceTypes.Destination,
				Text = $"Exported to {OutlookDesignation}"
			};
			// Outlook logic
			string tmpFile = capture.CaptureDetails.Filename;
			if ((tmpFile == null) || capture.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = capture.SaveNamedTmpFile(capture.CaptureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
			}
			else
			{
				Log.Info().WriteLine("Using already available file: {0}", tmpFile);
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
				Log.Error().WriteLine(ex, "Outlook export failed");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = ex.Message;
				returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, OutlookDesignation);
				return Task.FromResult(returnValue);
			}
			return Task.FromResult(returnValue);
		}

		protected override void Initialize()
		{
			base.Initialize();
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, null);
			Text = Text = $"Export to {OutlookDesignation}";
			Designation = OutlookDesignation;
			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.OfficeOutlook
			};
		}

		/// <summary>
		///     Load the current documents to export to
		/// </summary>
		/// <param name="caller1"></param>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public override async Task RefreshAsync(IExportContext caller1, CancellationToken token = default(CancellationToken))
		{
			Children.Clear();
			await Task.Run(() =>
			{
				return OutlookExporter.RetrievePossibleTargets().OrderBy(x => x.Key).Select(inspectorCaption => new OutlookDestination
				{
					Icon = OlObjectClass.olAppointment.Equals(inspectorCaption.Value) ?
						new PackIconModern
						{
							Kind = PackIconModernKind.OfficeOutlook
						} : new PackIconModern
						{
							Kind = PackIconModernKind.EmailOutlook
						},
					Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, inspectorCaption.Key),
					Text = inspectorCaption.Key,
					OfficeConfiguration = OfficeConfiguration,
					GreenshotLanguage = GreenshotLanguage
				}).ToList();
			}, token).ContinueWith(async destinations =>
			{
				foreach (var oneNoteDestination in await destinations)
				{
					Children.Add(oneNoteDestination);
				}
			}, token, TaskContinuationOptions.None, UiContext.UiTaskScheduler).ConfigureAwait(false);
		}
	}
}