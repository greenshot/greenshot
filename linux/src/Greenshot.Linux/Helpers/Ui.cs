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

namespace Greenshot.Linux.Helpers;

/// <summary>
/// Small builders for the windows which are made in code: sections, labeled rows, notes
/// </summary>
internal static class Ui
{
    public static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(16), Spacing = 14 };
        page.Children.AddRange(children);
        return page;
    }

    /// <summary>
    /// A titled group, the GroupBox of the Windows settings
    /// </summary>
    public static Control Section(string title, params Control[] children)
    {
        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, FontSize = 15 });
        content.Children.AddRange(children);
        return content;
    }

    public static CheckBox Check(string text, bool value, string? tooltip = null)
    {
        var check = new CheckBox { Content = new TextBlock { Text = Texts.WithoutAccessKey(text), TextWrapping = TextWrapping.Wrap }, IsChecked = value };
        if (tooltip != null)
        {
            ToolTip.SetTip(check, tooltip);
        }

        return check;
    }

    /// <summary>
    /// A label left of a control
    /// </summary>
    public static Control Row(string label, Control control, string? tooltip = null)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("220,*") };
        var text = new TextBlock { Text = Texts.WithoutAccessKey(label), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        if (tooltip != null)
        {
            ToolTip.SetTip(text, tooltip);
            ToolTip.SetTip(control, tooltip);
        }

        Grid.SetColumn(control, 1);
        grid.Children.Add(text);
        grid.Children.Add(control);
        return grid;
    }

    public static TextBlock Note(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.75,
        FontSize = 12,
    };

    public static StackPanel Horizontal(params Control[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.AddRange(children);
        return panel;
    }
}
