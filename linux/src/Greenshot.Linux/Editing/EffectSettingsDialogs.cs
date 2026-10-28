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

using Avalonia.Controls;
using Avalonia.Layout;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Helpers;
using SkiaSharp;

namespace Greenshot.Linux.Editing;

/// <summary>
/// The settings windows of the drop shadow and the torn edges (DropShadowSettingsWindow and TornEdgeSettingsWindow of Windows).
/// The settings are only changed with OK.
/// </summary>
internal static class EffectSettingsDialogs
{
    /// <summary>
    /// Darkness is shown as 1 to 40, like the slider of Windows
    /// </summary>
    private const int DarknessSteps = 40;

    public static async Task<bool> EditDropShadowAsync(Window owner, DropShadowSettings settings)
    {
        var shadow = new ShadowControls(settings);
        return await ShowAsync(owner, Texts.Editor("dropshadow_settings"), [.. shadow.Rows], () => shadow.Store(settings));
    }

    /// <param name="showEdges">false for the cut edges of a crop out, which only have the edges along the cut</param>
    public static async Task<bool> EditTornEdgeAsync(Window owner, TornEdgeSettings settings, bool showEdges, Func<IReadOnlyList<SKColor>> recentColors)
    {
        var rows = new List<Control>();
        CheckBox[] edges = [];
        if (showEdges)
        {
            string[] keys = ["tornedge_top", "tornedge_right", "tornedge_bottom", "tornedge_left"];
            edges = keys.Select((key, i) => Ui.Check(Texts.Editor(key), settings.Edges[i])).ToArray();
            var all = Ui.Check(Texts.Editor("tornedge_all"), settings.Edges.All(e => e));
            bool updating = false;
            all.IsCheckedChanged += (_, _) =>
            {
                if (updating) return;
                updating = true;
                foreach (var edge in edges)
                {
                    edge.IsChecked = all.IsChecked == true;
                }

                updating = false;
            };
            foreach (var edge in edges)
            {
                edge.IsCheckedChanged += (_, _) =>
                {
                    if (updating) return;
                    updating = true;
                    all.IsChecked = edges.All(e => e.IsChecked == true);
                    updating = false;
                };
            }

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto") };
            Place(grid, edges[0], 0, 1);
            Place(grid, edges[3], 1, 0);
            Place(grid, all, 1, 1);
            Place(grid, edges[1], 1, 2);
            Place(grid, edges[2], 2, 1);
            rows.Add(Ui.Section(Texts.Editor("tornedge_settings"), grid));
        }

        var toothSize = Number(settings.ToothHeight, 1, 40);
        var horizontalRange = Number(settings.HorizontalToothRange, 2, 40);
        var verticalRange = Number(settings.VerticalToothRange, 2, 40);
        var background = new ColorButton(Texts.Editor("tornedge_background_color"), settings.BackgroundColor, recentColors);
        var backgroundColor = settings.BackgroundColor;
        background.ColorChosen += color => backgroundColor = color;
        rows.Add(Ui.Section(Texts.Editor("tornedge_toothsize"),
            Ui.Row(Texts.Editor("tornedge_toothsize"), toothSize),
            Ui.Row(Texts.Editor("tornedge_horizontaltoothrange"), horizontalRange),
            Ui.Row(Texts.Editor("tornedge_verticaltoothrange"), verticalRange),
            Ui.Row(Texts.Editor("tornedge_background_color"), background)));

        var generateShadow = Ui.Check(Texts.Editor("tornedge_shadow"), settings.GenerateShadow);
        var shadow = new ShadowControls(settings);
        var shadowRows = new StackPanel { Spacing = 8, IsEnabled = settings.GenerateShadow };
        shadowRows.Children.AddRange(shadow.Rows);
        generateShadow.IsCheckedChanged += (_, _) => shadowRows.IsEnabled = generateShadow.IsChecked == true;
        rows.Add(Ui.Section(Texts.Editor("tornedge_shadow"), generateShadow, shadowRows));

        return await ShowAsync(owner, Texts.Editor("tornedge_settings"), rows, () =>
        {
            shadow.Store(settings);
            settings.ToothHeight = (int)(toothSize.Value ?? settings.ToothHeight);
            settings.HorizontalToothRange = (int)(horizontalRange.Value ?? settings.HorizontalToothRange);
            settings.VerticalToothRange = (int)(verticalRange.Value ?? settings.VerticalToothRange);
            settings.GenerateShadow = generateShadow.IsChecked == true;
            settings.BackgroundColor = backgroundColor;
            if (showEdges)
            {
                settings.Edges = edges.Select(e => e.IsChecked == true).ToArray();
            }
        });
    }

    /// <summary>
    /// Thickness, offset and darkness of a shadow
    /// </summary>
    private sealed class ShadowControls
    {
        private readonly NumericUpDown _size;
        private readonly NumericUpDown _offsetX;
        private readonly NumericUpDown _offsetY;
        private readonly NumericUpDown _darkness;

        public ShadowControls(DropShadowSettings settings)
        {
            _size = Number(settings.ShadowSize, 1, 20);
            _offsetX = Number(settings.ShadowOffset.X, -20, 20);
            _offsetY = Number(settings.ShadowOffset.Y, -20, 20);
            _darkness = Number((int)Math.Round(settings.Darkness * DarknessSteps), 1, DarknessSteps);
            Rows =
            [
                Ui.Row(Texts.Editor("dropshadow_thickness"), _size),
                Ui.Row(Texts.Editor("dropshadow_offset"), Ui.Horizontal(new TextBlock { Text = "X", VerticalAlignment = VerticalAlignment.Center }, _offsetX,
                    new TextBlock { Text = "Y", VerticalAlignment = VerticalAlignment.Center }, _offsetY)),
                Ui.Row(Texts.Editor("dropshadow_darkness"), _darkness),
            ];
        }

        public IReadOnlyList<Control> Rows { get; }

        public void Store(DropShadowSettings settings)
        {
            settings.ShadowSize = (int)(_size.Value ?? settings.ShadowSize);
            settings.ShadowOffset = new SKPointI((int)(_offsetX.Value ?? 0), (int)(_offsetY.Value ?? 0));
            settings.Darkness = (float)(_darkness.Value ?? DarknessSteps) / DarknessSteps;
        }
    }

    private static NumericUpDown Number(int value, int minimum, int maximum) => new()
    {
        Value = Math.Clamp(value, minimum, maximum),
        Minimum = minimum,
        Maximum = maximum,
        Increment = 1,
        FormatString = "0",
        Width = 120,
    };

    private static void Place(Grid grid, Control control, int row, int column)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        control.Margin = new Avalonia.Thickness(4);
        grid.Children.Add(control);
    }

    private static async Task<bool> ShowAsync(Window owner, string title, IReadOnlyList<Control> rows, Action store)
    {
        var dialog = new Window
        {
            Title = Texts.Label(title),
            Icon = AppIcons.Window,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        bool confirmed = false;
        var ok = new Button { Content = Texts.Linux("ok"), IsDefault = true, MinWidth = 90 };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, MinWidth = 90 };
        ok.Click += (_, _) =>
        {
            store();
            confirmed = true;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();
        var buttons = Ui.Horizontal(ok, cancel);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        dialog.Content = Ui.Page([.. rows, buttons]);
        await dialog.ShowDialog(owner);
        return confirmed;
    }
}
