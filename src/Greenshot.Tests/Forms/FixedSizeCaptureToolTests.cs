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

using System.Linq;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core.Enums;
using Greenshot.Capturing.Tools;
using Xunit;

namespace Greenshot.Tests.Forms;

/// <summary>
/// The sizes and the snapping of the fixed size capture
/// </summary>
public class FixedSizeCaptureToolTests
{
    [Theory]
    [InlineData("1280x720", 1280, 720)]
    [InlineData(" 32 X 32 ", 32, 32)]
    [InlineData("800×600", 800, 600)]
    [InlineData("0x10", 0, 0)]
    [InlineData("1280", 0, 0)]
    [InlineData("axb", 0, 0)]
    [InlineData(null, 0, 0)]
    public void ParseSize(string text, int width, int height)
    {
        Assert.Equal(new NativeSize(width, height), FixedSizeCaptureTool.ParseSize(text));
    }

    [Fact]
    public void GetSizes_ConfiguredFirst_ThenRecentWhichAreNotConfigured()
    {
        var sizes = FixedSizeCaptureTool.GetSizes(new[] { "1280x720", "bad", "32x32" }, new[] { "32x32", "500x400", "500x400" });

        Assert.Equal(new[] { new NativeSize(1280, 720), new NativeSize(32, 32), new NativeSize(500, 400) }, sizes.Select(entry => entry.Size));
        Assert.Equal(new[] { false, false, true }, sizes.Select(entry => entry.Recent));
    }

    [Fact]
    public void Snap_MovesTheClosestEdgeOntoAnEdgeWithinTheDistance()
    {
        var window = new NativeRect(100, 100, 400, 300);

        // Left edge 5 pixels right of the window's left edge, bottom edge 3 pixels above the window's bottom edge
        var snapped = FixedSizeCaptureTool.Snap(new NativeRect(105, 200, 50, 197), new[] { window }, 8);
        Assert.Equal(new NativeRect(100, 203, 50, 197), snapped);

        // Too far away
        var free = new NativeRect(120, 120, 50, 50);
        Assert.Equal(free, FixedSizeCaptureTool.Snap(free, new[] { window }, 8));
    }

    [Theory]
    [InlineData(FixedCaptureAnchor.TopLeft, 100, 200)]
    [InlineData(FixedCaptureAnchor.TopRight, 41, 200)]
    [InlineData(FixedCaptureAnchor.BottomRight, 41, 161)]
    [InlineData(FixedCaptureAnchor.BottomLeft, 100, 161)]
    [InlineData(FixedCaptureAnchor.Center, 70, 180)]
    public void Place_TheCursorPixelIsTheCornerPixelOfTheArea(FixedCaptureAnchor anchor, int left, int top)
    {
        var rect = FixedSizeCaptureTool.Place(new NativePoint(100, 200), new NativeSize(60, 40), anchor);

        Assert.Equal(new NativeRect(left, top, 60, 40), rect);
        // The cursor is on the area
        Assert.True(rect.Contains(new NativePoint(100, 200)));
    }

    [Fact]
    public void KeepInside_MovesTheRectangleIntoTheBounds_WhenItFits()
    {
        var monitor = new NativeRect(0, 0, 1920, 1080);

        Assert.Equal(new NativeRect(1820, 0, 100, 100), FixedSizeCaptureTool.KeepInside(new NativeRect(1850, -20, 100, 100), monitor));
        var tooLarge = new NativeRect(-10, -10, 2000, 100);
        Assert.Equal(tooLarge, FixedSizeCaptureTool.KeepInside(tooLarge, monitor));
    }
}
