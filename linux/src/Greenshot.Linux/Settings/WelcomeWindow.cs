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
using Avalonia.Layout;
using Avalonia.Media;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Helpers;

namespace Greenshot.Linux.Settings;

/// <summary>
/// One check of the setup page: what it found, and whether it is fine
/// </summary>
internal sealed record SetupCheck(bool IsOk, string Text);

/// <summary>
/// The welcome window at the first start (docs/vNext/welcome-window.md): thanks, how capturing works,
/// whether the desktop gives Greenshot what it needs, and done
/// </summary>
internal sealed class WelcomeWindow : Window
{
    private readonly List<Control> _pages = new();
    private readonly ContentControl _pageHost = new();
    private readonly Button _back;
    private readonly Button _next;
    private int _page;

    public WelcomeWindow(IReadOnlyList<SetupCheck> checks, Action openSettings)
    {
        Title = "Greenshot";
        Icon = AppIcons.Window;
        Width = 620;
        Height = 480;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _pages.Add(Page(Texts.Welcome("title"),
            Text(Texts.Linux("welcome_thanks")),
            Text(Texts.Welcome("intro"))));
        _pages.Add(Page(Texts.Welcome("basics_title"),
            KeyRow("PrintScreen", Texts.Welcome("basics_region")),
            KeyRow("Alt + PrintScreen", Texts.Welcome("basics_window")),
            KeyRow("Ctrl + PrintScreen", Texts.Welcome("basics_fullscreen")),
            KeyRow("Shift + PrintScreen", Texts.Welcome("basics_lastregion")),
            Ui.Note(Texts.Linux("welcome_keys_note")),
            Text(Texts.Linux("welcome_menu")),
            Text(Texts.Welcome("basics_after"))));
        _pages.Add(Page(Texts.Welcome("setup_title"),
            [Ui.Note(Texts.Welcome("setup_sub")), .. checks.Select(CheckRow)]));
        var settings = new Button { Content = Texts.Welcome("button_settings") };
        settings.Click += (_, _) =>
        {
            Close();
            openSettings();
        };
        _pages.Add(Page(Texts.Welcome("done_title"),
            Text(Texts.Welcome("done")),
            Text(Texts.Welcome($"tip{Random.Shared.Next(1, 11)}")).With(t => t.FontStyle = FontStyle.Italic),
            settings));

        _back = new Button { Content = Texts.Welcome("button_back"), MinWidth = 90 };
        _back.Click += (_, _) => Show(_page - 1);
        _next = new Button { Content = Texts.Welcome("button_next"), MinWidth = 90, IsDefault = true };
        _next.Click += (_, _) =>
        {
            if (_page == _pages.Count - 1)
            {
                Close();
            }
            else
            {
                Show(_page + 1);
            }
        };

        var buttons = Ui.Horizontal(_back, _next);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        buttons.Margin = new Thickness(16);
        var root = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(_pageHost);
        Content = root;
        Show(0);
    }

    private void Show(int page)
    {
        _page = Math.Clamp(page, 0, _pages.Count - 1);
        _pageHost.Content = _pages[_page];
        _back.IsVisible = _page > 0;
        _next.Content = _page == _pages.Count - 1 ? Texts.Welcome("button_start") : Texts.Welcome("button_next");
    }

    private static Control Page(string title, params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(24), Spacing = 12 };
        page.Children.Add(new TextBlock { Text = title, FontSize = 22, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        page.Children.AddRange(children);
        return new ScrollViewer { Content = page };
    }

    private static TextBlock Text(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

    private static Control KeyRow(string key, string text)
    {
        var keyBox = new Border
        {
            Child = new TextBlock { Text = key, FontFamily = new FontFamily("monospace") },
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2),
            MinWidth = 170,
        };
        return Ui.Horizontal(keyBox, new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
    }

    private static Control CheckRow(SetupCheck check)
    {
        var mark = new TextBlock
        {
            Text = check.IsOk ? "✔" : "⚠",
            Foreground = check.IsOk ? Brushes.SeaGreen : Brushes.DarkOrange,
            FontSize = 16,
            Width = 24,
        };
        var text = new TextBlock { Text = check.Text, TextWrapping = TextWrapping.Wrap, MaxWidth = 520 };
        return Ui.Horizontal(mark, text);
    }
}
