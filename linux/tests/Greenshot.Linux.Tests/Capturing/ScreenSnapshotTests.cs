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

using Greenshot.Linux.Core.Capturing;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Capturing;

public class ScreenSnapshotTests
{
    /// <summary>
    /// A 1920x1080 monitor at scale 1 left of a 1280x720 monitor at scale 2, grim renders the bounding box at scale 2
    /// </summary>
    private static ScreenSnapshot MixedScaleSnapshot()
    {
        var layout = new ScreenLayout(
        [
            new ScreenInfo("DP-1", new LayoutRect(0, 0, 1920, 1080), 1, true),
            new ScreenInfo("eDP-1", new LayoutRect(1920, 0, 1280, 720), 2, false),
        ]);
        return new ScreenSnapshot(new SKBitmap(3200 * 2, 1080 * 2), layout, DateTime.Now);
    }

    [Fact]
    public void Layout_bounds_cover_all_screens()
    {
        using var snapshot = MixedScaleSnapshot();
        Assert.Equal(new LayoutRect(0, 0, 3200, 1080), snapshot.Layout.Bounds);
        Assert.Equal(2, snapshot.ScaleX);
    }

    [Fact]
    public void Layout_rectangles_map_to_image_pixels_and_back()
    {
        using var snapshot = MixedScaleSnapshot();
        var area = new LayoutRect(1900, 100, 50, 40);
        var pixels = snapshot.ToPixels(area);
        Assert.Equal(new SKRectI(3800, 200, 3900, 280), pixels);
        Assert.Equal(area, snapshot.ToLayout(pixels));
    }

    [Fact]
    public void Pixels_outside_the_image_are_cut_off()
    {
        using var snapshot = MixedScaleSnapshot();
        var pixels = snapshot.ToPixels(new LayoutRect(3100, 1000, 500, 500));
        Assert.Equal(new SKRectI(6200, 2000, 6400, 2160), pixels);
    }

    [Fact]
    public void Layouts_with_negative_coordinates_start_at_pixel_zero()
    {
        var layout = new ScreenLayout(
        [
            new ScreenInfo("left", new LayoutRect(-1920, 0, 1920, 1080), 1, false),
            new ScreenInfo("main", new LayoutRect(0, 0, 1920, 1080), 1, true),
        ]);
        using var snapshot = new ScreenSnapshot(new SKBitmap(3840, 1080), layout, DateTime.Now);
        Assert.Equal(new SKRectI(1920, 0, 3840, 1080), snapshot.ToPixels(layout.Screens[1].Bounds));
    }

    [Fact]
    public void Crop_copies_the_pixels()
    {
        var layout = new ScreenLayout([new ScreenInfo("a", new LayoutRect(0, 0, 4, 4), 1, true)]);
        var image = new SKBitmap(4, 4);
        image.Erase(SKColors.Blue);
        image.SetPixel(2, 1, SKColors.Red);
        using var snapshot = new ScreenSnapshot(image, layout, DateTime.Now);
        using var crop = snapshot.Crop(new SKRectI(2, 1, 4, 3));
        Assert.Equal(2, crop.Width);
        Assert.Equal(SKColors.Red, crop.GetPixel(0, 0));
        Assert.Equal(SKColors.Blue, crop.GetPixel(1, 1));
    }

    [Theory]
    [InlineData("10,20,300,200", true)]
    [InlineData("-1920,0,1920,1080", true)]
    [InlineData("10,20,300", false)]
    [InlineData("a,b,c,d", false)]
    [InlineData("", false)]
    public void Layout_rectangles_parse_the_windows_format(string text, bool valid)
    {
        Assert.Equal(valid, LayoutRect.TryParse(text, out var rect));
        if (valid)
        {
            Assert.Equal(text, rect.ToString());
        }
    }
}
