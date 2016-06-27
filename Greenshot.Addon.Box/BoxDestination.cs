/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom,
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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Windows;
using Dapplo.Log.Facade;

namespace Greenshot.Addon.Box
{
	[Destination(BoxDesignation)]
	public sealed class BoxDestination : AbstractDestination
	{
		private const string BoxDesignation = "Box";
		private static readonly LogSource Log = new LogSource();
		private static readonly BitmapSource BoxIcon;
		private OAuth2Settings _oauth2Settings;

		static BoxDestination()
		{
			var resources = new ComponentResourceManager(typeof(BoxPlugin));
			using (var boxImage = (Bitmap)resources.GetObject("Box"))
			{
				BoxIcon = boxImage.ToBitmapSource();
			}
		}

		[Import]
		private IBoxConfiguration BoxConfiguration
		{
			get;
			set;
		}

		[Import]
		private IBoxLanguage BoxLanguage
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
			Designation = BoxDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = BoxLanguage.UploadMenuItem;
			Icon = BoxIcon;

			_oauth2Settings = new OAuth2Settings
			{
				AuthorizationUri = new Uri("https://app.box.com").
					AppendSegments("api", "oauth2", "authorize").
					ExtendQuery(new Dictionary<string, string>{
						{ "response_type", "code"},
						{ "client_id", "{ClientId}" },
						{ "redirect_uri", "{RedirectUrl}" },
						{ "state", "{State}"}
					}),
				TokenUrl = new Uri("https://api.box.com/oauth2/token"),
				CloudServiceName = "Box",
				ClientId = BoxConfiguration.ClientId,
				ClientSecret = BoxConfiguration.ClientSecret,
				RedirectUrl = "https://www.box.com/home/",
				AuthorizeMode = AuthorizeModes.EmbeddedBrowser,
				Token = BoxConfiguration
			};
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = BoxDesignation,
				SourceType = SourceTypes.Destination,
				Text = string.Format(BoxLanguage.UploadSuccess, BoxDesignation)
			};
			try
			{
				var url = await PleaseWaitWindow.CreateAndShowAsync(
					BoxDesignation,
					BoxLanguage.CommunicationWait,
					async (progress, pleaseWaitToken) => await BoxUtils.UploadToBoxAsync(_oauth2Settings, capture, progress, token),
					token);

				if (url != null)
				{
					returnValue.ImageLocation = new Uri(url);
					if (BoxConfiguration.AfterUploadLinkToClipBoard)
					{
						ClipboardHelper.SetClipboardData(url);
					}
				}

			}
			catch (TaskCanceledException tcEx)
			{
				returnValue.Text = string.Format(BoxLanguage.UploadFailure, BoxDesignation);
				returnValue.NotificationType = NotificationTypes.Cancel;
				returnValue.ErrorText = tcEx.Message;
				Log.Info().WriteLine(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(BoxLanguage.UploadFailure, BoxDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				Log.Warn().WriteLine(e, "Box export failed");
				MessageBox.Show(BoxLanguage.UploadFailure + " " + e.Message, BoxDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
        }
	}
}