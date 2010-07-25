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
using GreenshotPlugin.Core;

namespace Greenshot.Forms {
	/// <summary>
	/// Description of PrintOptionsDialog.
	/// </summary>
	public partial class PrintOptionsDialog : Form {
		AppConfig conf;
		ILanguage lang;
		
		public bool AllowPrintCenter;
		public bool AllowPrintEnlarge;
		public bool AllowPrintRotate;
		public bool AllowPrintShrink;
		public bool PrintDateTime;
		
		public PrintOptionsDialog()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			conf = AppConfig.GetInstance();
			lang = Language.GetInstance();
			
			this.AllowPrintCenter = this.checkboxAllowCenter.Checked = (bool)conf.Output_Print_Center;
			this.AllowPrintEnlarge = this.checkboxAllowEnlarge.Checked = (bool)conf.Output_Print_AllowEnlarge;
			this.AllowPrintRotate = this.checkboxAllowRotate.Checked = (bool)conf.Output_Print_AllowRotate;
			this.AllowPrintShrink = this.checkboxAllowShrink.Checked = (bool)conf.Output_Print_AllowShrink;
			this.PrintDateTime = this.checkboxDateTime.Checked = (bool)conf.Output_Print_Timestamp;
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
		
		
		void Button_okClick(object sender, EventArgs e)
		{
			this.AllowPrintCenter = this.checkboxAllowCenter.Checked;
			this.AllowPrintEnlarge = this.checkboxAllowEnlarge.Checked;
			this.AllowPrintRotate = this.checkboxAllowRotate.Checked;
			this.AllowPrintShrink = this.checkboxAllowShrink.Checked;
			this.PrintDateTime = this.checkboxDateTime.Checked;

			// update config
			conf.Output_Print_Center = (bool?)this.AllowPrintCenter;
			conf.Output_Print_AllowEnlarge = (bool?)this.AllowPrintEnlarge;
			conf.Output_Print_AllowRotate = (bool?)this.AllowPrintRotate;
			conf.Output_Print_AllowShrink = (bool?)this.AllowPrintShrink;
			conf.Output_Print_Timestamp = (bool?)this.PrintDateTime;
			conf.Output_Print_PromptOptions = !this.checkbox_dontaskagain.Checked;
			conf.Store();
			
		}
	}
}
