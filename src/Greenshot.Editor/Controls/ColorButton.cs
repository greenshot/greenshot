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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Controls;
using ColorDialog = Greenshot.Editor.Forms.ColorDialog;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// Description of ColorButton.
    /// </summary>
    public class ColorButton : Button, IGreenshotLanguageBindable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private Color _selectedColor = Color.White;

        [Category("Greenshot"), DefaultValue(null), Description("Specifies key of the language file to use when displaying the text.")]
        public string LanguageKey { get; set; }

        public ColorButton()
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
                    graphics.FillRectangle(brush, new NativeRect(4, 17, 16, 3));
                }

                // cleanup GDI Object
                brush.Dispose();
                Invalidate();
            }
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