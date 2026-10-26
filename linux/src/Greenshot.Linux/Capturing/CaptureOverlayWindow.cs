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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Capturing.Interactive;
using SkiaSharp;

namespace Greenshot.Linux.Capturing;

/// <summary>
/// The capture screen on one monitor. Wayland can't place one window over all monitors,
/// so there is one fullscreen window per monitor; they share the <see cref="SelectionModel"/>.
/// </summary>
internal sealed class CaptureOverlayWindow : Window
{
    private readonly SelectionModel _model;
    private readonly CaptureSurface _surface;
    private readonly ScreenInfo _screen;
    private readonly bool _moveWithSway;

    /// <param name="moveWithSway">
    /// Sway opens every X11 window on the focused output and ignores the position; with more monitors swaymsg moves it
    /// </param>
    public CaptureOverlayWindow(Bitmap image, SKRectI screenPixels, SelectionModel model, ScreenInfo screen, PixelRect platformBounds,
        bool moveWithSway)
    {
        _model = model;
        _screen = screen;
        _moveWithSway = moveWithSway;
        _surface = new CaptureSurface(image, screenPixels, model);

        Title = OverlayTitle(screen);
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        Background = Brushes.Black;
        Cursor = new Cursor(StandardCursorType.Cross);
        Content = _surface;
        Position = platformBounds.Position;
        Width = platformBounds.Width;
        Height = platformBounds.Height;
        WindowState = WindowState.FullScreen;

        _model.Changed += OnModelChanged;
        _surface.PointerMoved += (_, e) => _model.MoveTo(_surface.ToImage(e.GetPosition(_surface)));
        _surface.PointerEntered += (_, e) => _model.MoveTo(_surface.ToImage(e.GetPosition(_surface)));
        _surface.PointerPressed += OnPointerPressed;
        _surface.PointerReleased += OnPointerReleased;
    }

    private static string OverlayTitle(ScreenInfo screen) => $"Greenshot capture {screen.Name}";

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Activate();
        _surface.Focus();
        if (_moveWithSway)
        {
            var criteria = $"[title=\"^{OverlayTitle(_screen)}$\"]";
            _ = ProcessRunner.RunAsync("swaymsg", [criteria, "move", "container", "to", "output", _screen.Name, ",", "fullscreen", "enable"]);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _model.Changed -= OnModelChanged;
        base.OnClosed(e);
    }

    private void OnModelChanged() => _surface.InvalidateVisual();

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_surface);
        _model.MoveTo(_surface.ToImage(point.Position));
        if (point.Properties.IsLeftButtonPressed)
        {
            _model.Press();
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            _model.Cancel();
        }

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        _model.MoveTo(_surface.ToImage(e.GetPosition(_surface)));
        _model.Release();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int step = e.KeyModifiers.HasFlag(KeyModifiers.Control) ? 10 : 1;
        e.Handled = true;
        switch (e.Key)
        {
            case Key.Escape:
                _model.Cancel();
                break;
            case Key.Enter:
                _model.Confirm();
                break;
            case Key.Space:
                _model.ToggleMode();
                break;
            case Key.Tab:
                _model.SelectNextWindow(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                break;
            case Key.Left:
                _model.MoveBy(-step, 0);
                break;
            case Key.Right:
                _model.MoveBy(step, 0);
                break;
            case Key.Up:
                _model.MoveBy(0, -step);
                break;
            case Key.Down:
                _model.MoveBy(0, step);
                break;
            case Key.Z:
                _model.ToggleZoomer();
                break;
            case Key.T:
                _model.ToggleTextMode();
                break;
            case Key.F1:
                _model.ToggleHelp();
                break;
            case Key.I:
                _model.ToggleInfo();
                break;
            default:
                e.Handled = false;
                base.OnKeyDown(e);
                break;
        }
    }
}
