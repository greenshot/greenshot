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
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Greenshot.Editor.Standalone.Ipc
{
    /// <summary>
    /// The named pipe of the running Greenshot (Greenshot_{SID of the user}, see Greenshot's NamedPipeEndpoint).
    /// Greenshot checks which program connects; the editor does the same and only talks to the Greenshot it was installed with.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class GreenshotPipe
    {
        private const int ConnectTimeout = 2000;
        private const uint ProcessQueryLimitedInformation = 0x1000;

        public static string PipeName => "Greenshot_" + (WindowsIdentity.GetCurrent().User?.Value ?? throw new InvalidOperationException("The user of the editor has no SID"));

        /// <summary>
        /// Connects to Greenshot. Throws when it doesn't run, or when the program behind the pipe isn't the expected Greenshot.exe.
        /// </summary>
        public static async Task<Stream> ConnectAsync(string greenshotExecutable, CancellationToken cancellationToken)
        {
            var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await pipe.ConnectAsync(ConnectTimeout, cancellationToken).ConfigureAwait(false);
                if (!GetNamedPipeServerProcessId(pipe.SafePipeHandle, out uint processId) || PathOf(processId) is not string server)
                {
                    throw new UnauthorizedAccessException("The program behind the pipe of Greenshot could not be identified.");
                }

                if (!string.Equals(Path.GetFullPath(server), Path.GetFullPath(greenshotExecutable), StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException($"The pipe of Greenshot is served by '{server}', not by '{greenshotExecutable}'.");
                }

                // Greenshot shows the windows of its destinations (a file dialog, the printer dialog) in front of the editor
                AllowSetForegroundWindow(processId);
                return pipe;
            }
            catch
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        private static string PathOf(uint processId)
        {
            using var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (process.IsInvalid)
            {
                return null;
            }

            var buffer = new StringBuilder(1024);
            int size = buffer.Capacity;
            return QueryFullProcessImageName(process, 0, buffer, ref size) ? buffer.ToString(0, size) : null;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint serverProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeProcessHandle OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder exeName, ref int size);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllowSetForegroundWindow(uint processId);
    }
}
