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
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// Form for searching and obfuscating text in OCR results
    /// </summary>
    public partial class TextObfuscationForm : EditorForm
    {
        private readonly ISurface _surface;
        private readonly OcrInformation _ocrInfo;
        private readonly List<NativeRect> _matchedBounds = new List<NativeRect>();
        private readonly List<RectangleContainer> _previewContainers = new List<RectangleContainer>();

        public TextObfuscationForm(ISurface surface, OcrInformation ocrInfo)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _ocrInfo = ocrInfo ?? throw new ArgumentNullException(nameof(ocrInfo));
            InitializeComponent();
            
            searchScopeComboBox.Items.Add(Language.GetString("editor_obfuscate_text_scope_words"));
            searchScopeComboBox.Items.Add(Language.GetString("editor_obfuscate_text_scope_lines"));
            searchScopeComboBox.SelectedIndex = 0;
            
            UpdatePreview();
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            if (autoSearchCheckBox.Checked)
            {
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            ClearPreview();
            _matchedBounds.Clear();

            string searchText = searchTextBox.Text;
            if (string.IsNullOrEmpty(searchText))
            {
                matchCountLabel.Text = string.Format(Language.GetString("editor_obfuscate_text_matches"), "0");
                return;
            }

            bool useRegex = regexCheckBox.Checked;
            bool searchWords = searchScopeComboBox.SelectedIndex == 0;

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

        private void SearchWords(string searchText, bool useRegex)
        {
            foreach (var line in _ocrInfo.Lines)
            {
                foreach (var word in line.Words)
                {
                    if (IsMatch(word.Text, searchText, useRegex))
                    {
                        _matchedBounds.Add(word.Bounds);
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
                    _matchedBounds.Add(line.CalculatedBounds);
                }
            }
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
            foreach (var bounds in _matchedBounds)
            {
                var container = new RectangleContainer(_surface)
                {
                    Left = bounds.Left,
                    Top = bounds.Top,
                    Width = bounds.Width,
                    Height = bounds.Height
                };
                container.SetFieldValue(FieldType.LINE_COLOR, Color.Yellow);
                container.SetFieldValue(FieldType.LINE_THICKNESS, 3);
                container.SetFieldValue(FieldType.FILL_COLOR, Color.FromArgb(50, Color.Yellow));
                _surface.AddElement(container, false);
                _previewContainers.Add(container);
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

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            ClearPreview();

            foreach (var bounds in _matchedBounds)
            {
                var obfuscate = new ObfuscateContainer(_surface)
                {
                    Left = bounds.Left,
                    Top = bounds.Top,
                    Width = bounds.Width,
                    Height = bounds.Height
                };
                _surface.AddElement(obfuscate, true);
            }

            DialogResult = DialogResult.OK;
            Close();
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
            base.OnFormClosing(e);
        }
    }
}
