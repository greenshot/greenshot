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

using Greenshot.Core;

namespace RunAtOutput {
	/// <summary>
	/// Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : Form {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SettingsForm));
		private RunAtOutputConfiguration config;
		
		public SettingsForm() {
			this.config = IniConfig.GetIniSection<RunAtOutputConfiguration>();
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			UpdateView();
		}
		
		void ButtonOkClick(object sender, EventArgs e) {
			IniConfig.Save();
			DialogResult = DialogResult.OK;
		}
		
		void ButtonCancelClick(object sender, EventArgs e) {
			DialogResult = DialogResult.Cancel;
		}

		void ShowSelectedItem() {
			foreach ( ListViewItem item in listView1.SelectedItems ) {
				string commando = item.Tag as string;
				textBox_name.Text = commando;
				textBox_commandline.Text = config.commandlines[commando];
				textBox_arguments.Text = config.arguments[commando];
			}
		}
		
		void ButtonAddClick(object sender, EventArgs e) {
			config.commands.Add(textBox_name.Text);
			config.commandlines.Add(textBox_name.Text, textBox_commandline.Text);
			config.arguments.Add(textBox_name.Text, textBox_arguments.Text);
			UpdateView();
		}

		void ButtonDeleteClick(object sender, EventArgs e) {
			foreach ( ListViewItem item in listView1.SelectedItems ) {
				string commando = item.Tag as string;
				config.active.Remove(textBox_name.Text);
				config.commands.Remove(textBox_name.Text);
				config.commandlines.Remove(textBox_name.Text);
				config.arguments.Remove(textBox_name.Text);
			}
			UpdateView();
		}

		void UpdateView() {
			listView1.Items.Clear();
			if (config.commands != null) {
				foreach(string commando in config.commands) {
					ListViewItem item = new ListViewItem("");
					item.SubItems.Add(commando);
					if (config.active != null) {
						item.Checked = config.active.Contains(commando);
					}
					item.Tag = commando;
					listView1.Items.Add(item);
				}
			}
		}
		
		
		void ListView1ItemSelectionChanged(object sender, EventArgs e) {
			ShowSelectedItem();
		}
		
		void ListView1ItemChecked(object sender, ItemCheckedEventArgs e) {
			string commando = e.Item.Tag as string;
			LOG.Debug("ItemChecked " + commando + " to " + e.Item.Checked);
			if (e.Item.Checked && !config.active.Contains(commando)) {
				config.active.Add(commando);
			}
			if (!e.Item.Checked) {
				config.active.Remove(commando);
			}
		}
	}
}
