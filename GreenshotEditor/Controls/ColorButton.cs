/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Windows.Forms;
using Greenshot.Configuration;

namespace Greenshot.Controls {
	public class ColorButton : ToolStripButton, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		private Color selectedColor = Color.Transparent;
		
		public ColorButton() {
			Bitmap bmp = new Bitmap(1,1);
			bmp.SetPixel(0,0, selectedColor);
			this.Image = bmp;
			this.BackgroundImageLayout = ImageLayout.Tile;
			Click+= new EventHandler(ColorButtonClick);
		}

		public Color SelectedColor {
			get {return selectedColor;}
			set {
				//(Image as Bitmap).SetPixel(0,0, value);
				selectedColor = value;

				Brush brush;
				if(value != Color.Transparent) {
					brush = new SolidBrush(value);
				} else {
					brush = new HatchBrush(HatchStyle.Percent50,Color.White, Color.Gray);
				}

				using (Graphics graphics = Graphics.FromImage(Image)) {
					graphics.FillRectangle(brush, new Rectangle(0,13,16,3));
				}
				// cleanup GDI Object
				brush.Dispose();
				Invalidate();
			}
		}

		void ColorButtonClick(object sender, EventArgs e) {
			
			ColorDialog colorDialog = ColorDialog.GetInstance();
			colorDialog.Color = SelectedColor;
			colorDialog.ShowDialog();
			if (colorDialog.DialogResult != DialogResult.Cancel) {
				if(!colorDialog.Color.Equals(SelectedColor)) {
					AppConfig conf = AppConfig.GetInstance();
					conf.Editor_RecentColors = colorDialog.RecentColors;
					conf.Store();
					SelectedColor = colorDialog.Color;
					if(PropertyChanged != null) {
						PropertyChanged(this, new PropertyChangedEventArgs("SelectedColor"));
					}
				}
			}
		}
	}
}
