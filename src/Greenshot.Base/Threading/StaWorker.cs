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
using System.Windows.Forms;
using log4net;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// <see cref="IStaWorker"/> implementation: a background STA thread running a WinForms message loop,
    /// with a COM message filter and a watchdog which logs calls running longer than expected.
    /// </summary>
    public sealed class StaWorker : IStaWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StaWorker));
        private readonly TaskCompletionSource<SynchronizationContext> _ready = Tcs.Create<SynchronizationContext>();
        private readonly TaskCompletionSource<bool> _stopped = Tcs.Create<bool>();
        private readonly TimeSpan _watchdogTimeout;
        private readonly TimeSpan _comRetryTimeout;
        private ApplicationContext _applicationContext;
        private volatile bool _healthy = true;
        private volatile bool _disposed;

        /// <param name="name">Name of the worker, e.g. "Office"</param>
        /// <param name="watchdogTimeout">A call running longer than this is logged, default 30 seconds</param>
        /// <param name="comRetryTimeout">How long the COM message filter retries calls rejected by a busy server, default 60 seconds</param>
        public StaWorker(string name, TimeSpan? watchdogTimeout = null, TimeSpan? comRetryTimeout = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _watchdogTimeout = watchdogTimeout ?? TimeSpan.FromSeconds(30);
            _comRetryTimeout = comRetryTimeout ?? TimeSpan.FromSeconds(60);
#pragma warning disable RS0030 // The STA worker is the one place which owns a dedicated STA thread
            var thread = new Thread(ThreadMain)
#pragma warning restore RS0030
            {
                IsBackground = true,
                Name = $"STA worker {name}"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public string Name { get; }

        public bool IsHealthy => _healthy && !_disposed;

        private void ThreadMain()
        {
            try
            {
                OleMessageFilter.Register(_comRetryTimeout);
#pragma warning disable RS0030 // Needs its own WinForms context to pump messages
                var context = new WindowsFormsSynchronizationContext();
#pragma warning restore RS0030
                SynchronizationContext.SetSynchronizationContext(context);
                _applicationContext = new ApplicationContext();
                _ready.TrySetResult(context);
                Application.Run(_applicationContext);
            }
            catch (Exception ex)
            {
                Log.Error($"STA worker '{Name}' stopped with an error", ex);
                _ready.TrySetException(ex);
                _healthy = false;
            }
            finally
            {
                try
                {
                    OleMessageFilter.Revoke();
                }
                catch (Exception ex)
                {
                    Log.Debug("Couldn't revoke the message filter", ex);
                }

                _stopped.TrySetResult(true);
            }
        }

        public Task RunAsync(Action action, CancellationToken cancellationToken = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return RunAsync(() =>
            {
                action();
                return true;
            }, cancellationToken);
        }

        public async Task<T> RunAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (_disposed) throw new ObjectDisposedException($"STA worker {Name}");
            cancellationToken.ThrowIfCancellationRequested();

            var context = await _ready.Task.ConfigureAwait(false);
            var tcs = Tcs.Create<T>();
            // 0: queued, 1: running, 2: abandoned before it started
            int state = 0;
            context.Post(_ =>
            {
                if (cancellationToken.IsCancellationRequested || Interlocked.CompareExchange(ref state, 1, 0) != 0)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                using var watchdog = new System.Threading.Timer(_ => Log.WarnFormat("STA worker '{0}': a call is running for more than {1:0} s", Name, _watchdogTimeout.TotalSeconds),
                    null, _watchdogTimeout, Timeout.InfiniteTimeSpan);
                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);

            try
            {
                return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (Interlocked.CompareExchange(ref state, 2, 0) == 1 && !tcs.Task.IsCompleted)
            {
                // The call is still running on the STA thread and can't be aborted: stop using this worker.
                Log.WarnFormat("STA worker '{0}': a running call was abandoned, the worker will be replaced.", Name);
                _healthy = false;
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (!_ready.Task.IsCompleted)
            {
                await Task.WhenAny(_ready.Task, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
            }

            if (_ready.Task.Status == TaskStatus.RanToCompletion)
            {
                var context = await _ready.Task.ConfigureAwait(false);
                context.Post(_ => _applicationContext?.ExitThread(), null);
                // Don't wait forever: a hanging COM call keeps the thread busy, it's a background thread and dies with the process.
                await Task.WhenAny(_stopped.Task, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
            }
        }
    }
}
