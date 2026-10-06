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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using log4net;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Gives the Windows title bar (the frame drawn by the desktop window manager) the colors of Greenshot's theme: dark when the
    /// theme is dark, and keeps it in sync when the theme changes. Every WPF window gets this on its own (see <see cref="Register"/>),
    /// WinForms forms call <see cref="Attach(IntPtr)"/> when their handle is created.
    /// Also offers the rounded corners of Windows 11 for windows without a title bar.
    /// </summary>
    public static class WindowFrameTheme
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowFrameTheme));

        // DWMWINDOWATTRIBUTE values, see dwmapi.h
        private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwaNcRenderingPolicy = 2;
        private const int DwmwaCloak = 13;

        private const int DwmncrpUseWindowStyle = 0;

        private const int DwmwcpRound = 2;
        private const int DwmwcpRoundSmall = 3;

        /// <summary>
        /// Windows 11 starts with build 22000, it is the first to round window corners
        /// </summary>
        public static bool HasRoundedCorners { get; } = Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;

        // The windows which follow the theme, by handle: windows live on several UI threads, the handle can be used from any of them
        private static readonly ConcurrentDictionary<IntPtr, bool> AttachedWindows = new ConcurrentDictionary<IntPtr, bool>();
        private static bool _registered;
        private static readonly object RegisterLock = new object();

        /// <summary>
        /// Every WPF window of the process gets the title bar of the theme when it's loaded, a window which wants it earlier
        /// (no light title bar for a moment) calls <see cref="Attach(Window)"/> in OnSourceInitialized.
        /// Called by the <see cref="ThemeManager"/>, calling it again does nothing.
        /// </summary>
        internal static void Register()
        {
            lock (RegisterLock)
            {
                if (_registered)
                {
                    return;
                }

                _registered = true;
            }

            try
            {
                EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded), true);
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't register the title bar theme for WPF windows", ex);
            }
        }

        private static void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window && ReferenceEquals(e.OriginalSource, window))
            {
                Attach(window);
            }
        }

        /// <summary>
        /// The title bar of the window follows the theme from now on, until the window is closed
        /// </summary>
        public static void Attach(Window window)
        {
            // Only on the thread of the window, theme changes reach windows on other threads through UpdateAll
            if (window == null || !window.CheckAccess())
            {
                return;
            }

            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero || !AttachedWindows.TryAdd(handle, true))
            {
                // No window yet, or already attached
                ApplyTheme(handle);
                return;
            }

            window.Closed += (s, e) => Detach(handle);
            ApplyTheme(handle);
        }

        /// <summary>
        /// The title bar of the window (e.g. a WinForms form) follows the theme from now on, call <see cref="Detach"/> when the handle is destroyed
        /// </summary>
        public static void Attach(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            AttachedWindows.TryAdd(handle, true);
            ApplyTheme(handle);
        }

        /// <summary>
        /// The window is gone, it no longer follows the theme
        /// </summary>
        public static void Detach(IntPtr handle)
        {
            AttachedWindows.TryRemove(handle, out _);
        }

        /// <summary>
        /// The theme changed: update the title bars of all attached windows
        /// </summary>
        internal static void UpdateAll()
        {
            foreach (var handle in AttachedWindows.Keys)
            {
                if (!IsWindow(handle))
                {
                    Detach(handle);
                    continue;
                }

                ApplyTheme(handle, true);
            }
        }

        /// <summary>
        /// Ask Windows 11 to round the corners of a window without a title bar (with a title bar Windows does it anyway).
        /// Doesn't work for windows with AllowsTransparency (layered windows), they draw their own corners.
        /// Does nothing on Windows 10.
        /// </summary>
        /// <param name="window">The window, its handle must exist (call it in OnSourceInitialized or later)</param>
        /// <param name="small">The small radius of menus and tooltips instead of the one of windows</param>
        public static void SetRoundedCorners(Window window, bool small = false)
        {
            if (window == null || !HasRoundedCorners || !window.CheckAccess())
            {
                return;
            }

            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            int preference = small ? DwmwcpRoundSmall : DwmwcpRound;
            DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }

        /// <summary>
        /// Windows shows a window before WPF drew it: a big window (e.g. the settings) is white for a moment, also with a dark theme.
        /// The desktop window manager keeps the window invisible (cloaked) until its content is rendered.
        /// </summary>
        /// <param name="window">The window, its handle must exist and it must not be shown yet (call it in OnSourceInitialized)</param>
        public static void CloakUntilRendered(Window window)
        {
            if (window == null || !window.CheckAccess())
            {
                return;
            }

            // Window.IsVisible is true already when the handle is created, Windows knows whether it's on the screen
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero || IsWindowVisible(handle))
            {
                return;
            }

            int cloak = 1;
            if (DwmSetWindowAttribute(handle, DwmwaCloak, ref cloak, sizeof(int)) != 0)
            {
                // No desktop window manager: shown as before
                return;
            }

            bool uncloaked = false;
            void Uncloak()
            {
                if (uncloaked)
                {
                    return;
                }

                uncloaked = true;
                int value = 0;
                DwmSetWindowAttribute(handle, DwmwaCloak, ref value, sizeof(int));
            }

            window.ContentRendered += (s, e) => Uncloak();
            // Never an invisible window: when nothing is rendered in time, it shows anyway
            var fallback = new System.Windows.Threading.DispatcherTimer(TimeSpan.FromSeconds(2), System.Windows.Threading.DispatcherPriority.Normal, (s, e) =>
            {
                ((System.Windows.Threading.DispatcherTimer)s).Stop();
                Uncloak();
            }, window.Dispatcher);
            fallback.Start();
        }

        /// <summary>
        /// The window had a WindowChrome which was removed while it is open: WPF leaves the frame drawing of the desktop window manager
        /// switched off, Windows then draws the old frame of Windows 7 "basic". Switch it on again and redraw the frame.
        /// </summary>
        /// <param name="window">The window, without WindowChrome now</param>
        public static void RestoreSystemFrame(Window window)
        {
            if (window == null || !window.CheckAccess())
            {
                return;
            }

            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                // Not shown yet: the frame is created as it should be
                return;
            }

            try
            {
                int policy = DwmncrpUseWindowStyle;
                DwmSetWindowAttribute(handle, DwmwaNcRenderingPolicy, ref policy, sizeof(int));
                SetWindowRgn(handle, IntPtr.Zero, IsWindowVisible(handle));
                SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SwpFrameChanged | SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpNoOwnerZOrder);
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't restore the frame of the window", ex);
            }

            ApplyTheme(handle);
        }

        private static void ApplyTheme(IntPtr handle, bool redrawFrame = false)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                int useDarkMode = ThemeManager.Instance.IsDarkTheme ? 1 : 0;
                if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int)) != 0)
                {
                    // Windows 10 before 20H1 used another (undocumented) number for the same attribute
                    DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBefore20H1, ref useDarkMode, sizeof(int));
                }

                if (redrawFrame && !HasRoundedCorners && IsWindowVisible(handle))
                {
                    // Windows 10 only repaints the title bar of a visible window when its frame changes
                    SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SwpFrameChanged | SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpNoOwnerZOrder);
                }
            }
            catch (Exception ex)
            {
                // Old Windows versions or no desktop window manager, the title bar just stays as it is
                Log.Debug("Couldn't set the title bar theme", ex);
            }
        }

        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;
        private const uint SwpNoOwnerZOrder = 0x0200;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, [MarshalAs(UnmanagedType.Bool)] bool bRedraw);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    }
}
