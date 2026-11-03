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
using System.Linq;
using System.Numerics;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Editor.Drawing.Fields;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Editor.Drawing.Filters
{
    /// <summary>
    /// The filters of an element as ImageFilters (ImageSharp, any pixel type, in linear light), used on the HDR pixels for an HDR file (see
    /// HdrOverlay.Filters): the highlighter, the area highlight (brightness and blur outside), grayscale and the magnifier, like on the 8-bit image. Hiding filters
    /// (blur and pixelate of the obfuscate element) are not: their area comes from the 8-bit image, so exactly what is shown is hidden.
    /// </summary>
    internal static class ElementImageFilters
    {
        /// <summary>
        /// The image filters of the element, null when one of its filters can't be done on the HDR pixels (then its area comes from the 8-bit image)
        /// </summary>
        /// <param name="element">The element with filters</param>
        /// <param name="bounds">The image</param>
        /// <param name="invertedHoles">For inverted filters (they work outside of the elements): the bounds of the elements of the group (see
        /// DrawableContainerList), null when the group was done already by another element</param>
        public static IReadOnlyList<ImageFilter> Create(DrawableContainer element, NativeRect bounds, IReadOnlyList<NativeRect> invertedHoles)
        {
            if (element is ObfuscateContainer)
            {
                return null;
            }
            var imageFilters = new List<ImageFilter>();
            foreach (var filter in element.Filters.OfType<AbstractFilter>())
            {
                IReadOnlyList<NativeRect> areas;
                if (filter.Invert)
                {
                    if (invertedHoles == null)
                    {
                        continue;
                    }
                    areas = HdrImage.Outside(bounds, invertedHoles);
                }
                else
                {
                    var area = element.Bounds.Normalize().Intersect(bounds);
                    areas = area.Width > 0 && area.Height > 0 ? new[] { area } : Array.Empty<NativeRect>();
                }
                var imageFilter = Create(filter, element.Bounds.Normalize(), areas.Select(ToRectangle).ToList(), bounds);
                if (imageFilter == null)
                {
                    return null;
                }
                imageFilters.Add(imageFilter);
            }
            return imageFilters;
        }

        private static ImageFilter Create(AbstractFilter filter, NativeRect elementBounds, IReadOnlyList<Rectangle> areas, NativeRect bounds)
        {
            switch (filter)
            {
                case HighlightFilter:
                    var color = filter.GetFieldValueAsColor(FieldType.FILL_COLOR);
                    var highlight = new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
                    return new ImageFilter($"Highlight {color}", (pixels, white) => pixels.Mutate(context =>
                    {
                        foreach (var area in areas)
                        {
                            context.LinearHighlight(white, highlight, area);
                        }
                    }));
                case BrightnessFilter:
                    var curve = ImageEdits.AdjustCurve(filter.GetFieldValueAsFloat(FieldType.BRIGHTNESS), 1f, 1f);
                    return new ImageFilter("Brightness", (pixels, white) => pixels.Mutate(context =>
                    {
                        foreach (var area in areas)
                        {
                            context.LinearCurve(white, curve, area);
                        }
                    }));
                case GrayscaleFilter:
                    return new ImageFilter("Grayscale", (pixels, _) => pixels.Mutate(context =>
                    {
                        foreach (var area in areas)
                        {
                            context.LinearGrayscale(area);
                        }
                    }));
                case BlurFilter:
                    // Like ImageHelper.ApplyBoxBlur: a box of the (odd) radius as width, twice
                    int range = filter.GetFieldValueAsInt(FieldType.BLUR_RADIUS) | 1;
                    int radius = range / 2;
                    return new ImageFilter($"Blur {range}", (pixels, _) =>
                    {
                        if (radius < 1 || areas.Count == 0)
                        {
                            return;
                        }
                        // Blurred once, like the editor under its clip: the areas (e.g. the bands around the holes of an area highlight) must not
                        // read what is blurred for another one. The pixels around them are read too.
                        var all = areas.Aggregate(Rectangle.Union);
                        var around = Rectangle.Intersect(Rectangle.Inflate(all, 2 * radius, 2 * radius), new Rectangle(0, 0, pixels.Width, pixels.Height));
                        // On float pixels: ImageSharp would mix HalfVector4 on its scaled values (see FloatPixels)
                        using var blurred = FloatPixels.Of(pixels, around);
                        FloatPixels.Mutate(blurred, context => context.BoxBlur(radius).BoxBlur(radius));
                        foreach (var area in areas)
                        {
                            ImageFilter.CopyArea(pixels, blurred, new Point(around.X, around.Y), area);
                        }
                    });
                case MagnifierFilter:
                    if (filter.Invert)
                    {
                        return null;
                    }
                    int factor = Math.Max(filter.GetFieldValueAsInt(FieldType.MAGNIFICATION_FACTOR), 1);
                    // Like the magnifier filter: the middle of the element, enlarged to all of it (nearest neighbor)
                    var source = new NativeRect(elementBounds.X + elementBounds.Width / 2 - elementBounds.Width / factor / 2,
                        elementBounds.Y + elementBounds.Height / 2 - elementBounds.Height / factor / 2, elementBounds.Width / factor, elementBounds.Height / factor);
                    if (source.Width <= 0 || source.Height <= 0 || !source.Intersect(bounds).Equals(source) || !elementBounds.Intersect(bounds).Equals(elementBounds))
                    {
                        // Partly outside the image: the 8-bit image shows what GDI+ does there
                        return null;
                    }
                    return new ImageFilter($"Magnify {factor}x", (pixels, _) =>
                    {
                        using var magnified = FloatPixels.Of(pixels, ToRectangle(source));
                        FloatPixels.Mutate(magnified, context => context.Resize(elementBounds.Width, elementBounds.Height, KnownResamplers.NearestNeighbor));
                        ImageFilter.CopyArea(pixels, magnified, new Point(elementBounds.X, elementBounds.Y), ToRectangle(elementBounds));
                    });
                default:
                    // Pixelate, or a filter of a plugin
                    return null;
            }
        }

        private static Rectangle ToRectangle(NativeRect rect) => new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
