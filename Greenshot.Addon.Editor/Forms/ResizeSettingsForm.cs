/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Config.Language;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;

namespace Greenshot.Addon.Editor.Forms
{
	/// <summary>
	/// A form to set the resize settings
	/// </summary>
	public partial class ResizeSettingsForm : BaseForm
	{
		private static readonly IEditorLanguage Language = LanguageLoader.Current.Get<IGreenshotLanguage>();

		private ResizeEffect _effect;
		private readonly string _valuePercent;
		private double _newWidth, _newHeight;

		public ResizeSettingsForm(ResizeEffect effect)
		{
			_effect = effect;
			InitializeComponent();
			string valuePixel = Language.EditorResizePixel;
			_valuePercent = Language.EditorResizePercent;
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
			combobox_width.SelectedIndexChanged += combobox_SelectedIndexChanged;
			combobox_height.SelectedIndexChanged += combobox_SelectedIndexChanged;

			checkbox_aspectratio.Checked = effect.MaintainAspectRatio;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (_newWidth != _effect.Width || _newHeight != _effect.Height)
			{
				_effect.Width = (int) _newWidth;
				_effect.Height = (int) _newHeight;
				_effect.MaintainAspectRatio = checkbox_aspectratio.Checked;
				DialogResult = DialogResult.OK;
			}
		}

		private bool validate(object sender)
		{
			TextBox textbox = sender as TextBox;
			if (textbox != null)
			{
				double numberEntered;
				if (!double.TryParse(textbox.Text, out numberEntered))
				{
					textbox.BackColor = Color.Red;
					return false;
				}
				else
				{
					textbox.BackColor = Color.White;
				}
			}
			return true;
		}

		private void DisplayWidth()
		{
			double displayValue;
			if (_valuePercent.Equals(combobox_width.SelectedItem))
			{
				displayValue = (_newWidth/_effect.Width)*100d;
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
				displayValue = (_newHeight/_effect.Height)*100d;
			}
			else
			{
				displayValue = _newHeight;
			}
			textbox_height.Text = ((int) displayValue).ToString();
		}

		private void textbox_KeyUp(object sender, KeyEventArgs e)
		{
			if (!validate(sender))
			{
				return;
			}
			TextBox textbox = sender as TextBox;
			if (textbox == null || textbox.Text.Length == 0)
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
			bool isPercent;
			if (isWidth)
			{
				isPercent = _valuePercent.Equals(combobox_width.SelectedItem);
			}
			else
			{
				isPercent = _valuePercent.Equals(combobox_height.SelectedItem);
			}
			double percent;
			if (isWidth)
			{
				if (isPercent)
				{
					percent = double.Parse(textbox_width.Text);
					_newWidth = (_effect.Width/100d)*percent;
				}
				else
				{
					_newWidth = double.Parse(textbox_width.Text);
					percent = (double.Parse(textbox_width.Text)/_effect.Width)*100d;
				}
				if (checkbox_aspectratio.Checked)
				{
					_newHeight = (_effect.Height/100d)*percent;
					DisplayHeight();
				}
			}
			else
			{
				if (isPercent)
				{
					percent = double.Parse(textbox_height.Text);
					_newHeight = (_effect.Height/100d)*percent;
				}
				else
				{
					_newHeight = double.Parse(textbox_height.Text);
					percent = (double.Parse(textbox_height.Text)/_effect.Height)*100d;
				}
				if (checkbox_aspectratio.Checked)
				{
					_newWidth = (_effect.Width/100d)*percent;
					DisplayWidth();
				}
			}
		}

		private void textbox_Validating(object sender, CancelEventArgs e)
		{
			validate(sender);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void combobox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (validate(textbox_width))
			{
				DisplayWidth();
			}
			if (validate(textbox_height))
			{
				DisplayHeight();
			}
		}
	}
}