/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

using GreenshotPlugin.Core;
using GreenshotPlugin.Windows;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Dapplo.HttpExtensions;
using GreenshotConfluencePlugin.Model;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotConfluencePlugin
{
	[Destination(ConfluenceDesignation)]
	public sealed class ConfluenceDestination : AbstractDestination
	{
		private const string ConfluenceDesignation = "Confluence";
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(ConfluenceDestination));
		private static readonly BitmapSource ConfluenceIcon;

		static ConfluenceDestination()
		{
			var confluenceIconUri = new Uri("/GreenshotConfluencePlugin;component/Images/Confluence.ico", UriKind.Relative);
			var streamResourceInfo = Application.GetResourceStream(confluenceIconUri);
			if (streamResourceInfo != null)
			{
				using (var iconStream = streamResourceInfo.Stream)
				{
					using (var tmpImage = (Bitmap)Image.FromStream(iconStream))
					{
						ConfluenceIcon = tmpImage.ToBitmapSource();
					}
				}
			}
		}

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
			Icon = ConfluenceIcon;
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
			var currentPages = await ConfluenceUtils.GetCurrentPages(token);
			if (currentPages == null || currentPages.Count == 0)
			{
				return;
			}
			foreach (var currentPage in currentPages)
			{
				var confluenceDestination = new ConfluenceDestination
				{
					Icon = ConfluenceIcon,
					Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, currentPage, exportToken),
					Text = currentPage.Title,
					ConfluenceConfiguration = ConfluenceConfiguration,
					ConfluenceLanguage = ConfluenceLanguage
				};
				Children.Add(confluenceDestination);
			}
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
				var confluenceUpload = new Forms.ConfluenceUpload(filename);
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
						var multipartFormDataContent = new MultipartFormDataContent();
						using (var stream = new MemoryStream())
						{
							ImageOutput.SaveToStream(capture, stream, outputSettings);
							stream.Position = 0;
							using (var uploadStream = new ProgressStream(stream, progress))
							{
								using (var streamContent = new StreamContent(uploadStream))
								{
									streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
									multipartFormDataContent.Add(streamContent, "file", filename);
									return await confluenceApi.AttachToContentAsync(page.Id, multipartFormDataContent, pleaseWaitToken);
								}
							}
						}
					}, token);

					LOG.Debug("Uploaded to Confluence.");
					var exportedUri = confluenceApi.ConfluenceBaseUri.AppendSegments("pages", "viewpage.action").ExtendQuery(new Dictionary<string, object>
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
					LOG.Information(tcEx.Message);
				}
				catch (Exception e)
				{
					returnValue.Text = string.Format(ConfluenceLanguage.UploadFailure, ConfluenceDesignation);
					returnValue.NotificationType = NotificationTypes.Fail;
					returnValue.ErrorText = e.Message;
					LOG.Warning(e, "Confluence export failed");
				}
			}

			return returnValue;
        }
	}
}