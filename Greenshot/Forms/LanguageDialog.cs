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
		private ILanguage language = Language.GetInstance();
		private LanguageDialog() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Load += FormLoad;
			
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
			this.comboBoxLanguage.DataSource = language.SupportedLanguages;
			
			if (language.CurrentLanguage != null) {
				LOG.DebugFormat("Selecting {0}", language.CurrentLanguage);
				this.comboBoxLanguage.SelectedValue = language.CurrentLanguage;
			}
		}
		
		void BtnOKClick(object sender, EventArgs e) {
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
