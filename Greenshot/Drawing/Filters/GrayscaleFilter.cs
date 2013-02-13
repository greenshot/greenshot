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
using GreenshotPlugin.Core;
using Greenshot.Plugin.Drawing;

namespace Greenshot.Drawing.Filters {
	/// <summary>
	/// Description of GrayscaleFilter.
	/// </summary>
	[Serializable()] 
	public class GrayscaleFilter : AbstractFilter {
		public GrayscaleFilter(DrawableContainer parent) : base(parent) {
		}

		public override void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode) {
			Rectangle applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);

			if (applyRect.Width == 0 || applyRect.Height == 0) {
				// nothing to do
				return;
			}

			using (IFastBitmap fastBitmap = FastBitmap.CreateCloneOf(applyBitmap, applyRect)) {
				for (int y = 0; y < fastBitmap.Height; y++) {
					for (int x = 0; x < fastBitmap.Width; x++) {
						if (parent.Contains(applyRect.Left + x, applyRect.Top + y) ^ Invert) {
							Color color = fastBitmap.GetColorAt(x, y);
							int luma = (int)((0.3 * color.R) + (0.59 * color.G) + (0.11 * color.B));
							color = Color.FromArgb(luma, luma, luma);
							fastBitmap.SetColorAt(x, y, color);
						}
					}
				}
				fastBitmap.DrawTo(graphics, applyRect.Location);
			}
		}
	}
}
