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
using log4net;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;

namespace GreenshotBoxPlugin
{
	[Destination(BoxDesignation)]
	public sealed class BoxDestination : AbstractDestination
	{
		private const string BoxDesignation = "Box";
		private static readonly ILog LOG = LogManager.GetLogger(typeof (BoxDestination));
		private static readonly BitmapSource BoxIcon;

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
				var url = await PleaseWaitWindow.CreateAndShowAsync(BoxDesignation, BoxLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					return await BoxUtils.UploadToBoxAsync(capture, progress, token);
				}, token);

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
				LOG.Info(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(BoxLanguage.UploadFailure, BoxDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				LOG.Warn(e);
				MessageBox.Show(BoxLanguage.UploadFailure + " " + e.Message, BoxDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
        }
	}
}