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

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of DrawingHelper.
	/// </summary>
	public class DrawingHelper {
		private DrawingHelper() {
		}
		
		/// <summary>
		/// Perpendicular distance to line is calculated using Analytical Geometry
		/// </summary>
		/// <returns>Distance to line or -1 if not between the points"</returns>
		public static double CalculateLinePointDistance(double x1, double y1, double x2, double y2, double px, double py) {
			double deltaX = x2 - x1;
			double deltaY = y2 - y1;
			double delta = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
			double ratio = (double) ((px - x1) * deltaX + (py - y1) * deltaY) / (deltaX * deltaX + deltaY * deltaY);
			if (ratio * (1 - ratio) < 0) {
				return -1;
			}
			return (double) Math.Abs(deltaX * (py - y1) - deltaY * (px - x1)) / delta;
		}
	}
}
