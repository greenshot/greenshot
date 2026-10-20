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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Interop;
using Dapplo.Windows.Common.Structs;
using Greenshot.Editor.Views;

namespace Greenshot.Editor.Controls
{
    public class ToolStripColorButton : ToolStripButton, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Color _selectedColor = Color.Transparent;

        public ToolStripColorButton()
        {
            Click += ColorButtonClick;
        }

        public Color SelectedColor
        {
            get { return _selectedColor; }
            set
            {
                _selectedColor = value;

                Brush brush;
                if (value != Color.Transparent)
                {
                    brush = new SolidBrush(value);
                }
                else
                {
                    brush = new HatchBrush(HatchStyle.Percent50, Color.White, Color.Gray);
                }

                if (Image != null)
                {
                    using Graphics graphics = Graphics.FromImage(Image);
                    graphics.FillRectangle(brush, new NativeRect(0, 13, 16, 3));
                }

                // cleanup GDI Object
                brush.Dispose();
                // The color is only drawn, a screen reader reads it from the description
                AccessibleDescription = ColorDescription.Describe(value);
                Invalidate();
            }
        }

        private void ColorButtonClick(object sender, EventArgs e)
        {
            var colorPickerWindow = new ColorPickerWindow
            {
                SelectedColor = SelectedColor
            };

            // Use the parent to make sure the dialog doesn't show on another window.
            new WindowInteropHelper(colorPickerWindow)
            {
                Owner = Parent.Parent.Handle
            };

            if (colorPickerWindow.ShowDialog() != true)
            {
                return;
            }

            if (colorPickerWindow.SelectedColor.Equals(SelectedColor))
            {
                return;
            }

            SelectedColor = colorPickerWindow.SelectedColor;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedColor)));
        }
    }
}