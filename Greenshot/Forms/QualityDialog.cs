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
using Greenshot.Plugin;

namespace Greenshot {
	/// <summary>
	/// Description of JpegQualityDialog.
	/// </summary>
	public partial class QualityDialog : Form {
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		public OutputSettings Settings {
			get;
			set;
		}
		public QualityDialog(OutputSettings outputSettings) {
			Settings = outputSettings;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();

			this.checkBox_reduceColors.Checked = Settings.ReduceColors;
			this.trackBarJpegQuality.Enabled = OutputFormat.jpg.Equals(outputSettings.Format);
			this.trackBarJpegQuality.Value = Settings.JPGQuality;
			this.textBoxJpegQuality.Enabled = OutputFormat.jpg.Equals(outputSettings.Format);
			this.textBoxJpegQuality.Text = Settings.JPGQuality.ToString();
			UpdateUI();
			WindowDetails.ToForeground(Handle);
		}
		
		void Button_okClick(object sender, System.EventArgs e) {
			Settings.JPGQuality = this.trackBarJpegQuality.Value;
			Settings.ReduceColors = checkBox_reduceColors.Checked;
			if (this.checkbox_dontaskagain.Checked) {
				conf.OutputFileJpegQuality = Settings.JPGQuality;
				conf.OutputFilePromptQuality = false;
				conf.OutputFileReduceColors = Settings.ReduceColors;
				IniConfig.Save();
			}
		}
		
		void UpdateUI() {
			this.Text = Language.GetString(LangKey.jpegqualitydialog_title);
			this.label_choosejpegquality.Text = Language.GetString(LangKey.jpegqualitydialog_choosejpegquality);
			this.checkbox_dontaskagain.Text = Language.GetString(LangKey.jpegqualitydialog_dontaskagain);
			this.checkBox_reduceColors.Text = Language.GetString(LangKey.settings_reducecolors);
		}
		
		void TrackBarJpegQualityScroll(object sender, System.EventArgs e) {
			textBoxJpegQuality.Text = trackBarJpegQuality.Value.ToString();
		}
	}
}
