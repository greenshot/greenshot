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


using System;
using System.Collections.Generic;
using System.Windows.Input;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.UI.Capture.Tools;
using Xunit;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;

namespace Greenshot.Tests.Forms;

/// <summary>
/// The region tool of the capture window, against a fake window
/// </summary>
public class RegionCaptureToolTests
{
    private sealed class FakeHost : ICaptureToolHost
    {
        public ICapture Capture => null;
        public NativeRect ScreenBounds => new NativeRect(0, 0, 1920, 1080);
        public NativePoint CursorPosition { get; set; }
        public ICaptureTool ActiveTool { get; set; }
        public IReadOnlyList<WindowDetails> Windows => Array.Empty<WindowDetails>();
        public bool IsSelectionVisible { get; private set; }
        public bool IsSelectionAnimating => false;
        public NativeRect Selection { get; private set; }
        public NativeSize LabelSize { get; private set; }
        public NativeRect? Accepted { get; private set; }

        public WindowDetails FindWindowUnderCursor(bool includeChildren) => null;
        public System.Windows.Media.Color GetPixelColor(NativePoint location) => System.Windows.Media.Colors.Transparent;

        public void ShowSelection(NativeRect rect, bool animate = false, Action completed = null)
        {
            Selection = rect;
            IsSelectionVisible = true;
            completed?.Invoke();
        }

        public void HideSelection() => IsSelectionVisible = false;
        public void ShowLabels(NativeRect rect, NativeSize size, bool fadeIn = false, string debugText = null) => LabelSize = size;
        public void ClearLabels() => LabelSize = NativeSize.Empty;
        public NativeRect GetMonitorBounds() => ScreenBounds;
        public void ShowPanel(object owner, System.Windows.Size contentSize, Action<System.Windows.Media.DrawingContext> drawContent) { }
        public void HidePanel(object owner) { }
        public CaptureToolStyle ToolStyle => null;
        public void Redraw() { }
        public void Redraw(ICaptureOverlay overlay) { }
        public object FindResource(object resourceKey) => null;
        public void Accept(NativeRect rect, WindowDetails window = null) => Accepted = rect;
        public void Cancel() { }
    }

    private static (RegionCaptureTool tool, FakeHost host) Create()
    {
        var host = new FakeHost();
        var tool = new RegionCaptureTool();
        host.ActiveTool = tool;
        tool.Activate(host);
        return (tool, host);
    }

    private static void MoveTo(ICaptureTool tool, FakeHost host, int x, int y)
    {
        host.CursorPosition = new NativePoint(x, y);
        tool.OnMouseMove();
    }

    [Fact]
    public void Drag_AcceptsTheRegionIncludingThePixelUnderTheCursor()
    {
        var (tool, host) = Create();
        MoveTo(tool, host, 100, 50);
        tool.OnMouseDown();
        MoveTo(tool, host, 20, 10);

        Assert.True(host.IsSelectionVisible);
        Assert.Equal(new NativeRect(20, 10, 80, 40), host.Selection);
        Assert.Equal(new NativeSize(81, 41), host.LabelSize);

        tool.OnMouseUp();
        Assert.Equal(new NativeRect(20, 10, 81, 41), host.Accepted);
    }

    [Fact]
    public void Click_DoesNotAccept_AndHidesTheSelection()
    {
        var (tool, host) = Create();
        MoveTo(tool, host, 100, 100);
        tool.OnMouseDown();
        MoveTo(tool, host, 102, 102);
        tool.OnMouseUp();

        Assert.Null(host.Accepted);
        Assert.False(host.IsSelectionVisible);
        Assert.True(tool.ShowsCrosshair);
    }

    [Fact]
    public void Enter_StartsAndEndsTheSelection()
    {
        var (tool, host) = Create();
        MoveTo(tool, host, 10, 10);
        Assert.True(tool.OnKeyDown(Key.Return));
        Assert.False(tool.ShowsCrosshair);
        MoveTo(tool, host, 110, 60);
        Assert.True(tool.OnKeyDown(Key.Return));

        Assert.Equal(new NativeRect(10, 10, 101, 51), host.Accepted);
    }

    [Fact]
    public void OtherKeys_AreLeftToTheWindow()
    {
        var (tool, _) = Create();
        Assert.False(tool.OnKeyDown(Key.Space));
        Assert.False(tool.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void Deactivate_WhileSelecting_RemovesTheSelection()
    {
        var (tool, host) = Create();
        tool.OnMouseDown();
        MoveTo(tool, host, 300, 300);
        tool.Deactivate();

        Assert.False(host.IsSelectionVisible);
        Assert.Equal(NativeSize.Empty, host.LabelSize);
        Assert.Equal(CaptureMode.Region, tool.Mode);
    }
}
