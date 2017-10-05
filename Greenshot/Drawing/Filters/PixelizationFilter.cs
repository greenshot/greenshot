#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Greenshot.Drawing.Fields;
using Greenshot.Gfx;
using Greenshot.Gfx.FastBitmap;
using Greenshot.Helpers;
using GreenshotPlugin.Interfaces.Drawing;

#endregion

namespace Greenshot.Drawing.Filters
{
    [Serializable]
    public class PixelizationFilter : AbstractFilter
    {
        public PixelizationFilter(DrawableContainer parent) : base(parent)
        {
            AddField(GetType(), FieldType.PIXEL_SIZE, 5);
        }

        public override void Apply(Graphics graphics, Bitmap applyBitmap, NativeRect rect, RenderMode renderMode)
        {
            var pixelSize = GetFieldValueAsInt(FieldType.PIXEL_SIZE);
            BitmapHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);
            if (pixelSize <= 1 || rect.Width == 0 || rect.Height == 0)
            {
                // Nothing to do
                return;
            }
            if (rect.Width < pixelSize)
            {
                pixelSize = rect.Width;
            }
            if (rect.Height < pixelSize)
            {
                pixelSize = rect.Height;
            }
            using (var dest = FastBitmapBase.CreateCloneOf(applyBitmap, area: rect))
            {
                using (var src = FastBitmapBase.Create(applyBitmap, rect))
                {
                    var colors = new List<Color>();
                    var halbPixelSize = pixelSize / 2;
                    for (var y = src.Top - halbPixelSize; y < src.Bottom + halbPixelSize; y = y + pixelSize)
                    {
                        for (var x = src.Left - halbPixelSize; x <= src.Right + halbPixelSize; x = x + pixelSize)
                        {
                            colors.Clear();
                            for (var yy = y; yy < y + pixelSize; yy++)
                            {
                                if (yy < src.Top || yy >= src.Bottom)
                                {
                                    continue;
                                }
                                for (var xx = x; xx < x + pixelSize; xx++)
                                {
                                    if (xx < src.Left || xx >= src.Right)
                                    {
                                        continue;
                                    }
                                    colors.Add(src.GetColorAt(xx, yy));
                                }
                            }
                            var currentAvgColor = Colors.Mix(colors);
                            for (var yy = y; yy <= y + pixelSize; yy++)
                            {
                                if (yy < src.Top || yy >= src.Bottom)
                                {
                                    continue;
                                }
                                for (var xx = x; xx <= x + pixelSize; xx++)
                                {
                                    if (xx < src.Left || xx >= src.Right)
                                    {
                                        continue;
                                    }
                                    dest.SetColorAt(xx, yy, ref currentAvgColor);
                                }
                            }
                        }
                    }
                }
                dest.DrawTo(graphics, rect.Location);
            }
        }
    }
}