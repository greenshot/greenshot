/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Helpers;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.UnmanagedHelpers;
using Greenshot.Plugin;
using Greenshot.IniFile;
using System.Text.RegularExpressions;

namespace Greenshot {
	/// <summary>
	/// Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : Form {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SettingsForm));
		private static CoreConfiguration coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
		private static EditorConfiguration editorConfiguration = IniConfig.GetIniSection<EditorConfiguration>();
		private ILanguage lang;
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
			UpdateUI();
			DisplaySettings();
			CheckSettings();
		}
		
		/// <summary>
		/// This is a method to popululate the ComboBox
		/// with the items from the enumeration
		/// </summary>
		/// <param name="comboBox">ComboBox to populate</param>
		/// <param name="enumeration">Enum to populate with</param>
		private void PopulateComboBox<ET>(ComboBox comboBox) {
			ET[] availableValues = (ET[])Enum.GetValues(typeof(ET));
			PopulateComboBox<ET>(comboBox, availableValues, availableValues[0]);
		}

		/// <summary>
		/// This is a method to popululate the ComboBox
		/// with the items from the enumeration
		/// </summary>
		/// <param name="comboBox">ComboBox to populate</param>
		/// <param name="enumeration">Enum to populate with</param>
		private void PopulateComboBox<ET>(ComboBox comboBox, ET[] availableValues, ET selectedValue) {
			comboBox.Items.Clear();
			string enumTypeName = typeof(ET).Name;
			foreach(ET enumValue in availableValues) {
				string translation = lang.GetString(enumTypeName + "." + enumValue.ToString());
				comboBox.Items.Add(translation);
			}
			comboBox.SelectedItem = lang.GetString(enumTypeName + "." + selectedValue.ToString());
		}
		
		
		/// <summary>
		/// Get the selected enum value from the combobox, uses generics
		/// </summary>
		/// <param name="comboBox">Combobox to get the value from</param>
		/// <returns>The generics value of the combobox</returns>
		private ET GetSelected<ET>(ComboBox comboBox) {
			string enumTypeName = typeof(ET).Name;
			string selectedValue = comboBox.SelectedItem as string;
			ET[] availableValues = (ET[])Enum.GetValues(typeof(ET));
			ET returnValue = availableValues[0];
			foreach(ET enumValue in availableValues) {
				string translation = lang.GetString(enumTypeName + "." + enumValue.ToString());
				if (translation.Equals(selectedValue)) {
					returnValue = enumValue;
					break;
				}
			}
			return returnValue;
		}
		
		private void SetWindowCaptureMode(WindowCaptureMode selectedWindowCaptureMode) {
			WindowCaptureMode[] availableModes;
			if (!DWM.isDWMEnabled()) {
				// Remove DWM from configuration, as DWM is disabled!
				if (coreConfiguration.WindowCaptureMode == WindowCaptureMode.Aero || coreConfiguration.WindowCaptureMode == WindowCaptureMode.AeroTransparent) {
					coreConfiguration.WindowCaptureMode = WindowCaptureMode.GDI;
				}
				availableModes = new WindowCaptureMode[]{WindowCaptureMode.Auto, WindowCaptureMode.Screen, WindowCaptureMode.GDI};
			} else {
				availableModes = new WindowCaptureMode[]{WindowCaptureMode.Auto, WindowCaptureMode.Screen, WindowCaptureMode.GDI, WindowCaptureMode.Aero, WindowCaptureMode.AeroTransparent};
			}
			PopulateComboBox<WindowCaptureMode>(combobox_window_capture_mode, availableModes, selectedWindowCaptureMode);
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

			this.groupbox_editor.Text = lang.GetString(LangKey.settings_editor);
			this.checkbox_editor_match_capture_size.Text = lang.GetString(LangKey.editor_match_capture_size);

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
			if (lang.CurrentLanguage != null) {
				this.combobox_language.SelectedValue = lang.CurrentLanguage;
			}
			// Set datasource last to prevent problems
			// See: http://www.codeproject.com/KB/database/scomlistcontrolbinding.aspx?fid=111644
			this.combobox_language.DataSource = lang.SupportedLanguages;
			
			// Delaying the SelectedIndexChanged events untill all is initiated
			this.combobox_language.SelectedIndexChanged += new System.EventHandler(this.Combobox_languageSelectedIndexChanged);
		}
		
		// Check the settings and somehow visibly mark when something is incorrect
		private bool CheckSettings() {
			bool settingsOk = true;
			if(!Directory.Exists(FilenameHelper.FillVariables(textbox_storagelocation.Text, false))) {
				textbox_storagelocation.BackColor = Color.Red;
				settingsOk = false;
			} else {
				textbox_storagelocation.BackColor = Control.DefaultBackColor;
			}
			return settingsOk;
		}

		private void DisplaySettings() {
			region_hotkeyControl.SetHotkey(coreConfiguration.RegionHotkey);
			fullscreen_hotkeyControl.SetHotkey(coreConfiguration.FullscreenHotkey);
			window_hotkeyControl.SetHotkey(coreConfiguration.WindowHotkey);
			lastregion_hotkeyControl.SetHotkey(coreConfiguration.LastregionHotkey);
			ie_hotkeyControl.SetHotkey(coreConfiguration.IEHotkey);
			colorButton_window_background.SelectedColor = coreConfiguration.DWMBackgroundColor;
			
			checkbox_ie_capture.Checked = coreConfiguration.IECapture;
			if (lang.CurrentLanguage != null) {
				combobox_language.SelectedValue = lang.CurrentLanguage;
			}
			textbox_storagelocation.Text = FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false);
			textbox_screenshotname.Text = coreConfiguration.OutputFileFilenamePattern;
			combobox_primaryimageformat.SelectedItem = coreConfiguration.OutputFileFormat;
			
			SetWindowCaptureMode(coreConfiguration.WindowCaptureMode);

			checkbox_copypathtoclipboard.Checked = coreConfiguration.OutputFileCopyPathToClipboard;
			trackBarJpegQuality.Value = coreConfiguration.OutputFileJpegQuality;
			textBoxJpegQuality.Text = coreConfiguration.OutputFileJpegQuality+"%";
			checkbox_alwaysshowjpegqualitydialog.Checked = coreConfiguration.OutputFilePromptJpegQuality;
			checkbox_playsound.Checked = coreConfiguration.PlayCameraSound;
			
			checkedDestinationsListBox.Items.Clear();
			foreach(IDestination destination in DestinationHelper.GetAllDestinations()) {
				checkedDestinationsListBox.Items.Add(destination, coreConfiguration.OutputDestinations.Contains(destination.Designation));
			}
