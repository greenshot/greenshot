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
using Greenshot.Base.Hdr;
using Greenshot.Editor.Drawing.Fields;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Editor.Drawing.Filters
{
    /// <summary>
    /// The filters of an element as ImageFilters (ImageSharp, any pixel type, in linear light), used on the HDR pixels for an HDR file (see
    /// ImageSharpStep.Filter): the highlighter, the area highlight (brightness and blur outside), grayscale, the magnifier and the blur and pixelate of
    /// the obfuscate element, like on the 8-bit image. The pixelate uses the blocks and noise of the editor.
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
        /// <param name="invertedFrom">The element of the group whose inverted filters are used (like the editor: the selected one, else the
        /// first), null for this element</param>
        /// <param name="sdrImage">The 8-bit image of the capture, which the editor shows: the highlighter works on the HDR pixels as strong as on it</param>
        public static IReadOnlyList<ImageFilter> Create(DrawableContainer element, NativeRect bounds, IReadOnlyList<NativeRect> invertedHoles,
            DrawableContainer invertedFrom = null, System.Drawing.Bitmap sdrImage = null)
        {
            var imageFilters = new List<ImageFilter>();
            // First the inverted filters of the group, then the element's own, like the editor (DrawableContainerList.Draw, then DrawContent
            // with skipInvertedFilters)
            var filters = (invertedFrom ?? element).Filters.OfType<AbstractFilter>().Where(filter => filter.Invert)
                .Concat(element.Filters.OfType<AbstractFilter>().Where(filter => !filter.Invert));
            foreach (var filter in filters)
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
                var imageFilter = Create(filter, element.Bounds.Normalize(), areas.Select(ToRectangle).ToList(), bounds, sdrImage);
                if (imageFilter == null)
                {
                    return null;
                }
                imageFilters.Add(imageFilter);
            }
            return imageFilters;
        }

        private static ImageFilter Create(AbstractFilter filter, NativeRect elementBounds, IReadOnlyList<Rectangle> areas, NativeRect bounds, System.Drawing.Bitmap sdrImage)
        {
            switch (filter)
            {
                case HighlightFilter:
                    var color = filter.GetFieldValueAsColor(FieldType.FILL_COLOR);
                    var highlight = new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
                    if (sdrImage != null && sdrImage.Width >= bounds.Width && sdrImage.Height >= bounds.Height)
                    {
                        // As strong as in the editor: each channel of the HDR pixel gets the factor which the highlighter applies to the 8-bit
                        // pixel (the editor shows it tone mapped). A minimum of the HDR pixel itself would cut its highlights down to the color.
                        var factors = areas.Select(area => HighlightFactors(sdrImage, area, highlight)).ToList();
                        return new ImageFilter($"Highlight {color} like the 8-bit image", (pixels, _) => pixels.Mutate(context =>
                        {
                            for (int i = 0; i < areas.Count; i++)
                            {
                                var area = areas[i];
                                var areaFactors = factors[i];
                                context.ProcessPixelRowsAsVector4((row, start) =>
                                {
                                    int offset = (start.Y - area.Y) * area.Width + start.X - area.X;
                                    for (int x = 0; x < row.Length; x++)
                                    {
                                        var factor = areaFactors[offset + x];
                                        row[x] = new Vector4(row[x].X * factor.X, row[x].Y * factor.Y, row[x].Z * factor.Z, row[x].W);
                                    }
                                }, area);
                            }
                        }));
                    }
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
                case BlurFilter blur:
                    // Like the editor (BlurFilter): from radius 20 on the blur of GDI+, a Gaussian (measured: sigma 0.42-0.43 x the radius), here a bit
                    // stronger, so the HDR file never shows more than the editor; below a box of the (odd) radius as width, twice (ImageHelper.ApplyBoxBlur)
                    int blurRadius = filter.GetFieldValueAsInt(FieldType.BLUR_RADIUS);
                    bool gaussian = Dapplo.Windows.Gdi32.GdiPlusApi.IsBlurPossible(blurRadius);
                    int range = blurRadius | 1;
                    int radius = gaussian ? (int)Math.Ceiling(3 * GdiBlurSigma * blurRadius) : range / 2;
                    // The noise of the editor's blur, in the same levels
                    var (noiseArea, noise) = blur.Noise(bounds);
                    return new ImageFilter(gaussian ? $"Gaussian blur {blurRadius}" : $"Blur {range}", (pixels, white) =>
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
                        FloatPixels.Mutate(blurred, context =>
                        {
                            if (gaussian)
                            {
                                context.GaussianBlur(GdiBlurSigma * blurRadius);
                            }
                            else
                            {
                                context.BoxBlur(radius).BoxBlur(radius);
                            }
                            // Not finer than the 8-bit image, with its noise: finer values or no noise would make it easier to undo the blur
                            context.ProcessPixelRowsAsVector4((row, start) =>
                            {
                                int noiseY = around.Y + start.Y - noiseArea.Y;
                                for (int x = 0; x < row.Length; x++)
                                {
                                    int noiseX = around.X + start.X + x - noiseArea.X;
                                    var pixel = row[x];
                                    float alpha = (float)Math.Round(pixel.W * 255) / 255;
                                    if (alpha > 0 && noiseX >= 0 && noiseX < noiseArea.Width && noiseY >= 0 && noiseY < noiseArea.Height)
                                    {
                                        int index = (noiseY * noiseArea.Width + noiseX) * 3;
                                        row[x] = new Vector4(ToSdrSteps(pixel.X, white, noise[index]), ToSdrSteps(pixel.Y, white, noise[index + 1]),
                                            ToSdrSteps(pixel.Z, white, noise[index + 2]), alpha);
                                    }
                                    else
                                    {
                                        row[x] = new Vector4(ToSdrSteps(pixel.X, white, 0), ToSdrSteps(pixel.Y, white, 0), ToSdrSteps(pixel.Z, white, 0), alpha);
                                    }
                                }
                            });
                        });
                        foreach (var area in areas)
                        {
                            ImageFilter.CopyArea(pixels, blurred, new Point(around.X, around.Y), area);
                        }
                    });
                case MagnifierFilter:
                    if (filter.Invert)
                    {
                        // No editor preset makes an inverted magnifier (only area highlight and grayscale invert their filters), one from a
                        // .greenshot file comes from the 8-bit image instead of being magnified the wrong way
                        return null;
                    }
                    int factor = Math.Max(filter.GetFieldValueAsInt(FieldType.MAGNIFICATION_FACTOR), 1);
                    // Like the magnifier filter: the middle of the element, enlarged to all of it (nearest neighbor)
                    var source = new NativeRect(elementBounds.X + elementBounds.Width / 2 - elementBounds.Width / factor / 2,
                        elementBounds.Y + elementBounds.Height / 2 - elementBounds.Height / factor / 2, elementBounds.Width / factor, elementBounds.Height / factor);
                    var visible = elementBounds.Intersect(bounds);
                    var sourceInside = source.Intersect(bounds);
                    if (source.Width <= 0 || source.Height <= 0 || visible.Width <= 0 || visible.Height <= 0 || sourceInside.Width <= 0 || sourceInside.Height <= 0)
                    {
                        return new ImageFilter("Magnify nothing", (_, _) => { });
                    }
                    return new ImageFilter($"Magnify {factor}x", (pixels, _) =>
                    {
                        // Outside the image there is nothing to magnify (transparent), what is magnified goes over the pixels
                        using var magnified = new Image<RgbaVector>(source.Width, source.Height);
                        using (var inside = FloatPixels.Of(pixels, ToRectangle(sourceInside)))
                        {
                            magnified.Mutate(context => context.DrawImage(inside, new Point(sourceInside.X - source.X, sourceInside.Y - source.Y), 1f));
                        }
                        FloatPixels.Mutate(magnified, context => context.Resize(elementBounds.Width, elementBounds.Height, KnownResamplers.NearestNeighbor));
                        using var result = FloatPixels.Of(pixels, ToRectangle(visible));
                        result.Mutate(context => context.DrawImage(magnified, new Point(elementBounds.X - visible.X, elementBounds.Y - visible.Y), 1f));
                        ImageFilter.CopyArea(pixels, result, new Point(visible.X, visible.Y), ToRectangle(visible));
                    });
                case PixelizationFilter pixelization:
                    return Pixelate(pixelization, elementBounds, areas, bounds, sdrImage);
                default:
                    // A filter of a plugin
                    return null;
            }
        }

        /// <summary>
        /// True when all filters of the element can be done on the HDR pixels (see <see cref="Create(DrawableContainer, NativeRect, IReadOnlyList{NativeRect}, DrawableContainer, System.Drawing.Bitmap)"/>):
        /// the filters of the editor; not an inverted magnifier (no preset makes one, only an old .greenshot file) or a filter of another kind
        /// </summary>
        public static bool Supports(DrawableContainer element) =>
            element.Filters.All(filter => filter is HighlightFilter or BrightnessFilter or GrayscaleFilter or BlurFilter or PixelizationFilter
                                          || filter is MagnifierFilter { Invert: false });

        private static Rectangle ToRectangle(NativeRect rect) => new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);

        /// <summary>
        /// The sigma of the Gaussian blur for the GDI+ blur of the editor, per pixel of the radius (measured 0.42-0.43, a bit more)
        /// </summary>
        private const float GdiBlurSigma = 0.45f;

        /// <summary>
        /// The linear value rounded to the steps of the 8-bit image (the sRGB levels of the white, continued above it), moved by the levels of
        /// the noise
        /// </summary>
        private static float ToSdrSteps(float linear, float white, int noiseLevels) =>
            LinearColorEffects.Decode(Math.Max((float)Math.Round(LinearColorEffects.Encode(linear / white) * 255) + noiseLevels, 0f) / 255) * white;

        /// <summary>
        /// Pixelate the HDR pixels in the blocks and with the noise of the editor (PixelizationFilter.Pixelate of the 8-bit image): each block
        /// gets the average of its HDR pixels, each pixel the noise of the editor's pixel as factor (in linear light). Taking the area from the
        /// 8-bit image would put it in at most as bright as SDR white, darker than the HDR pixels around it.
        /// </summary>
        private static ImageFilter Pixelate(PixelizationFilter filter, NativeRect elementBounds, IReadOnlyList<Rectangle> areas, NativeRect bounds, System.Drawing.Bitmap sdrImage)
        {
            if (sdrImage == null || sdrImage.Width < bounds.Width || sdrImage.Height < bounds.Height)
            {
                return null;
            }
            var blocks = new List<System.Drawing.Rectangle>();
            Vector3[] noise;
            Rectangle area;
            using (var pixelated = filter.Pixelate(sdrImage, elementBounds, blocks, out var applyRect))
            {
                if (pixelated == null)
                {
                    return new ImageFilter("Pixelate nothing", (_, _) => { });
                }
                area = ToRectangle(applyRect);
                noise = PixelateNoise(sdrImage, pixelated, area, blocks);
            }
            return new ImageFilter($"Pixelate {blocks.Count} blocks like the 8-bit image", (pixels, _) =>
            {
                var values = new Vector4[area.Width * area.Height];
                pixels.Mutate(context => context.ProcessPixelRowsAsVector4((row, start) =>
                    row.CopyTo(values.AsSpan((start.Y - area.Y) * area.Width + start.X - area.X, row.Length)), area));
                foreach (var block in blocks)
                {
                    var sum = Vector4.Zero;
                    for (int y = block.Top; y < block.Bottom; y++)
                    {
                        for (int x = block.Left; x < block.Right; x++)
                        {
                            sum += values[y * area.Width + x];
                        }
                    }
                    var average = sum / (block.Width * block.Height);
                    for (int y = block.Top; y < block.Bottom; y++)
                    {
                        for (int x = block.Left; x < block.Right; x++)
                        {
                            int index = y * area.Width + x;
                            values[index] = new Vector4(average.X * noise[index].X, average.Y * noise[index].Y, average.Z * noise[index].Z, average.W);
                        }
                    }
                }
                // Only into the areas of the filter: inverted, the blocks cover the image, but the elements are holes in it
                pixels.Mutate(context =>
                {
                    foreach (var target in areas.Select(part => Rectangle.Intersect(part, area)).Where(part => part.Width > 0 && part.Height > 0))
                    {
                        context.ProcessPixelRowsAsVector4((row, start) =>
                            values.AsSpan((start.Y - area.Y) * area.Width + start.X - area.X, row.Length).CopyTo(row), target);
                    }
                });
            });
        }

        /// <summary>
        /// For each pixel of the pixelated area: the noise of the editor, as factor of each channel in linear light (the pixelated value / the
        /// average of its block; 1 for a black block)
        /// </summary>
        private static Vector3[] PixelateNoise(System.Drawing.Bitmap sdrImage, System.Drawing.Bitmap pixelated, Rectangle area,
            IReadOnlyList<System.Drawing.Rectangle> blocks)
        {
            var linear = new float[256];
            for (int level = 0; level < 256; level++)
            {
                linear[level] = LinearColorEffects.Decode(level / 255f);
            }
            var source = new SixLabors.ImageSharp.PixelFormats.Bgra32[area.Width * area.Height];
            var result = new SixLabors.ImageSharp.PixelFormats.Bgra32[area.Width * area.Height];
            BitmapPixels.ProcessPixelRows<SixLabors.ImageSharp.PixelFormats.Bgra32>(sdrImage, new System.Drawing.Rectangle(area.X, area.Y, area.Width, area.Height), pixels =>
            {
                for (int y = 0; y < pixels.Height; y++)
                {
                    pixels.GetRowSpan(y).CopyTo(source.AsSpan(y * area.Width, area.Width));
                }
            }, System.Drawing.Imaging.ImageLockMode.ReadOnly);
            BitmapPixels.ProcessPixelRows<SixLabors.ImageSharp.PixelFormats.Bgra32>(pixelated, pixels =>
            {
                for (int y = 0; y < pixels.Height; y++)
                {
                    pixels.GetRowSpan(y).CopyTo(result.AsSpan(y * area.Width, area.Width));
                }
            }, System.Drawing.Imaging.ImageLockMode.ReadOnly);

            float Factor(byte value, int average) => average == 0 ? 1f : linear[value] / linear[average];
            var noise = new Vector3[area.Width * area.Height];
            foreach (var block in blocks)
            {
                // The average of the block, like the editor
                int sumR = 0, sumG = 0, sumB = 0;
                for (int y = block.Top; y < block.Bottom; y++)
                {
                    for (int x = block.Left; x < block.Right; x++)
                    {
                        var pixel = source[y * area.Width + x];
                        sumR += pixel.R;
                        sumG += pixel.G;
                        sumB += pixel.B;
                    }
                }
                int count = block.Width * block.Height;
                int averageR = sumR / count, averageG = sumG / count, averageB = sumB / count;
                for (int y = block.Top; y < block.Bottom; y++)
                {
                    for (int x = block.Left; x < block.Right; x++)
                    {
                        int index = y * area.Width + x;
                        var pixel = result[index];
                        noise[index] = new Vector3(Factor(pixel.R, averageR), Factor(pixel.G, averageG), Factor(pixel.B, averageB));
                    }
                }
            }
            return noise;
        }

        /// <summary>
        /// For each pixel of the area: the factor of each channel which the highlighter applies to the 8-bit pixel, in linear light
        /// (min(color, value) / value; 1 for black)
        /// </summary>
        private static Vector3[] HighlightFactors(System.Drawing.Bitmap sdrImage, Rectangle area, Vector3 color)
        {
            // The linear value of every 8-bit level
            var linear = new float[256];
            for (int level = 0; level < 256; level++)
            {
                linear[level] = LinearColorEffects.Decode(level / 255f);
            }
            var marker = new Vector3(LinearColorEffects.Decode(color.X), LinearColorEffects.Decode(color.Y), LinearColorEffects.Decode(color.Z));
            float Factor(byte level, float markerChannel) => level == 0 ? 1f : Math.Min(markerChannel, linear[level]) / linear[level];

            var factors = new Vector3[area.Width * area.Height];
            BitmapPixels.ProcessPixelRows<SixLabors.ImageSharp.PixelFormats.Bgra32>(sdrImage, new System.Drawing.Rectangle(area.X, area.Y, area.Width, area.Height), pixels =>
            {
                for (int y = 0; y < pixels.Height; y++)
                {
                    var row = pixels.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        factors[y * area.Width + x] = new Vector3(Factor(row[x].R, marker.X), Factor(row[x].G, marker.Y), Factor(row[x].B, marker.Z));
                    }
                }
            }, System.Drawing.Imaging.ImageLockMode.ReadOnly);
            return factors;
        }
    }
}
