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

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Interfaces.Plugin;
using GreenshotPlugin.Extensions;
namespace GreenshotFlickrPlugin
{
	[Destination(FlickrDesignation)]
	public sealed class FlickrDestination : AbstractDestination
	{
		private const string FlickrDesignation = "Flickr";
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(FlickrDestination));
		private static readonly BitmapSource FlickrIcon;

		static FlickrDestination()
		{
			var resources = new ComponentResourceManager(typeof(FlickrPlugin));
			using (var flickrImage = (Bitmap) resources.GetObject("flickr"))
			{
				FlickrIcon = flickrImage.ToBitmapSource();
			}

		}

		[Import]
		private IFlickrConfiguration FlickrConfiguration
		{
			get;
			set;
		}

		[Import]
		private IFlickrLanguage FlickrLanguage
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
			Designation = FlickrDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = FlickrLanguage.UploadMenuItem;
			Icon = FlickrIcon;
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = FlickrDesignation,
				SourceType = SourceTypes.Destination,
				Text = string.Format(FlickrLanguage.UploadSuccess, FlickrDesignation)
			};
			var outputSettings = new SurfaceOutputSettings(FlickrConfiguration.UploadFormat, FlickrConfiguration.UploadJpegQuality, false);
			try
			{
				var url = await PleaseWaitWindow.CreateAndShowAsync(Designation, FlickrLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					string filename = Path.GetFileName(FilenameHelper.GetFilename(FlickrConfiguration.UploadFormat, capture.CaptureDetails));
					return await FlickrUtils.UploadToFlickrAsync(capture, outputSettings, capture.CaptureDetails.Title, filename, progress, token);
				}, token);

				if (url != null)
				{
					returnValue.ImageLocation = new Uri(url);
					if (FlickrConfiguration.AfterUploadLinkToClipBoard)
					{
						ClipboardHelper.SetClipboardData(url);
					}
				}
			}
			catch (TaskCanceledException tcEx)
			{
				returnValue.Text = string.Format(FlickrLanguage.UploadFailure, FlickrDesignation);
                returnValue.NotificationType = NotificationTypes.Cancel;
				returnValue.ErrorText = tcEx.Message;
				LOG.Information(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(FlickrLanguage.UploadFailure, FlickrDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				LOG.Warning(e, "Flickr upload gave an exception");
				MessageBox.Show(FlickrLanguage.UploadFailure + " " + e.Message, FlickrDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
        }
	}
}