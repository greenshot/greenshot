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
using GreenshotPlugin.UnmanagedHelpers;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GreenshotEditorPlugin.Drawing.Filters
{
    [Serializable] 
	public class BlurFilter : AbstractFilter {
		private int _blurRadius = 3;
		[Field(FieldTypes.BLUR_RADIUS)]
		public int BlurRadius {
			get {
				return _blurRadius;
			}
			set {
				_blurRadius = value;
				OnFieldPropertyChanged(FieldTypes.BLUR_RADIUS);
			}
		}
		public BlurFilter(DrawableContainer parent) : base(parent) {
		}

		public override void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode) {
			Rectangle applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);
			if (applyRect.Width == 0 || applyRect.Height == 0) {
				return;
			}
			var state = graphics.Save();
			if (Invert) {
				graphics.SetClip(applyRect);
				graphics.ExcludeClip(rect);
			}
			if (GDIplus.IsBlurPossible(_blurRadius)) {
				GDIplus.DrawWithBlur(graphics, applyBitmap, applyRect, null, null, _blurRadius, false);
			} else {
				using (IFastBitmap fastBitmap = FastBitmap.CreateCloneOf(applyBitmap, applyRect)) {
					ImageHelper.ApplyBoxBlur(fastBitmap, _blurRadius);
					fastBitmap.DrawTo(graphics, applyRect);
				}
			}
			graphics.Restore(state);
		}
	}
}