//			checkbox_clipboard.Checked = coreConfiguration.OutputDestinations.Contains("Clipboard");
//			checkbox_file.Checked = coreConfiguration.OutputDestinations.Contains("File");
//			checkbox_fileas.Checked = coreConfiguration.OutputDestinations.Contains("FileWithDialog");
//			checkbox_printer.Checked = coreConfiguration.OutputDestinations.Contains("Printer");
//			checkbox_editor.Checked = coreConfiguration.OutputDestinations.Contains("Editor");
//			checkbox_email.Checked = coreConfiguration.OutputDestinations.Contains("EMail");

			checkboxPrintInverted.Checked = coreConfiguration.OutputPrintInverted;
			checkboxAllowCenter.Checked = coreConfiguration.OutputPrintCenter;
			checkboxAllowEnlarge.Checked = coreConfiguration.OutputPrintAllowEnlarge;
			checkboxAllowRotate.Checked = coreConfiguration.OutputPrintAllowRotate;
			checkboxAllowShrink.Checked = coreConfiguration.OutputPrintAllowShrink;
			checkboxTimestamp.Checked = coreConfiguration.OutputPrintFooter;
			checkbox_alwaysshowprintoptionsdialog.Checked = coreConfiguration.OutputPrintPromptOptions;
			checkbox_capture_mousepointer.Checked = coreConfiguration.CaptureMousepointer;
			checkbox_capture_windows_interactive.Checked = coreConfiguration.CaptureWindowsInteractive;
			
			checkbox_editor_match_capture_size.Checked = editorConfiguration.MatchSizeToCapture;

			numericUpDownWaitTime.Value = coreConfiguration.CaptureDelay >=0?coreConfiguration.CaptureDelay:0;

			// If the run for all is set we disable and set the checkbox
			if (StartupHelper.checkRunAll()) {
				checkbox_autostartshortcut.Enabled = false;
				checkbox_autostartshortcut.Checked = true;
			} else {
				// No run for all, enable the checkbox and set it to true if the current user has a key
				checkbox_autostartshortcut.Enabled = true;
				checkbox_autostartshortcut.Checked = StartupHelper.checkRunUser();
			}
			
			checkbox_usedefaultproxy.Checked = coreConfiguration.UseProxy;
			numericUpDown_daysbetweencheck.Value = coreConfiguration.UpdateCheckInterval;
			CheckDestinationSettings();
		}

		private void SaveSettings() {
			if (combobox_language.SelectedItem != null) {
				coreConfiguration.Language = combobox_language.SelectedValue.ToString();
			}

			coreConfiguration.WindowCaptureMode = GetSelected<WindowCaptureMode>(combobox_window_capture_mode);
			coreConfiguration.OutputFileFilenamePattern = textbox_screenshotname.Text;
			if (!FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false).Equals(textbox_storagelocation.Text)) {
				coreConfiguration.OutputFilePath = textbox_storagelocation.Text;
			}
			if (combobox_primaryimageformat.SelectedItem != null) {
				coreConfiguration.OutputFileFormat = (OutputFormat)combobox_primaryimageformat.SelectedItem;
			} else {
				coreConfiguration.OutputFileFormat = OutputFormat.png;
			}

			coreConfiguration.OutputFileCopyPathToClipboard = checkbox_copypathtoclipboard.Checked;
			coreConfiguration.OutputFileJpegQuality = trackBarJpegQuality.Value;
			coreConfiguration.OutputFilePromptJpegQuality = checkbox_alwaysshowjpegqualitydialog.Checked;
			coreConfiguration.PlayCameraSound = checkbox_playsound.Checked;

			List<string> destinations = new List<string>();
			foreach(int index in checkedDestinationsListBox.CheckedIndices) {
				IDestination destination = (IDestination)checkedDestinationsListBox.Items[index];
				if (checkedDestinationsListBox.GetItemCheckState(index) == CheckState.Checked) {
					destinations.Add(destination.Designation);
				}
			}
			coreConfiguration.OutputDestinations = destinations;
			
			coreConfiguration.OutputPrintInverted = checkboxPrintInverted.Checked;
			coreConfiguration.OutputPrintCenter = checkboxAllowCenter.Checked;
			coreConfiguration.OutputPrintAllowEnlarge = checkboxAllowEnlarge.Checked;
			coreConfiguration.OutputPrintAllowRotate = checkboxAllowRotate.Checked;
			coreConfiguration.OutputPrintAllowShrink = checkboxAllowShrink.Checked;
			coreConfiguration.OutputPrintFooter = checkboxTimestamp.Checked;
			coreConfiguration.OutputPrintPromptOptions = checkbox_alwaysshowprintoptionsdialog.Checked;
			coreConfiguration.CaptureMousepointer = checkbox_capture_mousepointer.Checked;
			coreConfiguration.CaptureWindowsInteractive = checkbox_capture_windows_interactive.Checked;
			coreConfiguration.CaptureDelay = (int)numericUpDownWaitTime.Value;
			coreConfiguration.DWMBackgroundColor = colorButton_window_background.SelectedColor;

			coreConfiguration.RegionHotkey = region_hotkeyControl.ToString();
			coreConfiguration.FullscreenHotkey = fullscreen_hotkeyControl.ToString();
			coreConfiguration.WindowHotkey = window_hotkeyControl.ToString();
			coreConfiguration.LastregionHotkey = lastregion_hotkeyControl.ToString();
			coreConfiguration.IEHotkey = ie_hotkeyControl.ToString();

			coreConfiguration.IECapture = checkbox_ie_capture.Checked;

			coreConfiguration.UpdateCheckInterval = (int)numericUpDown_daysbetweencheck.Value;
			coreConfiguration.UseProxy = checkbox_usedefaultproxy.Checked;

			editorConfiguration.MatchSizeToCapture = checkbox_editor_match_capture_size.Checked;

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
			DialogResult = DialogResult.Cancel;
			lang.FreeResources();
		}
		
		void Settings_okayClick(object sender, System.EventArgs e) {
			if (CheckSettings()) {
				SaveSettings();
				DialogResult = DialogResult.OK;
			} else {
				this.tabcontrol.SelectTab(this.tab_output);
			}
			lang.FreeResources();
		}
		
		void BrowseClick(object sender, System.EventArgs e) {
			// Get the storage location and replace the environment variables
			this.folderBrowserDialog1.SelectedPath = FilenameHelper.FillVariables(this.textbox_storagelocation.Text, false);
			if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK) {
				// Only change if there is a change, otherwise we might overwrite the environment variables
				if (this.folderBrowserDialog1.SelectedPath != null && !this.folderBrowserDialog1.SelectedPath.Equals(FilenameHelper.FillVariables(this.textbox_storagelocation.Text, false))) {
					this.textbox_storagelocation.Text = this.folderBrowserDialog1.SelectedPath;
				}
			}
			CheckSettings();
		}
		
		void TrackBarJpegQualityScroll(object sender, System.EventArgs e) {
			textBoxJpegQuality.Text = trackBarJpegQuality.Value.ToString();
		}

		
		void BtnPatternHelpClick(object sender, EventArgs e) {
			string filenamepatternText = lang.GetString(LangKey.settings_message_filenamepattern);
			// Convert %NUM% to ${NUM} for old language files!
			filenamepatternText = Regex.Replace(filenamepatternText, "%([a-zA-Z_0-9]+)%", @"${$1}");
			MessageBox.Show(filenamepatternText, lang.GetString(LangKey.settings_filenamepattern));
		}
		
		void Listview_pluginsSelectedIndexChanged(object sender, EventArgs e) {
			button_pluginconfigure.Enabled = PluginHelper.instance.isSelectedItemConfigurable(listview_plugins);
		}
		
		void Button_pluginconfigureClick(object sender, EventArgs e) {
			PluginHelper.instance.ConfigureSelectedItem(listview_plugins);
		}
		
		void Combobox_languageSelectedIndexChanged(object sender, EventArgs e) {
			// Get the combobox values BEFORE changing the language
			//EmailFormat selectedEmailFormat = GetSelected<EmailFormat>(combobox_emailformat);
			WindowCaptureMode selectedWindowCaptureMode = GetSelected<WindowCaptureMode>(combobox_window_capture_mode);
			if (combobox_language.SelectedItem != null) {
				LOG.Debug("Setting language to: " + (string)combobox_language.SelectedValue);
				lang.SetLanguage((string)combobox_language.SelectedValue);
			}
			// Reflect language changes to the settings form
			UpdateUI();
			
			// Update the email & windows capture mode
			//SetEmailFormat(selectedEmailFormat);
			SetWindowCaptureMode(selectedWindowCaptureMode);
		}
		
		void Combobox_window_capture_modeSelectedIndexChanged(object sender, EventArgs e) {
			int windowsVersion = Environment.OSVersion.Version.Major;
			WindowCaptureMode mode = GetSelected<WindowCaptureMode>(combobox_window_capture_mode);
			if (windowsVersion >= 6) {
				switch (mode) {
					case WindowCaptureMode.Aero:
					case WindowCaptureMode.Auto:
						colorButton_window_background.Visible = true;
						return;
				}
			}
			colorButton_window_background.Visible = false;
		}
		
		void SettingsFormFormClosing(object sender, FormClosingEventArgs e) {
			MainForm.RegisterHotkeys();
		}
		
		void SettingsFormShown(object sender, EventArgs e) {
			HotkeyControl.UnregisterHotkeys();
		}
		
		/// <summary>
		/// Check the destination settings
		/// </summary>
		void CheckDestinationSettings() {
			bool clipboardDestinationChecked = false;
			bool pickerSelected = false;
			foreach(IDestination destination in checkedDestinationsListBox.CheckedItems) {
				if (destination.Designation.Equals("Clipboard")) {
					clipboardDestinationChecked = true;
				}
				if (destination.Designation.Equals("Picker")) {
					pickerSelected = true;
				}
			}
			if (pickerSelected) {
				foreach(int index in checkedDestinationsListBox.CheckedIndices) {
					IDestination destination = (IDestination)checkedDestinationsListBox.Items[index];
					if (!destination.Designation.Equals("Picker")) {
						checkedDestinationsListBox.SetItemCheckState(index, CheckState.Indeterminate);
					}
				}
			} else {
				foreach(int index in checkedDestinationsListBox.CheckedIndices) {
					if (checkedDestinationsListBox.GetItemCheckState(index) == CheckState.Indeterminate) {
						checkedDestinationsListBox.SetItemCheckState(index, CheckState.Checked);
					}
				}
				// Prevent multiple clipboard settings at once, see bug #3435056
				if (clipboardDestinationChecked) {
					checkbox_copypathtoclipboard.Checked = false;
					checkbox_copypathtoclipboard.Enabled = false;
				} else {
					checkbox_copypathtoclipboard.Enabled = true;
				}
			}
		}

		void DestinationsCheckStateChanged(object sender, EventArgs e) {
			CheckDestinationSettings();
		}
	}
}
