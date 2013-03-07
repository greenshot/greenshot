/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Experimental;
using Greenshot.Forms;
using Greenshot.Help;
using Greenshot.Helpers;
using Greenshot.Plugin;
using GreenshotPlugin.UnmanagedHelpers;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using Greenshot.Destinations;

namespace Greenshot {
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : BaseForm {
		private static log4net.ILog LOG = null;
		private static Mutex applicationMutex = null;
		private static CoreConfiguration conf;
		public static string LogFileLocation = null;

		public static void Start(string[] args) {
			bool isAlreadyRunning = false;
			List<string> filesToOpen = new List<string>();

			// Set the Thread name, is better than "1"
			Thread.CurrentThread.Name = Application.ProductName;
			
			// Init Log4NET
			LogFileLocation = LogHelper.InitializeLog4NET();
			// Get logger
			LOG = log4net.LogManager.GetLogger(typeof(MainForm));

			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			// Initialize the IniConfig
			IniConfig.Init();

			// Log the startup
			LOG.Info("Starting: " + EnvironmentInfo.EnvironmentToString(false));

			// Upgrade if needed
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
					if (argument.ToLower().Equals("/help") || argument.ToLower().Equals("/h") || argument.ToLower().Equals("/?")) {
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
						helpOutput.AppendLine("\t\tSet the language of Greenshot, e.g. greenshot /language en-US.");
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

				if (isAlreadyRunning) {
					// We didn't initialize the language yet, do it here just for the message box
					if (filesToOpen.Count > 0) {
						SendData(transport);
					} else {
						StringBuilder instanceInfo = new StringBuilder();
						bool matchedThisProcess = false;
						int index = 1;
						foreach (Process greenshotProcess in Process.GetProcessesByName("greenshot")) {
							try {
								instanceInfo.Append(index++ + ": ").AppendLine(Kernel32.GetProcessPath(new IntPtr(greenshotProcess.Id)));
								if (Process.GetCurrentProcess().Id == greenshotProcess.Id) {
									matchedThisProcess = true;
								}
							} catch (Exception ex) {
								LOG.Debug(ex);
							}
						}
						if (!matchedThisProcess) {
							instanceInfo.Append(index++ + ": ").AppendLine(Kernel32.GetProcessPath(new IntPtr(Process.GetCurrentProcess().Id)));
						}
						MessageBox.Show(Language.GetString(LangKey.error_multipleinstances) + "\r\n" + instanceInfo.ToString(), Language.GetString(LangKey.error));
					}
					FreeMutex();
					Application.Exit();
					return;
				}

				// From here on we continue starting Greenshot
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				// if language is not set, show language dialog
				if(string.IsNullOrEmpty(conf.Language)) {
					LanguageDialog languageDialog = LanguageDialog.GetInstance();
					languageDialog.ShowDialog();
					conf.Language = languageDialog.SelectedLanguage;
					IniConfig.Save();
				}

				// Check if it's the first time launch?
				if(conf.IsFirstLaunch) {
					conf.IsFirstLaunch = false;
					IniConfig.Save();
					transport.AddCommand(CommandEnum.FirstLaunch);
				}

				MainForm mainForm = new MainForm(transport);
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

		private static MainForm instance = null;
		public static MainForm Instance {
			get {
				return instance;
			}
		}

		private ToolTip tooltip;
		private CopyData copyData = null;
		
		// Thumbnail preview
		private ThumbnailForm thumbnailForm = null;
		private IntPtr thumbnailHandle = IntPtr.Zero;
		private Rectangle parentMenuBounds = Rectangle.Empty;
		// Make sure we have only one settings form
		private SettingsForm settingsForm = null;
		// Make sure we have only one about form
		private AboutForm aboutForm = null;
		// Timer for the double click test
		private System.Timers.Timer doubleClickTimer = new System.Timers.Timer();

		public NotifyIcon NotifyIcon {
			get {
				return notifyIcon;				
			}
		}

		public MainForm(CopyDataTransport dataTransport) {
			instance = this;
			
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			try {
				InitializeComponent();
			} catch (ArgumentException ex) {
				// Added for Bug #1420, this doesn't solve the issue but maybe the user can do something with it.
				ex.Data.Add("more information here", "http://support.microsoft.com/kb/943140");
				throw;
			}
			this.notifyIcon.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();

			// Disable access to the settings, for feature #3521446
			contextmenu_settings.Visible = !conf.DisableSettings;

			IniConfig.IniChanged += new FileSystemEventHandler(ReloadConfiguration);
			
			// Make sure all hotkeys pass this window!
			HotkeyControl.RegisterHotkeyHWND(this.Handle);
			RegisterHotkeys();
			
			tooltip = new ToolTip();
			
			UpdateUI();
			
			// This forces the registration of all destinations inside Greenshot itself.
			DestinationHelper.GetAllDestinations();
			// This forces the registration of all processors inside Greenshot itself.
			ProcessorHelper.GetAllProcessors();
			
			// Load all the plugins
			PluginHelper.Instance.LoadPlugins();

			// Check destinations, remove all that don't exist
			foreach(string destination in conf.OutputDestinations.ToArray()) {
				if (DestinationHelper.GetDestination(destination) == null) {
					conf.OutputDestinations.Remove(destination);
				}
			}

			// we should have at least one!
			if (conf.OutputDestinations.Count == 0) {
				conf.OutputDestinations.Add(Destinations.EditorDestination.DESIGNATION);
			}
			if (conf.DisableQuickSettings) {
				contextmenu_quicksettings.Visible = false;
			} else {
				// Do after all plugins & finding the destination, otherwise they are missing!
				InitializeQuickSettingsMenu();
			}
			SoundHelper.Initialize();

			// Set the Greenshot icon visibility depending on the configuration. (Added for feature #3521446)
			// Setting it to true this late prevents Problems with the context menu
			notifyIcon.Visible = !conf.HideTrayicon;

			// Make sure we never capture the mainform
			WindowDetails.RegisterIgnoreHandle(this.Handle);

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
						LOG.Info("Exit requested");
						Exit();
						break;
					case CommandEnum.FirstLaunch:
						LOG.Info("FirstLaunch: Created new configuration, showing balloon.");

						try {
							EventHandler balloonTipClickedHandler = null;
							EventHandler balloonTipClosedHandler = null;
							balloonTipClosedHandler = delegate(object sender, EventArgs e) {
								notifyIcon.BalloonTipClicked -= balloonTipClickedHandler;
								notifyIcon.BalloonTipClosed -= balloonTipClosedHandler;
							};

							balloonTipClickedHandler = delegate(object sender, EventArgs e) {
								ShowSetting();
								notifyIcon.BalloonTipClicked -= balloonTipClickedHandler;
								notifyIcon.BalloonTipClosed -= balloonTipClosedHandler;
							};
							notifyIcon.BalloonTipClicked += balloonTipClickedHandler;
							notifyIcon.BalloonTipClosed += balloonTipClosedHandler;
							notifyIcon.ShowBalloonTip(2000, "Greenshot", Language.GetFormattedString(LangKey.tooltip_firststart, HotkeyControl.GetLocalizedHotkeyStringFromString(conf.RegionHotkey)), ToolTipIcon.Info);
						} catch {}
						break;
					case CommandEnum.ReloadConfig:
						LOG.Info("Reload requested");
						try {
							IniConfig.Reload();
							ReloadConfiguration(null, null);
						} catch {}
						break;
					case CommandEnum.OpenFile:
						string filename = command.Value;
						LOG.InfoFormat("Open file requested: {0}", filename);
						if (File.Exists(filename)) {
							BeginInvoke((MethodInvoker)delegate {
								CaptureHelper.CaptureFile(filename);
							});
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
		
		/// <summary>
		/// This is called when the ini-file changes
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void ReloadConfiguration(object source, FileSystemEventArgs e) {
			Language.CurrentLanguage = null;	// Reload
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
			if (HotkeyControl.HandleMessages(ref m)) {
				return;
			}
			base.WndProc(ref m);
		}

		/// <summary>
		/// Helper method to cleanly register a hotkey
		/// </summary>
		/// <param name="failedKeys"></param>
		/// <param name="functionName"></param>
		/// <param name="hotkeyString"></param>
		/// <param name="handler"></param>
		/// <returns></returns>
		private static bool RegisterHotkey(StringBuilder failedKeys, string functionName, string hotkeyString, HotKeyHandler handler) {
			Keys modifierKeyCode = HotkeyControl.HotkeyModifiersFromString(hotkeyString);
			Keys virtualKeyCode = HotkeyControl.HotkeyFromString(hotkeyString);
			if (!Keys.None.Equals(virtualKeyCode)) {
				if (HotkeyControl.RegisterHotKey(modifierKeyCode, virtualKeyCode, handler) < 0) {
					LOG.DebugFormat("Failed to register {0} to hotkey: {1}", functionName, hotkeyString);
					if (failedKeys.Length > 0) {
						failedKeys.Append(", ");
					}
					failedKeys.Append(hotkeyString);
					return false;
				} else {
					LOG.DebugFormat("Registered {0} to hotkey: {1}", functionName, hotkeyString);
				}
			} else {
				LOG.InfoFormat("Skipping hotkey registration for {0}, no hotkey set!", functionName);
			}
			return true;
		}

		private static bool RegisterWrapper(StringBuilder failedKeys, string functionName, string configurationKey, HotKeyHandler handler) {
			IniValue hotkeyValue = conf.Values[configurationKey];
			try {
				return RegisterHotkey(failedKeys, functionName, hotkeyValue.Value.ToString(), handler);
			} catch (Exception ex) {
				LOG.Warn(ex);
				LOG.WarnFormat("Repairing the hotkey for {0}, stored under {1} from '{2}' to '{3}'", functionName, configurationKey, hotkeyValue.Value, hotkeyValue.Attributes.DefaultValue);
				// when getting an exception the key wasn't found: reset the hotkey value
				hotkeyValue.UseValueOrDefault(null);
				hotkeyValue.ContainingIniSection.IsDirty = true;
				return RegisterHotkey(failedKeys, functionName, hotkeyValue.Value.ToString(), handler);
			}
		}

		public static void RegisterHotkeys() {
			if (instance == null) {
				return;
			}
			bool success = true;
			StringBuilder failedKeys = new StringBuilder();

			if (!RegisterWrapper(failedKeys, "CaptureRegion", "RegionHotkey", new HotKeyHandler(instance.CaptureRegion))) {
				success = false;
			}
			if (!RegisterWrapper(failedKeys, "CaptureWindow", "WindowHotkey", new HotKeyHandler(instance.CaptureWindow))) {
				success = false;
			}
			if (!RegisterWrapper(failedKeys, "CaptureFullScreen", "FullscreenHotkey", new HotKeyHandler(instance.CaptureFullScreen))) {
				success = false;
			}
			if (!RegisterWrapper(failedKeys, "CaptureLastRegion", "LastregionHotkey", new HotKeyHandler(instance.CaptureLastRegion))) {
				success = false;
			}
			if (conf.IECapture) {
				if (!RegisterWrapper(failedKeys, "CaptureIE", "IEHotkey", new HotKeyHandler(instance.CaptureIE))) {
					success = false;
				}
			}

			if (!success) {
				MessageBox.Show(MainForm.Instance, Language.GetFormattedString(LangKey.warning_hotkeys, failedKeys.ToString()),Language.GetString(LangKey.warning), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		#endregion
		
		public void UpdateUI() {
			// As the form is never loaded, call ApplyLanguage ourselves
			ApplyLanguage();

			// Show hotkeys in Contextmenu
			this.contextmenu_capturearea.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.RegionHotkey);
			this.contextmenu_capturelastregion.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.LastregionHotkey);
			this.contextmenu_capturewindow.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.WindowHotkey);
			this.contextmenu_capturefullscreen.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.FullscreenHotkey);
			this.contextmenu_captureie.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(conf.IEHotkey);
		}
		
		
		#region mainform events
		void MainFormFormClosing(object sender, FormClosingEventArgs e) {
			LOG.DebugFormat("Mainform closing, reason: {0}", e.CloseReason);
			instance = null;
			Exit();
		}

		void MainFormActivated(object sender, EventArgs e) {
			Hide();
			ShowInTaskbar = false;
		}
		#endregion

		#region key handlers
		void CaptureRegion() {
			CaptureHelper.CaptureRegion(true);
		}

		void CaptureClipboard() {
			CaptureHelper.CaptureClipboard();
		}

		void CaptureFile() {
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image files (*.greenshot, *.png, *.jpg, *.gif, *.bmp, *.ico, *.tiff, *.wmf)|*.greenshot; *.png; *.jpg; *.jpeg; *.gif; *.bmp; *.ico; *.tiff; *.tif; *.wmf";
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				if (File.Exists(openFileDialog.FileName)) {
					CaptureHelper.CaptureFile(openFileDialog.FileName);
				}
			}
		}

		void CaptureFullScreen() {
			CaptureHelper.CaptureFullscreen(true, conf.ScreenCaptureMode);
		}

		void CaptureLastRegion() {
			CaptureHelper.CaptureLastRegion(true);
		}

		void CaptureIE() {
			if (conf.IECapture) {
				CaptureHelper.CaptureIE(true, null);
			}
		}

		void CaptureWindow() {
			if (conf.CaptureWindowsInteractive) {
				CaptureHelper.CaptureWindowInteractive(true);
			} else {
				CaptureHelper.CaptureWindow(true);
			}
		}
		#endregion


		#region contextmenu
		void ContextMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)	{
			contextmenu_captureclipboard.Enabled = ClipboardHelper.ContainsImage();
			contextmenu_capturelastregion.Enabled = RuntimeConfig.LastCapturedRegion != Rectangle.Empty;

			// IE context menu code
			try {
				if (conf.IECapture && IECaptureHelper.IsIERunning()) {
					this.contextmenu_captureie.Enabled = true;
					this.contextmenu_captureiefromlist.Enabled = true;
				} else {
					this.contextmenu_captureie.Enabled = false;
					this.contextmenu_captureiefromlist.Enabled = false;
				}
			} catch (Exception ex) {
				LOG.WarnFormat("Problem accessing IE information: {0}", ex.Message);
			}

			// Multi-Screen captures
			this.contextmenu_capturefullscreen.Click -= new System.EventHandler(this.CaptureFullScreenToolStripMenuItemClick);
			this.contextmenu_capturefullscreen.DropDownOpening -= new System.EventHandler(MultiScreenDropDownOpening);
			this.contextmenu_capturefullscreen.DropDownClosed -= new System.EventHandler(MultiScreenDropDownClosing);
			if (Screen.AllScreens.Length > 1) {
				this.contextmenu_capturefullscreen.DropDownOpening += new System.EventHandler(MultiScreenDropDownOpening);
				this.contextmenu_capturefullscreen.DropDownClosed += new System.EventHandler(MultiScreenDropDownClosing);
			} else {
				this.contextmenu_capturefullscreen.Click += new System.EventHandler(this.CaptureFullScreenToolStripMenuItemClick);
			}
		}
		
		void ContextMenuClosing(object sender, EventArgs e) {
			this.contextmenu_captureiefromlist.DropDownItems.Clear();
			this.contextmenu_capturewindowfromlist.DropDownItems.Clear();
			cleanupThumbnail();
		}
		
		/// <summary>
		/// Build a selectable list of IE tabs when we enter the menu item
		/// </summary>
		void CaptureIEMenuDropDownOpening(object sender, EventArgs e) {
			if (!conf.IECapture) {
				return;
			}
			try {
				List<KeyValuePair<WindowDetails, string>> tabs = IECaptureHelper.GetBrowserTabs();
				this.contextmenu_captureiefromlist.DropDownItems.Clear();
				if (tabs.Count > 0) {
					this.contextmenu_captureie.Enabled = true;
					this.contextmenu_captureiefromlist.Enabled = true;
					Dictionary<WindowDetails, int> counter = new Dictionary<WindowDetails, int>();
					
					foreach(KeyValuePair<WindowDetails, string> tabData in tabs) {
						string title = tabData.Value;
						if (title == null) {
							continue;
						}
						if (title.Length > conf.MaxMenuItemLength) {
							title = title.Substring(0, Math.Min(title.Length, conf.MaxMenuItemLength));
						}
						ToolStripItem captureIETabItem = contextmenu_captureiefromlist.DropDownItems.Add(title);
						int index;
						if (counter.ContainsKey(tabData.Key)) {
							index = counter[tabData.Key];
						} else {
							index = 0;
						}
						captureIETabItem.Image = tabData.Key.DisplayIcon;
						captureIETabItem.Tag = new KeyValuePair<WindowDetails, int>(tabData.Key, index++);
						captureIETabItem.Click += new System.EventHandler(Contextmenu_captureiefromlist_Click);
						this.contextmenu_captureiefromlist.DropDownItems.Add(captureIETabItem);
						if (counter.ContainsKey(tabData.Key)) {
							counter[tabData.Key] = index;
						} else {
							counter.Add(tabData.Key, index);
						}
					}
				} else {
					this.contextmenu_captureie.Enabled = false;
					this.contextmenu_captureiefromlist.Enabled = false;
				}
			} catch (Exception ex) {
				LOG.WarnFormat("Problem accessing IE information: {0}", ex.Message);
			}
		}

		/// <summary>
		/// MultiScreenDropDownOpening is called when mouse hovers over the Capture-Screen context menu 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MultiScreenDropDownOpening(object sender, EventArgs e) {
			ToolStripMenuItem captureScreenMenuItem = (ToolStripMenuItem)sender;
			captureScreenMenuItem.DropDownItems.Clear();
			if (Screen.AllScreens.Length > 1) {
				ToolStripMenuItem captureScreenItem;
				Rectangle allScreensBounds = WindowCapture.GetScreenBounds();
				string allDeviceName = "";
				foreach (Screen screen in Screen.AllScreens) {
					string deviceName = screen.DeviceName;
					if (allDeviceName.Length > 0) {
						allDeviceName += " + ";
					}
					allDeviceName += deviceName.Substring(deviceName.Length - 1);
				}
				captureScreenItem = new ToolStripMenuItem(Language.GetString(LangKey.contextmenu_capturefullscreen_all) + " (" + allDeviceName + ")");
				captureScreenItem.Click += delegate {
					BeginInvoke((MethodInvoker)delegate {
						CaptureHelper.CaptureFullscreen(false, ScreenCaptureMode.FullScreen);
					});
				};
				captureScreenMenuItem.DropDownItems.Add(captureScreenItem);
				foreach (Screen screen in Screen.AllScreens) {
					Screen screenToCapture = screen;
					string deviceName = screenToCapture.DeviceName;
					string deviceAlignment = "";
					deviceName = deviceName.Substring(deviceName.Length - 1);
					if(screen.Bounds.Top == allScreensBounds.Top && screen.Bounds.Bottom != allScreensBounds.Bottom) {
						deviceAlignment += " " + Language.GetString(LangKey.contextmenu_capturefullscreen_top);
					} else if(screen.Bounds.Top != allScreensBounds.Top && screen.Bounds.Bottom == allScreensBounds.Bottom) {
						deviceAlignment += " " + Language.GetString(LangKey.contextmenu_capturefullscreen_bottom);
					}
					if(screen.Bounds.Left == allScreensBounds.Left && screen.Bounds.Right != allScreensBounds.Right) {
						deviceAlignment += " " + Language.GetString(LangKey.contextmenu_capturefullscreen_left);
					} else if(screen.Bounds.Left != allScreensBounds.Left && screen.Bounds.Right == allScreensBounds.Right) {
						deviceAlignment += " " + Language.GetString(LangKey.contextmenu_capturefullscreen_right);
					}
					deviceName = deviceAlignment + " ("+ deviceName +")";
					captureScreenItem = new ToolStripMenuItem(deviceName);
					captureScreenItem.Click += delegate {
						BeginInvoke((MethodInvoker)delegate {
							CaptureHelper.CaptureRegion(false, screenToCapture.Bounds);
						});
					};
					captureScreenMenuItem.DropDownItems.Add(captureScreenItem);
				}
			}
		}

		/// <summary>
		/// MultiScreenDropDownOpening is called when mouse leaves the context menu 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MultiScreenDropDownClosing(object sender, EventArgs e) {
			ToolStripMenuItem captureScreenMenuItem = (ToolStripMenuItem)sender;
			captureScreenMenuItem.DropDownItems.Clear();
		}
		
