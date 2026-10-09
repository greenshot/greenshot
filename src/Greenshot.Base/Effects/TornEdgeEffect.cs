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

        /// <summary>
        /// The color of the torn off parts and behind the shadow, transparent by default.
        /// Only with a (partly) transparent color the result needs an alpha channel.
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// The seed for the random edges, the same seed always gives the same edges
        /// </summary>
        public int Seed { get; set; }

        /// <summary>
        /// Pick new random edges
        /// </summary>
        public void Reseed()
        {
            Seed = System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode();
        }

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
            BackgroundColor = Color.Transparent;
            Reseed();
        }

        public override Image Apply(Image sourceImage, Matrix matrix)
        {
            Image tornImage = ImageHelper.CreateTornEdge(sourceImage, ToothHeight, HorizontalToothRange, VerticalToothRange, Edges, Seed);
            if (GenerateShadow)
            {
                using var withoutShadow = tornImage;
                tornImage = ImageHelper.CreateShadow(withoutShadow, Darkness, ShadowSize, ShadowOffset, matrix, PixelFormat.Format32bppArgb);
            }

            if (BackgroundColor.A == 0)
            {
                return tornImage;
            }

            // Put it on the background color, an alpha channel is only needed for a partly transparent color or when the source had one
            var pixelFormat = BackgroundColor.A < 255 || Image.IsAlphaPixelFormat(sourceImage.PixelFormat) ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
            using (tornImage)
            {
                var result = ImageHelper.CreateEmpty(tornImage.Width, tornImage.Height, pixelFormat, BackgroundColor, tornImage.HorizontalResolution, tornImage.VerticalResolution);
                using var graphics = Graphics.FromImage(result);
                graphics.DrawImage(tornImage, 0, 0, tornImage.Width, tornImage.Height);
                return result;
            }
        }
    }
}