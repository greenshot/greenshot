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
using Greenshot.Linux.Core.Capturing.Interactive;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Capturing;

public class SelectionModelTests
{
    private static readonly SKRectI Image = new(0, 0, 1000, 800);

    private static readonly SelectableWindow Floating = new("Floating", new SKRectI(100, 100, 300, 300));
    private static readonly SelectableWindow Background = new("Background", new SKRectI(0, 0, 1000, 800));

    private static (SelectionModel Model, List<SelectionResult?> Results) Create(CaptureMode mode = CaptureMode.Region,
        params SelectableWindow[] windows)
    {
        var model = new SelectionModel(Image, windows, mode, new SKPointI(500, 400));
        var results = new List<SelectionResult?>();
        model.Completed += results.Add;
        return (model, results);
    }

    [Fact]
    public void Dragging_selects_a_region_with_both_corners_included()
    {
        var (model, results) = Create();
        model.MoveTo(new SKPointI(10, 20));
        model.Press();
        model.MoveTo(new SKPointI(109, 69));
        model.Release();

        var result = Assert.Single(results);
        Assert.Equal(new SKRectI(10, 20, 110, 70), result!.Pixels);
        Assert.Equal(CaptureMode.Region, result.Mode);
    }

    [Fact]
    public void Dragging_backwards_gives_the_same_region()
    {
        var (model, results) = Create();
        model.MoveTo(new SKPointI(109, 69));
        model.Press();
        model.MoveTo(new SKPointI(10, 20));
        model.Release();
        Assert.Equal(new SKRectI(10, 20, 110, 70), results.Single()!.Pixels);
    }

    [Fact]
    public void A_click_in_region_mode_captures_the_window_under_the_cursor()
    {
        var (model, results) = Create(CaptureMode.Region, Floating, Background);
        model.MoveTo(new SKPointI(150, 150));
        model.Press();
        model.MoveTo(new SKPointI(151, 151));
        model.Release();

        var result = Assert.Single(results);
        Assert.Equal(Floating.Pixels, result!.Pixels);
        Assert.Equal(CaptureMode.Window, result.Mode);
        Assert.Equal("Floating", result.Title);
    }

    [Fact]
    public void A_click_without_windows_starts_over()
    {
        var (model, results) = Create();
        model.Press();
        model.Release();
        Assert.Empty(results);
        Assert.False(model.IsSelecting);
    }

    [Fact]
    public void The_top_most_window_wins()
    {
        var (model, _) = Create(CaptureMode.Window, Floating, Background);
        model.MoveTo(new SKPointI(200, 200));
        Assert.Equal("Floating", model.HoveredWindow!.Title);
        model.MoveTo(new SKPointI(600, 600));
        Assert.Equal("Background", model.HoveredWindow!.Title);
    }

    [Fact]
    public void Keyboard_selection_with_enter_and_arrows()
    {
        var (model, results) = Create();
        model.Confirm();
        model.MoveBy(10, 0);
        model.MoveBy(0, -10);
        model.Confirm();
        Assert.Equal(new SKRectI(500, 390, 511, 401), results.Single()!.Pixels);
    }

    [Fact]
    public void The_cursor_is_unknown_until_it_moves()
    {
        var (model, _) = Create();
        Assert.False(model.IsCursorKnown);
        model.MoveTo(model.Cursor);
        Assert.True(model.IsCursorKnown);
    }

    [Fact]
    public void The_cursor_stays_on_the_image()
    {
        var (model, _) = Create();
        model.MoveTo(new SKPointI(-50, 5000));
        Assert.Equal(new SKPointI(0, 799), model.Cursor);
    }

    [Fact]
    public void Window_mode_needs_windows()
    {
        var (model, _) = Create(CaptureMode.Window);
        Assert.Equal(CaptureMode.Region, model.Mode);
        model.ToggleMode();
        Assert.Equal(CaptureMode.Region, model.Mode);
    }

    [Fact]
    public void Tab_cycles_through_the_windows()
    {
        var (model, results) = Create(CaptureMode.Window, Floating, Background);
        model.MoveTo(new SKPointI(600, 600));
        model.SelectNextWindow(backwards: false);
        Assert.Equal("Floating", model.HoveredWindow!.Title);
        model.SelectNextWindow(backwards: false);
        Assert.Equal("Background", model.HoveredWindow!.Title);
        model.SelectNextWindow(backwards: true);
        model.Confirm();
        Assert.Equal("Floating", results.Single()!.Title);
    }

    [Fact]
    public void Cancel_completes_without_result_once()
    {
        var (model, results) = Create();
        model.Cancel();
        model.Cancel();
        model.Confirm();
        Assert.Equal([null], results);
    }

    [Fact]
    public void Windows_outside_the_image_are_ignored()
    {
        var (model, _) = Create(CaptureMode.Window, new SelectableWindow("Gone", new SKRectI(2000, 2000, 2100, 2100)));
        Assert.False(model.CanSelectWindows);
    }

