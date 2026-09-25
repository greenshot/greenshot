/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

        internal static void InvokeOnSta(SynchronizationContext uiContext, Action action)
        {
            if (action == null) return;

            if (uiContext != null && SynchronizationContext.Current != uiContext)
            {
                uiContext.Send(_ => action(), null);
            }
            else if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(action);
            }
            else if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                Exception threadEx = null;
                var staThread = new Thread(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        threadEx = ex;
                    }
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
                if (threadEx != null)
                {
                    throw threadEx;
                }
            }
            else
            {
                action();
            }
        }

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
                surface.SurfaceMessage -= SurfaceMessageReceived;
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

            bool promptQuality = context.Properties.TryGetValue("Destination.PromptQuality", out var pqObj) && pqObj is bool pq
                ? pq
                : CoreConfig.OutputFilePromptQuality;

            var sharedFileOutputSettings = context.Properties.TryGetValue("Destination.SurfaceOutputSettings", out var sosObj) && sosObj is SurfaceOutputSettings customSos
                ? customSos
                : new SurfaceOutputSettings();

            if (hasFileDestination && promptQuality)
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
            int successfulExports = 0;
            var failedExports = new List<(string Designation, string Error, Exception Exception)>();

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

                    if (destination.Designation == nameof(WellKnownDestinations.FileNoDialog))
                    {
                        string fullPath;
                        bool overwrite;
                        string originalFilename = captureDetails.Filename;
                        if (captureDetails.Filename != null)
                        {
                            fullPath = captureDetails.Filename;
                            overwrite = context.Properties.TryGetValue("Destination.AllowOverwrite", out var aoVal) && aoVal is bool ao ? ao : true;
                            sharedFileOutputSettings.Format = ImageIO.FormatForFilename(fullPath);
                        }
                        else
                        {
                            fullPath = FileDestination.CreateNewFilename(captureDetails);
                            overwrite = context.Properties.TryGetValue("Destination.AllowOverwrite", out var aoVal) && aoVal is bool ao ? ao : CoreConfig.OutputFileAllowOverwrite;
                        }

                        if (fullPath == null)
                        {
                            context.LogStep("User cancelled filename dialog, skipping file destination.");
                            continue;
                        }

                        var bgFullPath = fullPath;
                        var bgOverwrite = overwrite;
                        var bgOutputSettings = sharedFileOutputSettings;

                        bool copyPath = context.Properties.TryGetValue("Destination.CopyPathToClipboard", out var cpVal) && cpVal is bool cp
                            ? cp
                            : CoreConfig.OutputFileCopyPathToClipboard;

                        Image bgRenderedBitmap = sharedRenderedBitmap != null
                            ? (Image)sharedRenderedBitmap.Clone()
                            : surface?.GetImageForExport();

                        var destDesignation = destination.Designation;
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
                                        copyPath,
                                        uiContext);
                                }

                                captureDetails.Filename = bgFullPath;
                                uiContext?.Post(_ => CoreConfig.OutputFileAsFullpath = bgFullPath, null);
                                Interlocked.Increment(ref successfulExports);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error saving screenshot in background to '{bgFullPath}'!", ex);
                                captureDetails.Filename = originalFilename;
                                payload.RetainSurfaceForEditor = true;
                                lock (failedExports)
                                {
                                    failedExports.Add((destDesignation, ex.Message, ex));
                                }
                            }
                        }, cancellationToken);

                        backgroundTasks.Add(task);
                    }
                    else if (sharedRenderedBitmap != null && destination is IAcceptsPreRenderedImage preRenderDest)
                    {
                        ExportInformation exportInformation = null;
                        Exception destEx = null;
                        InvokeOnSta(uiContext, () =>
                        {
                            try
                            {
                                exportInformation = preRenderDest.ExportCaptureWithRenderedImage(sharedRenderedBitmap, surface, captureDetails);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error exporting to {destination.Designation}", ex);
                                destEx = ex;
                                exportInformation = new ExportInformation(destination.Designation, destination.Description)
                                {
                                    ExportMade = false,
                                    ErrorMessage = ex.Message
                                };
                            }
                        });

                        if (exportInformation != null && exportInformation.ExportMade)
                        {
                            successfulExports++;
                        }
                        else if (exportInformation != null && !exportInformation.ExportMade && !string.IsNullOrEmpty(exportInformation.ErrorMessage))
                        {
                            payload.RetainSurfaceForEditor = true;
                            failedExports.Add((destination.Designation, exportInformation.ErrorMessage, destEx));
                        }
                    }
                    else
                    {
                        if (EditorDestination.DESIGNATION.Equals(destination.Designation, StringComparison.OrdinalIgnoreCase))
                        {
                            payload.RetainSurfaceForEditor = true;
                        }

                        ExportInformation exportInformation = null;
                        Exception destEx = null;
                        InvokeOnSta(uiContext, () =>
                        {
                            try
                            {
                                exportInformation = destination.ExportCapture(false, surface, captureDetails);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error exporting to {destination.Designation}", ex);
                                destEx = ex;
                                exportInformation = new ExportInformation(destination.Designation, destination.Description)
                                {
                                    ExportMade = false,
                                    ErrorMessage = ex.Message
                                };
                            }
                        });

                        Log.InfoFormat("Destination '{0}' export completed (ExportMade: {1}{2})",
                            destination.Designation,
                            exportInformation?.ExportMade ?? false,
                            !string.IsNullOrEmpty(exportInformation?.ErrorMessage) ? $", Error: {exportInformation.ErrorMessage}" : "");

                        if (exportInformation != null && exportInformation.ExportMade)
                        {
                            successfulExports++;
                        }
                        else if (exportInformation != null && !exportInformation.ExportMade && !string.IsNullOrEmpty(exportInformation.ErrorMessage))
                        {
                            payload.RetainSurfaceForEditor = true;
                            failedExports.Add((destination.Designation, exportInformation.ErrorMessage, destEx));
                        }

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

                if (failedExports.Count > 0)
                {
                    context.Properties["DestinationExportErrors"] = string.Join("; ", failedExports.Select(f => $"{f.Designation}: {f.Error}"));
                    var first = failedExports[0];
                    throw new DestinationExportException($"Export to {first.Designation} failed: {first.Error}", first.Designation, first.Exception);
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
                        notifyService.ShowErrorMessage(eventArgs.Message, TimeSpan.FromHours(1), () =>
                        {
                            if (eventArgs.Surface != null)
                            {
                                DestinationHelper.ExportCapture(false, EditorDestination.DESIGNATION, eventArgs.Surface, eventArgs.Surface.CaptureDetails);
                            }
                        });
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
