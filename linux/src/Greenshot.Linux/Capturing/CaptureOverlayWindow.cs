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
using Greenshot.Linux.Core.Barcodes;
using Greenshot.Linux.Core.Languages;
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
        _model.HotspotClicked += OnHotspotClicked;
        _surface.PointerMoved += (_, e) =>
        {
            // Alt places the fixed size area without snapping
            _model.SnapDisabled = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
            _model.MoveTo(_surface.ToImage(e.GetPosition(_surface)));
        };
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
        _model.HotspotClicked -= OnHotspotClicked;
        base.OnClosed(e);
    }

    private DetectedBarcode? _shownHotspot;

    private void OnModelChanged()
    {
        _surface.InvalidateVisual();
        // A hand and the text over a frame of a QR code
        var hotspot = _model.HoveredHotspot;
        if (hotspot != _shownHotspot)
        {
            _shownHotspot = hotspot;
            _surface.Cursor = new Cursor(hotspot != null ? StandardCursorType.Hand : StandardCursorType.Cross);
            ToolTip.SetTip(_surface, hotspot?.Text);
            ToolTip.SetIsOpen(_surface, hotspot != null);
        }
    }

    /// <summary>
    /// The menu of a QR code: its text, copy it, open it when it is a link (the hotspot menu of Windows)
    /// </summary>
    private void OnHotspotClicked(DetectedBarcode hotspot)
    {
        if (!_surface.IsPointerOver)
        {
            // Every monitor's window hears it, the one under the pointer shows the menu
            return;
        }

        string preview = hotspot.Text.Length > 30 ? hotspot.Text[..27] + "..." : hotspot.Text;
        var items = new List<Control>
        {
            new MenuItem { Header = Texts.Core(hotspot.IsQrCode ? "notification_qrcode" : "notification_barcode", preview), IsEnabled = false },
            new Separator(),
        };
        var copy = new MenuItem { Header = Texts.Linux("qr_copy") };
        copy.Click += (_, _) => _ = Greenshot.Linux.Core.Output.SystemClipboard.SetTextAsync(hotspot.Text);
        items.Add(copy);
        if (hotspot.IsUrl)
        {
            var open = new MenuItem { Header = Texts.Linux("qr_open_link") };
            open.Click += (_, _) => ProcessRunner.StartDetached("xdg-open", hotspot.Text);
            items.Add(open);
        }

        var menu = new ContextMenu { ItemsSource = items, Placement = Avalonia.Controls.PlacementMode.Pointer };
        menu.Open(_surface);
    }

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

    /// <summary>
    /// The keys of the fixed size capture: 1-9 pick a size, PageUp / PageDown step through them, C moves the cursor's corner
    /// </summary>
    private bool HandleFixedSizeKey(Key key)
    {
        switch (key)
        {
            case >= Key.D1 and <= Key.D9:
                _model.PickFixedSize(key - Key.D1);
                return true;
            case >= Key.NumPad1 and <= Key.NumPad9:
                _model.PickFixedSize(key - Key.NumPad1);
                return true;
            case Key.PageUp:
                _model.StepFixedSize(-1);
                return true;
            case Key.PageDown:
                _model.StepFixedSize(1);
                return true;
            case Key.C:
                _model.NextFixedAnchor();
                return true;
            default:
                return false;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key is Key.LeftAlt or Key.RightAlt && _model.SnapDisabled)
        {
            _model.SnapDisabled = false;
            _model.MoveBy(0, 0);
            OnModelChanged();
        }

        base.OnKeyUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int step = e.KeyModifiers.HasFlag(KeyModifiers.Control) ? 10 : 1;
        e.Handled = true;
        if (_model.IsFixedSize && HandleFixedSizeKey(e.Key))
        {
            return;
        }

        switch (e.Key)
        {
            case Key.F:
                _model.ToggleFixedSize();
                break;
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
