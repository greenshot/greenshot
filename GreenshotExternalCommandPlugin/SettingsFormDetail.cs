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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Dapplo.Config.Ini;

namespace GreenshotExternalCommandPlugin
{
	/// <summary>
	/// Description of SettingsFormDetail.
	/// </summary>
	public partial class SettingsFormDetail : ExternalCommandForm
	{
		private string commando;
		private int commandIndex;

		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (SettingsFormDetail));
		private static IExternalCommandConfiguration config = IniConfig.Current.Get<IExternalCommandConfiguration>();

		public SettingsFormDetail(string commando)
		{
			InitializeComponent();
			AcceptButton = buttonOk;
			CancelButton = buttonCancel;
			this.commando = commando;

			if (commando != null)
			{
				textBox_name.Text = commando;
				textBox_commandline.Text = config.Commandline[commando];
				textBox_arguments.Text = config.Argument[commando];
				commandIndex = config.Commands.IndexOf(commando);
			}
			else
			{
				textBox_arguments.Text = "\"{0}\"";
			}
			OKButtonState();
		}

		private void ButtonOkClick(object sender, EventArgs e)
		{
			string commandName = textBox_name.Text;
			string commandLine = textBox_commandline.Text;
			string arguments = textBox_arguments.Text;
			if (commando != null)
			{
				config.Commands[commandIndex] = commandName;
				config.Commandline.Remove(commando);
				config.Commandline.Add(commandName, commandLine);
				config.Argument.Remove(commando);
				config.Argument.Add(commandName, arguments);
			}
			else
			{
				config.Commands.Add(commandName);
				config.Commandline.Add(commandName, commandLine);
				config.Argument.Add(commandName, arguments);
			}
		}

		private void Button3Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Executables (*.exe, *.bat, *.com)|*.exe; *.bat; *.com|All files (*)|*";
			openFileDialog.FilterIndex = 1;
			openFileDialog.CheckFileExists = true;
			openFileDialog.Multiselect = false;
			string initialPath = null;
			try
			{
				initialPath = Path.GetDirectoryName(textBox_commandline.Text);
			}
			catch
			{
			}
			if (initialPath != null && Directory.Exists(initialPath))
			{
				openFileDialog.InitialDirectory = initialPath;
			}
			else
			{
				initialPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				openFileDialog.InitialDirectory = initialPath;
			}
			LOG.DebugFormat("Starting OpenFileDialog at {0}", initialPath);
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				textBox_commandline.Text = openFileDialog.FileName;
			}
		}

		private void OKButtonState()
		{
			// Assume OK
			buttonOk.Enabled = true;
			textBox_name.BackColor = Color.White;
			textBox_commandline.BackColor = Color.White;
			textBox_arguments.BackColor = Color.White;
			// Is there a text in the name field
			if (string.IsNullOrEmpty(textBox_name.Text))
			{
				buttonOk.Enabled = false;
			}
			// Check if commandname is unique
			if (commando == null && !string.IsNullOrEmpty(textBox_name.Text) && config.Commands.Contains(textBox_name.Text))
			{
				buttonOk.Enabled = false;
				textBox_name.BackColor = Color.Red;
			}
			// Is there a text in the commandline field
			if (string.IsNullOrEmpty(textBox_commandline.Text))
			{
				buttonOk.Enabled = false;
			}
			// Is the command available?
			if (!string.IsNullOrEmpty(textBox_commandline.Text) && !File.Exists(textBox_commandline.Text))
			{
				buttonOk.Enabled = false;
				textBox_commandline.BackColor = Color.Red;
			}
			// Are the arguments in a valid format? 
			try
			{
				ExternalCommandLegacyDestination.FormatArguments(textBox_arguments.Text, string.Empty);
			}
			catch
			{
				buttonOk.Enabled = false;
				textBox_arguments.BackColor = Color.Red;
			}
		}

		private void textBox_name_TextChanged(object sender, EventArgs e)
		{
			OKButtonState();
		}

		private void textBox_commandline_TextChanged(object sender, EventArgs e)
		{
			OKButtonState();
		}

		private void textBox_arguments_TextChanged(object sender, EventArgs e)
		{
			OKButtonState();
		}
	}
}