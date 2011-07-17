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
using System.Drawing;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;

namespace Greenshot.Forms
{
	/// <summary>
	/// Description of DestinationPickerForm.
	/// </summary>
	public partial class DestinationPickerForm : Form {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(DestinationPickerForm));
		private static CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();
		private static ILanguage lang = Language.GetInstance();
		public DestinationPickerForm() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Text = lang.GetString(LangKey.settings_destination);
			AddCheckBox(flowLayoutPanel1, Destination.Clipboard, lang.GetString(LangKey.settings_destination_clipboard));
			AddCheckBox(flowLayoutPanel1, Destination.Editor, lang.GetString(LangKey.settings_destination_editor));
			AddCheckBox(flowLayoutPanel1, Destination.EMail, lang.GetString(LangKey.settings_destination_email));
			AddCheckBox(flowLayoutPanel1, Destination.FileDefault, lang.GetString(LangKey.settings_destination_file));
			AddCheckBox(flowLayoutPanel1, Destination.FileWithDialog, lang.GetString(LangKey.settings_destination_fileas));
			AddCheckBox(flowLayoutPanel1, Destination.Printer, lang.GetString(LangKey.settings_destination_printer));
		}
		
		public void ShowAtMouse(IWin32Window owner) {
			this.Show(owner);
			Point target = Cursor.Position;
			target.X -= this.Width / 2;
			target.Y -= this.Height / 2;
			this.Location = target;
		}
		
		private void AddCheckBox(Panel panel, Destination destination, string text) {
			CheckBox checkbox = new CheckBox();
			checkbox.Text = text;
			//checkbox.Width = 200;
			//checkbox.Height = 20;
			checkbox.AutoSize = true;
			if (config.OutputDestinations.Contains(destination)) {
				checkbox.Checked = true;
			}
			panel.Controls.Add(checkbox);
		}
	}
}
