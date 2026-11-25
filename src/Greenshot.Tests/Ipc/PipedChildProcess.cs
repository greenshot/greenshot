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
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Greenshot.Tests.Ipc
{
    /// <summary>
    /// Starts an executable with anonymous pipes as stdin and stdout, the way a browser starts a Native Messaging host.
    /// </summary>
    /// <remarks>
    /// System.Diagnostics.Process can't be used for this: on .NET Framework, Process.StandardInput is a StreamWriter in
    /// Console.InputEncoding with AutoFlush on, and switching AutoFlush on writes the encoding's preamble to the child's
    /// stdin while the process is started. When that encoding is UTF-8 (console code page 65001, or UTF-8 as the system
    /// code page when the test host has no console) the child reads a UTF-8 BOM in front of the first frame, even when the
    /// test only writes to StandardInput.BaseStream. A browser never sends that.
    /// </remarks>
    internal sealed class PipedChildProcess : IDisposable
    {
        private const uint CreateNoWindow = 0x08000000;
        private const int StartfUseStdHandles = 0x00000100;
        private const uint WaitObject0 = 0;
        private const uint StillActive = 259;

        private readonly AnonymousPipeServerStream _stdin;
        private readonly AnonymousPipeServerStream _stdout;
        private readonly SafeProcessHandle _process;

        private PipedChildProcess(AnonymousPipeServerStream stdin, AnonymousPipeServerStream stdout, SafeProcessHandle process)
        {
            _stdin = stdin;
            _stdout = stdout;
            _process = process;
        }

        /// <summary>Pipe to the child's stdin; dispose it to close stdin.</summary>
        public Stream StandardInput => _stdin;

        /// <summary>Pipe from the child's stdout (stderr goes to the same pipe).</summary>
        public Stream StandardOutput => _stdout;

        public static PipedChildProcess Start(string fileName, string arguments, string workingDirectory)
        {
            var stdin = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            var stdout = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            try
            {
                var startupInfo = new StartupInfo
                {
                    cb = Marshal.SizeOf<StartupInfo>(),
                    dwFlags = StartfUseStdHandles,
                    hStdInput = stdin.ClientSafePipeHandle.DangerousGetHandle(),
                    hStdOutput = stdout.ClientSafePipeHandle.DangerousGetHandle(),
                    hStdError = stdout.ClientSafePipeHandle.DangerousGetHandle()
                };
                var commandLine = new StringBuilder($"\"{fileName}\" {arguments}");
                if (!CreateProcessW(fileName, commandLine, IntPtr.Zero, IntPtr.Zero, true, CreateNoWindow, IntPtr.Zero, workingDirectory, ref startupInfo, out var processInformation))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Starting {fileName} failed.");
                }
                CloseHandle(processInformation.hThread);

                // Only the child may hold the client ends, otherwise closing stdin never reaches it as end of stream
                stdin.DisposeLocalCopyOfClientHandle();
                stdout.DisposeLocalCopyOfClientHandle();
                return new PipedChildProcess(stdin, stdout, new SafeProcessHandle(processInformation.hProcess, true));
            }
            catch
            {
                stdin.Dispose();
                stdout.Dispose();
                throw;
            }
        }

        public bool HasExited => WaitForExit(0);

        public int ExitCode => GetExitCodeProcess(_process, out uint exitCode) && exitCode != StillActive ? (int)exitCode : throw new InvalidOperationException("The process has not exited.");

        public bool WaitForExit(int milliseconds) => WaitForSingleObject(_process, (uint)milliseconds) == WaitObject0;

        public void Kill()
        {
            if (!HasExited)
            {
                TerminateProcess(_process, 1);
                WaitForExit(5000);
            }
        }

        public void Dispose()
        {
            _stdin.Dispose();
            _stdout.Dispose();
            _process.Dispose();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateProcessW(string applicationName, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes,
            bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory, ref StartupInfo startupInfo, out ProcessInformation processInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(SafeProcessHandle handle, out uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(SafeProcessHandle handle, uint exitCode);
    }
}
