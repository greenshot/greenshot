/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Addon.Extensions
{
	/// <summary>
	/// Extensions for the Size struct
	/// </summary>
	public static class SizeExtentions
	{
		/// <summary>
		/// Scale a rectangle
		/// </summary>
		/// <param name="currentSize">SizeF</param>
		/// <param name="targetSize">SizeF</param>
		/// <param name="crop">bool to specify if a crop can take place</param>
		/// <returns>SizeF</returns>
		public static SizeF Scale(this SizeF currentSize, SizeF targetSize, bool crop)
		{
			float wFactor = targetSize.Width/currentSize.Width;
			float hFactor = targetSize.Height/currentSize.Height;

			float factor = crop ? Math.Max(wFactor, hFactor) : Math.Min(wFactor, hFactor);
			return new SizeF(currentSize.Width*factor, currentSize.Height*factor);
		}
	}
}