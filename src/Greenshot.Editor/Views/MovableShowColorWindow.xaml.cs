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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Color = System.Drawing.Color;

namespace Greenshot.Editor.Views
{
    /// <summary>
    /// A small, non-activating WPF window showing the color below the cursor.
    /// </summary>
    public partial class MovableShowColorWindow : System.Windows.Window
    {
        private const int WindowWidth = 100;
        private const int WindowHeight = 100;
        private Color _color;

        public Color Color => _color;

        public MovableShowColorWindow()
        {
            InitializeComponent();

            // Create the native window now so MoveTo can position it in physical screen pixels,
            // just like the former WinForms window. These styles also keep it out of Alt+Tab and
            // ensure that showing the pipette preview never steals focus from the captured app.
            var handle = new WindowInteropHelper(this).EnsureHandle();
            var extendedStyle = (ExtendedWindowStyleFlags)User32Api.GetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE);
            extendedStyle |= ExtendedWindowStyleFlags.WS_EX_NOACTIVATE | ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW;
            User32Api.SetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)extendedStyle));
        }

        /// <summary>
        /// Move the preview beside the specified screen coordinate and display the color at it.
        /// Screen coordinates and display bounds are physical pixels.
        /// </summary>
        public void MoveTo(NativePoint screenCoordinates)
        {
            _color = GetPixelColor(screenCoordinates);
            Preview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(_color.A, _color.R, _color.G, _color.B));
            Html.Text = $"#{_color.R:X2}{_color.G:X2}{_color.B:X2}";
            Red.Text = _color.R.ToString();
            Green.Text = _color.G.ToString();
            Blue.Text = _color.B.ToString();
            Alpha.Text = _color.A.ToString();

            var cursor = System.Windows.Forms.Cursor.Current;
            NativeSize cursorSize = cursor.Size;
            NativePoint hotspot = cursor.HotSpot;
            var location = new NativePoint(screenCoordinates.X, screenCoordinates.Y)
                .Offset(cursorSize.Width + 5 - hotspot.X, cursorSize.Height + 5 - hotspot.Y);

            foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
            {
                NativeRect screenRectangle = displayInfo.Bounds;
                if (!screenRectangle.Contains(screenCoordinates))
                {
                    continue;
                }

                if (location.X < screenRectangle.X)
                {
                    location = location.ChangeX(screenRectangle.X);
                }
                else if (location.X + WindowWidth > screenRectangle.X + screenRectangle.Width)
                {
                    location = location.ChangeX(screenCoordinates.X - WindowWidth - 5 - hotspot.X);
                }

                if (location.Y < screenRectangle.Y)
                {
                    location = location.ChangeY(screenRectangle.Y);
                }
                else if (location.Y + WindowHeight > screenRectangle.Y + screenRectangle.Height)
                {
                    location = location.ChangeY(screenCoordinates.Y - WindowHeight - 5 - hotspot.Y);
                }

                break;
            }

            if (!IsVisible)
            {
                Show();
            }

            var handle = new WindowInteropHelper(this).Handle;
            UpdateLayout();
            User32Api.SetWindowPos(handle, IntPtr.Zero, location.X, location.Y, 0, 0,
                WindowPos.SWP_NOSIZE | WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);
        }

        /// <summary>
        /// Get the color from the pixel on the screen at the specified coordinates.
        /// </summary>
        private static Color GetPixelColor(NativePoint screenCoordinates)
        {
            using SafeWindowDcHandle safeWindowDcHandle = SafeWindowDcHandle.FromDesktop();
            try
            {
                uint pixel = Gdi32Api.GetPixel(safeWindowDcHandle, screenCoordinates.X, screenCoordinates.Y);
                return Color.FromArgb(255, (int)(pixel & 0xFF), (int)(pixel & 0xFF00) >> 8, (int)(pixel & 0xFF0000) >> 16);
            }
            catch (Exception)
            {
                return Color.Empty;
            }
        }
    }
}
