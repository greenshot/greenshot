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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Interop;

namespace Greenshot.Addon.Win10.Native
{

    /// <summary>
    ///     A monitor for window messages
    /// </summary>
    public static class WindowMessageMonitor
    {
        /// <summary>
        /// Create a HwndSource for the specified Window
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns>HwndSource</returns>
        private static HwndSource ToHwndSource(this Window window)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }
            return HwndSource.FromHwnd(windowHandle);
        }

        /// <summary>
        ///     Create an observable for the specified window
        /// </summary>
        public static IObservable<WindowMessageInfo> WindowMessages(this Window window)
        {
            return WindowMessages(window, null);
        }

        /// <summary>
        ///     Create an observable for the specified HwndSource
        /// </summary>
        public static IObservable<WindowMessageInfo> WindowMessages(this HwndSource hwndSource)
        {
            return WindowMessages(null, hwndSource);
        }

        /// <summary>
        /// Create an observable for the specified window or HwndSource
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="hwndSource">HwndSource</param>
        /// <returns>IObservable</returns>
        private static IObservable<WindowMessageInfo> WindowMessages(Window window, HwndSource hwndSource)
        {
            if (window == null && hwndSource == null)
            {
                throw new NotSupportedException("One of Window or HwndSource must be supplied");
            }
            if (window != null && hwndSource != null)
            {
                throw new NotSupportedException("Either Window or HwndSource must be supplied");
            }

            return Observable.Create<WindowMessageInfo>(observer =>
            {
                // This handles the message, and generates the observable OnNext
                IntPtr WindowMessageHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    observer.OnNext(WindowMessageInfo.Create(hwnd, msg, wParam, lParam, ref handled));
                    // ReSharper disable once AccessToDisposedClosure
                    if (hwndSource.IsDisposed)
                    {
                        observer.OnCompleted();
                    }
                    return IntPtr.Zero;
                }

                void HwndSourceDisposedHandle(object sender, EventArgs e)
                {
                    observer.OnCompleted();
                }

                void RegisterHwndSource()
                {
                    hwndSource.Disposed += HwndSourceDisposedHandle;
                    hwndSource.AddHook(WindowMessageHandler);
                }

                if (window != null)
                {
                    hwndSource = window.ToHwndSource();
                }
                if (hwndSource != null)
                {
                    RegisterHwndSource();
                }
                else
                {
                    // No, try to get it later
                    window.SourceInitialized += (sender, args) =>
                    {
                        hwndSource = window.ToHwndSource();
                        RegisterHwndSource();
                    };
                }

                return Disposable.Create(() =>
                {
                    hwndSource.Disposed -= HwndSourceDisposedHandle;
                    hwndSource.RemoveHook(WindowMessageHandler);
                    hwndSource.Dispose();
                });
            })
                // Make sure there is always a value produced when connecting
                .Publish()
                .RefCount();
        }
    }
}
