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
    /// A dedicated STA thread with a message pump for slow COM calls (Office interop, MAPI), see roadmap section 4.2.
    /// There is one worker per COM server, so a hanging Outlook can't block a Word export or the UI.
    /// </summary>
    public interface IStaWorker : IAsyncDisposable
    {
        /// <summary>
        /// Name of the worker, e.g. "Office" or "MAPI" (logs and hang diagnostics).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// False after a call was abandoned (cancelled while still running), the factory then creates a new worker.
        /// </summary>
        bool IsHealthy { get; }

        /// <summary>
        /// Run the action on the STA thread. Cancelling the token before the action starts skips it; cancelling while it runs
        /// stops waiting for it (a COM call can't be aborted) and marks the worker unhealthy.
        /// </summary>
        Task RunAsync(Action action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Run the function on the STA thread and return its result, see <see cref="RunAsync(Action, CancellationToken)"/>.
        /// </summary>
        Task<T> RunAsync<T>(Func<T> func, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Hands out one lazily created <see cref="IStaWorker"/> per name, and replaces unhealthy workers.
    /// </summary>
    public interface IStaWorkerFactory
    {
        /// <summary>
        /// Get the worker for the supplied name, e.g. "Office" or "MAPI".
        /// </summary>
        IStaWorker Get(string name);
    }
}
