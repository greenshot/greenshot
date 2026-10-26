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
using Greenshot.Linux.Helpers;

namespace Greenshot.Linux.Editing;

public enum SaveChoice
{
    Save,
    Discard,
    Cancel,
}

/// <summary>
/// The small questions of the editor
/// </summary>
internal static class Dialogs
{
    /// <summary>
    /// "Do you want to save the changes?" when an edited image is closed
    /// </summary>
    public static async Task<SaveChoice> AskSaveChangesAsync(Window owner)
    {
        var choice = SaveChoice.Cancel;
        var dialog = CreateDialog("Greenshot", "Do you want to save the changes to the image?");
        var buttons = (StackPanel)((StackPanel)dialog.Content!).Children[^1];
        AddButton(buttons, "_Save", () => choice = SaveChoice.Save, dialog, isDefault: true);
        AddButton(buttons, "_Don't save", () => choice = SaveChoice.Discard, dialog);
        AddButton(buttons, "Cancel", () => choice = SaveChoice.Cancel, dialog, isCancel: true);
        await dialog.ShowDialog(owner);
        return choice;
    }

    /// <summary>
    /// Ask for a whole number, null when cancelled
    /// </summary>
    public static async Task<int?> AskNumberAsync(Window owner, string title, string question, int value, int minimum, int maximum)
    {
        int? result = null;
        var dialog = CreateDialog(title, question);
        var numeric = new NumericUpDown { Value = value, Minimum = minimum, Maximum = maximum, FormatString = "0", Width = 160 };
        var content = (StackPanel)dialog.Content!;
        content.Children.Insert(1, numeric);
        var buttons = (StackPanel)content.Children[^1];
        AddButton(buttons, "OK", () => result = (int?)numeric.Value, dialog, isDefault: true);
        AddButton(buttons, "Cancel", () => result = null, dialog, isCancel: true);
        dialog.Opened += (_, _) => numeric.Focus();
        await dialog.ShowDialog(owner);
        return result;
    }

    /// <summary>
    /// The resize dialog: a new width and height, optionally keeping the proportions; null when cancelled
    /// </summary>
    public static async Task<(int Width, int Height)?> AskSizeAsync(Window owner, int width, int height)
    {
        (int, int)? result = null;
        var dialog = CreateDialog("Resize", "The new size of the image in pixels");
        var widthBox = new NumericUpDown { Value = width, Minimum = 1, Maximum = 30000, FormatString = "0", Width = 140 };
        var heightBox = new NumericUpDown { Value = height, Minimum = 1, Maximum = 30000, FormatString = "0", Width = 140 };
        var percent = new NumericUpDown { Value = 100, Minimum = 1, Maximum = 1000, FormatString = "0", Width = 140 };
        var keepAspect = new CheckBox { Content = "Keep the proportions", IsChecked = true };
        bool updating = false;
        double ratio = (double)width / height;

        void Update(Action action)
        {
            if (updating)
            {
                return;
            }

            updating = true;
            action();
            updating = false;
        }

        widthBox.ValueChanged += (_, _) => Update(() =>
        {
            if (keepAspect.IsChecked == true && widthBox.Value is { } w)
            {
                heightBox.Value = Math.Max(1, Math.Round(w / (decimal)ratio));
            }

            percent.Value = widthBox.Value is { } v ? Math.Round(v * 100 / width) : percent.Value;
        });
        heightBox.ValueChanged += (_, _) => Update(() =>
        {
            if (keepAspect.IsChecked == true && heightBox.Value is { } h)
            {
                widthBox.Value = Math.Max(1, Math.Round(h * (decimal)ratio));
            }
        });
        percent.ValueChanged += (_, _) => Update(() =>
        {
            if (percent.Value is { } p)
            {
                widthBox.Value = Math.Max(1, Math.Round(width * p / 100));
                heightBox.Value = Math.Max(1, Math.Round(height * p / 100));
            }
        });

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,8,Auto"), RowDefinitions = new RowDefinitions("Auto,6,Auto,6,Auto,6,Auto") };
        AddRow(grid, 0, "Percent", percent);
        AddRow(grid, 2, "Width", widthBox);
        AddRow(grid, 4, "Height", heightBox);
        Grid.SetRow(keepAspect, 6);
        Grid.SetColumn(keepAspect, 2);
        grid.Children.Add(keepAspect);

        var content = (StackPanel)dialog.Content!;
        content.Children.Insert(1, grid);
        var buttons = (StackPanel)content.Children[^1];
        AddButton(buttons, "OK", () =>
        {
            if (widthBox.Value is { } w && heightBox.Value is { } h)
            {
                result = ((int)w, (int)h);
            }
        }, dialog, isDefault: true);
        AddButton(buttons, "Cancel", () => result = null, dialog, isCancel: true);
        await dialog.ShowDialog(owner);
        return result;
    }

    private static void AddRow(Grid grid, int row, string label, Control control)
    {
        var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(text, row);
        Grid.SetRow(control, row);
        Grid.SetColumn(control, 2);
        grid.Children.Add(text);
        grid.Children.Add(control);
    }

    private static Window CreateDialog(string title, string message)
    {
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var content = new StackPanel { Margin = new Thickness(20), Spacing = 16 };
        content.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 380 });
        content.Children.Add(buttons);
        return new Window
        {
            Title = title,
            Icon = AppIcons.Window,
            Content = content,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
    }

    private static void AddButton(StackPanel buttons, string text, Action onClick, Window dialog, bool isDefault = false, bool isCancel = false)
    {
        var button = new Button { Content = text, IsDefault = isDefault, IsCancel = isCancel, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
        button.Click += (_, _) =>
        {
            onClick();
            dialog.Close();
        };
        buttons.Children.Add(button);
    }
}