		/// <summary>
		/// Build a selectable list of windows when we enter the menu item
		/// </summary>
		private void CaptureWindowFromListMenuDropDownOpening(object sender, EventArgs e) {
			// The Capture window context menu item used to go to the following code:
			// captureForm.MakeCapture(CaptureMode.Window, false);
			// Now we check which windows are there to capture
			ToolStripMenuItem captureWindowFromListMenuItem = (ToolStripMenuItem)sender;
			AddCaptureWindowMenuItems(captureWindowFromListMenuItem, Contextmenu_capturewindowfromlist_Click);
		}
		
		private void CaptureWindowFromListMenuDropDownClosed(object sender, EventArgs e) {
			cleanupThumbnail();
		}
		
		private void ShowThumbnailOnEnter(object sender, EventArgs e) {
			ToolStripMenuItem captureWindowItem = sender as ToolStripMenuItem;
			WindowDetails window = captureWindowItem.Tag as WindowDetails;
			if (thumbnailForm == null) {
				thumbnailForm = new ThumbnailForm();
			}
			thumbnailForm.ShowThumbnail(window, captureWindowItem.GetCurrentParent().TopLevelControl);
		}

		private void HideThumbnailOnLeave(object sender, EventArgs e) {
			if (thumbnailForm != null) {
				thumbnailForm.Hide();
			}
		}
		
