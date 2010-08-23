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
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Core;

namespace Greenshot.Forms {
	/// <summary>
	/// Description of PrintOptionsDialog.
	/// </summary>
	public partial class PrintOptionsDialog : Form {
		private CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private ILanguage lang;
		
		public PrintOptionsDialog() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			lang = Language.GetInstance();
			
			this.checkboxAllowCenter.Checked = conf.OutputPrintCenter;
			this.checkboxAllowEnlarge.Checked = conf.OutputPrintAllowEnlarge;
			this.checkboxAllowRotate.Checked = conf.OutputPrintAllowRotate;
			this.checkboxAllowShrink.Checked = conf.OutputPrintAllowShrink;
			this.checkboxDateTime.Checked = conf.OutputPrintTimestamp;
			this.checkbox_dontaskagain.Checked = false;
			UpdateUI();
		}
		
		void UpdateUI() {
			this.Text = lang.GetString(LangKey.printoptions_title);
			this.checkboxAllowCenter.Text = lang.GetString(LangKey.printoptions_allowcenter);
			this.checkboxAllowEnlarge.Text = lang.GetString(LangKey.printoptions_allowenlarge);
			this.checkboxAllowRotate.Text = lang.GetString(LangKey.printoptions_allowrotate);
			this.checkboxAllowShrink.Text = lang.GetString(LangKey.printoptions_allowshrink);
			this.checkbox_dontaskagain.Text = lang.GetString(LangKey.printoptions_dontaskagain);
			this.checkboxDateTime.Text = lang.GetString(LangKey.printoptions_timestamp);
		}
		
		
		void Button_okClick(object sender, EventArgs e) {
			// update config
			conf.OutputPrintCenter = this.checkboxAllowCenter.Checked;
			conf.OutputPrintAllowEnlarge = this.checkboxAllowEnlarge.Checked;
			conf.OutputPrintAllowRotate = this.checkboxAllowRotate.Checked;
			conf.OutputPrintAllowShrink = this.checkboxAllowShrink.Checked;
			conf.OutputPrintTimestamp = this.checkboxDateTime.Checked;
			conf.OutputPrintPromptOptions = !this.checkbox_dontaskagain.Checked;
			IniConfig.Save();
		}
	}
}
