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

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Exception thrown when a clipboard operation fails, such as when the Windows clipboard is locked by another process.
    /// </summary>
    public class ClipboardException : ExternalException
    {
        public const int ClipbrdECantOpen = unchecked((int)0x800401D0);

        /// <summary>
        /// Gets the process name or window title of the application holding the clipboard lock, if identified.
        /// </summary>
        public string OwnerProcess { get; }

        public ClipboardException(string message) : base(message)
        {
            HResult = ClipbrdECantOpen;
        }

        public ClipboardException(string message, Exception innerException) : base(message, innerException)
        {
            HResult = (innerException as ExternalException)?.ErrorCode ?? ClipbrdECantOpen;
        }

        public ClipboardException(string message, string ownerProcess, Exception innerException = null)
            : base(FormatMessage(message, ownerProcess), innerException)
        {
            OwnerProcess = ownerProcess;
            HResult = (innerException as ExternalException)?.ErrorCode ?? ClipbrdECantOpen;
        }

        private static string FormatMessage(string message, string ownerProcess)
        {
            if (!string.IsNullOrEmpty(ownerProcess) && message != null && !message.Contains(ownerProcess))
            {
                return $"{message} (Locked by: {ownerProcess})";
            }
            return message;
        }
    }
}
