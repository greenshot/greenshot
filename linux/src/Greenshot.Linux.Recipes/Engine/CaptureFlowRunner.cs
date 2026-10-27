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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// Default <see cref="ICaptureFlowRunner"/>: the only place which moves a flow from the UI thread to the thread pool,
    /// and the only place which turns a failed flow into a user notification.
    /// </summary>
    public sealed class CaptureFlowRunner : ICaptureFlowRunner
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CaptureFlowRunner));
        private readonly ICapturePipeline _pipeline;
        private readonly IUiDispatcher _ui;
        private readonly ConcurrentDictionary<Guid, CaptureFlowHandle> _running = new ConcurrentDictionary<Guid, CaptureFlowHandle>();
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly object _startLock = new object();
        private volatile bool _stopped;

        /// <param name="pipeline">The pipeline which executes the recipes</param>
        /// <param name="ui">Dispatcher for the UI thread (notifications, bringing the selection to the front)</param>
        /// <param name="selector">The interactive selector, to bring an open selection to the front instead of starting a second one</param>
        public CaptureFlowRunner(ICapturePipeline pipeline, IUiDispatcher ui)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        private static readonly Lazy<CaptureFlowRunner> Fallback = new Lazy<CaptureFlowRunner>(() => new CaptureFlowRunner(
            RecipeServices.Pipeline ?? CapturePipeline.Instance, RecipeServices.Ui ?? InlineUiDispatcher.Instance), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// The runner of the app, a fallback without one (tests)
        /// </summary>
        public static ICaptureFlowRunner Current => RecipeServices.Runner ?? Fallback.Value;

        public IReadOnlyCollection<CaptureFlowHandle> Running => _running.Values.ToList();

        public CaptureFlowHandle Start(CaptureRecipe recipe, FlowTriggerContext trigger = null, Action<CaptureFlowContext> configure = null)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            trigger ??= FlowTriggerContext.Empty();

            if (_stopped)
            {
                Log.WarnFormat("Not starting recipe '{0}', Greenshot is shutting down.", recipe.Name);
                return CaptureFlowHandle.Rejected(recipe, "Greenshot is shutting down.");
            }

            // Only one interactive selection at a time: a second start brings the open selection to the front.
            if (RecipeServices.Host is { IsSelecting: true } host && recipe.FindFirstNodeByType(WellKnownStepTypes.InteractiveSelection) != null)
            {
                Log.InfoFormat("Interactive selection already open, bringing it to the front instead of starting '{0}'.", recipe.Name);
                host.BringSelectionToFront();
                return CaptureFlowHandle.Rejected(recipe, "An interactive selection is already open.");
            }

            CaptureFlowHandle handle;
            lock (_startLock)
            {
                var concurrency = recipe.Concurrency ?? FlowConcurrency.Parallel;
                var runningOfRecipe = _running.Values.Where(h => string.Equals(h.Recipe?.Id, recipe.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                if (concurrency == FlowConcurrency.Exclusive && runningOfRecipe.Count > 0)
                {
                    Log.InfoFormat("Recipe '{0}' is exclusive and already running, ignoring the start.", recipe.Name);
                    return runningOfRecipe[0];
                }

                if (concurrency == FlowConcurrency.ReplacePrevious)
                {
                    foreach (var previous in runningOfRecipe)
                    {
                        Log.InfoFormat("Cancelling running flow {0} of recipe '{1}', it is replaced.", previous.Id, recipe.Name);
                        previous.Cancel();
                    }
                }

                var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
                handle = new CaptureFlowHandle(Guid.NewGuid(), recipe, cancellation);
                _running[handle.Id] = handle;

                handle.SetCompletion(Task.Run(() => RunAsync(handle, cancellation, trigger, configure)));
            }

            return handle;
        }

        private async Task<CaptureFlowResult> RunAsync(CaptureFlowHandle handle, CancellationTokenSource cancellation, FlowTriggerContext trigger, Action<CaptureFlowContext> configure)
        {
            CaptureFlowResult result;
            try
            {
                var context = await _pipeline.ExecuteAsync(handle.Recipe, trigger.Trigger, ctx =>
                {
                    ctx.ExecutionId = handle.Id;
                    ctx.TriggerContext = trigger;
                    ctx.Ui = _ui;
                    configure?.Invoke(ctx);
                }, cancellation.Token).ConfigureAwait(false);
                result = CaptureFlowResult.FromContext(context);
            }
            catch (OperationCanceledException)
            {
                result = CaptureFlowResult.Cancelled(handle.Id, "Flow was cancelled.");
            }
            catch (Exception ex)
            {
                Log.Error($"Flow {handle.Id} of recipe '{handle.Recipe?.Name}' failed", ex);
                result = CaptureFlowResult.Failed(handle.Id, ex);
            }
            finally
            {
                _running.TryRemove(handle.Id, out _);
                cancellation.Dispose();
            }

            if (result.State == CaptureFlowState.Failed && !_stopped)
            {
                NotifyFailure(result);
            }

            return result;
        }

        private static void NotifyFailure(CaptureFlowResult result)
        {
            // The notification of the exports showed the failure already (see CapturePipeline)
            if (result.Context?.Properties.TryGetValue(ExportNotification.FailureShownProperty, out var shown) == true && shown is true)
            {
                return;
            }

            string message = result.Reason ?? result.Error?.Message ?? "Capture flow failed.";
            _ = RecipeServices.Host?.NotifyExportsAsync(Array.Empty<ExportOutcome>(), message, CancellationToken.None);
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            List<CaptureFlowHandle> running;
            lock (_startLock)
            {
                _stopped = true;
                running = _running.Values.ToList();
            }

            if (running.Count == 0)
            {
                return;
            }

            Log.InfoFormat("Cancelling {0} running flow(s) for shutdown", running.Count);
            _shutdown.Cancel();
            try
            {
                await Task.WhenAll(running.Select(h => h.Completion)).WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                foreach (var handle in _running.Values)
                {
                    Log.WarnFormat("Flow {0} of recipe '{1}' didn't stop in time", handle.Id, handle.Recipe?.Name);
                }
            }
        }
    }
}
