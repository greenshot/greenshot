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
using System.Threading.Tasks;
using System.Windows.Threading;
using log4net;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// The questions Greenshot asks the user on its own, e.g. after the start about changed recipe, command or uploader files or
    /// plugins which aren't signed: one window at a time, in the order they were asked. A modal window runs a message loop, so
    /// without this a question posted meanwhile opens its window next to the open one.
    /// A question asked by the user's own action (an import, a button) is shown at once and doesn't go through here.
    /// </summary>
    public static class UserQuestions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UserQuestions));

        // Used on the UI thread; locked for the case without one (tests, headless)
        private static readonly object Lock = new object();
        private static readonly Queue<KeyValuePair<string, Func<Task>>> Pending = new Queue<KeyValuePair<string, Func<Task>>>();
        private static bool _asking;

        /// <summary>
        /// Ask after the questions before it, on the UI thread once it is idle
        /// </summary>
        /// <param name="what">what is asked, for the log</param>
        /// <param name="ask">shows the window(s) and returns when the user answered</param>
        public static void Enqueue(string what, Action ask)
        {
            if (ask == null) throw new ArgumentNullException(nameof(ask));
            Enqueue(what, () =>
            {
                ask();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Ask after the questions before it, on the UI thread once it is idle; the next question waits for the returned task
        /// </summary>
        /// <param name="what">what is asked, for the log</param>
        /// <param name="ask">shows the window(s), the task completes when the user answered</param>
        public static void Enqueue(string what, Func<Task> ask)
        {
            if (ask == null) throw new ArgumentNullException(nameof(ask));
            UiDispatcher.Current.InvokeAsync(() =>
            {
                lock (Lock)
                {
                    Pending.Enqueue(new KeyValuePair<string, Func<Task>>(what, ask));
                    if (_asking) return;
                    _asking = true;
                }

                AskAllAsync().FireAndLog("Ask the user", Log);
            }).FireAndLog("Queue a question for the user", Log);
        }

        private static async Task AskAllAsync()
        {
            try
            {
                // The caller (e.g. the start) goes on, the questions follow when the UI thread is idle. Only the dispatcher of the
                // application: another thread can have a dispatcher which doesn't run (e.g. one a test made), waiting on it never ends
                if (System.Windows.Application.Current?.Dispatcher is { } dispatcher && dispatcher.CheckAccess())
                {
                    await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                }
                else
                {
                    await Task.Yield();
                }

                while (true)
                {
                    KeyValuePair<string, Func<Task>> next;
                    lock (Lock)
                    {
                        if (Pending.Count == 0)
                        {
                            _asking = false;
                            return;
                        }

                        next = Pending.Dequeue();
                    }

                    try
                    {
                        Log.DebugFormat("Asking the user: {0}", next.Key);
                        // The questions show windows: they stay on the UI thread
                        await next.Value().ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Asking the user failed: {next.Key}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                // The questions which wait are asked with the next one
                lock (Lock)
                {
                    _asking = false;
                }

                Log.Error("Asking the user failed", ex);
            }
        }
    }
}
