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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// "await ThreadPoolSwitch.SwitchToThreadPoolAsync()" continues on a thread pool (MTA) thread, unless the caller already is on one.
    /// For code which must not run on the UI (STA) thread, e.g. Direct3D objects bound to the MTA. It is a boundary like Task.Run (rule R10),
    /// use it only where the work itself requires it, not to "get off the UI thread".
    /// </summary>
    public static class ThreadPoolSwitch
    {
        public static SwitchAwaitable SwitchToThreadPoolAsync() => default;

        public readonly struct SwitchAwaitable
        {
            public SwitchAwaiter GetAwaiter() => default;
        }

        public readonly struct SwitchAwaiter : ICriticalNotifyCompletion
        {
            public bool IsCompleted =>
                Thread.CurrentThread.IsThreadPoolThread &&
                SynchronizationContext.Current == null &&
                Thread.CurrentThread.GetApartmentState() != ApartmentState.STA;

            public void OnCompleted(Action continuation)
            {
                ThreadPool.QueueUserWorkItem(state => ((Action)state)(), continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                ThreadPool.UnsafeQueueUserWorkItem(state => ((Action)state)(), continuation);
            }

            public void GetResult()
            {
            }
        }
    }
}
