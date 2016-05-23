/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.IniFile;

namespace Greenshot.Forms {
	/// <summary>
	/// Description of PrintOptionsDialog.
	/// </summary>
	public partial class PrintOptionsDialog : BaseForm {
		public PrintOptionsDialog() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			checkbox_dontaskagain.Checked = false;
		}
		
		
		void Button_okClick(object sender, EventArgs e) {
			// update config
			coreConfiguration.OutputPrintPromptOptions = !checkbox_dontaskagain.Checked;
			IniConfig.Save();
			DialogResult = DialogResult.OK;
		}

        protected override void OnFieldsFilled() {
            // the color radio button is not actually bound to a setting, but checked when monochrome/grayscale are not checked
            if(!radioBtnGrayScale.Checked && !radioBtnMonochrome.Checked) {
                radioBtnColorPrint.Checked = true;
            }
        }
	}
}
