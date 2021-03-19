﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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

using System.Drawing;
using System.Drawing.Drawing2D;
using GreenshotPlugin.Core;

namespace GreenshotPlugin.Effects
{
	/// <summary>
	/// MonochromeEffect
	/// </summary>
	public class MonochromeEffect : IEffect {
		private readonly byte _threshold;
		/// <param name="threshold">Threshold for monochrome filter (0 - 255), lower value means less black</param>
		public MonochromeEffect(byte threshold) {
			_threshold = threshold;
		}
		public void Reset() {
			// TODO: Modify the threshold to have a default, which is reset here
		}
		public Image Apply(Image sourceImage, Matrix matrix) {
			return ImageHelper.CreateMonochrome(sourceImage, _threshold);
		}
	}
}