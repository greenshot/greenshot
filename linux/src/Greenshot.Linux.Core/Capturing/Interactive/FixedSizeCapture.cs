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

namespace Greenshot.Linux.Core.Capturing.Interactive;

/// <summary>
/// Where the cursor holds the area of the fixed size capture (FixedCaptureAnchor of Windows), C changes it
/// </summary>
public enum FixedCaptureAnchor
{
    TopLeft,
    TopRight,
    BottomRight,
    BottomLeft,
    Center,
}

/// <summary>
/// A size of the fixed size capture: from the settings, or one of the last region captures
/// </summary>
public sealed record FixedCaptureSize(SKSizeI Size, bool Recent);

/// <summary>
/// The fixed size capture (F on the capture screen), the FixedSizeCaptureTool of Windows: an area of a chosen size follows the
/// cursor, held at a corner or in the middle; its edges snap to the window under the cursor and the monitor.
/// </summary>
public static class FixedSizeCapture
{
    public const string DefaultSizes = "640x480,800x600,1024x768,1280x720,1920x1080";

    /// <summary>
    /// The number of remembered sizes of region captures
    /// </summary>
    public const int RecentSizesCount = 5;

    /// <summary>
    /// An edge closer than this to the edge of a window or monitor snaps to it
    /// </summary>
    public const int SnapDistance = 8;

    /// <summary>
    /// The area of the size with the cursor at the anchor: at a corner the cursor's pixel is the corner pixel of the area,
    /// so the zoomer shows exactly where the area starts or ends
    /// </summary>
    public static SKRectI Place(SKPointI cursor, SKSizeI size, FixedCaptureAnchor anchor)
    {
        int left = anchor switch
        {
            FixedCaptureAnchor.TopRight or FixedCaptureAnchor.BottomRight => cursor.X - size.Width + 1,
            FixedCaptureAnchor.Center => cursor.X - size.Width / 2,
            _ => cursor.X,
        };
        int top = anchor switch
        {
            FixedCaptureAnchor.BottomLeft or FixedCaptureAnchor.BottomRight => cursor.Y - size.Height + 1,
            FixedCaptureAnchor.Center => cursor.Y - size.Height / 2,
            _ => cursor.Y,
        };
        return SKRectI.Create(left, top, size.Width, size.Height);
    }

    /// <summary>
    /// Move the rectangle so the edge closest to an edge of the bounds lies on it, for each direction, when it is within the distance
    /// </summary>
    public static SKRectI Snap(SKRectI rect, IEnumerable<SKRectI> bounds, int distance)
    {
        var list = bounds.ToList();
        int dx = Nearest(list.SelectMany(b => new[] { b.Left, b.Right }), rect.Left, rect.Right, distance);
        int dy = Nearest(list.SelectMany(b => new[] { b.Top, b.Bottom }), rect.Top, rect.Bottom, distance);
        return SKRectI.Create(rect.Left + dx, rect.Top + dy, rect.Width, rect.Height);
    }

    private static int Nearest(IEnumerable<int> edges, int start, int end, int distance)
    {
        int move = 0;
        int moveDistance = distance + 1;
        foreach (int edge in edges)
        {
            foreach (int candidate in new[] { edge - start, edge - end })
            {
                if (Math.Abs(candidate) < moveDistance)
                {
                    move = candidate;
                    moveDistance = Math.Abs(candidate);
                }
            }
        }

        return move;
    }

    /// <summary>
    /// Move the rectangle into the bounds, when it fits
    /// </summary>
    public static SKRectI KeepInside(SKRectI rect, SKRectI bounds)
    {
        if (rect.Width > bounds.Width || rect.Height > bounds.Height)
        {
            return rect;
        }

        int x = Math.Min(Math.Max(rect.Left, bounds.Left), bounds.Right - rect.Width);
        int y = Math.Min(Math.Max(rect.Top, bounds.Top), bounds.Bottom - rect.Height);
        return SKRectI.Create(x, y, rect.Width, rect.Height);
    }

    /// <summary>
    /// "1280x720" (also with × or spaces) to a size, empty when it isn't one
    /// </summary>
    public static SKSizeI ParseSize(string? text)
    {
        var parts = text?.Split('x', 'X', '×');
        return parts?.Length == 2 && int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
                                  && int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int height) && width > 0 && height > 0
            ? new SKSizeI(width, height)
            : SKSizeI.Empty;
    }

    public static string FormatSize(SKSizeI size) => string.Create(CultureInfo.InvariantCulture, $"{size.Width}x{size.Height}");

    /// <summary>
    /// The configured sizes, then the recent sizes which aren't configured
    /// </summary>
    public static List<FixedCaptureSize> GetSizes(IEnumerable<string> configured, IEnumerable<string> recent)
    {
        var sizes = new List<FixedCaptureSize>();
        void Add(IEnumerable<string> texts, bool isRecent)
        {
            foreach (var size in texts.Select(ParseSize))
            {
                if (!size.IsEmpty && sizes.All(s => s.Size != size))
                {
                    sizes.Add(new FixedCaptureSize(size, isRecent));
                }
            }
        }

        Add(configured, false);
        Add(recent, true);
        return sizes;
    }

    /// <summary>
    /// The recent sizes with this one first, at most <see cref="RecentSizesCount"/>
    /// </summary>
    public static IReadOnlyList<string> RememberSize(IEnumerable<string> recent, SKSizeI size)
    {
        var sizes = recent.Select(ParseSize).Where(s => !s.IsEmpty && s != size).Take(RecentSizesCount - 1).Select(FormatSize).ToList();
        sizes.Insert(0, FormatSize(size));
        return sizes;
    }
}
