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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Greenshot.Base.Controls;
using Dapplo.Windows.Common.Structs;
using ColorDialog = Greenshot.Editor.Forms.ColorDialog;

namespace Greenshot.Editor.Controls
{
    public class ToolStripColorButton : ToolStripButton, INotifyPropertyChanged, IIconDecorator
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
                if (Image != null)
                {
                    DecorateIcon(Image);
                }

                Invalidate();
            }
        }

        /// <summary>
        /// Draw the selected color as a bar at the bottom of the icon: the lowest 3 of 16 pixels, scaled with the icon
        /// </summary>
        /// <param name="icon">Image</param>
        public void DecorateIcon(Image icon)
        {
            if (icon == null)
            {
                return;
            }

            using Brush brush = _selectedColor != Color.Transparent
                ? new SolidBrush(_selectedColor)
                : new HatchBrush(HatchStyle.Percent50, Color.White, Color.Gray);
            int barHeight = Math.Max(1, (int)Math.Round(icon.Height * 3 / 16.0));
            using Graphics graphics = Graphics.FromImage(icon);
            graphics.FillRectangle(brush, new NativeRect(0, icon.Height - barHeight, icon.Width, barHeight));
        }

        private void ColorButtonClick(object sender, EventArgs e)
        {
            var colorDialog = new ColorDialog
            {
                Color = SelectedColor
            };
            // Using the parent to make sure the dialog doesn't show on another window
            colorDialog.ShowDialog(Parent.Parent);
            if (colorDialog.DialogResult == DialogResult.Cancel)
            {
                return;
            }

            if (colorDialog.Color.Equals(SelectedColor))
            {
                return;
            }

            SelectedColor = colorDialog.Color;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedColor"));
        }
    }
}