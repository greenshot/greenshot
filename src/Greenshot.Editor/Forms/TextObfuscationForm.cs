/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// Enhanced form for searching and obfuscating text in OCR results
    /// </summary>
    public partial class TextObfuscationForm : EditorForm
    {
        private static readonly EditorConfiguration EditorConfig = IniConfig.GetIniSection<EditorConfiguration>();
        
        private readonly ISurface _surface;
        private readonly OcrInformation _ocrInfo;
        private readonly List<NativeRect> _matchedBounds = new List<NativeRect>();
        private readonly List<FilterContainer> _previewContainers = new List<FilterContainer>();
        private IDisposable _searchSubscription;
        private bool _isInitializing = true;

        public TextObfuscationForm(ISurface surface, OcrInformation ocrInfo)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _ocrInfo = ocrInfo ?? throw new ArgumentNullException(nameof(ocrInfo));
            InitializeComponent();
            
            // Initialize match count label with formatted text
            matchCountLabel.Text = string.Format(Language.GetString("editor_obfuscate_text_matches"), "0");
            
            InitializeEffectDropdown();
            InitializeSearchScopeDropdown();
            LoadSettings();
            SetupDebouncedSearch();
            SetupColorPicker();
            
            _isInitializing = false;
            
            // Trigger initial search if text is pre-filled
            if (!string.IsNullOrEmpty(searchTextBox.Text) && searchTextBox.Text.Length >= 3)
            {
                UpdatePreview();
            }
        }

        private void InitializeEffectDropdown()
        {
            effectComboBox.Items.Clear();
            effectComboBox.Items.Add(new EffectItem(PreparedFilter.PIXELIZE, Language.GetString("editor_obfuscate_pixelize")));
            effectComboBox.Items.Add(new EffectItem(PreparedFilter.BLUR, Language.GetString("editor_obfuscate_blur")));
            effectComboBox.Items.Add(new EffectItem(PreparedFilter.TEXT_HIGHTLIGHT, "Highlight Text"));
            effectComboBox.Items.Add(new EffectItem(PreparedFilter.MAGNIFICATION, "Magnify"));
            // Exclude AREA_HIGHLIGHT and GRAYSCALE as requested
            
            effectComboBox.SelectedIndex = 0;
            effectComboBox.SelectedIndexChanged += EffectComboBox_SelectedIndexChanged;
        }

        private void InitializeSearchScopeDropdown()
        {
            searchScopeComboBox.Items.Add(Language.GetString("editor_obfuscate_text_scope_words"));
            searchScopeComboBox.Items.Add(Language.GetString("editor_obfuscate_text_scope_lines"));
            searchScopeComboBox.SelectedIndex = 0;
        }

        private void LoadSettings()
        {
            searchTextBox.Text = EditorConfig.TextObfuscationSearchPattern ?? "";
            regexCheckBox.Checked = EditorConfig.TextObfuscationUseRegex;
            caseSensitiveCheckBox.Checked = EditorConfig.TextObfuscationCaseSensitive;
            searchScopeComboBox.SelectedIndex = EditorConfig.TextObfuscationSearchScope;
            paddingHorizontalUpDown.Value = EditorConfig.TextObfuscationPaddingHorizontal;
            paddingVerticalUpDown.Value = EditorConfig.TextObfuscationPaddingVertical;
            offsetHorizontalUpDown.Value = EditorConfig.TextObfuscationOffsetHorizontal;
            offsetVerticalUpDown.Value = EditorConfig.TextObfuscationOffsetVertical;
            
            // Set effect from config
            if (!string.IsNullOrEmpty(EditorConfig.TextObfuscationEffect))
            {
                for (int i = 0; i < effectComboBox.Items.Count; i++)
                {
                    if (effectComboBox.Items[i] is EffectItem item && item.Effect.ToString() == EditorConfig.TextObfuscationEffect)
                    {
                        effectComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            
            // Update effect settings visibility after loading
            UpdateEffectSettings();
        }

        private void SaveSettings()
        {
            EditorConfig.TextObfuscationSearchPattern = searchTextBox.Text;
            EditorConfig.TextObfuscationUseRegex = regexCheckBox.Checked;
            EditorConfig.TextObfuscationCaseSensitive = caseSensitiveCheckBox.Checked;
            EditorConfig.TextObfuscationSearchScope = searchScopeComboBox.SelectedIndex;
            EditorConfig.TextObfuscationPaddingHorizontal = (int)paddingHorizontalUpDown.Value;
            EditorConfig.TextObfuscationPaddingVertical = (int)paddingVerticalUpDown.Value;
            EditorConfig.TextObfuscationOffsetHorizontal = (int)offsetHorizontalUpDown.Value;
            EditorConfig.TextObfuscationOffsetVertical = (int)offsetVerticalUpDown.Value;
            
            if (effectComboBox.SelectedItem is EffectItem item)
            {
                EditorConfig.TextObfuscationEffect = item.Effect.ToString();
            }
        }

        private void SetupDebouncedSearch()
        {
            // Create observable from TextChanged event with debounce
            var textChanged = Observable.FromEventPattern<EventArgs>(searchTextBox, "TextChanged")
                .Select(_ => searchTextBox.Text)
                .DistinctUntilChanged()
                .Where(text => text.Length == 0 || text.Length >= 3) // Minimum 3 characters
                .Throttle(TimeSpan.FromMilliseconds(300)) // Debounce 300ms
                .ObserveOn(this);

            _searchSubscription = textChanged.Subscribe(_ => UpdatePreview());

            // Also trigger search when checkboxes change
            regexCheckBox.CheckedChanged += (s, e) => UpdatePreview();
            caseSensitiveCheckBox.CheckedChanged += (s, e) => UpdatePreview();
            searchScopeComboBox.SelectedIndexChanged += (s, e) => UpdatePreview();
        }

        private void SetupColorPicker()
        {
            highlightColorButton.Click += (s, e) =>
            {
                using (var colorDialog = new ColorDialog())
                {
                    colorDialog.Color = highlightColorButton.BackColor;
                    if (colorDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        highlightColorButton.BackColor = colorDialog.Color;
                        // Update preview if we're showing highlight effect
                        if (effectComboBox.SelectedItem is EffectItem item && item.Effect == PreparedFilter.TEXT_HIGHTLIGHT)
                        {
                            UpdatePreview();
                        }
                    }
                }
            };
            
            // Update preview when any setting changes
            pixelSizeUpDown.ValueChanged += (s, e) => { if (!_isInitializing) UpdatePreview(); };
            blurRadiusUpDown.ValueChanged += (s, e) => { if (!_isInitializing) UpdatePreview(); };
            magnificationUpDown.ValueChanged += (s, e) => { if (!_isInitializing) UpdatePreview(); };
            paddingHorizontalUpDown.ValueChanged += (s, e) => { if (!_isInitializing) UpdatePreview(); };
            paddingVerticalUpDown.ValueChanged += (s, e) => { if (!_isInitializing) UpdatePreview(); };
            offsetHorizontalUpDown.ValueChanged += (s, e) => { if (!_isInitializing) UpdatePreview(); };
            offsetVerticalUpDown.ValueChanged += (s, e) => { if (!_isInitializing) UpdatePreview(); };
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            ClearPreview();
            _matchedBounds.Clear();

            string searchText = searchTextBox.Text;
            if (string.IsNullOrEmpty(searchText) || searchText.Length < 3)
            {
                matchCountLabel.Text = string.Format(Language.GetString("editor_obfuscate_text_matches"), "0");
                return;
            }

            bool useRegex = regexCheckBox.Checked;
            bool searchWords = searchScopeComboBox.SelectedIndex == 0;

            // Validate regex if using regex mode
            if (useRegex && !IsValidRegex(searchText))
            {
                matchCountLabel.Text = Language.GetString("editor_obfuscate_text_error") + ": Invalid regex pattern";
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
                matchCountLabel.Text = string.Format(Language.GetString("editor_obfuscate_text_matches"), _matchedBounds.Count.ToString());
            }
            catch (Exception ex)
            {
                matchCountLabel.Text = Language.GetString("editor_obfuscate_text_error") + ": " + ex.Message;
            }
        }

        private bool IsValidRegex(string pattern)
        {
            try
            {
                var options = caseSensitiveCheckBox.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
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
            foreach (var line in _ocrInfo.Lines)
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
            foreach (var line in _ocrInfo.Lines)
            {
                if (IsMatch(line.Text, searchText, useRegex))
                {
                    _matchedBounds.Add(ApplyPadding(line.CalculatedBounds));
                }
            }
        }

        private NativeRect ApplyPadding(NativeRect bounds)
        {
            int horizontalPadding = (int)paddingHorizontalUpDown.Value;
            int verticalPadding = (int)paddingVerticalUpDown.Value;
            int horizontalOffset = (int)offsetHorizontalUpDown.Value;
            int verticalOffset = (int)offsetVerticalUpDown.Value;
            
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
                    return Regex.IsMatch(text, searchText, caseSensitiveCheckBox.Checked ? RegexOptions.None : RegexOptions.IgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            return caseSensitiveCheckBox.Checked
                ? text.Contains(searchText)
                : text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ShowPreview()
        {
            if (!(effectComboBox.SelectedItem is EffectItem item))
            {
                return;
            }

            foreach (var bounds in _matchedBounds)
            {
                // Create preview using actual effect instead of yellow boxes
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
            foreach (var container in _previewContainers)
            {
                _surface.RemoveElement(container, false);
            }
            _previewContainers.Clear();
            _surface.Invalidate();
        }

        private void EffectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEffectSettings();
            // Update preview to show new effect
            if (!_isInitializing)
            {
                UpdatePreview();
            }
        }

        private void UpdateEffectSettings()
        {
            if (!(effectComboBox.SelectedItem is EffectItem item))
            {
                return;
            }

            // Hide all settings first
            pixelSizeLabel.Visible = pixelSizeUpDown.Visible = false;
            blurRadiusLabel.Visible = blurRadiusUpDown.Visible = false;
            highlightColorLabel.Visible = highlightColorButton.Visible = false;
            magnificationLabel.Visible = magnificationUpDown.Visible = false;

            // Show relevant settings based on effect
            switch (item.Effect)
            {
                case PreparedFilter.PIXELIZE:
                    pixelSizeLabel.Visible = pixelSizeUpDown.Visible = true;
                    break;
                case PreparedFilter.BLUR:
                    blurRadiusLabel.Visible = blurRadiusUpDown.Visible = true;
                    break;
                case PreparedFilter.TEXT_HIGHTLIGHT:
                    highlightColorLabel.Visible = highlightColorButton.Visible = true;
                    break;
                case PreparedFilter.MAGNIFICATION:
                    magnificationLabel.Visible = magnificationUpDown.Visible = true;
                    break;
            }
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            ClearPreview();
            SaveSettings();

            if (!(effectComboBox.SelectedItem is EffectItem item))
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

            if (containers.Count > 0)
            {
                _surface.AddElements(containers, true);
            }

            DialogResult = DialogResult.OK;
            Close();
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
                        container.SetFieldValue(FieldType.PIXEL_SIZE, (int)pixelSizeUpDown.Value);
                    }
                    else
                    {
                        container.SetFieldValue(FieldType.BLUR_RADIUS, (int)blurRadiusUpDown.Value);
                    }
                    break;
                    
                case PreparedFilter.TEXT_HIGHTLIGHT:
                case PreparedFilter.MAGNIFICATION:
                    container = new HighlightContainer(_surface);
                    container.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, effect);
                    if (effect == PreparedFilter.TEXT_HIGHTLIGHT)
                    {
                        // HighlightFilter uses FILL_COLOR, not HIGHLIGHT_COLOR
                        container.SetFieldValue(FieldType.FILL_COLOR, highlightColorButton.BackColor);
                    }
                    else
                    {
                        container.SetFieldValue(FieldType.MAGNIFICATION_FACTOR, (int)magnificationUpDown.Value);
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

        private void CancelButton_Click(object sender, EventArgs e)
        {
            ClearPreview();
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ClearPreview();
            _searchSubscription?.Dispose();
            base.OnFormClosing(e);
        }

        private void AdvancedSettingsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            advancedSettingsGroupBox.Visible = advancedSettingsCheckBox.Checked;
            
            // Resize form based on whether advanced settings are shown
            if (advancedSettingsCheckBox.Checked)
            {
                ClientSize = new System.Drawing.Size(511, 340);  // 215 + 80 (groupbox) + 45 (buttons)
            }
            else
            {
                ClientSize = new System.Drawing.Size(511, 250);  // 190 + 30 (checkbox) + 30 (buttons)
            }
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
