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
	/// Description of WordDestination.
	/// </summary>
	[Destination(WordDesignation), PartNotDiscoverable]
	public sealed class WordDestination : AbstractDestination
	{
		public const string WordDesignation = "Word";
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(WordDestination));
		private static readonly BitmapSource DocumentIcon;
		private static readonly BitmapSource ApplicationIcon;

		static WordDestination()
		{
			var exePath = PluginUtils.GetExePath("WINWORD.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("WINWORD");
				DocumentIcon = PluginUtils.GetCachedExeIcon(exePath, 1).ToBitmapSource();
				ApplicationIcon = PluginUtils.GetCachedExeIcon(exePath, 0).ToBitmapSource();
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
			Text = Text = $"Export to {WordDesignation}";
			Designation = WordDesignation;
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
				() => {
					Children.Clear();
					foreach (var caption in WordExporter.GetWordDocuments().OrderBy(x => x))
					{
						var wordDestination = new WordDestination
						{
							Icon = DocumentIcon,
							Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, caption),
							Text = caption,
							OfficeConfiguration = OfficeConfiguration,
							GreenshotLanguage = GreenshotLanguage
						};
						Children.Add(wordDestination);
					}
				},
				token,
				TaskCreationOptions.None,
				TaskScheduler.FromCurrentSynchronizationContext()
			);
		}

		private Task<INotification> ExportCaptureAsync(ICapture capture, string documentCaption)
		{
			INotification returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = WordDesignation,
				SourceType = SourceTypes.Destination,
				Text = $"Exported to {WordDesignation}"
			};
			string tmpFile = capture.CaptureDetails.Filename;
			if (tmpFile == null || capture.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(capture, capture.CaptureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
			}
			try
			{
				if (documentCaption != null)
				{
					try
					{
						WordExporter.InsertIntoExistingDocument(documentCaption, tmpFile);
					}
					catch (Exception)
					{
						// Retry once, just in case
						WordExporter.InsertIntoExistingDocument(documentCaption, tmpFile);
					}
				}
				else
				{
					try
					{
						WordExporter.InsertIntoNewDocument(tmpFile, null, null);
					}
					catch (Exception)
					{
						// Retry once, just in case
						WordExporter.InsertIntoNewDocument(tmpFile, null, null);
					}
				}

			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error exporting image to Word");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = ex.Message;
				returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, WordDesignation);
				return Task.FromResult(returnValue);
			}
			return Task.FromResult(returnValue);
		}
	}
}