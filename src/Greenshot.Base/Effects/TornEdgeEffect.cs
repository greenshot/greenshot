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

using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Greenshot.Base.Core;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// TornEdgeEffect extends on DropShadowEffect
    /// </summary>
    [TypeConverter(typeof(EffectConverter))]
    public sealed class TornEdgeEffect : DropShadowEffect
    {
        public TornEdgeEffect()
        {
            Reset();
        }

        public int ToothHeight { get; set; }
        public int HorizontalToothRange { get; set; }
        public int VerticalToothRange { get; set; }
        public bool[] Edges { get; set; }
        public bool GenerateShadow { get; set; }

        public override void Reset()
        {
            base.Reset();
            ShadowSize = 7;
            ToothHeight = 12;
            HorizontalToothRange = 20;
            VerticalToothRange = 20;
            Edges = new[]
            {
                true, true, true, true
            };
            GenerateShadow = true;
        }

        public override Image Apply(Image sourceImage, Matrix matrix)
        {
            Image tmpTornImage = ImageHelper.CreateTornEdge(sourceImage, ToothHeight, HorizontalToothRange, VerticalToothRange, Edges);
            if (GenerateShadow)
            {
                using (tmpTornImage)
                {
                    return ImageHelper.CreateShadow(tmpTornImage, Darkness, ShadowSize, ShadowOffset, matrix, PixelFormat.Format32bppArgb);
                }
            }

            return tmpTornImage;
        }
    }
}