		private void cleanupThumbnail() {
			if (thumbnailForm != null) {
				thumbnailForm.Close();
				thumbnailForm = null;
			}
		}

		public void AddCaptureWindowMenuItems(ToolStripMenuItem menuItem, EventHandler eventHandler) {
			menuItem.DropDownItems.Clear();
			// check if thumbnailPreview is enabled and DWM is enabled
			bool thumbnailPreview = conf.ThumnailPreview && DWM.isDWMEnabled();

			List<WindowDetails> windows = WindowDetails.GetTopLevelWindows();
			foreach(WindowDetails window in windows) {

				string title = window.Text;
				if (title != null) {
					if (title.Length > conf.MaxMenuItemLength) {
						title = title.Substring(0, Math.Min(title.Length, conf.MaxMenuItemLength));
					}
					ToolStripItem captureWindowItem = menuItem.DropDownItems.Add(title);
					captureWindowItem.Tag = window;
					captureWindowItem.Image = window.DisplayIcon;
					captureWindowItem.Click += new System.EventHandler(eventHandler);
					// Only show preview when enabled
					if (thumbnailPreview) {
						captureWindowItem.MouseEnter += new System.EventHandler(ShowThumbnailOnEnter);
						captureWindowItem.MouseLeave += new System.EventHandler(HideThumbnailOnLeave);
					}
				}
			}
		}

