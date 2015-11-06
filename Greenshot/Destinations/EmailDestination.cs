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

using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;
using log4net;
using System;
using System.Threading;
using System.Threading.Tasks;
using GreenshotPlugin.Interfaces;
using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.Interfaces.Destination;
using Greenshot.Helpers;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of EmailDestination.
	/// </summary>
	[Destination(EmailDesignation)]
	public sealed class EmailDestination : AbstractDestination
	{
		private const string EmailDesignation = "Email";
		private static readonly ILog LOG = LogManager.GetLogger(typeof (EmailDestination));
		private static readonly BitmapSource EmailIcon;
		static EmailDestination()
		{
			using (var emailIcon = GreenshotResources.GetImage("Email.Image"))
			{
				EmailIcon = emailIcon.ToBitmapSource();
			}
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
			Text = GreenshotLanguage.SettingsDestinationEmail;
			Designation = EmailDesignation;
			Export = async (capture, token) => await ExportCaptureAsync(capture, token);
			Icon = EmailIcon;
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = EmailDesignation,
				SourceType = SourceTypes.Destination,
				Text = Text
			};

			try
			{
				// There is not much that can work async for the MapiMailMessage
				await Task.Factory.StartNew(() =>
				{
					MapiMailMessage.SendImage(capture, capture.CaptureDetails);
				}, token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}
			catch (Exception e)
			{
				LOG.Error(e);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				returnValue.Text = GreenshotLanguage.SettingsDestinationEmail;
            }

			return returnValue;
		}
	}
}