/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// The parts of Greenshot's small themed dialogs (message box, quality, progress), built in code
    /// </summary>
    internal static class ThemedControls
    {
        /// <summary>
        /// A window in the card look without a title bar: it is moved by its header
        /// </summary>
        public static void ApplyDialogLook(Window window, string title)
        {
            window.Title = title ?? "Greenshot";
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.ShowInTaskbar = false;
            window.Background = WpfThemeHelper.CardBackground;
            window.BorderBrush = WpfThemeHelper.CardBorder;
            window.BorderThickness = new Thickness(1);
            // Windows 11 rounds windows with a title bar by itself, this one has none
            window.SourceInitialized += (s, e) => WindowFrameTheme.SetRoundedCorners(window);
        }

        /// <summary>
        /// The header of a dialog without a title bar, dragging it moves the window
        /// </summary>
        public static TextBlock CreateHeader(Window window, string text)
        {
            var header = new TextBlock
            {
                Text = text ?? "Greenshot",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = WpfThemeHelper.TextPrimary,
                Margin = new Thickness(0, 0, 0, 10)
            };
            header.MouseLeftButtonDown += (s, e) => window.DragMove();
            return header;
        }

        /// <summary>
        /// A dialog button, the default one in the accent color
        /// </summary>
        public static Button CreateButton(string text, bool isDefault, bool isCancel)
        {
            var palette = ThemeManager.Instance.CurrentPalette;
            return new Button
            {
                Content = text,
                MinWidth = 88,
                Padding = new Thickness(14, 5, 14, 5),
                Margin = new Thickness(8, 0, 0, 0),
                IsDefault = isDefault,
                IsCancel = isCancel,
                Foreground = isDefault ? palette.AccentForegroundBrush : WpfThemeHelper.TextPrimary,
                Background = isDefault ? WpfThemeHelper.Accent : palette.ButtonBackgroundBrush,
                BorderBrush = isDefault ? WpfThemeHelper.Accent : WpfThemeHelper.CardBorder,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Template = CreateButtonTemplate()
            };
        }

        /// <summary>
        /// A flat, rounded button which uses the background and border it is given
        /// </summary>
        private static ControlTemplate CreateButtonTemplate()
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = RelativeSource.TemplatedParent });
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            var template = new ControlTemplate(typeof(Button)) { VisualTree = border };
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.85));
            template.Triggers.Add(hover);
            var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.5));
            template.Triggers.Add(disabled);
            var focused = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
            focused.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(2)));
            template.Triggers.Add(focused);
            return template;
        }

        /// <summary>
        /// A check box with the text in the theme's color
        /// </summary>
        public static CheckBox CreateCheckBox(string text, bool isChecked)
        {
            return new CheckBox
            {
                Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 340 },
                IsChecked = isChecked,
                Foreground = WpfThemeHelper.TextPrimary,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            };
        }
    }
}
