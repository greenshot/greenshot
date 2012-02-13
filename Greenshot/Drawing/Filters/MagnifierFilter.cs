/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using GreenshotPlugin.Core;

namespace Greenshot.Drawing.Filters {
	[Serializable()] 
	public class MagnifierFilter : AbstractFilter {
		
		private BitmapBuffer bbbSrc;
		private int magnificationFactor;
				
		public MagnifierFilter(DrawableContainer parent) : base(parent) {
			AddField(GetType(), FieldType.MAGNIFICATION_FACTOR, 2);
		}
		
		public override void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode) {
			magnificationFactor = GetFieldValueAsInt(FieldType.MAGNIFICATION_FACTOR);
			applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);
			
			bbbSrc = new BitmapBuffer(applyBitmap, applyRect);
			try {
				bbbSrc.Lock();
				base.Apply(graphics, applyBitmap, applyRect, renderMode);
			} finally {
				bbbSrc.Dispose();
				bbbSrc = null;
			}
		}
		
		protected override void IteratePixel(int x, int y) {
			int halfWidth = bbb.Size.Width/2;
			int halfHeight = bbb.Size.Height/2;
			int yDistanceFromCenter = halfHeight-y;
			int xDistanceFromCenter = halfWidth-x;
			Color color = bbbSrc.GetColorAt(halfWidth-xDistanceFromCenter/magnificationFactor,halfHeight-yDistanceFromCenter/magnificationFactor);
			bbb.SetColorAt(x, y, color);
		}
	}
	
}
