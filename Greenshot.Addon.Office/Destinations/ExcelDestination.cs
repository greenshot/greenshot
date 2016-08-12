/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Utils;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Office.OfficeExport;
using Dapplo.Log.Facade;
using MahApps.Metro.IconPacks;

namespace Greenshot.Addon.Office.Destinations
{
	/// <summary>
	/// Description of ExcelDestination.
	/// </summary>
	[Destination(ExcelDesignation), PartNotDiscoverable]
	public sealed class ExcelDestination : AbstractDestination
	{
		public const string ExcelDesignation = "Excel";
		private static readonly LogSource Log = new LogSource();

		static ExcelDestination()
		{
			var exePath = PluginUtils.GetExePath("EXCEL.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("excel");
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
			Text = Text = $"Export to {ExcelDesignation}";
			Designation = ExcelDesignation;
			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.OfficeExcel
			};
		}

		/// <summary>
		/// Load the current documents to export to
		/// </summary>
		/// <param name="caller1"></param>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public override async Task RefreshAsync(IExportContext caller1, CancellationToken token = default(CancellationToken))
		{
			Children.Clear();
			await Task.Run(() =>
			{
				return ExcelExporter.GetWorkbooks().OrderBy(x => x).Select(workbook => new ExcelDestination
				{
					Icon = new PackIconModern
					{
						Kind = PackIconModernKind.PageExcel
					},

					Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, workbook),
					Text = workbook,
					OfficeConfiguration = OfficeConfiguration,
					GreenshotLanguage = GreenshotLanguage
				}).ToList();
			}, token).ContinueWith(async destinations =>
			{
				foreach (var excelDestination in await destinations)
				{
					Children.Add(excelDestination);
				}
			}, token, TaskContinuationOptions.None, UiContext.UiTaskScheduler).ConfigureAwait(false);
		}

		private Task<INotification> ExportCaptureAsync(ICapture capture, string workbook)
		{
			INotification returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = ExcelDesignation,
				SourceType = SourceTypes.Destination,
				Text = $"Exported to {ExcelDesignation}"
			};
			bool createdFile = false;
			string imageFile = capture.CaptureDetails.Filename;
			try
			{
				if (imageFile == null || capture.Modified || !Regex.IsMatch(imageFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
				{
					imageFile = ImageOutput.SaveNamedTmpFile(capture, capture.CaptureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
					createdFile = true;
				}
				if (workbook != null)
				{
					ExcelExporter.InsertIntoExistingWorkbook(workbook, imageFile, capture.Image.Size);
				}
				else
				{
					ExcelExporter.InsertIntoNewWorkbook(imageFile, capture.Image.Size);
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "Excel export failed");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = ex.Message;
				returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, ExcelDesignation);
				return Task.FromResult(returnValue);
			}
			finally
			{
				if (createdFile)
				{
					ImageOutput.DeleteNamedTmpFile(imageFile);
				}
			}
			return Task.FromResult(returnValue);
		}
	}
}