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
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.ImageSharpDrawing
{
    /// <summary>
    /// The room which an effect of the editor adds around the image, drawn by ImageSharp for any pixel type, like the effect draws it into the
    /// 8-bit image: a bigger canvas (ImageHelper.ResizeCanvas, the margins of torn edges), a border (ImageHelper.CreateBorder) and a drop
    /// shadow (ImageHelper.CreateShadow). The colors are in the values of the image (straight alpha), for an HDR image in linear light.
    /// </summary>
    public static class RoomDrawing
    {
        /// <summary>
        /// The sigma of a Gaussian blur for the blur of GDI+, per pixel of its radius (measured 0.42-0.43, a bit more)
        /// </summary>
        public const float GdiBlurSigma = 0.45f;

        /// <summary>
        /// The image in a bigger one, over the background (DrawImage of GDI+ blends it over)
        /// </summary>
        /// <param name="source">The image</param>
        /// <param name="size">The size of the result</param>
        /// <param name="offset">Where the image is in the result</param>
        /// <param name="background">The color under it, transparent for none</param>
        /// <returns>The result in the pixel type and the configuration of the image</returns>
        public static Image<TPixel> Place<TPixel>(Image<TPixel> source, Size size, Point offset, Vector4 background) where TPixel : unmanaged, IPixel<TPixel>
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Compose(source, size, offset, (_, row) => row.Fill(background));
        }

        /// <summary>
        /// The image with a border around it, like ImageHelper.CreateBorder: a line of the width along the middle of the border, with round
        /// corners, the image inside it
        /// </summary>
        /// <param name="source">The image</param>
        /// <param name="borderSize">The width of the border</param>
        /// <param name="color">The color of the border</param>
        /// <returns>The result in the pixel type and the configuration of the image, bigger by the border on each side</returns>
        public static Image<TPixel> Border<TPixel>(Image<TPixel> source, int borderSize, Vector4 color) where TPixel : unmanaged, IPixel<TPixel>
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var size = new Size(source.Width + 2 * borderSize, source.Height + 2 * borderSize);
            using var border = new Image<RgbaVector>(size.Width, size.Height);
            if (borderSize > 0 && color.W > 0)
            {
                // GDI+ (PixelOffsetMode.HighQuality) has the edges of the pixels on whole numbers, ImageSharp their middles: half a pixel back
                float half = borderSize / 2f - 0.5f;
                var pen = Painting.Pen(Color.FromPixel(new RgbaVector(color.X, color.Y, color.Z, color.W)), borderSize, JointStyle.Round);
                border.Mutate(context => context.Draw(pen, new RectangularPolygon(half, half, size.Width - borderSize, size.Height - borderSize)));
            }
            // Read row by row while the result is made, no copy of all of it
            return Compose(source, size, new Point(borderSize, borderSize), (y, row) =>
            {
                var borderRow = border.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < row.Length; x++)
                {
                    row[x] = borderRow[x].ToVector4();
                }
            });
        }

        /// <summary>
        /// The image over its shadow, like ImageHelper.CreateShadow: the alpha of the image at the darkness, at (size, size) with the odd size,
        /// blurred (a Gaussian for the blur of GDI+, otherwise a box of the odd size twice like ImageHelper.ApplyBoxBlur)
        /// </summary>
        /// <param name="source">The image</param>
        /// <param name="size">The size of the result (bigger by the shadow)</param>
        /// <param name="offset">Where the image is in the result</param>
        /// <param name="darkness">The darkness of the shadow (with the blur of GDI+ the effect makes it 0.1 darker, the caller gives that)</param>
        /// <param name="shadowSize">The size of the shadow before it is made odd, like the effect</param>
        /// <param name="gdiBlur">The effect blurs with GDI+ (see GdiPlusApi.IsBlurPossible)</param>
        /// <returns>The result in the pixel type and the configuration of the image</returns>
        public static Image<TPixel> DropShadow<TPixel>(Image<TPixel> source, Size size, Point offset, float darkness, int shadowSize, bool gdiBlur)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            int oddSize = (shadowSize & 1) == 0 ? shadowSize + 1 : shadowSize;
            // Alpha 1 everywhere: the blur premultiplies, the shadow is in the red channel
            using var mask = new Image<RgbaVector>(size.Width, size.Height, new RgbaVector(0, 0, 0, 1));
            mask.ProcessPixelRows(source, (maskRows, sourceRows) =>
            {
                for (int y = 0; y < source.Height && y + oddSize < size.Height; y++)
                {
                    var sourceRow = sourceRows.GetRowSpan(y);
                    var maskRow = maskRows.GetRowSpan(y + oddSize);
                    for (int x = 0; x < source.Width && x + oddSize < size.Width; x++)
                    {
                        maskRow[x + oddSize] = new RgbaVector(Math.Max(Math.Min(sourceRow[x].ToVector4().W, 1f), 0f) * darkness, 0, 0, 1);
                    }
                }
            });
            if (gdiBlur)
            {
                // ImageHelper.CreateShadow blurs with the radius of the odd size + 1
                mask.Mutate(context => context.GaussianBlur(GdiBlurSigma * (oddSize + 1)));
            }
            else if (oddSize / 2 > 0)
            {
                mask.Mutate(context => context.BoxBlur(oddSize / 2).BoxBlur(oddSize / 2));
            }
            // Read row by row while the result is made, no copy of all of it
            return Compose(source, size, offset, (y, row) =>
            {
                var maskRow = mask.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < row.Length; x++)
                {
                    row[x] = new Vector4(0, 0, 0, maskRow[x].R);
                }
            });
        }

        /// <summary>
        /// The image over what is under it, in a result of the size
        /// </summary>
        /// <param name="underRow">Fills a row (its y, the colors of the row) with what is under the image</param>
        private static Image<TPixel> Compose<TPixel>(Image<TPixel> source, Size size, Point offset, UnderRow underRow) where TPixel : unmanaged, IPixel<TPixel>
        {
            var result = new Image<TPixel>(source.GetConfiguration(), size.Width, size.Height);
            var under = new Vector4[size.Width];
            result.ProcessPixelRows(source, (target, sourceRows) =>
            {
                for (int y = 0; y < size.Height; y++)
                {
                    var targetRow = target.GetRowSpan(y);
                    int sourceY = y - offset.Y;
                    var sourceRow = sourceY >= 0 && sourceY < source.Height ? sourceRows.GetRowSpan(sourceY) : default;
                    underRow(y, under);
                    for (int x = 0; x < size.Width; x++)
                    {
                        var color = under[x];
                        int sourceX = x - offset.X;
                        if (!sourceRow.IsEmpty && sourceX >= 0 && sourceX < source.Width)
                        {
                            color = Over(sourceRow[sourceX].ToVector4(), color);
                        }
                        TPixel value = default;
                        value.FromVector4(color);
                        targetRow[x] = value;
                    }
                }
            });
            return result;
        }

        /// <summary>
        /// Fills the colors under a row of the image
        /// </summary>
        private delegate void UnderRow(int y, Span<Vector4> row);

        /// <summary>
        /// The pixel over the one under it, straight alpha
        /// </summary>
        private static Vector4 Over(Vector4 top, Vector4 bottom)
        {
            float topAlpha = Math.Max(Math.Min(top.W, 1f), 0f);
            if (topAlpha >= 1f)
            {
                return top;
            }
            float bottomAlpha = Math.Max(Math.Min(bottom.W, 1f), 0f) * (1f - topAlpha);
            float alpha = topAlpha + bottomAlpha;
            if (alpha <= 0f)
            {
                return Vector4.Zero;
            }
            var color = (new Vector3(top.X, top.Y, top.Z) * topAlpha + new Vector3(bottom.X, bottom.Y, bottom.Z) * bottomAlpha) / alpha;
            return new Vector4(color, alpha);
        }
    }
}
