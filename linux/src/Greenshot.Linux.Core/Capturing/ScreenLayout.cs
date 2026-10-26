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

using System.Globalization;
using SkiaSharp;

namespace Greenshot.Linux.Core.Capturing;

/// <summary>
/// A rectangle in the coordinates of the compositor layout (logical pixels, the space all outputs are placed in).
/// </summary>
public readonly record struct LayoutRect(int X, int Y, int Width, int Height)
{
    public static LayoutRect Empty => default;

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public int Right => X + Width;

    public int Bottom => Y + Height;

    public bool Contains(double x, double y) => x >= X && x < Right && y >= Y && y < Bottom;

    public LayoutRect Union(LayoutRect other)
    {
        if (IsEmpty)
        {
            return other;
        }

        if (other.IsEmpty)
        {
            return this;
        }

        int left = Math.Min(X, other.X);
        int top = Math.Min(Y, other.Y);
        return new LayoutRect(left, top, Math.Max(Right, other.Right) - left, Math.Max(Bottom, other.Bottom) - top);
    }

    public LayoutRect Intersect(LayoutRect other)
    {
        int left = Math.Max(X, other.X);
        int top = Math.Max(Y, other.Y);
        int right = Math.Min(Right, other.Right);
        int bottom = Math.Min(Bottom, other.Bottom);
        return right <= left || bottom <= top ? Empty : new LayoutRect(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// The format of NativeRect in greenshot.ini: x,y,width,height
    /// </summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{X},{Y},{Width},{Height}");

    public static bool TryParse(string? text, out LayoutRect rect)
    {
        rect = Empty;
        var parts = text?.Split(',', StringSplitOptions.TrimEntries);
        if (parts is not { Length: 4 })
        {
            return false;
        }

        var values = new int[4];
        for (int i = 0; i < 4; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[i]))
            {
                return false;
            }
        }

        rect = new LayoutRect(values[0], values[1], values[2], values[3]);
        return true;
    }
}

/// <summary>
/// One monitor: its output name (DP-1, eDP-1, ...), where it is in the layout and its scale
/// </summary>
public sealed record ScreenInfo(string Name, LayoutRect Bounds, double Scale, bool IsPrimary);

/// <summary>
/// All monitors and where they are in the layout
/// </summary>
public sealed class ScreenLayout
{
    public ScreenLayout(IReadOnlyList<ScreenInfo> screens)
    {
        if (screens.Count == 0)
        {
            throw new ArgumentException("A layout needs at least one screen", nameof(screens));
        }

        Screens = screens;
        Bounds = screens.Aggregate(LayoutRect.Empty, (union, screen) => union.Union(screen.Bounds));
    }

    public IReadOnlyList<ScreenInfo> Screens { get; }

    /// <summary>
    /// The bounding box of all screens
    /// </summary>
    public LayoutRect Bounds { get; }

    public ScreenInfo? ScreenAt(double x, double y) => Screens.FirstOrDefault(s => s.Bounds.Contains(x, y));
}

/// <summary>
/// A picture of all screens, and how its pixels relate to the layout. A grabber renders the layout bounding box,
/// usually at the highest scale of the outputs, so one layout pixel is ScaleX by ScaleY image pixels.
/// </summary>
public sealed class ScreenSnapshot : IDisposable
{
    public ScreenSnapshot(SKBitmap image, ScreenLayout layout, DateTime takenAt)
    {
        Image = image;
        Layout = layout;
        TakenAt = takenAt;
        ScaleX = (double)image.Width / layout.Bounds.Width;
        ScaleY = (double)image.Height / layout.Bounds.Height;
    }

    public SKBitmap Image { get; }

    public ScreenLayout Layout { get; }

    public DateTime TakenAt { get; }

    public double ScaleX { get; }

    public double ScaleY { get; }

    /// <summary>
    /// The image pixels which show the layout rectangle
    /// </summary>
    public SKRectI ToPixels(LayoutRect rect)
    {
        var bounds = Layout.Bounds;
        int left = (int)Math.Round((rect.X - bounds.X) * ScaleX);
        int top = (int)Math.Round((rect.Y - bounds.Y) * ScaleY);
        int right = (int)Math.Round((rect.Right - bounds.X) * ScaleX);
        int bottom = (int)Math.Round((rect.Bottom - bounds.Y) * ScaleY);
        var pixels = new SKRectI(left, top, right, bottom);
        pixels.Intersect(new SKRectI(0, 0, Image.Width, Image.Height));
        return pixels;
    }

    /// <summary>
    /// The layout rectangle shown by the image pixels
    /// </summary>
    public LayoutRect ToLayout(SKRectI pixels)
    {
        var bounds = Layout.Bounds;
        int left = (int)Math.Floor(pixels.Left / ScaleX) + bounds.X;
        int top = (int)Math.Floor(pixels.Top / ScaleY) + bounds.Y;
        int right = (int)Math.Ceiling(pixels.Right / ScaleX) + bounds.X;
        int bottom = (int)Math.Ceiling(pixels.Bottom / ScaleY) + bounds.Y;
        return new LayoutRect(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// Copy a part of the image, the caller owns the result
    /// </summary>
    public SKBitmap Crop(SKRectI pixels)
    {
        pixels.Intersect(new SKRectI(0, 0, Image.Width, Image.Height));
        if (pixels.IsEmpty)
        {
            throw new ArgumentException("The area is outside of the screens", nameof(pixels));
        }

        var result = new SKBitmap(new SKImageInfo(pixels.Width, pixels.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(result);
        canvas.DrawBitmap(Image, pixels, new SKRect(0, 0, pixels.Width, pixels.Height));
        return result;
    }

    public void Dispose() => Image.Dispose();
}
