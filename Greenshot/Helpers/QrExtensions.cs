/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace Greenshot.Helpers
{
    public static class QrExtensions
    {
        /// <summary>
        /// Find the bounding box for the Qr Result.
        /// </summary>
        /// <param name="result">Result</param>
        /// <returns>Rectangle</returns>
        public static Rectangle BoundingQrBox(this Result result)
        {
            var xValues = result.ResultPoints.Select(p => (int)p.X).ToList();
            int xMin = xValues.Min();
            int xMax = xValues.Max();

            var yValues = result.ResultPoints.Select(p => (int)p.Y).ToList();
            int yMin = yValues.Min();
            int yMax = yValues.Max();

            return new Rectangle(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
