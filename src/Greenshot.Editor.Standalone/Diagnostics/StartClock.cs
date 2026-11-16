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
using System.Diagnostics;
using System.Globalization;

namespace Greenshot.Editor.Standalone.Diagnostics
{
    /// <summary>
    /// Says in the log how long the start of the editor took, step by step: the time since the process was started
    /// and the time since the step before.
    /// </summary>
    public static class StartClock
    {
        private static readonly DateTime ProcessStart = StartTime();
        private static double _previous;

        /// <summary>
        /// Milliseconds since the process was started
        /// </summary>
        public static double Elapsed => (DateTime.Now - ProcessStart).TotalMilliseconds;

        public static void Mark(string step)
        {
            double now = Elapsed;
            EditorLog.Info(string.Format(CultureInfo.InvariantCulture, "Start: {0} after {1:F0} ms (+{2:F0} ms)", step, now, now - _previous));
            _previous = now;
        }

        private static DateTime StartTime()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                return process.StartTime;
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
            {
                return DateTime.Now;
            }
        }
    }
}
