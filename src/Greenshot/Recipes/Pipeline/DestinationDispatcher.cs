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
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using Greenshot.Destinations;
using Greenshot.Editor.Destinations;
using log4net;

namespace Greenshot.Recipes.Pipeline
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

            // Extensions on the BeforeDestination slot run for each destination, on its own copy of the capture
            var chains = context.Recipe?.DestinationChains ?? Array.Empty<ExtensionChain>();

            // If Destination Picker is in the list, let the user pick
            var picker = destinationList.FirstOrDefault(d => nameof(WellKnownDestinations.Picker).Equals(d.Designation, StringComparison.OrdinalIgnoreCase));
            if (picker != null && chains.Count > 0)
            {
                // The picked destination must be known before the extensions run, so the picker is shown here
                context.LogStep("Dispatching to Picker destination, with extensions per destination.");
                await PickAndExportAsync(context, source, captureDetails, userInteraction, showNotify, cancellationToken).ConfigureAwait(false);
                return;
            }
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

            bool skipFileDestinations = false;
            if (hasFileDestination && promptQuality && userInteraction.IsInteractive)
            {
                // Asked once for all file destinations of the flow; cancelling it means: don't save, the other destinations still run
                var promptedSettings = await userInteraction.PromptOutputSettingsAsync(sharedFileOutputSettings, cancellationToken).ConfigureAwait(false);
                skipFileDestinations = promptedSettings == null;
                sharedFileOutputSettings = promptedSettings ?? sharedFileOutputSettings;
            }

            var failedExports = new List<(string Designation, string Error, Exception Exception)>();
            var copies = new Dictionary<string, PreparedCopy>();
            foreach (var destination in destinationList.OrderBy(d => d, DestinationComparer.Instance))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destinationToUse = destination;
                if (skipFileDestinations && (destination.Designation == nameof(WellKnownDestinations.FileNoDialog) || destination.Designation == nameof(WellKnownDestinations.FileDialog)))
                {
                    context.LogStep($"The quality dialog was cancelled, not saving to {destination.Descriptor?.DisplayName}");
                    continue;
                }

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
                var result = await ExportWithChainsAsync(context, destinationToUse, destination.Designation, source, captureDetails, userInteraction, copies, showNotify, false, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Shows the destination picker until an export succeeds or the user closes it; the extensions run for the picked destination
        /// </summary>
        private static async Task PickAndExportAsync(CaptureFlowContext context, IExportSource source, ICaptureDetails captureDetails, IUserInteraction userInteraction,
            bool showNotify, CancellationToken cancellationToken)
        {
            var copies = new Dictionary<string, PreparedCopy>();
            var choices = DestinationHelper.GetAllDestinations()
                .Where(d => !nameof(WellKnownDestinations.Picker).Equals(d.Designation, StringComparison.OrdinalIgnoreCase) && d.IsAvailableFor(captureDetails))
                .ToList();
            while (true)
            {
                var picked = await userInteraction.PickDestinationAsync(choices, captureDetails, cancellationToken).ConfigureAwait(false);
                if (picked == null)
                {
                    context.LogStep("The destination picker was closed.");
                    return;
                }

                var result = await ExportWithChainsAsync(context, picked, picked.Designation, source, captureDetails, userInteraction, copies, showNotify, true, cancellationToken).ConfigureAwait(false);
                if (result.IsSucceeded)
                {
                    return;
                }
                // Export cancelled or failed: the problem was shown, the picker comes again
            }
        }

        /// <summary>
        /// A copy of the capture changed by the extensions of a destination (BeforeDestination), shared by the destinations
        /// with the same extensions
        /// </summary>
        private sealed class PreparedCopy
        {
            public CaptureFlowContext Context;
            public IExportSource Source;
            public ICaptureDetails CaptureDetails;
        }

        /// <summary>
        /// The extensions (BeforeDestination) which run for the destination, in their order
        /// </summary>
        public static IReadOnlyList<ExtensionChain> ChainsFor(CaptureFlowContext context, string designation) =>
            context?.Recipe?.DestinationChains?.Where(c => c.RunsFor(designation)).ToList() ?? new List<ExtensionChain>();

        /// <summary>
        /// Runs the extensions on a copy of the capture. Returns the context of the copy (disposed with the flow, unless a
        /// destination keeps it), or null when an extension failed.
        /// </summary>
        public static async Task<CaptureFlowContext> RunChainsOnCopyAsync(CaptureFlowContext context, IReadOnlyList<ExtensionChain> chains, string forWhat, CancellationToken cancellationToken)
        {
            var copyContext = context.CreateBranchContext();
            var engine = new DagExecutionEngine(StepRegistry.Instance.CreateStep, StepRegistry.Instance.GetContract);
            foreach (var chain in chains)
            {
                copyContext.LogStep($"Running extension '{chain.Extension.Name ?? chain.Extension.Id}' for {forWhat}.");
                await engine.ExecuteAsync(chain.Flow, copyContext, cancellationToken).ConfigureAwait(false);
                if (copyContext.IsAborted)
                {
                    Log.WarnFormat("Extension '{0}' failed for {1} ({2}), the capture is used without it.", chain.Extension.Id, forWhat, copyContext.AbortReason);
                    return null;
                }
            }
            return copyContext;
        }

        /// <summary>
        /// Exports to the destination: when extensions run for it (BeforeDestination), they change a copy of the capture,
        /// which is exported instead, so the other destinations get the capture without them. Destinations with the same
        /// extensions share one copy (<paramref name="copies"/>), so the extensions run once per combination.
        /// </summary>
        private static async Task<ExportResult> ExportWithChainsAsync(CaptureFlowContext context, IDestination destination, string designation, IExportSource source, ICaptureDetails captureDetails,
            IUserInteraction userInteraction, IDictionary<string, PreparedCopy> copies, bool showNotify, bool manuallyInitiated, CancellationToken cancellationToken)
        {
            var applicable = ChainsFor(context, designation);
            if (applicable.Count == 0)
            {
                return await ExportAsync(context, destination, source, captureDetails, userInteraction, cancellationToken, manuallyInitiated).ConfigureAwait(false);
            }

            string key = string.Join("|", applicable.Select(c => c.Extension.Id));
            if (!copies.TryGetValue(key, out var prepared))
            {
                prepared = null;
                var copyContext = await RunChainsOnCopyAsync(context, applicable, $"destination '{designation}'", cancellationToken).ConfigureAwait(false);
                if (copyContext != null)
                {
                    var copySurface = copyContext.Payload?.EnsureSurface();
                    if (showNotify && copySurface != null)
                    {
                        copySurface.SurfaceMessage -= SurfaceMessageReceived;
                        copySurface.SurfaceMessage += SurfaceMessageReceived;
                    }
                    prepared = new PreparedCopy
                    {
                        Context = copyContext,
                        Source = await copyContext.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false),
                        CaptureDetails = copyContext.Payload.RawCapture?.CaptureDetails ?? copySurface?.CaptureDetails ?? captureDetails
                    };
                }
                // A failed extension isn't tried again for the next destination with the same extensions
                copies[key] = prepared;
            }

            if (prepared == null)
            {
                return await ExportAsync(context, destination, source, captureDetails, userInteraction, cancellationToken, manuallyInitiated).ConfigureAwait(false);
            }
            return await ExportAsync(prepared.Context, destination, prepared.Source, prepared.CaptureDetails, userInteraction, cancellationToken, manuallyInitiated).ConfigureAwait(false);
        }
        private static async Task<ExportResult> ExportAsync(CaptureFlowContext context, IDestination destination, IExportSource source, ICaptureDetails captureDetails, IUserInteraction userInteraction,
            CancellationToken cancellationToken, bool manuallyInitiated = false)
        {
            var result = await DestinationExporter.ExportAsync(destination, source, captureDetails, manuallyInitiated, userInteraction, cancellationToken).ConfigureAwait(false);
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
