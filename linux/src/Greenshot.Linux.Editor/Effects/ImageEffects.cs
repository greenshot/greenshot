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

using SkiaSharp;

namespace Greenshot.Linux.Editor.Effects;

/// <summary>
/// The result of an effect: the new image and how the elements move with it
/// </summary>
public sealed record EffectResult(SKBitmap Image, SKMatrix ElementTransform);

/// <summary>
/// The effects of the Image menu, with the parameters of Greenshot.Base (DropShadowEffect, TornEdgeEffect, BorderEffect, ...).
/// Each returns a new image, the source is not changed.
/// </summary>
public static class ImageEffects
{
    public static SKBitmap NewBitmap(int width, int height) =>
        new(new SKImageInfo(Math.Max(1, width), Math.Max(1, height), SKColorType.Bgra8888, SKAlphaType.Premul));

    public static EffectResult Crop(SKBitmap source, SKRectI area)
    {
        area.Intersect(new SKRectI(0, 0, source.Width, source.Height));
        var result = NewBitmap(area.Width, area.Height);
        using (var canvas = new SKCanvas(result))
        {
            canvas.DrawBitmap(source, area, new SKRect(0, 0, area.Width, area.Height));
        }

        return new EffectResult(result, SKMatrix.CreateTranslation(-area.Left, -area.Top));
    }

    public static EffectResult RotateClockwise(SKBitmap source)
    {
        var result = NewBitmap(source.Height, source.Width);
        using (var canvas = new SKCanvas(result))
        {
            canvas.Translate(source.Height, 0);
            canvas.RotateDegrees(90);
            canvas.DrawBitmap(source, 0, 0);
        }

        // (x, y) becomes (height - y, x)
        var matrix = new SKMatrix(0, -1, source.Height, 1, 0, 0, 0, 0, 1);
        return new EffectResult(result, matrix);
    }

    public static EffectResult RotateCounterClockwise(SKBitmap source)
    {
        var result = NewBitmap(source.Height, source.Width);
        using (var canvas = new SKCanvas(result))
        {
            canvas.Translate(0, source.Width);
            canvas.RotateDegrees(-90);
            canvas.DrawBitmap(source, 0, 0);
        }

        // (x, y) becomes (y, width - x)
        var matrix = new SKMatrix(0, 1, 0, -1, 0, source.Width, 0, 0, 1);
        return new EffectResult(result, matrix);
    }

    public static EffectResult Grayscale(SKBitmap source) => WithColorFilter(source, SKColorFilter.CreateColorMatrix(
    [
        0.299f, 0.587f, 0.114f, 0, 0,
        0.299f, 0.587f, 0.114f, 0, 0,
        0.299f, 0.587f, 0.114f, 0, 0,
        0, 0, 0, 1, 0,
    ]));

    public static EffectResult Invert(SKBitmap source) => WithColorFilter(source, SKColorFilter.CreateColorMatrix(
    [
        -1, 0, 0, 0, 1,
        0, -1, 0, 0, 1,
        0, 0, -1, 0, 1,
        0, 0, 0, 1, 0,
    ]));

    /// <summary>
    /// "Clear": a transparent image of the same size, to draw on an empty canvas
    /// </summary>
    public static EffectResult Clear(SKBitmap source)
    {
        var result = NewBitmap(source.Width, source.Height);
        result.Erase(SKColors.Transparent);
        return new EffectResult(result, SKMatrix.Identity);
    }

    /// <summary>
    /// The BorderEffect: a line of the color around the image, which grows by twice the width
    /// </summary>
    public static EffectResult Border(SKBitmap source, int width = 2, SKColor? color = null)
    {
        var result = NewBitmap(source.Width + width * 2, source.Height + width * 2);
        using (var canvas = new SKCanvas(result))
        {
            using var pen = new SKPaint
            {
                Color = color ?? SKColors.Black,
                StrokeWidth = width,
                Style = SKPaintStyle.Stroke,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
            };
            canvas.DrawRect(new SKRect(width / 2f, width / 2f, result.Width - width / 2f, result.Height - width / 2f), pen);
            canvas.DrawBitmap(source, width, width);
        }

        return new EffectResult(result, SKMatrix.CreateTranslation(width, width));
    }

