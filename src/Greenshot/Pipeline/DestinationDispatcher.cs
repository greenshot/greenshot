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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Destinations;
using Greenshot.Editor.Destinations;
using log4net;

namespace Greenshot.Pipeline
{
    /// <summary>
    /// Default implementation of IDestinationDispatcher: exports the capture of a flow to its destinations, on the thread pool.
    /// The destinations share one export source (renders and encodings are cached) and run sequentially in recipe order,
    /// a later one may depend on an earlier one's result (e.g. "upload, then copy the link").
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
            var destinationList = destinations?.Where(d => d != null).ToList() ?? new List<IDestination>();
            if (destinationList.Count == 0)
            {
                context.LogStep("No destinations to dispatch to.");
                Log.Warn("DestinationDispatcher: No destinations to dispatch to.");
                return;
            }

            var payload = context.Payload;
            var surface = payload?.EnsureSurface();
            var captureDetails = payload?.RawCapture?.CaptureDetails ?? surface?.CaptureDetails;
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

            var userInteraction = context.UserInteraction;
            var source = await payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);

            // If Destination Picker is in the list, let the user pick
            var picker = destinationList.FirstOrDefault(d => nameof(WellKnownDestinations.Picker).Equals(d.Designation, StringComparison.OrdinalIgnoreCase));
            if (picker != null)
            {
                context.LogStep("Dispatching to Picker destination.");
                var pickerResult = await ExportAsync(context, picker, source, captureDetails, userInteraction, cancellationToken).ConfigureAwait(false);
                if (pickerResult.Status == ExportStatus.Failed)
                {
                    throw new DestinationExportException($"Export to {picker.Designation} failed: {pickerResult.Error}", picker.Designation, pickerResult.Exception);
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

            if (hasFileDestination && promptQuality && userInteraction.IsInteractive)
            {
                // Asked once for all file destinations of the flow
                sharedFileOutputSettings = await userInteraction.PromptOutputSettingsAsync(sharedFileOutputSettings, cancellationToken).ConfigureAwait(false) ?? sharedFileOutputSettings;
            }

            var failedExports = new List<(string Designation, string Error, Exception Exception)>();
            foreach (var destination in destinationList.OrderBy(d => d, DestinationComparer.Instance))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destinationToUse = destination;
                if (destination.Designation == nameof(WellKnownDestinations.FileNoDialog))
                {
                    // The flow can override the options of the file export
                    destinationToUse = new FileDestination(new FileDestinationOptions
                    {
                        AllowOverwrite = context.Properties.TryGetValue("Destination.AllowOverwrite", out var aoVal) && aoVal is bool ao ? ao : (bool?)null,
                        CopyPathToClipboard = context.Properties.TryGetValue("Destination.CopyPathToClipboard", out var cpVal) && cpVal is bool cp ? cp : (bool?)null,
                        OutputSettings = sharedFileOutputSettings
                    });
                }

                context.LogStep($"Calling destination: {destination.Descriptor?.DisplayName}");
                Log.InfoFormat("Calling destination {0}", destination.Designation);
                var result = await ExportAsync(context, destinationToUse, source, captureDetails, userInteraction, cancellationToken).ConfigureAwait(false);
                if (result.Status == ExportStatus.Failed)
                {
                    // Keep the capture, so the user can open it in the editor (notification click)
                    payload.RetainSurfaceForEditor = true;
                    failedExports.Add((destination.Designation, result.Error ?? "unknown error", result.Exception));
                }
            }

            if (failedExports.Count > 0)
            {
                context.Properties["DestinationExportErrors"] = string.Join("; ", failedExports.Select(f => $"{f.Designation}: {f.Error}"));
                var first = failedExports[0];
                throw new DestinationExportException($"Export to {first.Designation} failed: {first.Error}", first.Designation, first.Exception);
            }
        }

        private static async Task<ExportResult> ExportAsync(CaptureFlowContext context, IDestination destination, IExportSource source, ICaptureDetails captureDetails, IUserInteraction userInteraction,
            CancellationToken cancellationToken)
        {
            var result = await DestinationExporter.ExportAsync(destination, source, captureDetails, false, userInteraction, cancellationToken).ConfigureAwait(false);
            if (result.KeepsCapture)
            {
                // The editor shows the surface now: the flow must not dispose it
                context.Payload.RetainSurfaceForEditor = true;
            }

            await ExportResultHandler.ApplyAsync(destination, result, source, cancellationToken).ConfigureAwait(false);
            Log.InfoFormat("Destination '{0}' export completed: {1}", destination.Designation, result);
            return result;
        }

        /// <summary>
        /// Turns surface messages into notifications, raised on the UI thread (see ExportResultHandler).
        /// </summary>
        public static void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs?.Message)) return;

            Notification notification;
            switch (eventArgs.MessageType)
            {
                case SurfaceMessageTyp.Error:
                    notification = new Notification(NotificationKind.Error, eventArgs.Message, TimeSpan.FromHours(1), () =>
                    {
                        if (eventArgs.Surface != null)
                        {
                            DestinationHelper.StartExport(EditorDestination.DESIGNATION, eventArgs.Surface, false);
                        }
                    });
                    break;
                case SurfaceMessageTyp.Info:
                    notification = new Notification(NotificationKind.Info, eventArgs.Message, TimeSpan.FromHours(1), () => Log.Info("Clicked!"));
                    break;
                case SurfaceMessageTyp.FileSaved:
                case SurfaceMessageTyp.UploadedUri:
                    notification = new Notification(NotificationKind.Info, eventArgs.Message, TimeSpan.FromHours(1), () => OpenCaptureOnClick(eventArgs));
                    break;
                default:
                    return;
            }

            UserInteraction.Current.NotifyAsync(notification).FireAndLog("Export notification", Log);
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
