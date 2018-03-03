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
using System.IO;
using Windows.Storage.Streams;
using GreenshotPlugin.Core;
using GreenshotWin10Plugin.Native;
using System.Threading.Tasks;
using Windows.Storage;
using Color = Windows.UI.Color;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using Dapplo.Log;
using Greenshot.Gfx;
using GreenshotPlugin.Addons;
using GreenshotPlugin.Components;
using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.Gfx;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotWin10Plugin
{
    /// <summary>
    /// This uses the Share from Windows 10 to make the capture available to apps.
    /// </summary>
    [Destination("WIN10Share")]
    public class Win10ShareDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
	    private readonly WindowHandle _windowHandle;

        [ImportingConstructor]
        public Win10ShareDestination(WindowHandle windowHandle)
	    {
	        _windowHandle = windowHandle;
	    }

		public override string Description { get; } = "Windows 10 share";

		/// <summary>
		/// Icon for the App-share, the icon was found via: http://help4windows.com/windows_8_shell32_dll.shtml
		/// </summary>
		public override Bitmap DisplayIcon => PluginUtils.GetCachedExeIcon(FilenameHelper.FillCmdVariables(@"%windir%\system32\shell32.dll"), 238);

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
				var exportTarget = Task.Run(async () =>
				{
					var taskCompletionSource = new TaskCompletionSource<string>();

					using (var imageStream = new MemoryRandomAccessStream())
					using (var logoStream = new MemoryRandomAccessStream())
					using (var thumbnailStream = new MemoryRandomAccessStream())
					{
						var outputSettings = new SurfaceOutputSettings();
						outputSettings.PreventGreenshotFormat();

						// Create capture for export
						ImageOutput.SaveToStream(surface, imageStream, outputSettings);
						imageStream.Position = 0;
						Log.Info().WriteLine("Created RandomAccessStreamReference for the image");
						var imageRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(imageStream);
						RandomAccessStreamReference thumbnailRandomAccessStreamReference;
						RandomAccessStreamReference logoRandomAccessStreamReference;

						// Create thumbnail
						using (var tmpImageForThumbnail = surface.GetBitmapForExport())
						{
							using (var thumbnail = tmpImageForThumbnail.CreateThumbnail(240, 160))
							{
								ImageOutput.SaveToStream(thumbnail, null, thumbnailStream, outputSettings);
								thumbnailStream.Position = 0;
								thumbnailRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(thumbnailStream);
								Log.Info().WriteLine("Created RandomAccessStreamReference for the thumbnail");
							}
						}
						// Create logo
						using (var logo = GreenshotResources.GetGreenshotIcon().ToBitmap())
						{
							using (var logoThumbnail = logo.CreateThumbnail(30, 30))
							{
								ImageOutput.SaveToStream(logoThumbnail, null, logoStream, outputSettings);
								logoStream.Position = 0;
								logoRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(logoStream);
								Log.Info().WriteLine("Created RandomAccessStreamReference for the logo");
							}
						}
						string applicationName = null;
						var dataTransferManagerHelper = new DataTransferManagerHelper(_windowHandle.Handle);
						dataTransferManagerHelper.DataTransferManager.TargetApplicationChosen += (dtm, args) =>
						{
							Log.Info().WriteLine("Trying to share with {0}", args.ApplicationName);
							applicationName = args.ApplicationName;
						};
						var filename = FilenameHelper.GetFilename(OutputFormats.png, captureDetails);
						var storageFile = await StorageFile.CreateStreamedFileAsync(filename, async streamedFileDataRequest =>
						{
							// Information on how was found here: https://socialeboladev.wordpress.com/2013/03/15/how-to-use-createstreamedfileasync/
							Log.Debug().WriteLine("Creating deferred file {0}", filename);
							try
							{
								using (var deferredStream = streamedFileDataRequest.AsStreamForWrite())
								{
									await imageStream.CopyToAsync(deferredStream).ConfigureAwait(false);
									await imageStream.FlushAsync().ConfigureAwait(false);
								}
								// Signal that the stream is ready
								streamedFileDataRequest.Dispose();
							}
							catch (Exception)
							{
								streamedFileDataRequest.FailAndClose(StreamedFileFailureMode.Incomplete);
							}
							// Signal transfer ready to the await down below
							taskCompletionSource.TrySetResult(applicationName);
						}, imageRandomAccessStreamReference).AsTask().ConfigureAwait(false);
						
						dataTransferManagerHelper.DataTransferManager.DataRequested += (sender, args) =>
						{
							var deferral = args.Request.GetDeferral();
						    var dataPackage = args.Request.Data;
						    dataPackage.OperationCompleted += (dp, eventArgs) =>
							{
								Log.Debug().WriteLine("OperationCompleted: {0}, shared with", eventArgs.Operation);
								taskCompletionSource.TrySetResult(applicationName);
							};
							dataPackage.Properties.Title = captureDetails.Title;
							dataPackage.Properties.ApplicationName = "Greenshot";
							dataPackage.Properties.Description = "Share a screenshot";
							dataPackage.Properties.Thumbnail = thumbnailRandomAccessStreamReference;
							dataPackage.Properties.Square30x30Logo = logoRandomAccessStreamReference;
							dataPackage.Properties.LogoBackgroundColor = Color.FromArgb(0xff, 0x3d, 0x3d, 0x3d);
							dataPackage.SetStorageItems(new List<IStorageItem> { storageFile });
							dataPackage.SetBitmap(imageRandomAccessStreamReference);
							dataPackage.Destroyed += (dp, o) =>
							{
								Log.Debug().WriteLine("Destroyed.");
							};
						    dataPackage.OperationCompleted += (dp, o) =>
						    {
						        Log.Debug().WriteLine("Completed.");
						    };
						    dataPackage.ShareCompleted += (dp, o) =>
						    {
						        Log.Debug().WriteLine("Share completed.");
						    };
                            deferral.Complete();
						};
						dataTransferManagerHelper.ShowShareUi();
						return await taskCompletionSource.Task.ConfigureAwait(false);
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
