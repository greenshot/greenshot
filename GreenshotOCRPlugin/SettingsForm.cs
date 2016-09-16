/*
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
using System;

namespace GreenshotOCR {
	/// <summary>
	/// Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : OCRForm {
		private readonly OCRConfiguration config;

		public SettingsForm(string [] languages, OCRConfiguration config) {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			this.config = config;
			InitializeComponent();
			AcceptButton = buttonOK;
			CancelButton = buttonCancel;
			
			comboBox_languages.Items.Clear();
			int index=0;

			// Preventing Tracker #3234560, although this should not happen...
			string languageFromConfig = "ENGLISH";
			if (config.Language != null) {
				languageFromConfig  = config.Language;
			}
			foreach(string availableLanguage in languages) {
				string displayLanguage = availableLanguage.Substring(0, 1).ToUpper() + availableLanguage.Substring(1).ToLower();
				comboBox_languages.Items.Add(displayLanguage);
				if (availableLanguage.Equals(languageFromConfig, StringComparison.CurrentCultureIgnoreCase)) {
					comboBox_languages.SelectedIndex = index;
				}
				index++;
			}
		}

		private void ButtonOKClick(object sender, EventArgs e) {
			string selectedString = (string) comboBox_languages.SelectedItem;
			if (selectedString != null) {
				config.Language = selectedString.ToUpper();
			}
		}
	}
}
