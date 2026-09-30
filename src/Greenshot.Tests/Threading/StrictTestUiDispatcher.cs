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
using System.Threading;
using Greenshot.Base.Threading;

namespace Greenshot.Tests.Threading
{
    /// <summary>
    /// A dedicated single "UI" thread with its own SynchronizationContext, for tests which must prove that code
    /// touches UI objects only through the dispatcher.
    /// </summary>
    public sealed class StrictTestUiDispatcher : SynchronizationContextUiDispatcher, IDisposable
    {
        private readonly SingleThreadContext _context;

        private StrictTestUiDispatcher(SingleThreadContext context) : base(context, context.ThreadId)
        {
            _context = context;
        }

        public static StrictTestUiDispatcher Create()
        {
            return new StrictTestUiDispatcher(new SingleThreadContext());
        }

        public int ThreadId => _context.ThreadId;

        public void Dispose() => _context.Dispose();

        private sealed class SingleThreadContext : SynchronizationContext, IDisposable
        {
            private readonly BlockingCollection<(SendOrPostCallback Callback, object State)> _queue = new();
            private readonly Thread _thread;

            public SingleThreadContext()
            {
                using var started = new ManualResetEventSlim();
                _thread = new Thread(() =>
                {
                    SetSynchronizationContext(this);
                    started.Set();
                    foreach (var (callback, state) in _queue.GetConsumingEnumerable())
                    {
                        callback(state);
                    }
                })
                {
                    IsBackground = true,
                    Name = "Test UI thread"
                };
                _thread.SetApartmentState(ApartmentState.STA);
                _thread.Start();
                started.Wait();
            }

            public int ThreadId => _thread.ManagedThreadId;

            public override void Post(SendOrPostCallback d, object state) => _queue.Add((d, state));

            public override void Send(SendOrPostCallback d, object state) => throw new InvalidOperationException("Send is not allowed (rule R5).");

            public override SynchronizationContext CreateCopy() => this;

            public void Dispose()
            {
                _queue.CompleteAdding();
                _thread.Join(TimeSpan.FromSeconds(5));
            }
        }
    }
}