		void CaptureAreaToolStripMenuItemClick(object sender, EventArgs e) {
			BeginInvoke((MethodInvoker)delegate {
				CaptureHelper.CaptureRegion(false);
			});
		}

		void CaptureClipboardToolStripMenuItemClick(object sender, EventArgs e) {
			BeginInvoke((MethodInvoker)delegate {
				CaptureHelper.CaptureClipboard();
			});
		}
		
		void OpenFileToolStripMenuItemClick(object sender, EventArgs e) {
			BeginInvoke((MethodInvoker)delegate {
				CaptureFile();
			});
		}

		void CaptureFullScreenToolStripMenuItemClick(object sender, EventArgs e) {
			BeginInvoke((MethodInvoker)delegate {
				CaptureHelper.CaptureFullscreen(false, conf.ScreenCaptureMode);
			});
		}
		
		void Contextmenu_capturelastregionClick(object sender, EventArgs e) {
			BeginInvoke((MethodInvoker)delegate {
				CaptureHelper.CaptureLastRegion(false);
			});
		}
		
		void Contextmenu_capturewindow_Click(object sender,EventArgs e) {
			BeginInvoke((MethodInvoker)delegate {
				CaptureHelper.CaptureWindowInteractive(false);
			});
		}

		void Contextmenu_capturewindowfromlist_Click(object sender,EventArgs e) {
			ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
			BeginInvoke((MethodInvoker)delegate {
				try {
					WindowDetails windowToCapture = (WindowDetails)clickedItem.Tag;
					CaptureHelper.CaptureWindow(windowToCapture);
				} catch (Exception exception) {
					LOG.Error(exception);
				}
			});
		}
		
