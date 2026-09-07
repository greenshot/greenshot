/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Configuration;
using Greenshot.Destinations;
using Greenshot.Editor.Destinations;
using Greenshot.Forms;
using log4net;

namespace Greenshot.Pipeline
{
    /// <summary>
    /// Default implementation of IDestinationDispatcher managing destination execution,
    /// pre-rendered bitmap caching, background file saves, and completion notifications.
    /// </summary>
    public class DestinationDispatcher : IDestinationDispatcher
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationDispatcher));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        public async Task DispatchAsync(
            CaptureFlowContext context,
            IEnumerable<IDestination> destinations,
            CancellationToken cancellationToken = default)
        {
            var destinationList = destinations?.ToList() ?? new List<IDestination>();
            if (destinationList.Count == 0)
            {
                context.LogStep("No destinations to dispatch to.");
                Log.Warn("DestinationDispatcher: No destinations to dispatch to.");
                return;
            }

            var payload = context.Payload;
            var surface = payload.EnsureSurface();
            var captureDetails = payload.RawCapture?.CaptureDetails;

            if (surface == null || captureDetails == null)
            {
                context.LogStep("Surface or CaptureDetails is null, cannot dispatch to destinations.");
                Log.Warn("DestinationDispatcher: Surface or CaptureDetails is null, cannot dispatch to destinations.");
                return;
            }

            // Register completion notification events if enabled
            bool showNotify = context.Properties.TryGetValue("EnableCompletionNotification", out var notifObj) && notifObj is bool en
                ? en
                : CoreConfig.ShowTrayNotification && !CoreConfig.HideTrayicon;
            if (showNotify)
            {
                surface.SurfaceMessage += SurfaceMessageReceived;
            }

            var uiContext = SimpleServiceProvider.Current.GetInstance<SynchronizationContext>(isOptional: true) ?? SynchronizationContext.Current;

            // Retain surface if Editor is a target destination so context.Dispose() does not free bitmap
            if (destinationList.Any(d => EditorDestination.DESIGNATION.Equals(d.Designation, StringComparison.OrdinalIgnoreCase)))
            {
                payload.RetainSurfaceForEditor = true;
            }

            // If Destination Picker is in the list, show picker and let user pick
            if (destinationList.Any(d => nameof(WellKnownDestinations.Picker).Equals(d.Designation, StringComparison.OrdinalIgnoreCase)))
            {
                context.LogStep("Dispatching to Picker destination.");
                payload.RetainSurfaceForEditor = true;
                if (uiContext != null && SynchronizationContext.Current != uiContext)
                {
                    uiContext.Send(_ => DestinationHelper.ExportCapture(false, nameof(WellKnownDestinations.Picker), surface, captureDetails), null);
                }
                else
                {
                    DestinationHelper.ExportCapture(false, nameof(WellKnownDestinations.Picker), surface, captureDetails);
                }
                return;
            }

            bool hasFileDestination = destinationList.Exists(d =>
                d.Designation == nameof(WellKnownDestinations.FileNoDialog) ||
                d.Designation == nameof(WellKnownDestinations.FileDialog));

            var sharedFileOutputSettings = new SurfaceOutputSettings();
            if (hasFileDestination && CoreConfig.OutputFilePromptQuality)
            {
                if (uiContext != null && SynchronizationContext.Current != uiContext)
                {
                    uiContext.Send(_ =>
                    {
                        var qualityDialog = new QualityDialog(sharedFileOutputSettings);
                        qualityDialog.ShowDialog();
                    }, null);
                }
                else
                {
                    var qualityDialog = new QualityDialog(sharedFileOutputSettings);
                    qualityDialog.ShowDialog();
                }
            }

            bool hasPreRenderDestination = hasFileDestination ||
                destinationList.Exists(d => d is IAcceptsPreRenderedImage);

            Image sharedRenderedBitmap = null;
            bool disposeSharedBitmap = false;
            if (hasPreRenderDestination)
            {
                disposeSharedBitmap = ImageIO.CreateImageFromSurface(surface, sharedFileOutputSettings, out sharedRenderedBitmap);
                payload.SharedRenderedBitmap = sharedRenderedBitmap;
            }

            var backgroundTasks = new List<Task>();

            try
            {
                foreach (IDestination destination in destinationList.OrderBy(d => d.Priority).ThenBy(d => d.Description))
                {
                    if (nameof(WellKnownDestinations.Picker).Equals(destination.Designation, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    context.LogStep($"Calling destination: {destination.Description}");
                    Log.InfoFormat("Calling destination {0}", destination.Description);

                    if (destination.Designation == nameof(WellKnownDestinations.FileNoDialog) ||
                        destination.Designation == nameof(WellKnownDestinations.FileDialog))
                    {
                        string fullPath;
                        bool overwrite;
                        if (captureDetails.Filename != null)
                        {
                            fullPath = captureDetails.Filename;
                            overwrite = true;
                            sharedFileOutputSettings.Format = ImageIO.FormatForFilename(fullPath);
                        }
                        else
                        {
                            fullPath = FileDestination.CreateNewFilename(captureDetails);
                            overwrite = CoreConfig.OutputFileAllowOverwrite;
                        }

                        if (fullPath == null)
                        {
                            context.LogStep("User cancelled filename dialog, skipping file destination.");
                            continue;
                        }

                        captureDetails.Filename = fullPath;
                        var bgFullPath = fullPath;
                        var bgOverwrite = overwrite;
                        var bgOutputSettings = sharedFileOutputSettings;

                        Image bgRenderedBitmap = sharedRenderedBitmap != null ? (Image)sharedRenderedBitmap.Clone() : null;

                        var task = Task.Run(() =>
                        {
                            try
                            {
                                using (bgRenderedBitmap)
                                {
                                    ImageIO.SaveRenderedImage(
                                        bgRenderedBitmap,
                                        bgFullPath,
                                        bgOverwrite,
                                        bgOutputSettings,
                                        CoreConfig.OutputFileCopyPathToClipboard,
                                        uiContext);
                                }

                                uiContext?.Post(_ => CoreConfig.OutputFileAsFullpath = bgFullPath, null);
                            }
                            catch (ArgumentException ex1)
                            {
                                Log.InfoFormat("Not overwriting: {0}", ex1.Message);
                                uiContext?.Send(_ => ImageIO.SaveWithDialog(surface, captureDetails), null);
                            }
                            catch (Exception ex2)
                            {
                                Log.Error("Error saving screenshot in background!", ex2);
                                uiContext?.Post(_ => MessageBox.Show(
                                    Language.GetString(LangKey.error_save),
                                    Language.GetString(LangKey.error)), null);
                            }
                        }, cancellationToken);

                        backgroundTasks.Add(task);
                    }
                    else if (sharedRenderedBitmap != null && destination is IAcceptsPreRenderedImage preRenderDest)
                    {
                        if (uiContext != null && SynchronizationContext.Current != uiContext)
                        {
                            uiContext.Send(_ => preRenderDest.ExportCaptureWithRenderedImage(sharedRenderedBitmap, surface, captureDetails), null);
                        }
                        else
                        {
                            preRenderDest.ExportCaptureWithRenderedImage(sharedRenderedBitmap, surface, captureDetails);
                        }
                    }
                    else
                    {
                        if (EditorDestination.DESIGNATION.Equals(destination.Designation, StringComparison.OrdinalIgnoreCase))
                        {
                            payload.RetainSurfaceForEditor = true;
                        }

                        ExportInformation exportInformation = null;
                        if (uiContext != null && SynchronizationContext.Current != uiContext)
                        {
                            uiContext.Send(_ => exportInformation = destination.ExportCapture(false, surface, captureDetails), null);
                        }
                        else
                        {
                            exportInformation = destination.ExportCapture(false, surface, captureDetails);
                        }

                        Log.InfoFormat("Destination '{0}' export completed (ExportMade: {1}{2})",
                            destination.Designation,
                            exportInformation?.ExportMade ?? false,
                            !string.IsNullOrEmpty(exportInformation?.ErrorMessage) ? $", Error: {exportInformation.ErrorMessage}" : "");

                        if (EditorDestination.DESIGNATION.Equals(destination.Designation, StringComparison.OrdinalIgnoreCase) &&
                            exportInformation != null && exportInformation.ExportMade)
                        {
                            payload.RetainSurfaceForEditor = true;
                        }
                    }
                }

                if (backgroundTasks.Count > 0)
                {
                    await Task.WhenAll(backgroundTasks).ConfigureAwait(false);
                }
            }
            finally
            {
                if (disposeSharedBitmap)
                {
                    sharedRenderedBitmap?.Dispose();
                    payload.SharedRenderedBitmap = null;
                }
            }
        }

        public static void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs?.Message)) return;

            var notifyService = SimpleServiceProvider.Current.GetInstance<INotificationService>(isOptional: true);
            if (notifyService == null) return;

            var uiContext = SimpleServiceProvider.Current.GetInstance<SynchronizationContext>(isOptional: true) ?? SynchronizationContext.Current;
            void Notify()
            {
                switch (eventArgs.MessageType)
                {
                    case SurfaceMessageTyp.Error:
                        notifyService.ShowErrorMessage(eventArgs.Message, TimeSpan.FromHours(1));
                        break;
                    case SurfaceMessageTyp.Info:
                        notifyService.ShowInfoMessage(eventArgs.Message, TimeSpan.FromHours(1), () => Log.Info("Clicked!"));
                        break;
                    case SurfaceMessageTyp.FileSaved:
                    case SurfaceMessageTyp.UploadedUri:
                        notifyService.ShowInfoMessage(eventArgs.Message, TimeSpan.FromHours(1), () => OpenCaptureOnClick(eventArgs));
                        break;
                }
            }

            if (uiContext != null && SynchronizationContext.Current != uiContext)
            {
                uiContext.Post(_ => Notify(), null);
            }
            else
            {
                Notify();
            }
        }

        private static void OpenCaptureOnClick(SurfaceMessageEventArgs eventArgs)
        {
            ISurface surface = eventArgs.Surface;
            if (surface != null)
            {
                switch (eventArgs.MessageType)
                {
                    case SurfaceMessageTyp.FileSaved:
                        ExplorerHelper.OpenInExplorer(surface.LastSaveFullPath);
                        break;
                    case SurfaceMessageTyp.UploadedUri:
                        Process.Start(surface.UploadUrl);
                        break;
                }
            }
        }
    }
}
