/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.IniFile;

namespace Greenshot.Plugin.CaptionBar
{
    /// <summary>
    /// Settings form for CaptionBar plugin
    /// </summary>
    public partial class CaptionBarSettingsForm : CaptionBarForm
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(CaptionBarSettingsForm));
        private readonly CaptionBarConfiguration _config;

        public CaptionBarSettingsForm(CaptionBarConfiguration config)
        {
            _config = config;
            InitializeComponent();

            // Populate alignment combo boxes with enum values
            comboBoxTimestampAlignment.Items.AddRange(new object[] { "Left (Near)", "Center", "Right (Far)" });
            comboBoxCustomTextAlignment.Items.AddRange(new object[] { "Left (Near)", "Center", "Right (Far)" });

            // Load values for controls that don't have auto-binding
            LoadNumericValues();
        }

        /// <summary>
        /// Load numeric values and color previews manually (NumericUpDown doesn't support Greenshot config binding)
        /// </summary>
        private void LoadNumericValues()
        {
            try
            {
                Log.DebugFormat("Loading settings: BarHeight={0}, FontSize={1}, TextPadding={2}",
                    _config.BarHeight, _config.FontSize, _config.TextPadding);

                // Set numeric values with bounds checking
                numericBarHeight.Value = Math.Min(Math.Max(_config.BarHeight, 20), 200);
                numericFontSize.Value = Math.Min(Math.Max((int)_config.FontSize, 6), 72);
                numericTextPadding.Value = Math.Min(Math.Max(_config.TextPadding, 0), 50);

                // Set color previews
                panelBackgroundColorPreview.BackColor = _config.BackgroundColor;
                panelTextColorPreview.BackColor = _config.TextColor;

                // Set alignment combo box selections
                int timestampAlignIndex = (int)_config.TimestampAlignment;
                int customTextAlignIndex = (int)_config.CustomTextAlignment;

                if (timestampAlignIndex >= 0 && timestampAlignIndex < comboBoxTimestampAlignment.Items.Count)
                {
                    comboBoxTimestampAlignment.SelectedIndex = timestampAlignIndex;
                }

                if (customTextAlignIndex >= 0 && customTextAlignIndex < comboBoxCustomTextAlignment.Items.Count)
                {
                    comboBoxCustomTextAlignment.SelectedIndex = customTextAlignIndex;
                }

                Log.DebugFormat("Loaded numeric values: BarHeight={0}, FontSize={1}, TextPadding={2}",
                    numericBarHeight.Value, numericFontSize.Value, numericTextPadding.Value);
            }
            catch (Exception ex)
            {
                Log.Error("Error loading numeric settings", ex);
            }
        }

        /// <summary>
        /// Save numeric values and colors manually before closing
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                SaveNumericValues();
            }
            base.OnClosed(e);  // This calls GreenshotForm.StoreFields() for config-bound controls
        }

        /// <summary>
        /// Save numeric values and color settings manually
        /// </summary>
        private void SaveNumericValues()
        {
            try
            {
                _config.BarHeight = (int)numericBarHeight.Value;
                _config.FontSize = (float)numericFontSize.Value;
                _config.TextPadding = (int)numericTextPadding.Value;

                _config.BackgroundColor = panelBackgroundColorPreview.BackColor;
                _config.TextColor = panelTextColorPreview.BackColor;

                // Set alignments from combo boxes
                if (comboBoxTimestampAlignment.SelectedIndex >= 0)
                {
                    _config.TimestampAlignment = (StringAlignment)comboBoxTimestampAlignment.SelectedIndex;
                }

                if (comboBoxCustomTextAlignment.SelectedIndex >= 0)
                {
                    _config.CustomTextAlignment = (StringAlignment)comboBoxCustomTextAlignment.SelectedIndex;
                }

                // Save to INI file
                IniConfig.Save();

                Log.InfoFormat("CaptionBar settings saved: BarHeight={0}, FontSize={1}, TextPadding={2}",
                    _config.BarHeight, _config.FontSize, _config.TextPadding);
            }
            catch (Exception ex)
            {
                Log.Error("Error saving settings", ex);
                MessageBox.Show("Error saving settings: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handle background color picker button click
        /// </summary>
        private void ButtonBackgroundColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = panelBackgroundColorPreview.BackColor;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    panelBackgroundColorPreview.BackColor = colorDialog.Color;
                }
            }
        }

        /// <summary>
        /// Handle text color picker button click
        /// </summary>
        private void ButtonTextColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = panelTextColorPreview.BackColor;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    panelTextColorPreview.BackColor = colorDialog.Color;
                }
            }
        }
    }
}
