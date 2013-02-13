/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
	[Serializable] 
	public class MagnifierFilter : AbstractFilter {
		public MagnifierFilter(DrawableContainer parent) : base(parent) {
			AddField(GetType(), FieldType.MAGNIFICATION_FACTOR, 2);
		}

		public override void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode) {
			Rectangle applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);

			if (applyRect.Width == 0 || applyRect.Height == 0) {
				// nothing to do
				return;
			}
			int magnificationFactor = GetFieldValueAsInt(FieldType.MAGNIFICATION_FACTOR);

			using (BitmapBuffer bbb = new BitmapBuffer(applyBitmap, applyRect)) {
				int halfWidth = bbb.Size.Width / 2;
				int halfHeight = bbb.Size.Height / 2;
				bbb.Lock();
				using (BitmapBuffer bbbSrc = new BitmapBuffer(applyBitmap, applyRect)) {
					for (int y = 0; y < bbb.Height; y++) {
						int yDistanceFromCenter = halfHeight - y;
						for (int x = 0; x < bbb.Width; x++) {
							int xDistanceFromCenter = halfWidth - x;
							if (parent.Contains(applyRect.Left + x, applyRect.Top + y) ^ Invert) {
								Color color = bbbSrc.GetColorAt(halfWidth - xDistanceFromCenter / magnificationFactor, halfHeight - yDistanceFromCenter / magnificationFactor);
								bbb.SetColorAt(x, y, color);
							}
						}
					}
				}
				bbb.DrawTo(graphics, applyRect.Location);
			}
		}
	}
}
