/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
	partial class SettingsForm : System.Windows.Forms.Form {
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
			this.textbox_storagelocation = new System.Windows.Forms.TextBox();
			this.label_storagelocation = new System.Windows.Forms.Label();
			this.settings_cancel = new System.Windows.Forms.Button();
			this.settings_okay = new System.Windows.Forms.Button();
			this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
			this.browse = new System.Windows.Forms.Button();
			this.label_screenshotname = new System.Windows.Forms.Label();
			this.textbox_screenshotname = new System.Windows.Forms.TextBox();
			this.label_language = new System.Windows.Forms.Label();
			this.combobox_language = new System.Windows.Forms.ComboBox();
			this.combobox_primaryimageformat = new System.Windows.Forms.ComboBox();
			this.label_primaryimageformat = new System.Windows.Forms.Label();
			this.groupbox_preferredfilesettings = new System.Windows.Forms.GroupBox();
			this.btnPatternHelp = new System.Windows.Forms.Button();
			this.checkbox_copypathtoclipboard = new System.Windows.Forms.CheckBox();
			this.groupbox_visualisation = new System.Windows.Forms.GroupBox();
			this.checkbox_playsound = new System.Windows.Forms.CheckBox();
			this.checkbox_showflashlight = new System.Windows.Forms.CheckBox();
			this.groupbox_applicationsettings = new System.Windows.Forms.GroupBox();
			this.checkbox_autostartshortcut = new System.Windows.Forms.CheckBox();
			this.checkbox_registerhotkeys = new System.Windows.Forms.CheckBox();
			this.checkbox_editor = new System.Windows.Forms.CheckBox();
			this.groupbox_jpegsettings = new System.Windows.Forms.GroupBox();
			this.checkbox_alwaysshowjpegqualitydialog = new System.Windows.Forms.CheckBox();
			this.label_jpegquality = new System.Windows.Forms.Label();
			this.textBoxJpegQuality = new System.Windows.Forms.TextBox();
			this.trackBarJpegQuality = new System.Windows.Forms.TrackBar();
			this.checkbox_clipboard = new System.Windows.Forms.CheckBox();
			this.checkbox_file = new System.Windows.Forms.CheckBox();
			this.checkbox_printer = new System.Windows.Forms.CheckBox();
			this.groupbox_destination = new System.Windows.Forms.GroupBox();
			this.checkbox_email = new System.Windows.Forms.CheckBox();
			this.checkbox_fileas = new System.Windows.Forms.CheckBox();
			this.tabcontrol = new System.Windows.Forms.TabControl();
			this.tab_general = new System.Windows.Forms.TabPage();
			this.groupbox_capture = new System.Windows.Forms.GroupBox();
			this.checkbox_capture_window_content = new System.Windows.Forms.CheckBox();
			this.checkbox_capture_complete_windows = new System.Windows.Forms.CheckBox();
			this.label_waittime = new System.Windows.Forms.Label();
			this.numericUpDownWaitTime = new System.Windows.Forms.NumericUpDown();
			this.checkbox_capture_windows_interactive = new System.Windows.Forms.CheckBox();
			this.checkbox_capture_mousepointer = new System.Windows.Forms.CheckBox();
			this.tab_output = new System.Windows.Forms.TabPage();
			this.tab_printer = new System.Windows.Forms.TabPage();
			this.groupbox_printoptions = new System.Windows.Forms.GroupBox();
			this.checkbox_alwaysshowprintoptionsdialog = new System.Windows.Forms.CheckBox();
			this.checkboxTimestamp = new System.Windows.Forms.CheckBox();
			this.checkboxAllowCenter = new System.Windows.Forms.CheckBox();
			this.checkboxAllowRotate = new System.Windows.Forms.CheckBox();
			this.checkboxAllowEnlarge = new System.Windows.Forms.CheckBox();
			this.checkboxAllowShrink = new System.Windows.Forms.CheckBox();
			this.tab_plugins = new System.Windows.Forms.TabPage();
			this.groupbox_plugins = new System.Windows.Forms.GroupBox();
			this.listview_plugins = new System.Windows.Forms.ListView();
			this.button_pluginconfigure = new System.Windows.Forms.Button();
			this.groupbox_preferredfilesettings.SuspendLayout();
			this.groupbox_visualisation.SuspendLayout();
			this.groupbox_applicationsettings.SuspendLayout();
			this.groupbox_jpegsettings.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarJpegQuality)).BeginInit();
			this.groupbox_destination.SuspendLayout();
			this.tabcontrol.SuspendLayout();
			this.tab_general.SuspendLayout();
			this.groupbox_capture.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownWaitTime)).BeginInit();
			this.tab_output.SuspendLayout();
			this.tab_printer.SuspendLayout();
			this.groupbox_printoptions.SuspendLayout();
			this.tab_plugins.SuspendLayout();
			this.groupbox_plugins.SuspendLayout();
			this.SuspendLayout();
			// 
			// textbox_storagelocation
			// 
			this.textbox_storagelocation.Location = new System.Drawing.Point(138, 18);
			this.textbox_storagelocation.Name = "textbox_storagelocation";
			this.textbox_storagelocation.ReadOnly = true;
			this.textbox_storagelocation.Size = new System.Drawing.Size(233, 20);
			this.textbox_storagelocation.TabIndex = 12;
			// 
			// label_storagelocation
			// 
			this.label_storagelocation.Location = new System.Drawing.Point(6, 21);
			this.label_storagelocation.Name = "label_storagelocation";
			this.label_storagelocation.Size = new System.Drawing.Size(126, 23);
			this.label_storagelocation.TabIndex = 11;
			this.label_storagelocation.Text = "Storage Location";
			// 
			// settings_cancel
			// 
			this.settings_cancel.Location = new System.Drawing.Point(367, 366);
			this.settings_cancel.Name = "settings_cancel";
			this.settings_cancel.Size = new System.Drawing.Size(75, 23);
			this.settings_cancel.TabIndex = 7;
			this.settings_cancel.Text = "Cancel";
			this.settings_cancel.UseVisualStyleBackColor = true;
			this.settings_cancel.Click += new System.EventHandler(this.Settings_cancelClick);
			// 
			// settings_okay
			// 
			this.settings_okay.Location = new System.Drawing.Point(286, 366);
			this.settings_okay.Name = "settings_okay";
			this.settings_okay.Size = new System.Drawing.Size(75, 23);
			this.settings_okay.TabIndex = 6;
			this.settings_okay.Text = "OK";
			this.settings_okay.UseVisualStyleBackColor = true;
			this.settings_okay.Click += new System.EventHandler(this.Settings_okayClick);
			// 
			// browse
			// 
			this.browse.Location = new System.Drawing.Point(371, 17);
			this.browse.Name = "browse";
			this.browse.Size = new System.Drawing.Size(35, 23);
			this.browse.TabIndex = 1;
			this.browse.Text = "...";
			this.browse.UseVisualStyleBackColor = true;
			this.browse.Click += new System.EventHandler(this.BrowseClick);
			// 
			// label_screenshotname
			// 
			this.label_screenshotname.Location = new System.Drawing.Point(6, 44);
			this.label_screenshotname.Name = "label_screenshotname";
			this.label_screenshotname.Size = new System.Drawing.Size(126, 23);
			this.label_screenshotname.TabIndex = 9;
			this.label_screenshotname.Text = "Filename pattern";
			// 
			// textbox_screenshotname
			// 
			this.textbox_screenshotname.Location = new System.Drawing.Point(138, 41);
			this.textbox_screenshotname.Name = "textbox_screenshotname";
			this.textbox_screenshotname.Size = new System.Drawing.Size(233, 20);
			this.textbox_screenshotname.TabIndex = 2;
			// 
			// label_language
			// 
			this.label_language.Location = new System.Drawing.Point(6, 20);
			this.label_language.Name = "label_language";
			this.label_language.Size = new System.Drawing.Size(181, 23);
			this.label_language.TabIndex = 10;
			this.label_language.Text = "Language";
			// 
			// combobox_language
			// 
			this.combobox_language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.combobox_language.FormattingEnabled = true;
			this.combobox_language.Location = new System.Drawing.Point(193, 17);
			this.combobox_language.Name = "combobox_language";
			this.combobox_language.Size = new System.Drawing.Size(213, 21);
			this.combobox_language.TabIndex = 0;
			this.combobox_language.MaxDropDownItems = 15;
			// 
			// combobox_primaryimageformat
			// 
			this.combobox_primaryimageformat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.combobox_primaryimageformat.FormattingEnabled = true;
			this.combobox_primaryimageformat.Location = new System.Drawing.Point(138, 64);
			this.combobox_primaryimageformat.Name = "combobox_primaryimageformat";
			this.combobox_primaryimageformat.Size = new System.Drawing.Size(268, 21);
			this.combobox_primaryimageformat.TabIndex = 4;
			// 
			// label_primaryimageformat
			// 
			this.label_primaryimageformat.Location = new System.Drawing.Point(6, 67);
			this.label_primaryimageformat.Name = "label_primaryimageformat";
			this.label_primaryimageformat.Size = new System.Drawing.Size(126, 19);
			this.label_primaryimageformat.TabIndex = 8;
			this.label_primaryimageformat.Text = "Primary image format";
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
			this.groupbox_preferredfilesettings.Location = new System.Drawing.Point(2, 106);
			this.groupbox_preferredfilesettings.Name = "groupbox_preferredfilesettings";
			this.groupbox_preferredfilesettings.Size = new System.Drawing.Size(412, 122);
			this.groupbox_preferredfilesettings.TabIndex = 13;
			this.groupbox_preferredfilesettings.TabStop = false;
			this.groupbox_preferredfilesettings.Text = "Preferred Output File Settings";
			// 
			// btnPatternHelp
			// 
			this.btnPatternHelp.Location = new System.Drawing.Point(371, 39);
			this.btnPatternHelp.Name = "btnPatternHelp";
			this.btnPatternHelp.Size = new System.Drawing.Size(35, 23);
			this.btnPatternHelp.TabIndex = 19;
			this.btnPatternHelp.Text = "?";
			this.btnPatternHelp.UseVisualStyleBackColor = true;
			this.btnPatternHelp.Click += new System.EventHandler(this.BtnPatternHelpClick);
			// 
			// checkbox_copypathtoclipboard
			// 
			this.checkbox_copypathtoclipboard.Location = new System.Drawing.Point(12, 89);
			this.checkbox_copypathtoclipboard.Name = "checkbox_copypathtoclipboard";
			this.checkbox_copypathtoclipboard.Size = new System.Drawing.Size(394, 24);
			this.checkbox_copypathtoclipboard.TabIndex = 18;
			this.checkbox_copypathtoclipboard.Text = "Copy file path to clipboard every time an image is saved";
			this.checkbox_copypathtoclipboard.UseVisualStyleBackColor = true;
			// 
			// groupbox_visualisation
			// 
			this.groupbox_visualisation.Controls.Add(this.checkbox_playsound);
			this.groupbox_visualisation.Controls.Add(this.checkbox_showflashlight);
			this.groupbox_visualisation.Location = new System.Drawing.Point(2, 102);
			this.groupbox_visualisation.Name = "groupbox_visualisation";
			this.groupbox_visualisation.Size = new System.Drawing.Size(412, 69);
			this.groupbox_visualisation.TabIndex = 15;
			this.groupbox_visualisation.TabStop = false;
			this.groupbox_visualisation.Text = "Visualisation";
			// 
			// checkbox_playsound
			// 
			this.checkbox_playsound.Location = new System.Drawing.Point(12, 39);
			this.checkbox_playsound.Name = "checkbox_playsound";
			this.checkbox_playsound.Size = new System.Drawing.Size(394, 24);
			this.checkbox_playsound.TabIndex = 12;
			this.checkbox_playsound.Text = "Play camera sound";
			this.checkbox_playsound.UseVisualStyleBackColor = true;
			// 
			// checkbox_showflashlight
			// 
			this.checkbox_showflashlight.Location = new System.Drawing.Point(12, 18);
			this.checkbox_showflashlight.Name = "checkbox_showflashlight";
			this.checkbox_showflashlight.Size = new System.Drawing.Size(394, 24);
			this.checkbox_showflashlight.TabIndex = 11;
			this.checkbox_showflashlight.Text = "Show flashlight";
			this.checkbox_showflashlight.UseVisualStyleBackColor = true;
			// 
			// groupbox_applicationsettings
			// 
			this.groupbox_applicationsettings.Controls.Add(this.checkbox_autostartshortcut);
			this.groupbox_applicationsettings.Controls.Add(this.label_language);
			this.groupbox_applicationsettings.Controls.Add(this.combobox_language);
			this.groupbox_applicationsettings.Controls.Add(this.checkbox_registerhotkeys);
			this.groupbox_applicationsettings.Location = new System.Drawing.Point(2, 6);
			this.groupbox_applicationsettings.Name = "groupbox_applicationsettings";
			this.groupbox_applicationsettings.Size = new System.Drawing.Size(412, 95);
			this.groupbox_applicationsettings.TabIndex = 14;
			this.groupbox_applicationsettings.TabStop = false;
			this.groupbox_applicationsettings.Text = "Application Settings";
			// 
			// checkbox_autostartshortcut
			// 
			this.checkbox_autostartshortcut.Location = new System.Drawing.Point(12, 67);
			this.checkbox_autostartshortcut.Name = "checkbox_autostartshortcut";
			this.checkbox_autostartshortcut.Size = new System.Drawing.Size(394, 25);
			this.checkbox_autostartshortcut.TabIndex = 15;
			this.checkbox_autostartshortcut.Text = "Launch Greenshot on startup";
			this.checkbox_autostartshortcut.UseVisualStyleBackColor = true;
			// 
			// checkbox_registerhotkeys
			// 
			this.checkbox_registerhotkeys.Location = new System.Drawing.Point(12, 46);
			this.checkbox_registerhotkeys.Name = "checkbox_registerhotkeys";
			this.checkbox_registerhotkeys.Size = new System.Drawing.Size(394, 25);
			this.checkbox_registerhotkeys.TabIndex = 14;
			this.checkbox_registerhotkeys.Text = "Register Hotkeys";
			this.checkbox_registerhotkeys.UseVisualStyleBackColor = true;
			// 
			// checkbox_editor
			// 
			this.checkbox_editor.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_editor.Location = new System.Drawing.Point(12, 19);
			this.checkbox_editor.Name = "checkbox_editor";
			this.checkbox_editor.Size = new System.Drawing.Size(158, 24);
			this.checkbox_editor.TabIndex = 14;
			this.checkbox_editor.Text = "Open in editor";
			this.checkbox_editor.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_editor.UseVisualStyleBackColor = true;
			// 
			// groupbox_jpegsettings
			// 
			this.groupbox_jpegsettings.Controls.Add(this.checkbox_alwaysshowjpegqualitydialog);
			this.groupbox_jpegsettings.Controls.Add(this.label_jpegquality);
			this.groupbox_jpegsettings.Controls.Add(this.textBoxJpegQuality);
			this.groupbox_jpegsettings.Controls.Add(this.trackBarJpegQuality);
			this.groupbox_jpegsettings.Location = new System.Drawing.Point(2, 234);
			this.groupbox_jpegsettings.Name = "groupbox_jpegsettings";
			this.groupbox_jpegsettings.Size = new System.Drawing.Size(412, 83);
			this.groupbox_jpegsettings.TabIndex = 14;
			this.groupbox_jpegsettings.TabStop = false;
			this.groupbox_jpegsettings.Text = "JPEG Settings";
			// 
			// checkbox_alwaysshowjpegqualitydialog
			// 
			this.checkbox_alwaysshowjpegqualitydialog.Location = new System.Drawing.Point(12, 50);
			this.checkbox_alwaysshowjpegqualitydialog.Name = "checkbox_alwaysshowjpegqualitydialog";
			this.checkbox_alwaysshowjpegqualitydialog.Size = new System.Drawing.Size(394, 25);
			this.checkbox_alwaysshowjpegqualitydialog.TabIndex = 16;
			this.checkbox_alwaysshowjpegqualitydialog.Text = "Show quality dialog every time a JPEG image is saved";
			this.checkbox_alwaysshowjpegqualitydialog.UseVisualStyleBackColor = true;
			// 
			// label_jpegquality
			// 
			this.label_jpegquality.Location = new System.Drawing.Point(6, 24);
			this.label_jpegquality.Name = "label_jpegquality";
			this.label_jpegquality.Size = new System.Drawing.Size(116, 23);
			this.label_jpegquality.TabIndex = 13;
			this.label_jpegquality.Text = "JPEG Quality";
			// 
			// textBoxJpegQuality
			// 
			this.textBoxJpegQuality.Location = new System.Drawing.Point(371, 21);
			this.textBoxJpegQuality.Name = "textBoxJpegQuality";
			this.textBoxJpegQuality.ReadOnly = true;
			this.textBoxJpegQuality.Size = new System.Drawing.Size(35, 20);
			this.textBoxJpegQuality.TabIndex = 13;
			this.textBoxJpegQuality.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// trackBarJpegQuality
			// 
			this.trackBarJpegQuality.BackColor = System.Drawing.SystemColors.Control;
			this.trackBarJpegQuality.LargeChange = 10;
			this.trackBarJpegQuality.Location = new System.Drawing.Point(138, 21);
			this.trackBarJpegQuality.Maximum = 100;
			this.trackBarJpegQuality.Name = "trackBarJpegQuality";
			this.trackBarJpegQuality.Size = new System.Drawing.Size(233, 42);
			this.trackBarJpegQuality.TabIndex = 0;
			this.trackBarJpegQuality.TickFrequency = 10;
			this.trackBarJpegQuality.Scroll += new System.EventHandler(this.TrackBarJpegQualityScroll);
			// 
			// checkbox_clipboard
			// 
			this.checkbox_clipboard.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_clipboard.Location = new System.Drawing.Point(12, 41);
			this.checkbox_clipboard.Name = "checkbox_clipboard";
			this.checkbox_clipboard.Size = new System.Drawing.Size(158, 24);
			this.checkbox_clipboard.TabIndex = 15;
			this.checkbox_clipboard.Text = "Copy to clipboard";
			this.checkbox_clipboard.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_clipboard.UseVisualStyleBackColor = true;
			// 
			// checkbox_file
			// 
			this.checkbox_file.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_file.Location = new System.Drawing.Point(176, 42);
			this.checkbox_file.Name = "checkbox_file";
			this.checkbox_file.Size = new System.Drawing.Size(230, 23);
			this.checkbox_file.TabIndex = 16;
			this.checkbox_file.Text = "Sofort speichern (mit nachstehend angegebenen Einstellungen)";
			this.checkbox_file.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_file.UseVisualStyleBackColor = true;
			// 
			// checkbox_printer
			// 
			this.checkbox_printer.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_printer.Location = new System.Drawing.Point(12, 63);
			this.checkbox_printer.Name = "checkbox_printer";
			this.checkbox_printer.Size = new System.Drawing.Size(158, 24);
			this.checkbox_printer.TabIndex = 17;
			this.checkbox_printer.Text = "Send to printer";
			this.checkbox_printer.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_printer.UseVisualStyleBackColor = true;
			// 
			// groupbox_destination
			// 
			this.groupbox_destination.Controls.Add(this.checkbox_email);
			this.groupbox_destination.Controls.Add(this.checkbox_fileas);
			this.groupbox_destination.Controls.Add(this.checkbox_printer);
			this.groupbox_destination.Controls.Add(this.checkbox_editor);
			this.groupbox_destination.Controls.Add(this.checkbox_file);
			this.groupbox_destination.Controls.Add(this.checkbox_clipboard);
			this.groupbox_destination.Location = new System.Drawing.Point(2, 6);
			this.groupbox_destination.Name = "groupbox_destination";
			this.groupbox_destination.Size = new System.Drawing.Size(412, 94);
			this.groupbox_destination.TabIndex = 16;
			this.groupbox_destination.TabStop = false;
			this.groupbox_destination.Text = "Screenshot Destination";
			// 
			// checkbox_email
			// 
			this.checkbox_email.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_email.Location = new System.Drawing.Point(176, 63);
			this.checkbox_email.Name = "checkbox_email";
			this.checkbox_email.Size = new System.Drawing.Size(230, 23);
			this.checkbox_email.TabIndex = 19;
			this.checkbox_email.Text = "Email";
			this.checkbox_email.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_email.UseVisualStyleBackColor = true;
			// 
			// checkbox_fileas
			// 
			this.checkbox_fileas.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_fileas.Location = new System.Drawing.Point(176, 19);
			this.checkbox_fileas.Name = "checkbox_fileas";
			this.checkbox_fileas.Size = new System.Drawing.Size(230, 24);
			this.checkbox_fileas.TabIndex = 18;
			this.checkbox_fileas.Text = "Save as (displaying dialog)";
			this.checkbox_fileas.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_fileas.UseVisualStyleBackColor = true;
			// 
			// tabcontrol
			// 
			this.tabcontrol.Controls.Add(this.tab_general);
			this.tabcontrol.Controls.Add(this.tab_output);
			this.tabcontrol.Controls.Add(this.tab_printer);
			this.tabcontrol.Location = new System.Drawing.Point(12, 13);
			this.tabcontrol.Name = "tabcontrol";
			this.tabcontrol.SelectedIndex = 0;
			this.tabcontrol.Size = new System.Drawing.Size(431, 346);
			this.tabcontrol.TabIndex = 17;
			// 
			// tab_general
			// 
			this.tab_general.BackColor = System.Drawing.Color.Transparent;
			this.tab_general.Controls.Add(this.groupbox_capture);
			this.tab_general.Controls.Add(this.groupbox_applicationsettings);
			this.tab_general.Controls.Add(this.groupbox_visualisation);
			this.tab_general.Location = new System.Drawing.Point(4, 22);
			this.tab_general.Name = "tab_general";
			this.tab_general.Padding = new System.Windows.Forms.Padding(3);
			this.tab_general.Size = new System.Drawing.Size(423, 320);
			this.tab_general.TabIndex = 0;
			this.tab_general.Text = "General";
			this.tab_general.UseVisualStyleBackColor = true;
			// 
			// groupbox_capture
			// 
			this.groupbox_capture.Controls.Add(this.checkbox_capture_window_content);
			this.groupbox_capture.Controls.Add(this.checkbox_capture_complete_windows);
			this.groupbox_capture.Controls.Add(this.label_waittime);
			this.groupbox_capture.Controls.Add(this.numericUpDownWaitTime);
			this.groupbox_capture.Controls.Add(this.checkbox_capture_windows_interactive);
			this.groupbox_capture.Controls.Add(this.checkbox_capture_mousepointer);
			this.groupbox_capture.Location = new System.Drawing.Point(2, 174);
			this.groupbox_capture.Name = "groupbox_capture";
			this.groupbox_capture.Size = new System.Drawing.Size(412, 140);
			this.groupbox_capture.TabIndex = 16;
			this.groupbox_capture.TabStop = false;
			this.groupbox_capture.Text = "Capture";
			// 
			// checkbox_capture_window_content
			// 
			this.checkbox_capture_window_content.Location = new System.Drawing.Point(12, 104);
			this.checkbox_capture_window_content.Name = "checkbox_capture_window_content";
			this.checkbox_capture_window_content.Size = new System.Drawing.Size(394, 24);
			this.checkbox_capture_window_content.TabIndex = 16;
			this.checkbox_capture_window_content.Text = "Capture content only by supported applications";
			this.checkbox_capture_window_content.UseVisualStyleBackColor = true;
			// 
			// checkbox_capture_complete_windows
			// 
			this.checkbox_capture_complete_windows.Location = new System.Drawing.Point(12, 84);
			this.checkbox_capture_complete_windows.Name = "checkbox_capture_complete_windows";
			this.checkbox_capture_complete_windows.Size = new System.Drawing.Size(394, 24);
			this.checkbox_capture_complete_windows.TabIndex = 15;
			this.checkbox_capture_complete_windows.Text = "Capture window even if obstructed";
			this.checkbox_capture_complete_windows.UseVisualStyleBackColor = true;
			// 
			// label_waittime
			// 
			this.label_waittime.Location = new System.Drawing.Point(75, 64);
			this.label_waittime.Name = "label_waittime";
			this.label_waittime.Size = new System.Drawing.Size(331, 16);
			this.label_waittime.TabIndex = 14;
			this.label_waittime.Text = "Wait before capture (ms)";
			// 
			// numericUpDownWaitTime
			// 
			this.numericUpDownWaitTime.Increment = new decimal(new int[] {
									100,
									0,
									0,
									0});
			this.numericUpDownWaitTime.Location = new System.Drawing.Point(12, 62);
			this.numericUpDownWaitTime.Maximum = new decimal(new int[] {
									10000,
									0,
									0,
									0});
			this.numericUpDownWaitTime.Name = "numericUpDownWaitTime";
			this.numericUpDownWaitTime.Size = new System.Drawing.Size(57, 20);
			this.numericUpDownWaitTime.TabIndex = 13;
			this.numericUpDownWaitTime.ThousandsSeparator = true;
			// 
			// checkbox_capture_windows_interactive
			// 
			this.checkbox_capture_windows_interactive.Location = new System.Drawing.Point(12, 38);
			this.checkbox_capture_windows_interactive.Name = "checkbox_capture_windows_interactive";
			this.checkbox_capture_windows_interactive.Size = new System.Drawing.Size(394, 24);
			this.checkbox_capture_windows_interactive.TabIndex = 12;
			this.checkbox_capture_windows_interactive.Text = "Interactiv window capture";
			this.checkbox_capture_windows_interactive.UseVisualStyleBackColor = true;
			// 
			// checkbox_capture_mousepointer
			// 
			this.checkbox_capture_mousepointer.Location = new System.Drawing.Point(12, 18);
			this.checkbox_capture_mousepointer.Name = "checkbox_capture_mousepointer";
			this.checkbox_capture_mousepointer.Size = new System.Drawing.Size(394, 24);
			this.checkbox_capture_mousepointer.TabIndex = 11;
			this.checkbox_capture_mousepointer.Text = "Capture mousepointer";
			this.checkbox_capture_mousepointer.UseVisualStyleBackColor = true;
			// 
			// tab_output
			// 
			this.tab_output.BackColor = System.Drawing.Color.Transparent;
			this.tab_output.Controls.Add(this.groupbox_destination);
			this.tab_output.Controls.Add(this.groupbox_jpegsettings);
			this.tab_output.Controls.Add(this.groupbox_preferredfilesettings);
			this.tab_output.Location = new System.Drawing.Point(4, 22);
			this.tab_output.Name = "tab_output";
			this.tab_output.Padding = new System.Windows.Forms.Padding(3);
			this.tab_output.Size = new System.Drawing.Size(423, 320);
			this.tab_output.TabIndex = 1;
			this.tab_output.Text = "Output";
			this.tab_output.UseVisualStyleBackColor = true;
			// 
			// tab_printer
			// 
			this.tab_printer.Controls.Add(this.groupbox_printoptions);
			this.tab_printer.Location = new System.Drawing.Point(4, 22);
			this.tab_printer.Name = "tab_printer";
			this.tab_printer.Padding = new System.Windows.Forms.Padding(3);
			this.tab_printer.Size = new System.Drawing.Size(423, 320);
			this.tab_printer.TabIndex = 2;
			this.tab_printer.Text = "Printer";
			this.tab_printer.UseVisualStyleBackColor = true;
			// 
			// groupbox_printoptions
			// 
			this.groupbox_printoptions.Controls.Add(this.checkbox_alwaysshowprintoptionsdialog);
			this.groupbox_printoptions.Controls.Add(this.checkboxTimestamp);
			this.groupbox_printoptions.Controls.Add(this.checkboxAllowCenter);
			this.groupbox_printoptions.Controls.Add(this.checkboxAllowRotate);
			this.groupbox_printoptions.Controls.Add(this.checkboxAllowEnlarge);
			this.groupbox_printoptions.Controls.Add(this.checkboxAllowShrink);
			this.groupbox_printoptions.Location = new System.Drawing.Point(2, 6);
			this.groupbox_printoptions.Name = "groupbox_printoptions";
			this.groupbox_printoptions.Size = new System.Drawing.Size(412, 170);
			this.groupbox_printoptions.TabIndex = 18;
			this.groupbox_printoptions.TabStop = false;
			this.groupbox_printoptions.Text = "Print options";
			// 
			// checkbox_alwaysshowprintoptionsdialog
			// 
			this.checkbox_alwaysshowprintoptionsdialog.Location = new System.Drawing.Point(12, 138);
			this.checkbox_alwaysshowprintoptionsdialog.Name = "checkbox_alwaysshowprintoptionsdialog";
			this.checkbox_alwaysshowprintoptionsdialog.Size = new System.Drawing.Size(394, 19);
			this.checkbox_alwaysshowprintoptionsdialog.TabIndex = 17;
			this.checkbox_alwaysshowprintoptionsdialog.Text = "Show print options dialog every time an image is printed";
			this.checkbox_alwaysshowprintoptionsdialog.UseVisualStyleBackColor = true;
			// 
			// checkboxTimestamp
			// 
			this.checkboxTimestamp.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxTimestamp.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxTimestamp.Location = new System.Drawing.Point(12, 116);
			this.checkboxTimestamp.Name = "checkboxTimestamp";
			this.checkboxTimestamp.Size = new System.Drawing.Size(394, 16);
			this.checkboxTimestamp.TabIndex = 30;
			this.checkboxTimestamp.Text = "Print date / time at bottom of page";
			this.checkboxTimestamp.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxTimestamp.UseVisualStyleBackColor = true;
			// 
			// checkboxAllowCenter
			// 
			this.checkboxAllowCenter.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowCenter.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowCenter.Location = new System.Drawing.Point(12, 92);
			this.checkboxAllowCenter.Name = "checkboxAllowCenter";
			this.checkboxAllowCenter.Size = new System.Drawing.Size(394, 18);
			this.checkboxAllowCenter.TabIndex = 29;
			this.checkboxAllowCenter.Text = "Center printout on page";
			this.checkboxAllowCenter.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowCenter.UseVisualStyleBackColor = true;
			// 
			// checkboxAllowRotate
			// 
			this.checkboxAllowRotate.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowRotate.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowRotate.Location = new System.Drawing.Point(12, 69);
			this.checkboxAllowRotate.Name = "checkboxAllowRotate";
			this.checkboxAllowRotate.Size = new System.Drawing.Size(394, 17);
			this.checkboxAllowRotate.TabIndex = 28;
			this.checkboxAllowRotate.Text = "Rotate printouts to page orientation.";
			this.checkboxAllowRotate.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowRotate.UseVisualStyleBackColor = true;
			// 
			// checkboxAllowEnlarge
			// 
			this.checkboxAllowEnlarge.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowEnlarge.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowEnlarge.Location = new System.Drawing.Point(12, 45);
			this.checkboxAllowEnlarge.Name = "checkboxAllowEnlarge";
			this.checkboxAllowEnlarge.Size = new System.Drawing.Size(394, 19);
			this.checkboxAllowEnlarge.TabIndex = 27;
			this.checkboxAllowEnlarge.Text = "Enlarge small printouts to paper size.";
			this.checkboxAllowEnlarge.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowEnlarge.UseVisualStyleBackColor = true;
			// 
			// checkboxAllowShrink
			// 
			this.checkboxAllowShrink.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowShrink.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowShrink.Location = new System.Drawing.Point(12, 22);
			this.checkboxAllowShrink.Name = "checkboxAllowShrink";
			this.checkboxAllowShrink.Size = new System.Drawing.Size(394, 17);
			this.checkboxAllowShrink.TabIndex = 26;
			this.checkboxAllowShrink.Text = "Shrink large printouts to paper size.";
			this.checkboxAllowShrink.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowShrink.UseVisualStyleBackColor = true;
			// 
			// tab_plugins
			// 
			this.tab_plugins.Controls.Add(this.groupbox_plugins);
			this.tab_plugins.Location = new System.Drawing.Point(4, 22);
			this.tab_plugins.Name = "tab_plugins";
			this.tab_plugins.Size = new System.Drawing.Size(423, 500);
			this.tab_plugins.TabIndex = 2;
			this.tab_plugins.Text = "Plugins";
			this.tab_plugins.UseVisualStyleBackColor = true;
			// 
			// groupbox_plugins
			// 
			this.groupbox_plugins.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.groupbox_plugins.Controls.Add(this.listview_plugins);
			this.groupbox_plugins.Controls.Add(this.button_pluginconfigure);
			this.groupbox_plugins.Location = new System.Drawing.Point(3, 3);
			this.groupbox_plugins.Name = "groupbox_plugins";
			this.groupbox_plugins.Size = new System.Drawing.Size(417, 494);
			this.groupbox_plugins.TabIndex = 0;
			this.groupbox_plugins.TabStop = false;
			this.groupbox_plugins.Text = "Plugin settings";
			// 
			// listview_plugins
			// 
			this.listview_plugins.FullRowSelect = true;
			this.listview_plugins.Location = new System.Drawing.Point(7, 19);
			this.listview_plugins.Name = "listview_plugins";
			this.listview_plugins.Size = new System.Drawing.Size(404, 175);
			this.listview_plugins.TabIndex = 2;
			this.listview_plugins.UseCompatibleStateImageBehavior = false;
			this.listview_plugins.View = System.Windows.Forms.View.Details;
			this.listview_plugins.SelectedIndexChanged += new System.EventHandler(this.Listview_pluginsSelectedIndexChanged);
			this.listview_plugins.Click += new System.EventHandler(this.Listview_pluginsSelectedIndexChanged);
			// 
			// button_pluginconfigure
			// 
			this.button_pluginconfigure.Enabled = false;
			this.button_pluginconfigure.Location = new System.Drawing.Point(7, 200);
			this.button_pluginconfigure.Name = "button_pluginconfigure";
			this.button_pluginconfigure.Size = new System.Drawing.Size(75, 23);
			this.button_pluginconfigure.TabIndex = 1;
			this.button_pluginconfigure.Text = "Configure";
			this.button_pluginconfigure.UseVisualStyleBackColor = true;
			this.button_pluginconfigure.Click += new System.EventHandler(this.Button_pluginconfigureClick);
			// 
			// SettingsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(451, 396);
			this.Controls.Add(this.tabcontrol);
			this.Controls.Add(this.settings_okay);
			this.Controls.Add(this.settings_cancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SettingsForm";
			this.Text = "SettingsForm";
			this.groupbox_preferredfilesettings.ResumeLayout(false);
			this.groupbox_preferredfilesettings.PerformLayout();
			this.groupbox_visualisation.ResumeLayout(false);
			this.groupbox_applicationsettings.ResumeLayout(false);
			this.groupbox_jpegsettings.ResumeLayout(false);
			this.groupbox_jpegsettings.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarJpegQuality)).EndInit();
			this.groupbox_destination.ResumeLayout(false);
			this.tabcontrol.ResumeLayout(false);
			this.tab_general.ResumeLayout(false);
			this.groupbox_capture.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownWaitTime)).EndInit();
			this.tab_output.ResumeLayout(false);
			this.tab_printer.ResumeLayout(false);
			this.groupbox_printoptions.ResumeLayout(false);
			this.tab_plugins.ResumeLayout(false);
			this.groupbox_plugins.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.CheckBox checkbox_capture_window_content;
		private System.Windows.Forms.CheckBox checkbox_capture_complete_windows;
		private System.Windows.Forms.NumericUpDown numericUpDownWaitTime;
		private System.Windows.Forms.Label label_waittime;
		private System.Windows.Forms.CheckBox checkbox_email;
		private System.Windows.Forms.CheckBox checkbox_capture_windows_interactive;
		private System.Windows.Forms.CheckBox checkbox_capture_mousepointer;
		private System.Windows.Forms.GroupBox groupbox_capture;
		private System.Windows.Forms.TabPage tab_printer;
		private System.Windows.Forms.ListView listview_plugins;
		private System.Windows.Forms.Button button_pluginconfigure;
		private System.Windows.Forms.GroupBox groupbox_plugins;
		private System.Windows.Forms.TabPage tab_plugins;
		private System.Windows.Forms.CheckBox checkboxTimestamp;
		private System.Windows.Forms.CheckBox checkbox_fileas;
		private System.Windows.Forms.Button btnPatternHelp;
		private System.Windows.Forms.CheckBox checkbox_copypathtoclipboard;
		private System.Windows.Forms.CheckBox checkboxAllowShrink;
		private System.Windows.Forms.CheckBox checkboxAllowEnlarge;
		private System.Windows.Forms.CheckBox checkboxAllowRotate;
		private System.Windows.Forms.CheckBox checkboxAllowCenter;
		private System.Windows.Forms.CheckBox checkbox_alwaysshowprintoptionsdialog;
		private System.Windows.Forms.GroupBox groupbox_printoptions;
		private System.Windows.Forms.TabPage tab_output;
		private System.Windows.Forms.TabPage tab_general;
		private System.Windows.Forms.TabControl tabcontrol;
		private System.Windows.Forms.CheckBox checkbox_autostartshortcut;
		private System.Windows.Forms.GroupBox groupbox_destination;
		private System.Windows.Forms.CheckBox checkbox_editor;
		private System.Windows.Forms.CheckBox checkbox_clipboard;
		private System.Windows.Forms.CheckBox checkbox_file;
		private System.Windows.Forms.CheckBox checkbox_printer;
		private System.Windows.Forms.CheckBox checkbox_alwaysshowjpegqualitydialog;
		private System.Windows.Forms.TextBox textBoxJpegQuality;
		private System.Windows.Forms.Label label_jpegquality;
		private System.Windows.Forms.TrackBar trackBarJpegQuality;
		private System.Windows.Forms.GroupBox groupbox_jpegsettings;
		private System.Windows.Forms.CheckBox checkbox_registerhotkeys;
		private System.Windows.Forms.GroupBox groupbox_applicationsettings;
		private System.Windows.Forms.GroupBox groupbox_preferredfilesettings;
		private System.Windows.Forms.CheckBox checkbox_showflashlight;
		private System.Windows.Forms.CheckBox checkbox_playsound;
		private System.Windows.Forms.GroupBox groupbox_visualisation;
		private System.Windows.Forms.Label label_primaryimageformat;
		private System.Windows.Forms.ComboBox combobox_primaryimageformat;
		private System.Windows.Forms.ComboBox combobox_language;
		private System.Windows.Forms.Label label_language;
		private System.Windows.Forms.TextBox textbox_screenshotname;
		private System.Windows.Forms.Label label_screenshotname;
		private System.Windows.Forms.Button browse;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
		private System.Windows.Forms.Button settings_cancel;
		private System.Windows.Forms.Button settings_okay;
		private System.Windows.Forms.TextBox textbox_storagelocation;
		private System.Windows.Forms.Label label_storagelocation;

		
		

	}
}
