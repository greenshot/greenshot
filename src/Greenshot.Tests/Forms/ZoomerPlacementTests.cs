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

using Dapplo.Windows.Common.Structs;
using Greenshot.Capturing;
using Xunit;

namespace Greenshot.Tests.Forms;

/// <summary>
/// Where the zoomer of the capture window goes
/// </summary>
public class ZoomerPlacementTests
{
    private static readonly NativeRect Screen = new NativeRect(0, 0, 1920, 1080);
    private const int Size = 216;
    private static readonly NativePoint BottomRight = new NativePoint(ZoomerPlacement.Distance, ZoomerPlacement.Distance);

    [Fact]
    public void Size_IsAFifthOfTheSmallerSide_RoundedToAMultipleOf4()
    {
        Assert.Equal(216, ZoomerPlacement.GetSize(Screen));
        Assert.Equal(0, ZoomerPlacement.GetSize(new NativeRect(0, 0, 1366, 768)) % 4);
    }

    [Fact]
    public void Offset_StaysWhenItFits()
    {
        var offset = ZoomerPlacement.GetOffset(new NativePoint(500, 500), BottomRight, Size, Screen, NativeRect.Empty);
        Assert.Equal(BottomRight, offset);
    }

    [Fact]
    public void Offset_MovesLeftAtTheRightEdge()
    {
        var offset = ZoomerPlacement.GetOffset(new NativePoint(1900, 500), BottomRight, Size, Screen, NativeRect.Empty);
        Assert.Equal(new NativePoint(-ZoomerPlacement.Distance - Size, ZoomerPlacement.Distance), offset);
    }

    [Fact]
    public void Offset_MovesUpAtTheBottomRightCorner()
    {
        var offset = ZoomerPlacement.GetOffset(new NativePoint(1900, 1070), BottomRight, Size, Screen, NativeRect.Empty);
        Assert.Equal(new NativePoint(-ZoomerPlacement.Distance - Size, -ZoomerPlacement.Distance - Size), offset);
    }

    [Fact]
    public void Offset_AvoidsTheSelection()
    {
        // Dragging from the top left to the cursor: the selection is above and left of it, bottom right is free
        var cursor = new NativePoint(800, 300);
        var selection = new NativeRect(100, 100, 700, 200);
        var topLeft = new NativePoint(-ZoomerPlacement.Distance - Size, -ZoomerPlacement.Distance - Size);
        var offset = ZoomerPlacement.GetOffset(cursor, topLeft, Size, Screen, selection);
        Assert.Equal(BottomRight, offset);
    }

    [Fact]
    public void Offset_GoesOverTheSelectionWhenNothingElseFits()
    {
        // The selection covers the whole screen
        var offset = ZoomerPlacement.GetOffset(new NativePoint(500, 500), new NativePoint(5000, 5000), Size, Screen, Screen);
        Assert.Equal(BottomRight, offset);
    }
}
