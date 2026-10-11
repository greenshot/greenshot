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
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace Greenshot.Tests
{
    /// <summary>
    /// A Fact that is skipped while the Windows session is locked or not active (e.g. a disconnected remote desktop).
    /// The clipboard is not usable then, so such tests would fail for reasons that have nothing to do with the code,
    /// e.g. when the tests are started remotely.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class InteractiveDesktopFactAttribute : FactAttribute
    {
        public InteractiveDesktopFactAttribute([CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
        {
            if (!IsInteractiveSession())
            {
                Skip = "The Windows session is locked or not active, the clipboard is not usable.";
            }
        }

        /// <summary>
        /// True when the session of this process is active and unlocked, as reported by WTSQuerySessionInformation(WTSSessionInfoEx).
        /// If the state cannot be determined, the session is assumed to be usable.
        /// </summary>
        public static bool IsInteractiveSession()
        {
            const int WtsSessionInfoEx = 25;
            const int WtsActive = 0;
            const int WtsSessionStateLock = 0;

            if (!ProcessIdToSessionId((uint)Process.GetCurrentProcess().Id, out uint sessionId) ||
                !WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsSessionInfoEx, out IntPtr buffer, out uint bytes) ||
                buffer == IntPtr.Zero)
            {
                return true;
            }
            try
            {
                // WTSINFOEXW: Level (DWORD), then the WTSINFOEX_LEVEL1_W union member. That contains LARGE_INTEGER fields,
                // so it is 8-byte aligned and starts at offset 8: SessionId (ULONG), SessionState (int), SessionFlags (LONG), ...
                if (bytes < 20 || Marshal.ReadInt32(buffer, 0) != 1)
                {
                    return true;
                }
                int sessionState = Marshal.ReadInt32(buffer, 12);
                int sessionFlags = Marshal.ReadInt32(buffer, 16);
                return sessionState == WtsActive && sessionFlags != WtsSessionStateLock;
            }
            finally
            {
                WTSFreeMemory(buffer);
            }
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ProcessIdToSessionId(uint processId, out uint sessionId);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WTSQuerySessionInformation(IntPtr server, uint sessionId, int infoClass, out IntPtr buffer, out uint bytesReturned);

        [DllImport("wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr memory);
    }
}
