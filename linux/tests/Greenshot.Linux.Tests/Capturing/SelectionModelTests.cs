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
}
