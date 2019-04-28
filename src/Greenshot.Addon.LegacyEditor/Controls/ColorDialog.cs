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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Addons;
using Greenshot.Addons.Controls;

namespace Greenshot.Addon.LegacyEditor.Controls
{
	/// <summary>
	///     Description of ColorDialog.
	/// </summary>
	public partial class ColorDialog : GreenshotForm
	{
	    private readonly IEditorConfiguration _editorConfiguration;
	    private readonly ToolTip _toolTip = new ToolTip();
		private bool _updateInProgress;

        public ColorDialog(
            IEditorConfiguration editorConfiguration,
            IGreenshotLanguage greenshotLanguage) : base(greenshotLanguage)
		{
		    _editorConfiguration = editorConfiguration;
		    SuspendLayout();
            InitializeComponent();
            ResumeLayout();
		    Load += (sender, args) => DrawButtons();
        }

        private void DrawButtons()
	    {
            int size = FormDpiHandler.ScaleWithCurrentDpi(15);
	        var buttons = CreateColorPalette(size, size);
	        var recentColorButtons = CreateLastUsedColorButtonRow(size, size);

            SuspendLayout();
	        panelColors.Controls.Clear();
            panelColors.Controls.AddRange(buttons.ToArray());
	        panelRecentColors.Controls.Clear();
            panelRecentColors.Controls.AddRange(recentColorButtons.ToArray());
	        UpdateRecentColorsButtonRow();
            ResumeLayout();
        }

		public Color Color
		{
			get => colorPanel.BackColor;
		    set => PreviewColor(value, this);
		}

        private int GetColorPartIntFromString(string s)
		{
		    int.TryParse(s, out var ret);
			if (ret < 0)
			{
				ret = 0;
			}
			else if (ret > 255)
			{
				ret = 255;
			}
			return ret;
		}

        private void PipetteUsed(object sender, PipetteUsedArgs e)
		{
			Color = e.Color;
		}

