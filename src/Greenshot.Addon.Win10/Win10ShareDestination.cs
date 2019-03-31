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
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Dapplo.Log;
using Dapplo.Windows.Messages;
using Greenshot.Addon.Win10.Native;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Core.Enums;
using Greenshot.Gfx;
using Color = Windows.UI.Color;
using Greenshot.Addons.Resources;

namespace Greenshot.Addon.Win10
{
    /// <summary>
    /// This uses the Share from Windows 10 to make the capture available to apps.
    /// </summary>
    [Destination("WIN10Share")]
    public class Win10ShareDestination : AbstractDestination
	{
	    private readonly ExportNotification _exportNotification;
	    private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// Default constructor used for dependency injection
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
	    public Win10ShareDestination(
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification) : base(coreConfiguration, greenshotLanguage)
	    {
	        _exportNotification = exportNotification;
	    }

        /// <inheritdoc />
        public override string Description { get; } = "Windows 10 share";

		/// <summary>
		/// Icon for the App-share, the icon was found via: http://help4windows.com/windows_8_shell32_dll.shtml
		/// </summary>
		public override IBitmapWithNativeSupport DisplayIcon => PluginUtils.GetCachedExeIcon(FilenameHelper.FillCmdVariables(@"%windir%\system32\shell32.dll"), 238);

	    private class ShareInfo
	    {
            public string ApplicationName { get; set; }
            public bool AreShareProvidersRequested { get; set; }
	        public bool IsDeferredFileCreated { get; set; }
	        public DataPackageOperation CompletedWithOperation { get; set; }
	        public string AcceptedFormat { get; set; }
	        public bool IsDestroyed { get; set; }
	        public bool IsShareCompleted { get; set; }

            public TaskCompletionSource<bool> ShareTask { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
	        public bool IsDataRequested { get; set; }
	    }

        /// <summary>
        /// Share the screenshot with a windows app
        /// </summary>
        /// <param name="manuallyInitiated"></param>
        /// <param name="surface"></param>
        /// <param name="captureDetails"></param>
        /// <returns>ExportInformation</returns>
        public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            var exportInformation = new ExportInformation(Designation, Description);
			try
			{
                var triggerWindow = new Window
			    {
			        WindowState = WindowState.Normal,
			        WindowStartupLocation = WindowStartupLocation.CenterScreen,
			        Width = 400,
			        Height = 400
			    };
			    
                triggerWindow.Show();
			    var shareInfo = new ShareInfo();

                // This is a bad trick, but don't know how else to do it.
                // Wait for the focus to return, and depending on the state close the window!
                triggerWindow.WindowMessages()
			        .Where(m => m.Message == WindowsMessages.WM_SETFOCUS).Delay(TimeSpan.FromSeconds(1))
			        .Subscribe(info =>
                    {
                        if (shareInfo.ApplicationName != null)
                        {
                            return;
                        }

                        shareInfo.ShareTask.TrySetResult(false);
                    });
                var windowHandle = new WindowInteropHelper(triggerWindow).Handle;

			    await Share(shareInfo, windowHandle, surface, captureDetails);
			    Log.Debug().WriteLine("Sharing finished, closing window.");
			    triggerWindow.Close();
			    if (string.IsNullOrWhiteSpace(shareInfo.ApplicationName))
			    {
			        exportInformation.ExportMade = false;
			    }
			    else
			    {
			        exportInformation.ExportMade = true;
			        exportInformation.DestinationDescription = shareInfo.ApplicationName;
			    }
            }
			catch (Exception ex)
			{
				exportInformation.ExportMade = false;
				exportInformation.ErrorMessage = ex.Message;
			}

		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
			return exportInformation;

		}

