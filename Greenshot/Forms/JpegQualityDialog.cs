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
using System.Windows.Forms;
using Greenshot.Configuration;
using GreenshotPlugin.Core;
using IniFile;

namespace Greenshot {
	/// <summary>
	/// Description of JpegQualityDialog.
	/// </summary>
	public partial class JpegQualityDialog : Form {
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		ILanguage lang;
		public int Quality = 0;
		public JpegQualityDialog() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			lang = Language.GetInstance();
			this.trackBarJpegQuality.Value = conf.OutputFileJpegQuality;
			this.textBoxJpegQuality.Text = conf.OutputFileJpegQuality.ToString();
			UpdateUI();
		}
		
		
		void Button_okClick(object sender, System.EventArgs e) {
			Quality = this.trackBarJpegQuality.Value;
			if(this.checkbox_dontaskagain.Checked) {
				conf.OutputFileJpegQuality = Quality;
				conf.OutputFilePromptJpegQuality = false;
				IniConfig.Save();
			}
		}
		
		void UpdateUI() {
			this.Text = lang.GetString(LangKey.jpegqualitydialog_title);
			this.label_choosejpegquality.Text = lang.GetString(LangKey.jpegqualitydialog_choosejpegquality);
			this.checkbox_dontaskagain.Text = lang.GetString(LangKey.jpegqualitydialog_dontaskagain);
		}
		
		void TrackBarJpegQualityScroll(object sender, System.EventArgs e) {
			textBoxJpegQuality.Text = trackBarJpegQuality.Value.ToString();
		}
	}
}
