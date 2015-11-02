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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;

namespace GreenshotBoxPlugin
{
	[Destination(_boxDesignation)]
	public class BoxDestination : AbstractDestination
	{
		private const string _boxDesignation = "Box";
		private static readonly ILog LOG = LogManager.GetLogger(typeof (BoxDestination));

		[Import]
		public IBoxConfiguration BoxConfiguration
		{
			get;
			set;
		}

		[Import]
		public IBoxLanguage BoxLanguage
		{
			get;
			set;
		}

		public override string Designation
		{
			get
			{
				return _boxDesignation;
			}
		}

		public override string Text
		{
			get
			{
				return BoxLanguage.UploadMenuItem;
			}

			set
			{
			}
		}

		public BoxDestination()
		{
			Export = async (capture, token) => await ExportCaptureAsync(capture, token);
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = _boxDesignation,
				SourceType = SourceTypes.Destination,
				Text = string.Format(BoxLanguage.UploadSuccess, _boxDesignation)
			};
			try
			{
				var url = await PleaseWaitWindow.CreateAndShowAsync(_boxDesignation, BoxLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
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
				returnValue.Text = string.Format(BoxLanguage.UploadFailure, _boxDesignation);
                returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = tcEx.Message;
				LOG.Info(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(BoxLanguage.UploadFailure, _boxDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				LOG.Warn(e);
				MessageBox.Show(BoxLanguage.UploadFailure + " " + e.Message, _boxDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
        }
	}
}