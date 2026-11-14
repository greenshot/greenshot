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
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Recipes.ViewModels;

namespace Greenshot.Recipes.Views
{
    /// <summary>
    /// The countdown of the Delay step, at the bottom right of the display with the mouse. It never takes the focus, so a
    /// menu or popup the user opens stays open, and it is not in Alt+Tab.
    /// </summary>
    public partial class DelayCountdownWindow : Window
    {
        /// <summary>
        /// The distance to the edges of the work area, in device independent pixels
        /// </summary>
        private const double EdgeDistance = 16;

        private readonly DelayCountdownViewModel _viewModel;

        /// <param name="seconds">The seconds of the countdown</param>
        /// <param name="cancel">Called when the user clicks Cancel</param>
        public DelayCountdownWindow(int seconds, Action cancel)
        {
            InitializeComponent();
            // The step closes the window when it ends
            _viewModel = new DelayCountdownViewModel(seconds, cancel);
            DataContext = _viewModel;

            var handle = new WindowInteropHelper(this).EnsureHandle();
            var extendedStyle = (ExtendedWindowStyleFlags)User32Api.GetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE);
            extendedStyle |= ExtendedWindowStyleFlags.WS_EX_NOACTIVATE | ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW;
            User32Api.SetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)extendedStyle));
        }

        /// <summary>
        /// The seconds shown
        /// </summary>
        public int SecondsLeft
        {
            get => _viewModel.SecondsLeft;
            set => _viewModel.SecondsLeft = value;
        }

        /// <summary>
        /// Show the countdown at the bottom right of the work area of the display with the mouse, without taking the focus
        /// </summary>
        public void ShowWithoutFocus()
        {
            Show();
            UpdateLayout();
            var dpi = VisualTreeHelper.GetDpi(this);
            var workArea = Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
            int width = (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX);
            int height = (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY);
            int x = workArea.Right - width - (int)(EdgeDistance * dpi.DpiScaleX);
            int y = workArea.Bottom - height - (int)(EdgeDistance * dpi.DpiScaleY);
            User32Api.SetWindowPos(new WindowInteropHelper(this).Handle, IntPtr.Zero, x, y, 0, 0,
                WindowPos.SWP_NOSIZE | WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);
            _viewModel.Announce();
        }
    }
}
