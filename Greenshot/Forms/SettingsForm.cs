/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Destinations;
using Greenshot.Helpers;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.UnmanagedHelpers;
using Greenshot.Plugin;
using Greenshot.IniFile;
using System.Text.RegularExpressions;
using Greenshot.Controls;

namespace Greenshot {
	/// <summary>
	/// Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : BaseForm {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SettingsForm));
		private static EditorConfiguration editorConfiguration = IniConfig.GetIniSection<EditorConfiguration>();
		private ToolTip toolTip = new ToolTip();
		private bool inHotkey = false;

		public SettingsForm() : base() {
			InitializeComponent();
			
			// Make sure the store isn't called to early, that's why we do it manually
			ManualStoreFields = true;
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();

			// Fix for Vista/XP differences
			if (Environment.OSVersion.Version.Major >= 6) {
				this.trackBarJpegQuality.BackColor = System.Drawing.SystemColors.Window;
			} else {
				this.trackBarJpegQuality.BackColor = System.Drawing.SystemColors.Control;
			}

			// This makes it possible to still capture the settings screen
			this.fullscreen_hotkeyControl.Enter += delegate { EnterHotkeyControl(); };
			this.fullscreen_hotkeyControl.Leave += delegate { LeaveHotkeyControl(); };
			this.window_hotkeyControl.Enter += delegate { EnterHotkeyControl(); };
			this.window_hotkeyControl.Leave += delegate { LeaveHotkeyControl(); };
			this.region_hotkeyControl.Enter += delegate { EnterHotkeyControl(); };
			this.region_hotkeyControl.Leave += delegate { LeaveHotkeyControl(); };
			this.ie_hotkeyControl.Enter += delegate { EnterHotkeyControl(); };
			this.ie_hotkeyControl.Leave += delegate { LeaveHotkeyControl(); };
			this.lastregion_hotkeyControl.Enter += delegate { EnterHotkeyControl(); };
			this.lastregion_hotkeyControl.Leave += delegate { LeaveHotkeyControl(); };

			DisplayPluginTab();
			UpdateUI();
			ExpertSettingsEnableState(false);
			DisplaySettings();
			CheckSettings();
		}

		private void EnterHotkeyControl() {
			GreenshotPlugin.Controls.HotkeyControl.UnregisterHotkeys();
			inHotkey = true;
		}

		private void LeaveHotkeyControl() {
			MainForm.RegisterHotkeys();
			inHotkey = false;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
			switch (keyData) {
				case Keys.Escape:
					if (!inHotkey) {
						DialogResult = DialogResult.Cancel;
					} else {
						return base.ProcessCmdKey(ref msg, keyData);
					}
					break;
				default:
					return base.ProcessCmdKey(ref msg, keyData);
			}
			return true;
		}

		/// <summary>
		/// This is a method to popululate the ComboBox
		/// with the items from the enumeration
		/// </summary>
		/// <param name="comboBox">ComboBox to populate</param>
		/// <param name="enumeration">Enum to populate with</param>
		private void PopulateComboBox<ET>(ComboBox comboBox) where ET : struct {
			ET[] availableValues = (ET[])Enum.GetValues(typeof(ET));
			PopulateComboBox<ET>(comboBox, availableValues, availableValues[0]);
		}

