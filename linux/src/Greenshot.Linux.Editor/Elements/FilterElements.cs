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

using System.Text.Json.Serialization;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Rendering;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Elements;

/// <summary>
/// The FilterContainer of Windows: an area which changes the pixels below it, with an optional border
/// </summary>
public abstract class FilterElement : DrawableElement
{
    public override void Draw(RenderContext context)
    {
        var rect = NormalizedBounds;
        var area = SKRectI.Round(rect);
        if (area.Width > 0 && area.Height > 0)
        {
            ApplyFilter(context, area);
        }

        if (Style.LineThickness > 0 && Painting.IsVisible(Style.LineColor))
        {
            RectangleElement.DrawRectangle(context.Canvas, rect, Style.LineThickness, Style.LineColor, SKColors.Transparent, Style.Shadow);
        }
    }

    protected abstract void ApplyFilter(RenderContext context, SKRectI area);

    /// <summary>
    /// Filled areas: a filter is hit everywhere inside
    /// </summary>
    public override bool HitTest(SKPoint point, float tolerance)
    {
        var rect = NormalizedBounds;
        rect.Inflate(tolerance, tolerance);
        return rect.Contains(point);
    }
}

/// <summary>
/// HighlightContainer with its prepared filters: text highlight (a marker), area highlight (everything else blurred and darker),
/// grayscale (everything else gray) and magnification
/// </summary>
public sealed class HighlightElement : FilterElement
{
    public override string WindowsTypeName => "HighlightContainer";

    public override IReadOnlyList<FieldType> Fields => Style.HighlightFilter switch
    {
        HighlightFilter.TextHighlight => [FieldType.HighlightFilter, FieldType.FillColor, FieldType.LineThickness, FieldType.LineColor, FieldType.Shadow],
        HighlightFilter.Magnification => [FieldType.HighlightFilter, FieldType.MagnificationFactor, FieldType.LineThickness, FieldType.LineColor, FieldType.Shadow],
        _ => [FieldType.HighlightFilter, FieldType.LineThickness, FieldType.LineColor, FieldType.Shadow],
    };

    public override ElementStyle CreateDefaultStyle() => new()
    {
        HighlightFilter = HighlightFilter.TextHighlight,
        FillColor = SKColors.Yellow,
        LineThickness = 0,
        LineColor = SKColors.Red,
        Shadow = false,
        MagnificationFactor = 2,
        BlurRadius = 3,
    };

    protected override void ApplyFilter(RenderContext context, SKRectI area)
    {
        var canvas = context.Canvas;
        switch (Style.HighlightFilter)
        {
            case HighlightFilter.TextHighlight:
                // Every channel becomes the smaller of the pixel and the color, like a marker on paper
                using (var paint = new SKPaint { Color = Style.FillColor.WithAlpha(255), BlendMode = SKBlendMode.Darken })
                {
                    canvas.DrawRect(area, paint);
                }

                break;
            case HighlightFilter.AreaHighlight:
                DrawOutside(context, area, SKImageFilter.CreateBlur(Style.BlurRadius, Style.BlurRadius, SKShaderTileMode.Clamp,
                    SKImageFilter.CreateColorFilter(Brightness(0.9f))));
                break;
            case HighlightFilter.Grayscale:
                DrawOutside(context, area, SKImageFilter.CreateColorFilter(Grayscale()));
                break;
            case HighlightFilter.Magnification:
                Magnify(context, area);
                break;
        }
    }

    /// <summary>
    /// The inverted filters of Windows: everything outside the area gets the filter
    /// </summary>
    private static void DrawOutside(RenderContext context, SKRectI area, SKImageFilter filter)
    {
        using (filter)
        using (var drawn = context.CopyDrawn(context.TargetBounds))
        {
            if (drawn == null)
            {
                return;
            }

            var canvas = context.Canvas;
            canvas.Save();
            canvas.ClipRect(area, SKClipOperation.Difference);
            using var paint = new SKPaint { ImageFilter = filter };
            canvas.DrawBitmap(drawn, 0, 0, paint);
            canvas.Restore();
        }
    }

    /// <summary>
    /// The middle of the area, magnified to fill the area
    /// </summary>
    private void Magnify(RenderContext context, SKRectI area)
    {
        int factor = Math.Max(2, Style.MagnificationFactor);
        int width = Math.Max(1, area.Width / factor);
        int height = Math.Max(1, area.Height / factor);
        var source = SKRectI.Create(area.MidX - width / 2, area.MidY - height / 2, width, height);
        using var drawn = context.CopyDrawn(source);
        if (drawn == null)
        {
            return;
        }

        using var image = SKImage.FromBitmap(drawn);
        context.Canvas.DrawImage(image, area, new SKSamplingOptions(SKCubicResampler.Mitchell));
    }

    internal static SKColorFilter Brightness(float factor) => SKColorFilter.CreateColorMatrix(
    [
        factor, 0, 0, 0, 0,
        0, factor, 0, 0, 0,
        0, 0, factor, 0, 0,
        0, 0, 0, 1, 0,
    ]);

    internal static SKColorFilter Grayscale() => SKColorFilter.CreateColorMatrix(
    [
        0.299f, 0.587f, 0.114f, 0, 0,
        0.299f, 0.587f, 0.114f, 0, 0,
        0.299f, 0.587f, 0.114f, 0, 0,
        0, 0, 0, 1, 0,
    ]);
}

