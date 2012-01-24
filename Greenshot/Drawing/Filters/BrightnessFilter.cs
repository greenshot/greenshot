/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Drawing.Fields;
using Greenshot.Plugin.Drawing;

namespace Greenshot.Drawing.Filters {
	[Serializable()] 
	public class BrightnessFilter : AbstractFilter {
		
		private double brightness;
		
		public BrightnessFilter(DrawableContainer parent) : base(parent) {
			AddField(GetType(), FieldType.BRIGHTNESS, 0.9d);
		}
		
		protected override void IteratePixel(int x, int y) {
			Color color = bbb.GetColorAt(x, y);
			int r = Convert.ToInt16(color.R*brightness);
			int g = Convert.ToInt16(color.G*brightness);
			int b = Convert.ToInt16(color.B*brightness);
			r = (r>255) ? 255 : r;
			g = (g>255) ? 255 : g;
			b = (b>255) ? 255 : b;
			bbb.SetColorAt(x, y, Color.FromArgb(color.A, r, g, b));
		}
		
		public override void Apply(Graphics graphics, Bitmap bmp, Rectangle rect, RenderMode renderMode) {
			brightness = GetFieldValueAsDouble(FieldType.BRIGHTNESS);
			base.Apply(graphics, bmp, rect, renderMode);
		}
	}
}
