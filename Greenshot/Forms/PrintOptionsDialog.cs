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
using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace Greenshot.Forms {
	/// <summary>
	/// Description of PrintOptionsDialog.
	/// </summary>
	public partial class PrintOptionsDialog : Form {
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		
		public bool AllowPrintCenter;
		public bool AllowPrintEnlarge;
		public bool AllowPrintRotate;
		public bool AllowPrintShrink;
		public bool PrintDateTime;
		public bool PrintInverted;
		
		public PrintOptionsDialog() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
		
			this.AllowPrintCenter = this.checkboxAllowCenter.Checked = conf.OutputPrintCenter;
			this.AllowPrintEnlarge = this.checkboxAllowEnlarge.Checked = conf.OutputPrintAllowEnlarge;
			this.AllowPrintRotate = this.checkboxAllowRotate.Checked = conf.OutputPrintAllowRotate;
			this.AllowPrintShrink = this.checkboxAllowShrink.Checked = conf.OutputPrintAllowShrink;
			this.PrintInverted = this.checkboxPrintInverted.Checked = conf.OutputPrintInverted;
			this.PrintDateTime = this.checkboxDateTime.Checked = conf.OutputPrintFooter;
			this.checkbox_dontaskagain.Checked = false;
			UpdateUI();
		}
		
		void UpdateUI() {
			this.Text = Language.GetString(LangKey.printoptions_title);
			this.checkboxAllowCenter.Text = Language.GetString(LangKey.printoptions_allowcenter);
			this.checkboxAllowEnlarge.Text = Language.GetString(LangKey.printoptions_allowenlarge);
			this.checkboxAllowRotate.Text = Language.GetString(LangKey.printoptions_allowrotate);
			this.checkboxAllowShrink.Text = Language.GetString(LangKey.printoptions_allowshrink);
			this.checkbox_dontaskagain.Text = Language.GetString(LangKey.printoptions_dontaskagain);
			this.checkboxDateTime.Text = Language.GetString(LangKey.printoptions_timestamp);
			this.checkboxPrintInverted.Text = Language.GetString(LangKey.printoptions_inverted);
		}
		
		
		void Button_okClick(object sender, EventArgs e) {
			this.AllowPrintCenter = this.checkboxAllowCenter.Checked;
			this.AllowPrintEnlarge = this.checkboxAllowEnlarge.Checked;
			this.AllowPrintRotate = this.checkboxAllowRotate.Checked;
			this.AllowPrintShrink = this.checkboxAllowShrink.Checked;
			this.PrintDateTime = this.checkboxDateTime.Checked;
			this.PrintInverted = this.checkboxPrintInverted.Checked;

			// update config
			conf.OutputPrintCenter = this.AllowPrintCenter;
			conf.OutputPrintAllowEnlarge = this.AllowPrintEnlarge;
			conf.OutputPrintAllowRotate = this.AllowPrintRotate;
			conf.OutputPrintAllowShrink = this.AllowPrintShrink;
			conf.OutputPrintFooter = this.PrintDateTime;
			conf.OutputPrintInverted = this.PrintInverted;
			conf.OutputPrintPromptOptions = !this.checkbox_dontaskagain.Checked;
			IniConfig.Save();
		}
	}
}
