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

using GreenshotOfficePlugin.OfficeExport;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Interfaces.Plugin;
using log4net;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using GreenshotPlugin.Extensions;

namespace GreenshotOfficePlugin.Destinations
{
	/// <summary>
	/// Description of WordDestination.
	/// </summary>
	[Destination(WordDesignation)]
	public sealed class WordDestination : AbstractDestination
	{
		private const string WordDesignation = "Word";
		private static readonly ILog LOG = LogManager.GetLogger(typeof(WordDestination));
		private static readonly BitmapSource DocumentIcon;
		private static readonly BitmapSource ApplicationIcon;

		static WordDestination()
		{
			var exePath = PluginUtils.GetExePath("WINWORD.EXE");
			if (exePath != null && !File.Exists(exePath))
			{
				DocumentIcon = PluginUtils.GetCachedExeIcon(exePath, 1).ToBitmapSource();
				ApplicationIcon = PluginUtils.GetCachedExeIcon(exePath, 0).ToBitmapSource();
			}
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
			Export = async (capture, token) => await ExportCaptureAsync(capture, null, token);
			Text = Text = $"Export to {WordDesignation}";
			Designation = WordDesignation;
			Icon = ApplicationIcon;
		}

		/// <summary>
		/// Load the current documents to export to
		/// </summary>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public override Task Refresh(CancellationToken token = new CancellationToken())
		{
			Children.Clear();
			return Task.Run(() =>
			{
				foreach (var caption in WordExporter.GetWordDocuments().OrderBy(x => x))
				{
					var wordDestination = new WordDestination
					{
						Icon = DocumentIcon,
						Export = async (capture, exportToken) => await ExportCaptureAsync(capture, caption, exportToken),
						Text = $"Export to {WordDesignation} - {caption}",
						OfficeConfiguration = OfficeConfiguration,
						GreenshotLanguage = GreenshotLanguage
					};
                    Children.Add(wordDestination);
				}
			}, token);
		}

		private Task<INotification> ExportCaptureAsync(ICapture capture, string documentCaption, CancellationToken token = default(CancellationToken))
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
			if (documentCaption != null)
			{
				try
				{
					WordExporter.InsertIntoExistingDocument(documentCaption, tmpFile);
				}
				catch (Exception)
				{
					try
					{
						WordExporter.InsertIntoExistingDocument(documentCaption, tmpFile);
					}
					catch (Exception ex)
					{
						LOG.Error(ex);
						returnValue.ErrorText = ex.Message;
						returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, WordDesignation);
                        return Task.FromResult(returnValue);
					}
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
					try
					{
						WordExporter.InsertIntoNewDocument(tmpFile, null, null);
					}
					catch (Exception ex)
					{
						LOG.Error(ex);
						returnValue.ErrorText = ex.Message;
						returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, WordDesignation);
						return Task.FromResult(returnValue);
					}
				}
			}
			return Task.FromResult(returnValue);
		}
	}
}