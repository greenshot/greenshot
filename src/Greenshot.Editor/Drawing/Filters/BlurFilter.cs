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
            get { return previewQuality; }
            set
            {
                previewQuality = value;
                OnPropertyChanged("PreviewQuality");
            }
        }

        public BlurFilter(DrawableContainer parent) : base(parent)
        {
            AddField(GetType(), FieldType.BLUR_RADIUS, 3);
            AddField(GetType(), FieldType.PREVIEW_QUALITY, 1.0d);
        }

        protected override void ApplyFilter(Graphics graphics, Bitmap applyBitmap, NativeRect applyRect, RenderMode renderMode)
        {
            int blurRadius = GetFieldValueAsInt(FieldType.BLUR_RADIUS);
            if (GdiPlusApi.IsBlurPossible(blurRadius))
            {
                GdiPlusApi.DrawWithBlur(graphics, applyBitmap, applyRect, null, null, blurRadius, false);
            }
            else
            {
                using (IFastBitmap fastBitmap = FastBitmap.CreateCloneOf(applyBitmap, applyRect))
                {
                    ImageHelper.ApplyBoxBlur(fastBitmap, blurRadius);
                    fastBitmap.DrawTo(graphics, applyRect);
                }
            }
        }
    }
}