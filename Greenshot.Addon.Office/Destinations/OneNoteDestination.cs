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

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.Core;
using Greenshot.Core.Interfaces;
using MahApps.Metro.IconPacks;

#endregion

namespace Greenshot.Addon.Office.Destinations
{
	/// <summary>
	///     Description of OneNoteDestination.
	/// </summary>
	[Destination(OneNoteDesignation)]
	[PartNotDiscoverable]
	public sealed class OneNoteDestination : AbstractDestination
	{
		public const string OneNoteDesignation = "OneNote";
		private static readonly LogSource Log = new LogSource();

		static OneNoteDestination()
		{
			var exePath = PathHelper.GetExePath("ONENOTE.EXE");
			if ((exePath != null) && File.Exists(exePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("onenote");
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

		private Task<INotification> ExportCaptureAsync(ICapture capture, OneNotePage page)
		{
			INotification returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = OneNoteDesignation,
				SourceType = SourceTypes.Destination,
				Text = $"Exported to {OneNoteDesignation}"
			};
			try
			{
				if (page == null)
				{
					OneNoteExporter.ExportToNewPage(capture);
				}
				else
				{
					OneNoteExporter.ExportToPage(capture, page);
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "OneNote export failed");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = ex.Message;
				returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, OneNoteDesignation);
				return Task.FromResult(returnValue);
			}
			return Task.FromResult(returnValue);
		}

		protected override void Initialize()
		{
			base.Initialize();
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, null);
			Text = Text = $"Export to {OneNoteDesignation}";
			Designation = OneNoteDesignation;
			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.OfficeOnenote
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
				return OneNoteExporter.GetPages().OrderBy(x => x.DisplayName).Select(page => new OneNoteDestination
				{
					Icon = new PackIconModern
					{
						Kind = PackIconModernKind.PageOnenote
					},
					Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, page),
					Text = page.DisplayName,
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