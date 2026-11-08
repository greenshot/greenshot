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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Runs the queued housekeeping on the thread pool, one item after the other in the order they are due.
    /// The work queued before <see cref="Start"/> waits for it, so nothing competes with the start of Greenshot.
    /// </summary>
    public sealed class HousekeepingService : IHousekeeping, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HousekeepingService));

        /// <summary>
        /// The longest wait for the next item, SemaphoreSlim takes at most int.MaxValue milliseconds (about 24 days)
        /// </summary>
        private static readonly TimeSpan MaxWait = TimeSpan.FromDays(1);

        private readonly object _lock = new object();
        // Sorted by due time, equal due times keep the order in which they were queued
        private readonly List<Item> _items = new List<Item>();
        private readonly SemaphoreSlim _queued = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private int _started;

        private sealed class Item
        {
            public Item(string description, Func<CancellationToken, Task> work, DateTime due)
            {
                Description = description;
                Work = work;
                Due = due;
            }

            public string Description { get; }
            public Func<CancellationToken, Task> Work { get; }
            public DateTime Due { get; }
        }

        /// <inheritdoc />
        public void Enqueue(string description, Func<CancellationToken, Task> work, TimeSpan delay = default)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));
            if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay));
            var item = new Item(description ?? work.Method.Name, work, DateTime.UtcNow + delay);
            lock (_lock)
            {
                int index = _items.Count;
                while (index > 0 && _items[index - 1].Due > item.Due)
                {
                    index--;
                }
                _items.Insert(index, item);
            }
            _queued.Release();
        }

        /// <summary>
        /// Start working through the queue, call it once Greenshot has started
        /// </summary>
        public void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) == 0)
            {
                AsyncCommand.RunInBackground(RunAsync, "Housekeeping");
            }
        }

        private async Task RunAsync()
        {
            var token = _stop.Token;
            while (!token.IsCancellationRequested)
            {
                Item next = null;
                var wait = MaxWait;
                lock (_lock)
                {
                    if (_items.Count > 0)
                    {
                        var remaining = _items[0].Due - DateTime.UtcNow;
                        if (remaining <= TimeSpan.Zero)
                        {
                            next = _items[0];
                            _items.RemoveAt(0);
                        }
                        else if (remaining < wait)
                        {
                            wait = remaining;
                        }
                    }
                }

                if (next == null)
                {
                    // Wakes up when something is queued (it may be due earlier) or when the first item is due
                    await _queued.WaitAsync(wait, token).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    Log.DebugFormat("Housekeeping: {0}", next.Description);
                    await next.Work(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Housekeeping '{next.Description}' failed.", ex);
                }
            }
        }

        /// <summary>
        /// Stops the housekeeping, the running item is cancelled and the queued ones are dropped
        /// </summary>
        public void Dispose()
        {
            _stop.Cancel();
        }
    }
}
