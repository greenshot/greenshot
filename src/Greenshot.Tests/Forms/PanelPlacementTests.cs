/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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


using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.UI.Capture;
using Xunit;

namespace Greenshot.Tests.Forms;

/// <summary>
/// Where the panels of capture tools and overlays go
/// </summary>
public class PanelPlacementTests
{
    private static readonly NativeRect Monitor = new NativeRect(0, 0, 1920, 1080);
    private static readonly NativeSize Size = new NativeSize(300, 200);
    private const int M = PanelPlacement.Margin;

    [Fact]
    public void NewPanel_GoesToTheCornerFarthestFromTheCursor()
    {
        var bounds = PanelPlacement.Place(Size, Monitor, new NativePoint(100, 100), NativeRect.Empty, null);
        Assert.Equal(new NativeRect(1920 - M - 300, 1080 - M - 200, 300, 200), bounds);
    }

    [Fact]
    public void Panel_StaysWhereItIs_WhenThatIsFine()
    {
        var current = new NativeRect(M, M, 300, 200);
        var bounds = PanelPlacement.Place(Size, Monitor, new NativePoint(1000, 600), current, null);
        Assert.Equal(current, bounds);
    }

    [Fact]
    public void Panel_Moves_WhenTheCursorComesClose()
    {
        var current = new NativeRect(M, M, 300, 200);
        var bounds = PanelPlacement.Place(Size, Monitor, new NativePoint(150, 150), current, null);
        Assert.NotEqual(current, bounds);
        Assert.Equal(new NativeRect(1920 - M - 300, 1080 - M - 200, 300, 200), bounds);
    }

    [Fact]
    public void Panel_AvoidsTheSelectionAndOtherPanels()
    {
        // The farthest corner (bottom right) is covered by the selection, the next one (bottom left) by another panel
        var avoid = new[] { new NativeRect(1500, 800, 400, 270), new NativeRect(M, 1080 - M - 200, 300, 200) };
        var bounds = PanelPlacement.Place(Size, Monitor, new NativePoint(100, 100), NativeRect.Empty, avoid);
        Assert.Equal(new NativeRect(1920 - M - 300, M, 300, 200), bounds);
    }

    [Fact]
    public void Panel_StaysAwayFromTheCursor_WhenEverythingIsCovered()
    {
        var avoid = new[] { Monitor };
        var bounds = PanelPlacement.Place(Size, Monitor, new NativePoint(100, 100), NativeRect.Empty, avoid);
        Assert.Equal(new NativeRect(1920 - M - 300, 1080 - M - 200, 300, 200), bounds);
    }

    [Fact]
    public void Panel_IsOnTheMonitorOfTheCursor()
    {
        var secondMonitor = new NativeRect(1920, 0, 1280, 1024);
        var bounds = PanelPlacement.Place(Size, secondMonitor, new NativePoint(3000, 900), NativeRect.Empty, null);
        Assert.Equal(new NativeRect(1920 + M, M, 300, 200), bounds);
    }

    [Fact]
    public void ResizedPanel_KeepsItsCorner()
    {
        // Bottom right: the right and bottom edges stay
        var current = new NativeRect(Monitor.Right - M - 300, Monitor.Bottom - M - 200, 300, 200);
        var bounds = PanelPlacement.Place(new NativeSize(320, 210), Monitor, new NativePoint(100, 100), current, null);
        Assert.Equal(new NativeRect(Monitor.Right - M - 320, Monitor.Bottom - M - 210, 320, 210), bounds);
    }

    [Fact]
    public void ResizedPanel_TopLeft_GrowsToTheRightAndDown()
    {
        var current = new NativeRect(M, M, 300, 200);
        var bounds = PanelPlacement.Place(new NativeSize(320, 210), Monitor, new NativePoint(1800, 1000), current, null);
        Assert.Equal(new NativeRect(M, M, 320, 210), bounds);
    }

    [Fact]
    public void Panel_Stays_WhenItOnlyTouchesTheSelection()
    {
        var current = new NativeRect(1920 - M - 300, 1080 - M - 200, 300, 200);
        // Overlaps the top left 30 x 20 pixels of the panel, a free corner exists
        var selection = new NativeRect(1000, 600, 640, 290);
        Assert.True(selection.IntersectsWith(current));
        var bounds = PanelPlacement.Place(Size, Monitor, new NativePoint(1200, 700), current, null, new[] { selection });
        Assert.Equal(current, bounds);
    }

    [Fact]
    public void Panel_Moves_WhenItCoversMuchOfTheSelection()
    {
        var current = new NativeRect(1920 - M - 300, 1080 - M - 200, 300, 200);
        var selection = new NativeRect(1500, 800, 400, 270);
        var bounds = PanelPlacement.Place(Size, Monitor, new NativePoint(1200, 700), current, null, new[] { selection });
        Assert.Equal(new NativeRect(M, M, 300, 200), bounds);
    }

    [Fact]
    public void Panel_Stays_WhenNoCornerIsFree()
    {
        // A maximized window as selection covers every corner: moving would only go from one overlap to another
        var current = new NativeRect(M, M, 300, 200);
        var bounds = PanelPlacement.Place(Size, Monitor, new NativePoint(1200, 700), current, null, new[] { Monitor });
        Assert.Equal(current, bounds);
    }

    [Fact]
    public void Zoomer_AvoidsPanels()
    {
        int size = 216;
        var cursor = new NativePoint(500, 500);
        var bottomRight = new NativePoint(ZoomerPlacement.Distance, ZoomerPlacement.Distance);
        var panel = new NativeRect(510, 510, 300, 300);
        var offset = ZoomerPlacement.GetOffset(cursor, bottomRight, size, Monitor, new[] { panel });
        Assert.Equal(new NativePoint(-ZoomerPlacement.Distance - size, ZoomerPlacement.Distance), offset);
    }
}
