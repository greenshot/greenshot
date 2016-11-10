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
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Dapplo.Utils;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Helpers;
using Dapplo.Log;
using MahApps.Metro.IconPacks;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of EmailDestination.
	/// </summary>
	[Destination(EmailDesignation, 10)]
	public sealed class EmailDestination : AbstractDestination
	{
		private const string EmailDesignation = "Email";
		private static readonly LogSource Log = new LogSource();

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
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.Email
			};
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
				await UiContext.RunOn(
				() =>
				{
					MapiMailMessage.SendImage(capture, capture.CaptureDetails);
				}, token);
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Email export failed");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				returnValue.Text = GreenshotLanguage.SettingsDestinationEmail;
            }

			return returnValue;
		}
	}
}