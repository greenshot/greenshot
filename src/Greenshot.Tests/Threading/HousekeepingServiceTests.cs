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

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Threading;
using Xunit;

namespace Greenshot.Tests.Threading
{
    public class HousekeepingServiceTests
    {
        // Only a safety net against a hanging test, the tests wait for the work itself
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        private static async Task<T> WithTimeout<T>(Task<T> task)
        {
            Assert.Same(task, await Task.WhenAny(task, Task.Delay(Timeout, TestContext.Current.CancellationToken)));
            return await task;
        }

        private static Func<CancellationToken, Task> Record(ConcurrentQueue<string> order, string name, TaskCompletionSource<bool> done = null)
        {
            return _ =>
            {
                order.Enqueue(name);
                done?.TrySetResult(true);
                return Task.CompletedTask;
            };
        }

        [Fact]
        public async Task Enqueue_BeforeStart_RunsAfterStartInOrder()
        {
            using var housekeeping = new HousekeepingService();
            var order = new ConcurrentQueue<string>();
            bool started = false;
            var done = Tcs.Create<bool>();
            housekeeping.Enqueue("first", _ =>
            {
                order.Enqueue(Volatile.Read(ref started) ? "first" : "first before the start");
                return Task.CompletedTask;
            });
            housekeeping.Enqueue("second", Record(order, "second", done));

            Volatile.Write(ref started, true);
            housekeeping.Start();
            await WithTimeout(done.Task);

            Assert.Equal(new[] { "first", "second" }, order.ToArray());
        }

        [Fact]
        public async Task Enqueue_FailingWork_NextStillRuns()
        {
            using var housekeeping = new HousekeepingService();
            var order = new ConcurrentQueue<string>();
            var done = Tcs.Create<bool>();
            housekeeping.Enqueue("failing", _ => throw new InvalidOperationException("Test"));
            housekeeping.Enqueue("faulted task", _ => Task.FromException(new InvalidOperationException("Test")));
            housekeeping.Enqueue("next", Record(order, "next", done));

            housekeeping.Start();
            await WithTimeout(done.Task);

            Assert.Equal(new[] { "next" }, order.ToArray());
        }

        [Fact]
        public async Task Enqueue_WithDelay_RunsAfterWorkWhichIsDueEarlier()
        {
            using var housekeeping = new HousekeepingService();
            var order = new ConcurrentQueue<string>();
            var done = Tcs.Create<bool>();
            housekeeping.Enqueue("later", Record(order, "later", done), TimeSpan.FromMilliseconds(300));
            housekeeping.Enqueue("now", Record(order, "now"));

            housekeeping.Start();
            await WithTimeout(done.Task);

            Assert.Equal(new[] { "now", "later" }, order.ToArray());
        }

        [Fact]
        public async Task Dispose_CancelsTheRunningWork()
        {
            var housekeeping = new HousekeepingService();
            var running = Tcs.Create<bool>();
            var cancelled = Tcs.Create<bool>();
            housekeeping.Enqueue("waits until cancelled", async token =>
            {
                running.TrySetResult(true);
                try
                {
                    await Task.Delay(System.Threading.Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    cancelled.TrySetResult(true);
                    throw;
                }
            });
            housekeeping.Start();
            await WithTimeout(running.Task);

            housekeeping.Dispose();

            Assert.True(await WithTimeout(cancelled.Task));
        }

        [Fact]
        public void Enqueue_NegativeDelay_IsRejected()
        {
            using var housekeeping = new HousekeepingService();
            Assert.Throws<ArgumentOutOfRangeException>(() => housekeeping.Enqueue("negative", _ => Task.CompletedTask, TimeSpan.FromSeconds(-1)));
        }
    }
}
