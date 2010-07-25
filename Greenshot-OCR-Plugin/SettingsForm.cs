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
using System.Windows.Forms;

using GreenshotPlugin.Core;

namespace GreenshotOCR {
	/// <summary>
	/// Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : Form {
		private ILanguage language = Language.GetInstance();
		private string selectedLanguage;
		private bool orientImage;
		private bool straightenImage;
		        
		public SettingsForm(string [] languages, Properties config) {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			language.SynchronizeLanguageToCulture();
			initializeComponentText();
			
			comboBox_languages.Items.Clear();
			int index=0;
			foreach(string availableLanguage in languages) {
				string displayLanguage = cleanLanguage(availableLanguage);
				comboBox_languages.Items.Add(displayLanguage);
				if (availableLanguage.Equals(config["language"])) {
					comboBox_languages.SelectedIndex = index;
				}
				index++;
			}
			checkBox_orientImage.Checked = config.GetBoolProperty("orientImage");
			checkBox_straightenImage.Checked = config.GetBoolProperty("straightenImage");
		}
		private void initializeComponentText() {
			this.label_language.Text = language.GetString(LangKey.language);
			this.checkBox_orientImage.Text = language.GetString(LangKey.orient_image);
		}
		
		private string cleanLanguage(string suppliedLanguage) {
			string displayLanguage = "";
			if (suppliedLanguage != null) {
				displayLanguage = suppliedLanguage.Replace("miLANG_","").Replace("_"," ");
				displayLanguage = displayLanguage.Substring(0, 1).ToUpper() + displayLanguage.Substring(1).ToLower();
			}
			return displayLanguage;
		}
		
		public string OCRLanguage {
			get {return selectedLanguage;}
		}
		public bool OrientImage {
			get {return orientImage;}
		}
		public bool StraightenImage {
			get {return straightenImage;}
		}
		
		void ButtonCancelClick(object sender, EventArgs e) {
			DialogResult = DialogResult.Cancel;
		}
		
		void ButtonOKClick(object sender, EventArgs e) {
			string selectedString = (string) comboBox_languages.SelectedItem;
			if (selectedString != null) {
				selectedLanguage = "miLANG_" + selectedString.ToUpper().Replace(" ", "_");
			}
			orientImage = checkBox_orientImage.Checked;
			straightenImage = checkBox_straightenImage.Checked;

			DialogResult = DialogResult.OK;
			
		}
	}
}
