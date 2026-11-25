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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Capturing;
using Xunit;

namespace Greenshot.Tests.Forms;

/// <summary>
/// The numbers of the numbered hotspots: reading order, stable while more are found, per tool
/// </summary>
public class HotspotNumberingTests
{
    private static CaptureFormHotspot Hotspot(int x, int y, string text, string toolId = "Text", bool numbered = true) => new CaptureFormHotspot
    {
        Bounds = new NativeRect(x, y, 50, 12),
        Text = text,
        ToolTipText = "value " + text,
        ToolId = toolId,
        IsNumbered = numbered
    };

    [Fact]
    public void Numbers_AreInReadingOrder()
    {
        var numbering = new HotspotNumbering();
        var numbered = numbering.Number(new[] { Hotspot(200, 10, "c"), Hotspot(10, 50, "d"), Hotspot(10, 10, "a"), Hotspot(100, 10, "b") }, "Text");

        Assert.Equal(new[] { "a", "b", "c", "d" }, numbered.Select(entry => entry.Hotspot.Text));
        Assert.Equal(new[] { 1, 2, 3, 4 }, numbered.Select(entry => entry.Number));
    }

    [Fact]
    public void Numbers_StayWhenMoreAreFound()
    {
        var numbering = new HotspotNumbering();
        numbering.Number(new[] { Hotspot(10, 50, "later"), Hotspot(10, 100, "last") }, "Text");

        // The features changed: new hotspot objects, one more above the others
        var numbered = numbering.Number(new[] { Hotspot(10, 10, "new"), Hotspot(10, 50, "later"), Hotspot(10, 100, "last") }, "Text");

        Assert.Equal(new[] { ("later", 1), ("last", 2), ("new", 3) }, numbered.Select(entry => (entry.Hotspot.Text, entry.Number)));
    }

    [Fact]
    public void Numbers_OnlyForNumberedHotspotsOfTheTool()
    {
        var numbering = new HotspotNumbering();
        var hotspots = new[] { Hotspot(10, 10, "text"), Hotspot(10, 20, "region", "Region"), Hotspot(10, 30, "qr", null, false), Hotspot(10, 40, "plain", "Text", false) };

        Assert.Equal(new[] { "text" }, numbering.Number(hotspots, "Text").Select(entry => entry.Hotspot.Text));
        Assert.Equal(new[] { ("region", 1) }, numbering.Number(hotspots, "Region").Select(entry => (entry.Hotspot.Text, entry.Number)));
        Assert.Empty(numbering.Number(hotspots, null));
    }

    [Fact]
    public void SameItemTwice_GetsOneNumber()
    {
        var numbering = new HotspotNumbering();
        var numbered = numbering.Number(new[] { Hotspot(10, 10, "a"), Hotspot(10, 10, "a") }, "Text");

        Assert.Single(numbered);
    }
}
