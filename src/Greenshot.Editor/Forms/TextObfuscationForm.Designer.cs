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
            this.searchScopeLabel = new GreenshotLabel();
            this.searchScopeComboBox = new System.Windows.Forms.ComboBox();
            this.matchCountLabel = new GreenshotLabel();
            this.effectLabel = new GreenshotLabel();
            this.effectComboBox = new System.Windows.Forms.ComboBox();
            this.pixelSizeLabel = new GreenshotLabel();
            this.pixelSizeUpDown = new System.Windows.Forms.NumericUpDown();
            this.blurRadiusLabel = new GreenshotLabel();
            this.blurRadiusUpDown = new System.Windows.Forms.NumericUpDown();
            this.highlightColorLabel = new GreenshotLabel();
            this.highlightColorButton = new System.Windows.Forms.Button();
            this.magnificationLabel = new GreenshotLabel();
            this.magnificationUpDown = new System.Windows.Forms.NumericUpDown();
            this.paddingHorizontalLabel = new GreenshotLabel();
            this.paddingHorizontalUpDown = new System.Windows.Forms.NumericUpDown();
            this.paddingVerticalLabel = new GreenshotLabel();
            this.paddingVerticalUpDown = new System.Windows.Forms.NumericUpDown();
            this.applyButton = new GreenshotButton();
            this.cancelButton = new GreenshotButton();
            ((System.ComponentModel.ISupportInitialize)(this.pixelSizeUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blurRadiusUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.magnificationUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.paddingHorizontalUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.paddingVerticalUpDown)).BeginInit();
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
            // searchScopeLabel
            // 
            this.searchScopeLabel.LanguageKey = "editor_obfuscate_text_search_scope";
            this.searchScopeLabel.Location = new System.Drawing.Point(12, 70);
            this.searchScopeLabel.Name = "searchScopeLabel";
            this.searchScopeLabel.Size = new System.Drawing.Size(100, 20);
            this.searchScopeLabel.TabIndex = 5;
            // 
            // searchScopeComboBox
            // 
            this.searchScopeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.searchScopeComboBox.FormattingEnabled = true;
            this.searchScopeComboBox.Location = new System.Drawing.Point(118, 67);
            this.searchScopeComboBox.Name = "searchScopeComboBox";
            this.searchScopeComboBox.Size = new System.Drawing.Size(150, 21);
            this.searchScopeComboBox.TabIndex = 6;
            // 
            // matchCountLabel
            // 
            this.matchCountLabel.LanguageKey = "editor_obfuscate_text_matches";
            this.matchCountLabel.Location = new System.Drawing.Point(12, 100);
            this.matchCountLabel.Name = "matchCountLabel";
            this.matchCountLabel.Size = new System.Drawing.Size(487, 20);
            this.matchCountLabel.TabIndex = 7;
            // 
            // effectLabel
            // 
            this.effectLabel.Text = "Effect:";
            this.effectLabel.Location = new System.Drawing.Point(12, 130);
            this.effectLabel.Name = "effectLabel";
            this.effectLabel.Size = new System.Drawing.Size(100, 20);
            this.effectLabel.TabIndex = 8;
            // 
            // effectComboBox
            // 
            this.effectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.effectComboBox.FormattingEnabled = true;
            this.effectComboBox.Location = new System.Drawing.Point(118, 127);
            this.effectComboBox.Name = "effectComboBox";
            this.effectComboBox.Size = new System.Drawing.Size(150, 21);
            this.effectComboBox.TabIndex = 9;
            // 
            // pixelSizeLabel
            // 
            this.pixelSizeLabel.Text = "Pixel Size:";
            this.pixelSizeLabel.Location = new System.Drawing.Point(12, 160);
            this.pixelSizeLabel.Name = "pixelSizeLabel";
            this.pixelSizeLabel.Size = new System.Drawing.Size(100, 20);
            this.pixelSizeLabel.TabIndex = 10;
            this.pixelSizeLabel.Visible = false;
            // 
            // pixelSizeUpDown
            // 
            this.pixelSizeUpDown.Location = new System.Drawing.Point(118, 158);
            this.pixelSizeUpDown.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.pixelSizeUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.pixelSizeUpDown.Name = "pixelSizeUpDown";
            this.pixelSizeUpDown.Size = new System.Drawing.Size(80, 20);
            this.pixelSizeUpDown.TabIndex = 11;
            this.pixelSizeUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
            this.pixelSizeUpDown.Visible = false;
            // 
            // blurRadiusLabel
            // 
            this.blurRadiusLabel.Text = "Blur Radius:";
            this.blurRadiusLabel.Location = new System.Drawing.Point(12, 160);
            this.blurRadiusLabel.Name = "blurRadiusLabel";
            this.blurRadiusLabel.Size = new System.Drawing.Size(100, 20);
            this.blurRadiusLabel.TabIndex = 12;
            this.blurRadiusLabel.Visible = false;
            // 
            // blurRadiusUpDown
            // 
            this.blurRadiusUpDown.Location = new System.Drawing.Point(118, 158);
            this.blurRadiusUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.blurRadiusUpDown.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            this.blurRadiusUpDown.Name = "blurRadiusUpDown";
            this.blurRadiusUpDown.Size = new System.Drawing.Size(80, 20);
            this.blurRadiusUpDown.TabIndex = 13;
            this.blurRadiusUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
            this.blurRadiusUpDown.Visible = false;
            // 
            // highlightColorLabel
            // 
            this.highlightColorLabel.Text = "Highlight Color:";
            this.highlightColorLabel.Location = new System.Drawing.Point(12, 160);
            this.highlightColorLabel.Name = "highlightColorLabel";
            this.highlightColorLabel.Size = new System.Drawing.Size(100, 20);
            this.highlightColorLabel.TabIndex = 14;
            this.highlightColorLabel.Visible = false;
            // 
            // highlightColorButton
            // 
            this.highlightColorButton.BackColor = System.Drawing.Color.Yellow;
            this.highlightColorButton.Location = new System.Drawing.Point(118, 158);
            this.highlightColorButton.Name = "highlightColorButton";
            this.highlightColorButton.Size = new System.Drawing.Size(80, 23);
            this.highlightColorButton.TabIndex = 15;
            this.highlightColorButton.UseVisualStyleBackColor = false;
            this.highlightColorButton.Visible = false;
            // 
            // magnificationLabel
            // 
            this.magnificationLabel.Text = "Magnification:";
            this.magnificationLabel.Location = new System.Drawing.Point(12, 160);
            this.magnificationLabel.Name = "magnificationLabel";
            this.magnificationLabel.Size = new System.Drawing.Size(100, 20);
            this.magnificationLabel.TabIndex = 16;
            this.magnificationLabel.Visible = false;
            // 
            // magnificationUpDown
            // 
            this.magnificationUpDown.Location = new System.Drawing.Point(118, 158);
            this.magnificationUpDown.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.magnificationUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
            this.magnificationUpDown.Name = "magnificationUpDown";
            this.magnificationUpDown.Size = new System.Drawing.Size(80, 20);
            this.magnificationUpDown.TabIndex = 17;
            this.magnificationUpDown.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.magnificationUpDown.Visible = false;
            // 
            // paddingHorizontalLabel
            // 
            this.paddingHorizontalLabel.Text = "H-Padding %:";
            this.paddingHorizontalLabel.Location = new System.Drawing.Point(280, 130);
            this.paddingHorizontalLabel.Name = "paddingHorizontalLabel";
            this.paddingHorizontalLabel.Size = new System.Drawing.Size(80, 20);
            this.paddingHorizontalLabel.TabIndex = 20;
            // 
            // paddingHorizontalUpDown
            // 
            this.paddingHorizontalUpDown.Location = new System.Drawing.Point(366, 128);
            this.paddingHorizontalUpDown.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.paddingHorizontalUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.paddingHorizontalUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            this.paddingHorizontalUpDown.Name = "paddingHorizontalUpDown";
            this.paddingHorizontalUpDown.Size = new System.Drawing.Size(60, 20);
            this.paddingHorizontalUpDown.TabIndex = 21;
            this.paddingHorizontalUpDown.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // paddingVerticalLabel
            // 
            this.paddingVerticalLabel.Text = "V-Padding %:";
            this.paddingVerticalLabel.Location = new System.Drawing.Point(280, 156);
            this.paddingVerticalLabel.Name = "paddingVerticalLabel";
            this.paddingVerticalLabel.Size = new System.Drawing.Size(80, 20);
            this.paddingVerticalLabel.TabIndex = 22;
            // 
            // paddingVerticalUpDown
            // 
            this.paddingVerticalUpDown.Location = new System.Drawing.Point(366, 154);
            this.paddingVerticalUpDown.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.paddingVerticalUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.paddingVerticalUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            this.paddingVerticalUpDown.Name = "paddingVerticalUpDown";
            this.paddingVerticalUpDown.Size = new System.Drawing.Size(60, 20);
            this.paddingVerticalUpDown.TabIndex = 23;
            this.paddingVerticalUpDown.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // applyButton
            // 
            this.applyButton.LanguageKey = "editor_obfuscate_text_apply";
            this.applyButton.Location = new System.Drawing.Point(343, 190);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 18;
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.ApplyButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.LanguageKey = "CANCEL";
            this.cancelButton.Location = new System.Drawing.Point(424, 190);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 19;
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // TextObfuscationForm
            // 
            this.AcceptButton = this.applyButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(511, 225);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.paddingVerticalUpDown);
            this.Controls.Add(this.paddingVerticalLabel);
            this.Controls.Add(this.paddingHorizontalUpDown);
            this.Controls.Add(this.paddingHorizontalLabel);
            this.Controls.Add(this.magnificationUpDown);
            this.Controls.Add(this.magnificationLabel);
            this.Controls.Add(this.highlightColorButton);
            this.Controls.Add(this.highlightColorLabel);
            this.Controls.Add(this.blurRadiusUpDown);
            this.Controls.Add(this.blurRadiusLabel);
            this.Controls.Add(this.pixelSizeUpDown);
            this.Controls.Add(this.pixelSizeLabel);
            this.Controls.Add(this.effectComboBox);
            this.Controls.Add(this.effectLabel);
            this.Controls.Add(this.matchCountLabel);
            this.Controls.Add(this.searchScopeComboBox);
            this.Controls.Add(this.searchScopeLabel);
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
            ((System.ComponentModel.ISupportInitialize)(this.pixelSizeUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blurRadiusUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.magnificationUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.paddingHorizontalUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.paddingVerticalUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private GreenshotLabel searchLabel;
        private System.Windows.Forms.TextBox searchTextBox;
        private GreenshotButton searchButton;
        private GreenshotCheckBox regexCheckBox;
        private GreenshotCheckBox caseSensitiveCheckBox;
        private GreenshotLabel searchScopeLabel;
        private System.Windows.Forms.ComboBox searchScopeComboBox;
        private GreenshotLabel matchCountLabel;
        private GreenshotLabel effectLabel;
        private System.Windows.Forms.ComboBox effectComboBox;
        private GreenshotLabel pixelSizeLabel;
        private System.Windows.Forms.NumericUpDown pixelSizeUpDown;
        private GreenshotLabel blurRadiusLabel;
        private System.Windows.Forms.NumericUpDown blurRadiusUpDown;
        private GreenshotLabel highlightColorLabel;
        private System.Windows.Forms.Button highlightColorButton;
        private GreenshotLabel magnificationLabel;
        private System.Windows.Forms.NumericUpDown magnificationUpDown;
        private GreenshotLabel paddingHorizontalLabel;
        private System.Windows.Forms.NumericUpDown paddingHorizontalUpDown;
        private GreenshotLabel paddingVerticalLabel;
        private System.Windows.Forms.NumericUpDown paddingVerticalUpDown;
        private GreenshotButton applyButton;
        private GreenshotButton cancelButton;
    }
}
