/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Numerics;
using Greenshot.Base.Effects;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// The values of the colors of an image the drawing goes into: 8-bit sRGB colors (as the editor has them) become the values of its pixels.
    /// For an 8-bit image the sRGB values themselves (mixed like GDI+ does), for an HDR image linear light with SDR white at the given value
    /// (scRGB: 1.0 = 80 nits), mixed in linear light. Draw HDR into an Image&lt;RgbaVector&gt;: ImageSharp keeps values above 1 there, and mixes
    /// HalfVector4 pixels wrongly.
    /// </summary>
    public sealed class ImageValues
    {
        private readonly float? _white;

        private ImageValues(float? white)
        {
            _white = white;
        }

        /// <summary>
        /// The pixels are sRGB values from 0 to 1, e.g. an Image&lt;Rgba32&gt; like the editor's bitmap
        /// </summary>
        public static ImageValues Srgb { get; } = new ImageValues(null);

        /// <summary>
        /// The pixels are linear light, white (sRGB 255, 255, 255) is the value of white, e.g. the SDR white of an HDR image in scRGB
        /// </summary>
        public static ImageValues Linear(float white) => new ImageValues(white);

        /// <summary>
        /// The value of white, null for sRGB values
        /// </summary>
        public float? White => _white;

        /// <summary>
        /// The value of an 8-bit sRGB color with its alpha (straight alpha), e.g. a color of an element of the editor
        /// </summary>
        public Vector4 Of(Rgba32 color) => Of(new Vector4(color.R, color.G, color.B, color.A) / 255f);

        /// <summary>
        /// The 8-bit sRGB color as an ImageSharp color in the image's values
        /// </summary>
        public Color ColorOf(Rgba32 color)
        {
            var value = Of(color);
            return Color.FromPixel(new RgbaVector(value.X, value.Y, value.Z, value.W));
        }

        /// <summary>
        /// The value of an sRGB color from 0 to 1 with its alpha (straight alpha)
        /// </summary>
        public Vector4 Of(Vector4 srgb) => _white is float white
            ? new Vector4(LinearColorEffects.Decode(srgb.X) * white, LinearColorEffects.Decode(srgb.Y) * white, LinearColorEffects.Decode(srgb.Z) * white, srgb.W)
            : srgb;

        /// <summary>
        /// The value with its sRGB color inverted, like a mouse cursor inverts the screen; values above white count as white (they become black)
        /// </summary>
        public Vector4 Inverted(Vector4 value)
        {
            if (_white is not float white)
            {
                return new Vector4(1f - value.X, 1f - value.Y, 1f - value.Z, value.W);
            }
            float Invert(float linear) => LinearColorEffects.Decode(1f - LinearColorEffects.Encode(Math.Max(0f, Math.Min(1f, linear / white)))) * white;
            return new Vector4(Invert(value.X), Invert(value.Y), Invert(value.Z), value.W);
        }
    }
}
