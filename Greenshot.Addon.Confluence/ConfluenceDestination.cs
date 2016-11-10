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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Confluence.Entities;
using Dapplo.HttpExtensions;
using Dapplo.Utils;
using Greenshot.Addon.Confluence.Windows;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Windows;
using Dapplo.Log;

namespace Greenshot.Addon.Confluence
{
	[Destination(ConfluenceDesignation)]
	public sealed class ConfluenceDestination : AbstractDestination
	{
		private const string ConfluenceDesignation = "Confluence";
		private static readonly LogSource Log = new LogSource();

		[Import]
		private IConfluenceConfiguration ConfluenceConfiguration
		{
			get;
			set;
		}

		[Import]
		private IConfluenceLanguage ConfluenceLanguage
		{
			get;
			set;
		}

		/// <summary>
		/// Setup
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			Designation = ConfluenceDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, null, token);
			Text = ConfluenceLanguage.UploadMenuItem;
			Icon = new PackIconAtlassian
			{
				Kind = PackIconKindAtlassian.Confluence
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
			await Task.Run(async () =>
			{
				var pages = await ConfluenceUtils.GetCurrentPages(token);
				return pages.OrderBy(x => x.Title).Select(currentPage => new ConfluenceDestination
				{
					Icon = new PackIconAtlassian
					{
						Kind = PackIconKindAtlassian.Confluence
					},
					Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, currentPage, exportToken),
					Text = currentPage.Title,
					ConfluenceConfiguration = ConfluenceConfiguration,
					ConfluenceLanguage = ConfluenceLanguage
				}).ToList();
			}, token).ContinueWith(async destinations =>
			{
				foreach (var confluenceDestination in await destinations)
				{
					Children.Add(confluenceDestination);
				}
			}, token, TaskContinuationOptions.None, UiContext.UiTaskScheduler).ConfigureAwait(false);
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, Content page, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = ConfluenceDesignation,
				SourceType = SourceTypes.Destination,
				Text = string.Format(ConfluenceLanguage.UploadSuccess, ConfluenceDesignation)
			};

			string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(ConfluenceConfiguration.FilenamePattern, ConfluenceConfiguration.UploadFormat, capture.CaptureDetails));
			var outputSettings = new SurfaceOutputSettings(ConfluenceConfiguration.UploadFormat, ConfluenceConfiguration.UploadJpegQuality, ConfluenceConfiguration.UploadReduceColors);
			if (page == null)
			{
				var confluenceUpload = new ConfluenceUpload(filename);
				bool? dialogResult = await confluenceUpload.ShowDialogAsync(token);
				if (dialogResult.HasValue && dialogResult.Value)
				{
					page = confluenceUpload.SelectedPage;
					filename = confluenceUpload.Filename;
				}
			}

			string extension = "." + ConfluenceConfiguration.UploadFormat;
			if (filename != null && !filename.ToLower().EndsWith(extension))
			{
				filename = filename + extension;
			}
			if (page != null)
			{
				try
				{
					var confluenceApi = ConfluencePlugin.ConfluenceAPI;
					// Run upload in the background
					await PleaseWaitWindow.CreateAndShowAsync(ConfluenceDesignation, ConfluenceLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
					{
						var httpBehaviour = HttpBehaviour.Current.ShallowClone();
						// Use UploadProgress
						httpBehaviour.UploadProgress = (percent) =>
						{
							UiContext.RunOn(() => progress.Report((int)(percent * 100)));
						};
						httpBehaviour.MakeCurrent();
						using (var stream = new MemoryStream())
						{
							ImageOutput.SaveToStream(capture, stream, outputSettings);
							stream.Position = 0;
							using (var streamContent = new StreamContent(stream))
							{
								streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
								return await confluenceApi.AttachAsync(page.Id, streamContent, filename, "Added via Greenshot", "image/" + outputSettings.Format, pleaseWaitToken);
							}
						}
					}, token);

					Log.Debug().WriteLine("Uploaded to Confluence.");
					var exportedUri = confluenceApi.ConfluenceApiBaseUri.AppendSegments("pages", "viewpage.action").ExtendQuery(new Dictionary<string, object>
					{
						{
							"pageId", page.Id
						}
					});
					returnValue.ImageLocation = exportedUri;
					if (ConfluenceConfiguration.CopyWikiMarkupForImageToClipboard)
					{
						ClipboardHelper.SetClipboardData("!" + filename + "!");
					}
					if (ConfluenceConfiguration.OpenPageAfterUpload)
					{
						try
						{
							Process.Start(returnValue.ImageLocation.AbsoluteUri);
						}
						catch
						{
							// ignored
						}
					}
				}
				catch (TaskCanceledException tcEx)
				{
					returnValue.Text = string.Format(ConfluenceLanguage.UploadFailure, ConfluenceDesignation);
					returnValue.NotificationType = NotificationTypes.Cancel;
					returnValue.ErrorText = tcEx.Message;
					Log.Info().WriteLine(tcEx.Message);
				}
				catch (Exception e)
				{
					returnValue.Text = string.Format(ConfluenceLanguage.UploadFailure, ConfluenceDesignation);
					returnValue.NotificationType = NotificationTypes.Fail;
					returnValue.ErrorText = e.Message;
					Log.Warn().WriteLine(e, "Confluence export failed");
				}
			}

			return returnValue;
		}
	}
}