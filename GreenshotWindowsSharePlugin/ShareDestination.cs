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
using log4net;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Storage.Streams;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.Interfaces.Plugin;
using GreenshotWindowsSharePlugin.Native;

namespace GreenshotWindowsSharePlugin
{
	[Destination(ShareDesignation), PartNotDiscoverable]
	public sealed class ShareDestination : AbstractDestination
	{
		private const string ShareDesignation = "Share";
		private static readonly ILog LOG = LogManager.GetLogger(typeof (ShareDestination));
		private static readonly BitmapSource ShareIcon;

		static ShareDestination()
		{
			string exePath = PluginUtils.GetExePath("EXPLORER.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				using (var icon = PluginUtils.GetCachedExeIcon(exePath, 0))
				{
					ShareIcon = icon.ToBitmapSource();
				}
			}
		}

		/// <summary>
		/// Setup
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			Designation = ShareDesignation;
			Export = async (caller, capture, token) => await ExportCaptureAsync(caller, capture);
			Text = ShareDesignation;
			Icon = ShareIcon;
		}

		public override Task RefreshAsync(ICaller caller, CancellationToken token = default(CancellationToken))
		{
			IsEnabled = caller != null && caller.Handle != IntPtr.Zero;
			return Task.FromResult(true);
		}

		private Task<INotification> ExportCaptureAsync(ICaller caller, ICapture capture)
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = ShareDesignation,
				SourceType = SourceTypes.Destination,
				Text = ShareDesignation
			};

			if (caller == null || caller.Handle == IntPtr.Zero)
			{
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = "Caller doesn't have a windows handle, sharing not possible.";
				return Task.FromResult<INotification>(returnValue);
			}
			try
			{
				var dataTransferManagerHelper = new DataTransferManagerHelper(caller.Handle);
				dataTransferManagerHelper.DataTransferManager.DataRequested += (sender, args) =>
				{
					var dataPackage = args.Request.Data;
					dataPackage.Properties.Title = "Share";
					dataPackage.Properties.ApplicationName = "Greenshot";
					dataPackage.Properties.Description = "screenshot";
					try
					{
						//dataPackage.Properties.Thumbnail

						using (var imageStream = new MemoryStream())
						{
							ImageOutput.SaveToStream(capture, imageStream, new SurfaceOutputSettings());
							imageStream.Position = 0;
							var randomAccessStream = imageStream.AsRandomAccessStream();
							dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(randomAccessStream));
						}

					}
					catch (Exception ex)
					{
						args.Request.FailWithDisplayText(ex.Message);
					}
				};
				dataTransferManagerHelper.ShowShareUi();
			}
			catch (Exception e)
			{
				returnValue.Text = "Share failed.";
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				LOG.Warn(e);
			}
			return Task.FromResult<INotification>(returnValue);
        }
	}
}