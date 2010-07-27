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
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Core;

namespace Greenshot {
	/// <summary>
	/// Description of AboutForm.
	/// </summary>
	public partial class AboutForm : Form {
		private ILanguage lang;

		public AboutForm() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			Version v = Assembly.GetExecutingAssembly().GetName().Version;
			// Format is like this:  AssemblyVersion("Major.Minor.Build.Revision")]
			lblTitle.Text = "Greenshot " + v.Major + "." + v.Minor + "." + v.Build + " Build " + v.Revision.ToString("0000");
			lang = Language.GetInstance();
			updateUI();
		}
		
		void updateUI() {
			this.Text = lang.GetString(LangKey.about_title);
			this.lblLicense.Text = lang.GetString(LangKey.about_license);
			this.lblHost.Text = lang.GetString(LangKey.about_host);
			this.lblBugs.Text = lang.GetString(LangKey.about_bugs);
			this.lblDonations.Text = lang.GetString(LangKey.about_donations);
			this.lblIcons.Text = lang.GetString(LangKey.about_icons);
			this.lblTranslation.Text = lang.GetString(LangKey.about_translation);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
			try {
				if (msg.WParam.ToInt32() == (int)Keys.Escape) {
				    this.Close();
				} else {
				    return base.ProcessCmdKey(ref msg, keyData);
				}
			} catch (Exception) {
			}
			return base.ProcessCmdKey(ref msg,keyData);
		}
	}
}
