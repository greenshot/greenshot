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
	public class HighlightFilter : AbstractFilter {
		[NonSerialized]
		private Color highlightColor;
		
		public HighlightFilter(DrawableContainer parent) : base(parent) {
			AddField(GetType(), FieldType.FILL_COLOR, Color.Yellow);
		}
		
		protected override void IteratePixel(int x, int y) {
			Color color = bbb.GetColorAt(x, y);
			color = Color.FromArgb(color.A, Math.Min(highlightColor.R,color.R), Math.Min(highlightColor.G,color.G), Math.Min(highlightColor.B,color.B));
			bbb.SetColorAt(x, y, color);
		}

		public override void Apply(Graphics graphics, Bitmap bmp, Rectangle rect, RenderMode renderMode) {
			highlightColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
			base.Apply(graphics, bmp, rect, renderMode);
		}
	}
}
