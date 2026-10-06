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

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// The shadow for cut marks and torn edges. These cut parts out of the image, wherever they overlap:
    /// the shadow is cast by what is left of the image, which is the same for all of them,
    /// and every part of a cut area gets the shadow only once, from the cut which is drawn last there.
    /// </summary>
    internal static class CutShadow
    {
        /// <summary>
        /// The elements which cut parts out of the image, in the order they are drawn
        /// </summary>
        internal static IList<DrawableContainer> GetCuts(Surface surface, DrawableContainer self)
        {
            var cuts = surface?.Elements.OfType<DrawableContainer>().Where(element => element is CutMarkContainer or TornEdgeContainer).ToList() ?? new List<DrawableContainer>();
            if (!cuts.Contains(self))
            {
                cuts.Add(self);
            }

            return cuts;
        }

        /// <summary>
        /// The area which an element cuts out, in image coordinates
        /// </summary>
        /// <returns>GraphicsPath or null</returns>
        internal static GraphicsPath CreateCutPath(DrawableContainer element) => element switch
        {
            CutMarkContainer cutMark => cutMark.CreateCutPath(),
            TornEdgeContainer tornEdges => tornEdges.CreateCutPath(),
            _ => null
        };

        /// <summary>
        /// A key which changes when one of the cut areas changes
        /// </summary>
        internal static string GetKey(IEnumerable<DrawableContainer> cuts) => string.Join(";", cuts.Select(cut => cut switch
        {
            CutMarkContainer cutMark => cutMark.ShapeKey,
            TornEdgeContainer tornEdges => tornEdges.ShapeKey,
            _ => string.Empty
        }));

        /// <summary>
        /// Create the shadow of what is left of the image
        /// </summary>
        internal static Bitmap CreateShadow(Size imageSize, IEnumerable<DrawableContainer> cuts, float darkness, int shadowSize, NativePoint shadowOffset)
        {
            using var mask = new Bitmap(imageSize.Width, imageSize.Height, PixelFormat.Format32bppArgb);
            using (var maskGraphics = Graphics.FromImage(mask))
            {
                maskGraphics.Clear(Color.Black);
                maskGraphics.SmoothingMode = SmoothingMode.HighQuality;
                maskGraphics.CompositingMode = CompositingMode.SourceCopy;
                using var clear = new SolidBrush(Color.Transparent);
                foreach (var cut in cuts)
                {
                    using var cutPath = CreateCutPath(cut);
                    if (cutPath != null)
                    {
                        maskGraphics.FillPath(clear, cutPath);
                    }
                }
            }

            using var matrix = new Matrix();
            return ImageHelper.CreateShadow(mask, darkness, shadowSize, shadowOffset, matrix, PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Draw the shadow into the cut area of the element, leaving out the parts where a cut which is drawn later takes over
        /// </summary>
        internal static void Draw(Graphics graphics, Bitmap shadow, DrawableContainer self, IList<DrawableContainer> cuts, int shadowSize, NativePoint shadowOffset)
        {
            using var ownPath = CreateCutPath(self);
            if (ownPath == null)
            {
                return;
            }

            using var area = new Region(ownPath);
            for (int i = cuts.IndexOf(self) + 1; i < cuts.Count; i++)
            {
                using var laterPath = CreateCutPath(cuts[i]);
                if (laterPath != null)
                {
                    area.Exclude(laterPath);
                }
            }

            // The image itself is drawn at this offset in the shadow image
            var offset = shadowOffset.Offset(shadowSize - 1, shadowSize - 1);
            var state = graphics.Save();
            graphics.SetClip(area, CombineMode.Intersect);
            graphics.DrawImage(shadow, -offset.X, -offset.Y, shadow.Width, shadow.Height);
            graphics.Restore(state);
        }
    }
}
