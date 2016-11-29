//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Greenshot.Addon;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.CaptureCore;
using Greenshot.Core.Implementations;
using Greenshot.Core.Interfaces;
using MahApps.Metro.IconPacks;

#endregion

namespace Greenshot.Destinations
{
	/// <summary>
	///     Description of ClipboardDestination.
	/// </summary>
	[Destination(ClipboardDesignation, 2)]
	public sealed class ClipboardDestination : AbstractDestination
	{
		private const string ClipboardDesignation = "Clipboard";
		private static readonly LogSource Log = new LogSource();

		[Import]
		private ICoreConfiguration CoreConfiguration { get; set; }

		[Import]
		private IGreenshotLanguage GreenshotLanguage { get; set; }

		private Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = ClipboardDesignation,
				NotificationSourceType = NotificationSourceTypes.Destination,
				Text = Text
			};

			try
			{
				ClipboardHelper.SetClipboardData(capture);
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Clipboard export failed");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				returnValue.Text = GreenshotLanguage.ClipboardError;
			}

			return Task.FromResult<INotification>(returnValue);
		}

		protected override void Initialize()
		{
			base.Initialize();
			Text = GreenshotLanguage.SettingsDestinationClipboard;
			Designation = ClipboardDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.Clipboard
			};
		}
	}
}