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

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// <see cref="IUiDispatcher"/> which posts to the <see cref="SynchronizationContext"/> of a single (UI) thread.
    /// </summary>
    public class SynchronizationContextUiDispatcher : IUiDispatcher
    {
        private readonly SynchronizationContext _context;
        private readonly int _threadId;
        private volatile bool _shutdown;

        /// <param name="context">SynchronizationContext which executes posted callbacks on the UI thread</param>
        /// <param name="threadId">Managed thread id of the UI thread</param>
        public SynchronizationContextUiDispatcher(SynchronizationContext context, int threadId)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _threadId = threadId;
        }

        /// <summary>
        /// The SynchronizationContext of the UI thread, for the few places which need it directly (e.g. <c>Progress&lt;T&gt;</c> in UI code).
        /// </summary>
        public SynchronizationContext Context => _context;

        /// <summary>
        /// True after <see cref="BeginShutdown"/>: new invocations fail fast instead of waiting for a message loop which is gone.
        /// </summary>
        public bool IsShuttingDown => _shutdown;

        /// <summary>
        /// Stop accepting new work, every following InvokeAsync fails with an <see cref="ObjectDisposedException"/>.
        /// </summary>
        public void BeginShutdown()
        {
            _shutdown = true;
        }

        public bool CheckAccess() => Thread.CurrentThread.ManagedThreadId == _threadId;

        public void VerifyAccess()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException($"This must be called on the UI thread (thread {_threadId}), but was called on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
        }

        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return InvokeAsync(() =>
            {
                action();
                return true;
            }, cancellationToken);
        }

        public Task<T> InvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var tcs = Tcs.Create<T>();
            Post(tcs, cancellationToken, () =>
            {
                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }

        public Task<T> InvokeAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var tcs = Tcs.Create<T>();
            Post(tcs, cancellationToken, () => _ = RunAndCompleteAsync(func, tcs));
            return tcs.Task;
        }

        private static async Task RunAndCompleteAsync<T>(Func<Task<T>> func, TaskCompletionSource<T> tcs)
        {
            try
            {
                // Runs on the UI thread, the continuation stays there as the UI SynchronizationContext is captured.
                var result = await (func() ?? Task.FromResult(default(T))).ConfigureAwait(true);
                tcs.TrySetResult(result);
            }
            catch (OperationCanceledException oce)
            {
                tcs.TrySetCanceled(oce.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        private void Post<T>(TaskCompletionSource<T> tcs, CancellationToken cancellationToken, Action work)
        {
            if (_shutdown)
            {
                tcs.TrySetException(new ObjectDisposedException(nameof(IUiDispatcher), "The UI is shutting down."));
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return;
            }

            try
            {
                _context.Post(_ =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    work();
                }, null);
            }
            catch (Exception ex)
            {
                // The message loop is gone (e.g. marshaling control disposed during shutdown)
                tcs.TrySetException(new ObjectDisposedException(nameof(IUiDispatcher), ex.Message));
            }
        }
    }
}
