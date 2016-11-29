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
using Dapplo.Utils;
using Greenshot.Addon;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core.Implementations;
using Greenshot.Core.Interfaces;
using Greenshot.Legacy.Extensions;
using MahApps.Metro.IconPacks;

#endregion

namespace Greenshot.Destinations
{
	/// <summary>
	///     Description of FileWithDialogDestination.
	/// </summary>
	[Destination(FileWithDialogDesignation, 3)]
	public sealed class FileWithDialogDestination : AbstractDestination
	{
		private const string FileWithDialogDesignation = "FileWithDialog";
		private static readonly LogSource Log = new LogSource();

		[Import]
		private ICoreConfiguration CoreConfiguration { get; set; }

		[Import]
		private IGreenshotLanguage GreenshotLanguage { get; set; }

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = FileWithDialogDesignation,
				NotificationSourceType = NotificationSourceTypes.Destination,
				Text = Text
			};

			try
			{
				string savedTo = null;
				await UiContext.RunOn(() => savedTo = capture.SaveWithDialog(capture.CaptureDetails), token);

				// Bug #2918756 don't overwrite path if SaveWithDialog returns null!
				if (savedTo != null)
				{
					returnValue.ImageLocation = new Uri("file://" + savedTo);
					capture.CaptureDetails.Filename = savedTo;
					CoreConfiguration.OutputFileAsFullpath = savedTo;
				}
				else
				{
					returnValue.NotificationType = NotificationTypes.Cancel;
					returnValue.Text = GreenshotLanguage.EditorCancel;
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Save as gave an exception");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				returnValue.Text = GreenshotLanguage.EditorCancel;
			}

			return returnValue;
		}

		protected override void Initialize()
		{
			base.Initialize();
			Text = GreenshotLanguage.SettingsDestinationFileas;
			Designation = FileWithDialogDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.Save
			};
		}
	}
}