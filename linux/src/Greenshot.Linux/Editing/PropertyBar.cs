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
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using SkiaSharp;

namespace Greenshot.Linux.Editing;

/// <summary>
/// The properties of the selected elements or of the current tool, the second tool bar of the Windows editor.
/// It is rebuilt when the fields change and refreshed when only the values change.
/// </summary>
internal sealed class PropertyBar : StackPanel
{
    private static IReadOnlyList<string>? _fontFamilies;

    private readonly EditorController _controller;
    private readonly EditorSettings? _settings;
    private readonly Dictionary<FieldType, Action<object>> _refreshers = new();
    private IReadOnlyList<FieldType> _shownFields = [];
    private bool _shownCutButtons;
    private bool _refreshing;

    public PropertyBar(EditorController controller, EditorSettings? settings)
    {
        _controller = controller;
        _settings = settings;
        Orientation = Orientation.Horizontal;
        Spacing = 6;
        Margin = new Thickness(6, 3);
        MinHeight = 34;
        _controller.StateChanged += Update;
        Update();
    }

    private static IReadOnlyList<string> FontFamilies => _fontFamilies ??=
        SKFontManager.Default.FontFamilies.Distinct().OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase).ToList();

    /// <summary>
    /// The settings window of the selected cut edges or torn edges should open
    /// </summary>
    public event Action<CutElement>? CutSettingsRequested;

    /// <summary>
    /// A value was picked from a drop-down: the keys go back to the image, so Enter confirms a crop again
    /// </summary>
    public event Action? FocusReturned;

    public void Update()
    {
        var fields = _controller.VisibleFields;
        bool cutButtons = _controller.SelectedCuts.Count > 0;
        if (!fields.SequenceEqual(_shownFields) || cutButtons != _shownCutButtons)
        {
            Rebuild(fields, cutButtons);
        }

        _refreshing = true;
        try
        {
            foreach (var (field, refresh) in _refreshers)
            {
                refresh(_controller.GetFieldValue(field));
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void Rebuild(IReadOnlyList<FieldType> fields, bool cutButtons)
    {
        _shownFields = fields;
        _shownCutButtons = cutButtons;
        Children.Clear();
        _refreshers.Clear();
        foreach (var field in fields)
        {
            var control = CreateControl(field);
            if (control != null)
            {
                Children.Add(control);
            }
        }

        if (cutButtons)
        {
            // The context menu of the cut edges and torn edges of Windows
            var reseed = new Button { Content = Texts.Label(Texts.Editor("cut_mark_reseed")) };
            reseed.Click += (_, _) => _controller.NewRandomEdges();
            var settings = new Button { Content = Texts.Label(Texts.Editor("cut_mark_settings")) };
            settings.Click += (_, _) =>
            {
                if (_controller.SelectedCuts.FirstOrDefault() is { } cut)
                {
                    CutSettingsRequested?.Invoke(cut);
                }
            };
            var apply = new Button { Content = Texts.Label(Texts.Editor("apply_to_image")) };
            apply.Click += (_, _) => _controller.ApplyCutsToImage();
            // First, the fields don't always fit
            Children.InsertRange(0, [reseed, settings, apply, new Separator { Width = 1, Margin = new Thickness(4, 2) }]);
        }
    }

    private void Set(FieldType field, object value)
    {
        if (!_refreshing)
        {
            _controller.SetFieldValue(field, value);
        }
    }

    private Control? CreateControl(FieldType field)
    {
        var value = _controller.GetFieldValue(field);
        switch (value)
        {
            case SKColor color:
                var colorButton = new ColorButton(FieldText(field), color, () => _settings?.RecentColors ?? []);
                colorButton.ColorChosen += chosen => Set(field, chosen);
                _refreshers[field] = v => colorButton.Color = (SKColor)v;
                return Labeled(FieldText(field), colorButton);

            case bool flag:
                var toggle = new ToggleButton { Content = FieldText(field), IsChecked = flag, Padding = new Thickness(8, 4) };
                if (field == FieldType.FontBold)
                {
                    toggle.Content = "B";
                    toggle.FontWeight = Avalonia.Media.FontWeight.Bold;
                }
                else if (field == FieldType.FontItalic)
                {
                    toggle.Content = "I";
                    toggle.FontStyle = Avalonia.Media.FontStyle.Italic;
                }

                ToolTip.SetTip(toggle, FieldText(field));
                toggle.IsCheckedChanged += (_, _) => Set(field, toggle.IsChecked == true);
                _refreshers[field] = v => toggle.IsChecked = (bool)v;
                return toggle;

            case int number:
                var (minimum, maximum) = field switch
                {
                    FieldType.LineThickness => (0, 100),
                    FieldType.PixelSize => (2, 100),
                    FieldType.MagnificationFactor => (2, 8),
                    FieldType.ToothRange => (2, 100),
                    _ => (1, 100),
                };
                var numeric = new NumericUpDown
                {
                    Value = number,
                    Minimum = minimum,
                    Maximum = maximum,
                    Increment = 1,
                    FormatString = "0",
                    Width = 110,
                };
                numeric.ValueChanged += (_, e) =>
                {
                    if (e.NewValue is { } newValue)
                    {
                        Set(field, (int)newValue);
                    }
                };
                _refreshers[field] = v => numeric.Value = (int)v;
                return Labeled(FieldText(field), numeric);

            case float size:
                var fontSize = new NumericUpDown { Value = (decimal)size, Minimum = 4, Maximum = 500, Increment = 1, FormatString = "0", Width = 110 };
                fontSize.ValueChanged += (_, e) =>
                {
                    if (e.NewValue is { } newValue)
                    {
                        Set(field, (float)newValue);
                    }
                };
                _refreshers[field] = v => fontSize.Value = (decimal)(float)v;
                return Labeled(FieldText(field), fontSize);

            case string family:
                var fonts = new ComboBox { ItemsSource = FontFamilies, SelectedItem = family, Width = 180, MaxDropDownHeight = 400 };
                fonts.SelectionChanged += (_, _) =>
                {
                    if (fonts.SelectedItem is string selected)
                    {
                        Set(field, selected);
                    }
                };
                fonts.DropDownClosed += (_, _) => FocusReturned?.Invoke();
                _refreshers[field] = v => fonts.SelectedItem = FontFamilies.Contains((string)v) ? v : null;
                return Labeled(FieldText(field), fonts);

            case Enum:
                var values = Enum.GetValues(value.GetType()).Cast<object>().ToList();
                var combo = new ComboBox
                {
                    ItemsSource = values.Select(v => EnumText(v)).ToList(),
                    SelectedIndex = values.IndexOf(value),
                    MinWidth = 120,
                };
                combo.SelectionChanged += (_, _) =>
                {
                    if (combo.SelectedIndex >= 0)
                    {
                        Set(field, values[combo.SelectedIndex]);
                    }
                };
                combo.DropDownClosed += (_, _) => FocusReturned?.Invoke();
                _refreshers[field] = v => combo.SelectedIndex = values.IndexOf(v);
                return Labeled(FieldText(field), combo);

            default:
                return null;
        }
    }

    private static Control Labeled(string label, Control control)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(control);
        return panel;
    }

    /// <summary>
    /// The texts of the Windows editor for the fields, without their key hints
    /// </summary>
    private static string FieldText(FieldType field) => Texts.Label(field switch
    {
        FieldType.LineThickness => Texts.Editor("thickness"),
        FieldType.LineColor => Texts.Editor("forecolor"),
        FieldType.FillColor => Texts.Editor("backcolor"),
        FieldType.Shadow => Texts.Editor("shadow"),
        FieldType.ArrowHeads => Texts.Editor("arrowheads"),
        FieldType.FontFamily => Texts.Editor("font_family"),
        FieldType.FontSize => Texts.Editor("fontsize"),
        FieldType.FontBold => Texts.Editor("bold"),
        FieldType.FontItalic => Texts.Editor("italic"),
        FieldType.TextHorizontalAlignment => Texts.Editor("align_horizontal"),
        FieldType.TextVerticalAlignment => Texts.Editor("align_vertical"),
        FieldType.HighlightFilter => Texts.Editor("highlight_mode"),
        FieldType.ObfuscateFilter => Texts.Editor("obfuscate_mode"),
        FieldType.BlurRadius => Texts.Editor("blur_radius"),
        FieldType.PixelSize => Texts.Editor("pixel_size"),
        FieldType.MagnificationFactor => Texts.Editor("magnification_factor"),
        FieldType.CutMarkStyle => Texts.Editor("cut_mark"),
        FieldType.ToothHeight => Texts.Editor("tornedge_toothsize"),
        FieldType.ToothRange => Texts.Editor("cut_mark_tooth_range"),
        FieldType.CropMode => Texts.Editor("crop_mode"),
        _ => field.DisplayName(),
    });

    private static string EnumText(object value) => Texts.Label(value switch
    {
        ArrowHeads.None => Texts.Editor("arrowheads_none"),
        ArrowHeads.Start => Texts.Editor("arrowheads_start"),
        ArrowHeads.End => Texts.Editor("arrowheads_end"),
        ArrowHeads.Both => Texts.Editor("arrowheads_both"),
        HighlightFilter.TextHighlight => Texts.Editor("highlight_text"),
        HighlightFilter.AreaHighlight => Texts.Editor("highlight_area"),
        HighlightFilter.Grayscale => Texts.Editor("highlight_grayscale"),
        HighlightFilter.Magnification => Texts.Editor("highlight_magnify"),
        ObfuscateFilter.Pixelize => Texts.Editor("obfuscate_pixelize"),
        ObfuscateFilter.Blur => Texts.Editor("obfuscate_blur"),
        TextHorizontalAlignment.Left => Texts.Editor("align_left"),
        TextHorizontalAlignment.Center => Texts.Editor("align_center"),
        TextHorizontalAlignment.Right => Texts.Editor("align_right"),
        TextVerticalAlignment.Top => Texts.Editor("align_top"),
        TextVerticalAlignment.Center => Texts.Editor("align_middle"),
        TextVerticalAlignment.Bottom => Texts.Editor("align_bottom"),
        CutMarkStyle.None => Texts.Editor("cut_mark_none"),
        CutMarkStyle.Line => Texts.Editor("cut_mark_line"),
        CutMarkStyle.ZigZag => Texts.Editor("cut_mark_zigzag"),
        CutMarkStyle.Wave => Texts.Editor("cut_mark_wave"),
        CutMarkStyle.Torn => Texts.Editor("cut_mark_torn"),
        CropMode.Default => Texts.Editor("cropmode_default"),
        CropMode.Vertical => Texts.Editor("cropmode_vertical"),
        CropMode.Horizontal => Texts.Editor("cropmode_horizontal"),
        _ => value.ToString() ?? string.Empty,
    });
}
