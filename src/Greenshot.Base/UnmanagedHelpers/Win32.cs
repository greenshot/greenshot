/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Runtime.InteropServices;
using System.Text;
using Greenshot.Base.UnmanagedHelpers.Enums;

namespace Greenshot.Base.UnmanagedHelpers
{
    public static class Win32
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, [Out] StringBuilder lpBuffer, int nSize, IntPtr arguments);

        [DllImport("kernel32.dll")]
        public static extern void SetLastError(uint dwErrCode);

        public static Win32Error GetLastErrorCode()
        {
            return (Win32Error) Marshal.GetLastWin32Error();
        }

        public static string GetMessage(Win32Error errorCode)
        {
            var buffer = new StringBuilder(0x100);

            if (FormatMessage(0x3200, IntPtr.Zero, (uint) errorCode, 0, buffer, buffer.Capacity, IntPtr.Zero) == 0)
            {
                return "Unknown error (0x" + ((int) errorCode).ToString("x") + ")";
            }

            var result = new StringBuilder();
            int i = 0;

            while (i < buffer.Length)
            {
                if (!char.IsLetterOrDigit(buffer[i]) &&
                    !char.IsPunctuation(buffer[i]) &&
                    !char.IsSymbol(buffer[i]) &&
                    !char.IsWhiteSpace(buffer[i]))
                    break;

                result.Append(buffer[i]);
                i++;
            }

            return result.ToString().Replace("\r\n", string.Empty);
        }
    }
}