    private static readonly Greenshot.Linux.Core.Barcodes.DetectedBarcode Code =
        new("https://getgreenshot.org", "QR_CODE", [new SKPoint(500, 500), new SKPoint(560, 500), new SKPoint(500, 560)]);

    [Fact]
    public void A_click_on_a_qr_code_frame_shows_its_menu_instead_of_selecting()
    {
        var (model, results) = Create();
        Greenshot.Linux.Core.Barcodes.DetectedBarcode? clicked = null;
        model.HotspotClicked += code => clicked = code;
        model.SetHotspots([Code]);

        model.MoveTo(new SKPointI(530, 530));
        Assert.Same(Code, model.HoveredHotspot);
        model.Press();

        Assert.Same(Code, clicked);
        Assert.False(model.IsSelecting);
        Assert.Empty(results);
    }

    [Fact]
    public void The_codes_in_the_selection_come_with_the_result()
    {
        var (model, results) = Create();
        var outside = new Greenshot.Linux.Core.Barcodes.DetectedBarcode("other", "QR_CODE", [new SKPoint(10, 10), new SKPoint(40, 40)]);
        model.SetHotspots([Code, outside]);

        model.MoveTo(new SKPointI(450, 450));
        model.Press();
        // While selecting, the frames don't take the click
        model.MoveTo(new SKPointI(600, 600));
        Assert.Null(model.HoveredHotspot);
        model.Release();

        var result = Assert.Single(results);
        Assert.Same(Code, Assert.Single(result!.Barcodes));
    }

    private static IReadOnlyList<FixedCaptureSize> Sizes => FixedSizeCapture.GetSizes(["640x480", "100x50"], ["300x200", "640x480"]);

    [Fact]
    public void Fixed_sizes_are_the_configured_ones_then_new_recent_ones()
    {
        Assert.Equal([new FixedCaptureSize(new SKSizeI(640, 480), false), new FixedCaptureSize(new SKSizeI(100, 50), false), new FixedCaptureSize(new SKSizeI(300, 200), true)], Sizes);
        Assert.Equal(new SKSizeI(1280, 720), FixedSizeCapture.ParseSize(" 1280 × 720 "));
        Assert.True(FixedSizeCapture.ParseSize("12x").IsEmpty);
        Assert.Equal(["10x10", "1x2", "3x4", "5x6", "7x8"], FixedSizeCapture.RememberSize(["1x2", "10x10", "3x4", "5x6", "7x8", "9x9"], new SKSizeI(10, 10)));
    }

    [Theory]
    [InlineData(FixedCaptureAnchor.TopLeft, 500, 400)]
    [InlineData(FixedCaptureAnchor.BottomRight, 401, 351)]
    [InlineData(FixedCaptureAnchor.Center, 450, 375)]
    public void The_cursor_holds_the_fixed_size_area_at_its_anchor(FixedCaptureAnchor anchor, int left, int top) =>
        Assert.Equal(SKRectI.Create(left, top, 100, 50), FixedSizeCapture.Place(new SKPointI(500, 400), new SKSizeI(100, 50), anchor));

    [Fact]
    public void The_fixed_size_area_snaps_to_the_window_and_stays_on_the_monitor()
    {
        var (model, results) = Create(CaptureMode.Region, Floating);
        model.Screens = [new SKRectI(0, 0, 1000, 800)];
        model.SetFixedSizes(Sizes, new SKSizeI(100, 50), FixedCaptureAnchor.TopLeft);
        model.ToggleFixedSize();
        Assert.Equal(1, model.FixedSizeIndex);

        // 4 pixels from the left edge of the window: it snaps there
        model.MoveTo(new SKPointI(104, 150));
        Assert.Equal(SKRectI.Create(100, 150, 100, 50), model.Highlight);
        model.SnapDisabled = true;
        model.MoveBy(0, 0);
        Assert.Equal(SKRectI.Create(104, 150, 100, 50), model.Highlight);

        // At the bottom right of the monitor it is moved inside
        model.MoveTo(new SKPointI(990, 790));
        Assert.Equal(SKRectI.Create(900, 750, 100, 50), model.Highlight);

        model.StepFixedSize(1);
        Assert.Equal(new SKSizeI(300, 200), model.FixedSizes[model.FixedSizeIndex].Size);
        model.NextFixedAnchor();
        Assert.Equal(FixedCaptureAnchor.TopRight, model.FixedAnchor);

        model.Press();
        model.Release();
        var result = Assert.Single(results)!;
        Assert.True(result.IsFixedSize);
        Assert.Equal(new SKSizeI(300, 200), new SKSizeI(result.Pixels.Width, result.Pixels.Height));
    }

    [Fact]
    public void Without_sizes_there_is_no_fixed_size_capture()
    {
        var (model, _) = Create();
        model.ToggleFixedSize();
        Assert.False(model.IsFixedSize);
    }
}
