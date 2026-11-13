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
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Editor.Standalone.Diagnostics
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Lets the task run without waiting for it. A failure is never lost: it is logged, and given to
        /// <paramref name="failed"/> on the thread which started the task (the UI thread, when it was started there).
        /// </summary>
        /// <param name="task">The work which runs on</param>
        /// <param name="what">What the task does, for the log</param>
        /// <param name="failed">Tells the user, null when the log is enough</param>
        public static void Forget(this Task task, string what, Action<Exception> failed = null)
        {
            var scheduler = SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;
            _ = task.ContinueWith(done =>
            {
                var exception = done.Exception?.GetBaseException();
                if (exception == null || exception is OperationCanceledException)
                {
                    return;
                }

                EditorLog.Warn($"{what} failed", exception);
                failed?.Invoke(exception);
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, scheduler);
        }
    }
}
