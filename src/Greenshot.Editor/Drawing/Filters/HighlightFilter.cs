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
using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using SixLabors.ImageSharp.PixelFormats;

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

        protected override void ApplyFilter(Graphics graphics, Bitmap applyBitmap, NativeRect applyRect, RenderMode renderMode)
        {
            using Bitmap highlighted = ImageHelper.CloneArea(applyBitmap, applyRect, PixelFormat.Format32bppArgb);
            Color highlightColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
            BitmapPixels.ProcessPixelRows<Bgra32>(highlighted, pixels =>
            {
                for (int y = 0; y < pixels.Height; y++)
                {
                    var row = pixels.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        ref Bgra32 pixel = ref row[x];
                        pixel.R = Math.Min(highlightColor.R, pixel.R);
                        pixel.G = Math.Min(highlightColor.G, pixel.G);
                        pixel.B = Math.Min(highlightColor.B, pixel.B);
                    }
                }
            });

            graphics.DrawImage(highlighted, applyRect, new Rectangle(0, 0, highlighted.Width, highlighted.Height), GraphicsUnit.Pixel);
        }
    }
}