        private IList<Control> CreateColorPalette(int w, int h)
		{
		    int x = 0;
		    int y = 0;

            IList<Control> colorButtons = new List<Control>();
            CreateColorButtonColumn(colorButtons, 255, 0, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 255, 255 / 2, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 255, 255, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 255 / 2, 255, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 0, 255, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 0, 255, 255 / 2, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 0, 255, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 0, 255 / 2, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 0, 0, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 255 / 2, 0, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 255, 0, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(colorButtons, 255, 0, 255 / 2, x, y, w, h, 11);
			x += w + 5;
			CreateColorButtonColumn(colorButtons, 255 / 2, 255 / 2, 255 / 2, x, y, w, h, 11);

		    return colorButtons;
		}

		private void CreateColorButtonColumn(IList<Control> colorButtons, int red, int green, int blue, int x, int y, int w, int h, int shades)
		{
			var shadedColorsNum = (shades - 1) / 2;
			for (var i = 0; i <= shadedColorsNum; i++)
			{
				colorButtons.Add(CreateColorButton(red * i / shadedColorsNum, green * i / shadedColorsNum, blue * i / shadedColorsNum, x, y + i * h, w, h));
				if (i > 0)
				{
					colorButtons.Add(CreateColorButton(red + (255 - red) * i / shadedColorsNum, green + (255 - green) * i / shadedColorsNum, blue + (255 - blue) * i / shadedColorsNum,
						x, y + (i + shadedColorsNum) * h, w, h));
				}
			}
		}

		private Button CreateColorButton(int red, int green, int blue, int x, int y, int w, int h)
		{
			return CreateColorButton(Color.FromArgb(255, red, green, blue), x, y, w, h);
		}

		private Button CreateColorButton(Color color, int x, int y, int w, int h)
		{
			var b = new Button
			{
				BackColor = color,
				FlatStyle = FlatStyle.Flat,
				Location = new Point(x, y),
				Size = new Size(w, h),
				TabStop = false
			};
			b.FlatAppearance.BorderSize = 0;
			b.Click += ColorButtonClick;
			_toolTip.SetToolTip(b, ColorTranslator.ToHtml(color) + " | R:" + color.R + ", G:" + color.G + ", B:" + color.B);
			return b;
		}

		private IList<Control> CreateLastUsedColorButtonRow(int w, int h)
		{
		    int x = 0;
		    int y = 0;

            IList<Control> recentColorButtons = new List<Control>();

            for (var i = 0; i < 12; i++)
			{
				var b = CreateColorButton(Color.Transparent, x, y, w, h);
				b.Enabled = false;
				recentColorButtons.Add(b);
				x += w;
			}

		    return recentColorButtons;
		}

        private void UpdateRecentColorsButtonRow()
		{
			for (var i = 0; i < _editorConfiguration.RecentColors.Count && i < 12; i++)
			{
				panelRecentColors.Controls[i].BackColor = _editorConfiguration.RecentColors[i];
			    panelRecentColors.Controls[i].Enabled = true;
			}
		}

		private void PreviewColor(Color colorToPreview, Control trigger)
		{
			_updateInProgress = true;
			colorPanel.BackColor = colorToPreview;
			if (trigger != textBoxHtmlColor)
			{
				textBoxHtmlColor.Text = ColorTranslator.ToHtml(colorToPreview);
			}
			if (trigger != textBoxRed && trigger != textBoxGreen && trigger != textBoxBlue && trigger != textBoxAlpha)
			{
				textBoxRed.Text = colorToPreview.R.ToString();
				textBoxGreen.Text = colorToPreview.G.ToString();
				textBoxBlue.Text = colorToPreview.B.ToString();
				textBoxAlpha.Text = colorToPreview.A.ToString();
			}
			_updateInProgress = false;
		}

		private void AddToRecentColors(Color c)
		{
		    _editorConfiguration.RecentColors.Remove(c);
		    _editorConfiguration.RecentColors.Insert(0, c);
			if (_editorConfiguration.RecentColors.Count > 12)
			{
			    _editorConfiguration.RecentColors = _editorConfiguration.RecentColors.Skip(_editorConfiguration.RecentColors.Count - 12).ToList();
			}
			UpdateRecentColorsButtonRow();
		}

        private void TextBoxHexadecimalTextChanged(object sender, EventArgs e)
		{
			if (_updateInProgress)
			{
				return;
			}
			var textBox = (TextBox) sender;
			var text = textBox.Text.Replace("#", "");
		    Color c;
			if (int.TryParse(text, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture, out var i))
			{
				c = Color.FromArgb(i);
			}
			else
			{
				try
				{
					var knownColor = (KnownColor) Enum.Parse(typeof(KnownColor), text, true);
					c = Color.FromKnownColor(knownColor);
				}
				catch (Exception)
				{
					return;
				}
			}
			var opaqueColor = Color.FromArgb(255, c.R, c.G, c.B);
			PreviewColor(opaqueColor, textBox);
		}

		private void TextBoxRgbTextChanged(object sender, EventArgs e)
		{
			if (_updateInProgress)
			{
				return;
			}
			var textBox = (TextBox) sender;
			PreviewColor(
				Color.FromArgb(GetColorPartIntFromString(textBoxAlpha.Text), GetColorPartIntFromString(textBoxRed.Text), GetColorPartIntFromString(textBoxGreen.Text),
					GetColorPartIntFromString(textBoxBlue.Text)), textBox);
		}

		private void TextBoxGotFocus(object sender, EventArgs e)
		{
			textBoxHtmlColor.SelectAll();
		}

		private void TextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
			{
				AddToRecentColors(colorPanel.BackColor);
			}
		}

        private void ColorButtonClick(object sender, EventArgs e)
		{
			var b = (Button) sender;
			PreviewColor(b.BackColor, b);
		}

		private void BtnTransparentClick(object sender, EventArgs e)
		{
			ColorButtonClick(sender, e);
		}

		private void BtnApplyClick(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Hide();
			AddToRecentColors(colorPanel.BackColor);
		}
    }
}