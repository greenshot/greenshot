/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Plugin.Drawing;
using GreenshotPlugin.Core;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GreenshotEditorPlugin.Drawing.Filters
{
    [Serializable] 
	public class MagnifierFilter : AbstractFilter {

		protected int _magnificationFactor = 2;
		[Field(FieldTypes.MAGNIFICATION_FACTOR)]
		public int MagnificationFactor {
			get {
				return _magnificationFactor;
			}
			set {
				_magnificationFactor = value;
				OnFieldPropertyChanged(FieldTypes.MAGNIFICATION_FACTOR);
			}
		}

		public MagnifierFilter(DrawableContainer parent) : base(parent) {
		}

		public override void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode) {
			Rectangle applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);

			if (applyRect.Width == 0 || applyRect.Height == 0) {
				// nothing to do
				return;
			}
			GraphicsState state =  graphics.Save();
			if (Invert) {
				graphics.SetClip(applyRect);
				graphics.ExcludeClip(rect);
			}
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			int halfWidth = rect.Width / 2;
			int halfHeight = rect.Height / 2;
			int newWidth = rect.Width / _magnificationFactor;
			int newHeight = rect.Height / _magnificationFactor;
			Rectangle source = new Rectangle(rect.X + halfWidth - (newWidth / 2), rect.Y + halfHeight - (newHeight / 2), newWidth, newHeight);
			graphics.DrawImage(applyBitmap, rect, source, GraphicsUnit.Pixel);
			graphics.Restore(state);
		}
	}
}
