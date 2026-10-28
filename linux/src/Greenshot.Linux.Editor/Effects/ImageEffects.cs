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

    public static EffectResult DropShadow(SKBitmap source, DropShadowSettings settings) =>
        DropShadow(source, settings.Darkness, Math.Max(1, settings.ShadowSize), settings.ShadowOffset);

    /// <summary>
    /// The TornEdgeEffect with its settings: torn edges, the shadow, then the background color behind it
    /// </summary>
    public static EffectResult TornEdge(SKBitmap source, TornEdgeSettings settings)
    {
        var torn = TornEdge(source, settings.ToothHeight, settings.HorizontalToothRange, settings.VerticalToothRange, settings.Edges, shadow: false,
            seed: settings.Seed == 0 ? null : settings.Seed);
        var result = torn;
        if (settings.GenerateShadow)
        {
            using (torn.Image)
            {
                result = DropShadow(torn.Image, settings);
            }
        }

        if (settings.BackgroundColor.Alpha == 0)
        {
            return result;
        }

        var withBackground = NewBitmap(result.Image.Width, result.Image.Height);
        using (var canvas = new SKCanvas(withBackground))
        {
            canvas.Clear(settings.BackgroundColor);
            canvas.DrawBitmap(result.Image, 0, 0);
        }

        result.Image.Dispose();
        return new EffectResult(withBackground, result.ElementTransform);
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
    /// Black and white by the brightness of each pixel, the MonochromeEffect
    /// </summary>
    public static EffectResult Monochrome(SKBitmap source, byte threshold = 128)
    {
        var result = source.Copy(SKColorType.Bgra8888);
        var pixels = result.Pixels;
        for (int i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            byte value = (c.Red + c.Green + c.Blue) / 3 > threshold ? (byte)255 : (byte)0;
            pixels[i] = new SKColor(value, value, value, c.Alpha);
        }

        result.Pixels = pixels;
        return new EffectResult(result, SKMatrix.Identity);
    }

    /// <summary>
    /// Brightness, contrast and gamma like the AdjustEffect (1 changes nothing): the color matrix of Windows, then the gamma
    /// </summary>
    public static EffectResult Adjust(SKBitmap source, float brightness = 1f, float contrast = 1f, float gamma = 1f)
    {
        float offset = brightness - 1f;
        var table = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            table[i] = (byte)Math.Clamp(Math.Round(255 * Math.Pow(i / 255.0, gamma)), 0, 255);
        }

        using var matrix = SKColorFilter.CreateColorMatrix(
        [
            contrast, 0, 0, 0, offset,
            0, contrast, 0, 0, offset,
            0, 0, contrast, 0, offset,
            0, 0, 0, 1, 0,
        ]);
        using var gammaTable = SKColorFilter.CreateTable(null, table, table, table);
        using var filter = SKColorFilter.CreateCompose(gammaTable, matrix);
        var result = NewBitmap(source.Width, source.Height);
        using (var canvas = new SKCanvas(result))
        using (var paint = new SKPaint { ColorFilter = filter })
        {
            canvas.DrawBitmap(source, 0, 0, paint);
        }

        return new EffectResult(result, SKMatrix.Identity);
    }

    /// <summary>
    /// Rotate by the angle (clockwise, degrees), the canvas grows to hold the rotated image, the RotateEffect
    /// </summary>
    public static EffectResult Rotate(SKBitmap source, int angle)
    {
        angle = ((angle % 360) + 360) % 360;
        switch (angle)
        {
            case 0:
                return new EffectResult(source.Copy(), SKMatrix.Identity);
            case 90:
                return RotateClockwise(source);
            case 270:
                return RotateCounterClockwise(source);
        }

        var rotation = SKMatrix.CreateRotationDegrees(angle, source.Width / 2f, source.Height / 2f);
        var bounds = rotation.MapRect(SKRect.Create(source.Width, source.Height));
        var transform = rotation.PostConcat(SKMatrix.CreateTranslation(-bounds.Left, -bounds.Top));
        var result = NewBitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height));
        using (var canvas = new SKCanvas(result))
        using (var image = SKImage.FromBitmap(source))
        using (var paint = new SKPaint { IsAntialias = true })
        {
            canvas.SetMatrix(transform);
            canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear), paint);
        }

        return new EffectResult(result, transform);
    }

    /// <summary>
    /// At most the number of colors: the most used colors (5 bits per channel) and every pixel the nearest of them,
    /// the ReduceColorsEffect; transparency stays
    /// </summary>
    public static EffectResult ReduceColors(SKBitmap source, int colors = 256)
    {
        colors = Math.Clamp(colors, 2, 256);
        var result = source.Copy(SKColorType.Bgra8888);
        var pixels = result.Pixels;
        static int Bucket(SKColor c) => (c.Red >> 3) << 10 | (c.Green >> 3) << 5 | c.Blue >> 3;
        var counts = new Dictionary<int, (long Count, long R, long G, long B)>();
        foreach (var c in pixels)
        {
            int key = Bucket(c);
            counts.TryGetValue(key, out var entry);
            counts[key] = (entry.Count + 1, entry.R + c.Red, entry.G + c.Green, entry.B + c.Blue);
        }

        var palette = counts.Values.OrderByDescending(e => e.Count).Take(colors)
            .Select(e => new SKColor((byte)(e.R / e.Count), (byte)(e.G / e.Count), (byte)(e.B / e.Count))).ToArray();
        var nearest = new Dictionary<int, SKColor>();
        for (int i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            int key = Bucket(c);
            if (!nearest.TryGetValue(key, out var mapped))
            {
                mapped = palette.MinBy(p => (p.Red - c.Red) * (p.Red - c.Red) + (p.Green - c.Green) * (p.Green - c.Green) + (p.Blue - c.Blue) * (p.Blue - c.Blue));
                nearest[key] = mapped;
            }

            pixels[i] = mapped.WithAlpha(c.Alpha);
        }

        result.Pixels = pixels;
        return new EffectResult(result, SKMatrix.Identity);
    }

    /// <summary>
    /// The image on the color, without transparency, the RemoveTransparencyEffect (white by default)
    /// </summary>
    public static EffectResult RemoveTransparency(SKBitmap source, SKColor? color = null)
    {
        var result = NewBitmap(source.Width, source.Height);
        using (var canvas = new SKCanvas(result))
        {
            canvas.Clear((color ?? SKColors.White).WithAlpha(255));
            canvas.DrawBitmap(source, 0, 0);
        }

        return new EffectResult(result, SKMatrix.Identity);
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
