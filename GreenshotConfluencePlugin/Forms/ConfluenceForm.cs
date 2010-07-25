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

using Confluence;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Description of ConfluenceForm.
	/// </summary>
	public partial class ConfluenceForm : Form {
		private ConfluenceConnector confluence;
		public ConfluenceForm(ConfluenceConnector confluence) {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			this.confluence = confluence;
		}
		
		public void setFilename(string filename) {
			textBox_filename.Text = filename;
		}

		public void upload(byte [] buffer) {
			Page page = confluence.getPage(textBox_space.Text, textBox_page.Text);
			confluence.addAttachment(page.id, " image/png", "HALLO", textBox_filename.Text, buffer);
		}
		
		void ButtonCancelClick(object sender, EventArgs e) {
			this.DialogResult = DialogResult.Cancel;
		}

		void ButtonOKClick(object sender, EventArgs e) {
			this.DialogResult = DialogResult.OK;
		}
	}
}
