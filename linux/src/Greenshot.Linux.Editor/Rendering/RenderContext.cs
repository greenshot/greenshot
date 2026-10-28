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

using Greenshot.Linux.Editor.Elements;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Rendering;

public enum RenderMode
{
    /// <summary>
    /// On screen in the editor: unfinished things like an empty text box are shown
    /// </summary>
    Edit,

    /// <summary>
    /// For saving and exporting, only the result
    /// </summary>
    Export,
}

/// <summary>
/// What an element needs to draw itself: the canvas, the bitmap behind it (filters read what was drawn so far)
/// and the number of a step label
/// </summary>
public sealed class RenderContext(SKCanvas canvas, SKBitmap target, RenderMode mode, Func<StepLabelElement, int> stepNumber,
    IReadOnlyList<DrawableElement>? elements = null)
{
    /// <summary>
    /// All elements of the surface in drawing order, the cut edges need the other cuts
    /// </summary>
    public IReadOnlyList<DrawableElement> Elements { get; } = elements ?? [];

    public SKCanvas Canvas { get; } = canvas;

    public SKBitmap Target { get; } = target;

    public RenderMode Mode { get; } = mode;

    public int StepNumber(StepLabelElement label) => stepNumber(label);

    public SKRectI TargetBounds => new(0, 0, Target.Width, Target.Height);

    /// <summary>
    /// A copy of what was drawn so far in the area, null when the area is outside the image.
    /// Filters (blur, pixelate, highlight) work on it, like they work on the bitmap below them on Windows.
    /// </summary>
    public SKBitmap? CopyDrawn(SKRectI area)
    {
        area.Intersect(TargetBounds);
        if (area.IsEmpty)
        {
            return null;
        }

        Canvas.Flush();
        var copy = new SKBitmap(new SKImageInfo(area.Width, area.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(copy);
        canvas.DrawBitmap(Target, area, new SKRect(0, 0, area.Width, area.Height));
        return copy;
    }
}

/// <summary>
/// Paints shared by the elements
/// </summary>
internal static class Painting
{
    public static SKPaint Stroke(SKColor color, float thickness) => new()
    {
        Color = color,
        StrokeWidth = thickness,
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
        StrokeJoin = SKStrokeJoin.Miter,
    };

    public static SKPaint Fill(SKColor color) => new()
    {
        Color = color,
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    public static bool IsVisible(SKColor color) => color.Alpha > 0;

    /// <summary>
    /// The shadow of the Windows editor: five copies of the shape, each one pixel further and lighter,
    /// from alpha 100 down to 20. The step is the offset in pixels, the alpha the one to use.
    /// </summary>
    public static void DrawShadow(Action<byte, int> drawStep)
    {
        const int steps = 5;
        double alpha = 100;
        const double alphaStep = 100.0 / steps;
        for (int step = 0; alpha >= 1.0; step++)
        {
            drawStep((byte)Math.Round(alpha), step);
            alpha -= alphaStep;
        }
    }

    public static SKColor ShadowColor(byte alpha) => new(0, 0, 0, alpha);
}
