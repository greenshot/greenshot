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
using GreenshotConfluencePlugin.Model;
using GreenshotPlugin.Core;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dapplo.Config.Language;
using Dapplo.HttpExtensions;

namespace GreenshotConfluencePlugin
{
	/// <summary>
	/// Description of ConfluenceDestination.
	/// </summary>
	public class ConfluenceDestination : AbstractDestination
	{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (ConfluenceDestination));
		private static readonly IConfluenceConfiguration config = IniConfig.Current.Get<IConfluenceConfiguration>();
		private static readonly IConfluenceLanguage _language = LanguageLoader.Current.Get<IConfluenceLanguage>();
		private static Image confluenceIcon = null;
		private Content _content;

		public static bool IsInitialized
		{
			get;
			private set;
		}

		static ConfluenceDestination()
		{
			IsInitialized = false;
			try
			{
				Uri confluenceIconUri = new Uri("/GreenshotConfluencePlugin;component/Images/Confluence.ico", UriKind.Relative);
				using (Stream iconStream = Application.GetResourceStream(confluenceIconUri).Stream)
				{
					using (Image tmpImage = Image.FromStream(iconStream))
					{
						confluenceIcon = ImageHelper.Clone(tmpImage);
					}
				}
				IsInitialized = true;
			}
			catch (Exception ex)
			{
				LOG.ErrorFormat("Problem in the confluence static initializer: {0}", ex.Message);
			}
		}

		public ConfluenceDestination()
		{
		}

		public ConfluenceDestination(Content content)
		{
			_content = content;
		}

		public override string Designation
		{
			get
			{
				return "Confluence";
			}
		}

		public override string Description
		{
			get
			{
				if (_content == null)
				{
					return _language.UploadMenuItem;
				}
				else
				{
					return _language.UploadMenuItem + ": \"" + _content.Title + "\"";
				}
			}
		}

		public override bool IsDynamic
		{
			get
			{
				return true;
			}
		}

		public override bool IsActive
		{
			get
			{
				return base.IsActive && !string.IsNullOrEmpty(config.RestUrl);
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				return confluenceIcon;
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations()
		{
			// TODO: Fix async, this should not be called from synchronous code but should run as a task which adds to the context menu
			IList<Content> currentPages = Task.Run(async () => await ConfluenceUtils.GetCurrentPages()).GetAwaiter().GetResult();
			if (currentPages == null || currentPages.Count == 0)
			{
				yield break;
			}
			foreach (var currentPage in currentPages)
			{
				yield return new ConfluenceDestination(currentPage);
			}
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surfaceToUpload, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(config.FilenamePattern, config.UploadFormat, captureDetails));
			var outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			if (_content == null)
			{
				ConfluenceUpload confluenceUpload = new ConfluenceUpload(filename);
				Nullable<bool> dialogResult = await confluenceUpload.ShowDialogAsync(token);
				if (dialogResult.HasValue && dialogResult.Value)
				{
					_content = confluenceUpload.SelectedPage;
					filename = confluenceUpload.Filename;
				}
			}

			string extension = "." + config.UploadFormat;
			if (filename != null && !filename.ToLower().EndsWith(extension))
			{
				filename = filename + extension;
			}
			if (_content != null)
			{
				try
				{
					var confluenceApi = ConfluencePlugin.ConfluenceAPI;
					// Run upload in the background
					await PleaseWaitWindow.CreateAndShowAsync(Description, _language.CommunicationWait, async (progress, pleaseWaitToken) =>
					{
						var multipartFormDataContent = new MultipartFormDataContent();
						using (var stream = new MemoryStream())
						{
							ImageOutput.SaveToStream(surfaceToUpload, stream, outputSettings);
							stream.Position = 0;
							using (var uploadStream = new ProgressStream(stream, progress))
							{
								using (var streamContent = new StreamContent(uploadStream))
								{
									streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
									multipartFormDataContent.Add(streamContent, "file", filename);
									return await confluenceApi.AttachToContentAsync(_content.Id, multipartFormDataContent, pleaseWaitToken);
								}
							}
						}
					}, token);

					LOG.Debug("Uploaded to Confluence.");
					exportInformation.ExportMade = true;
					var exportedUri = confluenceApi.ConfluenceBaseUri.AppendSegments("pages", "viewpage.action").ExtendQuery(new Dictionary<string, object>
					{
						{
							"pageId", _content.Id
						}
					});
					exportInformation.ExportedToUri = exportedUri;
					if (config.CopyWikiMarkupForImageToClipboard)
					{
						ClipboardHelper.SetClipboardData("!" + filename + "!");
					}
					if (config.OpenPageAfterUpload)
					{
						try
						{
							Process.Start(exportInformation.ExportedToUri.AbsoluteUri);
						}
						catch
						{
							// ignored
						}
					}
				}
				catch (TaskCanceledException tcEx)
				{
					exportInformation.ErrorMessage = tcEx.Message;
					LOG.Info(tcEx.Message);
				}
				catch (Exception e)
				{
					exportInformation.ErrorMessage = e.Message;
					LOG.Warn(e);
					MessageBox.Show(_language.UploadFailure + " " + e.Message, Designation, MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			ProcessExport(exportInformation, surfaceToUpload);
			return exportInformation;
		}
	}
}