		void Contextmenu_captureie_Click(object sender, EventArgs e) {
			CaptureIE();
		}

		void Contextmenu_captureiefromlist_Click(object sender, EventArgs e) {
			if (!conf.IECapture) {
				LOG.InfoFormat("IE Capture is disabled.");
				return;
			}
			ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
			KeyValuePair<WindowDetails, int> tabData = (KeyValuePair<WindowDetails, int>)clickedItem.Tag;
			BeginInvoke((MethodInvoker)delegate {
				WindowDetails ieWindowToCapture = tabData.Key;
				if (ieWindowToCapture != null && (!ieWindowToCapture.Visible || ieWindowToCapture.Iconic)) {
					ieWindowToCapture.Restore();
				}
				try {
					IECaptureHelper.ActivateIETab(ieWindowToCapture, tabData.Value);
				} catch (Exception exception) {
					LOG.Error(exception);
				}
				try {
					CaptureHelper.CaptureIE(false, ieWindowToCapture);
				} catch (Exception exception) {
					LOG.Error(exception);
				}
			});
		}

		/// <summary>
		/// Context menu entry "Support Greenshot"
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Contextmenu_donateClick(object sender, EventArgs e) {
			BeginInvoke((MethodInvoker)delegate {
				Process.Start("http://getgreenshot.org/support/");
			});
		}
		
