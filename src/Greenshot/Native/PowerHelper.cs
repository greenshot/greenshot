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
using log4net;

namespace Greenshot.Native
{
    /// <summary>
    /// Helper to prevent the operating system from entering idle sleep or turning off the screen during active recording.
    /// </summary>
    internal sealed class PowerHelper : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PowerHelper));

        [Flags]
        private enum ExecutionState : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        private bool _disposed;

        public PowerHelper()
        {
            try
            {
                var previous = SetThreadExecutionState(ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED | ExecutionState.ES_AWAYMODE_REQUIRED);
                if (previous == 0)
                {
                    Log.Warn("SetThreadExecutionState returned 0 when requesting continuous system execution.");
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to configure thread execution state: " + ex.Message, ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // Restore default OS power management behavior
                SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);
            }
            catch (Exception ex)
            {
                Log.Debug("Failed to reset thread execution state: " + ex.Message, ex);
            }
        }
    }
}
