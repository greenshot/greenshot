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

using Greenshot.Base.Controls;

namespace Greenshot.Editor.Forms
{
    partial class TextObfuscationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.searchLabel = new GreenshotLabel();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.searchButton = new GreenshotButton();
            this.regexCheckBox = new GreenshotCheckBox();
            this.caseSensitiveCheckBox = new GreenshotCheckBox();
            this.autoSearchCheckBox = new GreenshotCheckBox();
            this.searchScopeLabel = new GreenshotLabel();
            this.searchScopeComboBox = new System.Windows.Forms.ComboBox();
            this.matchCountLabel = new GreenshotLabel();
            this.applyButton = new GreenshotButton();
            this.cancelButton = new GreenshotButton();
            this.SuspendLayout();
            // 
            // searchLabel
            // 
            this.searchLabel.LanguageKey = "editor_obfuscate_text_search";
            this.searchLabel.Location = new System.Drawing.Point(12, 15);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(100, 20);
            this.searchLabel.TabIndex = 0;
            // 
            // searchTextBox
            // 
            this.searchTextBox.Location = new System.Drawing.Point(118, 12);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(300, 20);
            this.searchTextBox.TabIndex = 1;
            this.searchTextBox.TextChanged += new System.EventHandler(this.SearchTextBox_TextChanged);
            // 
            // searchButton
            // 
            this.searchButton.LanguageKey = "editor_obfuscate_text_search_button";
            this.searchButton.Location = new System.Drawing.Point(424, 10);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(75, 23);
            this.searchButton.TabIndex = 2;
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.SearchButton_Click);
            // 
            // regexCheckBox
            // 
            this.regexCheckBox.LanguageKey = "editor_obfuscate_text_regex";
            this.regexCheckBox.Location = new System.Drawing.Point(118, 38);
            this.regexCheckBox.Name = "regexCheckBox";
            this.regexCheckBox.Size = new System.Drawing.Size(150, 24);
            this.regexCheckBox.TabIndex = 3;
            this.regexCheckBox.UseVisualStyleBackColor = true;
            // 
            // caseSensitiveCheckBox
            // 
            this.caseSensitiveCheckBox.LanguageKey = "editor_obfuscate_text_case_sensitive";
            this.caseSensitiveCheckBox.Location = new System.Drawing.Point(274, 38);
            this.caseSensitiveCheckBox.Name = "caseSensitiveCheckBox";
            this.caseSensitiveCheckBox.Size = new System.Drawing.Size(150, 24);
            this.caseSensitiveCheckBox.TabIndex = 4;
            this.caseSensitiveCheckBox.UseVisualStyleBackColor = true;
            // 
            // autoSearchCheckBox
            // 
            this.autoSearchCheckBox.Checked = true;
            this.autoSearchCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoSearchCheckBox.LanguageKey = "editor_obfuscate_text_auto_search";
            this.autoSearchCheckBox.Location = new System.Drawing.Point(118, 68);
            this.autoSearchCheckBox.Name = "autoSearchCheckBox";
            this.autoSearchCheckBox.Size = new System.Drawing.Size(150, 24);
            this.autoSearchCheckBox.TabIndex = 5;
            this.autoSearchCheckBox.UseVisualStyleBackColor = true;
            // 
            // searchScopeLabel
            // 
            this.searchScopeLabel.LanguageKey = "editor_obfuscate_text_search_scope";
            this.searchScopeLabel.Location = new System.Drawing.Point(12, 100);
            this.searchScopeLabel.Name = "searchScopeLabel";
            this.searchScopeLabel.Size = new System.Drawing.Size(100, 20);
            this.searchScopeLabel.TabIndex = 6;
            // 
            // searchScopeComboBox
            // 
            this.searchScopeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.searchScopeComboBox.FormattingEnabled = true;
            this.searchScopeComboBox.Location = new System.Drawing.Point(118, 97);
            this.searchScopeComboBox.Name = "searchScopeComboBox";
            this.searchScopeComboBox.Size = new System.Drawing.Size(150, 21);
            this.searchScopeComboBox.TabIndex = 7;
            // 
            // matchCountLabel
            // 
            this.matchCountLabel.LanguageKey = "editor_obfuscate_text_matches";
            this.matchCountLabel.Location = new System.Drawing.Point(12, 130);
            this.matchCountLabel.Name = "matchCountLabel";
            this.matchCountLabel.Size = new System.Drawing.Size(487, 20);
            this.matchCountLabel.TabIndex = 8;
            // 
            // applyButton
            // 
            this.applyButton.LanguageKey = "editor_obfuscate_text_apply";
            this.applyButton.Location = new System.Drawing.Point(343, 160);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 9;
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.ApplyButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.LanguageKey = "CANCEL";
            this.cancelButton.Location = new System.Drawing.Point(424, 160);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 10;
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // TextObfuscationForm
            // 
            this.AcceptButton = this.searchButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(511, 195);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.matchCountLabel);
            this.Controls.Add(this.searchScopeComboBox);
            this.Controls.Add(this.searchScopeLabel);
            this.Controls.Add(this.autoSearchCheckBox);
            this.Controls.Add(this.caseSensitiveCheckBox);
            this.Controls.Add(this.regexCheckBox);
            this.Controls.Add(this.searchButton);
            this.Controls.Add(this.searchTextBox);
            this.Controls.Add(this.searchLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.LanguageKey = "editor_obfuscate_text_title";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TextObfuscationForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private GreenshotLabel searchLabel;
        private System.Windows.Forms.TextBox searchTextBox;
        private GreenshotButton searchButton;
        private GreenshotCheckBox regexCheckBox;
        private GreenshotCheckBox caseSensitiveCheckBox;
        private GreenshotCheckBox autoSearchCheckBox;
        private GreenshotLabel searchScopeLabel;
        private System.Windows.Forms.ComboBox searchScopeComboBox;
        private GreenshotLabel matchCountLabel;
        private GreenshotButton applyButton;
        private GreenshotButton cancelButton;
    }
}
