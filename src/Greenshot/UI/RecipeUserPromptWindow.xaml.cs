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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Greenshot.Base.Recipes;

namespace Greenshot.UI
{
    public class PromptButtonViewModel
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Style { get; set; }
        public bool IsDefault { get; set; }
        public bool IsCancel { get; set; }
        public Style ButtonStyle { get; set; }
    }

    /// <summary>
    /// Interaction logic for RecipeUserPromptWindow.xaml
    /// </summary>
    public partial class RecipeUserPromptWindow : Window, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private int _secondsRemaining;
        private readonly string _defaultChoiceKey;
        private readonly string _cancelChoiceKey;

        public event PropertyChangedEventHandler PropertyChanged;

        public string WindowTitle { get; set; } = "Greenshot - Decision Prompt";
        public string PromptTitle { get; set; } = "Decision Required";
        public string PromptMessage { get; set; } = "Please select how you would like to proceed:";
        public BitmapSource PreviewImageSource { get; set; }
        public Visibility PreviewVisibility => PreviewImageSource != null ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<PromptButtonViewModel> ActionButtons { get; } = new ObservableCollection<PromptButtonViewModel>();

        public string SelectedChoiceKey { get; private set; }

        public string TimeoutText => _secondsRemaining > 0 ? $"Auto-selecting default choice in {_secondsRemaining}s..." : "";
        public Visibility TimeoutVisibility => _secondsRemaining > 0 ? Visibility.Visible : Visibility.Collapsed;

        public SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;

        public RecipeUserPromptWindow(
            string title,
            string message,
            IEnumerable<PromptChoice> choices,
            System.Drawing.Image previewImage = null,
            int timeoutSeconds = 0,
            string defaultChoice = null)
        {
            InitializeComponent();
            DataContext = this;

            if (!string.IsNullOrWhiteSpace(title))
            {
                PromptTitle = title;
                WindowTitle = $"Greenshot - {title}";
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                PromptMessage = message;
            }

            if (previewImage != null)
            {
                try
                {
                    using var ms = new MemoryStream();
                    previewImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.EndInit();
                    bi.Freeze();
                    PreviewImageSource = bi;
                }
                catch
                {
                    // Ignore preview load error
                }
            }

            var primaryStyle = FindResource("PrimaryButtonStyle") as Style;
            var dangerStyle = FindResource("DangerButtonStyle") as Style;
            var secondaryStyle = FindResource("SecondaryButtonStyle") as Style;

            if (choices != null)
            {
                foreach (var choice in choices)
                {
                    Style btnStyle = secondaryStyle;
                    if (string.Equals(choice.Style, "Primary", StringComparison.OrdinalIgnoreCase)) btnStyle = primaryStyle;
                    else if (string.Equals(choice.Style, "Danger", StringComparison.OrdinalIgnoreCase)) btnStyle = dangerStyle;

                    if (choice.IsDefault && _defaultChoiceKey == null)
                    {
                        _defaultChoiceKey = choice.Key;
                    }
                    if (choice.IsCancel && _cancelChoiceKey == null)
                    {
                        _cancelChoiceKey = choice.Key;
                    }
                    else if (_cancelChoiceKey == null && (string.Equals(choice.Key, "No", StringComparison.OrdinalIgnoreCase) || string.Equals(choice.Key, "Cancel", StringComparison.OrdinalIgnoreCase)))
                    {
                        _cancelChoiceKey = choice.Key;
                    }

                    ActionButtons.Add(new PromptButtonViewModel
                    {
                        Key = choice.Key,
                        Label = choice.Label ?? choice.Key,
                        Style = choice.Style,
                        IsDefault = choice.IsDefault,
                        IsCancel = choice.IsCancel,
                        ButtonStyle = btnStyle
                    });
                }
            }

            if (ActionButtons.Count == 0)
            {
                _defaultChoiceKey = "Yes";
                _cancelChoiceKey = "No";
                ActionButtons.Add(new PromptButtonViewModel { Key = "Yes", Label = "Yes, Proceed", ButtonStyle = primaryStyle, IsDefault = true });
                ActionButtons.Add(new PromptButtonViewModel { Key = "No", Label = "No, Cancel", ButtonStyle = secondaryStyle, IsCancel = true });
            }

            if (_cancelChoiceKey == null)
            {
                _cancelChoiceKey = ActionButtons.FirstOrDefault(b => b.IsCancel)?.Key ??
                                   ActionButtons.FirstOrDefault(b => string.Equals(b.Key, "No", StringComparison.OrdinalIgnoreCase))?.Key ??
                                   ActionButtons.LastOrDefault()?.Key ?? "No";
            }

            if (!string.IsNullOrWhiteSpace(defaultChoice))
            {
                _defaultChoiceKey = defaultChoice;
            }

            if (timeoutSeconds > 0)
            {
                _secondsRemaining = timeoutSeconds;
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += (s, e) =>
                {
                    _secondsRemaining--;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeoutText)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeoutVisibility)));
                    if (_secondsRemaining <= 0)
                    {
                        _timer.Stop();
                        SelectAndClose(_defaultChoiceKey ?? ActionButtons[0].Key);
                    }
                };
                _timer.Start();
            }

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    SelectAndClose(_defaultChoiceKey ?? ActionButtons[0].Key);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    SelectAndClose(_cancelChoiceKey ?? "No");
                    e.Handled = true;
                }
            };
        }

        private void SelectAndClose(string choiceKey)
        {
            _timer?.Stop();
            SelectedChoiceKey = choiceKey;
            DialogResult = true;
            Close();
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                SelectAndClose(key);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SelectAndClose(_cancelChoiceKey ?? "No");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (string.IsNullOrEmpty(SelectedChoiceKey))
            {
                SelectedChoiceKey = _cancelChoiceKey ?? "No";
            }
        }
    }
}
