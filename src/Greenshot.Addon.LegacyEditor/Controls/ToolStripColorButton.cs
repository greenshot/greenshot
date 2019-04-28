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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Greenshot.Addons;
using Greenshot.Addons.Controls;

namespace Greenshot.Addon.LegacyEditor.Controls
{
	public class ToolStripColorButton : ToolStripButton, INotifyPropertyChanged, IGreenshotLanguageBindable
	{
	    private readonly IEditorConfiguration _editorConfiguration;
	    private readonly IGreenshotLanguage _greenshotLanguage;
	    private Color _selectedColor = Color.Transparent;

		public ToolStripColorButton(
            IEditorConfiguration editorConfiguration,
		    IGreenshotLanguage greenshotLanguage)
		{
		    _editorConfiguration = editorConfiguration;
		    _greenshotLanguage = greenshotLanguage;
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
					using (var graphics = Graphics.FromImage(Image))
					{
						int quarterSize = Image.Height / 4;
						var colorArea = new Rectangle(0, Image.Height - quarterSize, Image.Width, quarterSize);
						graphics.FillRectangle(brush, colorArea);
					}
				}

				// cleanup GDI Object
				brush.Dispose();
				Invalidate();
			}
		}

		[Category("Greenshot")]
		[DefaultValue(null)]
		[Description("Specifies key of the language file to use when displaying the text.")]
		public string LanguageKey { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		private void ColorButtonClick(object sender, EventArgs e)
		{
			var colorDialog = new ColorDialog(_editorConfiguration, _greenshotLanguage)
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