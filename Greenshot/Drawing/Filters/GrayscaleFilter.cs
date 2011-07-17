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
using System.Drawing.Imaging;
using System.Runtime.Serialization;

using Greenshot.Plugin.Drawing;

namespace Greenshot.Drawing.Filters {
	/// <summary>
	/// Description of GrayscaleFilter.
	/// </summary>
	[Serializable()] 
	public class GrayscaleFilter : AbstractFilter {
		public GrayscaleFilter(DrawableContainer parent) : base(parent) {
		}
		
		protected override void IteratePixel(int x, int y) {
			Color color = bbb.GetColorAt(x, y);
			int luma  = (int)((0.3*color.R) + (0.59*color.G) + (0.11*color.B));
			color = Color.FromArgb(luma, luma, luma);
			bbb.SetColorAt(x, y, color);
		}
	}
}
