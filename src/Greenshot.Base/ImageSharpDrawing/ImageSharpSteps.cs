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
using System.Collections.Generic;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Effects;
using Greenshot.ImageSharpDrawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// Runs the ImageSharp steps of the elements (see ImageSharpStep, ISurface.CreateImageSharpSteps) on an image, for any pixel type: e.g. on
    /// the HDR pixels of an HDR file (HdrImage.Compose, HdrImage.Draw)
    /// </summary>
    public static class ImageSharpSteps
    {
        /// <summary>
        /// Do the steps on the image, in their order: a filter works on what is drawn before it
        /// </summary>
        /// <param name="image">The image, changed in place</param>
        /// <param name="steps">The steps</param>
        /// <param name="elementValues">The values the elements are drawn in, e.g. linear light with the white of the elements</param>
        /// <param name="filterWhite">The value of white for the filters, e.g. the SDR white of an HDR image</param>
        public static void Draw<TPixel>(Image<TPixel> image, IEnumerable<ImageSharpStep> steps, ImageValues elementValues, float filterWhite)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (elementValues == null) throw new ArgumentNullException(nameof(elementValues));
            foreach (var step in steps ?? Array.Empty<ImageSharpStep>())
            {
                if (step.Draw != null)
                {
                    DrawOver(image, new NativeRect(step.Location, step.Size), step.Draw, elementValues);
                }
                else
                {
                    step.Filter.Apply(image, filterWhite);
                }
            }
        }

        /// <summary>
        /// Draw an element with ImageSharp on an area of the image: on float pixels of the area (ImageSharp mixes e.g. HalfVector4 wrongly, see
        /// FloatPixels), then back into the image
        /// </summary>
        private static void DrawOver<TPixel>(Image<TPixel> image, NativeRect area, Action<Image, ImageValues> draw, ImageValues values)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            if (area.Width <= 0 || area.Height <= 0 || area.X < 0 || area.Y < 0 || area.Right > image.Width || area.Bottom > image.Height)
            {
                return;
            }
            using var canvas = FloatPixels.Of(image, new Rectangle(area.X, area.Y, area.Width, area.Height));
            draw(canvas, values);
            image.ProcessPixelRows(canvas, (target, drawn) =>
            {
                for (int y = 0; y < drawn.Height; y++)
                {
                    var targetRow = target.GetRowSpan(area.Y + y).Slice(area.X, area.Width);
                    var drawnRow = drawn.GetRowSpan(y);
                    for (int x = 0; x < targetRow.Length; x++)
                    {
                        targetRow[x].FromVector4(drawnRow[x].ToVector4());
                    }
                }
            });
        }
    }
}
