/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Drawing2D;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing.Filters
{
    /// <summary>
    /// This filter highlights an area
    /// </summary>
    [Serializable()]
    public class HighlightFilter : AbstractFilter
    {
        public HighlightFilter(DrawableContainer parent) : base(parent)
        {
            AddField(GetType(), FieldType.FILL_COLOR, Color.Yellow);
        }

        /// <summary>
        /// Implements the Apply code for the Brightness Filet
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="applyBitmap"></param>
        /// <param name="rect">NativeRect</param>
        /// <param name="renderMode"></param>
        public override void Apply(Graphics graphics, Bitmap applyBitmap, NativeRect rect, RenderMode renderMode)
        {
            var applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);

            if (applyRect.Width == 0 || applyRect.Height == 0)
            {
                // nothing to do
                return;
            }

            GraphicsState state = graphics.Save();
            if (Invert)
            {
                graphics.SetClip(applyRect);
                graphics.ExcludeClip(rect);
            }

            using (IFastBitmap fastBitmap = FastBitmap.CreateCloneOf(applyBitmap, applyRect))
            {
                Color highlightColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
                for (int y = fastBitmap.Top; y < fastBitmap.Bottom; y++)
                {
                    for (int x = fastBitmap.Left; x < fastBitmap.Right; x++)
                    {
                        Color color = fastBitmap.GetColorAt(x, y);
                        color = Color.FromArgb(color.A, Math.Min(highlightColor.R, color.R), Math.Min(highlightColor.G, color.G), Math.Min(highlightColor.B, color.B));
                        fastBitmap.SetColorAt(x, y, color);
                    }
                }

                fastBitmap.DrawTo(graphics, applyRect.Location);
            }

            graphics.Restore(state);
        }
    }
}