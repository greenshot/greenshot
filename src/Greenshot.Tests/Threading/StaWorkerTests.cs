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
    public class StaWorkerTests
    {
        [Fact]
        public async Task RunAsync_RunsOnStaThread()
        {
            await using var worker = new StaWorker("Test");
            var state = await worker.RunAsync(() => Thread.CurrentThread.GetApartmentState());
            Assert.Equal(ApartmentState.STA, state);
        }

        [Fact]
        public async Task RunAsync_UsesOneThread()
        {
            await using var worker = new StaWorker("Test");
            int first = await worker.RunAsync(() => Thread.CurrentThread.ManagedThreadId);
            int second = await worker.RunAsync(() => Thread.CurrentThread.ManagedThreadId);
            Assert.Equal(first, second);
            Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, first);
        }

        [Fact]
        public async Task RunAsync_PropagatesExceptions()
        {
            await using var worker = new StaWorker("Test");
            await Assert.ThrowsAsync<InvalidTimeZoneException>(() => worker.RunAsync(() => throw new InvalidTimeZoneException()));
        }

        [Fact]
        public async Task HangingCall_DoesNotBlockOtherWorker_AndIsReplaced()
        {
            await using var factory = new StaWorkerFactory();
            var hanging = factory.Get("Hanging");
            using var release = new ManualResetEventSlim();
            using var cts = new CancellationTokenSource();
            var hangingCall = hanging.RunAsync(() => release.Wait(TimeSpan.FromSeconds(10)), cts.Token);

            // Another worker still works
            var other = factory.Get("Other");
            Assert.Equal(42, await other.RunAsync(() => 42).WaitAsync(TimeSpan.FromSeconds(5)));

            // Stop waiting for the hanging call
            await Task.Delay(100);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => hangingCall.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.False(hanging.IsHealthy);

            // The factory creates a new worker for the name
            var replacement = factory.Get("Hanging");
            Assert.NotSame(hanging, replacement);
            Assert.Equal(1, await replacement.RunAsync(() => 1).WaitAsync(TimeSpan.FromSeconds(5)));
            release.Set();
        }

        [Fact]
        public async Task RunAsync_CancelledBeforeStart_DoesNotRun()
        {
            await using var worker = new StaWorker("Test");
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            bool ran = false;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.RunAsync(() => ran = true, cts.Token));
            Assert.False(ran);
            Assert.True(worker.IsHealthy);
        }
    }
}
