/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

#nullable disable

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Recipes.Steps;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// Exports the capture of a flow to destinations: the picker lets the user choose, the extensions of the BeforeDestination
    /// slot run once per combination on a copy, every export goes into the export log of the flow
    /// </summary>
    public class DestinationDispatcher : IDestinationDispatcher
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationDispatcher));

        public async Task DispatchAsync(CaptureFlowContext context, IEnumerable<IDestination> destinations, CancellationToken cancellationToken = default)
        {
            var destinationList = destinations?.Where(d => d != null).ToList() ?? new List<IDestination>();
            if (destinationList.Count == 0)
            {
                context.LogStep("No destinations to dispatch to.");
                Log.Warn("DestinationDispatcher: No destinations to dispatch to.");
                return;
            }

            if (context.Payload?.RawCapture == null)
            {
                context.LogStep("There is no capture, cannot dispatch to destinations.");
                return;
            }

            var source = await context.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
            var picker = destinationList.FirstOrDefault(d => WellKnownDestinations.Picker.Equals(d.Designation, StringComparison.OrdinalIgnoreCase));
            if (picker != null)
            {
                context.LogStep("Dispatching to Picker destination.");
                await PickAndExportAsync(context, source, cancellationToken).ConfigureAwait(false);
                return;
            }

            var failed = new List<(string Designation, string Error, Exception Exception)>();
            var copies = new Dictionary<string, CaptureFlowContext>();
            foreach (var destination in destinationList.OrderBy(d => d.Priority))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destinationToUse = destination;
                if (destination.Designation == WellKnownDestinations.FileDefault && RecipeFileDestination.HasOverrides(context))
                {
                    // The step's SaveDirectory, Format, AllowOverwrite, ... for this flow
                    destinationToUse = new RecipeFileDestination(context);
                }

                context.LogStep($"Calling destination: {destination.DisplayName}");
                Log.InfoFormat("Calling destination {0}", destination.Designation);
                var result = await ExportWithChainsAsync(context, destinationToUse, destination.Designation, source, copies, cancellationToken).ConfigureAwait(false);
                if (!result.Success && !result.Cancelled)
                {
                    // Keep the capture, so the user can open it in the editor
                    context.Payload.RetainSurfaceForEditor = true;
                    failed.Add((destination.Designation, result.Message ?? "unknown error", new IOException(result.Message ?? "unknown error")));
                }
            }

            if (failed.Count > 0)
            {
                context.Properties["DestinationExportErrors"] = string.Join("; ", failed.Select(f => $"{f.Designation}: {f.Error}"));
                throw new DestinationExportException($"Export to {failed[0].Designation} failed: {failed[0].Error}", failed[0].Designation, failed[0].Exception);
            }
        }

        private static async Task PickAndExportAsync(CaptureFlowContext context, Capture source, CancellationToken cancellationToken)
        {
            var host = RecipeServices.Host;
            var choices = (host?.Destinations ?? Array.Empty<IDestination>())
                .Where(d => !WellKnownDestinations.Picker.Equals(d.Designation, StringComparison.OrdinalIgnoreCase) && d.IsAvailable)
                .ToList();
            var copies = new Dictionary<string, CaptureFlowContext>();
            while (true)
            {
                var picked = await context.UserInteraction.PickDestinationAsync(choices, source, cancellationToken).ConfigureAwait(false);
                if (picked == null)
                {
                    context.LogStep("The destination picker was closed.");
                    return;
                }

                var result = await ExportWithChainsAsync(context, picked, picked.Designation, source, copies, cancellationToken).ConfigureAwait(false);
                if (result.Success)
                {
                    return;
                }
                // Canceled or failed: the problem was shown, the picker comes again
            }
        }

        public static IReadOnlyList<ExtensionChain> ChainsFor(CaptureFlowContext context, string designation) =>
            context?.Recipe?.DestinationChains?.Where(c => c.RunsFor(designation)).ToList() ?? new List<ExtensionChain>();

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

        private static async Task<ExportResult> ExportWithChainsAsync(CaptureFlowContext context, IDestination destination, string designation, Capture source,
            IDictionary<string, CaptureFlowContext> copies, CancellationToken cancellationToken)
        {
            var applicable = ChainsFor(context, designation);
            if (applicable.Count == 0)
            {
                return await ExportAsync(context, destination, source, cancellationToken).ConfigureAwait(false);
            }

            string key = string.Join("|", applicable.Select(c => c.Extension.Id));
            if (!copies.TryGetValue(key, out var copyContext))
            {
                // A failed extension isn't tried again for the next destination with the same extensions
                copyContext = await RunChainsOnCopyAsync(context, applicable, $"destination '{designation}'", cancellationToken).ConfigureAwait(false);
                copies[key] = copyContext;
            }

            if (copyContext?.Payload == null)
            {
                return await ExportAsync(context, destination, source, cancellationToken).ConfigureAwait(false);
            }

            var copySource = await copyContext.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
            return await ExportAsync(copyContext, destination, copySource, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ExportResult> ExportAsync(CaptureFlowContext context, IDestination destination, Capture source, CancellationToken cancellationToken)
        {
            ExportResult result;
            try
            {
                if (destination.Designation == WellKnownDestinations.Editor && RecipeServices.Host != null)
                {
                    // The editor gets the annotations as elements, not drawn into the image
                    var current = context.Properties.TryGetValue("EditorForm", out var editor) ? editor as IImageEditor : null;
                    result = await RecipeServices.Host.OpenInEditorAsync(context.Payload, current, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    result = await destination.ExportAsync(source, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error($"Destination '{destination.Designation}' failed", ex);
                result = ExportResult.Failed(ex.Message);
            }

            // The context of a copy with extensions shares the export log of the flow
            context.RecordExport(destination, result, source.Details, source);
            Log.InfoFormat("Destination '{0}' export completed: {1}", destination.Designation, result);
            return result;
        }
    }
}
