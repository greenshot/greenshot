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
using System.IO;
using System.Windows.Forms;

using GreenshotPlugin.Core;
using IniFile;

namespace ExternalCommand {
	/// <summary>
	/// Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : Form {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SettingsForm));
		private static ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		
		public SettingsForm() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			UpdateView();
		}
		
		void ButtonOkClick(object sender, EventArgs e) {
			// Update the details with those in the textboxes, if one is selected
			foreach(string command in config.commands) {
				if (command.Equals(textBox_name.Text)) {
					config.commandlines[command] = textBox_commandline.Text;
					config.arguments[command] = textBox_arguments.Text;
				}
			}
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
			string commandName = textBox_name.Text;
			string commandLine = textBox_commandline.Text;
			string arguments = textBox_arguments.Text;
			if (config.commands.Contains(commandName)) {
				config.commandlines[commandName] = commandLine;
				config.arguments[commandName] = arguments;
			} else {
				config.commands.Add(commandName);
				config.commandlines.Add(commandName, commandLine);
				config.arguments.Add(commandName, arguments);
			}
			UpdateView();
		}

		void ButtonDeleteClick(object sender, EventArgs e) {
			foreach ( ListViewItem item in listView1.SelectedItems ) {
				string commando = item.Tag as string;
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
					ListViewItem item = new ListViewItem(commando);
					item.Tag = commando;
					listView1.Items.Add(item);
				}
			}
		}

		void ListView1ItemSelectionChanged(object sender, EventArgs e) {
			ShowSelectedItem();
		}
		
		void Button3Click(object sender, EventArgs e) {
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Executables (*.exe, *.bat, *.com)|*.exe; *.bat; *.com|All files (*)|*";
			openFileDialog.FilterIndex = 1;
			openFileDialog.CheckFileExists = true;
			openFileDialog.Multiselect = false;
			string initialPath = null;
			try {
				initialPath = Path.GetDirectoryName(textBox_commandline.Text);
			} catch {}
			if (initialPath != null && Directory.Exists(initialPath)) {
				openFileDialog.InitialDirectory = initialPath;				
			} else {
				initialPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				openFileDialog.InitialDirectory = initialPath;
			}
			LOG.DebugFormat("Starting OpenFileDialog at {0}", initialPath);
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				textBox_commandline.Text = openFileDialog.FileName;
			}
		}
	}
}
