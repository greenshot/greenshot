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

namespace Greenshot.Editor.Drawing.Filters
{
    [Serializable()]
    public class BrightnessFilter : AbstractFilter
    {
        public BrightnessFilter(DrawableContainer parent) : base(parent)
        {
            AddField(GetType(), FieldType.BRIGHTNESS, 0.9d);
        }

        protected override void ApplyFilter(Graphics graphics, Bitmap applyBitmap, NativeRect applyRect, RenderMode renderMode)
        {
            float brightness = GetFieldValueAsFloat(FieldType.BRIGHTNESS);
            using (ImageAttributes ia = ImageHelper.CreateAdjustAttributes(brightness, 1f, 1f))
            {
                graphics.DrawImage(applyBitmap, applyRect, applyRect.X, applyRect.Y, applyRect.Width, applyRect.Height, GraphicsUnit.Pixel, ia);
            }
        }
    }
}