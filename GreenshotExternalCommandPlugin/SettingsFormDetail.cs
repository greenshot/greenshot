﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using GreenshotPlugin.IniFile;

namespace GreenshotExternalCommandPlugin {
	/// <summary>
	/// Description of SettingsFormDetail.
	/// </summary>
	public partial class SettingsFormDetail : ExternalCommandForm {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(SettingsFormDetail));
		private static readonly ExternalCommandConfiguration ExternalCommandConfig = IniConfig.GetIniSection<ExternalCommandConfiguration>();

		private readonly string _commando;
		private readonly int _commandIndex;

		public SettingsFormDetail(string commando) {
			InitializeComponent();
			AcceptButton = buttonOk;
			CancelButton = buttonCancel;
			_commando = commando;

			if(commando != null) {
				textBox_name.Text = commando;
				textBox_commandline.Text = ExternalCommandConfig.Commandline[commando];
				textBox_arguments.Text = ExternalCommandConfig.Argument[commando];
				_commandIndex = ExternalCommandConfig.Commands.FindIndex(s => s == commando);
			} else {
				textBox_arguments.Text = "\"{0}\"";
			}
			OkButtonState();
		}

		private void ButtonOkClick(object sender, EventArgs e) {
			string commandName = textBox_name.Text;
			string commandLine = textBox_commandline.Text;
			string arguments = textBox_arguments.Text;
			if(_commando != null) {
				ExternalCommandConfig.Commands[_commandIndex] = commandName;
				ExternalCommandConfig.Commandline.Remove(_commando);
				ExternalCommandConfig.Commandline.Add(commandName, commandLine);
				ExternalCommandConfig.Argument.Remove(_commando);
				ExternalCommandConfig.Argument.Add(commandName, arguments);
			} else {
				ExternalCommandConfig.Commands.Add(commandName);
				ExternalCommandConfig.Commandline.Add(commandName, commandLine);
				ExternalCommandConfig.Argument.Add(commandName, arguments);
			}
		}

		private void Button3Click(object sender, EventArgs e) {
			var openFileDialog = new OpenFileDialog
			{
				Filter = "Executables (*.exe, *.bat, *.com)|*.exe; *.bat; *.com|All files (*)|*",
				FilterIndex = 1,
				CheckFileExists = true,
				Multiselect = false
			};
			string initialPath = null;
			try
			{
				initialPath = Path.GetDirectoryName(textBox_commandline.Text);
			}
			catch (Exception ex)
			{
				Log.WarnFormat("Can't get the initial path via {0}", textBox_commandline.Text);
				Log.Warn("Exception: ", ex);
			}
			if(initialPath != null && Directory.Exists(initialPath)) {
				openFileDialog.InitialDirectory = initialPath;
			} else {
				initialPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				openFileDialog.InitialDirectory = initialPath;
			}
			Log.DebugFormat("Starting OpenFileDialog at {0}", initialPath);
			if(openFileDialog.ShowDialog() == DialogResult.OK) {
				textBox_commandline.Text = openFileDialog.FileName;
			}
		}

		private void OkButtonState() {
			// Assume OK
			buttonOk.Enabled = true;
			textBox_name.BackColor = Color.White;
			textBox_commandline.BackColor = Color.White;
			textBox_arguments.BackColor = Color.White;
			// Is there a text in the name field
			if(string.IsNullOrEmpty(textBox_name.Text)) {
				buttonOk.Enabled = false;
			}
			// Check if commandname is unique
			if(_commando == null && !string.IsNullOrEmpty(textBox_name.Text) && ExternalCommandConfig.Commands.Contains(textBox_name.Text)) {
				buttonOk.Enabled = false;
				textBox_name.BackColor = Color.Red;
			}
			// Is there a text in the commandline field
			if(string.IsNullOrEmpty(textBox_commandline.Text)) {
				buttonOk.Enabled = false;
			}

			if (!string.IsNullOrEmpty(textBox_commandline.Text))
			{
				// Added this to be more flexible, using the Greenshot var format
				string cmdPath = FilenameHelper.FillVariables(textBox_commandline.Text, true);
				// And also replace the "DOS" Variables
				cmdPath = FilenameHelper.FillCmdVariables(cmdPath, true);
				// Is the command available?
				if (!File.Exists(cmdPath))
				{
					buttonOk.Enabled = false;
					textBox_commandline.BackColor = Color.Red;
				}
			}
			// Are the arguments in a valid format? 
			try
			{
				string arguments = FilenameHelper.FillVariables(textBox_arguments.Text, false);
				arguments = FilenameHelper.FillCmdVariables(arguments, false);

				ExternalCommandDestination.FormatArguments(arguments, string.Empty);
			}
			catch
			{
				buttonOk.Enabled = false;
				textBox_arguments.BackColor = Color.Red;
			}  
		}

		private void textBox_name_TextChanged(object sender, EventArgs e) {
			OkButtonState();
		}

		private void textBox_commandline_TextChanged(object sender, EventArgs e) {
			OkButtonState();
		}

		private void textBox_arguments_TextChanged(object sender, EventArgs e)
		{
			OkButtonState();
		}

	}
}
