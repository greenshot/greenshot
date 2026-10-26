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
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Greenshot.Linux.Core.Languages;
using SkiaSharp;

namespace Greenshot.Linux.Editing;

/// <summary>
/// A button showing a color; its flyout offers the standard colors, the recent ones, transparent and a hex value
/// </summary>
internal sealed class ColorButton : Button
{
    private static readonly SKColor[] StandardColors =
    [
        SKColors.Black, new(0x40, 0x40, 0x40), SKColors.Gray, new(0xC0, 0xC0, 0xC0), SKColors.White,
        SKColors.Red, new(0x8B, 0, 0), SKColors.Orange, SKColors.Yellow, new(0xFF, 0xFF, 0x80),
        SKColors.Lime, SKColors.Green, new(0, 0x80, 0x80), SKColors.Cyan, SKColors.Blue,
        new(0, 0, 0x80), SKColors.Magenta, new(0x80, 0, 0x80), SKColors.Brown, SKColors.Pink,
    ];

    private readonly Border _swatch;
    private readonly Func<IReadOnlyList<SKColor>> _recentColors;
    private SKColor _color;

    public ColorButton(string tooltip, SKColor color, Func<IReadOnlyList<SKColor>> recentColors)
    {
        _recentColors = recentColors;
        ToolTip.SetTip(this, tooltip);
        Padding = new Thickness(4);
        _swatch = new Border
        {
            Width = 22,
            Height = 16,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
        };
        Content = _swatch;
        Color = color;
        var flyout = new Flyout { Placement = PlacementMode.Bottom };
        // Built when it opens, so the recent colors are current
        flyout.Opening += (_, _) => flyout.Content = CreatePalette();
        Flyout = flyout;
    }

    /// <summary>
    /// A color was picked
    /// </summary>
    public event Action<SKColor>? ColorChosen;

    public SKColor Color
    {
        get => _color;
        set
        {
            _color = value;
            _swatch.Background = value.Alpha == 0 ? TransparentPattern() : new SolidColorBrush(ToAvalonia(value));
        }
    }

    private Control CreatePalette()
    {
        var panel = new StackPanel { Spacing = 6, Width = 190 };
        panel.Children.Add(Grid(StandardColors));
        var recent = _recentColors();
        if (recent.Count > 0)
        {
            panel.Children.Add(new TextBlock { Text = Texts.Linux("recent_colors"), FontSize = 11, Opacity = 0.7 });
            panel.Children.Add(Grid(recent.Take(10)));
        }

        var transparent = new Button { Content = Texts.Editor("color_transparent"), HorizontalAlignment = HorizontalAlignment.Stretch };
        transparent.Click += (_, _) => Choose(SKColors.Transparent);
        panel.Children.Add(transparent);

        var hexRow = new DockPanel();
        var apply = new Button { Content = Texts.Linux("ok") };
        DockPanel.SetDock(apply, Dock.Right);
        var hex = new TextBox { Text = ToHex(_color), PlaceholderText = "#RRGGBB" };
        apply.Click += (_, _) =>
        {
            if (SKColor.TryParse(hex.Text, out var parsed))
            {
                Choose(parsed);
            }
        };
        hex.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter && SKColor.TryParse(hex.Text, out var parsed))
            {
                Choose(parsed);
            }
        };
        hexRow.Children.Add(apply);
        hexRow.Children.Add(hex);
        panel.Children.Add(hexRow);
        return panel;
    }

    private WrapPanel Grid(IEnumerable<SKColor> colors)
    {
        var grid = new WrapPanel();
        foreach (var color in colors)
        {
            var swatch = new Button
            {
                Width = 30,
                Height = 22,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                Background = new SolidColorBrush(ToAvalonia(color)),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
            };
            ToolTip.SetTip(swatch, ToHex(color));
            swatch.Click += (_, _) => Choose(color);
            grid.Children.Add(swatch);
        }

        return grid;
    }

    private void Choose(SKColor color)
    {
        Color = color;
        Flyout?.Hide();
        ColorChosen?.Invoke(color);
    }

    private static string ToHex(SKColor color) =>
        color.Alpha == 255 ? $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}" : $"#{color.Alpha:X2}{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

    internal static Color ToAvalonia(SKColor color) => Avalonia.Media.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);

    private static IBrush TransparentPattern() => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Colors.White, 0.45),
            new GradientStop(Colors.Red, 0.5),
            new GradientStop(Colors.White, 0.55),
        },
    };
}
