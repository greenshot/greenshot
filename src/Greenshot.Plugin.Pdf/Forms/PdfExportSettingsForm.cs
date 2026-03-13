/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using Greenshot.Base.Controls;
using Greenshot.Base.IniFile;
using Greenshot.Plugin.Pdf.Configuration;

namespace Greenshot.Plugin.Pdf.Forms
{
    /// <summary>
    /// Form for configuring PDF export settings
    /// </summary>
    internal partial class PdfExportSettingsForm : GreenshotForm
    {
        private PdfExportSettings _settings;

        public PdfExportSettingsForm()
        {
            InitializeComponent();
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;

            // Load settings from config
            _settings = IniConfig.GetIniSection<PdfExportSettings>();

            // Set UI values from config
            numericDocumentWidth.Value = (decimal)_settings.DocumentWidth;
            numericDocumentHeight.Value = (decimal)_settings.DocumentHeight;
            numericMarginTop.Value = (decimal)_settings.MarginTop;
            numericMarginBottom.Value = (decimal)_settings.MarginBottom;
            numericMarginLeft.Value = (decimal)_settings.MarginLeft;
            numericMarginRight.Value = (decimal)_settings.MarginRight;

            // Enable/disable document dimensions based on UseFixedDocument
            UpdateDocumentFieldsState();

            // Wire up checkbox change event
            checkBoxUseFixedDocument.CheckedChanged += CheckBoxUseFixedDocument_CheckedChanged;

            // Wire up OK button
            buttonOK.Click += ButtonOK_Click;
        }

        private void CheckBoxUseFixedDocument_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDocumentFieldsState();
        }

        private void UpdateDocumentFieldsState()
        {
            // Enable document width/height only when UseFixedDocument is checked
            numericDocumentWidth.Enabled = checkBoxUseFixedDocument.Checked;
            numericDocumentHeight.Enabled = checkBoxUseFixedDocument.Checked;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            // Write UI values back to config
            _settings.DocumentWidth = (double)numericDocumentWidth.Value;
            _settings.DocumentHeight = (double)numericDocumentHeight.Value;
            _settings.MarginTop = (double)numericMarginTop.Value;
            _settings.MarginBottom = (double)numericMarginBottom.Value;
            _settings.MarginLeft = (double)numericMarginLeft.Value;
            _settings.MarginRight = (double)numericMarginRight.Value;

            // Save to INI file
            IniConfig.Save();
        }
    }
}
