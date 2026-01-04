/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (c) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Win10.Internal;
using Greenshot.Plugin.Win10.Native;
using Color = Windows.UI.Color;

namespace Greenshot.Plugin.Win10.Destinations
{
    /// <summary>
    /// This uses the Share from Windows 10 to make the capture available to apps.
    /// </summary>
    public class Win10ShareDestination : AbstractDestination
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10ShareDestination));

        public override string Designation { get; } = "Windows10Share";
        public override string Description { get; } = "Windows 10 share";

        /// <summary>
        /// Icon for the App-share, the icon was found via: https://help4windows.com/windows_8_shell32_dll.shtml
        /// </summary>
        public override Image DisplayIcon => PluginUtils.GetCachedExeIcon(FilenameHelper.FillCmdVariables(@"%windir%\system32\shell32.dll"), 238);

        /// <summary>
        /// Share the screenshot with a windows app
        /// </summary>
        /// <param name="manuallyInitiated">bool</param>
        /// <param name="surface">ISurface</param>
        /// <param name="captureDetails">ICaptureDetails</param>
        /// <returns>ExportInformation</returns>
        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            var exportInformation = new ExportInformation(Designation, Description);
            try
            {
                var triggerWindow = new Window
                {
                    WindowState = WindowState.Normal,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowStyle = WindowStyle.None,
                    // TODO: Define right size
                    Width = 400,
                    Height = 400,
                    AllowsTransparency = true,
                    Background = new SolidColorBrush(Colors.Transparent)
                };
                var shareInfo = new ShareInfo();

                triggerWindow.Show();

                var windowHandle = new WindowInteropHelper(triggerWindow).Handle;

                // This is a bad trick, but don't know how else to do it.
                // Wait for the focus to return, and depending on the state close the window!
                var createDestroyMonitor = WinEventHook.WindowTitleChangeObservable().Subscribe(wei =>
                {
                    if (wei.WinEvent == WinEvents.EVENT_OBJECT_CREATE)
                    {
                        var windowTitle = User32Api.GetText(wei.Handle);
                        if ("Windows Shell Experience Host" == windowTitle)
                        {
                            shareInfo.SharingHwnd = wei.Handle;
                        }
                        return;
                    }
                    if (wei.Handle == shareInfo.SharingHwnd)
                    {
                        if (shareInfo.ApplicationName != null)
                        {
                            return;
                        }

                        shareInfo.ShareTask.TrySetResult(false);
                    }
                });

                Share(shareInfo, windowHandle, surface, captureDetails).GetAwaiter().GetResult();
                Log.Debug("Sharing finished, closing window.");
                triggerWindow.Close();
                createDestroyMonitor.Dispose();
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

            ProcessExport(exportInformation, surface);
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
            using var imageStream = new MemoryRandomAccessStream();
            using var logoStream = new MemoryRandomAccessStream();
            using var thumbnailStream = new MemoryRandomAccessStream();
            var outputSettings = new SurfaceOutputSettings();
            outputSettings.PreventGreenshotFormat();

            // Create capture for export
            ImageIO.SaveToStream(surface, imageStream, outputSettings);
            imageStream.Position = 0;
            Log.Debug("Created RandomAccessStreamReference for the image");
            var imageRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(imageStream);

            // Create thumbnail
            RandomAccessStreamReference thumbnailRandomAccessStreamReference;
            using (var tmpImageForThumbnail = surface.GetImageForExport())
            using (var thumbnail = ImageHelper.CreateThumbnail(tmpImageForThumbnail, 240, 160))
            {
                ImageIO.SaveToStream(thumbnail, null, thumbnailStream, outputSettings);
                thumbnailStream.Position = 0;
                thumbnailRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(thumbnailStream);
                Log.Debug("Created RandomAccessStreamReference for the thumbnail");
            }

            // Create logo
            RandomAccessStreamReference logoRandomAccessStreamReference;
            using (var logo = GreenshotResources.GetGreenshotIcon().ToBitmap())
            using (var logoThumbnail = ImageHelper.CreateThumbnail(logo, 30, 30))
            {
                ImageIO.SaveToStream(logoThumbnail, null, logoStream, outputSettings);
                logoStream.Position = 0;
                logoRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(logoStream);
                Log.Info("Created RandomAccessStreamReference for the logo");
            }

            var dataTransferManagerHelper = new DataTransferManagerHelper(handle);
            dataTransferManagerHelper.DataTransferManager.ShareProvidersRequested += (sender, args) =>
            {
                shareInfo.AreShareProvidersRequested = true;
                Log.DebugFormat("Share providers requested: {0}", string.Join(",", args.Providers.Select(p => p.Title)));
            };
            dataTransferManagerHelper.DataTransferManager.TargetApplicationChosen += (dtm, args) =>
            {
                shareInfo.ApplicationName = args.ApplicationName;
                Log.DebugFormat("TargetApplicationChosen: {0}", args.ApplicationName);
            };
            var filename = FilenameHelper.GetFilename(OutputFormat.png, captureDetails);
            var storageFile = await StorageFile.CreateStreamedFileAsync(filename, async streamedFileDataRequest =>
            {
                shareInfo.IsDeferredFileCreated = true;
                // Information on the "how" was found here: https://socialeboladev.wordpress.com/2013/03/15/how-to-use-createstreamedfileasync/
                Log.DebugFormat("Creating deferred file {0}", filename);
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
                    Log.DebugFormat("DataRequested with operation {0}", dataRequestedEventArgs.Request.Data.RequestedOperation);
                    var dataPackage = dataRequestedEventArgs.Request.Data;
                    dataPackage.OperationCompleted += (dp, eventArgs) =>
                    {
                        Log.DebugFormat("OperationCompleted: {0}, shared with", eventArgs.Operation);
                        shareInfo.CompletedWithOperation = eventArgs.Operation;
                        shareInfo.AcceptedFormat = eventArgs.AcceptedFormatId;

                        shareInfo.ShareTask.TrySetResult(true);
                    };
                    dataPackage.Destroyed += (dp, o) =>
                    {
                        shareInfo.IsDestroyed = true;
                        Log.Debug("Destroyed");
                        shareInfo.ShareTask.TrySetResult(true);
                    };
                    dataPackage.ShareCompleted += (dp, shareCompletedEventArgs) =>
                    {
                        shareInfo.IsShareCompleted = true;
                        Log.Debug("ShareCompleted");
                        shareInfo.ShareTask.TrySetResult(true);
                    };
                    dataPackage.Properties.Title = captureDetails.Title;
                    dataPackage.Properties.ApplicationName = "Greenshot";
                    dataPackage.Properties.Description = "Share a screenshot";
                    dataPackage.Properties.Thumbnail = thumbnailRandomAccessStreamReference;
                    dataPackage.Properties.Square30x30Logo = logoRandomAccessStreamReference;
                    dataPackage.Properties.LogoBackgroundColor = Color.FromArgb(0xff, 0x3d, 0x3d, 0x3d);
                    dataPackage.SetStorageItems(new[]
                    {
                        storageFile
                    });
                    dataPackage.SetBitmap(imageRandomAccessStreamReference);
                }
                finally
                {
                    deferral.Complete();
                    Log.Debug("Called deferral.Complete()");
                }
            };
            dataTransferManagerHelper.ShowShareUi();
            Log.Debug("ShowShareUi finished.");
            await shareInfo.ShareTask.Task.ConfigureAwait(false);
        }
    }
}