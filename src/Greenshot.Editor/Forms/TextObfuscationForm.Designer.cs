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
            this.mainFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.searchPanel = new System.Windows.Forms.Panel();
            this.searchLabel = new GreenshotLabel();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.searchButton = new GreenshotButton();
            this.checkboxPanel = new System.Windows.Forms.Panel();
            this.regexCheckBox = new GreenshotCheckBox();
            this.caseSensitiveCheckBox = new GreenshotCheckBox();
            this.scopePanel = new System.Windows.Forms.Panel();
            this.searchScopeLabel = new GreenshotLabel();
            this.searchScopeComboBox = new System.Windows.Forms.ComboBox();
            this.matchCountLabel = new GreenshotLabel();
            this.effectPanel = new System.Windows.Forms.Panel();
            this.effectLabel = new GreenshotLabel();
            this.effectComboBox = new System.Windows.Forms.ComboBox();
            this.effectSettingsPanel = new System.Windows.Forms.Panel();
            this.pixelSizeLabel = new GreenshotLabel();
            this.pixelSizeUpDown = new System.Windows.Forms.NumericUpDown();
            this.blurRadiusLabel = new GreenshotLabel();
            this.blurRadiusUpDown = new System.Windows.Forms.NumericUpDown();
            this.highlightColorLabel = new GreenshotLabel();
            this.highlightColorButton = new System.Windows.Forms.Button();
            this.magnificationLabel = new GreenshotLabel();
            this.magnificationUpDown = new System.Windows.Forms.NumericUpDown();
            this.advancedSettingsCheckBox = new GreenshotCheckBox();
            this.advancedSettingsGroupBox = new System.Windows.Forms.GroupBox();
            this.advancedPanel = new System.Windows.Forms.Panel();
            this.paddingHorizontalLabel = new GreenshotLabel();
            this.paddingHorizontalUpDown = new System.Windows.Forms.NumericUpDown();
            this.paddingVerticalLabel = new GreenshotLabel();
            this.paddingVerticalUpDown = new System.Windows.Forms.NumericUpDown();
            this.offsetHorizontalLabel = new GreenshotLabel();
            this.offsetHorizontalUpDown = new System.Windows.Forms.NumericUpDown();
            this.offsetVerticalLabel = new GreenshotLabel();
            this.offsetVerticalUpDown = new System.Windows.Forms.NumericUpDown();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.applyButton = new GreenshotButton();
            this.cancelButton = new GreenshotButton();
            this.mainFlowPanel.SuspendLayout();
            this.searchPanel.SuspendLayout();
            this.checkboxPanel.SuspendLayout();
            this.scopePanel.SuspendLayout();
            this.effectPanel.SuspendLayout();
            this.effectSettingsPanel.SuspendLayout();
            this.advancedSettingsGroupBox.SuspendLayout();
            this.advancedPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pixelSizeUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blurRadiusUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.magnificationUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.paddingHorizontalUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.paddingVerticalUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.offsetHorizontalUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.offsetVerticalUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // mainFlowPanel
            // 
            this.mainFlowPanel.AutoSize = true;
            this.mainFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mainFlowPanel.Controls.Add(this.searchPanel);
            this.mainFlowPanel.Controls.Add(this.checkboxPanel);
            this.mainFlowPanel.Controls.Add(this.scopePanel);
            this.mainFlowPanel.Controls.Add(this.matchCountLabel);
            this.mainFlowPanel.Controls.Add(this.effectPanel);
            this.mainFlowPanel.Controls.Add(this.effectSettingsPanel);
            this.mainFlowPanel.Controls.Add(this.advancedSettingsCheckBox);
            this.mainFlowPanel.Controls.Add(this.advancedSettingsGroupBox);
            this.mainFlowPanel.Controls.Add(this.buttonPanel);
            this.mainFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.mainFlowPanel.Location = new System.Drawing.Point(0, 0);
            this.mainFlowPanel.Name = "mainFlowPanel";
            this.mainFlowPanel.Padding = new System.Windows.Forms.Padding(5);
            this.mainFlowPanel.Size = new System.Drawing.Size(511, 300);
            this.mainFlowPanel.TabIndex = 0;
            this.mainFlowPanel.WrapContents = false;
            // 
            // searchPanel
            // 
            this.searchPanel.AutoSize = true;
            this.searchPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.searchPanel.Controls.Add(this.searchLabel);
            this.searchPanel.Controls.Add(this.searchTextBox);
            this.searchPanel.Controls.Add(this.searchButton);
            this.searchPanel.Location = new System.Drawing.Point(8, 8);
            this.searchPanel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.searchPanel.Name = "searchPanel";
            this.searchPanel.Size = new System.Drawing.Size(495, 26);
            this.searchPanel.TabIndex = 0;
            // 
            // searchLabel
            // 
            this.searchLabel.LanguageKey = "editor_obfuscate_text_search";
            this.searchLabel.Location = new System.Drawing.Point(0, 3);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(100, 20);
            this.searchLabel.TabIndex = 0;
            // 
            // searchTextBox
            // 
            this.searchTextBox.Location = new System.Drawing.Point(106, 0);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(300, 20);
            this.searchTextBox.TabIndex = 1;
            // 
            // searchButton
            // 
            this.searchButton.LanguageKey = "editor_obfuscate_text_search_button";
            this.searchButton.Location = new System.Drawing.Point(412, 0);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(75, 23);
            this.searchButton.TabIndex = 2;
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.SearchButton_Click);
            // 
            // checkboxPanel
            // 
            this.checkboxPanel.AutoSize = true;
            this.checkboxPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.checkboxPanel.Controls.Add(this.regexCheckBox);
            this.checkboxPanel.Controls.Add(this.caseSensitiveCheckBox);
            this.checkboxPanel.Location = new System.Drawing.Point(8, 40);
            this.checkboxPanel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.checkboxPanel.Name = "checkboxPanel";
            this.checkboxPanel.Size = new System.Drawing.Size(495, 24);
            this.checkboxPanel.TabIndex = 1;
            // 
            // regexCheckBox
            // 
            this.regexCheckBox.LanguageKey = "editor_obfuscate_text_regex";
            this.regexCheckBox.Location = new System.Drawing.Point(106, 0);
            this.regexCheckBox.Name = "regexCheckBox";
            this.regexCheckBox.Size = new System.Drawing.Size(150, 24);
            this.regexCheckBox.TabIndex = 0;
            this.regexCheckBox.UseVisualStyleBackColor = true;
            // 
            // caseSensitiveCheckBox
            // 
            this.caseSensitiveCheckBox.LanguageKey = "editor_obfuscate_text_case_sensitive";
            this.caseSensitiveCheckBox.Location = new System.Drawing.Point(262, 0);
            this.caseSensitiveCheckBox.Name = "caseSensitiveCheckBox";
            this.caseSensitiveCheckBox.Size = new System.Drawing.Size(150, 24);
            this.caseSensitiveCheckBox.TabIndex = 1;
            this.caseSensitiveCheckBox.UseVisualStyleBackColor = true;
            // 
            // scopePanel
            // 
            this.scopePanel.AutoSize = true;
            this.scopePanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.scopePanel.Controls.Add(this.searchScopeLabel);
            this.scopePanel.Controls.Add(this.searchScopeComboBox);
            this.scopePanel.Location = new System.Drawing.Point(8, 70);
            this.scopePanel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.scopePanel.Name = "scopePanel";
            this.scopePanel.Size = new System.Drawing.Size(495, 24);
            this.scopePanel.TabIndex = 2;
            // 
            // searchScopeLabel
            // 
            this.searchScopeLabel.LanguageKey = "editor_obfuscate_text_search_scope";
            this.searchScopeLabel.Location = new System.Drawing.Point(0, 2);
            this.searchScopeLabel.Name = "searchScopeLabel";
            this.searchScopeLabel.Size = new System.Drawing.Size(100, 20);
            this.searchScopeLabel.TabIndex = 0;
            // 
            // searchScopeComboBox
            // 
            this.searchScopeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.searchScopeComboBox.FormattingEnabled = true;
            this.searchScopeComboBox.Location = new System.Drawing.Point(106, 0);
            this.searchScopeComboBox.Name = "searchScopeComboBox";
            this.searchScopeComboBox.Size = new System.Drawing.Size(150, 21);
            this.searchScopeComboBox.TabIndex = 1;
            // 
            // matchCountLabel
            // 
            this.matchCountLabel.AutoSize = true;
            this.matchCountLabel.Location = new System.Drawing.Point(8, 100);
            this.matchCountLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.matchCountLabel.Name = "matchCountLabel";
            this.matchCountLabel.Size = new System.Drawing.Size(100, 13);
            this.matchCountLabel.TabIndex = 3;
            this.matchCountLabel.Text = "";
            // 
            // effectPanel
            // 
            this.effectPanel.AutoSize = true;
            this.effectPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.effectPanel.Controls.Add(this.effectLabel);
            this.effectPanel.Controls.Add(this.effectComboBox);
            this.effectPanel.Location = new System.Drawing.Point(8, 119);
            this.effectPanel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.effectPanel.Name = "effectPanel";
            this.effectPanel.Size = new System.Drawing.Size(495, 24);
            this.effectPanel.TabIndex = 4;
            // 
            // effectLabel
            // 
            this.effectLabel.Text = "Effect:";
            this.effectLabel.Location = new System.Drawing.Point(0, 2);
            this.effectLabel.Name = "effectLabel";
            this.effectLabel.Size = new System.Drawing.Size(100, 20);
            this.effectLabel.TabIndex = 0;
            // 
            // effectComboBox
            // 
            this.effectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.effectComboBox.FormattingEnabled = true;
            this.effectComboBox.Location = new System.Drawing.Point(106, 0);
            this.effectComboBox.Name = "effectComboBox";
            this.effectComboBox.Size = new System.Drawing.Size(150, 21);
            this.effectComboBox.TabIndex = 1;
            // 
            // effectSettingsPanel
            // 
            this.effectSettingsPanel.AutoSize = true;
            this.effectSettingsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.effectSettingsPanel.Controls.Add(this.pixelSizeLabel);
            this.effectSettingsPanel.Controls.Add(this.pixelSizeUpDown);
            this.effectSettingsPanel.Controls.Add(this.blurRadiusLabel);
            this.effectSettingsPanel.Controls.Add(this.blurRadiusUpDown);
            this.effectSettingsPanel.Controls.Add(this.highlightColorLabel);
            this.effectSettingsPanel.Controls.Add(this.highlightColorButton);
            this.effectSettingsPanel.Controls.Add(this.magnificationLabel);
            this.effectSettingsPanel.Controls.Add(this.magnificationUpDown);
            this.effectSettingsPanel.Location = new System.Drawing.Point(8, 149);
            this.effectSettingsPanel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.effectSettingsPanel.MinimumSize = new System.Drawing.Size(495, 30);
            this.effectSettingsPanel.Name = "effectSettingsPanel";
            this.effectSettingsPanel.Size = new System.Drawing.Size(495, 30);
            this.effectSettingsPanel.TabIndex = 5;
            // 
            // pixelSizeLabel
            // 
            this.pixelSizeLabel.Text = "Pixel Size:";
            this.pixelSizeLabel.Location = new System.Drawing.Point(0, 5);
            this.pixelSizeLabel.Name = "pixelSizeLabel";
            this.pixelSizeLabel.Size = new System.Drawing.Size(100, 20);
            this.pixelSizeLabel.TabIndex = 0;
            this.pixelSizeLabel.Visible = false;
            // 
            // pixelSizeUpDown
            // 
            this.pixelSizeUpDown.Location = new System.Drawing.Point(106, 3);
            this.pixelSizeUpDown.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.pixelSizeUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.pixelSizeUpDown.Name = "pixelSizeUpDown";
            this.pixelSizeUpDown.Size = new System.Drawing.Size(80, 20);
            this.pixelSizeUpDown.TabIndex = 1;
            this.pixelSizeUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
            this.pixelSizeUpDown.Visible = false;
            // 
            // blurRadiusLabel
            // 
            this.blurRadiusLabel.Text = "Blur Radius:";
            this.blurRadiusLabel.Location = new System.Drawing.Point(0, 5);
            this.blurRadiusLabel.Name = "blurRadiusLabel";
            this.blurRadiusLabel.Size = new System.Drawing.Size(100, 20);
            this.blurRadiusLabel.TabIndex = 2;
            this.blurRadiusLabel.Visible = false;
            // 
            // blurRadiusUpDown
            // 
            this.blurRadiusUpDown.Location = new System.Drawing.Point(106, 3);
            this.blurRadiusUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.blurRadiusUpDown.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            this.blurRadiusUpDown.Name = "blurRadiusUpDown";
            this.blurRadiusUpDown.Size = new System.Drawing.Size(80, 20);
            this.blurRadiusUpDown.TabIndex = 3;
            this.blurRadiusUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
            this.blurRadiusUpDown.Visible = false;
            // 
            // highlightColorLabel
            // 
            this.highlightColorLabel.Text = "Highlight Color:";
            this.highlightColorLabel.Location = new System.Drawing.Point(0, 5);
            this.highlightColorLabel.Name = "highlightColorLabel";
            this.highlightColorLabel.Size = new System.Drawing.Size(100, 20);
            this.highlightColorLabel.TabIndex = 4;
            this.highlightColorLabel.Visible = false;
            // 
            // highlightColorButton
            // 
            this.highlightColorButton.BackColor = System.Drawing.Color.Yellow;
            this.highlightColorButton.Location = new System.Drawing.Point(106, 3);
            this.highlightColorButton.Name = "highlightColorButton";
            this.highlightColorButton.Size = new System.Drawing.Size(80, 23);
            this.highlightColorButton.TabIndex = 5;
            this.highlightColorButton.UseVisualStyleBackColor = false;
            this.highlightColorButton.Visible = false;
            // 
            // magnificationLabel
            // 
            this.magnificationLabel.Text = "Magnification:";
            this.magnificationLabel.Location = new System.Drawing.Point(0, 5);
            this.magnificationLabel.Name = "magnificationLabel";
            this.magnificationLabel.Size = new System.Drawing.Size(100, 20);
            this.magnificationLabel.TabIndex = 6;
            this.magnificationLabel.Visible = false;
            // 
            // magnificationUpDown
            // 
            this.magnificationUpDown.Location = new System.Drawing.Point(106, 3);
            this.magnificationUpDown.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.magnificationUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
            this.magnificationUpDown.Name = "magnificationUpDown";
            this.magnificationUpDown.Size = new System.Drawing.Size(80, 20);
            this.magnificationUpDown.TabIndex = 7;
            this.magnificationUpDown.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.magnificationUpDown.Visible = false;
            // 
            // advancedSettingsCheckBox
            // 
            this.advancedSettingsCheckBox.AutoSize = true;
            this.advancedSettingsCheckBox.LanguageKey = "editor_obfuscate_text_advanced";
            this.advancedSettingsCheckBox.Location = new System.Drawing.Point(8, 185);
            this.advancedSettingsCheckBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.advancedSettingsCheckBox.Name = "advancedSettingsCheckBox";
            this.advancedSettingsCheckBox.Size = new System.Drawing.Size(115, 17);
            this.advancedSettingsCheckBox.TabIndex = 6;
            this.advancedSettingsCheckBox.Text = "Advanced Settings";
            this.advancedSettingsCheckBox.UseVisualStyleBackColor = true;
            this.advancedSettingsCheckBox.CheckedChanged += new System.EventHandler(this.AdvancedSettingsCheckBox_CheckedChanged);
            // 
            // advancedSettingsGroupBox
            // 
            this.advancedSettingsGroupBox.AutoSize = true;
            this.advancedSettingsGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.advancedSettingsGroupBox.Controls.Add(this.advancedPanel);
            this.advancedSettingsGroupBox.Location = new System.Drawing.Point(8, 208);
            this.advancedSettingsGroupBox.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.advancedSettingsGroupBox.Name = "advancedSettingsGroupBox";
            this.advancedSettingsGroupBox.Padding = new System.Windows.Forms.Padding(3, 3, 3, 6);
            this.advancedSettingsGroupBox.Size = new System.Drawing.Size(495, 85);
            this.advancedSettingsGroupBox.TabIndex = 7;
            this.advancedSettingsGroupBox.TabStop = false;
            this.advancedSettingsGroupBox.Text = "Positioning";
            this.advancedSettingsGroupBox.Visible = false;
            // 
            // advancedPanel
            // 
            this.advancedPanel.AutoSize = true;
            this.advancedPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.advancedPanel.Controls.Add(this.paddingHorizontalLabel);
            this.advancedPanel.Controls.Add(this.paddingHorizontalUpDown);
            this.advancedPanel.Controls.Add(this.paddingVerticalLabel);
            this.advancedPanel.Controls.Add(this.paddingVerticalUpDown);
            this.advancedPanel.Controls.Add(this.offsetHorizontalLabel);
            this.advancedPanel.Controls.Add(this.offsetHorizontalUpDown);
            this.advancedPanel.Controls.Add(this.offsetVerticalLabel);
            this.advancedPanel.Controls.Add(this.offsetVerticalUpDown);
            this.advancedPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.advancedPanel.Location = new System.Drawing.Point(3, 16);
            this.advancedPanel.Name = "advancedPanel";
            this.advancedPanel.Size = new System.Drawing.Size(489, 63);
            this.advancedPanel.TabIndex = 0;
            // 
            // paddingHorizontalLabel
            // 
            this.paddingHorizontalLabel.Text = "H-Padding %:";
            this.paddingHorizontalLabel.Location = new System.Drawing.Point(0, 5);
            this.paddingHorizontalLabel.Name = "paddingHorizontalLabel";
            this.paddingHorizontalLabel.Size = new System.Drawing.Size(80, 20);
            this.paddingHorizontalLabel.TabIndex = 0;
            // 
            // paddingHorizontalUpDown
            // 
            this.paddingHorizontalUpDown.Location = new System.Drawing.Point(86, 3);
            this.paddingHorizontalUpDown.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.paddingHorizontalUpDown.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.paddingHorizontalUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            this.paddingHorizontalUpDown.Name = "paddingHorizontalUpDown";
            this.paddingHorizontalUpDown.Size = new System.Drawing.Size(60, 20);
            this.paddingHorizontalUpDown.TabIndex = 1;
            this.paddingHorizontalUpDown.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // paddingVerticalLabel
            // 
            this.paddingVerticalLabel.Text = "V-Padding %:";
            this.paddingVerticalLabel.Location = new System.Drawing.Point(0, 31);
            this.paddingVerticalLabel.Name = "paddingVerticalLabel";
            this.paddingVerticalLabel.Size = new System.Drawing.Size(80, 20);
            this.paddingVerticalLabel.TabIndex = 2;
            // 
            // paddingVerticalUpDown
            // 
            this.paddingVerticalUpDown.Location = new System.Drawing.Point(86, 29);
            this.paddingVerticalUpDown.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.paddingVerticalUpDown.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.paddingVerticalUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            this.paddingVerticalUpDown.Name = "paddingVerticalUpDown";
            this.paddingVerticalUpDown.Size = new System.Drawing.Size(60, 20);
            this.paddingVerticalUpDown.TabIndex = 3;
            this.paddingVerticalUpDown.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // offsetHorizontalLabel
            // 
            this.offsetHorizontalLabel.Text = "H-Offset px:";
            this.offsetHorizontalLabel.Location = new System.Drawing.Point(240, 5);
            this.offsetHorizontalLabel.Name = "offsetHorizontalLabel";
            this.offsetHorizontalLabel.Size = new System.Drawing.Size(80, 20);
            this.offsetHorizontalLabel.TabIndex = 4;
            // 
            // offsetHorizontalUpDown
            // 
            this.offsetHorizontalUpDown.Location = new System.Drawing.Point(326, 3);
            this.offsetHorizontalUpDown.Minimum = new decimal(new int[] { 100, 0, 0, -2147483648 });
            this.offsetHorizontalUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.offsetHorizontalUpDown.Name = "offsetHorizontalUpDown";
            this.offsetHorizontalUpDown.Size = new System.Drawing.Size(60, 20);
            this.offsetHorizontalUpDown.TabIndex = 5;
            this.offsetHorizontalUpDown.Value = new decimal(new int[] { 0, 0, 0, 0 });
            // 
            // offsetVerticalLabel
            // 
            this.offsetVerticalLabel.Text = "V-Offset px:";
            this.offsetVerticalLabel.Location = new System.Drawing.Point(240, 31);
            this.offsetVerticalLabel.Name = "offsetVerticalLabel";
            this.offsetVerticalLabel.Size = new System.Drawing.Size(80, 20);
            this.offsetVerticalLabel.TabIndex = 6;
            // 
            // offsetVerticalUpDown
            // 
            this.offsetVerticalUpDown.Location = new System.Drawing.Point(326, 29);
            this.offsetVerticalUpDown.Minimum = new decimal(new int[] { 100, 0, 0, -2147483648 });
            this.offsetVerticalUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.offsetVerticalUpDown.Name = "offsetVerticalUpDown";
            this.offsetVerticalUpDown.Size = new System.Drawing.Size(60, 20);
            this.offsetVerticalUpDown.TabIndex = 7;
            this.offsetVerticalUpDown.Value = new decimal(new int[] { 5, 0, 0, -2147483648 });
            // 
            // buttonPanel
            // 
            this.buttonPanel.AutoSize = true;
            this.buttonPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonPanel.Controls.Add(this.applyButton);
            this.buttonPanel.Controls.Add(this.cancelButton);
            this.buttonPanel.Location = new System.Drawing.Point(8, 299);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(495, 29);
            this.buttonPanel.TabIndex = 8;
            // 
            // applyButton
            // 
            this.applyButton.LanguageKey = "editor_obfuscate_text_apply";
            this.applyButton.Location = new System.Drawing.Point(330, 0);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 0;
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.ApplyButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.LanguageKey = "CANCEL";
            this.cancelButton.Location = new System.Drawing.Point(411, 0);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // TextObfuscationForm
            // 
            this.AcceptButton = this.applyButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(511, 300);
            this.Controls.Add(this.mainFlowPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.LanguageKey = "editor_obfuscate_text_title";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TextObfuscationForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.mainFlowPanel.ResumeLayout(false);
            this.mainFlowPanel.PerformLayout();
            this.searchPanel.ResumeLayout(false);
            this.searchPanel.PerformLayout();
            this.checkboxPanel.ResumeLayout(false);
            this.scopePanel.ResumeLayout(false);
            this.effectPanel.ResumeLayout(false);
            this.effectSettingsPanel.ResumeLayout(false);
            this.advancedSettingsGroupBox.ResumeLayout(false);
            this.advancedSettingsGroupBox.PerformLayout();
            this.advancedPanel.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pixelSizeUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blurRadiusUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.magnificationUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.paddingHorizontalUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.paddingVerticalUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.offsetHorizontalUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.offsetVerticalUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel mainFlowPanel;
        private System.Windows.Forms.Panel searchPanel;
        private GreenshotLabel searchLabel;
        private System.Windows.Forms.TextBox searchTextBox;
        private GreenshotButton searchButton;
        private System.Windows.Forms.Panel checkboxPanel;
        private GreenshotCheckBox regexCheckBox;
        private GreenshotCheckBox caseSensitiveCheckBox;
        private System.Windows.Forms.Panel scopePanel;
        private GreenshotLabel searchScopeLabel;
        private System.Windows.Forms.ComboBox searchScopeComboBox;
        private GreenshotLabel matchCountLabel;
        private System.Windows.Forms.Panel effectPanel;
        private GreenshotLabel effectLabel;
        private System.Windows.Forms.ComboBox effectComboBox;
        private System.Windows.Forms.Panel effectSettingsPanel;
        private GreenshotLabel pixelSizeLabel;
        private System.Windows.Forms.NumericUpDown pixelSizeUpDown;
        private GreenshotLabel blurRadiusLabel;
        private System.Windows.Forms.NumericUpDown blurRadiusUpDown;
        private GreenshotLabel highlightColorLabel;
        private System.Windows.Forms.Button highlightColorButton;
        private GreenshotLabel magnificationLabel;
        private System.Windows.Forms.NumericUpDown magnificationUpDown;
        private GreenshotCheckBox advancedSettingsCheckBox;
        private System.Windows.Forms.GroupBox advancedSettingsGroupBox;
        private System.Windows.Forms.Panel advancedPanel;
        private GreenshotLabel paddingHorizontalLabel;
        private System.Windows.Forms.NumericUpDown paddingHorizontalUpDown;
        private GreenshotLabel paddingVerticalLabel;
        private System.Windows.Forms.NumericUpDown paddingVerticalUpDown;
        private GreenshotLabel offsetHorizontalLabel;
        private System.Windows.Forms.NumericUpDown offsetHorizontalUpDown;
        private GreenshotLabel offsetVerticalLabel;
        private System.Windows.Forms.NumericUpDown offsetVerticalUpDown;
        private System.Windows.Forms.Panel buttonPanel;
        private GreenshotButton applyButton;
        private GreenshotButton cancelButton;
    }
}