		/// <summary>
		/// Context menu entry "Preferences"
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Contextmenu_settingsClick(object sender, EventArgs e) {
			BeginInvoke((MethodInvoker)delegate {
				ShowSetting();
			});
		}
		
		/// <summary>
		/// This is called indirectly from the context menu "Preferences"
		/// </summary>
		public void ShowSetting() {
			if (settingsForm != null) {
				WindowDetails.ToForeground(settingsForm.Handle);
			} else {
				try {
					using (settingsForm = new SettingsForm()) {
						if (settingsForm.ShowDialog() == DialogResult.OK) {
							InitializeQuickSettingsMenu();
						}
					}
				} finally {
					settingsForm = null;
				}
			}
		}
		
		/// <summary>
		/// The "About Greenshot" entry is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Contextmenu_aboutClick(object sender, EventArgs e) {
			ShowAbout();
		}

		public void ShowAbout() {
			if (aboutForm != null) {
				WindowDetails.ToForeground(aboutForm.Handle);
			} else {
				try {
					using (aboutForm = new AboutForm()) {
						aboutForm.ShowDialog(this);
					}
				} finally {
					aboutForm = null;
				}
			}
		}
		
		/// <summary>
		/// The "Help" entry is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Contextmenu_helpClick(object sender, EventArgs e) {
			HelpFileLoader.LoadHelp();
		}
		
		/// <summary>
		/// The "Exit" entry is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Contextmenu_exitClick(object sender, EventArgs e) {
			 Exit();
		}
		
		/// <summary>
		/// This needs to be called to initialize the quick settings menu entries
		/// </summary>
		private void InitializeQuickSettingsMenu() {
			this.contextmenu_quicksettings.DropDownItems.Clear();

			if (conf.DisableQuickSettings) {
				return;
			}

			// Only add if the value is not fixed
			if (!conf.Values["CaptureMousepointer"].IsFixed) {
				// For the capture mousecursor option
				ToolStripMenuSelectListItem captureMouseItem = new ToolStripMenuSelectListItem();
				captureMouseItem.Text = Language.GetString("settings_capture_mousepointer");
				captureMouseItem.Checked = conf.CaptureMousepointer;
				captureMouseItem.CheckOnClick = true;
				captureMouseItem.CheckStateChanged += delegate {
					conf.CaptureMousepointer = captureMouseItem.Checked;
				};

				this.contextmenu_quicksettings.DropDownItems.Add(captureMouseItem);
			}
			ToolStripMenuSelectList selectList = null;
			if (!conf.Values["Destinations"].IsFixed) {
				// screenshot destination
				selectList = new ToolStripMenuSelectList("destinations", true);
				selectList.Text = Language.GetString(LangKey.settings_destination);
				// Working with IDestination:
				foreach (IDestination destination in DestinationHelper.GetAllDestinations()) {
					selectList.AddItem(destination.Description, destination, conf.OutputDestinations.Contains(destination.Designation));
				}
				selectList.CheckedChanged += new EventHandler(this.QuickSettingDestinationChanged);
				this.contextmenu_quicksettings.DropDownItems.Add(selectList);
			}

			if (!conf.Values["WindowCaptureMode"].IsFixed) {
				// Capture Modes
				selectList = new ToolStripMenuSelectList("capturemodes", false);
				selectList.Text = Language.GetString(LangKey.settings_window_capture_mode);
				string enumTypeName = typeof(WindowCaptureMode).Name;
				foreach (WindowCaptureMode captureMode in Enum.GetValues(typeof(WindowCaptureMode))) {
					selectList.AddItem(Language.GetString(enumTypeName + "." + captureMode.ToString()), captureMode, conf.WindowCaptureMode == captureMode);
				}
				selectList.CheckedChanged += new EventHandler(this.QuickSettingCaptureModeChanged);
				this.contextmenu_quicksettings.DropDownItems.Add(selectList);
			}

			// print options
			selectList = new ToolStripMenuSelectList("printoptions",true);
			selectList.Text = Language.GetString(LangKey.settings_printoptions);
			
			IniValue iniValue;
			foreach(string propertyName in conf.Values.Keys) {
				if (propertyName.StartsWith("OutputPrint")) {
					iniValue = conf.Values[propertyName];
					if (iniValue.Attributes.LanguageKey != null && !iniValue.IsFixed) {
						selectList.AddItem(Language.GetString(iniValue.Attributes.LanguageKey), iniValue, (bool)iniValue.Value);
					}
				}
			}
			if (selectList.DropDownItems.Count > 0) {
				selectList.CheckedChanged += new EventHandler(this.QuickSettingBoolItemChanged);
				this.contextmenu_quicksettings.DropDownItems.Add(selectList);
			}

			// effects
			selectList = new ToolStripMenuSelectList("effects",true);
			selectList.Text = Language.GetString(LangKey.settings_visualization);

			iniValue = conf.Values["PlayCameraSound"];
			if (!iniValue.IsFixed) {
				selectList.AddItem(Language.GetString(iniValue.Attributes.LanguageKey), iniValue, (bool)iniValue.Value);
			}
			iniValue = conf.Values["ShowTrayNotification"];
			if (!iniValue.IsFixed) {
				selectList.AddItem(Language.GetString(iniValue.Attributes.LanguageKey), iniValue, (bool)iniValue.Value);
			}
			if (selectList.DropDownItems.Count > 0) {
				selectList.CheckedChanged += new EventHandler(this.QuickSettingBoolItemChanged);
				this.contextmenu_quicksettings.DropDownItems.Add(selectList);
			}
		}
		
		void QuickSettingCaptureModeChanged(object sender, EventArgs e) {
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs)e).Item;
			WindowCaptureMode windowsCaptureMode = (WindowCaptureMode)item.Data;
			if (item.Checked) {
				conf.WindowCaptureMode = windowsCaptureMode;
			}
		}

		void QuickSettingBoolItemChanged(object sender, EventArgs e) {
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs)e).Item;
			IniValue iniValue = item.Data as IniValue;
			if (iniValue != null) {
				iniValue.Value = item.Checked;
				IniConfig.Save();
			}
		}

		void QuickSettingDestinationChanged(object sender, EventArgs e) {
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs)e).Item;
			IDestination selectedDestination = (IDestination)item.Data;
			if (item.Checked) {
				if (selectedDestination.Designation.Equals(PickerDestination.DESIGNATION)) {
					// If the item is the destination picker, remove all others
					conf.OutputDestinations.Clear();
				} else {
					// If the item is not the destination picker, remove the picker
					conf.OutputDestinations.Remove(PickerDestination.DESIGNATION);
				}
				// Checked an item, add if the destination is not yet selected
				if (!conf.OutputDestinations.Contains(selectedDestination.Designation)) {
					conf.OutputDestinations.Add(selectedDestination.Designation);
				}
			} else {
				// deselected a destination, only remove if it was selected
				if (conf.OutputDestinations.Contains(selectedDestination.Designation)) {
					conf.OutputDestinations.Remove(selectedDestination.Designation);
				}
			}
			// Check if something was selected, if not make the picker the default
			if (conf.OutputDestinations == null || conf.OutputDestinations.Count == 0) {
				conf.OutputDestinations.Add(PickerDestination.DESIGNATION);
			}
			IniConfig.Save();

			// Rebuild the quick settings menu with the new settings.
			InitializeQuickSettingsMenu();
		}
		#endregion
		
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			Exception exceptionToLog = e.ExceptionObject as Exception;
			string exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
			LOG.Error(EnvironmentInfo.ExceptionToString(exceptionToLog));
			new BugReportForm(exceptionText).ShowDialog();
		}
		
		private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
			Exception exceptionToLog = e.Exception;
			string exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
			LOG.Error(EnvironmentInfo.ExceptionToString(exceptionToLog));
			new BugReportForm(exceptionText).ShowDialog();
		}

		/// <summary>
		/// Handle the notify icon click
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NotifyIconClickTest(object sender, MouseEventArgs e) {
			if (e.Button != MouseButtons.Left) {
				return;
			}
			// The right button will automatically be handled with the context menu, here we only check the left.
			if (conf.DoubleClickAction == ClickActions.DO_NOTHING) {
				// As there isn't a double-click we can start the Left click
				NotifyIconClick(conf.LeftClickAction);
				// ready with the test
				return;
			}
			// If the timer is enabled we are waiting for a double click...
			if (doubleClickTimer.Enabled) {
				// User clicked a second time before the timer tick: Double-click!
				doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
				doubleClickTimer.Stop();
				NotifyIconClick(conf.DoubleClickAction);
			} else {
				// User clicked without a timer, set the timer and if it ticks it was a single click
				// Create timer, if it ticks before the NotifyIconClickTest is called again we have a single click
				doubleClickTimer.Elapsed += NotifyIconSingleClickTest;
				doubleClickTimer.Interval = SystemInformation.DoubleClickTime;
				doubleClickTimer.Start();
			}
		}

		/// <summary>
		/// Called by the doubleClickTimer, this means a single click was used on the tray icon
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NotifyIconSingleClickTest(object sender, EventArgs e) {
			doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
			doubleClickTimer.Stop();
			BeginInvoke((MethodInvoker)delegate {
				NotifyIconClick(conf.LeftClickAction);
			});
		}

		/// <summary>
		/// Handle the notify icon click
		/// </summary>
		private void NotifyIconClick(ClickActions clickAction) {
			switch (clickAction) {
				case ClickActions.OPEN_LAST_IN_EXPLORER:
					string path = null;
					string configPath = FilenameHelper.FillVariables(conf.OutputFilePath, false);
					string lastFilePath = Path.GetDirectoryName(conf.OutputFileAsFullpath);
					if (Directory.Exists(lastFilePath)) {
						path = lastFilePath;
					} else if (Directory.Exists(configPath)) {
						path = configPath;
					}

					try {
						System.Diagnostics.Process.Start(path);
					} catch (Exception ex) {
						// Make sure we show what we tried to open in the exception
						ex.Data.Add("path", path);
						throw;
					}
					break;
				case ClickActions.OPEN_LAST_IN_EDITOR:
					if (File.Exists(conf.OutputFileAsFullpath)) {
						CaptureHelper.CaptureFile(conf.OutputFileAsFullpath, DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
					}
					break;
				case ClickActions.OPEN_SETTINGS:
					ShowSetting();
					break;
				case ClickActions.SHOW_CONTEXT_MENU:
					MethodInfo oMethodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
					oMethodInfo.Invoke(notifyIcon, null);
					break;
				default:
					// Do nothing
					break;
			}
		}

		/// <summary>
		/// The Contextmenu_OpenRecent currently opens the last know save location
		/// </summary>
		private void Contextmenu_OpenRecent(object sender, EventArgs eventArgs) {
			string path = FilenameHelper.FillVariables(conf.OutputFilePath, false);
			// Fix for #1470, problems with a drive which is no longer available
			try {
				string lastFilePath = Path.GetDirectoryName(conf.OutputFileAsFullpath);
				if (Directory.Exists(lastFilePath)) {
					path = lastFilePath;
				} else if (!Directory.Exists(path)) {
					// What do I open when nothing can be found? Right, nothing...
					return;
				}
			} catch (Exception ex) {
				LOG.Warn("Couldn't open the path to the last exported file, taking default.", ex);
			}
			LOG.Debug("DoubleClick was called! Starting: " + path);
			try {
				System.Diagnostics.Process.Start(path);
			} catch (Exception ex) {
				// Make sure we show what we tried to open in the exception
				ex.Data.Add("path", path);
				LOG.Warn("Couldn't open the path to the last exported file", ex);
				// No reason to create a bug-form, we just display the error.
				MessageBox.Show(this, ex.Message, "Opening " + path, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		
		/// <summary>
		/// Shutdown / cleanup
		/// </summary>
		public void Exit() {
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
				PluginHelper.Instance.Shutdown();
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

			ImageOutput.RemoveTmpFiles();

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
			if (conf.MinimizeWorkingSetSize) {
				LOG.Info("Calling EmptyWorkingSet");
				PsAPI.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
			}
			if (UpdateHelper.IsUpdateCheckNeeded()) {
				LOG.Debug("BackgroundWorkerTimerTick checking for update");
				// Start update check in the background
				Thread backgroundTask = new Thread (new ThreadStart(UpdateHelper.CheckAndAskForUpdate));
				backgroundTask.Name = "Update check";
				backgroundTask.IsBackground = true;
				backgroundTask.Start();
			}
		}
	}
}