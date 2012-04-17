/*
 * Created by SharpDevelop.
 * User: 05018038
 * Date: 04.04.2012
 * Time: 10:50
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Greenshot.IniFile;

namespace ExternalCommand {
	/// <summary>
	/// Description of SettingsFormDetail.
	/// </summary>
	public partial class SettingsFormDetail : ExternalCommandForm {
		private string commando;
		private int commandIndex;
		
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SettingsFormDetail));
		private static ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		
		public SettingsFormDetail(string commando)
		{
			InitializeComponent();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
			this.commando = commando;
			
			if (commando != null) {
				textBox_name.Text = commando;
				textBox_commandline.Text = config.commandlines[commando];
				textBox_arguments.Text = config.arguments[commando];
				commandIndex = config.commands.FindIndex(delegate(string s) { return s == commando; });
			} else {
				textBox_arguments.Text = "\"{0}\"";
			}
			OKButtonState();
		}
		
		void ButtonOkClick(object sender, EventArgs e)
		{
			string commandName = textBox_name.Text;
			string commandLine = textBox_commandline.Text;
			string arguments = textBox_arguments.Text;
			if (commando != null) {
				config.commands[commandIndex] = commandName;
				config.commandlines.Remove(commando);
				config.commandlines.Add(commandName, commandLine);
				config.arguments.Remove(commando);
				config.arguments.Add(commandName, arguments);
			} else {
				config.commands.Add(commandName);
				config.commandlines.Add(commandName, commandLine);
				config.arguments.Add(commandName, arguments);
			}
			
			Close();
		}
		
		void ButtonCancelClick(object sender, EventArgs e)
		{
			Close();
		}
		
		void Button3Click(object sender, EventArgs e)
		{
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

		private void OKButtonState() {
			// Assume OK
			buttonOk.Enabled = true;
			textBox_name.BackColor = Color.White;
			textBox_commandline.BackColor = Color.White;
			// Is there a text in the name field
			if (string.IsNullOrEmpty(textBox_name.Text)) {
				buttonOk.Enabled = false;
			}
			// Check if commandname is unique
			if (commando == null && !string.IsNullOrEmpty(textBox_name.Text) && config.commands.Contains(textBox_name.Text)) {
				buttonOk.Enabled = false;
				textBox_name.BackColor = Color.Red;
			}
			// Is there a text in the commandline field
			if (string.IsNullOrEmpty(textBox_commandline.Text)) {
				buttonOk.Enabled = false;
			}
			// Is the command available?
			if (!string.IsNullOrEmpty(textBox_commandline.Text) && !File.Exists(textBox_commandline.Text)) {
				buttonOk.Enabled = false;
				textBox_commandline.BackColor = Color.Red;
			}
		}

		private void textBox_name_TextChanged(object sender, EventArgs e) {
			OKButtonState();
		}

		private void textBox_commandline_TextChanged(object sender, EventArgs e) {
			OKButtonState();
		}

	}
}
