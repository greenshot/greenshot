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
using System.Linq;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Helpers
{
    /// <summary>
    /// Creates the image for a crop out: the strip is removed and the two parts are joined.
    /// With a style other than None the parts get edges of that style, with a transparent gap and the shadow of the torn edge effect between them.
    /// </summary>
    public static class CutOutHelper
    {
        /// <summary>
        /// The size of the gap between the parts for a style, in pixels
        /// </summary>
        /// <param name="style">CutMarkStyle</param>
        /// <param name="settings">TornEdgeEffect with the tooth height</param>
        /// <returns>int</returns>
        public static int GetGap(CutMarkStyle style, TornEdgeEffect settings)
            => style == CutMarkStyle.None ? 0 : Math.Max(2, settings.ToothHeight);

        /// <summary>
        /// Cut out a strip of the image and join the parts before and after it
        /// </summary>
        /// <param name="image">Image to cut</param>
        /// <param name="cutStart">int first row (horizontal) or column which is cut out</param>
        /// <param name="cutSize">int number of rows or columns which are cut out</param>
        /// <param name="horizontal">true when rows are cut out, false for columns</param>
        /// <param name="style">CutMarkStyle for the edges, None joins seamlessly</param>
        /// <param name="settings">TornEdgeEffect with the tooth sizes and shadow settings</param>
        /// <returns>Bitmap, the part after the cut starts at cutStart + GetGap(style, settings)</returns>
        public static Bitmap CutOut(Image image, int cutStart, int cutSize, bool horizontal, CutMarkStyle style, TornEdgeEffect settings)
        {
            int length = horizontal ? image.Height : image.Width;
            int breadth = horizontal ? image.Width : image.Height;
            int afterLength = length - cutStart - cutSize;
            // Only a cut in the middle has a joint to show
            if (cutStart <= 0 || afterLength <= 0)
            {
                style = CutMarkStyle.None;
            }

            int gap = GetGap(style, settings);
            int newLength = cutStart + gap + afterLength;

            // Map (along the cut, across the cut) to image coordinates
            PointF Map(float along, float across) => horizontal ? new PointF(along, across) : new PointF(across, along);
            var parts = new Bitmap(horizontal ? breadth : newLength, horizontal ? newLength : breadth, PixelFormat.Format32bppArgb);
            parts.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var graphics = Graphics.FromImage(parts))
            {
                if (style == CutMarkStyle.None)
                {
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

                    return parts;
                }

                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                int toothRange = horizontal ? settings.HorizontalToothRange : settings.VerticalToothRange;
                var random = new Random();
                var beforeEdge = CreateEdge(style, breadth, settings.ToothHeight, toothRange, random);
                var afterEdge = CreateEdge(style, breadth, settings.ToothHeight, toothRange, random);

                // The part before the cut, its edge goes into the part
                using (var path = new GraphicsPath())
                {
                    path.AddLines(new[] { Map(0, 0), Map(breadth, 0) });
                    path.AddLines(Enumerable.Reverse(beforeEdge).Select(p => Map(p.X, cutStart - p.Y)).ToArray());
                    path.CloseFigure();
                    using var brush = new TextureBrush(image, WrapMode.Clamp);
                    graphics.FillPath(brush, path);
                }

                // The part after the cut, moved up / left over the cut out strip minus the gap
                int afterStart = cutStart + gap;
                using (var path = new GraphicsPath())
                {
                    path.AddLines(afterEdge.Select(p => Map(p.X, afterStart + p.Y)).ToArray());
                    path.AddLines(new[] { Map(breadth, newLength), Map(0, newLength) });
                    path.CloseFigure();
                    using var brush = new TextureBrush(image, WrapMode.Clamp);
                    var shift = Map(0, afterStart - (cutStart + cutSize));
                    brush.TranslateTransform(shift.X, shift.Y);
                    graphics.FillPath(brush, path);
                }
            }

            if (!settings.GenerateShadow)
            {
                return parts;
            }

            // The shadow of the torn edge effect, it only shows in the gap
            using (parts)
            {
                using var matrix = new Matrix();
                using var withShadow = ImageHelper.CreateShadow(parts, settings.Darkness, settings.ShadowSize, settings.ShadowOffset, matrix, PixelFormat.Format32bppArgb);
                var offset = settings.ShadowOffset.Offset(settings.ShadowSize - 1, settings.ShadowSize - 1);
                return ImageHelper.CloneArea(withShadow, new NativeRect(offset.X, offset.Y, parts.Width, parts.Height), PixelFormat.Format32bppArgb);
            }
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
