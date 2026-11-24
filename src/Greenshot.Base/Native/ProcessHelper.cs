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
using System.Runtime.InteropServices;

namespace Greenshot.Base.Native
{
    /// <summary>
    /// Cheap process checks.
    /// Process.GetProcessesByName builds a ProcessInfo with a ThreadInfo for every thread of every process,
    /// about 1.4 MB of managed garbage (part of it on the large object heap) with 340 processes and 5900 threads.
    /// This asks Windows for the same list into native memory and only compares the image names.
    /// </summary>
    public static class ProcessHelper
    {
        private const int SystemProcessInformation = 5;
        private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);

        // SYSTEM_PROCESS_INFORMATION (winternl.h): NextEntryOffset is at 0, ImageName (UNICODE_STRING) at 0x38 on x86 and x64.
        private const int ImageNameOffset = 0x38;

        // UNICODE_STRING: ushort Length (bytes), ushort MaximumLength, then the pointer, aligned to the pointer size
        private static readonly int ImageNameBufferOffset = ImageNameOffset + IntPtr.Size;

        private static int _bufferSize = 1024 * 1024;

        [DllImport("ntdll.dll")]
        private static extern int NtQuerySystemInformation(int systemInformationClass, IntPtr systemInformation, int systemInformationLength, out int returnLength);

        /// <summary>
        /// Is a process with this image name running, for example "ONENOTE.EXE"? The comparison ignores case.
        /// </summary>
        /// <param name="imageName">File name of the executable, with extension</param>
        /// <returns>true if at least one process has this image name</returns>
        public static bool IsRunning(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
            {
                return false;
            }

            while (true)
            {
                int bufferSize = _bufferSize;
                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    int status = NtQuerySystemInformation(SystemProcessInformation, buffer, bufferSize, out int needed);
                    if (status == StatusInfoLengthMismatch)
                    {
                        // Processes can start between the calls, leave some room
                        _bufferSize = Math.Max(bufferSize * 2, needed + 64 * 1024);
                        continue;
                    }

                    if (status < 0)
                    {
                        throw new InvalidOperationException($"NtQuerySystemInformation failed with 0x{status:X8}");
                    }

                    return ContainsImageName(buffer, imageName);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        private static bool ContainsImageName(IntPtr buffer, string imageName)
        {
            IntPtr entry = buffer;
            while (true)
            {
                int nameBytes = (ushort)Marshal.ReadInt16(entry, ImageNameOffset);
                IntPtr name = Marshal.ReadIntPtr(entry, ImageNameBufferOffset);
                if (name != IntPtr.Zero && nameBytes == imageName.Length * 2 && NameEquals(name, imageName))
                {
                    return true;
                }

                int next = Marshal.ReadInt32(entry);
                if (next == 0)
                {
                    return false;
                }

                entry = IntPtr.Add(entry, next);
            }
        }

        private static bool NameEquals(IntPtr name, string imageName)
        {
            for (int i = 0; i < imageName.Length; i++)
            {
                char c = (char)Marshal.ReadInt16(name, i * 2);
                if (char.ToUpperInvariant(c) != char.ToUpperInvariant(imageName[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
