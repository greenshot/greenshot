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

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Greenshot.Base.Wpf;

namespace Greenshot.WpfEditor.Controls
{
    /// <summary>
    /// A button which shows a color; a click opens a palette with the colors of the old editor, the colors used last,
    /// a hex field and the opacity
    /// </summary>
    public sealed class ColorButton : Button
    {
        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
            nameof(SelectedColor), typeof(Color), typeof(ColorButton),
            new FrameworkPropertyMetadata(Colors.Transparent, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((ColorButton)d).ShowColor()));

        // The palette of the old editor, in rows
        private static readonly string[] Palette =
        {
            "#000000", "#404040", "#808080", "#C0C0C0", "#FFFFFF", "#FF0000", "#FF8000", "#FFFF00",
            "#80FF00", "#00FF00", "#00FF80", "#00FFFF", "#0080FF", "#0000FF", "#8000FF", "#FF00FF",
            "#800000", "#804000", "#808000", "#408000", "#008000", "#008080", "#000080", "#800080"
        };

        // The colors chosen last, shared by all color buttons
        private static readonly System.Collections.Generic.List<Color> Recent = new();

        private readonly Border _swatch;
        private Popup _popup;
        private TextBox _hex;
        private ValueSlider _opacity;
        private WrapPanel _recentPanel;
        private bool _updating;

        public ColorButton()
        {
            SetResourceReference(StyleProperty, "SecondaryButtonStyle");
            Padding = new Thickness(4);
            MinWidth = 44;
            _swatch = new Border
            {
                Width = 28,
                Height = 16,
                CornerRadius = new CornerRadius(2),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            _swatch.SetBinding(Border.BorderBrushProperty, new Binding(nameof(ThemeManager.BorderBrush)) { Source = ThemeManager.Instance });
            Content = _swatch;
            Click += (_, _) => OpenPalette();
        }

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        private void ShowColor()
        {
            var color = SelectedColor;
            // Transparent shows as a red slash, like "no color" elsewhere
            _swatch.Background = color.A == 0 ? NoColorBrush : new SolidColorBrush(color);
            AutomationProperties.SetHelpText(this, color.A == 0 ? EditorTexts.Wpf.ColorNone : ToHex(color));
            if (_hex != null && !_updating)
            {
                _updating = true;
                _hex.Text = ToHex(color);
                _opacity.Value = color.A;
                _updating = false;
            }
        }

        private static readonly Brush NoColorBrush = CreateNoColorBrush();

        private static Brush CreateNoColorBrush()
        {
            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, 28, 16))));
            group.Children.Add(new GeometryDrawing(null, new Pen(Brushes.Red, 2), new LineGeometry(new Point(0, 16), new Point(28, 0))));
            var brush = new DrawingBrush(group);
            brush.Freeze();
            return brush;
        }

        private void OpenPalette()
        {
            _popup ??= CreatePopup();
            _recentPanel.Children.Clear();
            foreach (var color in Recent)
            {
                _recentPanel.Children.Add(Swatch(color));
            }

            _recentPanel.Visibility = Recent.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            ShowColor();
            _popup.IsOpen = true;
        }

        private Popup CreatePopup()
        {
            var theme = ThemeManager.Instance;
            var palette = new UniformGrid { Columns = 8 };
            foreach (var hex in Palette)
            {
                palette.Children.Add(Swatch((Color)ColorConverter.ConvertFromString(hex)));
            }

            _recentPanel = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            var none = new Button { Content = EditorTexts.Wpf.ColorNone, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            none.SetResourceReference(StyleProperty, "SecondaryTextButtonStyle");
            none.Click += (_, _) => Choose(Colors.Transparent);

            var hexLabel = new Label { Content = EditorTexts.Wpf.ColorHex, Padding = new Thickness(0, 6, 0, 2) };
            _hex = new TextBox { MinWidth = 120 };
            hexLabel.Target = _hex;
            _hex.TextChanged += (_, _) =>
            {
                if (!_updating && TryParse(_hex.Text, out var color))
                {
                    _updating = true;
                    SelectedColor = color;
                    _opacity.Value = color.A;
                    _updating = false;
                }
            };

            var opacityLabel = new Label { Content = EditorTexts.Wpf.Opacity, Padding = new Thickness(0, 6, 0, 2) };
            _opacity = new ValueSlider { Minimum = 0, Maximum = 255, SmallChange = 1, LargeChange = 32, ValueFormat = "{0:0}" };
            opacityLabel.Target = _opacity;
            _opacity.ValueChanged += (_, _) =>
            {
                if (!_updating)
                {
                    var color = SelectedColor;
                    _updating = true;
                    SelectedColor = Color.FromArgb((byte)Math.Round(_opacity.Value), color.R, color.G, color.B);
                    _hex.Text = ToHex(SelectedColor);
                    _updating = false;
                }
            };

            var panel = new StackPanel { Margin = new Thickness(8), Width = 8 * 20 };
            panel.Children.Add(palette);
            panel.Children.Add(_recentPanel);
            panel.Children.Add(none);
            panel.Children.Add(hexLabel);
            panel.Children.Add(_hex);
            panel.Children.Add(opacityLabel);
            panel.Children.Add(_opacity);
            var border = new Border { Child = panel, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4) };
            border.SetBinding(Border.BackgroundProperty, new Binding(nameof(ThemeManager.BackgroundBrush)) { Source = theme });
            border.SetBinding(Border.BorderBrushProperty, new Binding(nameof(ThemeManager.BorderBrush)) { Source = theme });
            border.SetBinding(TextElement.ForegroundProperty, new Binding(nameof(ThemeManager.ForegroundBrush)) { Source = theme });
            border.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Greenshot.Base;component/Wpf/Styles/DialogStyles.xaml", UriKind.Relative) });
            border.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Greenshot.Base;component/Wpf/Styles/SliderStyles.xaml", UriKind.Relative) });
            var popup = new Popup
            {
                Child = border,
                PlacementTarget = this,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };
            popup.Closed += (_, _) => Remember(SelectedColor);
            return popup;
        }

        private Button Swatch(Color color)
        {
            var swatch = new Button { Background = new SolidColorBrush(color), Width = 18, Height = 18 };
            swatch.SetResourceReference(StyleProperty, "PaletteSwatchStyle");
            AutomationProperties.SetName(swatch, ToHex(color));
            swatch.ToolTip = ToHex(color);
            swatch.Click += (_, _) => Choose(color);
            return swatch;
        }

        private void Choose(Color color)
        {
            SelectedColor = color;
            _popup.IsOpen = false;
        }

        private static void Remember(Color color)
        {
            if (color.A == 0)
            {
                return;
            }

            Recent.Remove(color);
            Recent.Insert(0, color);
            if (Recent.Count > 8)
            {
                Recent.RemoveAt(Recent.Count - 1);
            }
        }

        public static string ToHex(Color color) => color.A == 255 ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

        public static bool TryParse(string text, out Color color)
        {
            color = default;
            var hex = (text ?? string.Empty).Trim().TrimStart('#');
            if ((hex.Length != 6 && hex.Length != 8) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
            {
                return false;
            }

            if (hex.Length == 6)
            {
                value |= 0xFF000000;
            }

            color = Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
            return true;
        }
    }
}
