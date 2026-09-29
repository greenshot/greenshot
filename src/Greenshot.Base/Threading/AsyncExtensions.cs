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
using log4net;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Task helpers for the async/threading model.
    /// </summary>
    public static class AsyncExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AsyncExtensions));

        /// <summary>
        /// Observe a task which nobody awaits: faults are logged, cancellation is ignored. Use only where fire-and-forget
        /// is the intended behaviour (UI commands, notifications), flows are handed to the ICaptureFlowRunner instead.
        /// </summary>
        public static void FireAndLog(this Task task, string description, ILog log = null)
        {
            if (task == null)
            {
                return;
            }

            _ = task.ContinueWith(t =>
                {
                    (log ?? Log).Error($"Unhandled error in background operation '{description}'", t.Exception?.GetBaseException());
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        /// <summary>
        /// Stop waiting for the task when the token is cancelled, the task itself keeps running (e.g. a COM call which can't be aborted).
        /// This is Task.WaitAsync of .NET 6+, remove when moving to .NET 10.
        /// </summary>
        public static Task WaitAsync(this Task task, CancellationToken cancellationToken)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return WaitCoreAsync(task, cancellationToken);
        }

        /// <summary>
        /// Stop waiting for the task when the token is cancelled, the task itself keeps running.
        /// </summary>
        public static Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<T>(cancellationToken);
            }

            return WaitCoreAsync(task, cancellationToken);
        }

        /// <summary>
        /// Like <see cref="WaitAsync(Task, CancellationToken)"/>, with an additional timeout which throws a <see cref="TimeoutException"/>.
        /// </summary>
        public static async Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            try
            {
                return await task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == timeoutCts.Token && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"The operation didn't complete within {timeout}.");
            }
        }

        /// <summary>
        /// Like <see cref="WaitAsync(Task, CancellationToken)"/>, with an additional timeout which throws a <see cref="TimeoutException"/>.
        /// </summary>
        public static async Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            try
            {
                await task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == timeoutCts.Token && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"The operation didn't complete within {timeout}.");
            }
        }

        private static async Task WaitCoreAsync(Task task, CancellationToken cancellationToken)
        {
            var cancelled = Tcs.Create<bool>();
            using (cancellationToken.Register(state => ((TaskCompletionSource<bool>)state).TrySetResult(true), cancelled))
            {
                if (task != await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false))
                {
                    ObserveLater(task);
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            await task.ConfigureAwait(false);
        }

        private static async Task<T> WaitCoreAsync<T>(Task<T> task, CancellationToken cancellationToken)
        {
            var cancelled = Tcs.Create<bool>();
            using (cancellationToken.Register(state => ((TaskCompletionSource<bool>)state).TrySetResult(true), cancelled))
            {
                if (task != await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false))
                {
                    ObserveLater(task);
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// The task we stopped waiting for may still fault, observe it so it doesn't end up as UnobservedTaskException.
        /// </summary>
        private static void ObserveLater(Task task)
        {
            _ = task.ContinueWith(t => Log.Debug("Abandoned operation faulted after cancellation", t.Exception?.GetBaseException()),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
