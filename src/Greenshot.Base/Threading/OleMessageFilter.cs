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
using System.Runtime.InteropServices;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// COM message filter which retries calls rejected by a busy server (RPC_E_CALL_REJECTED / SERVERCALL_RETRYLATER),
    /// the classic "Office is busy" failure. Registered on every <see cref="StaWorker"/> thread.
    /// </summary>
    internal sealed class OleMessageFilter : OleMessageFilter.IOleMessageFilter
    {
        private const int ServerCallIsHandled = 0;
        private const int ServerCallRetryLater = 2;
        private const int PendingMsgWaitDefProcess = 2;
        private const int RetryDelayMilliseconds = 100;
        private readonly int _maxRetryMilliseconds;

        private OleMessageFilter(TimeSpan maxRetry)
        {
            _maxRetryMilliseconds = (int)maxRetry.TotalMilliseconds;
        }

        /// <summary>
        /// Register a message filter for the current (STA) thread.
        /// </summary>
        public static void Register(TimeSpan maxRetry)
        {
            CoRegisterMessageFilter(new OleMessageFilter(maxRetry), out _);
        }

        /// <summary>
        /// Remove the message filter of the current thread.
        /// </summary>
        public static void Revoke()
        {
            CoRegisterMessageFilter(null, out _);
        }

        int IOleMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo) => ServerCallIsHandled;

        int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType)
        {
            if (dwRejectType == ServerCallRetryLater && dwTickCount < _maxRetryMilliseconds)
            {
                // Retry after the delay
                return RetryDelayMilliseconds;
            }

            // Cancel the call
            return -1;
        }

        int IOleMessageFilter.MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType) => PendingMsgWaitDefProcess;

        [DllImport("ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);

        [ComImport]
        [Guid("00000016-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IOleMessageFilter
        {
            [PreserveSig]
            int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);

            [PreserveSig]
            int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);

            [PreserveSig]
            int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);
        }
    }
}