		/// <summary>
		/// This is a method to popululate the ComboBox
		/// with the items from the enumeration
		/// </summary>
		/// <param name="comboBox">ComboBox to populate</param>
		/// <param name="enumeration">Enum to populate with</param>
		private void PopulateComboBox<ET>(ComboBox comboBox, ET[] availableValues, ET selectedValue) where ET : struct {
			comboBox.Items.Clear();
			string enumTypeName = typeof(ET).Name;
			foreach(ET enumValue in availableValues) {
				comboBox.Items.Add(Language.Translate(enumValue));
			}
			comboBox.SelectedItem = Language.Translate(selectedValue);
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
				string translation = Language.GetString(enumTypeName + "." + enumValue.ToString());
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
		
		private void DisplayPluginTab() {
			if (!PluginHelper.Instance.HasPlugins()) {
				this.tabcontrol.TabPages.Remove(tab_plugins);
			} else {
				// Draw the Plugin listview
				listview_plugins.BeginUpdate();
				listview_plugins.Items.Clear();
				listview_plugins.Columns.Clear();
				string[] columns = {
					Language.GetString("settings_plugins_name"), 
					Language.GetString("settings_plugins_version"), 
					Language.GetString("settings_plugins_createdby"), 
					Language.GetString("settings_plugins_dllpath")};
				foreach (string column in columns) {
					listview_plugins.Columns.Add(column);
				}
				PluginHelper.Instance.FillListview(this.listview_plugins);
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

		/// <summary>
		/// Update the UI to reflect the language and other text settings
		/// </summary>
		private void UpdateUI() {
			if (coreConfiguration.HideExpertSettings) {
				tabcontrol.Controls.Remove(tab_expert);
			}
			toolTip.SetToolTip(label_language, Language.GetString(LangKey.settings_tooltip_language));
			toolTip.SetToolTip(label_storagelocation, Language.GetString(LangKey.settings_tooltip_storagelocation));
			toolTip.SetToolTip(label_screenshotname, Language.GetString(LangKey.settings_tooltip_filenamepattern));
			toolTip.SetToolTip(label_primaryimageformat, Language.GetString(LangKey.settings_tooltip_primaryimageformat));

			// Removing, otherwise we keep getting the event multiple times!
			this.combobox_language.SelectedIndexChanged -= new System.EventHandler(this.Combobox_languageSelectedIndexChanged);

			// Initialize the Language ComboBox
			this.combobox_language.DisplayMember = "Description";
			this.combobox_language.ValueMember = "Ietf";
			// Set datasource last to prevent problems
			// See: http://www.codeproject.com/KB/database/scomlistcontrolbinding.aspx?fid=111644
			this.combobox_language.DataSource = Language.SupportedLanguages;
			if (Language.CurrentLanguage != null) {
				this.combobox_language.SelectedValue = Language.CurrentLanguage;
			}

			// Delaying the SelectedIndexChanged events untill all is initiated
			this.combobox_language.SelectedIndexChanged += new System.EventHandler(this.Combobox_languageSelectedIndexChanged);
			UpdateDestinationDescriptions();
			UpdateClipboardFormatDescriptions();
		}
		
		// Check the settings and somehow visibly mark when something is incorrect
		private bool CheckSettings() {
			bool settingsOk = true;
			if(!Directory.Exists(FilenameHelper.FillVariables(textbox_storagelocation.Text, false))) {
				textbox_storagelocation.BackColor = Color.Red;
				settingsOk = false;
			} else {
				// "Added" feature #3547158
				if (Environment.OSVersion.Version.Major >= 6) {
					this.textbox_storagelocation.BackColor = System.Drawing.SystemColors.Window;
				} else {
					this.textbox_storagelocation.BackColor = System.Drawing.SystemColors.Control;
				}
			}
			return settingsOk;
		}

		private void StorageLocationChanged(object sender, System.EventArgs e) {
			CheckSettings();
		}

		/// <summary>
		/// Show all destination descriptions in the current language
		/// </summary>
		private void UpdateDestinationDescriptions() {
			foreach (ListViewItem item in listview_destinations.Items) {
				IDestination destination = item.Tag as IDestination;
				item.Text = destination.Description;
			}
		}

		/// <summary>
		/// Show all clipboard format descriptions in the current language
		/// </summary>
		private void UpdateClipboardFormatDescriptions() {
			foreach(ListViewItem item in listview_clipboardformats.Items) {
				ClipboardFormat cf = (ClipboardFormat) item.Tag;
			    item.Text = Language.Translate(cf);
			}
		}

		/// <summary>
		/// Build the view with all the destinations
		/// </summary>
		private void DisplayDestinations() {
			bool destinationsEnabled = true;
			if (coreConfiguration.Values.ContainsKey("Destinations")) {
				destinationsEnabled = !coreConfiguration.Values["Destinations"].IsFixed;
			}
			checkbox_picker.Checked = false;

			listview_destinations.Items.Clear();
			listview_destinations.ListViewItemSorter = new ListviewWithDestinationComparer();
			ImageList imageList = new ImageList();
			listview_destinations.SmallImageList = imageList;
			int imageNr = -1;
			foreach (IDestination destination in DestinationHelper.GetAllDestinations()) {
				Image destinationImage = destination.DisplayIcon;
				if (destinationImage != null) {
					imageList.Images.Add(destination.DisplayIcon);
					imageNr++;
				}
				if (PickerDestination.DESIGNATION.Equals(destination.Designation)) {
					checkbox_picker.Checked = coreConfiguration.OutputDestinations.Contains(destination.Designation);
					checkbox_picker.Text = destination.Description;
				} else {
					ListViewItem item;
					if (destinationImage != null) {
						item = listview_destinations.Items.Add(destination.Description, imageNr);
					} else {
						item = listview_destinations.Items.Add(destination.Description);
					}
					item.Tag = destination;
					item.Checked = coreConfiguration.OutputDestinations.Contains(destination.Designation);
				}
			}
			if (checkbox_picker.Checked) {
				listview_destinations.Enabled = false;
				foreach (int index in listview_destinations.CheckedIndices) {
					ListViewItem item = listview_destinations.Items[index];
					item.Checked = false;
				}
			}
			checkbox_picker.Enabled = destinationsEnabled;
			listview_destinations.Enabled = destinationsEnabled;
		}

		private void DisplaySettings() {
			colorButton_window_background.SelectedColor = coreConfiguration.DWMBackgroundColor;

			// Expert mode, the clipboard formats
			foreach (ClipboardFormat clipboardFormat in Enum.GetValues(typeof(ClipboardFormat))) {
				ListViewItem item = listview_clipboardformats.Items.Add(Language.Translate(clipboardFormat));
				item.Tag = clipboardFormat;
				item.Checked = coreConfiguration.ClipboardFormats.Contains(clipboardFormat);
			}
			
			if (Language.CurrentLanguage != null) {
				combobox_language.SelectedValue = Language.CurrentLanguage;
			}
			// Disable editing when the value is fixed
			combobox_language.Enabled = !coreConfiguration.Values["Language"].IsFixed;

			textbox_storagelocation.Text = FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false);
			// Disable editing when the value is fixed
			textbox_storagelocation.Enabled = !coreConfiguration.Values["OutputFilePath"].IsFixed;

			SetWindowCaptureMode(coreConfiguration.WindowCaptureMode);
			// Disable editing when the value is fixed
			combobox_window_capture_mode.Enabled = !coreConfiguration.Values["WindowCaptureMode"].IsFixed;

			trackBarJpegQuality.Value = coreConfiguration.OutputFileJpegQuality;
			trackBarJpegQuality.Enabled = !coreConfiguration.Values["OutputFileJpegQuality"].IsFixed;
			textBoxJpegQuality.Text = coreConfiguration.OutputFileJpegQuality+"%";

			DisplayDestinations();

			numericUpDownWaitTime.Value = coreConfiguration.CaptureDelay >=0?coreConfiguration.CaptureDelay:0;
			numericUpDownWaitTime.Enabled = !coreConfiguration.Values["CaptureDelay"].IsFixed;
			// Autostart checkbox logic.
			if (StartupHelper.hasRunAll()) {
				// Remove runUser if we already have a run under all
				StartupHelper.deleteRunUser();
				checkbox_autostartshortcut.Enabled = StartupHelper.canWriteRunAll();
				checkbox_autostartshortcut.Checked = true; // We already checked this
			} else if (StartupHelper.IsInStartupFolder()) {
				checkbox_autostartshortcut.Enabled = false;
				checkbox_autostartshortcut.Checked = true; // We already checked this
			} else {
				// No run for all, enable the checkbox and set it to true if the current user has a key
				checkbox_autostartshortcut.Enabled = StartupHelper.canWriteRunUser();
				checkbox_autostartshortcut.Checked = StartupHelper.hasRunUser();
			}
			
			numericUpDown_daysbetweencheck.Value = coreConfiguration.UpdateCheckInterval;
			numericUpDown_daysbetweencheck.Enabled = !coreConfiguration.Values["UpdateCheckInterval"].IsFixed;
			CheckDestinationSettings();
		}

		private void SaveSettings() {
			if (combobox_language.SelectedItem != null) {
				string newLang = combobox_language.SelectedValue.ToString();
				if (!string.IsNullOrEmpty(newLang)) {
					coreConfiguration.Language = combobox_language.SelectedValue.ToString();
				}
			}

			// retrieve the set clipboard formats
			List<ClipboardFormat> clipboardFormats = new List<ClipboardFormat>();
			foreach (int index in listview_clipboardformats.CheckedIndices) {
				ListViewItem item = listview_clipboardformats.Items[index];
				if (item.Checked) {
					clipboardFormats.Add((ClipboardFormat)item.Tag);
				}
			}
			coreConfiguration.ClipboardFormats = clipboardFormats;

			coreConfiguration.WindowCaptureMode = GetSelected<WindowCaptureMode>(combobox_window_capture_mode);
			if (!FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false).Equals(textbox_storagelocation.Text)) {
				coreConfiguration.OutputFilePath = textbox_storagelocation.Text;
			}
			coreConfiguration.OutputFileJpegQuality = trackBarJpegQuality.Value;

			List<string> destinations = new List<string>();
			if (checkbox_picker.Checked) {
				destinations.Add(PickerDestination.DESIGNATION);
			}
			foreach(int index in listview_destinations.CheckedIndices) {
				ListViewItem item = listview_destinations.Items[index];
				
				IDestination destination = item.Tag as IDestination;
				if (item.Checked) {
					destinations.Add(destination.Designation);
				}
			}
			coreConfiguration.OutputDestinations = destinations;
			coreConfiguration.CaptureDelay = (int)numericUpDownWaitTime.Value;
			coreConfiguration.DWMBackgroundColor = colorButton_window_background.SelectedColor;
			coreConfiguration.UpdateCheckInterval = (int)numericUpDown_daysbetweencheck.Value;

			try {
				if (checkbox_autostartshortcut.Checked) {
					// It's checked, so we set the RunUser if the RunAll isn't set.
					// Do this every time, so the executable is correct.
					if (!StartupHelper.hasRunAll()) {
						StartupHelper.setRunUser();
					}
				} else {
					// Delete both settings if it's unchecked
					if (StartupHelper.hasRunAll()) {
						StartupHelper.deleteRunAll();
					}
					if (StartupHelper.hasRunUser()) {
						StartupHelper.deleteRunUser();
					}
				}
			} catch (Exception e) {
				LOG.Warn("Problem checking registry, ignoring for now: ", e);
			}
		}
		
		void Settings_cancelClick(object sender, System.EventArgs e) {
			DialogResult = DialogResult.Cancel;
		}
		
		void Settings_okayClick(object sender, System.EventArgs e) {
			if (CheckSettings()) {
				GreenshotPlugin.Controls.HotkeyControl.UnregisterHotkeys();
				SaveSettings();
				StoreFields();
				MainForm.RegisterHotkeys();

				// Make sure the current language & settings are reflected in the Main-context menu
				MainForm.Instance.UpdateUI();
				DialogResult = DialogResult.OK;
			} else {
				this.tabcontrol.SelectTab(this.tab_output);
			}
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
		}
		
		void TrackBarJpegQualityScroll(object sender, System.EventArgs e) {
			textBoxJpegQuality.Text = trackBarJpegQuality.Value.ToString();
		}

		
		void BtnPatternHelpClick(object sender, EventArgs e) {
			string filenamepatternText = Language.GetString(LangKey.settings_message_filenamepattern);
			// Convert %NUM% to ${NUM} for old language files!
			filenamepatternText = Regex.Replace(filenamepatternText, "%([a-zA-Z_0-9]+)%", @"${$1}");
			MessageBox.Show(filenamepatternText, Language.GetString(LangKey.settings_filenamepattern));
		}
		
		void Listview_pluginsSelectedIndexChanged(object sender, EventArgs e) {
			button_pluginconfigure.Enabled = PluginHelper.Instance.isSelectedItemConfigurable(listview_plugins);
		}
		
		void Button_pluginconfigureClick(object sender, EventArgs e) {
			PluginHelper.Instance.ConfigureSelectedItem(listview_plugins);
		}

		void Combobox_languageSelectedIndexChanged(object sender, EventArgs e) {
			// Get the combobox values BEFORE changing the language
			//EmailFormat selectedEmailFormat = GetSelected<EmailFormat>(combobox_emailformat);
			WindowCaptureMode selectedWindowCaptureMode = GetSelected<WindowCaptureMode>(combobox_window_capture_mode);
			if (combobox_language.SelectedItem != null) {
				LOG.Debug("Setting language to: " + (string)combobox_language.SelectedValue);
				Language.CurrentLanguage = (string)combobox_language.SelectedValue;
			}
			// Reflect language changes to the settings form
			UpdateUI();

			// Reflect Language changes form
			ApplyLanguage();

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
						colorButton_window_background.Visible = true;
						return;
				}
			}
			colorButton_window_background.Visible = false;
		}
		
		/// <summary>
		/// Check the destination settings
		/// </summary>
		void CheckDestinationSettings() {
			bool clipboardDestinationChecked = false;
			bool pickerSelected = checkbox_picker.Checked;
			bool destinationsEnabled = true;
			if (coreConfiguration.Values.ContainsKey("Destinations")) {
				destinationsEnabled = !coreConfiguration.Values["Destinations"].IsFixed;
			}
			listview_destinations.Enabled = destinationsEnabled;
			
			foreach(int index in listview_destinations.CheckedIndices) {
				ListViewItem item = listview_destinations.Items[index];
				IDestination destination = item.Tag as IDestination;
				if (destination.Designation.Equals(ClipboardDestination.DESIGNATION)) {
					clipboardDestinationChecked = true;
					break;
				}
			}

			if (pickerSelected) {
				listview_destinations.Enabled = false;
				foreach(int index in listview_destinations.CheckedIndices) {
					ListViewItem item = listview_destinations.Items[index];
					item.Checked = false;
				}
			} else {
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

        protected override void OnFieldsFilled() {
            // the color radio button is not actually bound to a setting, but checked when monochrome/grayscale are not checked
            if(!radioBtnGrayScale.Checked && !radioBtnMonochrome.Checked) {
                radioBtnColorPrint.Checked = true;
            }
        }

		/// <summary>
		/// Set the enable state of the expert settings
		/// </summary>
		/// <param name="state"></param>
		private void ExpertSettingsEnableState(bool state) {
			listview_clipboardformats.Enabled = state;
			checkbox_autoreducecolors.Enabled = state;
			checkbox_optimizeforrdp.Enabled = state;
			checkbox_thumbnailpreview.Enabled = state;
			textbox_footerpattern.Enabled = state;
			textbox_counter.Enabled = state;
			checkbox_suppresssavedialogatclose.Enabled = state;
			checkbox_checkunstableupdates.Enabled = state;
			checkbox_minimizememoryfootprint.Enabled = state;
			checkbox_reuseeditor.Enabled = state;
		}

		/// <summary>
		/// Called if the "I know what I am doing" on the settings form is changed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void checkbox_enableexpert_CheckedChanged(object sender, EventArgs e) {
			CheckBox checkBox = sender as CheckBox;
			ExpertSettingsEnableState(checkBox.Checked);
		}
	}

	public class ListviewWithDestinationComparer : System.Collections.IComparer {
		public int Compare(object x, object y) {
			if (!(x is ListViewItem)) {
				return (0);
			}
			if (!(y is ListViewItem)) {
				return (0);
			}

			ListViewItem l1 = (ListViewItem)x;
			ListViewItem l2 = (ListViewItem)y;

			IDestination firstDestination = l1.Tag as IDestination;
			IDestination secondDestination = l2.Tag as IDestination;

			if (secondDestination == null) {
				return 1;
			}
			if (firstDestination.Priority == secondDestination.Priority) {
				return firstDestination.Description.CompareTo(secondDestination.Description);
			}
			return firstDestination.Priority - secondDestination.Priority;
		}
	}
}
