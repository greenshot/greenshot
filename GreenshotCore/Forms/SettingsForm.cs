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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.Core;

namespace Greenshot {
	/// <summary>
	/// Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : Form {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SettingsForm));
		ILanguage lang;
		AppConfig conf;
		private ToolTip toolTip;
		
		public SettingsForm() {
			InitializeComponent();
			conf = AppConfig.GetInstance();
			lang = Language.GetInstance();
			// Force loading of languages
			lang.Load();

			toolTip = new ToolTip();
			AddPluginTab();
			UpdateUI();
			this.combobox_primaryimageformat.Items.AddRange(RuntimeConfig.SupportedImageFormats);
			DisplaySettings();
			CheckSettings();
		}
		
		private void AddPluginTab() {
			if (PluginHelper.instance.HasPlugins()) {
				this.tabcontrol.TabPages.Add(tab_plugins);
				// Draw the Plugin listview
				listview_plugins.BeginUpdate();
				listview_plugins.Items.Clear();
				listview_plugins.Columns.Clear();
				string[] columns = { "Name", "Version", "DLL Path"};
				foreach (string column in columns) {
	            	listview_plugins.Columns.Add(column);
				}
				PluginHelper.instance.FillListview(this.listview_plugins);
				// Maximize Column size!
				for (int i = 0; i < listview_plugins.Columns.Count; i++) {
					listview_plugins.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
					int width = listview_plugins.Columns[i].Width;
					listview_plugins.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.HeaderSize);
					if (width > listview_plugins.Columns[i].Width) {
						listview_plugins.Columns[i].Width = width;
					}
				}
				listview_plugins.EndUpdate();
				listview_plugins.Refresh();
							
				// Disable the configure button, it will be enabled when a plugin is selected AND isConfigurable
				button_pluginconfigure.Enabled = false;
			}
		}
		private void UpdateUI() {
			this.Text = lang.GetString(LangKey.settings_title);
			
			this.tab_general.Text = lang.GetString(LangKey.settings_general);
			this.tab_output.Text = lang.GetString(LangKey.settings_output);
			this.tab_printer.Text = lang.GetString(LangKey.settings_printer);
			
			this.groupbox_capture.Text = lang.GetString(LangKey.settings_capture);
			this.checkbox_capture_mousepointer.Text = lang.GetString(LangKey.settings_capture_mousepointer);
			this.checkbox_capture_windows_interactive.Text = lang.GetString(LangKey.settings_capture_windows_interactive);
			this.checkbox_capture_complete_windows.Text = lang.GetString(LangKey.settings_capture_window_full);
			this.checkbox_capture_window_content.Text = lang.GetString(LangKey.settings_capture_window_content);
			this.label_waittime.Text = lang.GetString(LangKey.settings_waittime);
			
			this.groupbox_applicationsettings.Text = lang.GetString(LangKey.settings_applicationsettings);
			this.label_language.Text = lang.GetString(LangKey.settings_language);
			toolTip.SetToolTip(label_language, lang.GetString(LangKey.settings_tooltip_language));
			
			this.checkbox_registerhotkeys.Text = lang.GetString(LangKey.settings_registerhotkeys);
			toolTip.SetToolTip(checkbox_registerhotkeys, lang.GetString(LangKey.settings_tooltip_registerhotkeys));
			this.checkbox_autostartshortcut.Text = lang.GetString(LangKey.settings_autostartshortcut);
			
			this.groupbox_destination.Text = lang.GetString(LangKey.settings_destination);
			this.checkbox_clipboard.Text = lang.GetString(LangKey.settings_destination_clipboard);
			this.checkbox_printer.Text = lang.GetString(LangKey.settings_destination_printer);
			this.checkbox_file.Text = lang.GetString(LangKey.settings_destination_file);
			this.checkbox_fileas.Text = lang.GetString(LangKey.settings_destination_fileas);
			this.checkbox_editor.Text = lang.GetString(LangKey.settings_destination_editor);
			this.checkbox_email.Text = lang.GetString(LangKey.settings_destination_email);
			
			this.groupbox_preferredfilesettings.Text = lang.GetString(LangKey.settings_preferredfilesettings);
			
			this.label_storagelocation.Text = lang.GetString(LangKey.settings_storagelocation);
			toolTip.SetToolTip(label_storagelocation, lang.GetString(LangKey.settings_tooltip_storagelocation));
			
			this.label_screenshotname.Text = lang.GetString(LangKey.settings_filenamepattern);
			toolTip.SetToolTip(label_screenshotname, lang.GetString(LangKey.settings_tooltip_filenamepattern));
			
			this.label_primaryimageformat.Text = lang.GetString(LangKey.settings_primaryimageformat);
			this.checkbox_copypathtoclipboard.Text = lang.GetString(LangKey.settings_copypathtoclipboard);
			toolTip.SetToolTip(label_primaryimageformat, lang.GetString(LangKey.settings_tooltip_primaryimageformat));
			
			this.groupbox_jpegsettings.Text = lang.GetString(LangKey.settings_jpegsettings);
			this.label_jpegquality.Text = lang.GetString(LangKey.settings_jpegquality);
			this.checkbox_alwaysshowjpegqualitydialog.Text = lang.GetString(LangKey.settings_alwaysshowjpegqualitydialog);

			this.groupbox_visualisation.Text = lang.GetString(LangKey.settings_visualization);
			this.checkbox_showflashlight.Text = lang.GetString(LangKey.settings_showflashlight);
			this.checkbox_playsound.Text = lang.GetString(LangKey.settings_playsound);
			
			this.groupbox_printoptions.Text = lang.GetString(LangKey.settings_printoptions);
			this.checkboxAllowCenter.Text = lang.GetString(LangKey.printoptions_allowcenter);
			this.checkboxAllowEnlarge.Text = lang.GetString(LangKey.printoptions_allowenlarge);
			this.checkboxAllowRotate.Text = lang.GetString(LangKey.printoptions_allowrotate);
			this.checkboxAllowShrink.Text = lang.GetString(LangKey.printoptions_allowshrink);
			this.checkboxTimestamp.Text = lang.GetString(LangKey.printoptions_timestamp);
			this.checkbox_alwaysshowprintoptionsdialog.Text = lang.GetString(LangKey.settings_alwaysshowprintoptionsdialog);
			
			// Initialize the Language ComboBox
			this.combobox_language.DisplayMember = "Description";
			this.combobox_language.ValueMember = "Ietf";
			this.combobox_language.SelectedValue = lang.CurrentLanguage;
			// Set datasource last to prevent problems
			// See: http://www.codeproject.com/KB/database/scomlistcontrolbinding.aspx?fid=111644
			this.combobox_language.DataSource = lang.SupportedLanguages;
			
			// Delaying the SelectedIndexChanged events untill all is initiated
			this.combobox_language.SelectedIndexChanged += new System.EventHandler(this.Combobox_languageSelectedIndexChanged);

		}
		
		// Check the settings and somehow visibly mark when something is incorrect
		private bool CheckSettings() {
			bool settingsOk = true;
			if(!Directory.Exists(textbox_storagelocation.Text)) {
				textbox_storagelocation.BackColor = Color.Red;
				settingsOk = false;
			} else {
				textbox_storagelocation.BackColor = Control.DefaultBackColor;
			}
			return settingsOk;
		}

		private void DisplaySettings() {
			combobox_language.SelectedValue = lang.CurrentLanguage;
			checkbox_registerhotkeys.Checked = (bool)conf.General_RegisterHotkeys;
			textbox_storagelocation.Text = conf.Output_File_Path;
			textbox_screenshotname.Text = conf.Output_File_FilenamePattern;
			combobox_primaryimageformat.Text = conf.Output_File_Format.ToString();
			checkbox_copypathtoclipboard.Checked = (bool)conf.Output_File_CopyPathToClipboard;
			trackBarJpegQuality.Value = conf.Output_File_JpegQuality;
			textBoxJpegQuality.Text = conf.Output_File_JpegQuality+"%";
			checkbox_alwaysshowjpegqualitydialog.Checked = (bool)conf.Output_File_PromptJpegQuality;
			checkbox_showflashlight.Checked = (bool)conf.Ui_Effects_Flashlight;
			checkbox_playsound.Checked = (bool)conf.Ui_Effects_CameraSound;
			
			checkbox_clipboard.Checked = (conf.Output_Destinations&ScreenshotDestinations.Clipboard) == ScreenshotDestinations.Clipboard;
			checkbox_file.Checked = (conf.Output_Destinations&ScreenshotDestinations.FileDefault) == ScreenshotDestinations.FileDefault;
			checkbox_fileas.Checked = (conf.Output_Destinations&ScreenshotDestinations.FileWithDialog) == ScreenshotDestinations.FileWithDialog;
			checkbox_printer.Checked = (conf.Output_Destinations&ScreenshotDestinations.Printer) == ScreenshotDestinations.Printer;
			checkbox_editor.Checked = (conf.Output_Destinations&ScreenshotDestinations.Editor) == ScreenshotDestinations.Editor;
			checkbox_email.Checked = (conf.Output_Destinations&ScreenshotDestinations.EMail) == ScreenshotDestinations.EMail;

			checkboxAllowCenter.Checked = (bool)conf.Output_Print_Center;
			checkboxAllowEnlarge.Checked = (bool)conf.Output_Print_AllowEnlarge;
			checkboxAllowRotate.Checked = (bool)conf.Output_Print_AllowRotate;
			checkboxAllowShrink.Checked = (bool)conf.Output_Print_AllowShrink;
			checkboxTimestamp.Checked = (bool)conf.Output_Print_Timestamp;
			checkbox_alwaysshowprintoptionsdialog.Checked = (bool)conf.Output_Print_PromptOptions;
			checkbox_capture_mousepointer.Checked = (bool)conf.Capture_Mousepointer;
			checkbox_capture_windows_interactive.Checked = (bool)conf.Capture_Windows_Interactive;
			checkbox_capture_complete_windows.Checked = (bool)conf.Capture_Complete_Window;
			checkbox_capture_window_content.Checked = (bool)conf.Capture_Window_Content;
			numericUpDownWaitTime.Value = conf.Capture_Wait_Time;

			// If the run for all is set we disable and set the checkbox
			if (StartupHelper.checkRunAll()) {
				checkbox_autostartshortcut.Enabled = false;
				checkbox_autostartshortcut.Checked = true;
			} else {
				// No run for all, enable the checkbox and set it to true if the current user has a key
				checkbox_autostartshortcut.Enabled = true;
				checkbox_autostartshortcut.Checked = StartupHelper.checkRunUser();
			}
		}

		private void SaveSettings() {
			conf.Ui_Language = combobox_language.SelectedValue.ToString();

			// Make sure the current language is reflected in the Main-context menu
			//MainForm.instance.UpdateUI(); // TODO
						
			conf.General_RegisterHotkeys = (bool?)checkbox_registerhotkeys.Checked;
			conf.Output_File_Path = textbox_storagelocation.Text;
			conf.Output_File_FilenamePattern = textbox_screenshotname.Text;
			conf.Output_File_Format = combobox_primaryimageformat.Text;
			conf.Output_File_CopyPathToClipboard = (bool?)checkbox_copypathtoclipboard.Checked;
			conf.Output_File_JpegQuality = trackBarJpegQuality.Value;
			conf.Output_File_PromptJpegQuality = (bool?)checkbox_alwaysshowjpegqualitydialog.Checked;
			conf.Ui_Effects_Flashlight = (bool?)checkbox_showflashlight.Checked;
			conf.Ui_Effects_CameraSound = (bool?)checkbox_playsound.Checked;
			ScreenshotDestinations dest = 0;
			if(checkbox_clipboard.Checked) dest |= ScreenshotDestinations.Clipboard;
			if(checkbox_file.Checked) dest |= ScreenshotDestinations.FileDefault;
			if(checkbox_fileas.Checked) dest |= ScreenshotDestinations.FileWithDialog;
			if(checkbox_printer.Checked) dest |= ScreenshotDestinations.Printer;
			if(checkbox_editor.Checked) dest |= ScreenshotDestinations.Editor;
			if(checkbox_email.Checked) dest |= ScreenshotDestinations.EMail;
			if (!MapiMailMessage.HasMAPI()) {
				// Disable MAPI functionality as it's not available
				checkbox_email.Enabled = false;
				checkbox_email.Checked = false;
			} else {
				// Enable MAPI functionality
				checkbox_email.Enabled = true;
			}
			
			conf.Output_Destinations = dest;
			conf.Output_Print_Center = (bool?)checkboxAllowCenter.Checked;
			conf.Output_Print_AllowEnlarge = (bool?)checkboxAllowEnlarge.Checked;
			conf.Output_Print_AllowRotate = (bool?)checkboxAllowRotate.Checked;
			conf.Output_Print_AllowShrink = (bool?)checkboxAllowShrink.Checked;
			conf.Output_Print_Timestamp = (bool?)checkboxTimestamp.Checked;
			conf.Output_Print_PromptOptions = (bool?)checkbox_alwaysshowprintoptionsdialog.Checked;
			conf.Capture_Mousepointer = (bool?)checkbox_capture_mousepointer.Checked;
			conf.Capture_Windows_Interactive = (bool?)checkbox_capture_windows_interactive.Checked;
			conf.Capture_Window_Content = (bool?)checkbox_capture_window_content.Checked;

			conf.Capture_Complete_Window = (bool?)checkbox_capture_complete_windows.Checked;
			conf.Capture_Wait_Time = (int)numericUpDownWaitTime.Value;

			conf.Store();
			
			// Check if the Run for all is set
			if(!StartupHelper.checkRunAll()) {
				// If not set the registry according to the settings
				if (checkbox_autostartshortcut.Checked) {
					StartupHelper.setRunUser();
				} else {
					StartupHelper.deleteRunUser();
				}
			} else {
				// The run key for Greenshot is set for all users, delete the local version!
				StartupHelper.deleteRunUser();
			}
		}
		
		void Settings_cancelClick(object sender, System.EventArgs e) {
			this.Close();
		}
		
		void Settings_okayClick(object sender, System.EventArgs e) {
			SaveSettings();
			this.Close();
		}
		
		void BrowseClick(object sender, System.EventArgs e) {
			this.folderBrowserDialog1.SelectedPath = this.textbox_storagelocation.Text;
			if(this.folderBrowserDialog1.ShowDialog() == DialogResult.OK) {
				this.textbox_storagelocation.Text = this.folderBrowserDialog1.SelectedPath;
			}
			CheckSettings();
		}
		
		void TrackBarJpegQualityScroll(object sender, System.EventArgs e) {
			textBoxJpegQuality.Text = trackBarJpegQuality.Value.ToString();
		}

		
		void BtnPatternHelpClick(object sender, EventArgs e) {
			MessageBox.Show(lang.GetString(LangKey.settings_message_filenamepattern),lang.GetString(LangKey.settings_filenamepattern));
		}
		
		void Listview_pluginsSelectedIndexChanged(object sender, EventArgs e) {
			button_pluginconfigure.Enabled = PluginHelper.instance.isSelectedItemConfigurable(listview_plugins);
		}
		
		void Button_pluginconfigureClick(object sender, EventArgs e) {
			PluginHelper.instance.ConfigureSelectedItem(listview_plugins);
		}
		
		void Combobox_languageSelectedIndexChanged(object sender, EventArgs e) {
			LOG.Debug("Setting language to: " + (string)combobox_language.SelectedValue);
			lang.SetLanguage((string)combobox_language.SelectedValue);
			Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang.CurrentLanguage);
			// Reflect language changes to the settings form
			UpdateUI();
		}
	}
}
