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

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Core.Languages;

namespace Greenshot.Linux.Destinations;

/// <summary>
/// The "Picker" destination: after the capture the user chooses where it goes.
/// The keys 1-9 choose a destination, Esc closes without exporting.
/// </summary>
internal sealed class PickerWindow : Window
{
    private readonly TaskCompletionSource<IDestination?> _result = new();
    private readonly IReadOnlyList<IDestination> _destinations;

    private PickerWindow(Capture capture, IReadOnlyList<IDestination> destinations)
    {
        _destinations = destinations;
        Title = "Greenshot";
        Icon = AppIcons.Window;
        CanResize = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;
        ShowInTaskbar = false;

        var thumbnail = BitmapConverter.ToAvalonia(capture.Image);
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 8, MinWidth = 280 };
        panel.Children.Add(new Image
        {
            Source = thumbnail,
            MaxWidth = 360,
            MaxHeight = 220,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        panel.Children.Add(new TextBlock
        {
            Text = string.Create(CultureInfo.CurrentCulture, $"{capture.Image.Width} × {capture.Image.Height}"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Opacity = 0.7,
        });

        for (int i = 0; i < destinations.Count; i++)
        {
            var destination = destinations[i];
            var button = new Button
            {
                Content = i < 9 ? $"_{i + 1}  {destination.DisplayName}" : destination.DisplayName,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                IsEnabled = destination.IsAvailable,
            };
            button.Click += (_, _) => Choose(destination);
            panel.Children.Add(button);
        }

        var close = new Button { Content = Texts.Editor("close"), HorizontalAlignment = HorizontalAlignment.Right, IsCancel = true };
        close.Click += (_, _) => Close();
        panel.Children.Add(close);
        Content = panel;

        Closed += (_, _) =>
        {
            thumbnail.Dispose();
            _result.TrySetResult(null);
        };
    }

    /// <summary>
    /// Show the picker and wait for a choice, null when it was closed
    /// </summary>
    public static Task<IDestination?> PickAsync(Capture capture, IReadOnlyList<IDestination> destinations)
    {
        var window = new PickerWindow(capture, destinations);
        window.Show();
        window.Activate();
        return window._result.Task;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int index = e.Key switch
        {
            >= Key.D1 and <= Key.D9 => e.Key - Key.D1,
            >= Key.NumPad1 and <= Key.NumPad9 => e.Key - Key.NumPad1,
            _ => -1,
        };
        if (index >= 0 && index < _destinations.Count && _destinations[index].IsAvailable)
        {
            Choose(_destinations[index]);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void Choose(IDestination destination)
    {
        _result.TrySetResult(destination);
        Close();
    }
}
