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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The window rules of Greenshot on top of Dapplo.Windows: the windows of Greenshot itself which are left out, the active window,
    /// the windows to snap to and the windows to offer for a window capture.
    /// </summary>
    public static class WindowHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowHelper));
        private static readonly ICoreConfiguration Conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private static readonly ConcurrentDictionary<IntPtr, bool> IgnoreHandles = new();

        /// <summary>
        /// Leave a window of Greenshot (e.g. a message window or the capture window) out of the window lists and the active window
        /// </summary>
        /// <param name="handle">IntPtr</param>
        public static void RegisterIgnoreHandle(IntPtr handle) => IgnoreHandles[handle] = true;

        /// <summary>
        /// Remove a handle registered with RegisterIgnoreHandle
        /// </summary>
        /// <param name="handle">IntPtr</param>
        public static void UnregisterIgnoreHandle(IntPtr handle) => IgnoreHandles.TryRemove(handle, out _);

        private static bool IsIgnored(IInteropWindow window) => IgnoreHandles.ContainsKey(window.Handle);

        /// <summary>
        /// The foreground window, the desktop window when the foreground window is one of Greenshot's ignored windows or invisible
        /// </summary>
        /// <returns>IInteropWindow, null when there is no foreground window</returns>
        public static IInteropWindow GetActiveWindow()
        {
            var window = InteropWindowQuery.GetForegroundWindow();
            if (window.Handle == IntPtr.Zero)
            {
                return null;
            }

            return IsIgnored(window) || !window.IsVisible() ? InteropWindowQuery.GetDesktopWindow() : window;
        }

        /// <summary>
        /// The top-level windows which can be seen on the screen, in Z-order, to snap to. Their values are cached, so they describe the screen
        /// at the time of this call.
        /// </summary>
        /// <returns>IReadOnlyList with IInteropWindow</returns>
        public static IReadOnlyList<IInteropWindow> GetVisibleWindows()
        {
            var screenBounds = DisplayInfo.ScreenBounds;
            return InteropWindowQuery.GetTopWindows()
                .Where(window => !IsIgnored(window) && window.IsVisible() && !window.IsMinimized() && !window.CanIgnoreClass()
                                 && !window.GetInfo().Bounds.Intersect(screenBounds).IsEmpty)
                .ToList();
        }

        /// <summary>
        /// The application windows, including the minimized ones, in Z-order, to offer for a window capture
        /// </summary>
        /// <returns>IEnumerable with IInteropWindow</returns>
        public static IEnumerable<IInteropWindow> GetTopLevelWindows() =>
            InteropWindowQuery.GetVisibleApplicationWindows(includeMinimized: true).Where(window => !IsIgnored(window));

        /// <summary>
        /// Bring the window to the foreground, restoring it when it's minimized
        /// </summary>
        /// <param name="handle">IntPtr</param>
        public static void ToForeground(IntPtr handle) =>
            InteropWindowFactory.CreateFor(handle).ToForegroundAsync().AsTask().FireAndLog("Bring a window to the foreground", Log);

        /// <summary>
        /// The path of the executable of the process which owns the window, also for an elevated process
        /// </summary>
        /// <param name="window">IInteropWindow</param>
        /// <returns>string, null when it can't be read</returns>
        public static string GetProcessPath(this IInteropWindow window)
        {
            var processId = window.GetProcessId();
            // 0: the window doesn't exist (anymore)
            return processId == 0 ? null : Kernel32Api.GetProcessPath(processId);
        }

        /// <summary>
        /// The name (without .exe) of the process which owns the window
        /// </summary>
        /// <param name="window">IInteropWindow</param>
        /// <returns>string, empty when it can't be read</returns>
        public static string GetProcessName(this IInteropWindow window)
        {
            try
            {
                var path = window.GetProcessPath();
                if (!string.IsNullOrEmpty(path))
                {
                    return Path.GetFileNameWithoutExtension(path);
                }

                var processId = window.GetProcessId();
                if (processId == 0)
                {
                    return string.Empty;
                }
                using var process = Process.GetProcessById(processId);
                return process.ProcessName;
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not get the process name of window {window.Handle}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// The icon of the window, in the size of the icon size setting
        /// </summary>
        /// <param name="window">IInteropWindow</param>
        /// <returns>Bitmap, null when there is none</returns>
        public static Bitmap GetDisplayIcon(this IInteropWindow window)
        {
            try
            {
                return window.GetIcon<Bitmap>(Conf.IconSize.Width >= 32 || Conf.IconSize.Height >= 32);
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't get the icon for window {window.GetCaption()}", ex);
                return null;
            }
        }

        /// <summary>
        /// Find the window to capture for a recipe: the active window when it matches, else the first matching application window in Z-order.
        /// </summary>
        /// <param name="title">Part of the title, used when there is no titlePattern</param>
        /// <param name="titlePattern">Regular expression for the title, an invalid one is logged and ignored</param>
        /// <param name="processName">Process name, with or without .exe</param>
        /// <param name="matchCase">bool</param>
        /// <returns>IInteropWindow, null when no window matches</returns>
        public static IInteropWindow FindMatchingWindow(string title, string titlePattern, string processName, bool matchCase)
        {
            Regex regex = null;
            if (!string.IsNullOrEmpty(titlePattern))
            {
                try
                {
                    regex = new Regex(titlePattern, matchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
                }
                catch (ArgumentException ex)
                {
                    Log.Warn($"Invalid windowTitlePattern regex '{titlePattern}'", ex);
                }
            }

            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var activeWindow = GetActiveWindow();
            if (activeWindow != null && Matches(activeWindow, title, regex, processName, comparison))
            {
                return activeWindow;
            }

            return GetTopLevelWindows().FirstOrDefault(window => Matches(window, title, regex, processName, comparison));
        }

        private static bool Matches(IInteropWindow window, string title, Regex regex, string processName, StringComparison comparison)
        {
            if (!string.IsNullOrEmpty(processName))
            {
                var name = window.GetProcessName();
                if (!name.Equals(Path.GetFileNameWithoutExtension(processName), comparison) && !name.Equals(processName, comparison))
                {
                    return false;
                }
            }

            var caption = window.GetCaption() ?? string.Empty;
            if (regex != null)
            {
                return regex.IsMatch(caption);
            }

            return string.IsNullOrEmpty(title) || caption.IndexOf(title, comparison) >= 0;
        }
    }
}
