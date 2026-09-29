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
    /// The only way background code talks to the UI thread. See docs/async-threading-roadmap.md, section 4.1.
    /// </summary>
    /// <remarks>
    /// Semantics, identical for every implementation:
    /// <list type="bullet">
    /// <item><description>The delegate is always posted, also when the caller already is on the UI thread (no inline execution, no re-entrancy).</description></item>
    /// <item><description>The cancellation token cancels before the delegate starts; once it runs, the delegate owns cancellation.</description></item>
    /// <item><description>The returned task completes asynchronously (never inline on the UI thread).</description></item>
    /// <item><description>Exceptions of the delegate propagate to the awaiter.</description></item>
    /// </list>
    /// </remarks>
    public interface IUiDispatcher
    {
        /// <summary>
        /// True when the calling thread is the UI thread.
        /// </summary>
        bool CheckAccess();

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when the calling thread is not the UI thread.
        /// </summary>
        void VerifyAccess();

        /// <summary>
        /// Run the action on the UI thread.
        /// </summary>
        Task InvokeAsync(Action action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Run the function on the UI thread and return its result.
        /// </summary>
        Task<T> InvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default);

        /// <summary>
        /// Start the asynchronous function on the UI thread (e.g. a dialog which completes later) and return its result.
        /// </summary>
        Task<T> InvokeAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default);
    }
}
