/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.User32;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// This code was supplied by Hi-Coder as a patch for Greenshot
    /// Needed some modifications to be stable.
    /// </summary>
    public partial class MovableShowColorForm : Form
    {
        public Color Color
        {
            get { return preview.BackColor; }
        }

        public MovableShowColorForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Move the MovableShowColorForm to the specified location and display the color under the (current mouse) coordinates
        /// </summary>
        /// <param name="screenCoordinates">NativePoint with Coordinates</param>
        public void MoveTo(NativePoint screenCoordinates)
        {
            Color c = GetPixelColor(screenCoordinates);
            preview.BackColor = c;
            html.Text = "#" + c.Name.Substring(2).ToUpper();
            red.Text = string.Empty + c.R;
            blue.Text = string.Empty + c.B;
            green.Text = string.Empty + c.G;
            alpha.Text = string.Empty + c.A;

            NativeSize cursorSize = Cursor.Current.Size;
            NativePoint hotspot = Cursor.Current.HotSpot;

            var zoomerLocation = new NativePoint(screenCoordinates.X, screenCoordinates.Y)
                .Offset(cursorSize.Width + 5 - hotspot.X, cursorSize.Height + 5 - hotspot.Y);

            foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
            {
                NativeRect screenRectangle = displayInfo.Bounds;
                if (!displayInfo.Bounds.Contains(screenCoordinates)) continue;

                if (zoomerLocation.X < screenRectangle.X)
                {
                    zoomerLocation = zoomerLocation.ChangeX(screenRectangle.X);
                }
                else if (zoomerLocation.X + Width > screenRectangle.X + screenRectangle.Width)
                {
                    zoomerLocation = zoomerLocation.ChangeX(screenCoordinates.X - Width - 5 - hotspot.X);
                }

                if (zoomerLocation.Y < screenRectangle.Y)
                {
                    zoomerLocation = zoomerLocation.ChangeY(screenRectangle.Y);
                }
                else if (zoomerLocation.Y + Height > screenRectangle.Y + screenRectangle.Height)
                {
                    zoomerLocation = zoomerLocation.ChangeY(screenCoordinates.Y - Height - 5 - hotspot.Y);
                }

                break;
            }

            Location = zoomerLocation;
            Update();
        }

        /// <summary>
        /// Get the color from the pixel on the screen at "x,y"
        /// </summary>
        /// <param name="screenCoordinates">NativePoint with the coordinates</param>
        /// <returns>Color at the specified screenCoordinates</returns>
        private static Color GetPixelColor(NativePoint screenCoordinates)
        {
            using SafeWindowDcHandle safeWindowDcHandle = SafeWindowDcHandle.FromDesktop();
            try
            {
                uint pixel = Gdi32Api.GetPixel(safeWindowDcHandle, screenCoordinates.X, screenCoordinates.Y);
                Color color = Color.FromArgb(255, (int) (pixel & 0xFF), (int) (pixel & 0xFF00) >> 8, (int) (pixel & 0xFF0000) >> 16);
                return color;
            }
            catch (Exception)
            {
                return Color.Empty;
            }
        }
    }
}