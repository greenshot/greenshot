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
using System.Threading;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;

namespace Greenshot.Forms {
	/// <summary>
	/// Description of LanguageDialog.
	/// </summary>
	public partial class LanguageDialog : Form {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(LanguageDialog));
		private static LanguageDialog uniqueInstance;
		private bool properOkPressed = false;

		private LanguageDialog() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
			this.Load += FormLoad;
			this.FormClosing += PreventFormClose;
		}
		
		private void PreventFormClose(object sender, FormClosingEventArgs e) {
			if(!properOkPressed) {
				e.Cancel = true;
			}
		}

		public string SelectedLanguage {
			get { return comboBoxLanguage.SelectedValue.ToString(); }
		}
		
		protected void FormLoad(object sender, EventArgs e) {
			// Initialize the Language ComboBox
			this.comboBoxLanguage.DisplayMember = "Description";
			this.comboBoxLanguage.ValueMember = "Ietf";

			// Set datasource last to prevent problems
			// See: http://www.codeproject.com/KB/database/scomlistcontrolbinding.aspx?fid=111644
			this.comboBoxLanguage.DataSource = Language.SupportedLanguages;

			if (Language.CurrentLanguage != null) {
				LOG.DebugFormat("Selecting {0}", Language.CurrentLanguage);
				this.comboBoxLanguage.SelectedValue = Language.CurrentLanguage;
			} else {
				this.comboBoxLanguage.SelectedValue = Thread.CurrentThread.CurrentUICulture.Name;
			}

			// Close again when there is only one language, this shows the form briefly!
			// But the use-case is not so interesting, only happens once, to invest a lot of time here.
			if (Language.SupportedLanguages.Count == 1) {
				this.comboBoxLanguage.SelectedValue = Language.SupportedLanguages[0].Ietf;
				Language.CurrentLanguage = SelectedLanguage;
				properOkPressed = true;
				this.Close();
			}
		}
		
		void BtnOKClick(object sender, EventArgs e) {
			properOkPressed = true;
			// Fix for Bug #3431100 
			Language.CurrentLanguage = SelectedLanguage;
			this.Close();
		}
		
		public static LanguageDialog GetInstance() {
			if(uniqueInstance == null) {
				uniqueInstance = new LanguageDialog();
			}
			return uniqueInstance;
		}
	}
}
