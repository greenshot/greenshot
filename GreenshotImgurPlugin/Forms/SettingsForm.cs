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
using System.Windows.Forms;
using GreenshotImgurPlugin.Forms;
using GreenshotPlugin.Core;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// Description of PasswordRequestForm.
	/// </summary>
	public partial class SettingsForm : Form {
		private ILanguage lang = Language.GetInstance();

		public SettingsForm(ImgurConfiguration config) {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
			InitializeTexts();
			
			combobox_uploadimageformat.Items.Clear();
			foreach(OutputFormat format in Enum.GetValues(typeof(OutputFormat))) {
				combobox_uploadimageformat.Items.Add(format.ToString());
			}
			ImgurUtils.LoadHistory();

			if (config.runtimeImgurHistory.Count > 0) {
				historyButton.Enabled = true;
			} else {
				historyButton.Enabled = false;
			}
		}
				
		private void InitializeTexts() {
			this.label_url.Text = lang.GetString(LangKey.label_url);
			this.buttonOK.Text = lang.GetString(LangKey.OK);
			this.buttonCancel.Text = lang.GetString(LangKey.CANCEL);
			this.Text = lang.GetString(LangKey.settings_title);
			this.label_upload_format.Text = lang.GetString(LangKey.label_upload_format);
			this.checkbox_usepagelink.Text = lang.GetString(LangKey.use_page_link);
			this.historyButton.Text = lang.GetString(LangKey.imgur_history);
		}

		public string Url {
			get {return textBoxUrl.Text;}
			set {textBoxUrl.Text = value;}
		}

		public string UploadFormat {
			get {return combobox_uploadimageformat.Text;}
			set {combobox_uploadimageformat.Text = value;}
		}

		public bool UsePageLink {
			get { return checkbox_usepagelink.Checked; }
			set { checkbox_usepagelink.Checked = value; }
		}

		void ButtonOKClick(object sender, EventArgs e) {
			this.DialogResult = DialogResult.OK;
		}
		
		void ButtonCancelClick(object sender, System.EventArgs e) {
			this.DialogResult = DialogResult.Cancel;
		}
		
		void ButtonHistoryClick(object sender, EventArgs e) {
			ImgurHistory.ShowHistory();
		}
	}
}
