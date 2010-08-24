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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Configuration;
using Greenshot.Core;
using Greenshot.Drawing;
using Greenshot.Forms;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.UnmanagedHelpers;

namespace Greenshot {
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form {
		private const string LOG4NET_FILE = "log4net.xml";
		private static log4net.ILog LOG = null;
		private static Mutex applicationMutex = null;
		private static CoreConfiguration conf;
		
		private static void InitializeLog4NET() {
			// Setup log4j, currently the file is called log4net.xml
			string log4netFilename = Path.Combine(Application.StartupPath, LOG4NET_FILE);
			if (File.Exists(log4netFilename)) {
				log4net.Config.XmlConfigurator.Configure(new FileInfo(log4netFilename)); 
			} else {
				MessageBox.Show("Can't find file " + LOG4NET_FILE);
			}
			
			// Setup the LOG
			LOG = log4net.LogManager.GetLogger(typeof(MainForm));
		}

		[STAThread]
		public static void Main(string[] args) {
			bool isAlreadyRunning = false;
			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			
			List<string> filesToOpen = new List<string>();

			// Init Log4NET
			InitializeLog4NET();
			
			// Log the startup
			LOG.Info("Starting: " + EnvironmentInfo.EnvironmentToString(false));

			// Set the Thread name, is better than "1"
			Thread.CurrentThread.Name = Application.ProductName;

			// Read configuration
			conf = IniConfig.GetIniSection<CoreConfiguration>();
			if (conf.IsDirty) {
				IniConfig.Save();
			}
			LOG.Info("Firstlaunch: " + conf.IsFirstLaunch);
			LOG.Info("Destinations:");
			if (conf.OutputDestinations != null) {
				foreach(Destination destination in conf.OutputDestinations) {
					LOG.Info("\t" + destination);
				}
			}

			try {
				// Fix for Bug 2495900, Multi-user Environment
				// check whether there's an local instance running already
				
				try {
					// 1) Create Mutex
					applicationMutex = new Mutex(false, @"Local\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08");
					// 2) Get the right to it, this returns false if it's already locked
					if (!applicationMutex.WaitOne(0, false)) {
						isAlreadyRunning = true;
					}
				} catch (AbandonedMutexException e) {
					// Another Greenshot instance didn't cleanup correctly!
					// we can ignore the exception, it happend on the "waitone" but still the mutex belongs to us
					LOG.Warn("Greenshot didn't cleanup correctly!", e);
				} catch (UnauthorizedAccessException e) {
					LOG.Warn("Greenshot is most likely already running for a different user in the same session, can't create mutex due to error: ", e);
					isAlreadyRunning = true;
				} catch (Exception e) {
					LOG.Warn("Problem obtaining the Mutex, assuming it was already taken!", e);
					isAlreadyRunning = true;
				}

				if (args.Length > 0 && LOG.IsDebugEnabled) {
					StringBuilder argumentString = new StringBuilder();
					for(int argumentNr = 0; argumentNr < args.Length; argumentNr++) {
						argumentString.Append("[").Append(args[argumentNr]).Append("] ");
					}
					LOG.Debug("Greenshot arguments: " + argumentString.ToString());
				}

				for(int argumentNr = 0; argumentNr < args.Length; argumentNr++) {
					string argument = args[argumentNr];
					
					// Help
					if (argument.ToLower().Equals("/help")) {
						// Try to attach to the console
						bool attachedToConsole = User32.AttachConsole(User32.ATTACH_PARENT_PROCESS);
						// If attach didn't work, open a console
						if (!attachedToConsole) {
							User32.AllocConsole();
						}
						StringBuilder helpOutput = new StringBuilder();
						helpOutput.AppendLine();
						helpOutput.AppendLine("Greenshot commandline options:");
						helpOutput.AppendLine();
						helpOutput.AppendLine();
						helpOutput.AppendLine("\t/help");
						helpOutput.AppendLine("\t\tThis help.");
						helpOutput.AppendLine();
						helpOutput.AppendLine();
						helpOutput.AppendLine("\t/help configure");
						helpOutput.AppendLine("\t\tA detailed listing of available settings for the configure command.");
						helpOutput.AppendLine();
						helpOutput.AppendLine();
						helpOutput.AppendLine("\t/exit");
						helpOutput.AppendLine("\t\tTries to close all running instances.");
						helpOutput.AppendLine();
						helpOutput.AppendLine();
						helpOutput.AppendLine("\t/configure [property=value] [property=value] ...");
						helpOutput.AppendLine("\t\tChange the configuration of Greenshot via the commandline.");
						helpOutput.AppendLine("\t\tExample to change the language to English: greenshot.exe /configure Ui_Language=en-US");
						helpOutput.AppendLine("\t\tExample to change the destination: greenshot.exe /configure Output_File_Path=\"C:\\Documents and Settings\\\"");
						helpOutput.AppendLine();
						helpOutput.AppendLine();
						helpOutput.AppendLine("\t/openfile [filename]");
						helpOutput.AppendLine("\t\tOpen the bitmap file in the running Greenshot instance or start a new instance");
						helpOutput.AppendLine();
						helpOutput.AppendLine();
						helpOutput.AppendLine("\t/norun");
						helpOutput.AppendLine("\t\tCan be used if someone only wants to change the configuration.");
						helpOutput.AppendLine("\t\tAs soon as this option is found Greenshot exits if not and there is no running instance it will stay running.");
						helpOutput.AppendLine("\t\tExample: greenshot.exe /configure Output_File_Path=\"C:\\Documents and Settings\\\" --exit");
						Console.WriteLine(helpOutput.ToString());

						// If attach didn't work, wait for key otherwise the console will close to quickly
						if (!attachedToConsole) {
							Console.ReadKey();
						}
						FreeMutex();
						return;
					}

					// exit application
					if (argument.ToLower().Equals("/exit")) {
						try {
							LOG.Info("Sending all instances the exit command.");
							// Pass Exit to running instance, if any
							SendData(new CopyDataTransport(CommandEnum.Exit));
						} catch (Exception e) {
							LOG.Warn("Exception by exit.", e);
						}
						FreeMutex();
						return;
					}

					// Modify configuration
					if (argument.ToLower().Equals("/configure")) {
						LOG.Debug("Setting configuration!");
						Properties properties = new Properties();
						int propertyNr = argumentNr + 1;
						while(propertyNr < args.Length && args[propertyNr].Contains("=")) {
							// "Remove" the argument from the list as we used it
							argumentNr++;
							string [] splitargument = args[propertyNr].Split(new Char[] {'='});
							LOG.Debug("Found property: " + splitargument[0] +"="+ splitargument[1]);
							properties.AddProperty(splitargument[0], splitargument[1]);
							propertyNr++;
						}
						if (properties.Count > 0) {
							// TODO: Check properties!
							//conf.SetProperties(properties);
							//conf.Store();
							// Update running instances
							SendData(new CopyDataTransport(CommandEnum.ReloadConfig));
							LOG.Debug("Configuration modified!");
						} else {
							LOG.Debug("Configuration NOT modified!");
						}
					}

					// Make an exit possible
					if (argument.ToLower().Equals("/norun")) {
						FreeMutex();
						return;
					}

					if (argument.ToLower().Equals("/openfile")) {
						string filename = args[++argumentNr];
						filesToOpen.Add(filename);
					}
				}

				// Finished parsing the command line arguments, see if we need to do anything
				CopyDataTransport transport = new CopyDataTransport();
				if (filesToOpen.Count > 0) {
					foreach(string fileToOpen in filesToOpen) {
						transport.AddCommand(CommandEnum.OpenFile, fileToOpen);
					}
				}
				if (isAlreadyRunning) {
					if (filesToOpen.Count > 0) {
						SendData(transport);
					} else {
						ILanguage lang = Language.GetInstance();
						MessageBox.Show(lang.GetString(LangKey.error_multipleinstances), lang.GetString(LangKey.error));
					}
					FreeMutex();
					Application.Exit();
					return;
				}

				// From here on we continue starting Greenshot
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				// if language is not set, show language dialog
				if(conf.Language.Equals("")) {
					LanguageDialog ld = LanguageDialog.GetInstance();
					ld.ShowDialog();
					conf.Language = ld.Language;
					IniConfig.Save();
				}

				// Check if it's the first time launch?
	   			if(conf.IsFirstLaunch) {
					conf.IsFirstLaunch = false;
					IniConfig.Save();
					transport.AddCommand(CommandEnum.FirstLaunch);
				}
				MainForm mainForm = new MainForm(transport);
				Application.Run(mainForm);
			} catch(Exception ex) {
				LOG.Error("Exception in startup.", ex);
				Application_ThreadException(MainForm.ActiveForm, new ThreadExceptionEventArgs(ex));
			}
		}

		/// <summary>
		/// Send DataTransport Object via Window-messages
		/// </summary>
		/// <param name="dataTransport">DataTransport with data for a running instance</param>
		private static void SendData(CopyDataTransport copyDataTransport) {
			string appName = Application.ProductName;
			CopyData copyData = new CopyData();
			copyData.Channels.Add(appName);
			copyData.Channels[appName].Send(copyDataTransport);
		}

		private static void FreeMutex() {
			// Remove the application mutex
			if (applicationMutex != null) {
				try {
					applicationMutex.ReleaseMutex();
					applicationMutex = null;
				} catch (Exception ex) {
					LOG.Error("Error releasing Mutex!", ex);
				}
			}
		}

		public static MainForm instance = null;

		private ILanguage lang;
		private ToolTip tooltip;
		private CaptureForm captureForm = null;
		private string lastImagePath = null;
		private CopyData copyData = null;
				
		public MainForm(CopyDataTransport dataTransport) {
			instance = this;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			lang = Language.GetInstance();
			if(conf.RegisterHotkeys) {
				RegisterHotkeys();
			}
			
			tooltip = new ToolTip();
			
			UpdateUI();
			InitializeQuickSettingsMenu();

			captureForm = new CaptureForm();

			// Load all the plugins
			PluginHelper.instance.LoadPlugins(this.contextMenu, captureForm);
			PluginHelper.instance.OnImageOutput += new OnImageOutputHandler(ImageWritten);
			SoundHelper.Initialize();

			// Enable the Greenshot icon to be visible, this prevents Problems with the context menu
			notifyIcon.Visible = true;

			// Create a new instance of the class: copyData = new CopyData();
			copyData = new CopyData();

			// Assign the handle:
			copyData.AssignHandle(this.Handle);
			// Create the channel to send on:
			copyData.Channels.Add("Greenshot");     
			// Hook up received event:
			copyData.CopyDataReceived += new CopyDataReceivedEventHandler(CopyDataDataReceived);

			if (dataTransport != null) {
				HandleDataTransport(dataTransport);
			}
		}

		/// <summary>
		/// DataReceivedEventHandler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="dataReceivedEventArgs"></param>
		private void CopyDataDataReceived(object sender, CopyDataReceivedEventArgs copyDataReceivedEventArgs) {
			// Cast the data to the type of object we sent:
			CopyDataTransport dataTransport = (CopyDataTransport)copyDataReceivedEventArgs.Data;
			HandleDataTransport(dataTransport);
		}

		private void HandleDataTransport(CopyDataTransport dataTransport) {
			foreach(KeyValuePair<CommandEnum, string> command in dataTransport.Commands) {
				LOG.Debug("Data received, Command = " + command.Key + ", Data: " + command.Value);
				switch(command.Key) {
					case CommandEnum.Exit:
						exit();
						break;
					case CommandEnum.ReloadConfig:
						// TODO: Reload the configuration
						// Even update language when needed
						UpdateUI();
						break;
					case CommandEnum.OpenFile:
						string filename = command.Value;
						if (File.Exists(filename)) {
							captureForm.MakeCapture(filename);	
						} else {
							LOG.Warn("No such file: " + filename);
						}
						break;
					default:
						LOG.Error("Unknown command!");
						break;
				}
			}
		}

		public ContextMenuStrip MainMenu {
			get {return contextMenu;}
		}

		#region hotkeys
		protected override void WndProc(ref Message m) {
			HotkeyHelper.HandleMessages(ref m);
			base.WndProc(ref m);
		}
		
		private void RegisterHotkeys() {
			bool suc = true;
			suc &= HotkeyHelper.RegisterHotKey(this.Handle, (uint)HotkeyHelper.Modifiers.NONE, HotkeyHelper.VK_SNAPSHOT, new HotKeyHandler(CaptureRegion));
			suc &= HotkeyHelper.RegisterHotKey(this.Handle, (uint)HotkeyHelper.Modifiers.ALT, HotkeyHelper.VK_SNAPSHOT, new HotKeyHandler(CaptureWindow));
			suc &= HotkeyHelper.RegisterHotKey(this.Handle, (uint)HotkeyHelper.Modifiers.CTRL, HotkeyHelper.VK_SNAPSHOT, new HotKeyHandler(CaptureFullScreen));
			suc &= HotkeyHelper.RegisterHotKey(this.Handle, (uint)HotkeyHelper.Modifiers.SHIFT, HotkeyHelper.VK_SNAPSHOT, new HotKeyHandler(CaptureLastRegion));
			if (!suc) {
				MessageBox.Show(lang.GetString(LangKey.warning_hotkeys),lang.GetString(LangKey.warning));
			}
		}
		#endregion
		
		public void UpdateUI() {
			this.Text = lang.GetString(LangKey.application_title);
			this.contextmenu_settings.Text = lang.GetString(LangKey.contextmenu_settings);
			this.contextmenu_capturearea.Text = lang.GetString(LangKey.contextmenu_capturearea);
			this.contextmenu_capturelastregion.Text = lang.GetString(LangKey.contextmenu_capturelastregion);
			this.contextmenu_capturewindow.Text = lang.GetString(LangKey.contextmenu_capturewindow);
			this.contextmenu_capturefullscreen.Text = lang.GetString(LangKey.contextmenu_capturefullscreen);
			this.contextmenu_captureclipboard.Text = lang.GetString(LangKey.contextmenu_captureclipboard);
			this.contextmenu_openfile.Text = lang.GetString(LangKey.contextmenu_openfile);
			this.contextmenu_quicksettings.Text = lang.GetString(LangKey.contextmenu_quicksettings);
			this.contextmenu_help.Text = lang.GetString(LangKey.contextmenu_help);
			this.contextmenu_about.Text = lang.GetString(LangKey.contextmenu_about);
			this.contextmenu_donate.Text = lang.GetString(LangKey.contextmenu_donate);
			this.contextmenu_exit.Text = lang.GetString(LangKey.contextmenu_exit);
		}
		
		
		#region mainform events
		void MainFormFormClosing(object sender, FormClosingEventArgs e) {
			instance = null;
			exit();
		}

		void MainFormActivated(object sender, EventArgs e) {
			Hide();
			ShowInTaskbar = false;
		}
		#endregion

		#region key handlers
		void CaptureRegion() {
			captureForm.MakeCapture(CaptureMode.Region, true);
		}
		void CaptureClipboard() {
			captureForm.MakeCapture(CaptureMode.Clipboard, false);
		}
		void CaptureFile() {
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image files (*.png, *.jpg, *.gif, *.bmp, *.ico)|*.png; *.jpg; *.jpeg; *.gif; *.bmp; *.ico";
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				if (File.Exists(openFileDialog.FileName)) {
					captureForm.MakeCapture(openFileDialog.FileName);				
				}
			}
		}
		void CaptureFullScreen() {
			captureForm.MakeCapture(CaptureMode.FullScreen, true);
		}
		void CaptureLastRegion() {
			captureForm.MakeCapture(CaptureMode.LastRegion, true);
		}
		void CaptureWindow() {
			CaptureMode captureMode = CaptureMode.None;
			if (conf.CaptureWindowsInteractive) {
				captureMode = CaptureMode.Window;
			} else {
				captureMode = CaptureMode.ActiveWindow;
			}
			captureForm.MakeCapture(captureMode, true);
		}
		#endregion
		