	    /// <summary>
	    /// Share the surface by using the Share-UI of Windows 10
	    /// </summary>
	    /// <param name="shareInfo">ShareInfo</param>
	    /// <param name="handle">IntPtr with the handle for the hosting window</param>
	    /// <param name="surface">ISurface with the bitmap to share</param>
	    /// <param name="captureDetails">ICaptureDetails</param>
	    /// <returns>Task with string, which describes the application which was used to share with</returns>
	    private async Task Share(ShareInfo shareInfo, IntPtr handle, ISurface surface, ICaptureDetails captureDetails)
	    {
            using (var imageStream = new MemoryRandomAccessStream())
            using (var logoStream = new MemoryRandomAccessStream())
            using (var thumbnailStream = new MemoryRandomAccessStream())
            {
                var outputSettings = new SurfaceOutputSettings(CoreConfiguration);
                outputSettings.PreventGreenshotFormat();

                // Create capture for export
                ImageOutput.SaveToStream(surface, imageStream, outputSettings);
                imageStream.Position = 0;
                Log.Debug().WriteLine("Created RandomAccessStreamReference for the image");
                var imageRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(imageStream);

                // Create thumbnail
                RandomAccessStreamReference thumbnailRandomAccessStreamReference;
                using (var tmpImageForThumbnail = surface.GetBitmapForExport())
                using (var thumbnail = tmpImageForThumbnail.CreateThumbnail(240, 160))
                {
                    ImageOutput.SaveToStream(thumbnail, null, thumbnailStream, outputSettings);
                    thumbnailStream.Position = 0;
                    thumbnailRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(thumbnailStream);
                    Log.Debug().WriteLine("Created RandomAccessStreamReference for the thumbnail");
                }

                // Create logo
                RandomAccessStreamReference logoRandomAccessStreamReference;
                using (var logo = BitmapWrapper.FromBitmap(GreenshotResources.Instance.GetGreenshotIcon().ToBitmap()))
                using (var logoThumbnail = logo.CreateThumbnail(30, 30))
                {
                    ImageOutput.SaveToStream(logoThumbnail, null, logoStream, outputSettings);
                    logoStream.Position = 0;
                    logoRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(logoStream);
                    Log.Info().WriteLine("Created RandomAccessStreamReference for the logo");
                }

                var dataTransferManagerHelper = new DataTransferManagerHelper(handle);
                dataTransferManagerHelper.DataTransferManager.ShareProvidersRequested += (sender, args) =>
                {
                    shareInfo.AreShareProvidersRequested = true;
                    Log.Debug().WriteLine("Share providers requested: {0}", string.Join(",", args.Providers.Select(p => p.Title)));
                };
                dataTransferManagerHelper.DataTransferManager.TargetApplicationChosen += (dtm, args) =>
                {
                    shareInfo.ApplicationName = args.ApplicationName;
                    Log.Debug().WriteLine("TargetApplicationChosen: {0}", args.ApplicationName);
                };
                var filename = FilenameHelper.GetFilename(OutputFormats.png, captureDetails);
                var storageFile = await StorageFile.CreateStreamedFileAsync(filename, async streamedFileDataRequest =>
                {
                    shareInfo.IsDeferredFileCreated = true;
                    // Information on the "how" was found here: https://socialeboladev.wordpress.com/2013/03/15/how-to-use-createstreamedfileasync/
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
                        // Signal that the action is ready, bitmap was exported
                        shareInfo.ShareTask.TrySetResult(true);
                    }
                    catch (Exception)
                    {
                        streamedFileDataRequest.FailAndClose(StreamedFileFailureMode.Incomplete);
                    }
                }, imageRandomAccessStreamReference).AsTask().ConfigureAwait(false);

                dataTransferManagerHelper.DataTransferManager.DataRequested += (dataTransferManager, dataRequestedEventArgs) =>
                {
                    var deferral = dataRequestedEventArgs.Request.GetDeferral();
                    try
                    {
                        shareInfo.IsDataRequested = true;
                        Log.Debug().WriteLine("DataRequested with operation {0}", dataRequestedEventArgs.Request.Data.RequestedOperation);
                        var dataPackage = dataRequestedEventArgs.Request.Data;
                        dataPackage.OperationCompleted += (dp, eventArgs) =>
                        {
                            Log.Debug().WriteLine("OperationCompleted: {0}, shared with", eventArgs.Operation);
                            shareInfo.CompletedWithOperation = eventArgs.Operation;
                            shareInfo.AcceptedFormat = eventArgs.AcceptedFormatId;

                            shareInfo.ShareTask.TrySetResult(true);
                        };
                        dataPackage.Destroyed += (dp, o) =>
                        {
                            shareInfo.IsDestroyed = true;
                            Log.Debug().WriteLine("Destroyed");
                            shareInfo.ShareTask.TrySetResult(true);
                        };
                        dataPackage.ShareCompleted += (dp, shareCompletedEventArgs) =>
                        {
                            shareInfo.IsShareCompleted = true;
                            Log.Debug().WriteLine("ShareCompleted");
                            shareInfo.ShareTask.TrySetResult(true);
                        };
                        dataPackage.Properties.Title = captureDetails.Title;
                        dataPackage.Properties.ApplicationName = "Greenshot";
                        dataPackage.Properties.Description = "Share a screenshot";
                        dataPackage.Properties.Thumbnail = thumbnailRandomAccessStreamReference;
                        dataPackage.Properties.Square30x30Logo = logoRandomAccessStreamReference;
                        dataPackage.Properties.LogoBackgroundColor = Color.FromArgb(0xff, 0x3d, 0x3d, 0x3d);
                        dataPackage.SetStorageItems(new[] {storageFile});
                        dataPackage.SetBitmap(imageRandomAccessStreamReference);
                    }
                    finally
                    {
                        deferral.Complete();
                        Log.Debug().WriteLine("Called deferral.Complete()");
                    }
                };
                dataTransferManagerHelper.ShowShareUi();
                Log.Debug().WriteLine("ShowShareUi finished.");
                await shareInfo.ShareTask.Task.ConfigureAwait(false);
            }
        }
    }
}
