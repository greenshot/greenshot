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
using System.Diagnostics;
using System.Threading;
using log4net;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Debug-only thread assertions (roadmap Phase 0). A violation is logged with a stack trace and counted, it does not throw:
    /// the goal is to find the places, not to crash a debug session.
    /// </summary>
    public static class ThreadAssert
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ThreadAssert));
        private static IUiDispatcher _uiDispatcher;
        private static int _violations;

        /// <summary>
        /// Number of violations since the start of the process (used by tests and the stress suite).
        /// </summary>
        public static int Violations => Volatile.Read(ref _violations);

        /// <summary>
        /// Raised for every violation, with a description.
        /// </summary>
        public static event EventHandler<string> Violation;

        /// <summary>
        /// Register the UI dispatcher which knows the UI thread, called by the dispatcher itself.
        /// </summary>
        public static void Initialize(IUiDispatcher uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
        }

        /// <summary>
        /// Assert the caller is NOT on the UI thread (every pipeline step, destination export and network call).
        /// Also detects a WinForms SynchronizationContext which leaked onto a pool thread (created by <c>new Control()</c> on that thread).
        /// </summary>
        [Conditional("DEBUG")]
        public static void NotUi(string where)
        {
            var dispatcher = _uiDispatcher;
            if (dispatcher == null)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                Report($"{where} runs on the UI thread, but must run on the thread pool.");
                return;
            }

            var current = SynchronizationContext.Current;
            if (current != null && current.GetType().Name == "WindowsFormsSynchronizationContext")
            {
                Report($"{where} runs on a pool thread which has a WindowsFormsSynchronizationContext, a control was created on this thread.");
            }
        }

        /// <summary>
        /// Assert the caller is on the UI thread (surface, editor and other UI entry points).
        /// </summary>
        [Conditional("DEBUG")]
        public static void IsUi(string where)
        {
            var dispatcher = _uiDispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                Report($"{where} must run on the UI thread, but runs on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
        }

        private static void Report(string message)
        {
            Interlocked.Increment(ref _violations);
            Log.Error($"Thread assertion failed: {message}{Environment.NewLine}{new StackTrace(2, true)}");
            Violation?.Invoke(null, message);
        }
    }
}
