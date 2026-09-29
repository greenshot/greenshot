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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Threading;
using Xunit;

namespace Greenshot.Tests.Threading
{
    /// <summary>
    /// The semantics of IUiDispatcher (roadmap section 4.1), which every implementation must have.
    /// </summary>
    public class UiDispatcherTests
    {
        [Fact]
        public async Task InvokeAsync_RunsOnUiThread()
        {
            using var ui = StrictTestUiDispatcher.Create();
            Assert.False(ui.CheckAccess());
            int threadId = await ui.InvokeAsync(() => Thread.CurrentThread.ManagedThreadId);
            Assert.Equal(ui.ThreadId, threadId);
        }

        [Fact]
        public async Task InvokeAsync_FromUiThread_IsPostedNotInlined()
        {
            using var ui = StrictTestUiDispatcher.Create();
            var order = await ui.InvokeAsync(async () =>
            {
                var log = new System.Collections.Generic.List<string>();
                var inner = ui.InvokeAsync(() => log.Add("inner"));
                log.Add("after-post");
                await inner;
                return string.Join(",", log);
            });
            Assert.Equal("after-post,inner", order);
        }

        [Fact]
        public async Task InvokeAsync_CancelledBeforeStart_DoesNotRun()
        {
            using var ui = StrictTestUiDispatcher.Create();
            using var cts = new CancellationTokenSource();
            var gate = Tcs.Create<bool>();
            bool ran = false;
            // Block the UI thread until the second invoke is cancelled
            var blocker = ui.InvokeAsync(async () => await gate.Task);
            var second = ui.InvokeAsync(() => ran = true, cts.Token);
            cts.Cancel();
            gate.SetResult(true);
            await blocker;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
            Assert.False(ran);
        }

        [Fact]
        public async Task InvokeAsync_PropagatesExceptions()
        {
            using var ui = StrictTestUiDispatcher.Create();
            await Assert.ThrowsAsync<InvalidTimeZoneException>(() => ui.InvokeAsync(() => throw new InvalidTimeZoneException()));
            await Assert.ThrowsAsync<InvalidTimeZoneException>(() => ui.InvokeAsync<int>(async () =>
            {
                await Task.Yield();
                throw new InvalidTimeZoneException();
            }));
        }

        [Fact]
        public async Task InvokeAsync_ContinuationDoesNotRunOnUiThread()
        {
            using var ui = StrictTestUiDispatcher.Create();
            // Without a SynchronizationContext the continuation runs where the task completes: it must not be the UI thread
            int continuationThreadId = await Task.Run(async () =>
            {
                await ui.InvokeAsync(() => { });
                return Thread.CurrentThread.ManagedThreadId;
            });
            Assert.NotEqual(ui.ThreadId, continuationThreadId);
        }

        [Fact]
        public async Task InvokeAsync_AfterShutdown_FailsFast()
        {
            using var ui = StrictTestUiDispatcher.Create();
            ui.BeginShutdown();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => ui.InvokeAsync(() => 1));
        }

        [Fact]
        public async Task InlineDispatcher_RunsOnCaller()
        {
            int threadId = await InlineUiDispatcher.Instance.InvokeAsync(() => Thread.CurrentThread.ManagedThreadId);
            Assert.Equal(Thread.CurrentThread.ManagedThreadId, threadId);
        }

        [Fact]
        public async Task Tcs_RunsContinuationsAsynchronously()
        {
            var tcs = Tcs.Create<int>();
            int completingThread = 0;
            var waiter = Task.Run(async () =>
            {
                await tcs.Task.ConfigureAwait(false);
                return Thread.CurrentThread.ManagedThreadId;
            });
            // Make sure the waiter is waiting
            await Task.Delay(50);
            var completer = new Thread(() =>
            {
                completingThread = Thread.CurrentThread.ManagedThreadId;
                tcs.SetResult(1);
                // Keep the thread alive a moment, an inline continuation would run before this returns
                Thread.Sleep(100);
            });
            completer.Start();
            int continuationThread = await waiter;
            completer.Join();
            Assert.NotEqual(completingThread, continuationThread);
        }

        [Fact]
        public async Task WithCancellation_StopsWaiting()
        {
            var never = Tcs.Create<int>();
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => never.Task.WaitAsync(cts.Token));
        }

        [Fact]
        public async Task WithTimeout_ThrowsTimeout()
        {
            var never = Tcs.Create<int>();
            await Assert.ThrowsAsync<TimeoutException>(() => never.Task.WaitAsync(TimeSpan.FromMilliseconds(50)));
        }
    }
}
