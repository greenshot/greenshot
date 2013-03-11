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
using System.Drawing.Imaging;
using GreenshotPlugin.UnmanagedHelpers;
using System.Drawing.Drawing2D;

namespace Greenshot.Drawing.Filters {
	[Serializable()] 
	public class BlurFilter : AbstractFilter {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BlurFilter));

		public double previewQuality;
		public double PreviewQuality {
			get { return previewQuality; }
			set { previewQuality = value; OnPropertyChanged("PreviewQuality"); }
		}
		
		public BlurFilter(DrawableContainer parent) : base(parent) {
			AddField(GetType(), FieldType.BLUR_RADIUS, 3);
			AddField(GetType(), FieldType.PREVIEW_QUALITY, 1.0d);
		}

		public unsafe override void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode) {
			int blurRadius = GetFieldValueAsInt(FieldType.BLUR_RADIUS);
			double previewQuality = GetFieldValueAsDouble(FieldType.PREVIEW_QUALITY);
			Rectangle applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);
			GraphicsState state = graphics.Save();
			if (Invert) {
				graphics.SetClip(applyRect);
				graphics.ExcludeClip(rect);
			}
			if (GDIplus.isBlurPossible(blurRadius)) {
				GDIplus.DrawWithBlur(graphics, applyBitmap, applyRect, null, null, blurRadius, false);
			} else {
				using (IFastBitmap fastBitmap = FastBitmap.CreateCloneOf(applyBitmap, applyRect)) {
					ImageHelper.ApplyBoxBlur(fastBitmap, blurRadius);
					fastBitmap.DrawTo(graphics, applyRect);
				}
			}
			graphics.Restore(state);
			return;
		}
	}
}
