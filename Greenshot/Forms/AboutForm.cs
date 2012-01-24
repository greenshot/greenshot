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
using System.Reflection;
using System.Windows.Forms;
using System.IO;

using Greenshot.Helpers;
using Greenshot.Configuration;
using GreenshotPlugin.Core;
using IniFile;

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
			lblTitle.Text = "Greenshot " + v.Major + "." + v.Minor + "." + v.Build + " Build " + v.Revision + (IniConfig.IsPortable?" Portable":"") + (" (" + OSInfo.Bits +" bit)");
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
				switch (keyData) {
					case Keys.Escape:
						DialogResult = DialogResult.OK;
						break;
					case Keys.E:
						MessageBox.Show(EnvironmentInfo.EnvironmentToString(true));
						break;
					case Keys.L:
						try {
							if (File.Exists( MainForm.LogFileLocation)) {
								System.Diagnostics.Process.Start("\"" + MainForm.LogFileLocation + "\"");
							} else {
								MessageBox.Show("Greenshot can't write to logfile, otherwise it would be here: " + MainForm.LogFileLocation);
							}
						} catch (Exception) {
							MessageBox.Show("Couldn't open the greenshot.log, it's located here: " + MainForm.LogFileLocation, "Error opening greeenshot.log", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						}
						break;
					case Keys.I:
						try {
							System.Diagnostics.Process.Start("\"" + IniFile.IniConfig.ConfigLocation + "\"");
						} catch (Exception) {
							MessageBox.Show("Couldn't open the greenshot.ini, it's located here: " + IniFile.IniConfig.ConfigLocation, "Error opening greeenshot.ini", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						}
						break;
					default:
						return base.ProcessCmdKey(ref msg, keyData);
				}
			} catch (Exception) {
			}
			return true;
		}
	}
}
