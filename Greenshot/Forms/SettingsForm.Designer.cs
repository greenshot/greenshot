﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
namespace Greenshot {
	partial class SettingsForm {
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.textbox_storagelocation = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.label_storagelocation = new GreenshotPlugin.Controls.GreenshotLabel();
            this.settings_cancel = new GreenshotPlugin.Controls.GreenshotButton();
            this.settings_confirm = new GreenshotPlugin.Controls.GreenshotButton();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.browse = new System.Windows.Forms.Button();
            this.label_screenshotname = new GreenshotPlugin.Controls.GreenshotLabel();
            this.textbox_screenshotname = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.label_language = new GreenshotPlugin.Controls.GreenshotLabel();
            this.combobox_language = new System.Windows.Forms.ComboBox();
            this.combobox_primaryimageformat = new GreenshotPlugin.Controls.GreenshotComboBox();
            this.label_primaryimageformat = new GreenshotPlugin.Controls.GreenshotLabel();
            this.groupbox_preferredfilesettings = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.btnPatternHelp = new System.Windows.Forms.Button();
            this.checkbox_copypathtoclipboard = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_zoomer = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.groupbox_applicationsettings = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.numericUpdownIconSize = new System.Windows.Forms.NumericUpDown();
            this.label_icon_size = new GreenshotPlugin.Controls.GreenshotLabel();
            this.checkbox_autostartshortcut = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.groupbox_qualitysettings = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.checkbox_reducecolors = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_alwaysshowqualitydialog = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.label_jpegquality = new GreenshotPlugin.Controls.GreenshotLabel();
            this.textBoxJpegQuality = new System.Windows.Forms.TextBox();
            this.trackBarJpegQuality = new System.Windows.Forms.TrackBar();
            this.groupbox_destination = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.checkbox_picker = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.listview_destinations = new System.Windows.Forms.ListView();
            this.destination = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabcontrol = new System.Windows.Forms.TabControl();
            this.tab_general = new GreenshotPlugin.Controls.GreenshotTabPage();
            this.groupbox_network = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.numericUpDown_daysbetweencheck = new System.Windows.Forms.NumericUpDown();
            this.label_checkperiod = new GreenshotPlugin.Controls.GreenshotLabel();
            this.checkbox_usedefaultproxy = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.groupbox_hotkeys = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.label_lastregion_hotkey = new GreenshotPlugin.Controls.GreenshotLabel();
            this.lastregion_hotkeyControl = new GreenshotPlugin.Controls.HotkeyControl();
            this.label_ie_hotkey = new GreenshotPlugin.Controls.GreenshotLabel();
            this.ie_hotkeyControl = new GreenshotPlugin.Controls.HotkeyControl();
            this.label_region_hotkey = new GreenshotPlugin.Controls.GreenshotLabel();
            this.label_window_hotkey = new GreenshotPlugin.Controls.GreenshotLabel();
            this.label_fullscreen_hotkey = new GreenshotPlugin.Controls.GreenshotLabel();
            this.region_hotkeyControl = new GreenshotPlugin.Controls.HotkeyControl();
            this.window_hotkeyControl = new GreenshotPlugin.Controls.HotkeyControl();
            this.fullscreen_hotkeyControl = new GreenshotPlugin.Controls.HotkeyControl();
            this.tab_capture = new GreenshotPlugin.Controls.GreenshotTabPage();
            this.groupbox_editor = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.checkbox_editor_match_capture_size = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.groupbox_iecapture = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.checkbox_ie_capture = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.groupbox_windowscapture = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.colorButton_window_background = new Greenshot.Controls.ColorButton();
            this.radiobuttonWindowCapture = new GreenshotPlugin.Controls.GreenshotRadioButton();
            this.radiobuttonInteractiveCapture = new GreenshotPlugin.Controls.GreenshotRadioButton();
            this.combobox_window_capture_mode = new System.Windows.Forms.ComboBox();
            this.groupbox_capture = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.checkbox_notifications = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_playsound = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_capture_mousepointer = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.numericUpDownWaitTime = new System.Windows.Forms.NumericUpDown();
            this.label_waittime = new GreenshotPlugin.Controls.GreenshotLabel();
            this.tab_output = new GreenshotPlugin.Controls.GreenshotTabPage();
            this.tab_destinations = new GreenshotPlugin.Controls.GreenshotTabPage();
            this.groupDefaultEffects = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.greenshotLabel3 = new GreenshotPlugin.Controls.GreenshotLabel();
            this.greenshotLabel2 = new GreenshotPlugin.Controls.GreenshotLabel();
            this.btnEffectBorderColor = new Greenshot.Controls.ColorButton();
            this.greenshotLabel1 = new GreenshotPlugin.Controls.GreenshotLabel();
            this.txtEffectBorderWidth = new System.Windows.Forms.NumericUpDown();
            this.tab_printer = new GreenshotPlugin.Controls.GreenshotTabPage();
            this.groupBoxColors = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.checkboxPrintInverted = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.radioBtnColorPrint = new GreenshotPlugin.Controls.GreenshotRadioButton();
            this.radioBtnGrayScale = new GreenshotPlugin.Controls.GreenshotRadioButton();
            this.radioBtnMonochrome = new GreenshotPlugin.Controls.GreenshotRadioButton();
            this.groupBoxPrintLayout = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.checkboxDateTime = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkboxAllowShrink = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkboxAllowEnlarge = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkboxAllowRotate = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkboxAllowCenter = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_alwaysshowprintoptionsdialog = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.tab_plugins = new GreenshotPlugin.Controls.GreenshotTabPage();
            this.groupbox_plugins = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.listview_plugins = new System.Windows.Forms.ListView();
            this.button_pluginconfigure = new GreenshotPlugin.Controls.GreenshotButton();
            this.tab_expert = new GreenshotPlugin.Controls.GreenshotTabPage();
            this.groupbox_expert = new GreenshotPlugin.Controls.GreenshotGroupBox();
            this.checkbox_reuseeditor = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_minimizememoryfootprint = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_checkunstableupdates = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_suppresssavedialogatclose = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.label_counter = new GreenshotPlugin.Controls.GreenshotLabel();
            this.textbox_counter = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.label_footerpattern = new GreenshotPlugin.Controls.GreenshotLabel();
            this.textbox_footerpattern = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.checkbox_thumbnailpreview = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_optimizeforrdp = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.checkbox_autoreducecolors = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.label_clipboardformats = new GreenshotPlugin.Controls.GreenshotLabel();
            this.checkbox_enableexpert = new GreenshotPlugin.Controls.GreenshotCheckBox();
            this.listview_clipboardformats = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupbox_preferredfilesettings.SuspendLayout();
            this.groupbox_applicationsettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpdownIconSize)).BeginInit();
            this.groupbox_qualitysettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarJpegQuality)).BeginInit();
            this.groupbox_destination.SuspendLayout();
            this.tabcontrol.SuspendLayout();
            this.tab_general.SuspendLayout();
            this.groupbox_network.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_daysbetweencheck)).BeginInit();
            this.groupbox_hotkeys.SuspendLayout();
            this.tab_capture.SuspendLayout();
            this.groupbox_editor.SuspendLayout();
            this.groupbox_iecapture.SuspendLayout();
            this.groupbox_windowscapture.SuspendLayout();
            this.groupbox_capture.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownWaitTime)).BeginInit();
            this.tab_output.SuspendLayout();
            this.tab_destinations.SuspendLayout();
            this.groupDefaultEffects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtEffectBorderWidth)).BeginInit();
            this.tab_printer.SuspendLayout();
            this.groupBoxColors.SuspendLayout();
            this.groupBoxPrintLayout.SuspendLayout();
            this.tab_plugins.SuspendLayout();
            this.groupbox_plugins.SuspendLayout();
            this.tab_expert.SuspendLayout();
            this.groupbox_expert.SuspendLayout();
            this.SuspendLayout();
            // 
            // textbox_storagelocation
            // 
            this.textbox_storagelocation.Location = new System.Drawing.Point(172, 22);
            this.textbox_storagelocation.Margin = new System.Windows.Forms.Padding(4);
            this.textbox_storagelocation.Name = "textbox_storagelocation";
            this.textbox_storagelocation.Size = new System.Drawing.Size(290, 25);
            this.textbox_storagelocation.TabIndex = 1;
            this.textbox_storagelocation.TextChanged += new System.EventHandler(this.StorageLocationChanged);
            // 
            // label_storagelocation
            // 
            this.label_storagelocation.LanguageKey = "settings_storagelocation";
            this.label_storagelocation.Location = new System.Drawing.Point(8, 26);
            this.label_storagelocation.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_storagelocation.Name = "label_storagelocation";
            this.label_storagelocation.Size = new System.Drawing.Size(158, 29);
            this.label_storagelocation.TabIndex = 11;
            this.label_storagelocation.Text = "Storage location";
            // 
            // settings_cancel
            // 
            this.settings_cancel.LanguageKey = "CANCEL";
            this.settings_cancel.Location = new System.Drawing.Point(455, 495);
            this.settings_cancel.Margin = new System.Windows.Forms.Padding(4);
            this.settings_cancel.Name = "settings_cancel";
            this.settings_cancel.Size = new System.Drawing.Size(94, 29);
            this.settings_cancel.TabIndex = 21;
            this.settings_cancel.Text = "Cancel";
            this.settings_cancel.UseVisualStyleBackColor = true;
            this.settings_cancel.Click += new System.EventHandler(this.Settings_cancelClick);
            // 
            // settings_confirm
            // 
            this.settings_confirm.LanguageKey = "OK";
            this.settings_confirm.Location = new System.Drawing.Point(354, 495);
            this.settings_confirm.Margin = new System.Windows.Forms.Padding(4);
            this.settings_confirm.Name = "settings_confirm";
            this.settings_confirm.Size = new System.Drawing.Size(94, 29);
            this.settings_confirm.TabIndex = 20;
            this.settings_confirm.Text = "Ok";
            this.settings_confirm.UseVisualStyleBackColor = true;
            this.settings_confirm.Click += new System.EventHandler(this.Settings_okayClick);
            // 
            // browse
            // 
            this.browse.Location = new System.Drawing.Point(464, 21);
            this.browse.Margin = new System.Windows.Forms.Padding(4);
            this.browse.Name = "browse";
            this.browse.Size = new System.Drawing.Size(44, 29);
            this.browse.TabIndex = 2;
            this.browse.Text = "...";
            this.browse.UseVisualStyleBackColor = true;
            this.browse.Click += new System.EventHandler(this.BrowseClick);
            // 
            // label_screenshotname
            // 
            this.label_screenshotname.LanguageKey = "settings_filenamepattern";
            this.label_screenshotname.Location = new System.Drawing.Point(8, 55);
            this.label_screenshotname.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_screenshotname.Name = "label_screenshotname";
            this.label_screenshotname.Size = new System.Drawing.Size(158, 29);
            this.label_screenshotname.TabIndex = 9;
            this.label_screenshotname.Text = "Filename pattern";
            // 
            // textbox_screenshotname
            // 
            this.textbox_screenshotname.Location = new System.Drawing.Point(172, 51);
            this.textbox_screenshotname.Margin = new System.Windows.Forms.Padding(4);
            this.textbox_screenshotname.Name = "textbox_screenshotname";
            this.textbox_screenshotname.PropertyName = "OutputFileFilenamePattern";
            this.textbox_screenshotname.Size = new System.Drawing.Size(290, 25);
            this.textbox_screenshotname.TabIndex = 3;
            this.textbox_screenshotname.TextChanged += new System.EventHandler(this.FilenamePatternChanged);
            // 
            // label_language
            // 
            this.label_language.LanguageKey = "settings_language";
            this.label_language.Location = new System.Drawing.Point(8, 25);
            this.label_language.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_language.Name = "label_language";
            this.label_language.Size = new System.Drawing.Size(226, 29);
            this.label_language.TabIndex = 10;
            this.label_language.Text = "Language";
            // 
            // combobox_language
            // 
            this.combobox_language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combobox_language.FormattingEnabled = true;
            this.combobox_language.Location = new System.Drawing.Point(241, 21);
            this.combobox_language.Margin = new System.Windows.Forms.Padding(4);
            this.combobox_language.MaxDropDownItems = 15;
            this.combobox_language.Name = "combobox_language";
            this.combobox_language.Size = new System.Drawing.Size(262, 23);
            this.combobox_language.TabIndex = 0;
            // 
            // combobox_primaryimageformat
            // 
            this.combobox_primaryimageformat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combobox_primaryimageformat.FormattingEnabled = true;
            this.combobox_primaryimageformat.Location = new System.Drawing.Point(172, 80);
            this.combobox_primaryimageformat.Margin = new System.Windows.Forms.Padding(4);
            this.combobox_primaryimageformat.Name = "combobox_primaryimageformat";
            this.combobox_primaryimageformat.PropertyName = "OutputFileFormat";
            this.combobox_primaryimageformat.Size = new System.Drawing.Size(334, 23);
            this.combobox_primaryimageformat.TabIndex = 5;
            // 
            // label_primaryimageformat
            // 
            this.label_primaryimageformat.LanguageKey = "settings_primaryimageformat";
            this.label_primaryimageformat.Location = new System.Drawing.Point(8, 84);
            this.label_primaryimageformat.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_primaryimageformat.Name = "label_primaryimageformat";
            this.label_primaryimageformat.Size = new System.Drawing.Size(158, 24);
            this.label_primaryimageformat.TabIndex = 8;
            this.label_primaryimageformat.Text = "Image format";
            // 
            // groupbox_preferredfilesettings
            // 
            this.groupbox_preferredfilesettings.Controls.Add(this.btnPatternHelp);
            this.groupbox_preferredfilesettings.Controls.Add(this.checkbox_copypathtoclipboard);
            this.groupbox_preferredfilesettings.Controls.Add(this.combobox_primaryimageformat);
            this.groupbox_preferredfilesettings.Controls.Add(this.label_primaryimageformat);
            this.groupbox_preferredfilesettings.Controls.Add(this.label_storagelocation);
            this.groupbox_preferredfilesettings.Controls.Add(this.browse);
            this.groupbox_preferredfilesettings.Controls.Add(this.textbox_storagelocation);
            this.groupbox_preferredfilesettings.Controls.Add(this.textbox_screenshotname);
            this.groupbox_preferredfilesettings.Controls.Add(this.label_screenshotname);
            this.groupbox_preferredfilesettings.LanguageKey = "settings_preferredfilesettings";
            this.groupbox_preferredfilesettings.Location = new System.Drawing.Point(2, 8);
            this.groupbox_preferredfilesettings.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_preferredfilesettings.Name = "groupbox_preferredfilesettings";
            this.groupbox_preferredfilesettings.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_preferredfilesettings.Size = new System.Drawing.Size(515, 152);
            this.groupbox_preferredfilesettings.TabIndex = 13;
            this.groupbox_preferredfilesettings.TabStop = false;
            this.groupbox_preferredfilesettings.Text = "Preferred Output File Settings";
            // 
            // btnPatternHelp
            // 
            this.btnPatternHelp.Location = new System.Drawing.Point(464, 49);
            this.btnPatternHelp.Margin = new System.Windows.Forms.Padding(4);
            this.btnPatternHelp.Name = "btnPatternHelp";
            this.btnPatternHelp.Size = new System.Drawing.Size(44, 29);
            this.btnPatternHelp.TabIndex = 4;
            this.btnPatternHelp.Text = "?";
            this.btnPatternHelp.UseVisualStyleBackColor = true;
            this.btnPatternHelp.Click += new System.EventHandler(this.BtnPatternHelpClick);
            // 
            // checkbox_copypathtoclipboard
            // 
            this.checkbox_copypathtoclipboard.LanguageKey = "settings_copypathtoclipboard";
            this.checkbox_copypathtoclipboard.Location = new System.Drawing.Point(15, 111);
            this.checkbox_copypathtoclipboard.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_copypathtoclipboard.Name = "checkbox_copypathtoclipboard";
            this.checkbox_copypathtoclipboard.PropertyName = "OutputFileCopyPathToClipboard";
            this.checkbox_copypathtoclipboard.Size = new System.Drawing.Size(492, 30);
            this.checkbox_copypathtoclipboard.TabIndex = 6;
            this.checkbox_copypathtoclipboard.Text = "Copy file path to clipboard every time an image is saved";
            this.checkbox_copypathtoclipboard.UseVisualStyleBackColor = true;
            // 
            // checkbox_zoomer
            // 
            this.checkbox_zoomer.LanguageKey = "settings_zoom";
            this.checkbox_zoomer.Location = new System.Drawing.Point(14, 99);
            this.checkbox_zoomer.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_zoomer.Name = "checkbox_zoomer";
            this.checkbox_zoomer.PropertyName = "ZoomerEnabled";
            this.checkbox_zoomer.Size = new System.Drawing.Size(499, 30);
            this.checkbox_zoomer.TabIndex = 4;
            this.checkbox_zoomer.Text = "Show magnifier";
            this.checkbox_zoomer.UseVisualStyleBackColor = true;
            // 
            // groupbox_applicationsettings
            // 
            this.groupbox_applicationsettings.Controls.Add(this.label_language);
            this.groupbox_applicationsettings.Controls.Add(this.combobox_language);
            this.groupbox_applicationsettings.Controls.Add(this.numericUpdownIconSize);
            this.groupbox_applicationsettings.Controls.Add(this.label_icon_size);
            this.groupbox_applicationsettings.Controls.Add(this.checkbox_autostartshortcut);
            this.groupbox_applicationsettings.LanguageKey = "settings_applicationsettings";
            this.groupbox_applicationsettings.Location = new System.Drawing.Point(2, 8);
            this.groupbox_applicationsettings.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_applicationsettings.Name = "groupbox_applicationsettings";
            this.groupbox_applicationsettings.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_applicationsettings.Size = new System.Drawing.Size(515, 111);
            this.groupbox_applicationsettings.TabIndex = 14;
            this.groupbox_applicationsettings.TabStop = false;
            this.groupbox_applicationsettings.Text = "Application Settings";
            // 
            // numericUpdownIconSize
            // 
            this.numericUpdownIconSize.Increment = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpdownIconSize.Location = new System.Drawing.Point(449, 52);
            this.numericUpdownIconSize.Margin = new System.Windows.Forms.Padding(4);
            this.numericUpdownIconSize.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.numericUpdownIconSize.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpdownIconSize.Name = "numericUpdownIconSize";
            this.numericUpdownIconSize.Size = new System.Drawing.Size(55, 25);
            this.numericUpdownIconSize.TabIndex = 1;
            this.numericUpdownIconSize.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
            // 
            // label_icon_size
            // 
            this.label_icon_size.LanguageKey = "settings_iconsize";
            this.label_icon_size.Location = new System.Drawing.Point(8, 55);
            this.label_icon_size.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_icon_size.Name = "label_icon_size";
            this.label_icon_size.Size = new System.Drawing.Size(438, 20);
            this.label_icon_size.TabIndex = 6;
            this.label_icon_size.Text = "Icon size";
            // 
            // checkbox_autostartshortcut
            // 
            this.checkbox_autostartshortcut.LanguageKey = "settings_autostartshortcut";
            this.checkbox_autostartshortcut.Location = new System.Drawing.Point(10, 75);
            this.checkbox_autostartshortcut.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_autostartshortcut.Name = "checkbox_autostartshortcut";
            this.checkbox_autostartshortcut.Size = new System.Drawing.Size(496, 31);
            this.checkbox_autostartshortcut.TabIndex = 2;
            this.checkbox_autostartshortcut.Text = "Launch Greenshot on startup";
            this.checkbox_autostartshortcut.UseVisualStyleBackColor = true;
            // 
            // groupbox_qualitysettings
            // 
            this.groupbox_qualitysettings.Controls.Add(this.checkbox_reducecolors);
            this.groupbox_qualitysettings.Controls.Add(this.checkbox_alwaysshowqualitydialog);
            this.groupbox_qualitysettings.Controls.Add(this.label_jpegquality);
            this.groupbox_qualitysettings.Controls.Add(this.textBoxJpegQuality);
            this.groupbox_qualitysettings.Controls.Add(this.trackBarJpegQuality);
            this.groupbox_qualitysettings.LanguageKey = "settings_qualitysettings";
            this.groupbox_qualitysettings.Location = new System.Drawing.Point(2, 169);
            this.groupbox_qualitysettings.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_qualitysettings.Name = "groupbox_qualitysettings";
            this.groupbox_qualitysettings.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_qualitysettings.Size = new System.Drawing.Size(515, 132);
            this.groupbox_qualitysettings.TabIndex = 14;
            this.groupbox_qualitysettings.TabStop = false;
            this.groupbox_qualitysettings.Text = "Quality settings";
            // 
            // checkbox_reducecolors
            // 
            this.checkbox_reducecolors.LanguageKey = "settings_reducecolors";
            this.checkbox_reducecolors.Location = new System.Drawing.Point(15, 90);
            this.checkbox_reducecolors.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_reducecolors.Name = "checkbox_reducecolors";
            this.checkbox_reducecolors.PropertyName = "OutputFileReduceColors";
            this.checkbox_reducecolors.Size = new System.Drawing.Size(492, 31);
            this.checkbox_reducecolors.TabIndex = 10;
            this.checkbox_reducecolors.Text = "Reduce the amount of colors to a maximum of 256";
            this.checkbox_reducecolors.UseVisualStyleBackColor = true;
            // 
            // checkbox_alwaysshowqualitydialog
            // 
            this.checkbox_alwaysshowqualitydialog.LanguageKey = "settings_alwaysshowqualitydialog";
            this.checkbox_alwaysshowqualitydialog.Location = new System.Drawing.Point(15, 62);
            this.checkbox_alwaysshowqualitydialog.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_alwaysshowqualitydialog.Name = "checkbox_alwaysshowqualitydialog";
            this.checkbox_alwaysshowqualitydialog.PropertyName = "OutputFilePromptQuality";
            this.checkbox_alwaysshowqualitydialog.Size = new System.Drawing.Size(492, 31);
            this.checkbox_alwaysshowqualitydialog.TabIndex = 9;
            this.checkbox_alwaysshowqualitydialog.Text = "Show quality dialog every time an image is saved";
            this.checkbox_alwaysshowqualitydialog.UseVisualStyleBackColor = true;
            // 
            // label_jpegquality
            // 
            this.label_jpegquality.LanguageKey = "settings_jpegquality";
            this.label_jpegquality.Location = new System.Drawing.Point(8, 30);
            this.label_jpegquality.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_jpegquality.Name = "label_jpegquality";
            this.label_jpegquality.Size = new System.Drawing.Size(145, 29);
            this.label_jpegquality.TabIndex = 13;
            this.label_jpegquality.Text = "JPEG quality";
            // 
            // textBoxJpegQuality
            // 
            this.textBoxJpegQuality.Enabled = false;
            this.textBoxJpegQuality.Location = new System.Drawing.Point(464, 26);
            this.textBoxJpegQuality.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxJpegQuality.Name = "textBoxJpegQuality";
            this.textBoxJpegQuality.ReadOnly = true;
            this.textBoxJpegQuality.Size = new System.Drawing.Size(43, 25);
            this.textBoxJpegQuality.TabIndex = 8;
            this.textBoxJpegQuality.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // trackBarJpegQuality
            // 
            this.trackBarJpegQuality.LargeChange = 10;
            this.trackBarJpegQuality.Location = new System.Drawing.Point(172, 26);
            this.trackBarJpegQuality.Margin = new System.Windows.Forms.Padding(4);
            this.trackBarJpegQuality.Maximum = 100;
            this.trackBarJpegQuality.Name = "trackBarJpegQuality";
            this.trackBarJpegQuality.Size = new System.Drawing.Size(291, 56);
            this.trackBarJpegQuality.TabIndex = 7;
            this.trackBarJpegQuality.TickFrequency = 10;
            this.trackBarJpegQuality.Scroll += new System.EventHandler(this.TrackBarJpegQualityScroll);
            // 
            // groupbox_destination
            // 
            this.groupbox_destination.Controls.Add(this.checkbox_picker);
            this.groupbox_destination.Controls.Add(this.listview_destinations);
            this.groupbox_destination.LanguageKey = "settings_destination";
            this.groupbox_destination.Location = new System.Drawing.Point(2, 8);
            this.groupbox_destination.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_destination.Name = "groupbox_destination";
            this.groupbox_destination.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_destination.Size = new System.Drawing.Size(515, 302);
            this.groupbox_destination.TabIndex = 16;
            this.groupbox_destination.TabStop = false;
            this.groupbox_destination.Text = "Destination";
            // 
            // checkbox_picker
            // 
            this.checkbox_picker.LanguageKey = "settings_destination_picker";
            this.checkbox_picker.Location = new System.Drawing.Point(8, 18);
            this.checkbox_picker.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_picker.Name = "checkbox_picker";
            this.checkbox_picker.Size = new System.Drawing.Size(492, 30);
            this.checkbox_picker.TabIndex = 1;
            this.checkbox_picker.Text = "Select destination dynamically";
            this.checkbox_picker.UseVisualStyleBackColor = true;
            this.checkbox_picker.CheckStateChanged += new System.EventHandler(this.DestinationsCheckStateChanged);
            // 
            // listview_destinations
            // 
            this.listview_destinations.Alignment = System.Windows.Forms.ListViewAlignment.Left;
            this.listview_destinations.AutoArrange = false;
            this.listview_destinations.CheckBoxes = true;
            this.listview_destinations.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.destination});
            this.listview_destinations.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listview_destinations.LabelWrap = false;
            this.listview_destinations.Location = new System.Drawing.Point(8, 48);
            this.listview_destinations.Margin = new System.Windows.Forms.Padding(4);
            this.listview_destinations.Name = "listview_destinations";
            this.listview_destinations.ShowGroups = false;
            this.listview_destinations.Size = new System.Drawing.Size(500, 246);
            this.listview_destinations.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listview_destinations.TabIndex = 2;
            this.listview_destinations.UseCompatibleStateImageBehavior = false;
            this.listview_destinations.View = System.Windows.Forms.View.Details;
            // 
            // destination
            // 
            this.destination.Text = "Destination";
            this.destination.Width = 380;
            // 
            // tabcontrol
            // 
            this.tabcontrol.Controls.Add(this.tab_general);
            this.tabcontrol.Controls.Add(this.tab_capture);
            this.tabcontrol.Controls.Add(this.tab_output);
            this.tabcontrol.Controls.Add(this.tab_destinations);
            this.tabcontrol.Controls.Add(this.tab_printer);
            this.tabcontrol.Controls.Add(this.tab_plugins);
            this.tabcontrol.Controls.Add(this.tab_expert);
            this.tabcontrol.Location = new System.Drawing.Point(15, 16);
            this.tabcontrol.Margin = new System.Windows.Forms.Padding(4);
            this.tabcontrol.Name = "tabcontrol";
            this.tabcontrol.SelectedIndex = 0;
            this.tabcontrol.Size = new System.Drawing.Size(539, 471);
            this.tabcontrol.TabIndex = 0;
            // 
            // tab_general
            // 
            this.tab_general.BackColor = System.Drawing.Color.Transparent;
            this.tab_general.Controls.Add(this.groupbox_network);
            this.tab_general.Controls.Add(this.groupbox_hotkeys);
            this.tab_general.Controls.Add(this.groupbox_applicationsettings);
            this.tab_general.LanguageKey = "settings_general";
            this.tab_general.Location = new System.Drawing.Point(4, 25);
            this.tab_general.Margin = new System.Windows.Forms.Padding(4);
            this.tab_general.Name = "tab_general";
            this.tab_general.Padding = new System.Windows.Forms.Padding(4);
            this.tab_general.Size = new System.Drawing.Size(531, 442);
            this.tab_general.TabIndex = 0;
            this.tab_general.Text = "General";
            this.tab_general.UseVisualStyleBackColor = true;
            // 
            // groupbox_network
            // 
            this.groupbox_network.Controls.Add(this.numericUpDown_daysbetweencheck);
            this.groupbox_network.Controls.Add(this.label_checkperiod);
            this.groupbox_network.Controls.Add(this.checkbox_usedefaultproxy);
            this.groupbox_network.LanguageKey = "settings_network";
            this.groupbox_network.Location = new System.Drawing.Point(4, 316);
            this.groupbox_network.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_network.Name = "groupbox_network";
            this.groupbox_network.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_network.Size = new System.Drawing.Size(515, 90);
            this.groupbox_network.TabIndex = 54;
            this.groupbox_network.TabStop = false;
            this.groupbox_network.Text = "Network and updates";
            // 
            // numericUpDown_daysbetweencheck
            // 
            this.numericUpDown_daysbetweencheck.Location = new System.Drawing.Point(431, 46);
            this.numericUpDown_daysbetweencheck.Margin = new System.Windows.Forms.Padding(4);
            this.numericUpDown_daysbetweencheck.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this.numericUpDown_daysbetweencheck.Name = "numericUpDown_daysbetweencheck";
            this.numericUpDown_daysbetweencheck.Size = new System.Drawing.Size(71, 25);
            this.numericUpDown_daysbetweencheck.TabIndex = 8;
            this.numericUpDown_daysbetweencheck.ThousandsSeparator = true;
            // 
            // label_checkperiod
            // 
            this.label_checkperiod.LanguageKey = "settings_checkperiod";
            this.label_checkperiod.Location = new System.Drawing.Point(6, 49);
            this.label_checkperiod.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_checkperiod.Name = "label_checkperiod";
            this.label_checkperiod.Size = new System.Drawing.Size(418, 29);
            this.label_checkperiod.TabIndex = 19;
            this.label_checkperiod.Text = "Update check interval in days (0=no check)";
            // 
            // checkbox_usedefaultproxy
            // 
            this.checkbox_usedefaultproxy.LanguageKey = "settings_usedefaultproxy";
            this.checkbox_usedefaultproxy.Location = new System.Drawing.Point(9, 14);
            this.checkbox_usedefaultproxy.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_usedefaultproxy.Name = "checkbox_usedefaultproxy";
            this.checkbox_usedefaultproxy.PropertyName = "UseProxy";
            this.checkbox_usedefaultproxy.Size = new System.Drawing.Size(496, 31);
            this.checkbox_usedefaultproxy.TabIndex = 7;
            this.checkbox_usedefaultproxy.Text = "Use default system proxy";
            this.checkbox_usedefaultproxy.UseVisualStyleBackColor = true;
            // 
            // groupbox_hotkeys
            // 
            this.groupbox_hotkeys.Controls.Add(this.label_lastregion_hotkey);
            this.groupbox_hotkeys.Controls.Add(this.lastregion_hotkeyControl);
            this.groupbox_hotkeys.Controls.Add(this.label_ie_hotkey);
            this.groupbox_hotkeys.Controls.Add(this.ie_hotkeyControl);
            this.groupbox_hotkeys.Controls.Add(this.label_region_hotkey);
            this.groupbox_hotkeys.Controls.Add(this.label_window_hotkey);
            this.groupbox_hotkeys.Controls.Add(this.label_fullscreen_hotkey);
            this.groupbox_hotkeys.Controls.Add(this.region_hotkeyControl);
            this.groupbox_hotkeys.Controls.Add(this.window_hotkeyControl);
            this.groupbox_hotkeys.Controls.Add(this.fullscreen_hotkeyControl);
            this.groupbox_hotkeys.LanguageKey = "hotkeys";
            this.groupbox_hotkeys.Location = new System.Drawing.Point(2, 122);
            this.groupbox_hotkeys.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_hotkeys.Name = "groupbox_hotkeys";
            this.groupbox_hotkeys.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_hotkeys.Size = new System.Drawing.Size(515, 190);
            this.groupbox_hotkeys.TabIndex = 15;
            this.groupbox_hotkeys.TabStop = false;
            this.groupbox_hotkeys.Text = "Hotkeys";
            // 
            // label_lastregion_hotkey
            // 
            this.label_lastregion_hotkey.LanguageKey = "contextmenu_capturelastregion";
            this.label_lastregion_hotkey.Location = new System.Drawing.Point(8, 118);
            this.label_lastregion_hotkey.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_lastregion_hotkey.Name = "label_lastregion_hotkey";
            this.label_lastregion_hotkey.Size = new System.Drawing.Size(265, 25);
            this.label_lastregion_hotkey.TabIndex = 53;
            this.label_lastregion_hotkey.Text = "Capture last region";
            // 
            // lastregion_hotkeyControl
            // 
            this.lastregion_hotkeyControl.Hotkey = System.Windows.Forms.Keys.None;
            this.lastregion_hotkeyControl.HotkeyModifiers = System.Windows.Forms.Keys.None;
            this.lastregion_hotkeyControl.Location = new System.Drawing.Point(280, 118);
            this.lastregion_hotkeyControl.Margin = new System.Windows.Forms.Padding(4);
            this.lastregion_hotkeyControl.Name = "lastregion_hotkeyControl";
            this.lastregion_hotkeyControl.PropertyName = "LastregionHotkey";
            this.lastregion_hotkeyControl.Size = new System.Drawing.Size(223, 25);
            this.lastregion_hotkeyControl.TabIndex = 5;
            // 
            // label_ie_hotkey
            // 
            this.label_ie_hotkey.LanguageKey = "contextmenu_captureie";
            this.label_ie_hotkey.Location = new System.Drawing.Point(8, 150);
            this.label_ie_hotkey.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_ie_hotkey.Name = "label_ie_hotkey";
            this.label_ie_hotkey.Size = new System.Drawing.Size(265, 25);
            this.label_ie_hotkey.TabIndex = 51;
            this.label_ie_hotkey.Text = "Capture Internet Explorer";
            // 
            // ie_hotkeyControl
            // 
            this.ie_hotkeyControl.Hotkey = System.Windows.Forms.Keys.None;
            this.ie_hotkeyControl.HotkeyModifiers = System.Windows.Forms.Keys.None;
            this.ie_hotkeyControl.Location = new System.Drawing.Point(280, 150);
            this.ie_hotkeyControl.Margin = new System.Windows.Forms.Padding(4);
            this.ie_hotkeyControl.Name = "ie_hotkeyControl";
            this.ie_hotkeyControl.PropertyName = "IEHotkey";
            this.ie_hotkeyControl.Size = new System.Drawing.Size(223, 25);
            this.ie_hotkeyControl.TabIndex = 6;
            // 
            // label_region_hotkey
            // 
            this.label_region_hotkey.LanguageKey = "contextmenu_capturearea";
            this.label_region_hotkey.Location = new System.Drawing.Point(8, 85);
            this.label_region_hotkey.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_region_hotkey.Name = "label_region_hotkey";
            this.label_region_hotkey.Size = new System.Drawing.Size(265, 25);
            this.label_region_hotkey.TabIndex = 49;
            this.label_region_hotkey.Text = "Capture region";
            // 
            // label_window_hotkey
            // 
            this.label_window_hotkey.LanguageKey = "contextmenu_capturewindow";
            this.label_window_hotkey.Location = new System.Drawing.Point(8, 52);
            this.label_window_hotkey.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_window_hotkey.Name = "label_window_hotkey";
            this.label_window_hotkey.Size = new System.Drawing.Size(265, 29);
            this.label_window_hotkey.TabIndex = 48;
            this.label_window_hotkey.Text = "Capture window";
            // 
            // label_fullscreen_hotkey
            // 
            this.label_fullscreen_hotkey.LanguageKey = "contextmenu_capturefullscreen";
            this.label_fullscreen_hotkey.Location = new System.Drawing.Point(8, 20);
            this.label_fullscreen_hotkey.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_fullscreen_hotkey.Name = "label_fullscreen_hotkey";
            this.label_fullscreen_hotkey.Size = new System.Drawing.Size(265, 29);
            this.label_fullscreen_hotkey.TabIndex = 47;
            this.label_fullscreen_hotkey.Text = "Capture full screen";
            // 
            // region_hotkeyControl
            // 
            this.region_hotkeyControl.Hotkey = System.Windows.Forms.Keys.None;
            this.region_hotkeyControl.HotkeyModifiers = System.Windows.Forms.Keys.None;
            this.region_hotkeyControl.Location = new System.Drawing.Point(280, 85);
            this.region_hotkeyControl.Margin = new System.Windows.Forms.Padding(4);
            this.region_hotkeyControl.Name = "region_hotkeyControl";
            this.region_hotkeyControl.PropertyName = "RegionHotkey";
            this.region_hotkeyControl.Size = new System.Drawing.Size(223, 25);
            this.region_hotkeyControl.TabIndex = 4;
            // 
            // window_hotkeyControl
            // 
            this.window_hotkeyControl.Hotkey = System.Windows.Forms.Keys.None;
            this.window_hotkeyControl.HotkeyModifiers = System.Windows.Forms.Keys.None;
            this.window_hotkeyControl.Location = new System.Drawing.Point(280, 52);
            this.window_hotkeyControl.Margin = new System.Windows.Forms.Padding(4);
            this.window_hotkeyControl.Name = "window_hotkeyControl";
            this.window_hotkeyControl.PropertyName = "WindowHotkey";
            this.window_hotkeyControl.Size = new System.Drawing.Size(223, 25);
            this.window_hotkeyControl.TabIndex = 3;
            // 
            // fullscreen_hotkeyControl
            // 
            this.fullscreen_hotkeyControl.Hotkey = System.Windows.Forms.Keys.None;
            this.fullscreen_hotkeyControl.HotkeyModifiers = System.Windows.Forms.Keys.None;
            this.fullscreen_hotkeyControl.Location = new System.Drawing.Point(280, 20);
            this.fullscreen_hotkeyControl.Margin = new System.Windows.Forms.Padding(4);
            this.fullscreen_hotkeyControl.Name = "fullscreen_hotkeyControl";
            this.fullscreen_hotkeyControl.PropertyName = "FullscreenHotkey";
            this.fullscreen_hotkeyControl.Size = new System.Drawing.Size(223, 25);
            this.fullscreen_hotkeyControl.TabIndex = 2;
            // 
            // tab_capture
            // 
            this.tab_capture.Controls.Add(this.groupbox_editor);
            this.tab_capture.Controls.Add(this.groupbox_iecapture);
            this.tab_capture.Controls.Add(this.groupbox_windowscapture);
            this.tab_capture.Controls.Add(this.groupbox_capture);
            this.tab_capture.LanguageKey = "settings_capture";
            this.tab_capture.Location = new System.Drawing.Point(4, 25);
            this.tab_capture.Margin = new System.Windows.Forms.Padding(4);
            this.tab_capture.Name = "tab_capture";
            this.tab_capture.Size = new System.Drawing.Size(531, 442);
            this.tab_capture.TabIndex = 3;
            this.tab_capture.Text = "Capture";
            this.tab_capture.UseVisualStyleBackColor = true;
            // 
            // groupbox_editor
            // 
            this.groupbox_editor.Controls.Add(this.checkbox_editor_match_capture_size);
            this.groupbox_editor.LanguageKey = "settings_editor";
            this.groupbox_editor.Location = new System.Drawing.Point(5, 346);
            this.groupbox_editor.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_editor.Name = "groupbox_editor";
            this.groupbox_editor.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_editor.Size = new System.Drawing.Size(520, 62);
            this.groupbox_editor.TabIndex = 27;
            this.groupbox_editor.TabStop = false;
            this.groupbox_editor.Text = "Editor";
            // 
            // checkbox_editor_match_capture_size
            // 
            this.checkbox_editor_match_capture_size.LanguageKey = "editor_match_capture_size";
            this.checkbox_editor_match_capture_size.Location = new System.Drawing.Point(8, 24);
            this.checkbox_editor_match_capture_size.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_editor_match_capture_size.Name = "checkbox_editor_match_capture_size";
            this.checkbox_editor_match_capture_size.PropertyName = "MatchSizeToCapture";
            this.checkbox_editor_match_capture_size.SectionName = "Editor";
            this.checkbox_editor_match_capture_size.Size = new System.Drawing.Size(496, 30);
            this.checkbox_editor_match_capture_size.TabIndex = 11;
            this.checkbox_editor_match_capture_size.Text = "Match capture size";
            this.checkbox_editor_match_capture_size.UseVisualStyleBackColor = true;
            // 
            // groupbox_iecapture
            // 
            this.groupbox_iecapture.Controls.Add(this.checkbox_ie_capture);
            this.groupbox_iecapture.LanguageKey = "settings_iecapture";
            this.groupbox_iecapture.Location = new System.Drawing.Point(5, 280);
            this.groupbox_iecapture.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_iecapture.Name = "groupbox_iecapture";
            this.groupbox_iecapture.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_iecapture.Size = new System.Drawing.Size(520, 62);
            this.groupbox_iecapture.TabIndex = 2;
            this.groupbox_iecapture.TabStop = false;
            this.groupbox_iecapture.Text = "Internet Explorer capture";
            // 
            // checkbox_ie_capture
            // 
            this.checkbox_ie_capture.LanguageKey = "settings_iecapture";
            this.checkbox_ie_capture.Location = new System.Drawing.Point(8, 24);
            this.checkbox_ie_capture.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_ie_capture.Name = "checkbox_ie_capture";
            this.checkbox_ie_capture.PropertyName = "IECapture";
            this.checkbox_ie_capture.Size = new System.Drawing.Size(505, 30);
            this.checkbox_ie_capture.TabIndex = 10;
            this.checkbox_ie_capture.Text = "Internet Explorer capture";
            this.checkbox_ie_capture.UseVisualStyleBackColor = true;
            // 
            // groupbox_windowscapture
            // 
            this.groupbox_windowscapture.Controls.Add(this.colorButton_window_background);
            this.groupbox_windowscapture.Controls.Add(this.radiobuttonWindowCapture);
            this.groupbox_windowscapture.Controls.Add(this.radiobuttonInteractiveCapture);
            this.groupbox_windowscapture.Controls.Add(this.combobox_window_capture_mode);
            this.groupbox_windowscapture.LanguageKey = "settings_windowscapture";
            this.groupbox_windowscapture.Location = new System.Drawing.Point(5, 176);
            this.groupbox_windowscapture.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_windowscapture.Name = "groupbox_windowscapture";
            this.groupbox_windowscapture.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_windowscapture.Size = new System.Drawing.Size(520, 100);
            this.groupbox_windowscapture.TabIndex = 1;
            this.groupbox_windowscapture.TabStop = false;
            this.groupbox_windowscapture.Text = "Window capture";
            // 
            // colorButton_window_background
            // 
            this.colorButton_window_background.AutoSize = true;
            this.colorButton_window_background.Image = ((System.Drawing.Image)(resources.GetObject("colorButton_window_background.Image")));
            this.colorButton_window_background.Location = new System.Drawing.Point(468, 46);
            this.colorButton_window_background.Margin = new System.Windows.Forms.Padding(4);
            this.colorButton_window_background.Name = "colorButton_window_background";
            this.colorButton_window_background.SelectedColor = System.Drawing.Color.White;
            this.colorButton_window_background.Size = new System.Drawing.Size(36, 38);
            this.colorButton_window_background.TabIndex = 9;
            this.colorButton_window_background.UseVisualStyleBackColor = true;
            // 
            // radiobuttonWindowCapture
            // 
            this.radiobuttonWindowCapture.AutoSize = true;
            this.radiobuttonWindowCapture.LanguageKey = "settings_window_capture_mode";
            this.radiobuttonWindowCapture.Location = new System.Drawing.Point(14, 55);
            this.radiobuttonWindowCapture.Margin = new System.Windows.Forms.Padding(4);
            this.radiobuttonWindowCapture.Name = "radiobuttonWindowCapture";
            this.radiobuttonWindowCapture.Size = new System.Drawing.Size(180, 19);
            this.radiobuttonWindowCapture.TabIndex = 7;
            this.radiobuttonWindowCapture.TabStop = true;
            this.radiobuttonWindowCapture.Text = "Window capture mode";
            this.radiobuttonWindowCapture.UseVisualStyleBackColor = true;
            // 
            // radiobuttonInteractiveCapture
            // 
            this.radiobuttonInteractiveCapture.AutoSize = true;
            this.radiobuttonInteractiveCapture.LanguageKey = "settings_capture_windows_interactive";
            this.radiobuttonInteractiveCapture.Location = new System.Drawing.Point(14, 25);
            this.radiobuttonInteractiveCapture.Margin = new System.Windows.Forms.Padding(4);
            this.radiobuttonInteractiveCapture.Name = "radiobuttonInteractiveCapture";
            this.radiobuttonInteractiveCapture.PropertyName = "CaptureWindowsInteractive";
            this.radiobuttonInteractiveCapture.Size = new System.Drawing.Size(308, 19);
            this.radiobuttonInteractiveCapture.TabIndex = 6;
            this.radiobuttonInteractiveCapture.TabStop = true;
            this.radiobuttonInteractiveCapture.Text = "Use interactive window capture mode";
            this.radiobuttonInteractiveCapture.UseVisualStyleBackColor = true;
            this.radiobuttonInteractiveCapture.CheckedChanged += new System.EventHandler(this.radiobutton_CheckedChanged);
            // 
            // combobox_window_capture_mode
            // 
            this.combobox_window_capture_mode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combobox_window_capture_mode.FormattingEnabled = true;
            this.combobox_window_capture_mode.Location = new System.Drawing.Point(271, 54);
            this.combobox_window_capture_mode.Margin = new System.Windows.Forms.Padding(4);
            this.combobox_window_capture_mode.MaxDropDownItems = 15;
            this.combobox_window_capture_mode.Name = "combobox_window_capture_mode";
            this.combobox_window_capture_mode.Size = new System.Drawing.Size(188, 23);
            this.combobox_window_capture_mode.TabIndex = 8;
            this.combobox_window_capture_mode.SelectedIndexChanged += new System.EventHandler(this.Combobox_window_capture_modeSelectedIndexChanged);
            // 
            // groupbox_capture
            // 
            this.groupbox_capture.Controls.Add(this.checkbox_notifications);
            this.groupbox_capture.Controls.Add(this.checkbox_playsound);
            this.groupbox_capture.Controls.Add(this.checkbox_capture_mousepointer);
            this.groupbox_capture.Controls.Add(this.numericUpDownWaitTime);
            this.groupbox_capture.Controls.Add(this.label_waittime);
            this.groupbox_capture.Controls.Add(this.checkbox_zoomer);
            this.groupbox_capture.LanguageKey = "settings_capture";
            this.groupbox_capture.Location = new System.Drawing.Point(5, 5);
            this.groupbox_capture.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_capture.Name = "groupbox_capture";
            this.groupbox_capture.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_capture.Size = new System.Drawing.Size(520, 168);
            this.groupbox_capture.TabIndex = 0;
            this.groupbox_capture.TabStop = false;
            this.groupbox_capture.Text = "Capture";
            // 
            // checkbox_notifications
            // 
            this.checkbox_notifications.LanguageKey = "settings_shownotify";
            this.checkbox_notifications.Location = new System.Drawing.Point(14, 74);
            this.checkbox_notifications.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_notifications.Name = "checkbox_notifications";
            this.checkbox_notifications.PropertyName = "ShowTrayNotification";
            this.checkbox_notifications.Size = new System.Drawing.Size(499, 30);
            this.checkbox_notifications.TabIndex = 3;
            this.checkbox_notifications.Text = "Show notifications";
            this.checkbox_notifications.UseVisualStyleBackColor = true;
            // 
            // checkbox_playsound
            // 
            this.checkbox_playsound.LanguageKey = "settings_playsound";
            this.checkbox_playsound.Location = new System.Drawing.Point(14, 49);
            this.checkbox_playsound.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_playsound.Name = "checkbox_playsound";
            this.checkbox_playsound.PropertyName = "PlayCameraSound";
            this.checkbox_playsound.Size = new System.Drawing.Size(499, 30);
            this.checkbox_playsound.TabIndex = 2;
            this.checkbox_playsound.Text = "Play camera sound";
            this.checkbox_playsound.UseVisualStyleBackColor = true;
            // 
            // checkbox_capture_mousepointer
            // 
            this.checkbox_capture_mousepointer.LanguageKey = "settings_capture_mousepointer";
            this.checkbox_capture_mousepointer.Location = new System.Drawing.Point(14, 24);
            this.checkbox_capture_mousepointer.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_capture_mousepointer.Name = "checkbox_capture_mousepointer";
            this.checkbox_capture_mousepointer.PropertyName = "CaptureMousepointer";
            this.checkbox_capture_mousepointer.Size = new System.Drawing.Size(492, 30);
            this.checkbox_capture_mousepointer.TabIndex = 1;
            this.checkbox_capture_mousepointer.Text = "Capture mousepointer";
            this.checkbox_capture_mousepointer.UseVisualStyleBackColor = true;
            // 
            // numericUpDownWaitTime
            // 
            this.numericUpDownWaitTime.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownWaitTime.Location = new System.Drawing.Point(11, 129);
            this.numericUpDownWaitTime.Margin = new System.Windows.Forms.Padding(4);
            this.numericUpDownWaitTime.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDownWaitTime.Name = "numericUpDownWaitTime";
            this.numericUpDownWaitTime.Size = new System.Drawing.Size(71, 25);
            this.numericUpDownWaitTime.TabIndex = 5;
            this.numericUpDownWaitTime.ThousandsSeparator = true;
            // 
            // label_waittime
            // 
            this.label_waittime.LanguageKey = "settings_waittime";
            this.label_waittime.Location = new System.Drawing.Point(90, 132);
            this.label_waittime.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_waittime.Name = "label_waittime";
            this.label_waittime.Size = new System.Drawing.Size(414, 20);
            this.label_waittime.TabIndex = 5;
            this.label_waittime.Text = "Milliseconds to wait before capture";
            // 
            // tab_output
            // 
            this.tab_output.BackColor = System.Drawing.Color.Transparent;
            this.tab_output.Controls.Add(this.groupbox_preferredfilesettings);
            this.tab_output.Controls.Add(this.groupbox_qualitysettings);
            this.tab_output.LanguageKey = "settings_output";
            this.tab_output.Location = new System.Drawing.Point(4, 25);
            this.tab_output.Margin = new System.Windows.Forms.Padding(4);
            this.tab_output.Name = "tab_output";
            this.tab_output.Padding = new System.Windows.Forms.Padding(4);
            this.tab_output.Size = new System.Drawing.Size(531, 442);
            this.tab_output.TabIndex = 1;
            this.tab_output.Text = "Output";
            this.tab_output.UseVisualStyleBackColor = true;
            // 
            // tab_destinations
            // 
            this.tab_destinations.Controls.Add(this.groupDefaultEffects);
            this.tab_destinations.Controls.Add(this.groupbox_destination);
            this.tab_destinations.LanguageKey = "settings_destination";
            this.tab_destinations.Location = new System.Drawing.Point(4, 25);
            this.tab_destinations.Margin = new System.Windows.Forms.Padding(4);
            this.tab_destinations.Name = "tab_destinations";
            this.tab_destinations.Size = new System.Drawing.Size(531, 442);
            this.tab_destinations.TabIndex = 4;
            this.tab_destinations.Text = "Destination";
            this.tab_destinations.UseVisualStyleBackColor = true;
            // 
            // groupDefaultEffects
            // 
            this.groupDefaultEffects.Controls.Add(this.greenshotLabel3);
            this.groupDefaultEffects.Controls.Add(this.greenshotLabel2);
            this.groupDefaultEffects.Controls.Add(this.btnEffectBorderColor);
            this.groupDefaultEffects.Controls.Add(this.greenshotLabel1);
            this.groupDefaultEffects.Controls.Add(this.txtEffectBorderWidth);
            this.groupDefaultEffects.LanguageKey = "settings_destination_groupDefaultEffects";
            this.groupDefaultEffects.Location = new System.Drawing.Point(2, 328);
            this.groupDefaultEffects.Name = "groupDefaultEffects";
            this.groupDefaultEffects.Size = new System.Drawing.Size(515, 79);
            this.groupDefaultEffects.TabIndex = 18;
            this.groupDefaultEffects.TabStop = false;
            this.groupDefaultEffects.Text = "Default Effects";
            // 
            // greenshotLabel3
            // 
            this.greenshotLabel3.AutoSize = true;
            this.greenshotLabel3.LanguageKey = "settings_destination_defaultEffects_imageBorder_color";
            this.greenshotLabel3.Location = new System.Drawing.Point(236, 38);
            this.greenshotLabel3.Name = "greenshotLabel3";
            this.greenshotLabel3.Size = new System.Drawing.Size(47, 15);
            this.greenshotLabel3.TabIndex = 3;
            this.greenshotLabel3.Text = "Color";
            // 
            // greenshotLabel2
            // 
            this.greenshotLabel2.AutoSize = true;
            this.greenshotLabel2.LanguageKey = "settings_destination_defaultEffects_imageBorder_width";
            this.greenshotLabel2.Location = new System.Drawing.Point(120, 38);
            this.greenshotLabel2.Name = "greenshotLabel2";
            this.greenshotLabel2.Size = new System.Drawing.Size(47, 15);
            this.greenshotLabel2.TabIndex = 1;
            this.greenshotLabel2.Text = "Width";
            // 
            // btnEffectBorderColor
            // 
            this.btnEffectBorderColor.Location = new System.Drawing.Point(289, 33);
            this.btnEffectBorderColor.Name = "btnEffectBorderColor";
            this.btnEffectBorderColor.SelectedColor = System.Drawing.Color.White;
            this.btnEffectBorderColor.Size = new System.Drawing.Size(69, 26);
            this.btnEffectBorderColor.TabIndex = 0;
            this.btnEffectBorderColor.UseVisualStyleBackColor = true;
            // 
            // greenshotLabel1
            // 
            this.greenshotLabel1.AutoSize = true;
            this.greenshotLabel1.LanguageKey = "settings_destination_defaultEffects_imageBorder";
            this.greenshotLabel1.Location = new System.Drawing.Point(9, 38);
            this.greenshotLabel1.Name = "greenshotLabel1";
            this.greenshotLabel1.Size = new System.Drawing.Size(111, 15);
            this.greenshotLabel1.TabIndex = 0;
            this.greenshotLabel1.Text = "Image Border:";
            // 
            // txtEffectBorderWidth
            // 
            this.txtEffectBorderWidth.Location = new System.Drawing.Point(173, 34);
            this.txtEffectBorderWidth.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.txtEffectBorderWidth.Name = "txtEffectBorderWidth";
            this.txtEffectBorderWidth.Size = new System.Drawing.Size(51, 25);
            this.txtEffectBorderWidth.TabIndex = 2;
            // 
            // tab_printer
            // 
            this.tab_printer.Controls.Add(this.groupBoxColors);
            this.tab_printer.Controls.Add(this.groupBoxPrintLayout);
            this.tab_printer.Controls.Add(this.checkbox_alwaysshowprintoptionsdialog);
            this.tab_printer.LanguageKey = "settings_printer";
            this.tab_printer.Location = new System.Drawing.Point(4, 25);
            this.tab_printer.Margin = new System.Windows.Forms.Padding(4);
            this.tab_printer.Name = "tab_printer";
            this.tab_printer.Padding = new System.Windows.Forms.Padding(4);
            this.tab_printer.Size = new System.Drawing.Size(531, 442);
            this.tab_printer.TabIndex = 2;
            this.tab_printer.Text = "Printer";
            this.tab_printer.UseVisualStyleBackColor = true;
            // 
            // groupBoxColors
            // 
            this.groupBoxColors.AutoSize = true;
            this.groupBoxColors.Controls.Add(this.checkboxPrintInverted);
            this.groupBoxColors.Controls.Add(this.radioBtnColorPrint);
            this.groupBoxColors.Controls.Add(this.radioBtnGrayScale);
            this.groupBoxColors.Controls.Add(this.radioBtnMonochrome);
            this.groupBoxColors.LanguageKey = "printoptions_colors";
            this.groupBoxColors.Location = new System.Drawing.Point(8, 204);
            this.groupBoxColors.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxColors.Name = "groupBoxColors";
            this.groupBoxColors.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxColors.Size = new System.Drawing.Size(414, 161);
            this.groupBoxColors.TabIndex = 10;
            this.groupBoxColors.TabStop = false;
            this.groupBoxColors.Text = "Color settings";
            // 
            // checkboxPrintInverted
            // 
            this.checkboxPrintInverted.AutoSize = true;
            this.checkboxPrintInverted.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxPrintInverted.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxPrintInverted.LanguageKey = "printoptions_inverted";
            this.checkboxPrintInverted.Location = new System.Drawing.Point(16, 110);
            this.checkboxPrintInverted.Margin = new System.Windows.Forms.Padding(4);
            this.checkboxPrintInverted.Name = "checkboxPrintInverted";
            this.checkboxPrintInverted.PropertyName = "OutputPrintInverted";
            this.checkboxPrintInverted.Size = new System.Drawing.Size(237, 19);
            this.checkboxPrintInverted.TabIndex = 14;
            this.checkboxPrintInverted.Text = "Print with inverted colors";
            this.checkboxPrintInverted.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxPrintInverted.UseVisualStyleBackColor = true;
            // 
            // radioBtnColorPrint
            // 
            this.radioBtnColorPrint.AutoSize = true;
            this.radioBtnColorPrint.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.radioBtnColorPrint.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.radioBtnColorPrint.LanguageKey = "printoptions_printcolor";
            this.radioBtnColorPrint.Location = new System.Drawing.Point(16, 24);
            this.radioBtnColorPrint.Margin = new System.Windows.Forms.Padding(4);
            this.radioBtnColorPrint.Name = "radioBtnColorPrint";
            this.radioBtnColorPrint.PropertyName = "OutputPrintColor";
            this.radioBtnColorPrint.Size = new System.Drawing.Size(156, 19);
            this.radioBtnColorPrint.TabIndex = 11;
            this.radioBtnColorPrint.Text = "Full color print";
            this.radioBtnColorPrint.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.radioBtnColorPrint.UseVisualStyleBackColor = true;
            // 
            // radioBtnGrayScale
            // 
            this.radioBtnGrayScale.AutoSize = true;
            this.radioBtnGrayScale.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.radioBtnGrayScale.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.radioBtnGrayScale.LanguageKey = "printoptions_printgrayscale";
            this.radioBtnGrayScale.Location = new System.Drawing.Point(16, 52);
            this.radioBtnGrayScale.Margin = new System.Windows.Forms.Padding(4);
            this.radioBtnGrayScale.Name = "radioBtnGrayScale";
            this.radioBtnGrayScale.PropertyName = "OutputPrintGrayscale";
            this.radioBtnGrayScale.Size = new System.Drawing.Size(220, 19);
            this.radioBtnGrayScale.TabIndex = 12;
            this.radioBtnGrayScale.Text = "Force grayscale printing";
            this.radioBtnGrayScale.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.radioBtnGrayScale.UseVisualStyleBackColor = true;
            // 
            // radioBtnMonochrome
            // 
            this.radioBtnMonochrome.AutoSize = true;
            this.radioBtnMonochrome.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.radioBtnMonochrome.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.radioBtnMonochrome.LanguageKey = "printoptions_printmonochrome";
            this.radioBtnMonochrome.Location = new System.Drawing.Point(16, 81);
            this.radioBtnMonochrome.Margin = new System.Windows.Forms.Padding(4);
            this.radioBtnMonochrome.Name = "radioBtnMonochrome";
            this.radioBtnMonochrome.PropertyName = "OutputPrintMonochrome";
            this.radioBtnMonochrome.Size = new System.Drawing.Size(236, 19);
            this.radioBtnMonochrome.TabIndex = 13;
            this.radioBtnMonochrome.Text = "Force black/white printing";
            this.radioBtnMonochrome.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.radioBtnMonochrome.UseVisualStyleBackColor = true;
            // 
            // groupBoxPrintLayout
            // 
            this.groupBoxPrintLayout.AutoSize = true;
            this.groupBoxPrintLayout.Controls.Add(this.checkboxDateTime);
            this.groupBoxPrintLayout.Controls.Add(this.checkboxAllowShrink);
            this.groupBoxPrintLayout.Controls.Add(this.checkboxAllowEnlarge);
            this.groupBoxPrintLayout.Controls.Add(this.checkboxAllowRotate);
            this.groupBoxPrintLayout.Controls.Add(this.checkboxAllowCenter);
            this.groupBoxPrintLayout.LanguageKey = "printoptions_layout";
            this.groupBoxPrintLayout.Location = new System.Drawing.Point(8, 8);
            this.groupBoxPrintLayout.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxPrintLayout.Name = "groupBoxPrintLayout";
            this.groupBoxPrintLayout.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxPrintLayout.Size = new System.Drawing.Size(414, 195);
            this.groupBoxPrintLayout.TabIndex = 1;
            this.groupBoxPrintLayout.TabStop = false;
            this.groupBoxPrintLayout.Text = "Page layout settings";
            // 
            // checkboxDateTime
            // 
            this.checkboxDateTime.AutoSize = true;
            this.checkboxDateTime.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxDateTime.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxDateTime.LanguageKey = "printoptions_timestamp";
            this.checkboxDateTime.Location = new System.Drawing.Point(16, 144);
            this.checkboxDateTime.Margin = new System.Windows.Forms.Padding(4);
            this.checkboxDateTime.Name = "checkboxDateTime";
            this.checkboxDateTime.PropertyName = "OutputPrintFooter";
            this.checkboxDateTime.Size = new System.Drawing.Size(309, 19);
            this.checkboxDateTime.TabIndex = 6;
            this.checkboxDateTime.Text = "Print date / time at bottom of page";
            this.checkboxDateTime.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxDateTime.UseVisualStyleBackColor = true;
            // 
            // checkboxAllowShrink
            // 
            this.checkboxAllowShrink.AutoSize = true;
            this.checkboxAllowShrink.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowShrink.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowShrink.LanguageKey = "printoptions_allowshrink";
            this.checkboxAllowShrink.Location = new System.Drawing.Point(16, 29);
            this.checkboxAllowShrink.Margin = new System.Windows.Forms.Padding(4);
            this.checkboxAllowShrink.Name = "checkboxAllowShrink";
            this.checkboxAllowShrink.PropertyName = "OutputPrintAllowShrink";
            this.checkboxAllowShrink.Size = new System.Drawing.Size(293, 19);
            this.checkboxAllowShrink.TabIndex = 2;
            this.checkboxAllowShrink.Text = "Shrink printout to fit paper size";
            this.checkboxAllowShrink.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowShrink.UseVisualStyleBackColor = true;
            // 
            // checkboxAllowEnlarge
            // 
            this.checkboxAllowEnlarge.AutoSize = true;
            this.checkboxAllowEnlarge.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowEnlarge.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowEnlarge.LanguageKey = "printoptions_allowenlarge";
            this.checkboxAllowEnlarge.Location = new System.Drawing.Point(16, 58);
            this.checkboxAllowEnlarge.Margin = new System.Windows.Forms.Padding(4);
            this.checkboxAllowEnlarge.Name = "checkboxAllowEnlarge";
            this.checkboxAllowEnlarge.PropertyName = "OutputPrintAllowEnlarge";
            this.checkboxAllowEnlarge.Size = new System.Drawing.Size(301, 19);
            this.checkboxAllowEnlarge.TabIndex = 3;
            this.checkboxAllowEnlarge.Text = "Enlarge printout to fit paper size";
            this.checkboxAllowEnlarge.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowEnlarge.UseVisualStyleBackColor = true;
            // 
            // checkboxAllowRotate
            // 
            this.checkboxAllowRotate.AutoSize = true;
            this.checkboxAllowRotate.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowRotate.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowRotate.LanguageKey = "printoptions_allowrotate";
            this.checkboxAllowRotate.Location = new System.Drawing.Point(16, 86);
            this.checkboxAllowRotate.Margin = new System.Windows.Forms.Padding(4);
            this.checkboxAllowRotate.Name = "checkboxAllowRotate";
            this.checkboxAllowRotate.PropertyName = "OutputPrintAllowRotate";
            this.checkboxAllowRotate.Size = new System.Drawing.Size(309, 19);
            this.checkboxAllowRotate.TabIndex = 4;
            this.checkboxAllowRotate.Text = "Rotate printout to page orientation";
            this.checkboxAllowRotate.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowRotate.UseVisualStyleBackColor = true;
            // 
            // checkboxAllowCenter
            // 
            this.checkboxAllowCenter.AutoSize = true;
            this.checkboxAllowCenter.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowCenter.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowCenter.LanguageKey = "printoptions_allowcenter";
            this.checkboxAllowCenter.Location = new System.Drawing.Point(16, 115);
            this.checkboxAllowCenter.Margin = new System.Windows.Forms.Padding(4);
            this.checkboxAllowCenter.Name = "checkboxAllowCenter";
            this.checkboxAllowCenter.PropertyName = "OutputPrintCenter";
            this.checkboxAllowCenter.Size = new System.Drawing.Size(213, 19);
            this.checkboxAllowCenter.TabIndex = 5;
            this.checkboxAllowCenter.Text = "Center printout on page";
            this.checkboxAllowCenter.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.checkboxAllowCenter.UseVisualStyleBackColor = true;
            // 
            // checkbox_alwaysshowprintoptionsdialog
            // 
            this.checkbox_alwaysshowprintoptionsdialog.LanguageKey = "settings_alwaysshowprintoptionsdialog";
            this.checkbox_alwaysshowprintoptionsdialog.Location = new System.Drawing.Point(24, 366);
            this.checkbox_alwaysshowprintoptionsdialog.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_alwaysshowprintoptionsdialog.Name = "checkbox_alwaysshowprintoptionsdialog";
            this.checkbox_alwaysshowprintoptionsdialog.PropertyName = "OutputPrintPromptOptions";
            this.checkbox_alwaysshowprintoptionsdialog.Size = new System.Drawing.Size(492, 25);
            this.checkbox_alwaysshowprintoptionsdialog.TabIndex = 15;
            this.checkbox_alwaysshowprintoptionsdialog.Text = "Show print options dialog every time an image is printed";
            this.checkbox_alwaysshowprintoptionsdialog.UseVisualStyleBackColor = true;
            // 
            // tab_plugins
            // 
            this.tab_plugins.Controls.Add(this.groupbox_plugins);
            this.tab_plugins.LanguageKey = "settings_plugins";
            this.tab_plugins.Location = new System.Drawing.Point(4, 25);
            this.tab_plugins.Margin = new System.Windows.Forms.Padding(4);
            this.tab_plugins.Name = "tab_plugins";
            this.tab_plugins.Size = new System.Drawing.Size(531, 442);
            this.tab_plugins.TabIndex = 2;
            this.tab_plugins.Text = "Plugins";
            this.tab_plugins.UseVisualStyleBackColor = true;
            // 
            // groupbox_plugins
            // 
            this.groupbox_plugins.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupbox_plugins.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.groupbox_plugins.Controls.Add(this.listview_plugins);
            this.groupbox_plugins.Controls.Add(this.button_pluginconfigure);
            this.groupbox_plugins.LanguageKey = "settings_plugins";
            this.groupbox_plugins.Location = new System.Drawing.Point(0, 0);
            this.groupbox_plugins.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_plugins.Name = "groupbox_plugins";
            this.groupbox_plugins.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_plugins.Size = new System.Drawing.Size(529, 392);
            this.groupbox_plugins.TabIndex = 0;
            this.groupbox_plugins.TabStop = false;
            this.groupbox_plugins.Text = "Plugins";
            // 
            // listview_plugins
            // 
            this.listview_plugins.Dock = System.Windows.Forms.DockStyle.Top;
            this.listview_plugins.FullRowSelect = true;
            this.listview_plugins.Location = new System.Drawing.Point(4, 22);
            this.listview_plugins.Margin = new System.Windows.Forms.Padding(4);
            this.listview_plugins.Name = "listview_plugins";
            this.listview_plugins.Size = new System.Drawing.Size(521, 328);
            this.listview_plugins.TabIndex = 1;
            this.listview_plugins.UseCompatibleStateImageBehavior = false;
            this.listview_plugins.View = System.Windows.Forms.View.Details;
            this.listview_plugins.SelectedIndexChanged += new System.EventHandler(this.Listview_pluginsSelectedIndexChanged);
            this.listview_plugins.Click += new System.EventHandler(this.Listview_pluginsSelectedIndexChanged);
            // 
            // button_pluginconfigure
            // 
            this.button_pluginconfigure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_pluginconfigure.AutoSize = true;
            this.button_pluginconfigure.Enabled = false;
            this.button_pluginconfigure.LanguageKey = "settings_configureplugin";
            this.button_pluginconfigure.Location = new System.Drawing.Point(8, 356);
            this.button_pluginconfigure.Margin = new System.Windows.Forms.Padding(4);
            this.button_pluginconfigure.Name = "button_pluginconfigure";
            this.button_pluginconfigure.Size = new System.Drawing.Size(94, 29);
            this.button_pluginconfigure.TabIndex = 2;
            this.button_pluginconfigure.Text = "Configure";
            this.button_pluginconfigure.UseVisualStyleBackColor = true;
            this.button_pluginconfigure.Click += new System.EventHandler(this.Button_pluginconfigureClick);
            // 
            // tab_expert
            // 
            this.tab_expert.Controls.Add(this.groupbox_expert);
            this.tab_expert.LanguageKey = "expertsettings";
            this.tab_expert.Location = new System.Drawing.Point(4, 25);
            this.tab_expert.Margin = new System.Windows.Forms.Padding(4);
            this.tab_expert.Name = "tab_expert";
            this.tab_expert.Size = new System.Drawing.Size(531, 442);
            this.tab_expert.TabIndex = 5;
            this.tab_expert.Text = "Expert";
            this.tab_expert.UseVisualStyleBackColor = true;
            // 
            // groupbox_expert
            // 
            this.groupbox_expert.Controls.Add(this.checkbox_reuseeditor);
            this.groupbox_expert.Controls.Add(this.checkbox_minimizememoryfootprint);
            this.groupbox_expert.Controls.Add(this.checkbox_checkunstableupdates);
            this.groupbox_expert.Controls.Add(this.checkbox_suppresssavedialogatclose);
            this.groupbox_expert.Controls.Add(this.label_counter);
            this.groupbox_expert.Controls.Add(this.textbox_counter);
            this.groupbox_expert.Controls.Add(this.label_footerpattern);
            this.groupbox_expert.Controls.Add(this.textbox_footerpattern);
            this.groupbox_expert.Controls.Add(this.checkbox_thumbnailpreview);
            this.groupbox_expert.Controls.Add(this.checkbox_optimizeforrdp);
            this.groupbox_expert.Controls.Add(this.checkbox_autoreducecolors);
            this.groupbox_expert.Controls.Add(this.label_clipboardformats);
            this.groupbox_expert.Controls.Add(this.checkbox_enableexpert);
            this.groupbox_expert.Controls.Add(this.listview_clipboardformats);
            this.groupbox_expert.LanguageKey = "expertsettings";
            this.groupbox_expert.Location = new System.Drawing.Point(6, 6);
            this.groupbox_expert.Margin = new System.Windows.Forms.Padding(4);
            this.groupbox_expert.Name = "groupbox_expert";
            this.groupbox_expert.Padding = new System.Windows.Forms.Padding(4);
            this.groupbox_expert.Size = new System.Drawing.Size(515, 389);
            this.groupbox_expert.TabIndex = 17;
            this.groupbox_expert.TabStop = false;
            this.groupbox_expert.Text = "Expert";
            // 
            // checkbox_reuseeditor
            // 
            this.checkbox_reuseeditor.LanguageKey = "expertsettings_reuseeditorifpossible";
            this.checkbox_reuseeditor.Location = new System.Drawing.Point(12, 281);
            this.checkbox_reuseeditor.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_reuseeditor.Name = "checkbox_reuseeditor";
            this.checkbox_reuseeditor.PropertyName = "ReuseEditor";
            this.checkbox_reuseeditor.SectionName = "Editor";
            this.checkbox_reuseeditor.Size = new System.Drawing.Size(492, 30);
            this.checkbox_reuseeditor.TabIndex = 9;
            this.checkbox_reuseeditor.Text = "Reuse editor if possible";
            this.checkbox_reuseeditor.UseVisualStyleBackColor = true;
            // 
            // checkbox_minimizememoryfootprint
            // 
            this.checkbox_minimizememoryfootprint.LanguageKey = "expertsettings_minimizememoryfootprint";
            this.checkbox_minimizememoryfootprint.Location = new System.Drawing.Point(12, 258);
            this.checkbox_minimizememoryfootprint.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_minimizememoryfootprint.Name = "checkbox_minimizememoryfootprint";
            this.checkbox_minimizememoryfootprint.PropertyName = "MinimizeWorkingSetSize";
            this.checkbox_minimizememoryfootprint.Size = new System.Drawing.Size(492, 30);
            this.checkbox_minimizememoryfootprint.TabIndex = 8;
            this.checkbox_minimizememoryfootprint.Text = "Minimize memory footprint, but with a performance penalty (not advised).";
            this.checkbox_minimizememoryfootprint.UseVisualStyleBackColor = true;
            // 
            // checkbox_checkunstableupdates
            // 
            this.checkbox_checkunstableupdates.LanguageKey = "expertsettings_checkunstableupdates";
            this.checkbox_checkunstableupdates.Location = new System.Drawing.Point(12, 234);
            this.checkbox_checkunstableupdates.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_checkunstableupdates.Name = "checkbox_checkunstableupdates";
            this.checkbox_checkunstableupdates.PropertyName = "CheckForUnstable";
            this.checkbox_checkunstableupdates.Size = new System.Drawing.Size(492, 30);
            this.checkbox_checkunstableupdates.TabIndex = 7;
            this.checkbox_checkunstableupdates.Text = "Check for unstable updates";
            this.checkbox_checkunstableupdates.UseVisualStyleBackColor = true;
            // 
            // checkbox_suppresssavedialogatclose
            // 
            this.checkbox_suppresssavedialogatclose.LanguageKey = "expertsettings_suppresssavedialogatclose";
            this.checkbox_suppresssavedialogatclose.Location = new System.Drawing.Point(12, 210);
            this.checkbox_suppresssavedialogatclose.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_suppresssavedialogatclose.Name = "checkbox_suppresssavedialogatclose";
            this.checkbox_suppresssavedialogatclose.PropertyName = "SuppressSaveDialogAtClose";
            this.checkbox_suppresssavedialogatclose.SectionName = "Editor";
            this.checkbox_suppresssavedialogatclose.Size = new System.Drawing.Size(492, 30);
            this.checkbox_suppresssavedialogatclose.TabIndex = 6;
            this.checkbox_suppresssavedialogatclose.Text = "Suppress the save dialog when closing the editor";
            this.checkbox_suppresssavedialogatclose.UseVisualStyleBackColor = true;
            // 
            // label_counter
            // 
            this.label_counter.AutoSize = true;
            this.label_counter.LanguageKey = "expertsettings_counter";
            this.label_counter.Location = new System.Drawing.Point(9, 356);
            this.label_counter.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_counter.Name = "label_counter";
            this.label_counter.Size = new System.Drawing.Size(399, 15);
            this.label_counter.TabIndex = 27;
            this.label_counter.Text = "The number for the ${NUM} in the filename pattern";
            // 
            // textbox_counter
            // 
            this.textbox_counter.Location = new System.Drawing.Point(324, 352);
            this.textbox_counter.Margin = new System.Windows.Forms.Padding(4);
            this.textbox_counter.Name = "textbox_counter";
            this.textbox_counter.PropertyName = "OutputFileIncrementingNumber";
            this.textbox_counter.Size = new System.Drawing.Size(175, 25);
            this.textbox_counter.TabIndex = 11;
            // 
            // label_footerpattern
            // 
            this.label_footerpattern.AutoSize = true;
            this.label_footerpattern.LanguageKey = "expertsettings_footerpattern";
            this.label_footerpattern.Location = new System.Drawing.Point(9, 324);
            this.label_footerpattern.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_footerpattern.Name = "label_footerpattern";
            this.label_footerpattern.Size = new System.Drawing.Size(183, 15);
            this.label_footerpattern.TabIndex = 25;
            this.label_footerpattern.Text = "Printer footer pattern";
            // 
            // textbox_footerpattern
            // 
            this.textbox_footerpattern.Location = new System.Drawing.Point(172, 320);
            this.textbox_footerpattern.Margin = new System.Windows.Forms.Padding(4);
            this.textbox_footerpattern.Name = "textbox_footerpattern";
            this.textbox_footerpattern.PropertyName = "OutputPrintFooterPattern";
            this.textbox_footerpattern.Size = new System.Drawing.Size(326, 25);
            this.textbox_footerpattern.TabIndex = 10;
            // 
            // checkbox_thumbnailpreview
            // 
            this.checkbox_thumbnailpreview.LanguageKey = "expertsettings_thumbnailpreview";
            this.checkbox_thumbnailpreview.Location = new System.Drawing.Point(12, 186);
            this.checkbox_thumbnailpreview.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_thumbnailpreview.Name = "checkbox_thumbnailpreview";
            this.checkbox_thumbnailpreview.PropertyName = "ThumnailPreview";
            this.checkbox_thumbnailpreview.Size = new System.Drawing.Size(492, 30);
            this.checkbox_thumbnailpreview.TabIndex = 5;
            this.checkbox_thumbnailpreview.Text = "Show window thumbnails in context menu (for Vista and windows 7)";
            this.checkbox_thumbnailpreview.UseVisualStyleBackColor = true;
            // 
            // checkbox_optimizeforrdp
            // 
            this.checkbox_optimizeforrdp.LanguageKey = "expertsettings_optimizeforrdp";
            this.checkbox_optimizeforrdp.Location = new System.Drawing.Point(12, 162);
            this.checkbox_optimizeforrdp.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_optimizeforrdp.Name = "checkbox_optimizeforrdp";
            this.checkbox_optimizeforrdp.PropertyName = "OptimizeForRDP";
            this.checkbox_optimizeforrdp.Size = new System.Drawing.Size(492, 30);
            this.checkbox_optimizeforrdp.TabIndex = 4;
            this.checkbox_optimizeforrdp.Text = "Make some optimizations for usage with remote desktop";
            this.checkbox_optimizeforrdp.UseVisualStyleBackColor = true;
            // 
            // checkbox_autoreducecolors
            // 
            this.checkbox_autoreducecolors.LanguageKey = "expertsettings_autoreducecolors";
            this.checkbox_autoreducecolors.Location = new System.Drawing.Point(12, 139);
            this.checkbox_autoreducecolors.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_autoreducecolors.Name = "checkbox_autoreducecolors";
            this.checkbox_autoreducecolors.PropertyName = "OutputFileAutoReduceColors";
            this.checkbox_autoreducecolors.Size = new System.Drawing.Size(510, 30);
            this.checkbox_autoreducecolors.TabIndex = 3;
            this.checkbox_autoreducecolors.Text = "Create an 8-bit image if the colors are less than 256 while having a > 8 bits ima" +
    "ge";
            this.checkbox_autoreducecolors.UseVisualStyleBackColor = true;
            // 
            // label_clipboardformats
            // 
            this.label_clipboardformats.AutoSize = true;
            this.label_clipboardformats.LanguageKey = "expertsettings_clipboardformats";
            this.label_clipboardformats.Location = new System.Drawing.Point(9, 49);
            this.label_clipboardformats.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_clipboardformats.Name = "label_clipboardformats";
            this.label_clipboardformats.Size = new System.Drawing.Size(143, 15);
            this.label_clipboardformats.TabIndex = 20;
            this.label_clipboardformats.Text = "Clipboard formats";
            // 
            // checkbox_enableexpert
            // 
            this.checkbox_enableexpert.LanguageKey = "expertsettings_enableexpert";
            this.checkbox_enableexpert.Location = new System.Drawing.Point(8, 18);
            this.checkbox_enableexpert.Margin = new System.Windows.Forms.Padding(4);
            this.checkbox_enableexpert.Name = "checkbox_enableexpert";
            this.checkbox_enableexpert.Size = new System.Drawing.Size(492, 30);
            this.checkbox_enableexpert.TabIndex = 1;
            this.checkbox_enableexpert.Text = "I know what I am doing!";
            this.checkbox_enableexpert.UseVisualStyleBackColor = true;
            this.checkbox_enableexpert.CheckedChanged += new System.EventHandler(this.checkbox_enableexpert_CheckedChanged);
            // 
            // listview_clipboardformats
            // 
            this.listview_clipboardformats.Alignment = System.Windows.Forms.ListViewAlignment.Left;
            this.listview_clipboardformats.AutoArrange = false;
            this.listview_clipboardformats.CheckBoxes = true;
            this.listview_clipboardformats.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listview_clipboardformats.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listview_clipboardformats.LabelWrap = false;
            this.listview_clipboardformats.Location = new System.Drawing.Point(212, 48);
            this.listview_clipboardformats.Margin = new System.Windows.Forms.Padding(4);
            this.listview_clipboardformats.Name = "listview_clipboardformats";
            this.listview_clipboardformats.ShowGroups = false;
            this.listview_clipboardformats.Size = new System.Drawing.Size(286, 89);
            this.listview_clipboardformats.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listview_clipboardformats.TabIndex = 2;
            this.listview_clipboardformats.UseCompatibleStateImageBehavior = false;
            this.listview_clipboardformats.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Destination";
            this.columnHeader1.Width = 225;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(564, 539);
            this.Controls.Add(this.tabcontrol);
            this.Controls.Add(this.settings_confirm);
            this.Controls.Add(this.settings_cancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.LanguageKey = "settings_title";
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings";
            this.groupbox_preferredfilesettings.ResumeLayout(false);
            this.groupbox_preferredfilesettings.PerformLayout();
            this.groupbox_applicationsettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpdownIconSize)).EndInit();
            this.groupbox_qualitysettings.ResumeLayout(false);
            this.groupbox_qualitysettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarJpegQuality)).EndInit();
            this.groupbox_destination.ResumeLayout(false);
            this.tabcontrol.ResumeLayout(false);
            this.tab_general.ResumeLayout(false);
            this.groupbox_network.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_daysbetweencheck)).EndInit();
            this.groupbox_hotkeys.ResumeLayout(false);
            this.groupbox_hotkeys.PerformLayout();
            this.tab_capture.ResumeLayout(false);
            this.groupbox_editor.ResumeLayout(false);
            this.groupbox_iecapture.ResumeLayout(false);
            this.groupbox_windowscapture.ResumeLayout(false);
            this.groupbox_windowscapture.PerformLayout();
            this.groupbox_capture.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownWaitTime)).EndInit();
            this.tab_output.ResumeLayout(false);
            this.tab_destinations.ResumeLayout(false);
            this.groupDefaultEffects.ResumeLayout(false);
            this.groupDefaultEffects.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtEffectBorderWidth)).EndInit();
            this.tab_printer.ResumeLayout(false);
            this.tab_printer.PerformLayout();
            this.groupBoxColors.ResumeLayout(false);
            this.groupBoxColors.PerformLayout();
            this.groupBoxPrintLayout.ResumeLayout(false);
            this.groupBoxPrintLayout.PerformLayout();
            this.tab_plugins.ResumeLayout(false);
            this.groupbox_plugins.ResumeLayout(false);
            this.groupbox_plugins.PerformLayout();
            this.tab_expert.ResumeLayout(false);
            this.groupbox_expert.ResumeLayout(false);
            this.groupbox_expert.PerformLayout();
            this.ResumeLayout(false);

		}
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_notifications;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_minimizememoryfootprint;
		private System.Windows.Forms.ColumnHeader destination;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_picker;
		private System.Windows.Forms.ListView listview_destinations;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_editor;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_editor_match_capture_size;
		private System.Windows.Forms.NumericUpDown numericUpDown_daysbetweencheck;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_network;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_usedefaultproxy;
		private GreenshotPlugin.Controls.GreenshotLabel label_checkperiod;
		private GreenshotPlugin.Controls.HotkeyControl fullscreen_hotkeyControl;
		private GreenshotPlugin.Controls.HotkeyControl window_hotkeyControl;
		private GreenshotPlugin.Controls.HotkeyControl region_hotkeyControl;
		private GreenshotPlugin.Controls.GreenshotLabel label_fullscreen_hotkey;
		private GreenshotPlugin.Controls.GreenshotLabel label_window_hotkey;
		private GreenshotPlugin.Controls.GreenshotLabel label_region_hotkey;
		private GreenshotPlugin.Controls.HotkeyControl ie_hotkeyControl;
		private GreenshotPlugin.Controls.GreenshotLabel label_ie_hotkey;
		private GreenshotPlugin.Controls.HotkeyControl lastregion_hotkeyControl;
		private GreenshotPlugin.Controls.GreenshotLabel label_lastregion_hotkey;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_hotkeys;
		private Greenshot.Controls.ColorButton colorButton_window_background;
		private GreenshotPlugin.Controls.GreenshotRadioButton radiobuttonWindowCapture;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_ie_capture;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_capture;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_windowscapture;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_iecapture;
		private GreenshotPlugin.Controls.GreenshotTabPage tab_capture;
		private System.Windows.Forms.ComboBox combobox_window_capture_mode;
		private System.Windows.Forms.NumericUpDown numericUpDownWaitTime;
		private GreenshotPlugin.Controls.GreenshotLabel label_waittime;
		private GreenshotPlugin.Controls.GreenshotRadioButton radiobuttonInteractiveCapture;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_capture_mousepointer;
		private GreenshotPlugin.Controls.GreenshotTabPage tab_printer;
		private System.Windows.Forms.ListView listview_plugins;
		private GreenshotPlugin.Controls.GreenshotButton button_pluginconfigure;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_plugins;
		private GreenshotPlugin.Controls.GreenshotTabPage tab_plugins;
		private System.Windows.Forms.Button btnPatternHelp;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_copypathtoclipboard;
		private GreenshotPlugin.Controls.GreenshotTabPage tab_output;
		private GreenshotPlugin.Controls.GreenshotTabPage tab_general;
		private System.Windows.Forms.TabControl tabcontrol;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_autostartshortcut;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_destination;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_alwaysshowqualitydialog;
		private System.Windows.Forms.TextBox textBoxJpegQuality;
		private GreenshotPlugin.Controls.GreenshotLabel label_jpegquality;
		private System.Windows.Forms.TrackBar trackBarJpegQuality;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_qualitysettings;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_applicationsettings;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_preferredfilesettings;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_playsound;
		private GreenshotPlugin.Controls.GreenshotLabel label_primaryimageformat;
		private GreenshotPlugin.Controls.GreenshotComboBox combobox_primaryimageformat;
		private System.Windows.Forms.ComboBox combobox_language;
		private GreenshotPlugin.Controls.GreenshotLabel label_language;
		private GreenshotPlugin.Controls.GreenshotTextBox textbox_screenshotname;
		private GreenshotPlugin.Controls.GreenshotLabel label_screenshotname;
		private System.Windows.Forms.Button browse;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
		private GreenshotPlugin.Controls.GreenshotButton settings_cancel;
		private GreenshotPlugin.Controls.GreenshotButton settings_confirm;
		private GreenshotPlugin.Controls.GreenshotTextBox textbox_storagelocation;
		private GreenshotPlugin.Controls.GreenshotLabel label_storagelocation;
		private GreenshotPlugin.Controls.GreenshotTabPage tab_destinations;
		private GreenshotPlugin.Controls.GreenshotTabPage tab_expert;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupbox_expert;
		private GreenshotPlugin.Controls.GreenshotLabel label_clipboardformats;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_enableexpert;
		private System.Windows.Forms.ListView listview_clipboardformats;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_autoreducecolors;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_optimizeforrdp;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_thumbnailpreview;
		private GreenshotPlugin.Controls.GreenshotLabel label_footerpattern;
		private GreenshotPlugin.Controls.GreenshotTextBox textbox_footerpattern;
		private GreenshotPlugin.Controls.GreenshotLabel label_counter;
		private GreenshotPlugin.Controls.GreenshotTextBox textbox_counter;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_reducecolors;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_suppresssavedialogatclose;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_checkunstableupdates;
        private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_reuseeditor;
        private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_alwaysshowprintoptionsdialog;
        private GreenshotPlugin.Controls.GreenshotGroupBox groupBoxColors;
        private GreenshotPlugin.Controls.GreenshotCheckBox checkboxPrintInverted;
        private GreenshotPlugin.Controls.GreenshotRadioButton radioBtnColorPrint;
        private GreenshotPlugin.Controls.GreenshotRadioButton radioBtnGrayScale;
        private GreenshotPlugin.Controls.GreenshotRadioButton radioBtnMonochrome;
        private GreenshotPlugin.Controls.GreenshotGroupBox groupBoxPrintLayout;
        private GreenshotPlugin.Controls.GreenshotCheckBox checkboxDateTime;
        private GreenshotPlugin.Controls.GreenshotCheckBox checkboxAllowShrink;
        private GreenshotPlugin.Controls.GreenshotCheckBox checkboxAllowEnlarge;
        private GreenshotPlugin.Controls.GreenshotCheckBox checkboxAllowRotate;
        private GreenshotPlugin.Controls.GreenshotCheckBox checkboxAllowCenter;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_zoomer;
		private GreenshotPlugin.Controls.GreenshotLabel label_icon_size;
		private System.Windows.Forms.NumericUpDown numericUpdownIconSize;
        private Controls.ColorButton btnEffectBorderColor;
        private System.Windows.Forms.NumericUpDown txtEffectBorderWidth;
        private GreenshotPlugin.Controls.GreenshotGroupBox groupDefaultEffects;
        private GreenshotPlugin.Controls.GreenshotLabel greenshotLabel1;
        private GreenshotPlugin.Controls.GreenshotLabel greenshotLabel2;
        private GreenshotPlugin.Controls.GreenshotLabel greenshotLabel3;
    }
}
