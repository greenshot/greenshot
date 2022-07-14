﻿/*
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
using Dapplo.Windows.Gdi32;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing.Filters
{
    [Serializable]
    public class BlurFilter : AbstractFilter
    {
        public double previewQuality;

        public double PreviewQuality
        {
            get => previewQuality;
            set
            {
                previewQuality = value;
                OnPropertyChanged(nameof(PreviewQuality));
            }
        }

        public BlurFilter(DrawableContainer parent) : base(parent)
        {
            AddField(GetType(), FieldType.BLUR_RADIUS, 3);
            AddField(GetType(), FieldType.PREVIEW_QUALITY, 1.0d);
        }

        public override void Apply(Graphics graphics, Bitmap applyBitmap, NativeRect rect, RenderMode renderMode)
        {
            int blurRadius = GetFieldValueAsInt(FieldType.BLUR_RADIUS);
            var applyRect = ImageHelper.CreateIntersectRectangle(applyBitmap.Size, rect, Invert);
            if (applyRect.Width == 0 || applyRect.Height == 0)
            {
                return;
            }

            GraphicsState state = graphics.Save();
            if (Invert)
            {
                graphics.SetClip(applyRect);
                graphics.ExcludeClip(rect);
            }

            if (GdiPlusApi.IsBlurPossible(blurRadius))
            {
                GdiPlusApi.DrawWithBlur(graphics, applyBitmap, applyRect, null, null, blurRadius, false);
            }
            else
            {
                using IFastBitmap fastBitmap = FastBitmap.CreateCloneOf(applyBitmap, applyRect);
                ImageHelper.ApplyBoxBlur(fastBitmap, blurRadius);
                fastBitmap.DrawTo(graphics, applyRect);
            }

            graphics.Restore(state);
        }
    }
}