    /// <summary>
    /// The DropShadowEffect: a blurred dark copy below the image (darkness 0.6, size 7, offset -1,-1)
    /// </summary>
    public static EffectResult DropShadow(SKBitmap source, float darkness = 0.6f, int shadowSize = 7, SKPointI? shadowOffset = null)
    {
        var offset = shadowOffset ?? new SKPointI(-1, -1);
        int imageX = offset.X + shadowSize - 1;
        int imageY = offset.Y + shadowSize - 1;
        var result = NewBitmap(source.Width + shadowSize * 2, source.Height + shadowSize * 2);
        using (var canvas = new SKCanvas(result))
        {
            // The mask: the shape of the image in black with the darkness as alpha, blurred
            using var shadowPaint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateColorMatrix(
                [
                    0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0,
                    0, 0, 0, darkness, 0,
                ]),
                ImageFilter = SKImageFilter.CreateBlur(shadowSize / 2f, shadowSize / 2f),
            };
            canvas.DrawBitmap(source, shadowSize, shadowSize, shadowPaint);
            canvas.DrawBitmap(source, imageX, imageY);
        }

        return new EffectResult(result, SKMatrix.CreateTranslation(imageX, imageY));
    }

    /// <summary>
    /// The TornEdgeEffect: the edges are cut with random teeth (height 12, every 20 pixels), then the drop shadow is added.
    /// Edges are top, right, bottom, left.
    /// </summary>
    public static EffectResult TornEdge(SKBitmap source, int toothHeight = 12, int horizontalToothRange = 20, int verticalToothRange = 20,
        bool[]? edges = null, bool shadow = true, int? seed = null)
    {
        edges ??= [true, true, true, true];
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        using var path = TornEdgePath(source.Width, source.Height, toothHeight, horizontalToothRange, verticalToothRange, edges, random);

        var torn = NewBitmap(source.Width, source.Height);
        using (var canvas = new SKCanvas(torn))
        {
            canvas.ClipPath(path, antialias: true);
            canvas.DrawBitmap(source, 0, 0);
        }

        if (!shadow)
        {
            return new EffectResult(torn, SKMatrix.Identity);
        }

        using (torn)
        {
            return DropShadow(torn);
        }
    }

    /// <summary>
    /// The outline of the torn image, the CreateTornEdge of ImageHelper: points along each torn edge at a random depth
    /// </summary>
    internal static SKPath TornEdgePath(int width, int height, int toothHeight, int horizontalRange, int verticalRange, bool[] edges, Random random)
    {
        int horizontalRegions = (int)Math.Round((float)width / horizontalRange);
        int verticalRegions = (int)Math.Round((float)height / verticalRange);
        int Tooth() => random.Next(1, Math.Max(2, toothHeight));

        var points = new List<SKPoint>();
        // Top, left to right
        if (edges[0])
        {
            points.Add(edges[3] ? new SKPoint(Tooth(), Tooth()) : new SKPoint(0, 0));
            for (int i = 1; i < horizontalRegions - 1; i++)
            {
                points.Add(new SKPoint(i * horizontalRange, Tooth()));
            }

            points.Add(new SKPoint(width - Tooth(), Tooth()));
        }
        else
        {
            points.Add(new SKPoint(0, 0));
            points.Add(new SKPoint(width, 0));
        }

        // Right, top to bottom
        if (edges[1])
        {
            for (int i = 1; i < verticalRegions - 1; i++)
            {
                points.Add(new SKPoint(width - Tooth(), i * verticalRange));
            }

            points.Add(new SKPoint(width - Tooth(), height - Tooth()));
        }
        else
        {
            points.Add(new SKPoint(width, height));
        }

        // Bottom, right to left
        if (edges[2])
        {
            for (int i = 1; i < horizontalRegions - 1; i++)
            {
                points.Add(new SKPoint(width - i * horizontalRange, height - Tooth()));
            }

            points.Add(new SKPoint(Tooth(), height - Tooth()));
        }
        else
        {
            points.Add(new SKPoint(0, height));
        }

        // Left, bottom to top
        if (edges[3])
        {
            for (int i = 1; i < verticalRegions - 1; i++)
            {
                points.Add(new SKPoint(Tooth(), height - i * verticalRange));
            }
        }

        var path = new SKPath();
        path.AddPoly(points.ToArray(), close: true);
        return path;
    }

    /// <summary>
    /// Scale the image to the size; the elements are scaled with it
    /// </summary>
    public static EffectResult Resize(SKBitmap source, int width, int height)
    {
        var result = NewBitmap(width, height);
        using (var canvas = new SKCanvas(result))
        using (var image = SKImage.FromBitmap(source))
        {
            canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        }

        return new EffectResult(result, SKMatrix.CreateScale((float)width / source.Width, (float)height / source.Height));
    }

    /// <summary>
    /// Grow or shrink the canvas on each side (negative values shrink), the ResizeCanvasEffect; new space is transparent
    /// </summary>
    public static EffectResult ResizeCanvas(SKBitmap source, int left, int top, int right, int bottom)
    {
        var result = NewBitmap(source.Width + left + right, source.Height + top + bottom);
        using (var canvas = new SKCanvas(result))
        {
            canvas.DrawBitmap(source, left, top);
        }

        return new EffectResult(result, SKMatrix.CreateTranslation(left, top));
    }

    /// <summary>
    /// The area without the border of one color around the image, the AutoCrop of Windows; null when there is nothing to crop
    /// </summary>
    public static SKRectI? FindAutoCropArea(SKBitmap source, int tolerance = 0)
    {
        if (source.Width < 3 || source.Height < 3)
        {
            return null;
        }

        var reference = source.GetPixel(0, 0);
        bool Same(SKColor c) =>
            Math.Abs(c.Red - reference.Red) <= tolerance && Math.Abs(c.Green - reference.Green) <= tolerance
            && Math.Abs(c.Blue - reference.Blue) <= tolerance && Math.Abs(c.Alpha - reference.Alpha) <= tolerance;

        int top = 0, bottom = source.Height - 1, left = 0, right = source.Width - 1;
        while (top < bottom && RowIs(source, top, Same))
        {
            top++;
        }

        if (top == bottom && RowIs(source, top, Same))
        {
            // The whole image is one color
            return null;
        }

        while (bottom > top && RowIs(source, bottom, Same))
        {
            bottom--;
        }

        while (left < right && ColumnIs(source, left, top, bottom, Same))
        {
            left++;
        }

        while (right > left && ColumnIs(source, right, top, bottom, Same))
        {
            right--;
        }

        var area = new SKRectI(left, top, right + 1, bottom + 1);
        bool unchanged = area.Width == source.Width && area.Height == source.Height;
        return unchanged || area.IsEmpty ? null : area;
    }

    private static bool RowIs(SKBitmap bitmap, int y, Func<SKColor, bool> same)
    {
        for (int x = 0; x < bitmap.Width; x++)
        {
            if (!same(bitmap.GetPixel(x, y)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ColumnIs(SKBitmap bitmap, int x, int top, int bottom, Func<SKColor, bool> same)
    {
        for (int y = top; y <= bottom; y++)
        {
            if (!same(bitmap.GetPixel(x, y)))
            {
                return false;
            }
        }

        return true;
    }

    private static EffectResult WithColorFilter(SKBitmap source, SKColorFilter filter)
    {
        var result = NewBitmap(source.Width, source.Height);
        using (filter)
        using (var canvas = new SKCanvas(result))
        using (var paint = new SKPaint { ColorFilter = filter })
        {
            canvas.DrawBitmap(source, 0, 0, paint);
        }

        return new EffectResult(result, SKMatrix.Identity);
    }
}
