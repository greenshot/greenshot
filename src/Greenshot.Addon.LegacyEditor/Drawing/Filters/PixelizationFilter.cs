// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;
using Greenshot.Gfx.FastBitmap;

namespace Greenshot.Addon.LegacyEditor.Drawing.Filters
{
    /// <summary>
    /// This implements the pixelization filter
    /// </summary>
    [Serializable]
    public class PixelizationFilter : AbstractFilter
    {
        public PixelizationFilter(DrawableContainer parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
        {
            AddField(GetType(), FieldTypes.PIXEL_SIZE, 5);
        }

        public override void Apply(Graphics graphics, IBitmapWithNativeSupport applyBitmap, NativeRect rect, RenderMode renderMode)
        {
            var pixelSize = GetFieldValueAsInt(FieldTypes.PIXEL_SIZE);
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
            using (var dest = FastBitmapFactory.CreateCloneOf(applyBitmap, area: rect))
            {
                using (var src = FastBitmapFactory.Create(applyBitmap, rect))
                {
                    var halbPixelSize = pixelSize / 2;
                    // Create a list of x values
                    var xValues = new List<int>();
                    for (var x = src.Left - halbPixelSize; x <= src.Right + halbPixelSize; x = x + pixelSize)
                    {
                        xValues.Add(x);
                    }
                    for (var y = src.Top - halbPixelSize; y < src.Bottom + halbPixelSize; y = y + pixelSize)
                    {
                        Parallel.ForEach(xValues, x =>
                        {
                            // TODO: Use stackalloc, or byte[]?
                            var colors = new List<Color>();
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
                        });
                    }
                }
                dest.DrawTo(graphics, rect.Location);
            }
        }
    }
}