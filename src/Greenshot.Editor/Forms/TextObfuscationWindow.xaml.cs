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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using log4net;
using GreenshotLanguage = Greenshot.Base.Core.Language;
using DrawingColor = System.Drawing.Color;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Editor.Forms
{
    public partial class TextObfuscationWindow : Window
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(TextObfuscationWindow));
        private static readonly IEditorConfiguration EditorConfig = IniConfigRegistry.GetSection<IEditorConfiguration>();

        private readonly ISurface _surface;
        private readonly IEnumerable<IOcrLineFeature> _ocrLines;
        private readonly List<NativeRect> _matchedBounds = new List<NativeRect>();
        private readonly List<FilterContainer> _previewContainers = new List<FilterContainer>();
        private readonly DispatcherTimer _debounceTimer;
        private DrawingColor _highlightColor = DrawingColor.Yellow;
        private bool _isInitializing = true;

        public TextObfuscationWindow() : this(null, null)
        {
        }

        public TextObfuscationWindow(ISurface surface, IEnumerable<IOcrLineFeature> ocrLines)
        {
            _surface = surface;
            _ocrLines = ocrLines;

            InitializeComponent();
            try
            {
                Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
            }
            catch (Exception ex)
            {
                LOG.Debug("Could not set window icon", ex);
            }

            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                UpdatePreview();
            };

            InitializeDropdowns();
            LoadSettings();

            _isInitializing = false;

            if (!string.IsNullOrEmpty(SearchTextBox.Text) && SearchTextBox.Text.Length >= 3)
            {
                UpdatePreview();
            }
        }

        private void InitializeDropdowns()
        {
            SearchScopeComboBox.Items.Clear();
            SearchScopeComboBox.Items.Add(GreenshotLanguage.GetString("editor_obfuscate_text_scope_words"));
            SearchScopeComboBox.Items.Add(GreenshotLanguage.GetString("editor_obfuscate_text_scope_lines"));
            SearchScopeComboBox.SelectedIndex = 0;

            EffectComboBox.Items.Clear();
            EffectComboBox.Items.Add(new EffectItem(PreparedFilter.PIXELIZE, GreenshotLanguage.GetString("editor_obfuscate_pixelize")));
            EffectComboBox.Items.Add(new EffectItem(PreparedFilter.BLUR, GreenshotLanguage.GetString("editor_obfuscate_blur")));
            EffectComboBox.Items.Add(new EffectItem(PreparedFilter.TEXT_HIGHTLIGHT, GreenshotLanguage.GetString("editor_highlight_text")));
            EffectComboBox.Items.Add(new EffectItem(PreparedFilter.MAGNIFICATION, GreenshotLanguage.GetString("editor_highlight_magnify")));
            EffectComboBox.SelectedIndex = 0;
        }

        private void LoadSettings()
        {
            SearchTextBox.Text = EditorConfig.TextObfuscationSearchPattern ?? "";
            RegexCheckBox.IsChecked = EditorConfig.TextObfuscationUseRegex;
            CaseSensitiveCheckBox.IsChecked = EditorConfig.TextObfuscationCaseSensitive;
            SearchScopeComboBox.SelectedIndex = Math.Max(0, Math.Min(1, EditorConfig.TextObfuscationSearchScope));
            HorizontalPaddingSlider.Value = EditorConfig.TextObfuscationPaddingHorizontal;
            VerticalPaddingSlider.Value = EditorConfig.TextObfuscationPaddingVertical;
            HorizontalOffsetSlider.Value = EditorConfig.TextObfuscationOffsetHorizontal;
            VerticalOffsetSlider.Value = EditorConfig.TextObfuscationOffsetVertical;

            if (!string.IsNullOrEmpty(EditorConfig.TextObfuscationEffect))
            {
                for (int i = 0; i < EffectComboBox.Items.Count; i++)
                {
                    if (EffectComboBox.Items[i] is EffectItem item && item.Effect.ToString() == EditorConfig.TextObfuscationEffect)
                    {
                        EffectComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            UpdateEffectSettingsVisibility();
            UpdateColorPreview();
        }

        private void SaveSettings()
        {
            EditorConfig.TextObfuscationSearchPattern = SearchTextBox.Text;
            EditorConfig.TextObfuscationUseRegex = RegexCheckBox.IsChecked == true;
            EditorConfig.TextObfuscationCaseSensitive = CaseSensitiveCheckBox.IsChecked == true;
            EditorConfig.TextObfuscationSearchScope = SearchScopeComboBox.SelectedIndex;
            EditorConfig.TextObfuscationPaddingHorizontal = (int)HorizontalPaddingSlider.Value;
            EditorConfig.TextObfuscationPaddingVertical = (int)VerticalPaddingSlider.Value;
            EditorConfig.TextObfuscationOffsetHorizontal = (int)HorizontalOffsetSlider.Value;
            EditorConfig.TextObfuscationOffsetVertical = (int)VerticalOffsetSlider.Value;

            if (EffectComboBox.SelectedItem is EffectItem item)
            {
                EditorConfig.TextObfuscationEffect = item.Effect.ToString();
            }
        }

        private void UpdateEffectSettingsVisibility()
        {
            if (PixelSizeRow == null || BlurRadiusRow == null || HighlightColorRow == null || MagnificationRow == null)
            {
                return;
            }

            PixelSizeRow.Visibility = Visibility.Collapsed;
            BlurRadiusRow.Visibility = Visibility.Collapsed;
            HighlightColorRow.Visibility = Visibility.Collapsed;
            MagnificationRow.Visibility = Visibility.Collapsed;

            if (EffectComboBox.SelectedItem is EffectItem item)
            {
                switch (item.Effect)
                {
                    case PreparedFilter.PIXELIZE:
                        PixelSizeRow.Visibility = Visibility.Visible;
                        break;
                    case PreparedFilter.BLUR:
                        BlurRadiusRow.Visibility = Visibility.Visible;
                        break;
                    case PreparedFilter.TEXT_HIGHTLIGHT:
                        HighlightColorRow.Visibility = Visibility.Visible;
                        break;
                    case PreparedFilter.MAGNIFICATION:
                        MagnificationRow.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void UpdateColorPreview()
        {
            ColorPreviewBorder.Background = new SolidColorBrush(Color.FromArgb(_highlightColor.A, _highlightColor.R, _highlightColor.G, _highlightColor.B));
        }

        private void SearchInput_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void Setting_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void EffectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEffectSettingsVisibility();
            if (!_isInitializing)
            {
                UpdatePreview();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _debounceTimer.Stop();
            UpdatePreview();
        }

        private void PickColorButton_Click(object sender, RoutedEventArgs e)
        {
            var colorWindow = new ColorPickerWindow
            {
                SelectedColor = _highlightColor
            };
            new WindowInteropHelper(colorWindow) { Owner = new WindowInteropHelper(this).Handle };
            if (colorWindow.ShowDialog() == true)
            {
                _highlightColor = colorWindow.SelectedColor;
                UpdateColorPreview();
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            ClearPreview();
            _matchedBounds.Clear();

            if (_surface == null || _ocrLines == null)
            {
                return;
            }

            string searchText = SearchTextBox.Text;
            if (string.IsNullOrEmpty(searchText) || searchText.Length < 3)
            {
                MatchCountTextBlock.Text = string.Format(GreenshotLanguage.GetString("editor_obfuscate_text_matches"), "0");
                return;
            }

            bool useRegex = RegexCheckBox.IsChecked == true;
            bool searchWords = SearchScopeComboBox.SelectedIndex == 0;

            if (useRegex && !IsValidRegex(searchText))
            {
                MatchCountTextBlock.Text = GreenshotLanguage.GetString("editor_obfuscate_text_error") + ": Invalid regex";
                return;
            }

            try
            {
                if (searchWords)
                {
                    SearchWords(searchText, useRegex);
                }
                else
                {
                    SearchLines(searchText, useRegex);
                }

                ShowPreview();
                MatchCountTextBlock.Text = string.Format(GreenshotLanguage.GetString("editor_obfuscate_text_matches"), _matchedBounds.Count.ToString());
            }
            catch (Exception ex)
            {
                MatchCountTextBlock.Text = GreenshotLanguage.GetString("editor_obfuscate_text_error") + ": " + ex.Message;
            }
        }

        private bool IsValidRegex(string pattern)
        {
            try
            {
                var options = CaseSensitiveCheckBox.IsChecked == true ? RegexOptions.None : RegexOptions.IgnoreCase;
                Regex.IsMatch("", pattern, options);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SearchWords(string searchText, bool useRegex)
        {
            foreach (var line in _ocrLines)
            {
                foreach (var word in line.Words)
                {
                    if (IsMatch(word.Text, searchText, useRegex))
                    {
                        _matchedBounds.Add(ApplyPadding(word.Bounds));
                    }
                }
            }
        }

        private void SearchLines(string searchText, bool useRegex)
        {
            foreach (var line in _ocrLines)
            {
                if (IsMatch(line.Text, searchText, useRegex))
                {
                    _matchedBounds.Add(ApplyPadding(line.Bounds));
                }
            }
        }

        private NativeRect ApplyPadding(NativeRect bounds)
        {
            int horizontalPadding = (int)HorizontalPaddingSlider.Value;
            int verticalPadding = (int)VerticalPaddingSlider.Value;
            int horizontalOffset = (int)HorizontalOffsetSlider.Value;
            int verticalOffset = (int)VerticalOffsetSlider.Value;

            int widthPadding = (int)(bounds.Width * horizontalPadding / 100.0 / 2);
            int heightPadding = (int)(bounds.Height * verticalPadding / 100.0 / 2);

            return new NativeRect(
                bounds.Left - widthPadding + horizontalOffset,
                bounds.Top - heightPadding + verticalOffset,
                bounds.Width + (widthPadding * 2),
                bounds.Height + (heightPadding * 2)
            );
        }

        private bool IsMatch(string text, string searchText, bool useRegex)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (useRegex)
            {
                try
                {
                    return Regex.IsMatch(text, searchText, CaseSensitiveCheckBox.IsChecked == true ? RegexOptions.None : RegexOptions.IgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            return CaseSensitiveCheckBox.IsChecked == true
                ? text.Contains(searchText)
                : text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ShowPreview()
        {
            if (_surface == null || !(EffectComboBox.SelectedItem is EffectItem item))
            {
                return;
            }

            foreach (var bounds in _matchedBounds)
            {
                FilterContainer container = CreateFilterContainer(item.Effect, bounds);
                if (container != null)
                {
                    _surface.AddElement(container, false);
                    _previewContainers.Add(container);
                }
            }
            _surface.Invalidate();
        }

        private void ClearPreview()
        {
            if (_surface == null)
            {
                _previewContainers.Clear();
                return;
            }

            foreach (var container in _previewContainers)
            {
                _surface.RemoveElement(container, false);
            }
            _previewContainers.Clear();
            _surface.Invalidate();
        }

        private FilterContainer CreateFilterContainer(PreparedFilter effect, NativeRect bounds)
        {
            FilterContainer container = null;

            switch (effect)
            {
                case PreparedFilter.PIXELIZE:
                case PreparedFilter.BLUR:
                    container = new ObfuscateContainer(_surface);
                    container.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, effect);
                    if (effect == PreparedFilter.PIXELIZE)
                    {
                        container.SetFieldValue(FieldType.PIXEL_SIZE, (int)PixelSizeSlider.Value);
                    }
                    else
                    {
                        container.SetFieldValue(FieldType.BLUR_RADIUS, (int)BlurRadiusSlider.Value);
                    }
                    break;

                case PreparedFilter.TEXT_HIGHTLIGHT:
                case PreparedFilter.MAGNIFICATION:
                    container = new HighlightContainer(_surface);
                    container.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, effect);
                    if (effect == PreparedFilter.TEXT_HIGHTLIGHT)
                    {
                        container.SetFieldValue(FieldType.FILL_COLOR, _highlightColor);
                    }
                    else
                    {
                        container.SetFieldValue(FieldType.MAGNIFICATION_FACTOR, (int)MagnificationSlider.Value);
                    }
                    break;
            }

            if (container != null)
            {
                container.Left = bounds.Left;
                container.Top = bounds.Top;
                container.Width = bounds.Width;
                container.Height = bounds.Height;
            }

            return container;
        }

        public bool? ShowDialog(System.Windows.Forms.IWin32Window owner)
        {
            if (owner != null)
            {
                new WindowInteropHelper(this) { Owner = owner.Handle };
            }
            return ShowDialog();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ClearPreview();
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearPreview();
            DialogResult = false;
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ClearPreview();
            SaveSettings();

            if (!(EffectComboBox.SelectedItem is EffectItem item))
            {
                return;
            }

            var containers = new DrawableContainerList();

            foreach (var bounds in _matchedBounds)
            {
                FilterContainer container = CreateFilterContainer(item.Effect, bounds);
                if (container != null)
                {
                    containers.Add(container);
                }
            }

            if (_surface != null && containers.Count > 0)
            {
                _surface.AddElements(containers, true);
            }

            DialogResult = true;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            ClearPreview();
            _debounceTimer?.Stop();
            base.OnClosed(e);
        }

        private class EffectItem
        {
            public PreparedFilter Effect { get; }
            public string DisplayName { get; }

            public EffectItem(PreparedFilter effect, string displayName)
            {
                Effect = effect;
                DisplayName = displayName;
            }

            public override string ToString() => DisplayName;
        }
    }
}
