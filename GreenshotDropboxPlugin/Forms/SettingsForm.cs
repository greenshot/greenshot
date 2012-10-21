/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using GreenshotDropboxPlugin.Forms;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotDropboxPlugin {
	/// <summary>
	/// Description of PasswordRequestForm.
	/// </summary>
	public partial class SettingsForm : DropboxForm {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SettingsForm));
		private static DropboxPluginConfiguration config = IniConfig.GetIniSection<DropboxPluginConfiguration>();

		public SettingsForm() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
		}
		
		void ButtonOKClick(object sender, EventArgs e) {
			this.DialogResult = DialogResult.OK;
		}
		
		void ButtonCancelClick(object sender, System.EventArgs e) {
			this.DialogResult = DialogResult.Cancel;
		}
	}
}
