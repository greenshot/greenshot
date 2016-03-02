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
using System.Windows.Media.Imaging;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of FileWithDialogDestination.
	/// </summary>
	[Destination(FileWithDialogDesignation)]
	public sealed class FileWithDialogDestination : AbstractDestination
	{
		private const string FileWithDialogDesignation = "FileWithDialog";
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(FileWithDialogDestination));
		private static readonly BitmapSource FileWithDialogIcon;
		static FileWithDialogDestination()
		{
			using (var fileWithDialogIcon = GreenshotResources.GetImage("Save.Image"))
			{
				FileWithDialogIcon = fileWithDialogIcon.ToBitmapSource();
			}
        }
		[Import]
		private ICoreConfiguration CoreConfiguration
		{
			get;
			set;
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
			Text = GreenshotLanguage.SettingsDestinationFileas;
			Designation = FileWithDialogDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Icon = FileWithDialogIcon;
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = FileWithDialogDesignation,
				SourceType = SourceTypes.Destination,
				Text = Text
			};

			try
			{
				string savedTo = await Task.Factory.StartNew(() => ImageOutput.SaveWithDialog(capture, capture.CaptureDetails), token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

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
				Log.Error(e, "Save as gave an exception");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				returnValue.Text = GreenshotLanguage.EditorCancel;
            }

			return returnValue;
		}
	}
}