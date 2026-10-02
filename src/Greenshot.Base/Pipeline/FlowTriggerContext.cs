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
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Triggers;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// What the world looked like when a flow was triggered, snapshotted synchronously in the trigger handler (UI thread).
    /// The flow runs later on the thread pool, by then the foreground window or the cursor may have changed (roadmap 1.5).
    /// </summary>
    public sealed class FlowTriggerContext
    {
        public FlowTriggerContext(ITrigger trigger, IntPtr foregroundWindow, NativePoint cursorPosition, DateTimeOffset triggeredAt)
        {
            Trigger = trigger;
            ForegroundWindow = foregroundWindow;
            CursorPosition = cursorPosition;
            TriggeredAt = triggeredAt;
        }

        /// <summary>
        /// The trigger which started the flow, null when started from code (menu, editor, IPC without trigger).
        /// </summary>
        public ITrigger Trigger { get; }

        /// <summary>
        /// The foreground window at trigger time, IntPtr.Zero when unknown.
        /// </summary>
        public IntPtr ForegroundWindow { get; }

        /// <summary>
        /// The cursor position (screen coordinates) at trigger time.
        /// </summary>
        public NativePoint CursorPosition { get; }

        /// <summary>
        /// When the flow was triggered.
        /// </summary>
        public DateTimeOffset TriggeredAt { get; }

        /// <summary>
        /// True if the foreground window at trigger time belongs to another process than Greenshot
        /// (e.g. not the tray menu or a Greenshot window which started the flow).
        /// </summary>
        public bool HasExternalForegroundWindow
        {
            get
            {
                if (ForegroundWindow == IntPtr.Zero)
                {
                    return false;
                }

                User32Api.GetWindowThreadProcessId(ForegroundWindow, out var processId);
                using var currentProcess = Process.GetCurrentProcess();
                return processId != 0 && processId != currentProcess.Id;
            }
        }

        /// <summary>
        /// Snapshot the current foreground window and cursor position, call this synchronously where the flow is triggered.
        /// </summary>
        public static FlowTriggerContext Capture(ITrigger trigger = null)
        {
            IntPtr foreground = IntPtr.Zero;
            NativePoint cursor = NativePoint.Empty;
            try
            {
                foreground = User32Api.GetForegroundWindow();
                cursor = User32Api.GetCursorLocation();
            }
            catch (Exception)
            {
                // No desktop (e.g. service or test), keep the defaults
            }

            return new FlowTriggerContext(trigger, foreground, cursor, DateTimeOffset.Now);
        }

        /// <summary>
        /// A context without window or cursor information (tests, nested flows).
        /// </summary>
        public static FlowTriggerContext Empty(ITrigger trigger = null) => new FlowTriggerContext(trigger, IntPtr.Zero, NativePoint.Empty, DateTimeOffset.Now);
    }
}
