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
using Autofac.Features.OwnedInstances;
using Greenshot.Addons.Controls;

namespace Greenshot.Addon.LegacyEditor.Controls
{
	/// <summary>
	///     Description of ColorButton.
	/// </summary>
	public class ColorButton : Button, IGreenshotLanguageBindable
	{
	    private readonly Func<Owned<ColorDialog>> _colorDialogFactory;
	    private Color _selectedColor = Color.White;

		public ColorButton(Func<Owned<ColorDialog>> colorDialogFactory)
		{
		    _colorDialogFactory = colorDialogFactory;
		    Click += ColorButtonClick;
		}

		/// <summary>
		/// The color which is selected
		/// </summary>
		public Color SelectedColor
		{
			get { return _selectedColor; }
			set
			{
				_selectedColor = value;

				if (Image != null)
				{
					using (var brush = value != Color.Transparent ? new SolidBrush(value) : (Brush) new HatchBrush(HatchStyle.Percent50, Color.White, Color.Gray))
					{
						using (var graphics = Graphics.FromImage(Image))
						{
							int verticalOffset = Image.Height / 3;
							int horizontalOffset = (Image.Width / 3) / 2;
							int width = Image.Width - (Image.Width / 3);
							int height = (Image.Height / 3) / 2;
							graphics.FillRectangle(brush, new Rectangle(horizontalOffset, verticalOffset, width, height));
						}
					}
				}
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
            using (var ownedColorDialog = _colorDialogFactory())
            {
                var colorDialog = ownedColorDialog.Value;
                colorDialog.Color = SelectedColor;
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
}