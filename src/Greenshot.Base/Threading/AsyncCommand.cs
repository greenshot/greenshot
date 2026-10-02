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
using System.Threading.Tasks;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// The one way to start async work from a (sync) UI event handler, instead of <c>async void</c> (rule R2).
    /// Exceptions are logged, never lost and never crash the process.
    /// </summary>
    public static class AsyncCommand
    {
        /// <summary>
        /// Start the async handler on the calling (UI) thread, its continuations return to the UI thread.
        /// </summary>
        public static void Run(Func<Task> handler, string description)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Task task;
            try
            {
                task = handler();
            }
            catch (Exception ex)
            {
                task = Task.FromException(ex);
            }

            task.FireAndLog(description);
        }

        /// <summary>
        /// Start the async work on the thread pool: the boundary between a UI event and a background command (rule R10).
        /// </summary>
        public static void RunInBackground(Func<Task> work, string description)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));
#pragma warning disable RS0030 // R10: AsyncCommand is one of the allowed Task.Run boundaries
            Task.Run(work).FireAndLog(description);
#pragma warning restore RS0030
        }

        /// <summary>
        /// Create an EventHandler which runs the async handler, for "button.Click += AsyncCommand.Handler(...)".
        /// </summary>
        public static EventHandler Handler(Func<Task> handler, string description)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return (_, _) => Run(handler, description);
        }
    }
}
