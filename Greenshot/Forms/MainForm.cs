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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Drawing;
using Greenshot.Experimental;
using Greenshot.Forms;
using Greenshot.Help;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.UnmanagedHelpers;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

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
			List<string> filesToOpen = new List<string>();

			// Set the Thread name, is better than "1"
			Thread.CurrentThread.Name = Application.ProductName;
			
			// Init Log4NET
			InitializeLog4NET();

			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			// Log the startup
			LOG.Info("Starting: " + EnvironmentInfo.EnvironmentToString(false));

			AppConfig.UpgradeToIni();
			// Read configuration
			conf = IniConfig.GetIniSection<CoreConfiguration>();
			try {
				// Fix for Bug 2495900, Multi-user Environment
				// check whether there's an local instance running already
				
				try {
					// Added Mutex Security, hopefully this prevents the UnauthorizedAccessException more gracefully
					// See an example in Bug #3131534
					SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
					MutexSecurity mutexsecurity = new MutexSecurity();
					mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
					mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.ChangePermissions, AccessControlType.Deny));
					mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.Delete, AccessControlType.Deny));

        			bool created = false;
					// 1) Create Mutex
					applicationMutex = new Mutex(false, @"Local\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08", out created, mutexsecurity);
					// 2) Get the right to it, this returns false if it's already locked
					if (!applicationMutex.WaitOne(0, false)) {
						LOG.Debug("Greenshot seems already to be running!");
						isAlreadyRunning = true;
						// Clean up
						applicationMutex.Close();
						applicationMutex = null;
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
						bool attachedToConsole = Kernel32.AttachConsole(Kernel32.ATTACHCONSOLE_ATTACHPARENTPROCESS);
						// If attach didn't work, open a console
						if (!attachedToConsole) {
							Kernel32.AllocConsole();
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
						helpOutput.AppendLine("\t/exit");
						helpOutput.AppendLine("\t\tTries to close all running instances.");
						helpOutput.AppendLine();
						helpOutput.AppendLine();
						helpOutput.AppendLine("\t/reload");
						helpOutput.AppendLine("\t\tReload the configuration of Greenshot.");
						helpOutput.AppendLine();
						helpOutput.AppendLine();
						helpOutput.AppendLine("\t/language [language code]");
						helpOutput.AppendLine("\t\tSet the language of Greenshot, e.g. greenshot /language en-EN.");
						helpOutput.AppendLine();
						helpOutput.AppendLine();
						helpOutput.AppendLine("\t[filename]");
						helpOutput.AppendLine("\t\tOpen the bitmap files in the running Greenshot instance or start a new instance");
						Console.WriteLine(helpOutput.ToString());

						// If attach didn't work, wait for key otherwise the console will close to quickly
						if (!attachedToConsole) {
							Console.ReadKey();
						}
						FreeMutex();
						return;
					}
					
					if (argument.ToLower().Equals("/exit")) {
						// unregister application on uninstall (allow uninstall)
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
					
					// Reload the configuration
					if (argument.ToLower().Equals("/reload")) {
						// Modify configuration
						LOG.Info("Reloading configuration!");
						// Update running instances
						SendData(new CopyDataTransport(CommandEnum.ReloadConfig));
						FreeMutex();
						return;
					}
					
					// Stop running
					if (argument.ToLower().Equals("/norun")) {
						// Make an exit possible
						FreeMutex();
						return;
					}
					
					// Language
					if (argument.ToLower().Equals("/language")) {
						conf.Language = args[++argumentNr];
						IniConfig.Save();
						continue;
					}
					
					// Files to open
					filesToOpen.Add(argument);
				}

				// Finished parsing the command line arguments, see if we need to do anything
				CopyDataTransport transport = new CopyDataTransport();
				if (filesToOpen.Count > 0) {
					foreach(string fileToOpen in filesToOpen) {
						transport.AddCommand(CommandEnum.OpenFile, fileToOpen);
					}
				}

				ILanguage lang = Language.GetInstance();
				if (isAlreadyRunning) {
					if (filesToOpen.Count > 0) {
						SendData(transport);
					} else {
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
				if(conf.Language == null || conf.Language.Trim().Length == 0) {
					LanguageDialog ld = LanguageDialog.GetInstance();
					ld.ShowDialog();
					conf.Language = ld.SelectedLanguage;
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
		private static void SendData(CopyDataTransport dataTransport) {
			string appName = Application.ProductName;
			CopyData copyData = new CopyData();
			copyData.Channels.Add(appName);
			copyData.Channels[appName].Send(dataTransport);
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
		private CopyData copyData = null;
				
		public MainForm(CopyDataTransport dataTransport) {
			instance = this;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			lang = Language.GetInstance();
			IniConfig.IniChanged += new FileSystemEventHandler(ReloadConfiguration);
			
			// Make sure all hotkeys pass this window!
			HotkeyControl.RegisterHotkeyHWND(this.Handle);
			RegisterHotkeys();
			
			tooltip = new ToolTip();
			
			UpdateUI();
			InitializeQuickSettingsMenu();

			captureForm = new CaptureForm();

			// Load all the plugins
			PluginHelper.instance.LoadPlugins(this, captureForm);
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
			ClipboardHelper.RegisterClipboardViewer(this.Handle);
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
					case CommandEnum.FirstLaunch:
						LOG.Info("FirstLaunch: Created new configuration.");
						break;
					case CommandEnum.ReloadConfig:
						IniConfig.Reload();
						ReloadConfiguration(null, null);
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
		
		private void ReloadConfiguration(object source, FileSystemEventArgs e) {
			lang.SetLanguage(conf.Language);
			this.Invoke((MethodInvoker) delegate {
				// Even update language when needed
				UpdateUI();
				// Update the hotkey
				// Make sure the current hotkeys are disabled
				HotkeyControl.UnregisterHotkeys();
				RegisterHotkeys();
			});
		}

		public ContextMenuStrip MainMenu {
			get {return contextMenu;}
		}

		#region hotkeys
		protected override void WndProc(ref Message m) {
			if (ClipboardHelper.HandleClipboardMessages(ref m)) {
				return;
			}
			if (HotkeyControl.HandleMessages(ref m)) {
				return;
			}
			base.WndProc(ref m);
		}
		
		public static void RegisterHotkeys() {
			if (instance == null) {
				return;
			}
			bool success = true;
			StringBuilder failedKeys = new StringBuilder();

			// Capture region
			if (HotkeyControl.RegisterHotKey(conf.RegionHotkey, new HotKeyHandler(instance.CaptureRegion)) < 0) {
				LOG.DebugFormat("Failed to register CaptureRegion to hotkey: {0}", conf.RegionHotkey);
				success = false;
				if(failedKeys.Length > 0) {
					failedKeys.Append(", ");
				}
				failedKeys.Append(conf.RegionHotkey);
			} else {
				LOG.DebugFormat("Registered CaptureRegion to hotkey: {0}", conf.RegionHotkey);
			}

			// Capture window
			if (HotkeyControl.RegisterHotKey(conf.WindowHotkey, new HotKeyHandler(instance.CaptureWindow)) < 0) {
				LOG.DebugFormat("Failed to register CaptureWindow to hotkey: {0}", conf.WindowHotkey);
				success = false;
				if(failedKeys.Length > 0) {
					failedKeys.Append(", ");
				}
				failedKeys.Append(conf.WindowHotkey);
			} else {
				LOG.DebugFormat("Registered CaptureWindow to hotkey: {0}", conf.WindowHotkey);
			}

			// Capture fullScreen
			if (HotkeyControl.RegisterHotKey(conf.FullscreenHotkey, new HotKeyHandler(instance.CaptureFullScreen)) < 0) {
				LOG.DebugFormat("Failed to register CaptureFullScreen to hotkey: {0}", conf.FullscreenHotkey);
				success = false;
				if(failedKeys.Length > 0) {
					failedKeys.Append(", ");
				}
				failedKeys.Append(conf.FullscreenHotkey);
			} else {
				LOG.DebugFormat("Registered CaptureFullScreen to hotkey: {0}", conf.FullscreenHotkey);
			}

			// Capture last region
			if (HotkeyControl.RegisterHotKey(conf.LastregionHotkey, new HotKeyHandler(instance.CaptureLastRegion)) < 0) {
				LOG.DebugFormat("Failed to register CaptureLastRegion to hotkey: {0}", conf.LastregionHotkey);
				success = false;
				if(failedKeys.Length > 0) {
					failedKeys.Append(", ");
				}
				failedKeys.Append(conf.LastregionHotkey);
			} else {
				LOG.DebugFormat("Registered CaptureLastRegion to hotkey: {0}", conf.LastregionHotkey);
			}

			// Capture IE
			if (HotkeyControl.RegisterHotKey(conf.IEHotkey, new HotKeyHandler(instance.CaptureIE)) < 0) {
				LOG.DebugFormat("Failed to register CaptureIE to hotkey: {0}", conf.IEHotkey);
				success = false;
				if(failedKeys.Length > 0) {
					failedKeys.Append(", ");
				}
				failedKeys.Append(conf.IEHotkey);
			} else {
				LOG.DebugFormat("Registered CaptureIE to hotkey: {0}", conf.IEHotkey);
			}

			if (!success) {
				ILanguage lang = Language.GetInstance();
				MessageBox.Show(lang.GetFormattedString(LangKey.warning_hotkeys, failedKeys.ToString()),lang.GetString(LangKey.warning));
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
			this.contextmenu_captureie.Text = lang.GetString(LangKey.contextmenu_captureie);
			this.contextmenu_openrecentcapture.Text = lang.GetString(LangKey.contextmenu_openrecentcapture);
			
			// Show hotkeys in Contextmenu
			this.contextmenu_capturearea.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.RegionHotkey);
			this.contextmenu_capturelastregion.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.LastregionHotkey);
			this.contextmenu_capturewindow.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.WindowHotkey);
			this.contextmenu_capturefullscreen.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.FullscreenHotkey);
			this.contextmenu_captureie.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.IEHotkey);
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
		void CaptureIE() {
			captureForm.MakeCapture(CaptureMode.IE, true);
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
			if (Clipboard.ContainsImage()) {
				contextmenu_captureclipboard.Enabled = true;
			} else {
				contextmenu_captureclipboard.Enabled = false;
			}
			contextmenu_capturelastregion.Enabled = RuntimeConfig.LastCapturedRegion != Rectangle.Empty;

			// IE context menu code
			if (conf.IECapture && IECaptureHelper.IsIERunning()) {
				this.contextmenu_captureie.Enabled = true;
			} else {
				this.contextmenu_captureie.Enabled = false;
			}
		}
		
		void ContextMenuClosing(object sender, EventArgs e) {
			this.contextmenu_captureie.DropDownItems.Clear();
			this.contextmenu_capturewindow.DropDownItems.Clear();
		}
		
		/// <summary>
		/// Build a selectable list of IE tabs when we enter the menu item
		/// </summary>
		void EnterCaptureIEMenuItem(object sender, EventArgs e) {
			List<KeyValuePair<WindowDetails, string>> tabs = IECaptureHelper.GetTabList();
			this.contextmenu_captureie.DropDownItems.Clear();
			if (tabs.Count > 0) {
				this.contextmenu_captureie.Enabled = true;
				Dictionary<WindowDetails, int> counter = new Dictionary<WindowDetails, int>();
				
				foreach(KeyValuePair<WindowDetails, string> tabData in tabs) {
					ToolStripMenuItem captureIETabItem = new ToolStripMenuItem(tabData.Value);
					int index;
					if (counter.ContainsKey(tabData.Key)) {
						index = counter[tabData.Key];
					} else {
						index = 0;
					}
					captureIETabItem.Tag = new KeyValuePair<WindowDetails, int>(tabData.Key, index++);
	            	captureIETabItem.Click += new System.EventHandler(Contextmenu_captureIE_Click);
	            	this.contextmenu_captureie.DropDownItems.Add(captureIETabItem);
	            	if (counter.ContainsKey(tabData.Key)) {
	            		counter[tabData.Key] = index;
	            	} else {
	            		counter.Add(tabData.Key, index);
	            	}
				}
			} else {
				this.contextmenu_captureie.Enabled = false;
			}
		}
		
		/// <summary>
		/// Build a selectable list of windows when we enter the menu item
		/// </summary>
		void EnterCaptureWindowMenuItem(object sender, EventArgs e) {
			// The Capture window context menu item used to go to the following code:
			// captureForm.MakeCapture(CaptureMode.Window, false);
			// Now we check which windows are there to capture
			ToolStripMenuItem captureWindowMenuItem = (ToolStripMenuItem)sender;
			AddCaptureWindowMenuItems(captureWindowMenuItem, Contextmenu_window_Click);
		}
		
		public static void AddCaptureWindowMenuItems(ToolStripMenuItem menuItem, EventHandler eventHandler) {
			ILanguage lang = Language.GetInstance();
			menuItem.DropDownItems.Clear();
			List<WindowDetails> windows = WindowDetails.GetVisibleWindows();
			foreach(WindowDetails window in windows) {
				ToolStripMenuItem captureWindowItem = new ToolStripMenuItem(window.Text);
				captureWindowItem.Tag = window;
				captureWindowItem.Click += new System.EventHandler(eventHandler);
				menuItem.DropDownItems.Add(captureWindowItem);
			}
		}

		void CaptureAreaToolStripMenuItemClick(object sender, EventArgs e) {
			captureForm.MakeCapture(CaptureMode.Region, false);
		}

		void CaptureClipboardToolStripMenuItemClick(object sender, EventArgs e) {
			CaptureClipboard();
		}
		
		void OpenFileToolStripMenuItemClick(object sender, EventArgs e) {
			CaptureFile();
		}

		void CaptureFullScreenToolStripMenuItemClick(object sender, EventArgs e) {
			captureForm.MakeCapture(CaptureMode.FullScreen, false);
		}
		
		void Contextmenu_capturelastregionClick(object sender, EventArgs e) {
			captureForm.MakeCapture(CaptureMode.LastRegion, false);
		}

		void Contextmenu_window_Click(object sender,EventArgs e) {
			ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
			try {
				WindowDetails windowToCapture = (WindowDetails)clickedItem.Tag;
				captureForm.MakeCapture(windowToCapture);
			} catch (Exception exception) {
				LOG.Error(exception);
			}
		}

		void Contextmenu_captureIE_Click(object sender, EventArgs e) {
			if (!conf.IECapture) {
				LOG.InfoFormat("IE Capture is disabled.");
				return;
			}
			ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
			KeyValuePair<WindowDetails, int> tabData = (KeyValuePair<WindowDetails, int>)clickedItem.Tag;
			try {
				IECaptureHelper.ActivateIETab(tabData.Key, tabData.Value);
			} catch (Exception exception) {
				LOG.Error(exception);
			}
			try {
				captureForm.MakeCapture(CaptureMode.IE, false);
			} catch (Exception exception) {
				LOG.Error(exception);
			}
		}

		void Contextmenu_donateClick(object sender, EventArgs e) {
			Process.Start("http://getgreenshot.org/support/");
		}
		
		void Contextmenu_settingsClick(object sender, EventArgs e) {
			SettingsForm settings = new SettingsForm();
			settings.ShowDialog();
			InitializeQuickSettingsMenu();
			this.Hide();
		}
		
		void Contextmenu_aboutClick(object sender, EventArgs e) {
			new AboutForm().Show();
		}
		
		void Contextmenu_helpClick(object sender, EventArgs e) {
			HelpBrowserForm hpf = new HelpBrowserForm(conf.Language);
			hpf.Show();
		}
		
		void Contextmenu_exitClick(object sender, EventArgs e) {
			 exit();
		}
		
		private void InitializeQuickSettingsMenu() {
			this.contextmenu_quicksettings.DropDownItems.Clear();
			// screenshot destination
			ToolStripMenuSelectList selectList = new ToolStripMenuSelectList("destination",true);
			selectList.Text = lang.GetString(LangKey.settings_destination);
			selectList.AddItem(lang.GetString(LangKey.settings_destination_editor), Destination.Editor, conf.OutputDestinations.Contains(Destination.Editor));
			selectList.AddItem(lang.GetString(LangKey.settings_destination_clipboard), Destination.Clipboard, conf.OutputDestinations.Contains(Destination.Clipboard));
			selectList.AddItem(lang.GetString(LangKey.quicksettings_destination_file), Destination.FileDefault, conf.OutputDestinations.Contains(Destination.FileDefault));
			selectList.AddItem(lang.GetString(LangKey.settings_destination_fileas), Destination.FileWithDialog, conf.OutputDestinations.Contains(Destination.FileWithDialog));
			selectList.AddItem(lang.GetString(LangKey.settings_destination_printer), Destination.Printer, conf.OutputDestinations.Contains(Destination.Printer));
			selectList.AddItem(lang.GetString(LangKey.settings_destination_email), Destination.EMail, conf.OutputDestinations.Contains(Destination.EMail));
			selectList.CheckedChanged += new EventHandler(this.QuickSettingItemChanged);
			this.contextmenu_quicksettings.DropDownItems.Add(selectList);
			// print options
			selectList = new ToolStripMenuSelectList("printoptions",true);
			selectList.Text = lang.GetString(LangKey.settings_printoptions);
			selectList.AddItem(lang.GetString(LangKey.printoptions_allowshrink), "AllowPrintShrink", conf.OutputPrintAllowShrink);
			selectList.AddItem(lang.GetString(LangKey.printoptions_allowenlarge), "AllowPrintEnlarge", conf.OutputPrintAllowEnlarge);
			selectList.AddItem(lang.GetString(LangKey.printoptions_allowrotate), "AllowPrintRotate", conf.OutputPrintAllowRotate);
			selectList.AddItem(lang.GetString(LangKey.printoptions_allowcenter), "AllowPrintCenter", conf.OutputPrintCenter);
			selectList.CheckedChanged += new EventHandler(this.QuickSettingItemChanged);
			this.contextmenu_quicksettings.DropDownItems.Add(selectList);
			// effects
			selectList = new ToolStripMenuSelectList("effects",true);
			selectList.Text = lang.GetString(LangKey.settings_visualization);
			selectList.AddItem(lang.GetString(LangKey.settings_playsound), "PlaySound", conf.PlayCameraSound);
			selectList.CheckedChanged += new EventHandler(this.QuickSettingItemChanged);
			this.contextmenu_quicksettings.DropDownItems.Add(selectList);
		}
		
		void QuickSettingItemChanged(object sender, EventArgs e) {
			ToolStripMenuSelectList selectList = (ToolStripMenuSelectList)sender;
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs)e).Item;
			if(selectList.Identifier.Equals("destination")) {
				Destination selectedDestination = (Destination)item.Data;
				if (item.Checked && !conf.OutputDestinations.Contains(selectedDestination)) {
					conf.OutputDestinations.Add(selectedDestination);
				}
				if (!item.Checked && conf.OutputDestinations.Contains(selectedDestination)) {
					conf.OutputDestinations.Remove(selectedDestination);
				}
				IniConfig.Save();
			} else if(selectList.Identifier.Equals("printoptions")) {
				if(item.Data.Equals("AllowPrintShrink")) conf.OutputPrintAllowShrink = item.Checked;
				else if(item.Data.Equals("AllowPrintEnlarge")) conf.OutputPrintAllowEnlarge = item.Checked;
				else if(item.Data.Equals("AllowPrintRotate")) conf.OutputPrintAllowRotate = item.Checked;
				else if(item.Data.Equals("AllowPrintCenter")) conf.OutputPrintCenter = item.Checked;
				IniConfig.Save();
			} else if(selectList.Identifier.Equals("effects")) {
				if(item.Data.Equals("PlaySound")) {
					conf.PlayCameraSound = item.Checked;
				}
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
		
		private void NotifyIconClick(object sender, EventArgs eventArgs) {
			MethodInfo oMethodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
			oMethodInfo.Invoke(notifyIcon, null);
		}

		/// <summary>
		/// The Contextmenu_OpenRecent currently opens the last know save location
		/// </summary>
		private void Contextmenu_OpenRecent(object sender, EventArgs eventArgs) {
			string path;
			string configPath = FilenameHelper.FillVariables(conf.OutputFilePath);
			string lastFilePath = Path.GetDirectoryName(conf.OutputFileAsFullpath);
			if (Directory.Exists(lastFilePath)) {
				path = lastFilePath;
			} else if (Directory.Exists(configPath)) {
				path = configPath;
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
		/// Shutdown / cleanup
		/// </summary>
		public void exit() {
			ClipboardHelper.DeregisterClipboardViewer(this.Handle);
			
			LOG.Info("Exit: " + EnvironmentInfo.EnvironmentToString(false));

			// Close all open forms (except this), use a separate List to make sure we don't get a "InvalidOperationException: Collection was modified"
			List<Form> formsToClose = new List<Form>();
			foreach(Form form in Application.OpenForms) {
				if (form.Handle != this.Handle && !form.GetType().Equals(typeof(Greenshot.ImageEditorForm))) {
					formsToClose.Add(form);
				}
			}
			foreach(Form form in formsToClose) {
				try {
					LOG.InfoFormat("Closing form: {0}", form.Name);
					this.Invoke((MethodInvoker) delegate { form.Close(); });
				} catch (Exception e) {
					LOG.Error("Error closing form!", e);
				}
			}
			
			// Make sure hotkeys are disabled
			try {
				HotkeyControl.UnregisterHotkeys();
			} catch (Exception e) {
				LOG.Error("Error unregistering hotkeys!", e);
			}
	
			// Now the sound isn't needed anymore
			try {
				SoundHelper.Deinitialize();
			} catch (Exception e) {
				LOG.Error("Error deinitializing sound!", e);
			}
	
			// Inform all registed plugins
			try {
				PluginHelper.instance.Shutdown();
			} catch (Exception e) {
				LOG.Error("Error shutting down plugins!", e);
			}

			// Gracefull shutdown
			try {
				Application.DoEvents();
				Application.Exit();
			} catch (Exception e) {
				LOG.Error("Error closing application!", e);
			}

			// Store any open configuration changes
			try {
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
		
		/// <summary>
		/// Do work in the background
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BackgroundWorkerTimerTick(object sender, EventArgs e) {
			LOG.Debug("BackgroundWorkerTimerTick");
			
			if (UpdateHelper.IsUpdateCheckNeeded()) {
				// Start update check in the background
				Thread backgroundTask = new Thread (new ThreadStart(UpdateHelper.CheckAndAskForUpdate));
				backgroundTask.IsBackground = true;
				backgroundTask.Start();
			}
		}
	}
}