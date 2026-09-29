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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using Greenshot.Base.Triggers;
using Greenshot.Pipeline;
using Xunit;

namespace Greenshot.Tests.Threading
{
    /// <summary>
    /// The flow runner: flows run on the pool, are tracked, never fault, follow the concurrency policy and stop on shutdown.
    /// </summary>
    public class CaptureFlowRunnerTests
    {
        /// <summary>
        /// Pipeline which runs a delegate as the "flow".
        /// </summary>
        private sealed class FakePipeline : ICapturePipeline
        {
            private readonly Func<CaptureFlowContext, CancellationToken, Task> _flow;

            public FakePipeline(Func<CaptureFlowContext, CancellationToken, Task> flow)
            {
                _flow = flow;
            }

            public async Task<CaptureFlowContext> ExecuteAsync(CaptureRecipe recipe, ITrigger trigger = null, Action<CaptureFlowContext> configureContext = null, CancellationToken cancellationToken = default)
            {
                var context = new CaptureFlowContext(recipe, trigger, cancellationToken);
                configureContext?.Invoke(context);
                try
                {
                    await _flow(context, cancellationToken);
                    if (!context.IsAborted)
                    {
                        context.State = CaptureFlowState.Completed;
                    }
                }
                catch (OperationCanceledException)
                {
                    context.Abort("Flow was cancelled.");
                }
                return context;
            }
        }

        private static CaptureRecipe Recipe(string id, FlowConcurrency? concurrency = null) => new CaptureRecipe(id, id) { Concurrency = concurrency };

        [Fact]
        public async Task Start_RunsFlowOnThePool_NotOnTheCaller()
        {
            using var ui = StrictTestUiDispatcher.Create();
            int flowThread = 0;
            var runner = new CaptureFlowRunner(new FakePipeline((ctx, ct) =>
            {
                flowThread = Thread.CurrentThread.ManagedThreadId;
                Assert.Same(ui, ctx.Ui);
                Assert.NotNull(ctx.TriggerContext);
                return Task.CompletedTask;
            }), ui);

            // Start from the "UI thread", like a hotkey
            var handle = await ui.InvokeAsync(() => runner.Start(Recipe("a"), FlowTriggerContext.Empty()));
            var result = await handle.Completion;

            Assert.Equal(CaptureFlowState.Completed, result.State);
            Assert.NotEqual(ui.ThreadId, flowThread);
            Assert.Equal(handle.Id, result.FlowId);
            Assert.Empty(runner.Running);
        }

        [Fact]
        public async Task Completion_NeverFaults()
        {
            var runner = new CaptureFlowRunner(new FakePipeline((ctx, ct) => throw new InvalidTimeZoneException("boom")), InlineUiDispatcher.Instance);
            var result = await runner.Start(Recipe("a"), FlowTriggerContext.Empty()).Completion;
            Assert.Equal(CaptureFlowState.Failed, result.State);
            Assert.IsType<InvalidTimeZoneException>(result.Error);
        }

        [Fact]
        public async Task Exclusive_IgnoresSecondStart()
        {
            var gate = Tcs.Create<bool>();
            int runs = 0;
            var runner = new CaptureFlowRunner(new FakePipeline(async (ctx, ct) =>
            {
                Interlocked.Increment(ref runs);
                await gate.Task;
            }), InlineUiDispatcher.Instance);

            var recipe = Recipe("exclusive", FlowConcurrency.Exclusive);
            var first = runner.Start(recipe, FlowTriggerContext.Empty());
            var second = runner.Start(recipe, FlowTriggerContext.Empty());
            Assert.Same(first, second);

            gate.SetResult(true);
            await first.Completion;
            Assert.Equal(1, runs);
        }

        [Fact]
        public async Task ReplacePrevious_CancelsRunningFlow()
        {
            var runner = new CaptureFlowRunner(new FakePipeline((ctx, ct) => Task.Delay(Timeout.Infinite, ct)), InlineUiDispatcher.Instance);
            var recipe = Recipe("replace", FlowConcurrency.ReplacePrevious);
            var first = runner.Start(recipe, FlowTriggerContext.Empty());
            var second = runner.Start(recipe, FlowTriggerContext.Empty());
            Assert.NotSame(first, second);

            var firstResult = await first.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(CaptureFlowState.Cancelled, firstResult.State);
            second.Cancel();
            await second.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Parallel_RunsBoth()
        {
            var bothRunning = new CountdownEvent(2);
            var runner = new CaptureFlowRunner(new FakePipeline(async (ctx, ct) =>
            {
                bothRunning.Signal();
                while (!bothRunning.IsSet)
                {
                    await Task.Delay(10, ct);
                }
            }), InlineUiDispatcher.Instance);

            var recipe = Recipe("parallel");
            var first = runner.Start(recipe, FlowTriggerContext.Empty());
            var second = runner.Start(recipe, FlowTriggerContext.Empty());
            var results = await Task.WhenAll(first.Completion, second.Completion).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.All(results, r => Assert.Equal(CaptureFlowState.Completed, r.State));
        }

        [Fact]
        public async Task Shutdown_CancelsRunningFlows_AndRejectsNewOnes()
        {
            var runner = new CaptureFlowRunner(new FakePipeline((ctx, ct) => Task.Delay(Timeout.Infinite, ct)), InlineUiDispatcher.Instance);
            var handles = Enumerable.Range(0, 20).Select(i => runner.Start(Recipe("spam"), FlowTriggerContext.Empty())).ToList();
            Assert.Equal(20, runner.Running.Count);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await runner.ShutdownAsync(timeout.Token);

            foreach (var handle in handles)
            {
                Assert.Equal(CaptureFlowState.Cancelled, (await handle.Completion).State);
            }
            Assert.Empty(runner.Running);

            var rejected = await runner.Start(Recipe("late"), FlowTriggerContext.Empty()).Completion;
            Assert.Equal(CaptureFlowState.Cancelled, rejected.State);
        }

        [Fact]
        public async Task OpenSelection_IsBroughtToFront_InsteadOfStartingASecond()
        {
            var selector = new FakeSelector { IsSelecting = true };
            int runs = 0;
            var runner = new CaptureFlowRunner(new FakePipeline((ctx, ct) =>
            {
                Interlocked.Increment(ref runs);
                return Task.CompletedTask;
            }), InlineUiDispatcher.Instance, selector);

            var recipe = Recipe("region");
            recipe.AddNode(new RecipeNodeConfig { Id = "select", StepType = WellKnownStepTypes.InteractiveSelection });
            var result = await runner.Start(recipe, FlowTriggerContext.Empty()).Completion;

            Assert.Equal(CaptureFlowState.Cancelled, result.State);
            Assert.Equal(1, selector.BroughtToFront);
            Assert.Equal(0, runs);
        }

        private sealed class FakeSelector : IInteractiveCaptureSelector
        {
            public bool IsSelecting { get; set; }

            public int BroughtToFront { get; private set; }

            public void BringToFront() => BroughtToFront++;

            public Task<SelectionResult> SelectAsync(Base.Interfaces.ICapture fullscreenCapture, System.Collections.Generic.IReadOnlyList<Base.Core.WindowDetails> visibleWindows, Base.Interfaces.CaptureMode initialMode, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<SelectionResult>(null);
            }
        }
    }
}
