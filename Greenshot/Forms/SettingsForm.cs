/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Helpers.OfficeInterop;
using Greenshot.Plugin;
using Greenshot.UnmanagedHelpers;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace Greenshot {
	/// <summary>
	/// Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : Form {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SettingsForm));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		ILanguage lang;
		private ToolTip toolTip;
		
		public SettingsForm() {
			InitializeComponent();
			// Fix for Vista/XP differences
			if(Environment.OSVersion.Version.Major >= 6) {
				this.trackBarJpegQuality.BackColor = System.Drawing.SystemColors.Window;
			} else {
				this.trackBarJpegQuality.BackColor = System.Drawing.SystemColors.Control;
			}
			lang = Language.GetInstance();
			// Force re-loading of languages
			lang.Load();

			toolTip = new ToolTip();
			AddPluginTab();
			this.combobox_primaryimageformat.Items.AddRange(new object[]{OutputFormat.bmp, OutputFormat.gif, OutputFormat.jpg, OutputFormat.png, OutputFormat.tiff});
			this.combobox_emailformat.Items.AddRange(new object[]{EmailFormat.TXT, EmailFormat.HTML});
			this.combobox_window_capture_mode.Items.AddRange(new object[]{WindowCaptureMode.Auto, WindowCaptureMode.Screen, WindowCaptureMode.GDI});
			if (DWM.isDWMEnabled()) {
				this.combobox_window_capture_mode.Items.Add(WindowCaptureMode.Aero);
				this.combobox_window_capture_mode.Items.Add(WindowCaptureMode.AeroTransparent);
			}
			UpdateUI();
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
			this.tab_capture.Text = lang.GetString(LangKey.settings_capture);
			this.tab_plugins.Text = lang.GetString(LangKey.settings_plugins);

			this.groupbox_network.Text = lang.GetString(LangKey.settings_network);
			this.label_checkperiod.Text = lang.GetString(LangKey.settings_checkperiod);
			this.checkbox_usedefaultproxy.Text = lang.GetString(LangKey.settings_usedefaultproxy);

			this.groupbox_iecapture.Text = lang.GetString(LangKey.settings_iecapture);
			this.checkbox_ie_capture.Text = lang.GetString(LangKey.settings_iecapture);

			this.groupbox_windowscapture.Text  = lang.GetString(LangKey.settings_windowscapture);
			this.label_window_capture_mode.Text = lang.GetString(LangKey.settings_window_capture_mode);

			this.groupbox_capture.Text = lang.GetString(LangKey.settings_capture);
			this.checkbox_capture_mousepointer.Text = lang.GetString(LangKey.settings_capture_mousepointer);
			this.checkbox_capture_windows_interactive.Text = lang.GetString(LangKey.settings_capture_windows_interactive);
			this.label_waittime.Text = lang.GetString(LangKey.settings_waittime);
			
			this.groupbox_applicationsettings.Text = lang.GetString(LangKey.settings_applicationsettings);
			this.label_language.Text = lang.GetString(LangKey.settings_language);
			toolTip.SetToolTip(label_language, lang.GetString(LangKey.settings_tooltip_language));
			
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

			this.checkbox_playsound.Text = lang.GetString(LangKey.settings_playsound);
			
			this.groupbox_printoptions.Text = lang.GetString(LangKey.settings_printoptions);
			this.checkboxAllowCenter.Text = lang.GetString(LangKey.printoptions_allowcenter);
			this.checkboxAllowEnlarge.Text = lang.GetString(LangKey.printoptions_allowenlarge);
			this.checkboxAllowRotate.Text = lang.GetString(LangKey.printoptions_allowrotate);
			this.checkboxAllowShrink.Text = lang.GetString(LangKey.printoptions_allowshrink);
			this.checkboxTimestamp.Text = lang.GetString(LangKey.printoptions_timestamp);
			this.checkboxPrintInverted.Text = lang.GetString(LangKey.printoptions_inverted);
			this.checkbox_alwaysshowprintoptionsdialog.Text = lang.GetString(LangKey.settings_alwaysshowprintoptionsdialog);

			this.groupbox_hotkeys.Text = lang.GetString(LangKey.hotkeys);
			this.label_fullscreen_hotkey.Text = lang.GetString(LangKey.contextmenu_capturefullscreen);
			this.label_ie_hotkey.Text = lang.GetString(LangKey.contextmenu_captureie);
			this.label_lastregion_hotkey.Text = lang.GetString(LangKey.contextmenu_capturelastregion);
			this.label_region_hotkey.Text = lang.GetString(LangKey.contextmenu_capturearea);
			this.label_window_hotkey.Text = lang.GetString(LangKey.contextmenu_capturewindow);

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
			if(!Directory.Exists(FilenameHelper.FillVariables(textbox_storagelocation.Text))) {
				textbox_storagelocation.BackColor = Color.Red;
				settingsOk = false;
			} else {
				textbox_storagelocation.BackColor = Control.DefaultBackColor;
			}
			return settingsOk;
		}

		private void DisplaySettings() {
			region_hotkeyControl.SetHotkey(conf.RegionHotkey);
			fullscreen_hotkeyControl.SetHotkey(conf.FullscreenHotkey);
			window_hotkeyControl.SetHotkey(conf.WindowHotkey);
			lastregion_hotkeyControl.SetHotkey(conf.LastregionHotkey);
			ie_hotkeyControl.SetHotkey(conf.IEHotkey);
			colorButton_window_background.SelectedColor = conf.DWMBackgroundColor;
			
			checkbox_ie_capture.Checked = conf.IECapture;
			combobox_language.SelectedValue = lang.CurrentLanguage;
			textbox_storagelocation.Text = FilenameHelper.FillVariables(conf.OutputFilePath);
			textbox_screenshotname.Text = conf.OutputFileFilenamePattern;
			combobox_primaryimageformat.SelectedItem = conf.OutputFileFormat;
			combobox_emailformat.SelectedItem = conf.OutputEMailFormat;
			if (!DWM.isDWMEnabled()) {
				// Remove DWM from configuration, as DWM is disabled!
				if (conf.WindowCaptureMode == WindowCaptureMode.Aero || conf.WindowCaptureMode == WindowCaptureMode.AeroTransparent) {
					conf.WindowCaptureMode = WindowCaptureMode.GDI;
				}
			}
			combobox_window_capture_mode.SelectedItem = conf.WindowCaptureMode;

			checkbox_copypathtoclipboard.Checked = conf.OutputFileCopyPathToClipboard;
			trackBarJpegQuality.Value = conf.OutputFileJpegQuality;
			textBoxJpegQuality.Text = conf.OutputFileJpegQuality+"%";
			checkbox_alwaysshowjpegqualitydialog.Checked = conf.OutputFilePromptJpegQuality;
			checkbox_playsound.Checked = conf.PlayCameraSound;
			
			checkbox_clipboard.Checked = conf.OutputDestinations.Contains(Destination.Clipboard);
			checkbox_file.Checked = conf.OutputDestinations.Contains(Destination.FileDefault);
			checkbox_fileas.Checked = conf.OutputDestinations.Contains(Destination.FileWithDialog);
			checkbox_printer.Checked = conf.OutputDestinations.Contains(Destination.Printer);
			checkbox_editor.Checked = conf.OutputDestinations.Contains(Destination.Editor);
			checkbox_email.Checked = conf.OutputDestinations.Contains(Destination.EMail);

			checkboxPrintInverted.Checked = conf.OutputPrintInverted;
			checkboxAllowCenter.Checked = conf.OutputPrintCenter;
			checkboxAllowEnlarge.Checked = conf.OutputPrintAllowEnlarge;
			checkboxAllowRotate.Checked = conf.OutputPrintAllowRotate;
			checkboxAllowShrink.Checked = conf.OutputPrintAllowShrink;
			checkboxTimestamp.Checked = conf.OutputPrintTimestamp;
			checkbox_alwaysshowprintoptionsdialog.Checked = conf.OutputPrintPromptOptions;
			checkbox_capture_mousepointer.Checked = conf.CaptureMousepointer;
			checkbox_capture_windows_interactive.Checked = conf.CaptureWindowsInteractive;

			numericUpDownWaitTime.Value = conf.CaptureDelay >=0?conf.CaptureDelay:0;

			// If the run for all is set we disable and set the checkbox
			if (StartupHelper.checkRunAll()) {
				checkbox_autostartshortcut.Enabled = false;
				checkbox_autostartshortcut.Checked = true;
			} else {
				// No run for all, enable the checkbox and set it to true if the current user has a key
				checkbox_autostartshortcut.Enabled = true;
				checkbox_autostartshortcut.Checked = StartupHelper.checkRunUser();
			}
			
			if (!MapiMailMessage.HasMAPIorOutlook()) {
				// Disable MAPI functionality as it's not available
				checkbox_email.Enabled = false;
				checkbox_email.Checked = false;
				combobox_emailformat.Visible = false;
			} else {
				// Enable MAPI functionality
				checkbox_email.Enabled = true;
				if (OutlookExporter.HasOutlook()) {
					combobox_emailformat.Visible = true;
				}
			}
			
			checkbox_usedefaultproxy.Checked = conf.UseProxy;
			numericUpDown_daysbetweencheck.Value = conf.UpdateCheckInterval;
		}

		private void SaveSettings() {
			conf.Language = combobox_language.SelectedValue.ToString();

			conf.WindowCaptureMode = (WindowCaptureMode)combobox_window_capture_mode.SelectedItem;
			conf.OutputFileFilenamePattern = textbox_screenshotname.Text;
			if (!FilenameHelper.FillVariables(conf.OutputFilePath).Equals(textbox_storagelocation.Text)) {
				conf.OutputFilePath = textbox_storagelocation.Text;
			}
			conf.OutputFileFormat = (OutputFormat)combobox_primaryimageformat.SelectedItem;
			conf.OutputEMailFormat = (EmailFormat)combobox_emailformat.SelectedItem;

			conf.OutputFileCopyPathToClipboard = checkbox_copypathtoclipboard.Checked;
			conf.OutputFileJpegQuality = trackBarJpegQuality.Value;
			conf.OutputFilePromptJpegQuality = checkbox_alwaysshowjpegqualitydialog.Checked;
			conf.PlayCameraSound = checkbox_playsound.Checked;

			List<Destination> destinations = new List<Destination>();
			if (checkbox_clipboard.Checked) destinations.Add(Destination.Clipboard);
			if (checkbox_file.Checked) destinations.Add(Destination.FileDefault);
			if (checkbox_fileas.Checked) destinations.Add(Destination.FileWithDialog);
			if (checkbox_printer.Checked) destinations.Add(Destination.Printer);
			if (checkbox_editor.Checked) destinations.Add(Destination.Editor);
			if (checkbox_email.Checked) destinations.Add(Destination.EMail);
			conf.OutputDestinations = destinations;
			
			conf.OutputPrintInverted = checkboxPrintInverted.Checked;
			conf.OutputPrintCenter = checkboxAllowCenter.Checked;
			conf.OutputPrintAllowEnlarge = checkboxAllowEnlarge.Checked;
			conf.OutputPrintAllowRotate = checkboxAllowRotate.Checked;
			conf.OutputPrintAllowShrink = checkboxAllowShrink.Checked;
			conf.OutputPrintTimestamp = checkboxTimestamp.Checked;
			conf.OutputPrintPromptOptions = checkbox_alwaysshowprintoptionsdialog.Checked;
			conf.CaptureMousepointer = checkbox_capture_mousepointer.Checked;
			conf.CaptureWindowsInteractive = checkbox_capture_windows_interactive.Checked;
			conf.CaptureDelay = (int)numericUpDownWaitTime.Value;
			conf.DWMBackgroundColor = colorButton_window_background.SelectedColor;

			conf.RegionHotkey = region_hotkeyControl.ToString();
			conf.FullscreenHotkey = fullscreen_hotkeyControl.ToString();
			conf.WindowHotkey = window_hotkeyControl.ToString();
			conf.LastregionHotkey = lastregion_hotkeyControl.ToString();
			conf.IEHotkey = ie_hotkeyControl.ToString();

			conf.IECapture = checkbox_ie_capture.Checked;

			conf.UpdateCheckInterval = (int)numericUpDown_daysbetweencheck.Value;
			conf.UseProxy = checkbox_usedefaultproxy.Checked;

			IniConfig.Save();

			// Make sure the current language & settings are reflected in the Main-context menu
			MainForm.instance.UpdateUI();

			try {
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
			} catch (Exception e) {
				LOG.Warn("Problem checking registry, ignoring for now: ", e);
			}
		}
		
		void Settings_cancelClick(object sender, System.EventArgs e) {
			this.Close();
		}
		
		void Settings_okayClick(object sender, System.EventArgs e) {
			if (CheckSettings()) {
				SaveSettings();
				this.Close();
			} else {
				this.tabcontrol.SelectTab(this.tab_output);
			}
		}
		
		void BrowseClick(object sender, System.EventArgs e) {
			// Get the storage location and replace the environment variables
			this.folderBrowserDialog1.SelectedPath = FilenameHelper.FillVariables(this.textbox_storagelocation.Text);
			if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK) {
				// Only change if there is a change, otherwise we might overwrite the environment variables
				if (this.folderBrowserDialog1.SelectedPath != null && !this.folderBrowserDialog1.SelectedPath.Equals(FilenameHelper.FillVariables(this.textbox_storagelocation.Text))) {
					this.textbox_storagelocation.Text = this.folderBrowserDialog1.SelectedPath;
				}
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
			// Reflect language changes to the settings form
			UpdateUI();
		}
		
		void Combobox_window_capture_modeSelectedIndexChanged(object sender, EventArgs e) {
			int windowsVersion = Environment.OSVersion.Version.Major;
			string modeText = combobox_window_capture_mode.Text;
			string dwmMode = WindowCaptureMode.Aero.ToString();
			string autoMode = WindowCaptureMode.Auto.ToString();
			if (modeText.Equals(dwmMode, StringComparison.CurrentCultureIgnoreCase)
				|| (modeText.Equals(autoMode, StringComparison.CurrentCultureIgnoreCase) && windowsVersion >= 6) ) {
				colorButton_window_background.Visible = true;
			} else {
				colorButton_window_background.Visible = false;
			}
		}
		
		void SettingsFormFormClosing(object sender, FormClosingEventArgs e) {
			MainForm.RegisterHotkeys();
		}
		
		void SettingsFormShown(object sender, EventArgs e) {
			HotkeyControl.UnregisterHotkeys();
		}
	}
}
