/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using Windows.Storage.Streams;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotWin10Plugin.Native;
using System.Threading.Tasks;
using Color = Windows.UI.Color;

namespace GreenshotWin10Plugin
{
	/// <summary>
	/// This uses the Share from Windows 10 to make the capture available to apps.
	/// </summary>
	public class Win10ShareDestination : AbstractDestination
	{
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10ShareDestination));

		public override string Designation { get; } = "Share";
		public override string Description { get; } = "Windows 10 share";

		/// <summary>
		/// Share the screenshot with a windows app
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>ExportInformation</returns>
		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			try
			{
				var handle = PluginUtils.Host.GreenshotForm.Handle;

				var exportTarget = Task.Run(async () =>
				{
					var taskCompletionSource = new TaskCompletionSource<string>();

					using (var imageStream = new MemoryRandomAccessStream())
					using (var logoStream = new MemoryRandomAccessStream())
					using (var thumbnailStream = new MemoryRandomAccessStream())
					{
						// Create capture for export
						ImageOutput.SaveToStream(surface, imageStream, new SurfaceOutputSettings());
						imageStream.Position = 0;
						var imageRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(imageStream);
						RandomAccessStreamReference thumbnailRandomAccessStreamReference;
						RandomAccessStreamReference logoRandomAccessStreamReference;

						// Create thumbnail
						using (var tmpImageForThumbnail = surface.GetImageForExport())
						{
							using (var thumbnail = ImageHelper.CreateThumbnail(tmpImageForThumbnail, 240, 160))
							{
								ImageOutput.SaveToStream(thumbnail, null, thumbnailStream, new SurfaceOutputSettings());
								thumbnailStream.Position = 0;
								thumbnailRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(thumbnailStream);
							}
						}
						// Create logo
						using (var logo = GreenshotResources.getGreenshotIcon().ToBitmap())
						{
							using (var logoThumbnail = ImageHelper.CreateThumbnail(logo, 30, 30))
							{
								ImageOutput.SaveToStream(logoThumbnail, null, logoStream, new SurfaceOutputSettings());
								logoStream.Position = 0;
								logoRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(logoStream);
							}
						}
						string applicationName = null;
						var dataTransferManagerHelper = new DataTransferManagerHelper(handle);
						dataTransferManagerHelper.DataTransferManager.TargetApplicationChosen += (dtm, args) =>
						{
							Log.DebugFormat("Trying to share with {0}", args.ApplicationName);
							applicationName = args.ApplicationName;
						};
						dataTransferManagerHelper.DataTransferManager.DataRequested += (sender, args) =>
						{
							var deferral = args.Request.GetDeferral();
							args.Request.Data.OperationCompleted += (dp, eventArgs) =>
							{
								Log.DebugFormat("OperationCompleted: {0}, shared with", eventArgs.Operation);
								taskCompletionSource.TrySetResult(applicationName);
							};
							var dataPackage = args.Request.Data;
							dataPackage.Properties.Title = captureDetails.Title;
							dataPackage.Properties.ApplicationName = "Greenshot";
							dataPackage.Properties.Description = "Share a screenshot";
							dataPackage.Properties.Thumbnail = thumbnailRandomAccessStreamReference;
							dataPackage.Properties.Square30x30Logo = logoRandomAccessStreamReference;
							dataPackage.Properties.LogoBackgroundColor = Color.FromArgb(0xff, 0x3d, 0x3d, 0x3d);
							dataPackage.SetBitmap(imageRandomAccessStreamReference);
							dataPackage.Destroyed += (dp, o) =>
							{
								Log.Debug("Destroyed.");
								taskCompletionSource.TrySetCanceled();
							};
							deferral.Complete();
						};
						dataTransferManagerHelper.ShowShareUi();
						return await taskCompletionSource.Task;
					}
				}).Result;
				if (string.IsNullOrWhiteSpace(exportTarget))
				{
					exportInformation.ExportMade = false;
				}
				else
				{
					exportInformation.ExportMade = true;
					exportInformation.DestinationDescription = exportTarget;
				}
			}
			catch (Exception ex)
			{
				exportInformation.ExportMade = false;
				exportInformation.ErrorMessage = ex.Message;
			}

			ProcessExport(exportInformation, surface);
			return exportInformation;

		}
	}
}
