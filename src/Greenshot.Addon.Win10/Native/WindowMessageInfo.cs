// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using Dapplo.Windows.Messages;

namespace Greenshot.Addon.Win10.Native
{
    /// <summary>
    /// Container for the windows messages
    /// </summary>
    public class WindowMessageInfo
    {
        /// <summary>
        /// IntPtr with the Handle of the window
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// WindowsMessages which is the actual message
        /// </summary>
        public WindowsMessages Message { get; private set; }

        /// <summary>
        /// IntPtr with the word-param
        /// </summary>
        public IntPtr WordParam { get; private set; }

        /// <summary>
        /// IntPtr with the long-param
        /// </summary>
        public IntPtr LongParam { get; private set; }

        /// <summary>
        /// Factory method for the Window Message Info
        /// </summary>
        /// <param name="hwnd">IntPtr with the Handle of the window</param>
        /// <param name="msg">WindowsMessages which is the actual message</param>
        /// <param name="wParam">IntPtr with the word-param</param>
        /// <param name="lParam">IntPtr with the long-param</param>
        /// <param name="handled"></param>
        /// <returns>WindowMessageInfo</returns>
        public static WindowMessageInfo Create(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return new WindowMessageInfo
            {
                Handle = hwnd,
                Message = (WindowsMessages)msg,
                WordParam = wParam,
                LongParam = lParam
            };
        }
    }
}