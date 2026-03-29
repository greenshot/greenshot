/*
 * Greenshot - a free and open source screenshot tool
 * Copyright ? 2004-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Dpi;
using Greenshot.Base;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Configuration;
using Greenshot.Controls;
using Greenshot.Helpers;
using log4net;

namespace Greenshot.Forms
{
    /// <summary>
    /// Description of SettingsForm.
    /// </summary>
    public partial class SettingsForm : GreenshotForm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SettingsForm));
        private readonly ToolTip _toolTip = new ToolTip();
        private bool _inHotkey;
        private int _daysBetweenCheckPreviousValue;

        public SettingsForm()
        {
            InitializeComponent();
            InitializeLanguage();
            // Make sure we change the icon size depending on the scaling
            DpiChanged += AdjustToDpi;

            // Make sure the store isn't called to early, that's why we do it manually
            ManualStoreFields = true;
        }

        /// <inheritdoc />
        protected override void InitializeLanguage()
        {
            label_storagelocation.Text = Language.GetString("settings_storagelocation");
            settings_cancel.Text = Language.GetString("CANCEL");
            settings_confirm.Text = Language.GetString("OK");
            label_screenshotname.Text = Language.GetString("settings_filenamepattern");
            textbox_screenshotname.PropertyName = nameof(CoreConfiguration.OutputFileFilenamePattern);
            label_language.Text = Language.GetString("settings_language");
            combobox_primaryimageformat.PropertyName = nameof(CoreConfiguration.OutputFileFormat);
            label_primaryimageformat.Text = Language.GetString("settings_primaryimageformat");
            groupbox_preferredfilesettings.Text = Language.GetString("settings_preferredfilesettings");
            checkbox_copypathtoclipboard.Text = Language.GetString("settings_copypathtoclipboard");
            checkbox_copypathtoclipboard.PropertyName = nameof(CoreConfiguration.OutputFileCopyPathToClipboard);
            groupbox_applicationsettings.Text = Language.GetString("settings_applicationsettings");
            label_icon_size.Text = Language.GetString("settings_iconsize");
            checkbox_autostartshortcut.Text = Language.GetString("settings_autostartshortcut");
            groupbox_qualitysettings.Text = Language.GetString("settings_qualitysettings");
            checkbox_reducecolors.Text = Language.GetString("settings_reducecolors");
            checkbox_reducecolors.PropertyName = nameof(CoreConfiguration.OutputFileReduceColors);
            checkbox_alwaysshowqualitydialog.Text = Language.GetString("settings_alwaysshowqualitydialog");
            checkbox_alwaysshowqualitydialog.PropertyName = nameof(CoreConfiguration.OutputFilePromptQuality);
            label_jpegquality.Text = Language.GetString("settings_jpegquality");
            groupbox_destination.Text = Language.GetString("settings_destination");
            checkbox_picker.Text = Language.GetString("settings_destination_picker");
            tab_general.Text = Language.GetString("settings_general");
            groupbox_network.Text = Language.GetString("settings_network");
            label_checkperiod.Text = Language.GetString("settings_checkperiod");
            checkbox_usedefaultproxy.Text = Language.GetString("settings_usedefaultproxy");
            checkbox_usedefaultproxy.PropertyName = nameof(CoreConfiguration.UseProxy);
            groupbox_hotkeys.Text = Language.GetString("hotkeys");
            label_lastregion_hotkey.Text = Language.GetString("contextmenu_capturelastregion");
            lastregion_hotkeyControl.PropertyName = nameof(CoreConfiguration.LastregionHotkey);
            label_region_hotkey.Text = Language.GetString("contextmenu_capturearea");
            label_window_hotkey.Text = Language.GetString("contextmenu_capturewindow");
            label_fullscreen_hotkey.Text = Language.GetString("contextmenu_capturefullscreen");
            region_hotkeyControl.PropertyName = nameof(CoreConfiguration.RegionHotkey);
            window_hotkeyControl.PropertyName = nameof(CoreConfiguration.WindowHotkey);
            fullscreen_hotkeyControl.PropertyName = nameof(CoreConfiguration.FullscreenHotkey);
            tab_capture.Text = Language.GetString("settings_capture");
            groupbox_editor.Text = Language.GetString("settings_editor");
            checkbox_editor_match_capture_size.Text = Language.GetString("editor_match_capture_size");
            checkbox_editor_match_capture_size.PropertyName = nameof(Editor.Configuration.EditorConfiguration.MatchSizeToCapture);
            checkbox_editor_match_capture_size.SectionName = "Editor";
            groupbox_windowscapture.Text = Language.GetString("settings_windowscapture");
            radiobuttonWindowCapture.Text = Language.GetString("settings_window_capture_mode");
            radiobuttonInteractiveCapture.Text = Language.GetString("settings_capture_windows_interactive");
            radiobuttonInteractiveCapture.PropertyName = nameof(CoreConfiguration.CaptureWindowsInteractive);
            groupbox_capture.Text = Language.GetString("settings_capture");
            checkbox_zoomer.Text = Language.GetString("settings_zoom");
            checkbox_zoomer.PropertyName = nameof(CoreConfiguration.ZoomerEnabled);
            checkbox_notifications.Text = Language.GetString("settings_shownotify");
            checkbox_notifications.PropertyName = nameof(CoreConfiguration.ShowTrayNotification);
            checkbox_playsound.Text = Language.GetString("settings_playsound");
            checkbox_playsound.PropertyName = nameof(CoreConfiguration.PlayCameraSound);
            checkbox_capture_mousepointer.Text = Language.GetString("settings_capture_mousepointer");
            checkbox_capture_mousepointer.PropertyName = nameof(CoreConfiguration.CaptureMousepointer);
            label_waittime.Text = Language.GetString("settings_waittime");
            tab_output.Text = Language.GetString("settings_output");
            tab_destinations.Text = Language.GetString("settings_destination");
            tab_printer.Text = Language.GetString("settings_printer");
            groupBoxColors.Text = Language.GetString("printoptions_colors");
            checkboxPrintInverted.Text = Language.GetString("printoptions_inverted");
            checkboxPrintInverted.PropertyName = nameof(CoreConfiguration.OutputPrintInverted);
            radioBtnColorPrint.Text = Language.GetString("printoptions_printcolor");
            radioBtnGrayScale.Text = Language.GetString("printoptions_printgrayscale");
            radioBtnGrayScale.PropertyName = nameof(coreConfiguration.OutputPrintGrayscale);
            radioBtnMonochrome.Text = Language.GetString("printoptions_printmonochrome");
            radioBtnMonochrome.PropertyName = nameof(coreConfiguration.OutputPrintMonochrome);
            groupBoxPrintLayout.Text = Language.GetString("printoptions_layout");
            checkboxDateTime.Text = Language.GetString("printoptions_timestamp");
            checkboxDateTime.PropertyName = nameof(coreConfiguration.OutputPrintFooter);
            checkboxAllowShrink.Text = Language.GetString("printoptions_allowshrink");
            checkboxAllowShrink.PropertyName = nameof(coreConfiguration.OutputPrintAllowShrink);
            checkboxAllowEnlarge.Text = Language.GetString("printoptions_allowenlarge");
            checkboxAllowEnlarge.PropertyName = nameof(coreConfiguration.OutputPrintAllowEnlarge);
            checkboxAllowRotate.Text = Language.GetString("printoptions_allowrotate");
            checkboxAllowRotate.PropertyName = nameof(coreConfiguration.OutputPrintAllowRotate);
            checkboxAllowCenter.Text = Language.GetString("printoptions_allowcenter");
            checkboxAllowCenter.PropertyName = nameof(coreConfiguration.OutputPrintCenter);
            checkbox_alwaysshowprintoptionsdialog.Text = Language.GetString("settings_alwaysshowprintoptionsdialog");
            checkbox_alwaysshowprintoptionsdialog.PropertyName = nameof(coreConfiguration.OutputPrintPromptOptions);
            tab_plugins.Text = Language.GetString("settings_plugins");
            groupbox_plugins.Text = Language.GetString("settings_plugins");
            button_pluginconfigure.Text = Language.GetString("settings_configureplugin");
            tab_expert.Text = Language.GetString("expertsettings");
            groupbox_expert.Text = Language.GetString("expertsettings");
            checkbox_reuseeditor.Text = Language.GetString("expertsettings_reuseeditorifpossible");
            checkbox_reuseeditor.PropertyName = nameof(Editor.Configuration.EditorConfiguration.ReuseEditor);
            checkbox_reuseeditor.SectionName = "Editor";
            checkbox_minimizememoryfootprint.Text = Language.GetString("expertsettings_minimizememoryfootprint");
            checkbox_minimizememoryfootprint.PropertyName = nameof(coreConfiguration.MinimizeWorkingSetSize);
            checkbox_checkunstableupdates.Text = Language.GetString("expertsettings_checkunstableupdates");
            checkbox_checkunstableupdates.PropertyName = nameof(coreConfiguration.CheckForUnstable);
            checkbox_suppresssavedialogatclose.Text = Language.GetString("expertsettings_suppresssavedialogatclose");
            checkbox_suppresssavedialogatclose.PropertyName = nameof(Editor.Configuration.EditorConfiguration.SuppressSaveDialogAtClose);
            checkbox_suppresssavedialogatclose.SectionName = "Editor";
            label_counter.Text = Language.GetString("expertsettings_counter");
            textbox_counter.PropertyName = nameof(coreConfiguration.OutputFileIncrementingNumber);
            label_footerpattern.Text = Language.GetString("expertsettings_footerpattern");
            textbox_footerpattern.PropertyName = nameof(coreConfiguration.OutputPrintFooterPattern);
            checkbox_thumbnailpreview.Text = Language.GetString("expertsettings_thumbnailpreview");
            checkbox_thumbnailpreview.PropertyName = nameof(coreConfiguration.ThumnailPreview);
            checkbox_optimizeforrdp.Text = Language.GetString("expertsettings_optimizeforrdp");
            checkbox_optimizeforrdp.PropertyName = nameof(coreConfiguration.OptimizeForRDP);
            checkbox_autoreducecolors.Text = Language.GetString("expertsettings_autoreducecolors");
            checkbox_autoreducecolors.PropertyName = nameof(coreConfiguration.OutputFileAutoReduceColors);
            label_clipboardformats.Text = Language.GetString("expertsettings_clipboardformats");
            checkbox_enableexpert.Text = Language.GetString("expertsettings_enableexpert");
            Text = Language.GetString("settings_title");
        }

        /// <summary>
        /// Adjust the icons etc to the supplied DPI settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dpiChangedEventArgs">DpiChangedEventArgs</param>
        private void AdjustToDpi(object sender, DpiChangedEventArgs dpiChangedEventArgs)
        {
            DisplaySettings();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Fix for Vista/XP differences
            trackBarJpegQuality.BackColor = Environment.OSVersion.Version.Major >= 6 ? SystemColors.Window : SystemColors.Control;

            // This makes it possible to still capture the settings screen
            fullscreen_hotkeyControl.Enter += EnterHotkeyControl;
            fullscreen_hotkeyControl.Leave += LeaveHotkeyControl;
            window_hotkeyControl.Enter += EnterHotkeyControl;
            window_hotkeyControl.Leave += LeaveHotkeyControl;
            region_hotkeyControl.Enter += EnterHotkeyControl;
            region_hotkeyControl.Leave += LeaveHotkeyControl;
            lastregion_hotkeyControl.Enter += EnterHotkeyControl;
            lastregion_hotkeyControl.Leave += LeaveHotkeyControl;
            // Changes for BUG-2077
            numericUpDown_daysbetweencheck.ValueChanged += NumericUpDownDaysbetweencheckOnValueChanged;

            // Expert mode, the clipboard formats
            foreach (ClipboardFormat clipboardFormat in Enum.GetValues(typeof(ClipboardFormat)))
            {
                ListViewItem item = listview_clipboardformats.Items.Add(Language.Translate(clipboardFormat));
                item.Tag = clipboardFormat;
                item.Checked = coreConfiguration.ClipboardFormats.Contains(clipboardFormat);
            }

            _daysBetweenCheckPreviousValue = (int) numericUpDown_daysbetweencheck.Value;
            DisplayPluginTab();
            UpdateUi();
            ExpertSettingsEnableState(false);
            DisplaySettings();
            CheckSettings();
        }

        /// <summary>
        /// This makes sure the check cannot be set to 1-6
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="eventArgs">EventArgs</param>
        private void NumericUpDownDaysbetweencheckOnValueChanged(object sender, EventArgs eventArgs)
        {
            int currentValue = (int) numericUpDown_daysbetweencheck.Value;

            // Check if we can into the forbidden range
            if (currentValue > 0 && currentValue < 7)
            {
                if (_daysBetweenCheckPreviousValue <= currentValue)
                {
                    numericUpDown_daysbetweencheck.Value = 7;
                }
                else
                {
                    numericUpDown_daysbetweencheck.Value = 0;
                }
            }

            if ((int) numericUpDown_daysbetweencheck.Value < 0)
            {
                numericUpDown_daysbetweencheck.Value = 0;
            }

            if ((int) numericUpDown_daysbetweencheck.Value > 365)
            {
                numericUpDown_daysbetweencheck.Value = 365;
            }

            _daysBetweenCheckPreviousValue = (int) numericUpDown_daysbetweencheck.Value;
        }

        private void EnterHotkeyControl(object sender, EventArgs e)
        {
            HotkeyManager.UnregisterHotkeys();
            _inHotkey = true;
        }

        private void LeaveHotkeyControl(object sender, EventArgs e)
        {
            HotkeyHelper.RegisterHotkeys();
            _inHotkey = false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    if (!_inHotkey)
                    {
                        DialogResult = DialogResult.Cancel;
                    }
                    else
                    {
                        return base.ProcessCmdKey(ref msg, keyData);
                    }

                    break;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }

            return true;
        }

        /// <summary>
        /// This is a method to populate the ComboBox
        /// with the items from the enumeration
        /// </summary>
        /// <param name="comboBox">ComboBox to populate</param>
        /// <param name="availableValues"></param>
        /// <param name="selectedValue"></param>
        private void PopulateComboBox<TEnum>(ComboBox comboBox, TEnum[] availableValues, TEnum selectedValue) where TEnum : struct
        {
            comboBox.Items.Clear();
            foreach (TEnum enumValue in availableValues)
            {
                comboBox.Items.Add(Language.Translate(enumValue));
            }

            comboBox.SelectedItem = Language.Translate(selectedValue);
        }


        /// <summary>
        /// Get the selected enum value from the combobox, uses generics
        /// </summary>
        /// <param name="comboBox">Combobox to get the value from</param>
        /// <returns>The generics value of the combobox</returns>
        private TEnum GetSelected<TEnum>(ComboBox comboBox)
        {
            string enumTypeName = typeof(TEnum).Name;
            string selectedValue = comboBox.SelectedItem as string;
            TEnum[] availableValues = (TEnum[]) Enum.GetValues(typeof(TEnum));
            TEnum returnValue = availableValues[0];
            foreach (TEnum enumValue in availableValues)
            {
                string translation = Language.GetString(enumTypeName + "." + enumValue);
                if (translation.Equals(selectedValue))
                {
                    returnValue = enumValue;
                    break;
                }
            }

            return returnValue;
        }

        private void SetWindowCaptureMode(WindowCaptureMode selectedWindowCaptureMode)
        {
            WindowCaptureMode[] availableModes;
            if (!DwmApi.IsDwmEnabled)
            {
                // Remove DWM from configuration, as DWM is disabled!
                if (coreConfiguration.WindowCaptureMode == WindowCaptureMode.Aero || coreConfiguration.WindowCaptureMode == WindowCaptureMode.AeroTransparent)
                {
                    coreConfiguration.WindowCaptureMode = WindowCaptureMode.GDI;
                }

                availableModes = new[]
                {
                    WindowCaptureMode.Auto, WindowCaptureMode.Screen, WindowCaptureMode.GDI
                };
            }
            else
            {
                availableModes = new[]
                {
                    WindowCaptureMode.Auto, WindowCaptureMode.Screen, WindowCaptureMode.GDI, WindowCaptureMode.Aero, WindowCaptureMode.AeroTransparent
                };
            }

            PopulateComboBox(combobox_window_capture_mode, availableModes, selectedWindowCaptureMode);
        }

        private void DisplayPluginTab()
        {
            if (!SimpleServiceProvider.Current.GetAllInstances<IGreenshotPlugin>().Any())
            {
                tabcontrol.TabPages.Remove(tab_plugins);
                return;
            }

            // Draw the Plugin listview
            listview_plugins.BeginUpdate();
            listview_plugins.Items.Clear();
            listview_plugins.Columns.Clear();
            string[] columns =
            {
                Language.GetString("settings_plugins_name"), Language.GetString("settings_plugins_version"), Language.GetString("settings_plugins_createdby"),
                Language.GetString("settings_plugins_dllpath")
            };
            foreach (string column in columns)
            {
                listview_plugins.Columns.Add(column);
            }

            PluginHelper.Instance.FillListView(listview_plugins);
            // Maximize Column size!
            for (int i = 0; i < listview_plugins.Columns.Count; i++)
            {
                listview_plugins.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
                int width = listview_plugins.Columns[i].Width;
                listview_plugins.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.HeaderSize);
                if (width > listview_plugins.Columns[i].Width)
                {
                    listview_plugins.Columns[i].Width = width;
                }
            }

            listview_plugins.EndUpdate();
            listview_plugins.Refresh();

            // Disable the configure button, it will be enabled when a plugin is selected AND isConfigurable
            button_pluginconfigure.Enabled = false;
        }

        /// <summary>
        /// Update the UI to reflect the language and other text settings
        /// </summary>
        private void UpdateUi()
        {
            if (coreConfiguration.HideExpertSettings)
            {
                tabcontrol.Controls.Remove(tab_expert);
            }

            _toolTip.SetToolTip(label_language, Language.GetString(LangKey.settings_tooltip_language));
            _toolTip.SetToolTip(label_storagelocation, Language.GetString(LangKey.settings_tooltip_storagelocation));
            _toolTip.SetToolTip(label_screenshotname, Language.GetString(LangKey.settings_tooltip_filenamepattern));
            _toolTip.SetToolTip(label_primaryimageformat, Language.GetString(LangKey.settings_tooltip_primaryimageformat));

            // Removing, otherwise we keep getting the event multiple times!
            combobox_language.SelectedIndexChanged -= Combobox_languageSelectedIndexChanged;

            // Initialize the Language ComboBox
            combobox_language.DisplayMember = "Description";
            combobox_language.ValueMember = "Ietf";
            // Set datasource last to prevent problems
            // See: https://www.codeproject.com/KB/database/scomlistcontrolbinding.aspx?fid=111644
            combobox_language.DataSource = Language.SupportedLanguages;
            if (Language.CurrentLanguage != null)
            {
                combobox_language.SelectedValue = Language.CurrentLanguage;
            }

            // Delaying the SelectedIndexChanged events until all is initiated
            combobox_language.SelectedIndexChanged += Combobox_languageSelectedIndexChanged;
            UpdateDestinationDescriptions();
            UpdateClipboardFormatDescriptions();
        }

        // Check the settings and somehow visibly mark when something is incorrect
        private bool CheckSettings()
        {
            return CheckFilenamePattern() && CheckStorageLocationPath();
        }

        private bool CheckFilenamePattern()
        {
            string filename = FilenameHelper.GetFilenameFromPattern(textbox_screenshotname.Text, coreConfiguration.OutputFileFormat, null);
            // we allow dynamically created subfolders, need to check for them, too
            string[] pathParts = filename.Split(Path.DirectorySeparatorChar);

            string filenamePart = pathParts[pathParts.Length - 1];
            var settingsOk = FilenameHelper.IsFilenameValid(filenamePart);

            for (int i = 0; settingsOk && i < pathParts.Length - 1; i++)
            {
                settingsOk = FilenameHelper.IsDirectoryNameValid(pathParts[i]);
            }

            DisplayTextBoxValidity(textbox_screenshotname, settingsOk);

            return settingsOk;
        }

        private bool CheckStorageLocationPath()
        {
            bool settingsOk = Directory.Exists(FilenameHelper.FillVariables(textbox_storagelocation.Text, false));
            DisplayTextBoxValidity(textbox_storagelocation, settingsOk);
            return settingsOk;
        }

        private void DisplayTextBoxValidity(GreenshotTextBox textbox, bool valid)
        {
            if (valid)
            {
                // "Added" feature #3547158
                textbox.BackColor = Environment.OSVersion.Version.Major >= 6 ? SystemColors.Window : SystemColors.Control;
            }
            else
            {
                textbox.BackColor = Color.Red;
            }
        }

        private void FilenamePatternChanged(object sender, EventArgs e)
        {
            CheckFilenamePattern();
        }

        private void StorageLocationChanged(object sender, EventArgs e)
        {
            CheckStorageLocationPath();
        }

        /// <summary>
        /// Show all destination descriptions in the current language
        /// </summary>
        private void UpdateDestinationDescriptions()
        {
            foreach (ListViewItem item in listview_destinations.Items)
            {
                if (item.Tag is IDestination destinationFromTag)
                {
                    item.Text = destinationFromTag.Description;
                }
            }
        }

        /// <summary>
        /// Show all clipboard format descriptions in the current language
        /// </summary>
        private void UpdateClipboardFormatDescriptions()
        {
            foreach (ListViewItem item in listview_clipboardformats.Items)
            {
                ClipboardFormat cf = (ClipboardFormat) item.Tag;
                item.Text = Language.Translate(cf);
            }
        }

        /// <summary>
        /// Build the view with all the destinations
        /// </summary>
        private void DisplayDestinations()
        {
            bool destinationsEnabled = true;
            if (coreConfiguration.Values.ContainsKey("Destinations"))
            {
                destinationsEnabled = !coreConfiguration.Values["Destinations"].IsFixed;
            }

            checkbox_picker.Checked = false;

            listview_destinations.Items.Clear();
            var scaledIconSize = DpiCalculator.ScaleWithDpi(coreConfiguration.IconSize, NativeDpiMethods.GetDpi(Handle));
            listview_destinations.ListViewItemSorter = new ListviewWithDestinationComparer();
            ImageList imageList = new ImageList
            {
                ImageSize = scaledIconSize
            };
            listview_destinations.SmallImageList = imageList;
            int imageNr = -1;
            foreach (IDestination currentDestination in DestinationHelper.GetAllDestinations())
            {
                Image destinationImage = currentDestination.DisplayIcon;
                if (destinationImage != null)
                {
                    imageList.Images.Add(currentDestination.DisplayIcon);
                    imageNr++;
                }

                if (nameof(WellKnownDestinations.Picker).Equals(currentDestination.Designation))
                {
                    checkbox_picker.Checked = coreConfiguration.OutputDestinations.Contains(currentDestination.Designation);
                    checkbox_picker.Text = currentDestination.Description;
                }
                else
                {
                    ListViewItem item;
                    if (destinationImage != null)
                    {
                        item = listview_destinations.Items.Add(currentDestination.Description, imageNr);
                    }
                    else
                    {
                        item = listview_destinations.Items.Add(currentDestination.Description);
                    }

                    item.Tag = currentDestination;
                    item.Checked = coreConfiguration.OutputDestinations.Contains(currentDestination.Designation);
                }
            }

            if (checkbox_picker.Checked)
            {
                listview_destinations.Enabled = false;
                foreach (int index in listview_destinations.CheckedIndices)
                {
                    ListViewItem item = listview_destinations.Items[index];
                    item.Checked = false;
                }
            }

            checkbox_picker.Enabled = destinationsEnabled;
            listview_destinations.Enabled = destinationsEnabled;
        }

        private void DisplaySettings()
        {
            colorButton_window_background.SelectedColor = coreConfiguration.DWMBackgroundColor;

            if (Language.CurrentLanguage != null)
            {
                combobox_language.SelectedValue = Language.CurrentLanguage;
            }

            // Disable editing when the value is fixed
            combobox_language.Enabled = !coreConfiguration.Values["Language"].IsFixed;

            textbox_storagelocation.Text = FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false);
            // Disable editing when the value is fixed
            textbox_storagelocation.Enabled = !coreConfiguration.Values["OutputFilePath"].IsFixed;

            SetWindowCaptureMode(coreConfiguration.WindowCaptureMode);
            // Disable editing when the value is fixed
            combobox_window_capture_mode.Enabled = !coreConfiguration.CaptureWindowsInteractive && !coreConfiguration.Values["WindowCaptureMode"].IsFixed;
            radiobuttonWindowCapture.Checked = !coreConfiguration.CaptureWindowsInteractive;

            trackBarJpegQuality.Value = coreConfiguration.OutputFileJpegQuality;
            trackBarJpegQuality.Enabled = !coreConfiguration.Values["OutputFileJpegQuality"].IsFixed;
            textBoxJpegQuality.Text = coreConfiguration.OutputFileJpegQuality + "%";

            DisplayDestinations();

            numericUpDownWaitTime.Value = coreConfiguration.CaptureDelay >= 0 ? coreConfiguration.CaptureDelay : 0;
            numericUpDownWaitTime.Enabled = !coreConfiguration.Values["CaptureDelay"].IsFixed;
            if (IniConfig.IsPortable)
            {
                checkbox_autostartshortcut.Visible = false;
                checkbox_autostartshortcut.Checked = false;
            }
            else
            {
                // Autostart checkbox logic.
                if (StartupHelper.HasRunAll())
                {
                    // Remove runUser if we already have a run under all
                    StartupHelper.DeleteRunUser();
                    checkbox_autostartshortcut.Enabled = StartupHelper.CanWriteRunAll();
                    checkbox_autostartshortcut.Checked = true; // We already checked this
                }
                else if (StartupHelper.IsInStartupFolder())
                {
                    checkbox_autostartshortcut.Enabled = false;
                    checkbox_autostartshortcut.Checked = true; // We already checked this
                }
                else
                {
                    // No run for all, enable the checkbox and set it to true if the current user has a key
                    checkbox_autostartshortcut.Enabled = StartupHelper.CanWriteRunUser();
                    checkbox_autostartshortcut.Checked = StartupHelper.HasRunUser();
                }
            }

            numericUpDown_daysbetweencheck.Value = coreConfiguration.UpdateCheckInterval;
            numericUpDown_daysbetweencheck.Enabled = !coreConfiguration.Values["UpdateCheckInterval"].IsFixed;
            numericUpdownIconSize.Value = coreConfiguration.IconSize.Width;
            CheckDestinationSettings();
        }

        private void SaveSettings()
        {
            if (combobox_language.SelectedItem != null)
            {
                string newLang = combobox_language.SelectedValue.ToString();
                if (!string.IsNullOrEmpty(newLang))
                {
                    coreConfiguration.Language = combobox_language.SelectedValue.ToString();
                }
            }

            // retrieve the set clipboard formats
            var clipboardFormats = new List<ClipboardFormat>();
            foreach (int index in listview_clipboardformats.CheckedIndices)
            {
                var item = listview_clipboardformats.Items[index];
                if (item.Checked)
                {
                    clipboardFormats.Add((ClipboardFormat) item.Tag);
                }
            }

            coreConfiguration.ClipboardFormats = clipboardFormats;

            coreConfiguration.WindowCaptureMode = GetSelected<WindowCaptureMode>(combobox_window_capture_mode);
            if (!FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false).Equals(textbox_storagelocation.Text))
            {
                coreConfiguration.OutputFilePath = textbox_storagelocation.Text;
            }

            coreConfiguration.OutputFileJpegQuality = trackBarJpegQuality.Value;

            List<string> destinations = new List<string>();
            if (checkbox_picker.Checked)
            {
                destinations.Add(nameof(WellKnownDestinations.Picker));
            }

            foreach (int index in listview_destinations.CheckedIndices)
            {
                ListViewItem item = listview_destinations.Items[index];

                if (item.Checked && item.Tag is IDestination destinationFromTag)
                {
                    destinations.Add(destinationFromTag.Designation);
                }
            }

            coreConfiguration.OutputDestinations = destinations;
            coreConfiguration.CaptureDelay = (int) numericUpDownWaitTime.Value;
            coreConfiguration.DWMBackgroundColor = colorButton_window_background.SelectedColor;
            coreConfiguration.UpdateCheckInterval = (int) numericUpDown_daysbetweencheck.Value;

            coreConfiguration.IconSize = new Size((int) numericUpdownIconSize.Value, (int) numericUpdownIconSize.Value);

            try
            {
                if (checkbox_autostartshortcut.Checked)
                {
                    // It's checked, so we set the RunUser if the RunAll isn't set.
                    // Do this every time, so the executable is correct.
                    if (!StartupHelper.HasRunAll())
                    {
                        StartupHelper.SetRunUser();
                    }
                }
                else
                {
                    // Delete both settings if it's unchecked
                    if (StartupHelper.HasRunAll())
                    {
                        StartupHelper.DeleteRunAll();
                    }

                    if (StartupHelper.HasRunUser())
                    {
                        StartupHelper.DeleteRunUser();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn("Problem checking registry, ignoring for now: ", e);
            }
        }

        private void Settings_cancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void Settings_okayClick(object sender, EventArgs e)
        {
            if (CheckSettings())
            {
                HotkeyManager.UnregisterHotkeys();
                SaveSettings();
                StoreFields();
                HotkeyHelper.RegisterHotkeys();

                // Make sure the current language & settings are reflected in the Main-context menu
                var mainForm = SimpleServiceProvider.Current.GetInstance<MainForm>();
                mainForm.UpdateUi();
                DialogResult = DialogResult.OK;
            }
            else
            {
                tabcontrol.SelectTab(tab_output);
            }
        }

        private void BrowseClick(object sender, EventArgs e)
        {
            // Get the storage location and replace the environment variables
            string currentPath = FilenameHelper.FillVariables(textbox_storagelocation.Text, false);
            // Only use the path as the starting folder if it actually exists and is reachable;
            // otherwise leave SelectedPath empty so the dialog falls back to the default (My Documents).
            folderBrowserDialog1.SelectedPath = Directory.Exists(currentPath) ? currentPath : string.Empty;
            try
            {
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    // Only change if there is a change, otherwise we might overwrite the environment variables
                    if (folderBrowserDialog1.SelectedPath != null && !folderBrowserDialog1.SelectedPath.Equals(currentPath))
                    {
                        textbox_storagelocation.Text = folderBrowserDialog1.SelectedPath;
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                // This can happen when Greenshot was launched under a system/service account (e.g. after an
                // IT-managed silent install) and the shell cannot resolve the default root folder for the
                // current user context. Retry rooted at MyComputer with a known-good user folder so the
                // user can still pick a destination.
                Log.Warn("Problem opening folder browser dialog, retrying with user profile fallback: ", ex);
                string fallbackPath = GetAccessibleFallbackPath();
                folderBrowserDialog1.SelectedPath = fallbackPath;
                folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
                try
                {
                    if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                    {
                        textbox_storagelocation.Text = folderBrowserDialog1.SelectedPath;
                    }
                }
                catch (InvalidOperationException retryEx)
                {
                    Log.Error("Failed to open folder browser dialog even with fallback path: ", retryEx);
                    // Last resort: populate the textbox with the fallback path so the user at least
                    // has a valid, writable destination rather than a red invalid-path indicator.
                    if (!string.IsNullOrEmpty(fallbackPath))
                    {
                        textbox_storagelocation.Text = fallbackPath;
                        MessageBox.Show(
                            Language.GetString(LangKey.settings_storagelocation_folder_error),
                            Language.GetString(LangKey.settings_storagelocation_folder_error_title),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
                finally
                {
                    folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;
                }
            }
        }

        /// <summary>
        /// Returns the first accessible path suitable as a fallback storage location when the folder
        /// browser dialog cannot resolve the shell root (e.g. process started under a system account).
        /// Tries MyDocuments, Desktop, and TEMP in order.
        /// </summary>
        private static string GetAccessibleFallbackPath()
        {
            var candidates = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Path.GetTempPath()
            };
            foreach (string candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) && Directory.Exists(candidate))
                    return candidate;
            }
            return string.Empty;
        }

        private void TrackBarJpegQualityScroll(object sender, EventArgs e)
        {
            textBoxJpegQuality.Text = trackBarJpegQuality.Value.ToString(CultureInfo.InvariantCulture);
        }


        private void BtnPatternHelpClick(object sender, EventArgs e)
        {
            string filenamepatternText = Language.GetString(LangKey.settings_message_filenamepattern);
            // Convert %NUM% to ${NUM} for old language files!
            filenamepatternText = Regex.Replace(filenamepatternText, "%([a-zA-Z_0-9]+)%", @"${$1}");
            MessageBox.Show(filenamepatternText, Language.GetString(LangKey.settings_filenamepattern));
        }

        private void Listview_pluginsSelectedIndexChanged(object sender, EventArgs e)
        {
            button_pluginconfigure.Enabled = PluginHelper.Instance.IsSelectedItemConfigurable(listview_plugins);
        }

        private void Button_pluginconfigureClick(object sender, EventArgs e)
        {
            PluginHelper.Instance.ConfigureSelectedItem(listview_plugins);
        }

        private void Combobox_languageSelectedIndexChanged(object sender, EventArgs e)
        {
            // Get the combobox values BEFORE changing the language
            //EmailFormat selectedEmailFormat = GetSelected<EmailFormat>(combobox_emailformat);
            WindowCaptureMode selectedWindowCaptureMode = GetSelected<WindowCaptureMode>(combobox_window_capture_mode);
            if (combobox_language.SelectedItem != null)
            {
                Log.Debug("Setting language to: " + (string) combobox_language.SelectedValue);
                Language.CurrentLanguage = (string) combobox_language.SelectedValue;
            }

            // Reflect language changes to the settings form
            UpdateUi();

            // Reflect Language changes form
            InitializeLanguage();

            // Update the email & windows capture mode
            //SetEmailFormat(selectedEmailFormat);
            SetWindowCaptureMode(selectedWindowCaptureMode);
        }

        private void Combobox_window_capture_modeSelectedIndexChanged(object sender, EventArgs e)
        {
            int windowsVersion = Environment.OSVersion.Version.Major;
            WindowCaptureMode mode = GetSelected<WindowCaptureMode>(combobox_window_capture_mode);
            if (windowsVersion >= 6)
            {
                switch (mode)
                {
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
        private void CheckDestinationSettings()
        {
            bool clipboardDestinationChecked = false;
            bool pickerSelected = checkbox_picker.Checked;
            bool destinationsEnabled = true;
            if (coreConfiguration.Values.ContainsKey("Destinations"))
            {
                destinationsEnabled = !coreConfiguration.Values["Destinations"].IsFixed;
            }

            listview_destinations.Enabled = destinationsEnabled;

            foreach (int index in listview_destinations.CheckedIndices)
            {
                ListViewItem item = listview_destinations.Items[index];
                if (item.Tag is IDestination destinationFromTag && destinationFromTag.Designation.Equals(nameof(WellKnownDestinations.Clipboard)))
                {
                    clipboardDestinationChecked = true;
                    break;
                }
            }

            if (pickerSelected)
            {
                listview_destinations.Enabled = false;
                foreach (int index in listview_destinations.CheckedIndices)
                {
                    ListViewItem item = listview_destinations.Items[index];
                    item.Checked = false;
                }
            }
            else
            {
                // Prevent multiple clipboard settings at once, see bug #3435056
                if (clipboardDestinationChecked)
                {
                    checkbox_copypathtoclipboard.Checked = false;
                    checkbox_copypathtoclipboard.Enabled = false;
                }
                else
                {
                    checkbox_copypathtoclipboard.Enabled = true;
                }
            }
        }

        private void DestinationsCheckStateChanged(object sender, EventArgs e)
        {
            CheckDestinationSettings();
        }

        protected override void OnFieldsFilled()
        {
            // the color radio button is not actually bound to a setting, but checked when monochrome/grayscale are not checked
            if (!radioBtnGrayScale.Checked && !radioBtnMonochrome.Checked)
            {
                radioBtnColorPrint.Checked = true;
            }
        }

        /// <summary>
        /// Set the enable state of the expert settings
        /// </summary>
        /// <param name="state"></param>
        private void ExpertSettingsEnableState(bool state)
        {
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
        private void Checkbox_enableexpert_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                ExpertSettingsEnableState(checkBox.Checked);
            }
        }

        private void Radiobutton_CheckedChanged(object sender, EventArgs e)
        {
            combobox_window_capture_mode.Enabled = radiobuttonWindowCapture.Checked;
        }

        protected override void WndProc(ref Message m)
        {
            if (!WndProcDefaults.TryHandleMessage(ref m))
            {
                base.WndProc(ref m);
            }
        }
    }
}
