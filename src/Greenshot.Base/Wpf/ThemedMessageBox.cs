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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// A message box in Greenshot's light or dark theme, for the WPF windows (recipe editor, recipe manager, ...).
    /// Show works like <see cref="MessageBox.Show(string, string, MessageBoxButton, MessageBoxImage)"/>; ShowChoice takes own button texts.
    /// </summary>
    public sealed class ThemedMessageBox : Window
    {
        private int _choice = -1;
        private readonly int _cancelIndex;

        private ThemedMessageBox(string caption, string text, MessageBoxImage icon, IReadOnlyList<string> buttons, int defaultIndex, int cancelIndex)
        {
            _cancelIndex = cancelIndex;
            var palette = ThemeManager.Instance.CurrentPalette;
            Title = caption ?? "Greenshot";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = WpfThemeHelper.CardBackground;
            BorderBrush = WpfThemeHelper.CardBorder;
            BorderThickness = new Thickness(1);
            MinWidth = 360;
            MaxWidth = 640;

            var header = new TextBlock
            {
                Text = caption ?? "Greenshot",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = WpfThemeHelper.TextPrimary,
                Margin = new Thickness(0, 0, 0, 10)
            };
            // The window has no title bar: it is moved by its header
            header.MouseLeftButtonDown += (s, e) => DragMove();

            var message = new TextBlock
            {
                Text = text ?? string.Empty,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = WpfThemeHelper.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center
            };

            var body = new DockPanel { LastChildFill = true };
            string glyph = GetGlyph(icon);
            if (glyph != null)
            {
                var iconText = new TextBlock
                {
                    Text = glyph,
                    FontSize = 24,
                    Margin = new Thickness(0, 0, 14, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    Foreground = icon == MessageBoxImage.Error ? WpfThemeHelper.ErrorText
                        : icon == MessageBoxImage.Warning ? WpfThemeHelper.WarningText
                        : WpfThemeHelper.Accent
                };
                DockPanel.SetDock(iconText, Dock.Left);
                body.Children.Add(iconText);
            }
            body.Children.Add(new ScrollViewer
            {
                Content = message,
                MaxHeight = 420,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            });

            var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            for (int i = 0; i < buttons.Count; i++)
            {
                int index = i;
                bool isDefault = i == defaultIndex;
                var button = new Button
                {
                    Content = buttons[i],
                    MinWidth = 88,
                    Padding = new Thickness(14, 5, 14, 5),
                    Margin = new Thickness(8, 0, 0, 0),
                    IsDefault = isDefault,
                    IsCancel = i == cancelIndex,
                    Foreground = isDefault ? Brushes.White : WpfThemeHelper.TextPrimary,
                    Background = isDefault ? WpfThemeHelper.Accent : palette.ButtonBackgroundBrush,
                    BorderBrush = isDefault ? WpfThemeHelper.Accent : WpfThemeHelper.CardBorder,
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Template = CreateButtonTemplate()
                };
                button.Click += (s, e) =>
                {
                    _choice = index;
                    Close();
                };
                buttonRow.Children.Add(button);
                if (isDefault)
                {
                    Loaded += (s, e) => button.Focus();
                }
            }

            var root = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };
            root.Children.Add(header);
            root.Children.Add(body);
            root.Children.Add(buttonRow);
            Content = root;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_choice < 0)
            {
                // Closed with Escape or Alt+F4: the cancel button
                _choice = _cancelIndex;
            }
        }

        /// <summary>
        /// Shows the message with own button texts; returns the index of the chosen button, or cancelIndex when the box was closed.
        /// With onTop the box is shown over all windows, also those of other programs (for questions which come from outside).
        /// </summary>
        public static int ShowChoice(Window owner, string caption, string text, MessageBoxImage icon, IReadOnlyList<string> buttons, int defaultIndex = 0, int cancelIndex = -1, bool onTop = false)
        {
            var box = new ThemedMessageBox(caption, text, icon, buttons, defaultIndex, cancelIndex);
            owner ??= onTop ? null : FindOwner();
            if (onTop)
            {
                box.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                box.Topmost = true;
                box.ShowInTaskbar = true;
                box.Loaded += (s, e) => box.Activate();
            }
            else if (owner != null && owner.IsVisible)
            {
                box.Owner = owner;
            }
            else
            {
                box.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                box.Topmost = true;
            }
            box.ShowDialog();
            return box._choice;
        }

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
            => Show(null, text, caption, button, icon);

        public static MessageBoxResult Show(Window owner, string text, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
        {
            (string Text, MessageBoxResult Result)[] choices = button switch
            {
                MessageBoxButton.OKCancel => new[] { ("OK", MessageBoxResult.OK), ("Cancel", MessageBoxResult.Cancel) },
                MessageBoxButton.YesNo => new[] { ("Yes", MessageBoxResult.Yes), ("No", MessageBoxResult.No) },
                MessageBoxButton.YesNoCancel => new[] { ("Yes", MessageBoxResult.Yes), ("No", MessageBoxResult.No), ("Cancel", MessageBoxResult.Cancel) },
                _ => new[] { ("OK", MessageBoxResult.OK) }
            };
            int cancelIndex = Array.FindIndex(choices, c => c.Result == MessageBoxResult.Cancel || c.Result == MessageBoxResult.No);
            if (button == MessageBoxButton.YesNoCancel)
            {
                cancelIndex = 2;
            }
            if (cancelIndex < 0)
            {
                cancelIndex = 0;
            }
            int choice = ShowChoice(owner, caption, text, icon, choices.Select(c => c.Text).ToList(), 0, cancelIndex);
            return choice >= 0 && choice < choices.Length ? choices[choice].Result : MessageBoxResult.None;
        }

        private static Window FindOwner()
        {
            var windows = Application.Current?.Windows.OfType<Window>().Where(w => w.IsVisible && !(w is ThemedMessageBox)).ToList();
            return windows?.FirstOrDefault(w => w.IsActive) ?? windows?.LastOrDefault();
        }

        private static string GetGlyph(MessageBoxImage icon)
        {
            switch (icon)
            {
                case MessageBoxImage.Error: return "⛔";
                case MessageBoxImage.Warning: return "⚠";
                case MessageBoxImage.Question: return "❓";
                case MessageBoxImage.Information: return "ℹ";
                default: return null;
            }
        }

        /// <summary>
        /// A flat, rounded button which uses the background and border it is given
        /// </summary>
        private static ControlTemplate CreateButtonTemplate()
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            var template = new ControlTemplate(typeof(Button)) { VisualTree = border };
            var hover = new Trigger { Property = IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(OpacityProperty, 0.85));
            template.Triggers.Add(hover);
            var focused = new Trigger { Property = IsKeyboardFocusedProperty, Value = true };
            focused.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(2)));
            template.Triggers.Add(focused);
            return template;
        }
    }
}
