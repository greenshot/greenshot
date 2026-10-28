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

using Greenshot.Linux.Editor.Fields;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Effects;

/// <summary>
/// Cutting a strip out of the image and the shapes of cut edges, the CutOutHelper of the Windows editor
/// </summary>
public static class CutOut
{
    private static readonly Random SeedRandom = new();

    /// <summary>
    /// A new seed for random edges, different from the current one
    /// </summary>
    public static int NewSeed(int currentSeed)
    {
        lock (SeedRandom)
        {
            int seed;
            do
            {
                seed = SeedRandom.Next(1, int.MaxValue);
            }
            while (seed == currentSeed);

            return seed;
        }
    }

    /// <summary>
    /// Cut out a strip and join the parts before and after it seamlessly
    /// </summary>
    /// <param name="image">The image, not changed</param>
    /// <param name="cutStart">The first row (horizontal) or column which is cut out</param>
    /// <param name="cutSize">The number of rows or columns which are cut out</param>
    /// <param name="horizontal">true when rows are cut out</param>
    public static SKBitmap CutStrip(SKBitmap image, int cutStart, int cutSize, bool horizontal)
    {
        int length = horizontal ? image.Height : image.Width;
        int breadth = horizontal ? image.Width : image.Height;
        int afterLength = length - cutStart - cutSize;
        var result = ImageEffects.NewBitmap(horizontal ? breadth : length - cutSize, horizontal ? length - cutSize : breadth);
        using var canvas = new SKCanvas(result);
        using var copy = new SKPaint { BlendMode = SKBlendMode.Src };
        if (cutStart > 0)
        {
            var before = horizontal ? SKRect.Create(0, 0, breadth, cutStart) : SKRect.Create(0, 0, cutStart, breadth);
            canvas.DrawBitmap(image, before, before, copy);
        }

        if (afterLength > 0)
        {
            var source = horizontal ? SKRect.Create(0, cutStart + cutSize, breadth, afterLength) : SKRect.Create(cutStart + cutSize, 0, afterLength, breadth);
            var target = horizontal ? SKRect.Create(0, cutStart, breadth, afterLength) : SKRect.Create(cutStart, 0, afterLength, breadth);
            canvas.DrawBitmap(image, source, target, copy);
        }

        return result;
    }

    /// <summary>
    /// Make the canvas bigger, e.g. for the shadow of edges; the room is the background color or transparent
    /// </summary>
    /// <param name="margins">Top, right, bottom, left</param>
    public static SKBitmap Expand(SKBitmap image, SKColor background, int[] margins)
    {
        var result = ImageEffects.NewBitmap(image.Width + margins[1] + margins[3], image.Height + margins[0] + margins[2]);
        using var canvas = new SKCanvas(result);
        canvas.Clear(background.Alpha < 255 ? SKColors.Transparent : background);
        canvas.DrawBitmap(image, margins[3], margins[0]);
        return result;
    }

    /// <summary>
    /// An edge along a cut: X is the place along the cut from 0 to length, Y how deep the edge goes into the part (0 up to toothHeight)
    /// </summary>
    public static IReadOnlyList<SKPoint> CreateEdge(CutMarkStyle style, int length, int toothHeight, int toothRange, Random random)
    {
        toothHeight = Math.Max(1, toothHeight);
        toothRange = Math.Max(2, toothRange);
        var points = new List<SKPoint>();
        switch (style)
        {
            case CutMarkStyle.ZigZag:
                for (int i = 0; ; i++)
                {
                    float along = Math.Min(i * toothRange / 2f, length);
                    points.Add(new SKPoint(along, i % 2 == 0 ? 0 : toothHeight));
                    if (along >= length) break;
                }

                break;
            case CutMarkStyle.Wave:
                for (float along = 0; ; along += 2)
                {
                    float clamped = Math.Min(along, length);
                    points.Add(new SKPoint(clamped, toothHeight / 2f * (1 + MathF.Sin(MathF.PI * clamped / toothRange))));
                    if (along >= length) break;
                }

                break;
            case CutMarkStyle.Torn:
                // Like the torn edge effect: a random depth at every tooth
                for (int i = 0; ; i++)
                {
                    float along = Math.Min(i * toothRange, length);
                    points.Add(new SKPoint(along, random.Next(1, toothHeight + 1)));
                    if (along >= length) break;
                }

                break;
            default:
                points.Add(new SKPoint(0, 0));
                points.Add(new SKPoint(length, 0));
                break;
        }

        return points;
    }
}
