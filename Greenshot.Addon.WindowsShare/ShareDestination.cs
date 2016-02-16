/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows.Storage.Streams;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Windows;
using Greenshot.Addon.WindowsShare.Native;

namespace Greenshot.Addon.WindowsShare
{
	[Destination(ShareDesignation), PartNotDiscoverable]
	public sealed class ShareDestination : AbstractDestination
	{
		private const string ShareDesignation = "Share";
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(ShareDestination));
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
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = ShareDesignation;
			Icon = ShareIcon;
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = ShareDesignation,
				SourceType = SourceTypes.Destination,
				Text = ShareDesignation
			};

			try
			{
				await PleaseWaitWindow.CreateAndShowAsync(Designation, "Sharing", async (progress, pleaseWaitToken) =>
				{
					RandomAccessStreamReference imageRandomAccessStreamReference;
					using (var imageStream = new MemoryStream())
					{
						ImageOutput.SaveToStream(capture, imageStream, new SurfaceOutputSettings());
						imageStream.Position = 0;
						var randomAccessStream = await FromMemoryStream(imageStream);
						imageRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(randomAccessStream);
					}

					RandomAccessStreamReference thumbnailRandomAccessStreamReference;
					using (var thumbnailStream = new MemoryStream())
					{
						using (var tmpImageForThumbnail = capture.GetImageForExport())
						{
							using (var thumbnail = ImageHelper.CreateThumbnail(tmpImageForThumbnail, 240, 160))
							{
								ImageOutput.SaveToStream(thumbnail, null, thumbnailStream, new SurfaceOutputSettings());
								thumbnailStream.Position = 0;
								var randomAccessStream = await FromMemoryStream(thumbnailStream);
								thumbnailRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(randomAccessStream);
							}
						}
					}

					var pwWindow = progress as Window;
					if (pwWindow != null)
					{
						var handle = new WindowInteropHelper(pwWindow).Handle;

						var dataTransferManagerHelper = new DataTransferManagerHelper(handle);
						dataTransferManagerHelper.DataTransferManager.TargetApplicationChosen += (dtm, args) =>
						{
							Log.Debug("Exported to {0}", args.ApplicationName);
						};
						dataTransferManagerHelper.DataTransferManager.DataRequested += (sender, args) =>
						{
							var deferral = args.Request.GetDeferral();
							args.Request.Data.OperationCompleted += (dp, eventArgs) =>
							{
								Log.Debug("OperationCompleted: {0}", eventArgs.Operation);
							};
							var dataPackage = args.Request.Data;
							dataPackage.Properties.Title = "Share";
							dataPackage.Properties.ApplicationName = "Greenshot";
							dataPackage.Properties.Description = "screenshot";
							dataPackage.Properties.Thumbnail = thumbnailRandomAccessStreamReference;
							dataPackage.SetBitmap(imageRandomAccessStreamReference);
							dataPackage.Destroyed += (dp, o) =>
							{
								Log.Debug("Destroyed.");
							};
							deferral.Complete();
						};
						dataTransferManagerHelper.ShowShareUi();
					}
					await Task.Delay(1000, pleaseWaitToken);
					return true;
				}, token);

			}
			catch (TaskCanceledException tcEx)
			{
				returnValue.Text = "Share cancelled.";
				returnValue.NotificationType = NotificationTypes.Cancel;
				returnValue.ErrorText = tcEx.Message;
				Log.Information(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = "Share failed.";
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				Log.Warning(e, "Share export failed");
			}
			return returnValue;
        }

		private static async Task<IRandomAccessStream> FromMemoryStream(MemoryStream stream)
		{
			var inMemoryStream = new InMemoryRandomAccessStream();
			using (var inputStream = stream.AsInputStream())
			{
				await RandomAccessStream.CopyAsync(inputStream, inMemoryStream);
			}
			inMemoryStream.Seek(0);
			return inMemoryStream;
		}
	}
}