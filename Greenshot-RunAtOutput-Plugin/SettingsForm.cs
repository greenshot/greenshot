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

namespace RunAtOutput {
	/// <summary>
	/// Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : Form {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SettingsForm));
		private Configuration config;
		
		public SettingsForm(Configuration config) {
			this.config = config;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			UpdateView();
		}
		
		void ButtonOkClick(object sender, EventArgs e) {
			DialogResult = DialogResult.OK;
		}
		
		void ButtonCancelClick(object sender, EventArgs e) {
			DialogResult = DialogResult.Cancel;
		}

		void ShowSelectedItem() {
			foreach ( ListViewItem item in listView1.SelectedItems ) {
				Commando commando = item.Tag as Commando;
				textBox_arguments.Text = commando.Arguments;
				textBox_commandline.Text = commando.Commandline;
				textBox_name.Text = commando.Name;
			}
		}
		
		void ButtonAddClick(object sender, EventArgs e) {
			Commando commando = new Commando();
			commando.Arguments = textBox_arguments.Text;
			commando.Commandline = textBox_commandline.Text;
			commando.Name = textBox_name.Text;
			config.Commands.Add(commando);
			UpdateView();
		}

		void ButtonDeleteClick(object sender, EventArgs e) {
			foreach ( ListViewItem item in listView1.SelectedItems ) {
				Commando commando = item.Tag as Commando;
				config.Commands.Remove(commando);
			}
			UpdateView();
		}

		void UpdateView() {
			listView1.Items.Clear();
			foreach(Commando commando in config.Commands) {
				ListViewItem item = new ListViewItem("");
				item.SubItems.Add(commando.Name);
				item.Checked = commando.Active;
				item.Tag = commando;
				listView1.Items.Add(item);
			}
		}
		
		
		void ListView1ItemSelectionChanged(object sender, EventArgs e) {
			ShowSelectedItem();
		}
		
		void ListView1ItemChecked(object sender, ItemCheckedEventArgs e) {
			Commando commando = e.Item.Tag as Commando;
			LOG.Debug("ItemChecked " + commando.Name + " to " + e.Item.Checked);
			commando.Active = e.Item.Checked;
		}
	}
}
