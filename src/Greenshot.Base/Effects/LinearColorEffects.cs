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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// The color effects and filters of the editor (grayscale, invert, adjust, remove transparency, highlighter) as ImageSharp processing on
    /// linear light (straight alpha), for any pixel type: white is the value of white, e.g. the SDR white of an HDR image (scRGB: 1.0 = 80 nits).
    /// Values above white are highlights, below 0 colors outside sRGB; they are kept where the effect allows it. In the SDR range the result
    /// looks like the effect of the 8-bit image (which works on sRGB encoded values), so the parts of an HDR file which come from the 8-bit
    /// image fit. Each can be limited to an area of the image (a filter of an element).
    /// </summary>
    public static class LinearColorEffects
    {
        /// <summary>
        /// Gray of the same brightness: the luminance of BT.709 (the sRGB primaries) in linear light
        /// </summary>
        public static IImageProcessingContext LinearGrayscale(this IImageProcessingContext context, Rectangle? area = null) =>
            context.Process(row =>
            {
                for (int x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    float luminance = 0.2126f * pixel.X + 0.7152f * pixel.Y + 0.0722f * pixel.Z;
                    row[x] = new Vector4(luminance, luminance, luminance, pixel.W);
                }
            }, area);

        /// <summary>
        /// The negative, like the 8-bit image: each channel inverted in sRGB encoding between black and white. Highlights become black,
        /// colors outside sRGB the inverse of their nearest sRGB color.
        /// </summary>
        public static IImageProcessingContext LinearInvert(this IImageProcessingContext context, float white, Rectangle? area = null) =>
            context.MapChannels(white, encoded => 1f - Clamp01(encoded), area);

        /// <summary>
        /// A curve for each channel in sRGB encoding (0 is black, 1 white), e.g. of the adjust effect (brightness, contrast, gamma).
        /// Beyond white the curve goes on with its slope at white, so highlights keep their detail.
        /// </summary>
        /// <param name="context">The processing</param>
        /// <param name="white">The value of white</param>
        /// <param name="curve">257 values of the curve for the encoded values 0, 1/256 … 1 (see <see cref="CurveOf"/>)</param>
        /// <param name="area">Only this area, null for all of it</param>
        public static IImageProcessingContext LinearCurve(this IImageProcessingContext context, float white, float[] curve, Rectangle? area = null)
        {
            if (curve == null || curve.Length < 2) throw new ArgumentException("A curve needs at least two values", nameof(curve));
            int last = curve.Length - 1;
            float slopeAtWhite = (curve[last] - curve[last - 1]) * last;
            return context.MapChannels(white, encoded =>
            {
                if (encoded >= 1f)
                {
                    return curve[last] + (encoded - 1f) * slopeAtWhite;
                }
                float position = Math.Max(encoded, 0f) * last;
                int index = (int)position;
                float fraction = position - index;
                return curve[index] + (curve[index + 1] - curve[index]) * fraction;
            }, area);
        }

        /// <summary>
        /// The curve of a function on 8-bit values, for <see cref="LinearCurve"/>: e.g. what the adjust effect does to each gray level of the 8-bit image
        /// </summary>
        /// <param name="map">The 8-bit result for each 8-bit value</param>
        public static float[] CurveOf(Func<int, int> map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            // The 256 values of the 8-bit image at 0, 1/255 … 1, read at 0, 1/256 … 1
            var levels = new float[256];
            for (int level = 0; level < 256; level++)
            {
                levels[level] = map(level) / 255f;
            }
            var curve = new float[257];
            for (int i = 0; i <= 256; i++)
            {
                float position = i / 256f * 255f;
                int index = Math.Min((int)position, 254);
                float fraction = position - index;
                curve[i] = levels[index] + (levels[index + 1] - levels[index]) * fraction;
            }
            return curve;
        }

        /// <summary>
        /// The pixels on a background color (opaque), like the 8-bit image is drawn on it
        /// </summary>
        /// <param name="context">The processing</param>
        /// <param name="white">The value of white</param>
        /// <param name="background">The background as sRGB encoded values between 0 and 1</param>
        public static IImageProcessingContext LinearBackground(this IImageProcessingContext context, float white, Vector3 background)
        {
            var linear = ToLinear(background, white);
            return context.Process(row =>
            {
                for (int x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    float alpha = Clamp01(pixel.W);
                    var color = new Vector3(pixel.X, pixel.Y, pixel.Z) * alpha + linear * (1f - alpha);
                    row[x] = new Vector4(color, 1f);
                }
            }, null);
        }

        /// <summary>
        /// A text highlighter of the color, like the highlight filter does to the 8-bit image: each channel is at most the one of the color,
        /// so highlights under it get the color of the marker
        /// </summary>
        /// <param name="context">The processing</param>
        /// <param name="white">The value of white</param>
        /// <param name="color">The color as sRGB encoded values between 0 and 1</param>
        /// <param name="area">Only this area, null for all of it</param>
        public static IImageProcessingContext LinearHighlight(this IImageProcessingContext context, float white, Vector3 color, Rectangle? area = null)
        {
            var linear = ToLinear(color, white);
            return context.Process(row =>
            {
                for (int x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    row[x] = new Vector4(Math.Min(pixel.X, linear.X), Math.Min(pixel.Y, linear.Y), Math.Min(pixel.Z, linear.Z), pixel.W);
                }
            }, area);
        }

        /// <summary>
        /// Each color channel through the function on its sRGB encoded value (white is 1), alpha stays
        /// </summary>
        private static IImageProcessingContext MapChannels(this IImageProcessingContext context, float white, Func<float, float> map, Rectangle? area)
        {
            if (!(white > 0)) throw new ArgumentOutOfRangeException(nameof(white));
            return context.Process(row =>
            {
                for (int x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    row[x] = new Vector4(
                        Decode(map(Encode(pixel.X / white))) * white,
                        Decode(map(Encode(pixel.Y / white))) * white,
                        Decode(map(Encode(pixel.Z / white))) * white,
                        pixel.W);
                }
            }, area);
        }

        private static IImageProcessingContext Process(this IImageProcessingContext context, PixelRowOperation operation, Rectangle? area) =>
            area.HasValue ? context.ProcessPixelRowsAsVector4(operation, area.Value) : context.ProcessPixelRowsAsVector4(operation);

        private static Vector3 ToLinear(Vector3 encoded, float white) => new Vector3(Decode(encoded.X), Decode(encoded.Y), Decode(encoded.Z)) * white;

        /// <summary>
        /// sRGB encoding of a linear value, beyond 0 and 1 mirrored and continued (scRGB)
        /// </summary>
        internal static float Encode(float linear)
        {
            float magnitude = Math.Abs(linear);
            float encoded = magnitude <= 0.0031308f ? magnitude * 12.92f : 1.055f * (float)Math.Pow(magnitude, 1 / 2.4) - 0.055f;
            return linear < 0 ? -encoded : encoded;
        }

        /// <summary>
        /// The linear value of an sRGB encoded value, beyond 0 and 1 mirrored and continued (scRGB)
        /// </summary>
        internal static float Decode(float encoded)
        {
            float magnitude = Math.Abs(encoded);
            float linear = magnitude <= 0.04045f ? magnitude / 12.92f : (float)Math.Pow((magnitude + 0.055f) / 1.055f, 2.4);
            return encoded < 0 ? -linear : linear;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
