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

namespace Greenshot.Plugin.Win10
{
    /// <summary>
    /// What is done to the copy of the capture Windows OCR reads, the capture itself never changes
    /// </summary>
    [Flags]
    internal enum OcrPreparation
    {
        /// <summary>The pixels as they are (what Greenshot uses)</summary>
        None = 0,

        /// <summary>Paint over thin horizontal lines, e.g. the underlines of links</summary>
        RemoveLines = 1,

        /// <summary>Read small captures at double size</summary>
        Upscale = 2
    }

    /// <summary>
    /// Experiments to help Windows OCR with underlined and small screen text: thin horizontal lines (link underlines) painted over
    /// with the color below them, small captures read at double size. Not used by default: on a real page both made the OCR worse,
    /// OcrPreparationComparisonTests compares them on sample captures.
    /// </summary>
    internal static class OcrImagePreparation
    {
        /// <summary>A line shorter than this is kept: an underscore in a word is shorter</summary>
        internal const int MinimumLineLength = 20;

        /// <summary>Thicker lines are kept: they are part of a glyph or a drawing, not an underline</summary>
        internal const int MaximumLineThickness = 2;

        /// <summary>The longest interruption of a line, e.g. by the descender of a g</summary>
        internal const int MaximumGap = 4;

        /// <summary>How many percent of a line must be line pixels: the rest are the descenders crossing it</summary>
        internal const int MinimumLineCoverage = 85;

        /// <summary>Captures up to this many pixels are read at double size; larger ones would make the OCR slow</summary>
        internal const long UpscaleMaximumPixels = 4_000_000;

        // Colors closer than this (per channel) are the same color; further than DifferentColor are different
        private const int SameColor = 24;
        private const int DifferentColor = 48;

        /// <summary>
        /// The factor the image is enlarged with before the OCR: 2 for captures up to <see cref="UpscaleMaximumPixels"/> when that
        /// fits the engine, else 1. Windows OCR reads text of 20 pixels and more much better than the 11 to 16 pixels of screen text.
        /// </summary>
        internal static int UpscaleFactor(int width, int height, double maximumDimension)
        {
            if (width <= 0 || height <= 0) return 1;
            bool fits = width * 2 <= maximumDimension && height * 2 <= maximumDimension;
            return fits && (long)width * height <= UpscaleMaximumPixels ? 2 : 1;
        }

        /// <summary>
        /// Paint over thin horizontal lines: a run of at least <see cref="MinimumLineLength"/> pixels of one color, at most
        /// <see cref="MaximumLineThickness"/> pixels high, with another color below it and mostly another color above it (the
        /// descenders of g, p and y cross an underline). Each pixel of the line gets the color of the pixel below the line.
        /// </summary>
        /// <param name="pixels">32-bit pixels, row by row</param>
        /// <param name="width">pixels per row</param>
        /// <param name="height">rows</param>
        /// <returns>how many lines were painted over</returns>
        internal static int RemoveThinLines(int[] pixels, int width, int height)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (width < MinimumLineLength || height < 3 || pixels.Length < (long)width * height) return 0;

            int lines = 0;
            var thickness = new int[width];
            for (int y = 1; y < height - 1; y++)
            {
                // The thickness of the line at each pixel of the row, 0: not a line pixel
                for (int x = 0; x < width; x++)
                {
                    thickness[x] = LineThickness(pixels, width, height, x, y);
                }

                int start = 0;
                while (start < width)
                {
                    if (thickness[start] == 0)
                    {
                        start++;
                        continue;
                    }

                    int color = pixels[y * width + start];
                    int end = start;
                    int clearAbove = 0;
                    int linePixels = 0;
                    while (end < width)
                    {
                        if (IsLinePixel(pixels, thickness, width, y, end, color))
                        {
                            if (IsDifferent(pixels[(y - 1) * width + end], color)) clearAbove++;
                            linePixels++;
                            end++;
                            continue;
                        }

                        // A descender which crosses the line interrupts it for a few pixels
                        int next = end + 1;
                        while (next < width && next - end <= MaximumGap && !IsLinePixel(pixels, thickness, width, y, next, color)) next++;
                        if (next >= width || next - end > MaximumGap) break;
                        end = next;
                    }

                    int length = end - start;
                    // Above the line there is mostly something else: else it is the edge of an area of that color
                    // An underline is almost unbroken (only descenders cross it), the bottom row of a word has a gap after each stroke
                    if (length >= MinimumLineLength && linePixels * 100 >= length * MinimumLineCoverage && clearAbove * 10 >= linePixels * 6)
                    {
                        for (int x = start; x < end; x++)
                        {
                            if (!IsLinePixel(pixels, thickness, width, y, x, color)) continue;
                            int t = thickness[x];
                            int below = pixels[(y + t) * width + x];
                            for (int row = 0; row < t; row++)
                            {
                                pixels[(y + row) * width + x] = below;
                            }
                        }

                        lines++;
                    }

                    start = Math.Max(end, start + 1);
                }
            }

            return lines;
        }

        private static bool IsLinePixel(int[] pixels, int[] thickness, int width, int y, int x, int color) =>
            thickness[x] > 0 && IsSame(pixels[y * width + x], color);

        /// <summary>
        /// 1 or 2 when the pixel starts a line of that thickness downwards (the rows of the line have its color, the row below
        /// another one), else 0
        /// </summary>
        private static int LineThickness(int[] pixels, int width, int height, int x, int y)
        {
            int color = pixels[y * width + x];
            for (int t = 1; t <= MaximumLineThickness && y + t < height; t++)
            {
                int next = pixels[(y + t) * width + x];
                if (IsDifferent(next, color)) return t;
                if (!IsSame(next, color)) return 0;
            }

            return 0;
        }

        private static int Distance(int a, int b)
        {
            int distance = 0;
            for (int shift = 0; shift < 32; shift += 8)
            {
                distance = Math.Max(distance, Math.Abs((int)((a >> shift) & 0xFF) - (int)((b >> shift) & 0xFF)));
            }

            return distance;
        }

        private static bool IsSame(int a, int b) => Distance(a, b) <= SameColor;

        private static bool IsDifferent(int a, int b) => Distance(a, b) > DifferentColor;
    }
}
