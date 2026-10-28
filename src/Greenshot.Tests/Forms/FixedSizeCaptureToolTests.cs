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

        Assert.Equal(new[] { new NativeSize(1280, 720), new NativeSize(32, 32), new NativeSize(500, 400) }, sizes.Select(entry => entry.Area.Size));
        Assert.Equal(new[] { false, false, true }, sizes.Select(entry => entry.Recent));
    }

    [Fact]
    public void GetSizes_TheSameSizeAtAnotherPlace_IsAnotherEntry()
    {
        var sizes = FixedSizeCaptureTool.GetSizes(new[] { "1280x720", "1280x720+0+0" }, new[] { "1280x720+0+0", "1280x720+10+20@1920+0", "1280x720+10+20@notepad.exe" });

        Assert.Equal(new[] { "1280x720", "1280x720+0+0", "1280x720+10+20@1920+0", "1280x720+10+20@notepad.exe" }, sizes.Select(entry => entry.Area.ToString()));
        Assert.Equal(new[] { false, false, true, true }, sizes.Select(entry => entry.Recent));
    }

    [Theory]
    [InlineData("1280x720", 1280, 720, null, null, null, null, null)]
    [InlineData("1280x720+100+200", 1280, 720, 100, 200, null, null, null)]
    [InlineData(" 800 x 600 + 10 - 30 ", 800, 600, 10, -30, null, null, null)]
    [InlineData("800x600+100+200@-1920+0", 800, 600, 100, 200, -1920, 0, null)]
    [InlineData("800x600+100+200@0+0", 800, 600, 100, 200, null, null, null)]
    [InlineData("800x600-8-30@notepad++.exe", 800, 600, -8, -30, null, null, "notepad++.exe")]
    public void FixedCaptureArea_Parse(string text, int width, int height, int? x, int? y, int? monitorX, int? monitorY, string program)
    {
        var area = FixedCaptureArea.Parse(text);

        Assert.Equal(new NativeSize(width, height), area.Size);
        NativePoint? offset = x.HasValue ? new NativePoint(x.Value, y.Value) : null;
        NativePoint? monitor = monitorX.HasValue ? new NativePoint(monitorX.Value, monitorY.Value) : null;
        Assert.Equal(offset, area.Offset);
        Assert.Equal(monitor, area.Monitor);
        Assert.Equal(program, area.Program);
    }

    [Theory]
    [InlineData("1280x720+100")]
    [InlineData("1280x720@notepad.exe")]
    [InlineData("1280x720+1+2@")]
    [InlineData("1280x720+1+2@c:\\notepad.exe")]
    [InlineData("1280x720+1+2@1920")]
    [InlineData("0x720+1+2")]
    [InlineData(null)]
    public void FixedCaptureArea_Parse_NoArea(string text)
    {
        Assert.True(FixedCaptureArea.Parse(text).IsEmpty);
    }

    [Theory]
    [InlineData("1280x720")]
    [InlineData("1280x720+100+200")]
    [InlineData("1280x720-8-30@notepad.exe")]
    [InlineData("1280x720+100+200@-1920-1080")]
    [InlineData("1280x720+100+200@1920+0")]
    public void FixedCaptureArea_ToString_IsWhatParseReads(string text)
    {
        Assert.Equal(text, FixedCaptureArea.Parse(text).ToString());
    }

    [Fact]
    public void FixedCaptureArea_IsProgram_WithOrWithoutExe()
    {
        var area = FixedCaptureArea.Parse("10x10+0+0@Notepad.exe");

        Assert.True(area.IsProgram("notepad"));
        Assert.True(area.IsProgram("NOTEPAD.EXE"));
        Assert.False(area.IsProgram("notepad2"));
    }

    [Fact]
    public void ResolveMonitorPlace_ThePlace_WhenItIsOnTheScreens_ElseTheSamePlaceOnTheMonitorWithTheCursor()
    {
        var main = new NativeRect(0, 0, 1920, 1080);
        var second = new NativeRect(1920, 0, 1920, 1080);
        var area = FixedCaptureArea.Parse("800x600+100+200@1920+0");

        // Both monitors: the place itself, also with the cursor on the main monitor
        Assert.Equal((NativeRect?)new NativeRect(2020, 200, 800, 600), FixedSizeCaptureTool.ResolveMonitorPlace(area, new[] { main, second }, main));
        // The second monitor is off: the same place on the main monitor
        Assert.Equal((NativeRect?)new NativeRect(100, 200, 800, 600), FixedSizeCaptureTool.ResolveMonitorPlace(area, new[] { main }, main));
        // It doesn't fit on the monitor with the cursor
        Assert.Null(FixedSizeCaptureTool.ResolveMonitorPlace(area, new[] { main }, new NativeRect(0, 0, 800, 600)));
        // No place, or one in a window
        Assert.Null(FixedSizeCaptureTool.ResolveMonitorPlace(FixedCaptureArea.Parse("800x600"), new[] { main }, main));
        Assert.Null(FixedSizeCaptureTool.ResolveMonitorPlace(FixedCaptureArea.Parse("800x600+0+0@notepad.exe"), new[] { main }, main));
    }

    [Fact]
    public void ResolveWindowPlaces_InEachWindowOfTheProgram_TheOneUnderTheCursorFirst()
    {
        var screens = new[] { new NativeRect(0, 0, 1920, 1080) };
        var area = FixedCaptureArea.Parse("100x50+10+20@notepad.exe");
        var windows = new[]
        {
            // Top: another program, covering the place in the window below it completely
            (Bounds: new NativeRect(0, 0, 300, 300), Client: new NativeRect(8, 30, 284, 262), IsProgram: false),
            (Bounds: new NativeRect(50, 50, 400, 300), Client: new NativeRect(58, 80, 384, 262), IsProgram: true),
            (Bounds: new NativeRect(1000, 100, 400, 300), Client: new NativeRect(1008, 130, 384, 262), IsProgram: true),
            // Too small for the area
            (Bounds: new NativeRect(1500, 600, 100, 60), Client: new NativeRect(1508, 630, 84, 22), IsProgram: true),
            (Bounds: new NativeRect(600, 600, 400, 300), Client: new NativeRect(608, 630, 384, 262), IsProgram: true),
        };

        var places = FixedSizeCaptureTool.ResolveWindowPlaces(area, windows, screens, new NativePoint(700, 700));

        Assert.Equal(new[] { new NativeRect(618, 650, 100, 50), new NativeRect(1018, 150, 100, 50) }, places);
    }

    [Fact]
    public void AddRecent_NewestFirst_WithoutAnOlderCopy_AtMostFive()
    {
        var recent = new[] { "10x10+0+0", "20x20+0+0", "10x10+5+5", "30x30+0+0", "40x40+0+0", "50x50+0+0" };

        var areas = FixedSizeCaptureTool.AddRecent(recent, FixedCaptureArea.Parse("10x10+5+5"));

        Assert.Equal(new[] { "10x10+5+5", "10x10+0+0", "20x20+0+0", "30x30+0+0", "40x40+0+0" }, areas);
    }

    [Fact]
    public void FitsOnScreens_EveryPixelOnAMonitor()
    {
        // A main monitor and a smaller one on its left, aligned at the top: below the small one is no screen
        var screens = new[] { new NativeRect(0, 0, 1920, 1080), new NativeRect(-1280, 0, 1280, 720) };

        Assert.True(FixedSizeCaptureTool.FitsOnScreens(new NativeRect(100, 100, 800, 600), screens));
        // Over both monitors
        Assert.True(FixedSizeCaptureTool.FitsOnScreens(new NativeRect(-200, 100, 400, 300), screens));
        // Partly below the small monitor
        Assert.False(FixedSizeCaptureTool.FitsOnScreens(new NativeRect(-200, 600, 400, 300), screens));
        // Partly beyond the right edge
        Assert.False(FixedSizeCaptureTool.FitsOnScreens(new NativeRect(1800, 0, 200, 100), screens));
    }

    [Theory]
    [InlineData(FixedCaptureAnchor.TopLeft)]
    [InlineData(FixedCaptureAnchor.TopRight)]
    [InlineData(FixedCaptureAnchor.BottomRight)]
    [InlineData(FixedCaptureAnchor.BottomLeft)]
    [InlineData(FixedCaptureAnchor.Center)]
    public void GetAnchorPoint_PlacesTheAreaThereAgain(FixedCaptureAnchor anchor)
    {
        var rect = new NativeRect(100, 200, 61, 41);

        Assert.Equal(rect, FixedSizeCaptureTool.Place(FixedSizeCaptureTool.GetAnchorPoint(rect, anchor), rect.Size, anchor));
    }

    [Fact]
    public void IsClose_WithinTheDistanceInEachDirection()
    {
        var place = new NativeRect(100, 100, 50, 50);

        Assert.True(FixedSizeCaptureTool.IsClose(new NativeRect(120, 80, 50, 50), place, 24));
        Assert.False(FixedSizeCaptureTool.IsClose(new NativeRect(125, 100, 50, 50), place, 24));
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
