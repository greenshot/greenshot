/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Text;
using Greenshot.Base.UnmanagedHelpers.Enums;

namespace Greenshot.Base.UnmanagedHelpers
{
    /// <summary>
    /// Description of Kernel32.
    /// </summary>
    public class Kernel32
    {
        public const uint ATTACHCONSOLE_ATTACHPARENTPROCESS = 0x0ffffffff; // default value if not specifing a process ID

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32", SetLastError = true)]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32", SetLastError = true)]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint uuchMax);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetPackageFullName(IntPtr hProcess, ref Int32 packageFullNameLength, StringBuilder fullName);


        /// <summary>
        /// Method to get the process path
        /// </summary>
        /// <param name="processid"></param>
        /// <returns></returns>
        public static string GetProcessPath(int processid)
        {
            StringBuilder _PathBuffer = new StringBuilder(512);
            // Try the GetModuleFileName method first since it's the fastest. 
            // May return ACCESS_DENIED (due to VM_READ flag) if the process is not owned by the current user.
            // Will fail if we are compiled as x86 and we're trying to open a 64 bit process...not allowed.
            IntPtr hprocess = OpenProcess(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMRead, false, processid);
            if (hprocess != IntPtr.Zero)
            {
                try
                {
                    if (PsAPI.GetModuleFileNameEx(hprocess, IntPtr.Zero, _PathBuffer, (uint) _PathBuffer.Capacity) > 0)
                    {
                        return _PathBuffer.ToString();
                    }
                }
                finally
                {
                    CloseHandle(hprocess);
                }
            }

            hprocess = OpenProcess(ProcessAccessFlags.QueryInformation, false, processid);
            if (hprocess != IntPtr.Zero)
            {
                try
                {
                    // Try this method for Vista or higher operating systems
                    uint size = (uint) _PathBuffer.Capacity;
                    if ((Environment.OSVersion.Version.Major >= 6) && (QueryFullProcessImageName(hprocess, 0, _PathBuffer, ref size) && (size > 0)))
                    {
                        return _PathBuffer.ToString();
                    }

                    // Try the GetProcessImageFileName method
                    if (PsAPI.GetProcessImageFileName(hprocess, _PathBuffer, (uint) _PathBuffer.Capacity) > 0)
                    {
                        string dospath = _PathBuffer.ToString();
                        foreach (string drive in Environment.GetLogicalDrives())
                        {
                            if (QueryDosDevice(drive.TrimEnd('\\'), _PathBuffer, (uint) _PathBuffer.Capacity) > 0)
                            {
                                if (dospath.StartsWith(_PathBuffer.ToString()))
                                {
                                    return drive + dospath.Remove(0, _PathBuffer.Length);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    CloseHandle(hprocess);
                }
            }

            return string.Empty;
        }
    }
}