		#region contextmenu
		void ContextMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)	{
			string text = Clipboard.GetText();
			if ((text != null && text.StartsWith("http://")) || Clipboard.ContainsImage() || ClipboardHelper.GetFormats().Contains("HTML Format")) {
				contextmenu_captureclipboard.Enabled = true;
			} else {
				contextmenu_captureclipboard.Enabled = false;
			}
			contextmenu_capturelastregion.Enabled = RuntimeConfig.LastCapturedRegion != Rectangle.Empty;
		}

		void CaptureAreaToolStripMenuItemClick(object sender, EventArgs e) {
			captureForm.MakeCapture(CaptureMode.Region, false);
		}

		void CaptureClipboardToolStripMenuItemClick(object sender, System.EventArgs e) {
			CaptureClipboard();
		}
		
		void OpenFileToolStripMenuItemClick(object sender, System.EventArgs e) {
			CaptureFile();
		}

		void CaptureFullScreenToolStripMenuItemClick(object sender, System.EventArgs e) {
			captureForm.MakeCapture(CaptureMode.FullScreen, false);
		}
		
		void Contextmenu_capturelastregionClick(object sender, System.EventArgs e) {
			captureForm.MakeCapture(CaptureMode.LastRegion, false);
		}

		void Contextmenu_donateClick(object sender, System.EventArgs e) {
			Process.Start("http://getgreenshot.org/support/");
		}
		
		void CaptureWindowToolStripMenuItemClick(object sender, System.EventArgs e) {
			captureForm.MakeCapture(CaptureMode.Window, false);
		}
		
		void Contextmenu_settingsClick(object sender, System.EventArgs e) {
			SettingsForm settings = new SettingsForm();
			settings.ShowDialog();
			InitializeQuickSettingsMenu();
			this.Hide();
		}
		
		void Contextmenu_aboutClick(object sender, EventArgs e) {
			new AboutForm().Show();
		}
		
		void Contextmenu_helpClick(object sender, System.EventArgs e) {
			HelpBrowserForm hpf = new HelpBrowserForm(conf.Language);
			hpf.Show();
		}
		
		void Contextmenu_exitClick(object sender, EventArgs e) {
			Application.Exit();
		}
		
		private void InitializeQuickSettingsMenu() {
			this.contextmenu_quicksettings.DropDownItems.Clear();
			// screenshot destination
			ToolStripMenuSelectList sel = new ToolStripMenuSelectList("destination",true);
			sel.Text = lang.GetString(LangKey.settings_destination);
			sel.AddItem(lang.GetString(LangKey.settings_destination_editor), Destination.Editor, conf.OutputDestinations.Contains(Destination.Editor));
			sel.AddItem(lang.GetString(LangKey.settings_destination_clipboard), Destination.Clipboard, conf.OutputDestinations.Contains(Destination.Clipboard));
			sel.AddItem(lang.GetString(LangKey.quicksettings_destination_file), Destination.FileDefault, conf.OutputDestinations.Contains(Destination.FileDefault));
			sel.AddItem(lang.GetString(LangKey.settings_destination_fileas), Destination.FileWithDialog, conf.OutputDestinations.Contains(Destination.FileWithDialog));
			sel.AddItem(lang.GetString(LangKey.settings_destination_printer), Destination.Printer, conf.OutputDestinations.Contains(Destination.Printer));
			sel.AddItem(lang.GetString(LangKey.settings_destination_email), Destination.EMail, conf.OutputDestinations.Contains(Destination.EMail));
			sel.CheckedChanged += new EventHandler(this.QuickSettingItemChanged);
			this.contextmenu_quicksettings.DropDownItems.Add(sel);
			// print options
			sel = new ToolStripMenuSelectList("printoptions",true);
			sel.Text = lang.GetString(LangKey.settings_printoptions);
			sel.AddItem(lang.GetString(LangKey.printoptions_allowshrink), "AllowPrintShrink", conf.OutputPrintAllowShrink);
			sel.AddItem(lang.GetString(LangKey.printoptions_allowenlarge), "AllowPrintEnlarge", conf.OutputPrintAllowEnlarge);
			sel.AddItem(lang.GetString(LangKey.printoptions_allowrotate), "AllowPrintRotate", conf.OutputPrintAllowRotate);
			sel.AddItem(lang.GetString(LangKey.printoptions_allowcenter), "AllowPrintCenter", conf.OutputPrintCenter);
			sel.CheckedChanged += new EventHandler(this.QuickSettingItemChanged);
			this.contextmenu_quicksettings.DropDownItems.Add(sel);
			// effects
			sel = new ToolStripMenuSelectList("effects",true);
			sel.Text = lang.GetString(LangKey.settings_visualization);
			sel.AddItem(lang.GetString(LangKey.settings_playsound), "PlaySound", conf.PlayCameraSound);
			sel.AddItem(lang.GetString(LangKey.settings_showflashlight), "ShowFlashlight", conf.ShowFlash);
			sel.CheckedChanged += new EventHandler(this.QuickSettingItemChanged);
			this.contextmenu_quicksettings.DropDownItems.Add(sel);
		}
		
		void QuickSettingItemChanged(object sender, EventArgs e) {
			ToolStripMenuSelectList selectList = (ToolStripMenuSelectList)sender;
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs)e).Item;;
			if(selectList.Identifier.Equals("destination")) {
				IEnumerator en = selectList.DropDownItems.GetEnumerator();
				List<Destination> destinations = new List<Destination>();;
				while(en.MoveNext()) {
					ToolStripMenuSelectListItem i = (ToolStripMenuSelectListItem)en.Current;
					destinations.Add((Destination)i.Data);
				}
				conf.OutputDestinations = destinations;
				IniConfig.Save();
			} else if(selectList.Identifier.Equals("printoptions")) {
				if(item.Data.Equals("AllowPrintShrink")) conf.OutputPrintAllowShrink = item.Checked;
				else if(item.Data.Equals("AllowPrintEnlarge")) conf.OutputPrintAllowEnlarge = item.Checked;
				else if(item.Data.Equals("AllowPrintRotate")) conf.OutputPrintAllowRotate = item.Checked;
				else if(item.Data.Equals("AllowPrintCenter")) conf.OutputPrintCenter = item.Checked;
				IniConfig.Save();
			} else if(selectList.Identifier.Equals("effects")) {
				if(item.Data.Equals("PlaySound")) conf.PlayCameraSound = item.Checked;
				else if(item.Data.Equals("ShowFlashlight")) conf.ShowFlash = item.Checked;
				IniConfig.Save();

			}
		}
		#endregion
		
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			string exceptionText = EnvironmentInfo.BuildReport(e.ExceptionObject as Exception);
			LOG.Error(exceptionText);
			new BugReportForm(exceptionText).ShowDialog();
		}
		
		private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
			string exceptionText = EnvironmentInfo.BuildReport(e.Exception);
			LOG.Error(exceptionText);
			new BugReportForm(exceptionText).ShowDialog();
		}
		
		/// <summary>
		/// The ContextMenu DoubleClick currently opens the last know save location
		/// </summary>
		private void ContextMenuDoubleClick(object sender, EventArgs eventArgs) {
			string path;
			if (lastImagePath != null && Directory.Exists(lastImagePath)) {
				path = lastImagePath;
			} else if (Directory.Exists(conf.Output_File_Path)) {
				path = conf.Output_File_Path;
			} else {
				// What do I open when nothing can be found? Right, nothing...
				return;
			}
			LOG.Debug("DoubleClick was called! Starting: " + path);
			try {
				System.Diagnostics.Process.Start(path);
			} catch (Exception e) {
				// Make sure we show what we tried to open in the exception
				e.Data.Add("path", path);
				throw e;
			}
		}
		
		/// <summary>
		/// Handling of the OnImageOutputHandler event, stores the save location
		/// </summary>
		/// <param name="ImageOutputEventArgs">Has the full path</param>
		private void ImageWritten(object sender, ImageOutputEventArgs eventArgs) {
			lastImagePath = Path.GetDirectoryName(eventArgs.FullPath);
		}
		
		/// <summary>
		/// Shutdown / cleanup
		/// </summary>
		public void exit() {
			LOG.Info("Exit: " + EnvironmentInfo.EnvironmentToString(false));
			try {
				// Make sure hotkeys are disabled
				HotkeyHelper.UnregisterHotkeys(Handle);
			} catch (Exception e) {
				LOG.Error("Error unregistering hotkeys!", e);
			}
	
			try {
				// Now the sound isn't needed anymore
				SoundHelper.Deinitialize();
			} catch (Exception e) {
				LOG.Error("Error deinitializing sound!", e);
			}
	
			try {
				// Inform all registed plugins
				PluginHelper.instance.Shutdown();
			} catch (Exception e) {
				LOG.Error("Error shutting down plugins!", e);
			}
			try {
				// Making sure all Windows are closed, gracefull shutdown
				Application.Exit();
			} catch (Exception e) {
				LOG.Error("Error closing application!", e);
			}
				
			try {
				// Store any open configuration changes
				IniConfig.Save();
			} catch (Exception e) {
				LOG.Error("Error storing configuration!", e);
			}
			// Remove the application mutex
			FreeMutex();

			// make the icon invisible otherwise it stays even after exit!!
			if (notifyIcon != null) {
				notifyIcon.Visible = false;
				notifyIcon.Dispose();
				notifyIcon = null;
			}
		}
	}
}