/// <summary>
/// ObfuscateContainer: pixelate (the default) or blur what is below
/// </summary>
public sealed class ObfuscateElement : FilterElement
{
    public override string WindowsTypeName => "ObfuscateContainer";

    public override IReadOnlyList<FieldType> Fields => Style.ObfuscateFilter switch
    {
        ObfuscateFilter.Blur => [FieldType.ObfuscateFilter, FieldType.BlurRadius, FieldType.LineThickness, FieldType.LineColor, FieldType.Shadow],
        _ => [FieldType.ObfuscateFilter, FieldType.PixelSize, FieldType.LineThickness, FieldType.LineColor, FieldType.Shadow],
    };

    public override ElementStyle CreateDefaultStyle() => new()
    {
        ObfuscateFilter = ObfuscateFilter.Pixelize,
        PixelSize = 5,
        BlurRadius = 3,
        LineThickness = 0,
        LineColor = SKColors.Red,
        Shadow = false,
    };

    protected override void ApplyFilter(RenderContext context, SKRectI area)
    {
        if (Style.ObfuscateFilter == ObfuscateFilter.Blur)
        {
            Blur(context, area, Style.BlurRadius);
        }
        else
        {
            Pixelate(context, area, Style.PixelSize);
        }
    }

    private static void Blur(RenderContext context, SKRectI area, int radius)
    {
        // Take some pixels around the area, so the edges are blurred with their neighbours and not with nothing
        var source = area;
        source.Inflate(radius * 3, radius * 3);
        source.Intersect(context.TargetBounds);
        using var drawn = context.CopyDrawn(source);
        if (drawn == null)
        {
            return;
        }

        var canvas = context.Canvas;
        canvas.Save();
        canvas.ClipRect(area);
        using var paint = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(radius, radius, SKShaderTileMode.Clamp) };
        canvas.DrawBitmap(drawn, source.Left, source.Top, paint);
        canvas.Restore();
    }

    /// <summary>
    /// Every block of PixelSize pixels gets the average color of its pixels, the PixelizationFilter of Windows
    /// </summary>
    internal static void Pixelate(RenderContext context, SKRectI area, int pixelSize)
    {
        using var drawn = context.CopyDrawn(area);
        if (drawn == null)
        {
            return;
        }

        var visible = SKRectI.Intersect(area, context.TargetBounds);
        using var paint = new SKPaint { IsAntialias = false };
        for (int y = 0; y < drawn.Height; y += pixelSize)
        {
            for (int x = 0; x < drawn.Width; x += pixelSize)
            {
                int width = Math.Min(pixelSize, drawn.Width - x);
                int height = Math.Min(pixelSize, drawn.Height - y);
                paint.Color = Average(drawn, x, y, width, height);
                context.Canvas.DrawRect(SKRect.Create(visible.Left + x, visible.Top + y, width, height), paint);
            }
        }
    }

    private static SKColor Average(SKBitmap bitmap, int left, int top, int width, int height)
    {
        long r = 0, g = 0, b = 0, a = 0;
        for (int y = top; y < top + height; y++)
        {
            for (int x = left; x < left + width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                r += color.Red;
                g += color.Green;
                b += color.Blue;
                a += color.Alpha;
            }
        }

        long count = (long)width * height;
        return new SKColor((byte)(r / count), (byte)(g / count), (byte)(b / count), (byte)(a / count));
    }
}

/// <summary>
/// An image pasted or dropped on the capture, the ImageContainer of Windows
/// </summary>
public sealed class ImageElement : DrawableElement, IDisposable
{
    private static readonly FieldType[] ImageFields = [FieldType.Shadow];
    private SKBitmap? _bitmap;

    /// <summary>
    /// The image as PNG, kept for saving and undo
    /// </summary>
    public byte[] Png { get; set; } = [];

    public override string WindowsTypeName => "ImageContainer";

    public override IReadOnlyList<FieldType> Fields => ImageFields;

    [JsonIgnore]
    public SKBitmap? Bitmap => _bitmap ??= Png.Length > 0 ? SKBitmap.Decode(Png) : null;

    public static ImageElement FromBitmap(SKBitmap bitmap, SKPoint location)
    {
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return new ImageElement
        {
            Png = data.ToArray(),
            Bounds = SKRect.Create(location.X, location.Y, bitmap.Width, bitmap.Height),
        };
    }

    public override ElementStyle CreateDefaultStyle() => new() { Shadow = false, LineThickness = 0 };

    public override void Draw(RenderContext context)
    {
        var rect = NormalizedBounds;
        if (Style.Shadow)
        {
            Painting.DrawShadow((alpha, step) =>
            {
                using var shadowPaint = Painting.Fill(Painting.ShadowColor(alpha));
                var shadowRect = rect;
                shadowRect.Offset(step + 1, step + 1);
                context.Canvas.DrawRect(shadowRect, shadowPaint);
            });
        }

        if (Bitmap is { } bitmap)
        {
            using var image = SKImage.FromBitmap(bitmap);
            context.Canvas.DrawImage(image, rect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }
    }

    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
