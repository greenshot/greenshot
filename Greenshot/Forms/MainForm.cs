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
using Greenshot.Drawing;
using Greenshot.Forms;
using Greenshot.Help;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.UnmanagedHelpers;
using Greenshot.Core;

namespace Greenshot {
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form {
		private const string LOG4NET_FILE = "log4net.xml";
		private static log4net.ILog LOG = null;
		private static AppConfig conf;

		static Mutex applicationMutex = null;
		
		[STAThread]
		public static void Main(string[] args) {
			DataTransport dataTransport = null;
			// Set the Thread name, is better than "1"
			Thread.CurrentThread.Name = Application.ProductName;
			
			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
    		AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
    		
    		// Setup log4j, currently the file is called log4net.xml
    		string log4netFilename = Path.Combine(Application.StartupPath, LOG4NET_FILE);
    		if (File.Exists(log4netFilename)) {
	    		log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(log4netFilename)); 
    		} else {
    			MessageBox.Show("Can't find file " + LOG4NET_FILE);
    		}
    		
    		// Setup the LOG
			LOG = log4net.LogManager.GetLogger(typeof(MainForm));

			// Log the startup
			LOG.Info("Starting: " + EnvironmentInfo.EnvironmentToString(false));
			try {
				for(int argumentNr = 0; argumentNr < args.Length; argumentNr++) {
					string argument = args[argumentNr];
					
					// Help
					if (argument.Equals("--help")) {
						// Try to attach to the console
						bool attachedToConsole = User32.AttachConsole(User32.ATTACH_PARENT_PROCESS);
						// If attach didn't work, open a console
						if (!attachedToConsole) {
							User32.AllocConsole();
						}
						StringBuilder helpOutput = new StringBuilder();
						helpOutput.AppendLine();
						if (argumentNr + 1 < args.Length && args[argumentNr + 1].Equals("configure")) {
							helpOutput.AppendLine("Available configuration settings:");
							
							Properties properties = AppConfig.GetAvailableProperties();
							foreach(string key in properties.Keys) {
								helpOutput.AppendLine("\t\t" + key + "=" + properties.GetProperty(key));
							}
						} else {
							helpOutput.AppendLine("Greenshot commandline options:");
							helpOutput.AppendLine();
							helpOutput.AppendLine();
							helpOutput.AppendLine("\t--help [config]");
							helpOutput.AppendLine("\t\tThis help.");
							helpOutput.AppendLine();
							helpOutput.AppendLine();
							helpOutput.AppendLine("\t--uninstall");
							helpOutput.AppendLine("\t\tUnstall is called from the unstaller and tries to close all running instances.");
							helpOutput.AppendLine();
							helpOutput.AppendLine();
							helpOutput.AppendLine("\t--configure [property=value] ...");
							helpOutput.AppendLine("\t\tChange the configuration of Greenshot via the commandline.");
							helpOutput.AppendLine("\t\tExample to change the language to English: greenshot.exe --config Ui_Language=en-US");
							helpOutput.AppendLine("\t\tExample to change the destination: greenshot.exe --config Output_File_Path=\"C:\\Documents and Settings\\\"");
							helpOutput.AppendLine();
							helpOutput.AppendLine();
							helpOutput.AppendLine("\t--openfile [filename]");
							helpOutput.AppendLine("\t\tOpen the bitmap file in the running Greenshot instance or start a new instance");
							
						}
						Console.WriteLine(helpOutput.ToString());

						// If attach didn't work, wait for key otherwise the console will close to quickly
						if (!attachedToConsole) {
							Console.ReadKey();
						}
						return;
					}

					// unregister application on uninstall (allow uninstall)
					if (argument.Equals("--uninstall") || argument.Equals("uninstall")) {
						try {
							// Pass Exit to running instance, if any
							dataTransport = new DataTransport(CommandEnum.Exit, args[0]);
	    					SendData(dataTransport);
						} catch (Exception e) {
							LOG.Warn("Exception by exit.", e);
						}
						return;
					}

					// Modify configuration
					if (argument.Equals("--configure")) {
						LOG.Debug("Setting configuration!");
						conf = AppConfig.GetInstance();
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
							conf.SetProperties(properties);
							conf.Store();
							// Update running instances
							SendData(new DataTransport(CommandEnum.ReloadConfig));
							LOG.Debug("Configuration modified!");
						} else {
							LOG.Debug("Configuration NOT modified!");
						}
					}

					// Make an exit possible
					if (argument.Equals("--exit")) {
	    				return;
					}					

					if (argument.Equals("--openfile")) {
	    				// Take filename and send it to running instance or take it while opening
	    				dataTransport = new DataTransport(CommandEnum.OpenFile, args[0]);
					}
				}

				// Fix for Bug 2495900, Multi-user Environment
				// check whether there's an local instance running already
				
				// 1) Create Mutex
    			applicationMutex = new Mutex(false, @"Local\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08");
    			// 2) Get the right to it, this returns false if it's already locked
				if (!applicationMutex.WaitOne(0, false)) {
    				if (dataTransport != null) {
    					SendData(dataTransport);
    				} else {
						conf = AppConfig.GetInstance();
						ILanguage lang = Language.GetInstance();
						MessageBox.Show(lang.GetString(LangKey.error_multipleinstances), lang.GetString(LangKey.error));
    				}
					Application.Exit();
					return;
				}
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				conf = AppConfig.GetInstance();

				// if language is not set, show language dialog
				if(conf.Ui_Language.Equals("")) {
					LanguageDialog ld = LanguageDialog.GetInstance();
					ld.ShowDialog();
					conf.Ui_Language = ld.Language;
					conf.Store();
				}

				// Check if it's the first time launch?
				bool firstLaunch = (bool)conf.General_IsFirstLaunch;
	   			if(firstLaunch) {
					// todo: display basic instructions
					try {
					} catch (Exception ex) {
						LOG.Error("Exception in MainForm.", ex);
					} finally {
						conf.General_IsFirstLaunch = false;
						conf.Store();
					}
				}
				// Pass firstlaunch if it is the firstlaunch and no filename is given
				if (dataTransport ==  null && firstLaunch) {
					dataTransport = new DataTransport(CommandEnum.FirstLaunch, null);
				}
				MainForm mainForm = new MainForm(dataTransport);
				Application.Run();
			} catch(Exception ex) {
				LOG.Error("Exception in startup.", ex);
				Application_ThreadException(MainForm.ActiveForm, new ThreadExceptionEventArgs(ex));
			}
		}

		/// <summary>
		/// Send DataTransport Object via Window-messages
		/// </summary>
		/// <param name="dataTransport">DataTransport with data for a running instance</param>
		private static void SendData(DataTransport dataTransport) {
			string appName = Application.ProductName;
			CopyData copyData = new CopyData();
			copyData.Channels.Add(appName);
			copyData.Channels[appName].Send(dataTransport);
		}
		
		public static MainForm instance = null;

		private ILanguage lang;
		private ToolTip tooltip;
		private CaptureForm captureForm = null;
		private string lastImagePath = null;
		
		public MainForm(DataTransport dataTransport) {
			instance = this;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			lang = Language.GetInstance();
			if((bool)conf.General_RegisterHotkeys) {
				RegisterHotkeys();
			}
			
			tooltip = new ToolTip();
			
			UpdateUI();
			InitializeQuickSettingsMenu();

			captureForm = new CaptureForm();

			// Load all the plugins
			PluginHelper.instance.LoadPlugins(this, captureForm);
			PluginHelper.instance.OnImageOutput += new OnImageOutputHandler(ImageWritten);
			SoundHelper.Initialize();

			// Enable the Greenshot icon to be visible, this prevents Problems with the context menu
			notifyIcon.Visible = true;
			
			if (dataTransport != null) {
			// See what the dataTransport from the static main brought us!
				switch (dataTransport.Command) {
					case CommandEnum.FirstLaunch:
						// Show user where Greenshot is, only at first start
						notifyIcon.ShowBalloonTip(3000, "Greenshot", lang.GetString(LangKey.tooltip_firststart), ToolTipIcon.Info);
						break;
					case CommandEnum.OpenFile:
						captureForm.HandleDataTransport(dataTransport);
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
			suc &= HotkeyHelper.RegisterHotKey((int)this.Handle, (int)HotkeyHelper.Modifiers.NONE, HotkeyHelper.VK_SNAPSHOT, new HotKeyHandler(CaptureRegion));
			suc &= HotkeyHelper.RegisterHotKey((int)this.Handle, (int)HotkeyHelper.Modifiers.ALT, HotkeyHelper.VK_SNAPSHOT, new HotKeyHandler(CaptureWindow));
			suc &= HotkeyHelper.RegisterHotKey((int)this.Handle, (int)HotkeyHelper.Modifiers.CTRL, HotkeyHelper.VK_SNAPSHOT, new HotKeyHandler(CaptureFullScreen));
			suc &= HotkeyHelper.RegisterHotKey((int)this.Handle, (int)HotkeyHelper.Modifiers.SHIFT, HotkeyHelper.VK_SNAPSHOT, new HotKeyHandler(CaptureLastRegion));
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
			LOG.Info("Closing with reason: " + e.CloseReason);
			exit();
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
			if ((conf.Capture_Windows_Interactive.HasValue && conf.Capture_Windows_Interactive.Value)) {
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
			HelpBrowserForm hpf = new HelpBrowserForm(conf.Ui_Language);
			hpf.Show();
		}
		
		void Contextmenu_exitClick(object sender, EventArgs e) {
			exit();
			Application.Exit();
		}
		
		private void InitializeQuickSettingsMenu() {
			this.contextmenu_quicksettings.DropDownItems.Clear();
			// screenshot destination
			ToolStripMenuSelectList sel = new ToolStripMenuSelectList("destination",true);
			sel.Text = lang.GetString(LangKey.settings_destination);
			sel.AddItem(lang.GetString(LangKey.settings_destination_editor), ScreenshotDestinations.Editor, (conf.Output_Destinations&ScreenshotDestinations.Editor)==ScreenshotDestinations.Editor);
			sel.AddItem(lang.GetString(LangKey.settings_destination_clipboard), ScreenshotDestinations.Clipboard, (conf.Output_Destinations&ScreenshotDestinations.Clipboard)==ScreenshotDestinations.Clipboard);
			sel.AddItem(lang.GetString(LangKey.quicksettings_destination_file), ScreenshotDestinations.FileDefault, (conf.Output_Destinations&ScreenshotDestinations.FileDefault)==ScreenshotDestinations.FileDefault);
			sel.AddItem(lang.GetString(LangKey.settings_destination_fileas), ScreenshotDestinations.FileWithDialog, (conf.Output_Destinations&ScreenshotDestinations.FileWithDialog)==ScreenshotDestinations.FileWithDialog);
			sel.AddItem(lang.GetString(LangKey.settings_destination_printer), ScreenshotDestinations.Printer, (conf.Output_Destinations&ScreenshotDestinations.Printer)==ScreenshotDestinations.Printer);
			sel.CheckedChanged += new EventHandler(this.QuickSettingItemChanged);
			this.contextmenu_quicksettings.DropDownItems.Add(sel);
			// print options
			sel = new ToolStripMenuSelectList("printoptions",true);
			sel.Text = lang.GetString(LangKey.settings_printoptions);
			sel.AddItem(lang.GetString(LangKey.printoptions_allowshrink), "AllowPrintShrink", (bool)conf.Output_Print_AllowShrink);
			sel.AddItem(lang.GetString(LangKey.printoptions_allowenlarge), "AllowPrintEnlarge", (bool)conf.Output_Print_AllowEnlarge);
			sel.AddItem(lang.GetString(LangKey.printoptions_allowrotate), "AllowPrintRotate", (bool)conf.Output_Print_AllowRotate);
			sel.AddItem(lang.GetString(LangKey.printoptions_allowcenter), "AllowPrintCenter", (bool)conf.Output_Print_Center);
			sel.CheckedChanged += new EventHandler(this.QuickSettingItemChanged);
			this.contextmenu_quicksettings.DropDownItems.Add(sel);
			// effects
			sel = new ToolStripMenuSelectList("effects",true);
			sel.Text = lang.GetString(LangKey.settings_visualization);
			sel.AddItem(lang.GetString(LangKey.settings_playsound), "PlaySound", (bool)conf.Ui_Effects_CameraSound);
			sel.AddItem(lang.GetString(LangKey.settings_showflashlight), "ShowFlashlight", (bool)conf.Ui_Effects_Flashlight);
			sel.CheckedChanged += new EventHandler(this.QuickSettingItemChanged);
			this.contextmenu_quicksettings.DropDownItems.Add(sel);
		}
		
		void QuickSettingItemChanged(object sender, EventArgs e) {
			ToolStripMenuSelectList selectList = (ToolStripMenuSelectList)sender;
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs)e).Item;;
			if(selectList.Identifier.Equals("destination")) {
				IEnumerator en = selectList.DropDownItems.GetEnumerator();
				ScreenshotDestinations dest = 0;
				while(en.MoveNext()) {
					ToolStripMenuSelectListItem i = (ToolStripMenuSelectListItem)en.Current;
					if(i.Checked) dest |= (ScreenshotDestinations)i.Data;
				}
				conf.Output_Destinations = dest;
			   	conf.Store();
			} else if(selectList.Identifier.Equals("printoptions")) {
				if(item.Data.Equals("AllowPrintShrink")) conf.Output_Print_AllowShrink = (bool?)item.Checked;
				else if(item.Data.Equals("AllowPrintEnlarge")) conf.Output_Print_AllowEnlarge = (bool?)item.Checked;
				else if(item.Data.Equals("AllowPrintRotate")) conf.Output_Print_AllowRotate = (bool?)item.Checked;
				else if(item.Data.Equals("AllowPrintCenter")) conf.Output_Print_Center = (bool?)item.Checked;
				conf.Store();
			} else if(selectList.Identifier.Equals("effects")) {
				if(item.Data.Equals("PlaySound")) conf.Ui_Effects_CameraSound = (bool?)item.Checked;
				else if(item.Data.Equals("ShowFlashlight")) conf.Ui_Effects_Flashlight = (bool?)item.Checked;
				conf.Store();
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
		/// Exit/cleanup
		/// </summary>
		private void exit() {
			// Inform all registed plugins
			PluginHelper.instance.Shutdown();

			conf.Store();
			HotkeyHelper.UnregisterHotkeys((int)this.Handle);
			SoundHelper.Deinitialize();
			if (applicationMutex != null) {
				try {
					applicationMutex.ReleaseMutex();
				} catch (Exception ex) {
					LOG.Error("Error releasing Mutex!", ex);
				}
			}
		}
	}
}
