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
using Greenshot.Base.Core;
using Greenshot.Base.Effects;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// A form to set the resize settings
    /// </summary>
    public partial class ResizeSettingsForm : EditorForm
    {
        private readonly ResizeEffect _effect;
        private readonly string _valuePercent;
        private double _newWidth, _newHeight;

        public ResizeSettingsForm(ResizeEffect effect)
        {
            _effect = effect;
            InitializeComponent();
            var valuePixel = Language.GetString("editor_resize_pixel");
            _valuePercent = Language.GetString("editor_resize_percent");
            combobox_width.Items.Add(valuePixel);
            combobox_width.Items.Add(_valuePercent);
            combobox_width.SelectedItem = valuePixel;
            combobox_height.Items.Add(valuePixel);
            combobox_height.Items.Add(_valuePercent);
            combobox_height.SelectedItem = valuePixel;

            textbox_width.Text = effect.Width.ToString();
            textbox_height.Text = effect.Height.ToString();
            _newWidth = effect.Width;
            _newHeight = effect.Height;
            combobox_width.SelectedIndexChanged += Combobox_SelectedIndexChanged;
            combobox_height.SelectedIndexChanged += Combobox_SelectedIndexChanged;

            checkbox_aspectratio.Checked = effect.MaintainAspectRatio;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            const double tolerance = 3 * double.Epsilon;
            if (Math.Abs(_newWidth - _effect.Width) > tolerance || Math.Abs(_newHeight - _effect.Height) > tolerance)
            {
                _effect.Width = (int) _newWidth;
                _effect.Height = (int) _newHeight;
                _effect.MaintainAspectRatio = checkbox_aspectratio.Checked;
                DialogResult = DialogResult.OK;
            }
        }

        private static bool Validate(object sender)
        {
            if (sender is not TextBox textBox)
            {
                return true;
            }
            if (!double.TryParse(textBox.Text, out _))
            {
                textBox.BackColor = Color.Red;
                return false;
            }

            textBox.BackColor = Color.White;

            return true;
        }

        private void DisplayWidth()
        {
            double displayValue;
            if (_valuePercent.Equals(combobox_width.SelectedItem))
            {
                displayValue = _newWidth / _effect.Width * 100d;
            }
            else
            {
                displayValue = _newWidth;
            }

            textbox_width.Text = ((int) displayValue).ToString();
        }

        private void DisplayHeight()
        {
            double displayValue;
            if (_valuePercent.Equals(combobox_height.SelectedItem))
            {
                displayValue = _newHeight / _effect.Height * 100d;
            }
            else
            {
                displayValue = _newHeight;
            }

            textbox_height.Text = ((int) displayValue).ToString();
        }

        private void Textbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (!Validate(sender))
            {
                return;
            }

            TextBox textbox = sender as TextBox;
            if (string.IsNullOrEmpty(textbox?.Text))
            {
                return;
            }

            bool isWidth = textbox == textbox_width;
            if (!checkbox_aspectratio.Checked)
            {
                if (isWidth)
                {
                    _newWidth = double.Parse(textbox_width.Text);
                }
                else
                {
                    _newHeight = double.Parse(textbox_height.Text);
                }

                return;
            }

            var isPercent = _valuePercent.Equals(isWidth ? combobox_width.SelectedItem : combobox_height.SelectedItem);
            double percent;
            if (isWidth)
            {
                if (isPercent)
                {
                    percent = double.Parse(textbox_width.Text);
                    _newWidth = _effect.Width / 100d * percent;
                }
                else
                {
                    _newWidth = double.Parse(textbox_width.Text);
                    percent = double.Parse(textbox_width.Text) / _effect.Width * 100d;
                }

                if (checkbox_aspectratio.Checked)
                {
                    _newHeight = _effect.Height / 100d * percent;
                    DisplayHeight();
                }
            }
            else
            {
                if (isPercent)
                {
                    percent = double.Parse(textbox_height.Text);
                    _newHeight = _effect.Height / 100d * percent;
                }
                else
                {
                    _newHeight = double.Parse(textbox_height.Text);
                    percent = double.Parse(textbox_height.Text) / _effect.Height * 100d;
                }

                if (checkbox_aspectratio.Checked)
                {
                    _newWidth = _effect.Width / 100d * percent;
                    DisplayWidth();
                }
            }
        }

        private void Textbox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Validate(sender);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Combobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Validate(textbox_width))
            {
                DisplayWidth();
            }

            if (Validate(textbox_height))
            {
                DisplayHeight();
            }
        }
    }
}