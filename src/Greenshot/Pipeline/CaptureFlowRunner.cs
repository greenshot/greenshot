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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Pipeline
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
        private readonly IInteractiveCaptureSelector _selector;
        private readonly ConcurrentDictionary<Guid, CaptureFlowHandle> _running = new ConcurrentDictionary<Guid, CaptureFlowHandle>();
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly object _startLock = new object();
        private volatile bool _stopped;

        /// <param name="pipeline">The pipeline which executes the recipes</param>
        /// <param name="ui">Dispatcher for the UI thread (notifications, bringing the selection to the front)</param>
        /// <param name="selector">The interactive selector, to bring an open selection to the front instead of starting a second one</param>
        public CaptureFlowRunner(ICapturePipeline pipeline, IUiDispatcher ui, IInteractiveCaptureSelector selector = null)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _selector = selector;
        }

        private static readonly Lazy<CaptureFlowRunner> Fallback = new Lazy<CaptureFlowRunner>(() => new CaptureFlowRunner(
            SimpleServiceProvider.Current?.GetInstance<ICapturePipeline>(isOptional: true) ?? CapturePipeline.Instance,
            SimpleServiceProvider.Current?.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// The runner registered in the service locator (by the MainForm), or a default one (tests, headless).
        /// </summary>
        public static ICaptureFlowRunner Current => SimpleServiceProvider.Current?.GetInstance<ICaptureFlowRunner>(isOptional: true) ?? Fallback.Value;

        /// <summary>
        /// The registered runner, or a runner for the supplied pipeline when none is registered (tests which inject their own pipeline).
        /// </summary>
        public static ICaptureFlowRunner For(ICapturePipeline pipeline)
        {
            var registered = SimpleServiceProvider.Current?.GetInstance<ICaptureFlowRunner>(isOptional: true);
            if (registered != null || pipeline == null)
            {
                return registered ?? Fallback.Value;
            }

            return new CaptureFlowRunner(pipeline, SimpleServiceProvider.Current?.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance);
        }

        public IReadOnlyCollection<CaptureFlowHandle> Running => _running.Values.ToList();

        public CaptureFlowHandle Start(CaptureRecipe recipe, FlowTriggerContext trigger = null, Action<CaptureFlowContext> configure = null)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            trigger ??= FlowTriggerContext.Capture();

            if (_stopped)
            {
                Log.WarnFormat("Not starting recipe '{0}', Greenshot is shutting down.", recipe.Name);
                return CaptureFlowHandle.Rejected(recipe, "Greenshot is shutting down.");
            }

            // Only one interactive selection at a time: a second start brings the open selection to the front.
            if (_selector != null && _selector.IsSelecting && recipe.FindFirstNodeByType(WellKnownStepTypes.InteractiveSelection) != null)
            {
                Log.InfoFormat("Interactive selection already open, bringing it to the front instead of starting '{0}'.", recipe.Name);
                _selector.BringToFront();
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

#pragma warning disable RS0030 // R10: the flow runner is the one place which moves a flow to the thread pool
                handle.SetCompletion(Task.Run(() => RunAsync(handle, cancellation, trigger, configure)));
#pragma warning restore RS0030
            }

            return handle;
        }

        private async Task<CaptureFlowResult> RunAsync(CaptureFlowHandle handle, CancellationTokenSource cancellation, FlowTriggerContext trigger, Action<CaptureFlowContext> configure)
        {
            ThreadAssert.NotUi("CaptureFlowRunner");
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
                FlowDiagnostics.FlowEnded(handle.Id);
                cancellation.Dispose();
            }

            if (result.State == CaptureFlowState.Failed && !_stopped)
            {
                NotifyFailure(result);
            }

            return result;
        }

        private void NotifyFailure(CaptureFlowResult result)
        {
            var notifyService = SimpleServiceProvider.Current.GetInstance<INotificationService>(isOptional: true);
            if (notifyService == null)
            {
                return;
            }

            string message = result.Reason ?? result.Error?.Message ?? "Capture flow failed.";
            _ui.InvokeAsync(() => notifyService.ShowErrorMessage(message)).FireAndLog("Notify flow failure", Log);
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
                    Log.WarnFormat("Flow {0} of recipe '{1}' didn't stop in time, running: {2}", handle.Id, handle.Recipe?.Name, FlowDiagnostics.Describe());
                }
            }
        }
    }
}
