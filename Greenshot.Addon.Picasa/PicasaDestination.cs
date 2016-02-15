/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom,
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

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Windows;

namespace Greenshot.Addon.Picasa
{
	[Destination(PicasaDesignation)]
	public sealed class PicasaDestination : AbstractDestination
	{
		private const string PicasaDesignation = "Picasa";
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(PicasaDestination));
		private static readonly BitmapSource PicasaIcon;

		static PicasaDestination()
		{
			var resources = new ComponentResourceManager(typeof(PicasaPlugin));
			using (var picasaImage = (Bitmap) resources.GetObject("Picasa"))
			{
				PicasaIcon = picasaImage.ToBitmapSource();
			}

		}

		[Import]
		private IPicasaConfiguration PicasaConfiguration
		{
			get;
			set;
		}

		[Import]
		private IPicasaLanguage PicasaLanguage
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
			Designation = PicasaDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = PicasaLanguage.UploadMenuItem;
			Icon = PicasaIcon;
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = PicasaDesignation,
				SourceType = SourceTypes.Destination,
				Text = string.Format(PicasaLanguage.UploadSuccess, PicasaDesignation)
			};
			try
			{
				var uploadUrl = await PleaseWaitWindow.CreateAndShowAsync(Designation, PicasaLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					return await PicasaUtils.UploadToPicasa(capture, progress, token).ConfigureAwait(false);
				}, token);

				if (!string.IsNullOrEmpty(uploadUrl))
				{
					returnValue.ImageLocation = new Uri(uploadUrl);
				}
			}
			catch (TaskCanceledException tcEx)
			{
				returnValue.Text = string.Format(PicasaLanguage.UploadFailure, PicasaDesignation);
                returnValue.NotificationType = NotificationTypes.Cancel;
				returnValue.ErrorText = tcEx.Message;
				Log.Information(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(PicasaLanguage.UploadFailure, PicasaDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				Log.Warning(e, "Picasa export failed");
				MessageBox.Show(PicasaLanguage.UploadFailure + " " + e.Message, PicasaDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
        }
	}
}