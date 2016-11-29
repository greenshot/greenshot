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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.Storage.Streams;
using Dapplo.Log;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Windows;
using Greenshot.Addon.WindowsShare.Native;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core;
using Greenshot.Core.Gfx;
using Greenshot.Core.Implementations;
using Greenshot.Core.Interfaces;
using MahApps.Metro.IconPacks;
using Greenshot.Legacy.Extensions;

#endregion

namespace Greenshot.Addon.WindowsShare
{
	[Destination(ShareDesignation)]
	[PartNotDiscoverable]
	public sealed class ShareDestination : AbstractDestination
	{
		private const string ShareDesignation = "Share";
		private static readonly LogSource Log = new LogSource();

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = ShareDesignation,
				NotificationSourceType = NotificationSourceTypes.Destination,
				Text = ShareDesignation
			};

			try
			{
				await PleaseWaitWindow.CreateAndShowAsync(Designation, "Sharing", async (progress, pleaseWaitToken) =>
				{
					RandomAccessStreamReference imageRandomAccessStreamReference;
					using (var imageStream = new MemoryStream())
					{
						capture.SaveToStream(imageStream, new SurfaceOutputSettings());
						imageStream.Position = 0;
						imageRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(imageStream.AsRandomAccessStream());
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
								thumbnailRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(thumbnailStream.AsRandomAccessStream());
							}
						}
					}

					var pwWindow = progress as Window;
					if (pwWindow != null)
					{
						var handle = new WindowInteropHelper(pwWindow).Handle;

						var dataTransferManagerHelper = new DataTransferManagerHelper(handle);
						dataTransferManagerHelper.DataTransferManager.TargetApplicationChosen += (dtm, args) => { Log.Debug().WriteLine("Exported to {0}", args.ApplicationName); };
						dataTransferManagerHelper.DataTransferManager.DataRequested += (sender, args) =>
						{
							var deferral = args.Request.GetDeferral();
							args.Request.Data.OperationCompleted += (dp, eventArgs) => { Log.Debug().WriteLine("OperationCompleted: {0}", eventArgs.Operation); };
							var dataPackage = args.Request.Data;
							dataPackage.Properties.Title = "Share";
							dataPackage.Properties.ApplicationName = "Greenshot";
							dataPackage.Properties.Description = "screenshot";
							dataPackage.Properties.Thumbnail = thumbnailRandomAccessStreamReference;
							dataPackage.SetBitmap(imageRandomAccessStreamReference);
							dataPackage.Destroyed += (dp, o) => { Log.Debug().WriteLine("Destroyed."); };
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
				Log.Info().WriteLine(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = "Share failed.";
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				Log.Warn().WriteLine(e, "Share export failed");
			}
			return returnValue;
		}

		/// <summary>
		///     Setup
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			Designation = ShareDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = ShareDesignation;
			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.Share
			};
		}
	}
}