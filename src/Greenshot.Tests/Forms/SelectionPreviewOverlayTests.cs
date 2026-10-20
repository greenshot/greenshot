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

using Dapplo.Windows.Common.Structs;
using Greenshot.Capturing.Overlays;
using Xunit;

namespace Greenshot.Tests.Forms;

/// <summary>
/// What the preview of the selection shows, and how much it is enlarged or reduced
/// </summary>
public class SelectionPreviewOverlayTests
{
    private static readonly NativeRect Capture = new NativeRect(0, 0, 1920, 1080);

    [Fact]
    public void GetArea_UsesTheSizeOfTheLabels_AndStaysInsideTheCapture()
    {
        // The region tool's labels include the pixel under the cursor
        Assert.Equal(new NativeRect(10, 20, 101, 51), SelectionPreviewOverlay.GetArea(new NativeRect(10, 20, 100, 50), new NativeSize(101, 51), Capture));
        Assert.Equal(new NativeRect(10, 20, 100, 50), SelectionPreviewOverlay.GetArea(new NativeRect(10, 20, 100, 50), NativeSize.Empty, Capture));
        Assert.Equal(new NativeRect(1900, 0, 20, 30), SelectionPreviewOverlay.GetArea(new NativeRect(1900, -10, 50, 40), NativeSize.Empty, Capture));
        Assert.True(SelectionPreviewOverlay.GetArea(NativeRect.Empty, NativeSize.Empty, Capture).IsEmpty);
        Assert.True(SelectionPreviewOverlay.GetArea(new NativeRect(3000, 0, 10, 10), NativeSize.Empty, Capture).IsEmpty);
    }

    [Theory]
    // Small: enlarged by a whole number, fitting the box
    [InlineData(16, 16, 12)]
    [InlineData(100, 10, 3)]
    // About the size of the box: not enlarged
    [InlineData(300, 150, 1)]
    // Large: reduced to fit the box exactly
    [InlineData(1280, 720, 0.25)]
    [InlineData(400, 800, 0.25)]
    public void GetScale_FitsTheBox_WholeNumbersWhenEnlarging(int width, int height, double expected)
    {
        Assert.Equal(expected, SelectionPreviewOverlay.GetScale(new NativeSize(width, height), 320, 200), 3);
    }

    [Fact]
    public void GetZoomerSize_KeepsTheProportions_InsideTheZoomer()
    {
        // 216 zoomer, a frame of 2 pixels: 212 inside
        Assert.Equal(new NativeSize(212, 212), SelectionPreviewOverlay.GetZoomerSize(new NativeSize(16, 16), 216, out double small));
        Assert.Equal(13, small);
        Assert.Equal(new NativeSize(216, 123), SelectionPreviewOverlay.GetZoomerSize(new NativeSize(1280, 720), 216, out double large));
        Assert.Equal(212.0 / 1280, large, 6);
    }
}
