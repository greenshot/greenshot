/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Helpers
{
    /// <summary>
    /// Helps with a crop out: the strip is removed and the two parts are joined, the cut mark draws the edges
    /// </summary>
    public static class CutOutHelper
    {
        /// <summary>
        /// Cut out a strip of the image and join the parts before and after it seamlessly
        /// </summary>
        /// <param name="image">Image to cut</param>
        /// <param name="cutStart">int first row (horizontal) or column which is cut out</param>
        /// <param name="cutSize">int number of rows or columns which are cut out</param>
        /// <param name="horizontal">true when rows are cut out, false for columns</param>
        /// <returns>Bitmap</returns>
        public static Bitmap CutOut(Image image, int cutStart, int cutSize, bool horizontal)
        {
            int length = horizontal ? image.Height : image.Width;
            int breadth = horizontal ? image.Width : image.Height;
            int afterLength = length - cutStart - cutSize;
            var result = new Bitmap(horizontal ? breadth : length - cutSize, horizontal ? length - cutSize : breadth, PixelFormat.Format32bppArgb);
            result.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using var graphics = Graphics.FromImage(result);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            if (cutStart > 0)
            {
                var before = horizontal ? new Rectangle(0, 0, breadth, cutStart) : new Rectangle(0, 0, cutStart, breadth);
                graphics.DrawImage(image, before, before, GraphicsUnit.Pixel);
            }

            if (afterLength > 0)
            {
                var source = horizontal ? new Rectangle(0, cutStart + cutSize, breadth, afterLength) : new Rectangle(cutStart + cutSize, 0, afterLength, breadth);
                var target = horizontal ? new Rectangle(0, cutStart, breadth, afterLength) : new Rectangle(cutStart, 0, afterLength, breadth);
                graphics.DrawImage(image, target, source, GraphicsUnit.Pixel);
            }

            return result;
        }

        /// <summary>
        /// Creates an edge along the cut, X is the position along the cut from 0 to length, Y how deep the edge goes into the part (0 up to toothHeight)
        /// </summary>
        /// <param name="style">CutMarkStyle</param>
        /// <param name="length">int length of the cut</param>
        /// <param name="toothHeight">int how deep the edge goes into the part</param>
        /// <param name="toothRange">int how wide a tooth / wave is</param>
        /// <param name="random">Random for the torn style</param>
        /// <returns>PointF list from X=0 up to X=length</returns>
        public static IList<PointF> CreateEdge(CutMarkStyle style, int length, int toothHeight, int toothRange, Random random)
        {
            toothHeight = Math.Max(1, toothHeight);
            toothRange = Math.Max(2, toothRange);
            var points = new List<PointF>();
            switch (style)
            {
                case CutMarkStyle.ZigZag:
                    for (int i = 0; ; i++)
                    {
                        float along = Math.Min(i * toothRange / 2f, length);
                        points.Add(new PointF(along, i % 2 == 0 ? 0 : toothHeight));
                        if (along >= length) break;
                    }
                    break;
                case CutMarkStyle.Wave:
                    for (float along = 0; ; along += 2)
                    {
                        float clamped = Math.Min(along, length);
                        points.Add(new PointF(clamped, toothHeight / 2f * (1 + (float)Math.Sin(Math.PI * clamped / toothRange))));
                        if (along >= length) break;
                    }
                    break;
                case CutMarkStyle.Torn:
                    // Like the torn edge effect: a random depth at every tooth
                    for (int i = 0; ; i++)
                    {
                        float along = Math.Min(i * toothRange, length);
                        points.Add(new PointF(along, random.Next(1, toothHeight + 1)));
                        if (along >= length) break;
                    }
                    break;
                default:
                    points.Add(new PointF(0, 0));
                    points.Add(new PointF(length, 0));
                    break;
            }

            return points;
        }
    }
}
