/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.ComponentModel;
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
using log4net;
using Timer = System.Timers.Timer;

namespace Greenshot {
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : BaseForm {
		private static ILog LOG;
		private static Mutex _applicationMutex;
		private static CoreConfiguration _conf;
		public static string LogFileLocation = null;

		public static void Start(string[] args) {
			bool isAlreadyRunning = false;
			List<string> filesToOpen = new List<string>();

			// Set the Thread name, is better than "1"
			Thread.CurrentThread.Name = Application.ProductName;
			
			// Init Log4NET
			LogFileLocation = LogHelper.InitializeLog4NET();
			// Get logger
			LOG = LogManager.GetLogger(typeof(MainForm));

			Application.ThreadException += Application_ThreadException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			// Initialize the IniConfig
			IniConfig.Init();

			// Log the startup
			LOG.Info("Starting: " + EnvironmentInfo.EnvironmentToString(false));

			// Read configuration
			_conf = IniConfig.GetIniSection<CoreConfiguration>();
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

					bool created;
					// 1) Create Mutex
					_applicationMutex = new Mutex(false, @"Local\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08", out created, mutexsecurity);
					// 2) Get the right to it, this returns false if it's already locked
					if (!_applicationMutex.WaitOne(0, false)) {
						LOG.Debug("Greenshot seems already to be running!");
						isAlreadyRunning = true;
						// Clean up
						_applicationMutex.Close();
						_applicationMutex = null;
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
					LOG.Debug("Greenshot arguments: " + argumentString);
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
						helpOutput.AppendLine("\t/inidirectory [directory]");
						helpOutput.AppendLine("\t\tSet the directory where the greenshot.ini should be stored & read.");
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
						_conf.Language = args[++argumentNr];
						IniConfig.Save();
						continue;
					}

					// Setting the INI-directory
					if (argument.ToLower().Equals("/inidirectory")) {
						IniConfig.IniDirectory = args[++argumentNr];
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
						int currentProcessId;
						using (Process currentProcess = Process.GetCurrentProcess()) {
							currentProcessId = currentProcess.Id;
						}
						foreach (Process greenshotProcess in Process.GetProcessesByName("greenshot")) {
							try {
								instanceInfo.Append(index++ + ": ").AppendLine(Kernel32.GetProcessPath(greenshotProcess.Id));
								if (currentProcessId == greenshotProcess.Id) {
									matchedThisProcess = true;
								}
							} catch (Exception ex) {
								LOG.Debug(ex);
							}
							greenshotProcess.Dispose();
						}
						if (!matchedThisProcess) {
							using (Process currentProcess = Process.GetCurrentProcess()) {
								instanceInfo.Append(index + ": ").AppendLine(Kernel32.GetProcessPath(currentProcess.Id));
							}
						}

						// A dirty fix to make sure the messagebox is visible as a Greenshot window on the taskbar
						using (Form dummyForm = new Form()) {
							dummyForm.Icon = GreenshotResources.getGreenshotIcon();
							dummyForm.ShowInTaskbar = true;
							dummyForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
							dummyForm.Location = new Point(int.MinValue, int.MinValue);
							dummyForm.Load += delegate { dummyForm.Size = Size.Empty; };
							dummyForm.Show();
							MessageBox.Show(dummyForm, Language.GetString(LangKey.error_multipleinstances) + "\r\n" + instanceInfo, Language.GetString(LangKey.error), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						}
					}
					FreeMutex();
					Application.Exit();
					return;
				}

				// BUG-1809: Add message filter, to filter out all the InputLangChanged messages which go to a target control with a handle > 32 bit.
				Application.AddMessageFilter(new WmInputLangChangeRequestFilter());

				// From here on we continue starting Greenshot
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				// if language is not set, show language dialog
				if(string.IsNullOrEmpty(_conf.Language)) {
					LanguageDialog languageDialog = LanguageDialog.GetInstance();
					languageDialog.ShowDialog();
					_conf.Language = languageDialog.SelectedLanguage;
					IniConfig.Save();
				}

				// Check if it's the first time launch?
				if(_conf.IsFirstLaunch) {
					_conf.IsFirstLaunch = false;
					IniConfig.Save();
					transport.AddCommand(CommandEnum.FirstLaunch);
				}
				// Should fix BUG-1633
				Application.DoEvents();
				_instance = new MainForm(transport);
				Application.Run();
			} catch(Exception ex) {
				LOG.Error("Exception in startup.", ex);
				Application_ThreadException(ActiveForm, new ThreadExceptionEventArgs(ex));
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
			if (_applicationMutex != null) {
				try {
					_applicationMutex.ReleaseMutex();
					_applicationMutex = null;
				} catch (Exception ex) {
					LOG.Error("Error releasing Mutex!", ex);
				}
			}
		}

		private static MainForm _instance;
		public static MainForm Instance {
			get {
				return _instance;
			}
		}

		private readonly CopyData _copyData;
		
		// Thumbnail preview
		private ThumbnailForm _thumbnailForm;
		// Make sure we have only one settings form
		private SettingsForm _settingsForm;
		// Make sure we have only one about form
		private AboutForm _aboutForm;
		// Timer for the double click test
		private readonly Timer _doubleClickTimer = new Timer();

		public NotifyIcon NotifyIcon {
			get {
				return notifyIcon;				
			}
		}

		public MainForm(CopyDataTransport dataTransport) {
			_instance = this;

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
			notifyIcon.Icon = GreenshotResources.getGreenshotIcon();

			// Disable access to the settings, for feature #3521446
			contextmenu_settings.Visible = !_conf.DisableSettings;

			// Make sure all hotkeys pass this window!
			HotkeyControl.RegisterHotkeyHWND(Handle);
			RegisterHotkeys();
			
			new ToolTip();
			
			UpdateUI();
			
			// This forces the registration of all destinations inside Greenshot itself.
			DestinationHelper.GetAllDestinations();
			// This forces the registration of all processors inside Greenshot itself.
			ProcessorHelper.GetAllProcessors();
			
			// Load all the plugins
			PluginHelper.Instance.LoadPlugins();

			// Check destinations, remove all that don't exist
			foreach(string destination in _conf.OutputDestinations.ToArray()) {
				if (DestinationHelper.GetDestination(destination) == null) {
					_conf.OutputDestinations.Remove(destination);
				}
			}

			// we should have at least one!
			if (_conf.OutputDestinations.Count == 0) {
				_conf.OutputDestinations.Add(EditorDestination.DESIGNATION);
			}
			if (_conf.DisableQuickSettings) {
				contextmenu_quicksettings.Visible = false;
			} else {
				// Do after all plugins & finding the destination, otherwise they are missing!
				InitializeQuickSettingsMenu();
			}
			SoundHelper.Initialize();

			coreConfiguration.PropertyChanged += OnIconSizeChanged;
			OnIconSizeChanged(this, new PropertyChangedEventArgs("IconSize"));

			// Set the Greenshot icon visibility depending on the configuration. (Added for feature #3521446)
			// Setting it to true this late prevents Problems with the context menu
			notifyIcon.Visible = !_conf.HideTrayicon;

			// Make sure we never capture the mainform
			WindowDetails.RegisterIgnoreHandle(Handle);

			// Create a new instance of the class: copyData = new CopyData();
			_copyData = new CopyData();

			// Assign the handle:
			_copyData.AssignHandle(Handle);
			// Create the channel to send on:
			_copyData.Channels.Add("Greenshot");     
			// Hook up received event:
			_copyData.CopyDataReceived += CopyDataDataReceived;

			if (dataTransport != null) {
				HandleDataTransport(dataTransport);
			}
			// Make Greenshot use less memory after startup
			if (_conf.MinimizeWorkingSetSize) {
				PsAPI.EmptyWorkingSet();
			}
		}

		/// <summary>
		/// DataReceivedEventHandler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="copyDataReceivedEventArgs"></param>
		private void CopyDataDataReceived(object sender, CopyDataReceivedEventArgs copyDataReceivedEventArgs) {
			// Cast the data to the type of object we sent:
			var dataTransport = (CopyDataTransport)copyDataReceivedEventArgs.Data;
			HandleDataTransport(dataTransport);
		}

		private void BalloonTipClicked(object sender, EventArgs e) {
			try {
				ShowSetting();
			} finally {
				BalloonTipClosed(sender, e);
			}
		}

		private void BalloonTipClosed(object sender, EventArgs e) {
			notifyIcon.BalloonTipClicked -= BalloonTipClicked;
			notifyIcon.BalloonTipClosed -= BalloonTipClosed;
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
							notifyIcon.BalloonTipClicked += BalloonTipClicked;
							notifyIcon.BalloonTipClosed += BalloonTipClosed;
							notifyIcon.ShowBalloonTip(2000, "Greenshot", Language.GetFormattedString(LangKey.tooltip_firststart, HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.RegionHotkey)), ToolTipIcon.Info);
						} catch (Exception ex) {
							LOG.Warn("Exception while showing first launch: ", ex);
						}
						break;
					case CommandEnum.ReloadConfig:
						LOG.Info("Reload requested");
						try {
							IniConfig.Reload();
							Invoke((MethodInvoker) delegate {
								// Even update language when needed
								UpdateUI();
								// Update the hotkey
								// Make sure the current hotkeys are disabled
								HotkeyControl.UnregisterHotkeys();
								RegisterHotkeys();
							});
						} catch (Exception ex) {
							LOG.Warn("Exception while reloading configuration: ", ex);
						}
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
		
		public ContextMenuStrip MainMenu {
			get {return contextMenu;}
		}

		protected override void WndProc(ref Message m) {
			if (HotkeyControl.HandleMessages(ref m)) {
				return;
			}
			// BUG-1809 prevention, filter the InputLangChange messages
			if (WmInputLangChangeRequestFilter.PreFilterMessageExternal(ref m))
			{
				return;
			}
            base.WndProc(ref m);
		}

		#region hotkeys

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
				}
				LOG.DebugFormat("Registered {0} to hotkey: {1}", functionName, hotkeyString);
			} else {
				LOG.InfoFormat("Skipping hotkey registration for {0}, no hotkey set!", functionName);
			}
			return true;
		}

		private static bool RegisterWrapper(StringBuilder failedKeys, string functionName, string configurationKey, HotKeyHandler handler, bool ignoreFailedRegistration) {
			IniValue hotkeyValue = _conf.Values[configurationKey];
			try {
				bool success = RegisterHotkey(failedKeys, functionName, hotkeyValue.Value.ToString(), handler);
				if (!success && ignoreFailedRegistration) {
					LOG.DebugFormat("Ignoring failed hotkey registration for {0}, with value '{1}', resetting to 'None'.", functionName, hotkeyValue);
					_conf.Values[configurationKey].Value = Keys.None.ToString();
					_conf.IsDirty = true;
				}
				return success;
			} catch (Exception ex) {
				LOG.Warn(ex);
				LOG.WarnFormat("Restoring default hotkey for {0}, stored under {1} from '{2}' to '{3}'", functionName, configurationKey, hotkeyValue.Value, hotkeyValue.Attributes.DefaultValue);
				// when getting an exception the key wasn't found: reset the hotkey value
				hotkeyValue.UseValueOrDefault(null);
				hotkeyValue.ContainingIniSection.IsDirty = true;
				return RegisterHotkey(failedKeys, functionName, hotkeyValue.Value.ToString(), handler);
			}
		}

		/// <summary>
		/// Fix icon reference
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnIconSizeChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IconSize") {
				contextMenu.ImageScalingSize = coreConfiguration.IconSize;
				string ieExePath = PluginUtils.GetExePath("iexplore.exe");
				if (!string.IsNullOrEmpty(ieExePath)) {
					this.contextmenu_captureie.Image = PluginUtils.GetCachedExeIcon(ieExePath, 0);
				}
			}
		}

		/// <summary>
		/// Registers all hotkeys as configured, displaying a dialog in case of hotkey conflicts with other tools.
		/// </summary>
		/// <returns>Whether the hotkeys could be registered to the users content. This also applies if conflicts arise and the user decides to ignore these (i.e. not to register the conflicting hotkey).</returns>
		public static bool RegisterHotkeys() {
			return RegisterHotkeys(false);
		}

		/// <summary>
		/// Registers all hotkeys as configured, displaying a dialog in case of hotkey conflicts with other tools.
		/// </summary>
		/// <param name="ignoreFailedRegistration">if true, a failed hotkey registration will not be reported to the user - the hotkey will simply not be registered</param>
		/// <returns>Whether the hotkeys could be registered to the users content. This also applies if conflicts arise and the user decides to ignore these (i.e. not to register the conflicting hotkey).</returns>
		private static bool RegisterHotkeys(bool ignoreFailedRegistration) {
			if (_instance == null) {
				return false;
			}
			bool success = true;
			StringBuilder failedKeys = new StringBuilder();

			if (!RegisterWrapper(failedKeys, "CaptureRegion", "RegionHotkey", _instance.CaptureRegion, ignoreFailedRegistration)) {
				success = false;
			}
			if (!RegisterWrapper(failedKeys, "CaptureWindow", "WindowHotkey", _instance.CaptureWindow, ignoreFailedRegistration)) {
				success = false;
			}
			if (!RegisterWrapper(failedKeys, "CaptureFullScreen", "FullscreenHotkey", _instance.CaptureFullScreen, ignoreFailedRegistration)) {
				success = false;
			}
			if (!RegisterWrapper(failedKeys, "CaptureLastRegion", "LastregionHotkey", _instance.CaptureLastRegion, ignoreFailedRegistration)) {
				success = false;
			}
			if (_conf.IECapture) {
				if (!RegisterWrapper(failedKeys, "CaptureIE", "IEHotkey", _instance.CaptureIE, ignoreFailedRegistration)) {
					success = false;
				}
			}

			if (!success) {
				if (!ignoreFailedRegistration) {
					success = HandleFailedHotkeyRegistration(failedKeys.ToString());
				} else {
					// if failures have been ignored, the config has probably been updated
					if (_conf.IsDirty) {
						IniConfig.Save();
					}
				}
			}
			return success || ignoreFailedRegistration;
		}

		/// <summary>
		/// Displays a dialog for the user to choose how to handle hotkey registration failures: 
		/// retry (allowing to shut down the conflicting application before),
		/// ignore (not registering the conflicting hotkey and resetting the respective config to "None", i.e. not trying to register it again on next startup)
		/// abort (do nothing about it)
		/// </summary>
		/// <param name="failedKeys">comma separated list of the hotkeys that could not be registered, for display in dialog text</param>
		/// <returns></returns>
		private static bool HandleFailedHotkeyRegistration(string failedKeys) {
			bool success = false;
			DialogResult dr = MessageBox.Show(Instance, Language.GetFormattedString(LangKey.warning_hotkeys, failedKeys), Language.GetString(LangKey.warning), MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Exclamation);
			if (dr == DialogResult.Retry) {
				LOG.DebugFormat("Re-trying to register hotkeys");
				HotkeyControl.UnregisterHotkeys();
				success = RegisterHotkeys(false);
			} else if (dr == DialogResult.Ignore)  {
				LOG.DebugFormat("Ignoring failed hotkey registration");
				HotkeyControl.UnregisterHotkeys();
				success = RegisterHotkeys(true);
			}
			return success;
		}
		#endregion
		
		public void UpdateUI() {
			// As the form is never loaded, call ApplyLanguage ourselves
			ApplyLanguage();

			// Show hotkeys in Contextmenu
			contextmenu_capturearea.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.RegionHotkey);
			contextmenu_capturelastregion.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.LastregionHotkey);
			contextmenu_capturewindow.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.WindowHotkey);
			contextmenu_capturefullscreen.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.FullscreenHotkey);
			contextmenu_captureie.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.IEHotkey);
		}
		
		
		#region mainform events
		void MainFormFormClosing(object sender, FormClosingEventArgs e) {
			LOG.DebugFormat("Mainform closing, reason: {0}", e.CloseReason);
			_instance = null;
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

		void CaptureFile() {
			var openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image files (*.greenshot, *.png, *.jpg, *.gif, *.bmp, *.ico, *.tiff, *.wmf)|*.greenshot; *.png; *.jpg; *.jpeg; *.gif; *.bmp; *.ico; *.tiff; *.tif; *.wmf";
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				if (File.Exists(openFileDialog.FileName)) {
					CaptureHelper.CaptureFile(openFileDialog.FileName);
				}
			}
		}

		void CaptureFullScreen() {
			CaptureHelper.CaptureFullscreen(true, _conf.ScreenCaptureMode);
		}

		void CaptureLastRegion() {
			CaptureHelper.CaptureLastRegion(true);
		}

		void CaptureIE() {
			if (_conf.IECapture) {
				CaptureHelper.CaptureIE(true, null);
			}
		}

		void CaptureWindow() {
			if (_conf.CaptureWindowsInteractive) {
				CaptureHelper.CaptureWindowInteractive(true);
			} else {
				CaptureHelper.CaptureWindow(true);
			}
		}
		#endregion


		#region contextmenu
		void ContextMenuOpening(object sender, CancelEventArgs e)	{
			contextmenu_captureclipboard.Enabled = ClipboardHelper.ContainsImage();
			contextmenu_capturelastregion.Enabled = coreConfiguration.LastCapturedRegion != Rectangle.Empty;

			// IE context menu code
			try {
				if (_conf.IECapture && IECaptureHelper.IsIERunning()) {
					contextmenu_captureie.Enabled = true;
					contextmenu_captureiefromlist.Enabled = true;
				} else {
					contextmenu_captureie.Enabled = false;
					contextmenu_captureiefromlist.Enabled = false;
				}
			} catch (Exception ex) {
				LOG.WarnFormat("Problem accessing IE information: {0}", ex.Message);
			}

			// Multi-Screen captures
			contextmenu_capturefullscreen.Click -= CaptureFullScreenToolStripMenuItemClick;
			contextmenu_capturefullscreen.DropDownOpening -= MultiScreenDropDownOpening;
			contextmenu_capturefullscreen.DropDownClosed -= MultiScreenDropDownClosing;
			if (Screen.AllScreens.Length > 1) {
				contextmenu_capturefullscreen.DropDownOpening += MultiScreenDropDownOpening;
				contextmenu_capturefullscreen.DropDownClosed += MultiScreenDropDownClosing;
			} else {
				contextmenu_capturefullscreen.Click += CaptureFullScreenToolStripMenuItemClick;
			}

			var now = DateTime.Now;
			if ((now.Month == 12 && now.Day > 19 && now.Day < 27) || // christmas
			    (now.Month ==  3 && now.Day > 13 && now.Day < 21)) { // birthday
				var resources = new ComponentResourceManager(typeof(MainForm));
					contextmenu_donate.Image = (Image)resources.GetObject("contextmenu_present.Image");
			}
		}
		
		void ContextMenuClosing(object sender, EventArgs e) {
			contextmenu_captureiefromlist.DropDownItems.Clear();
			contextmenu_capturewindowfromlist.DropDownItems.Clear();
			CleanupThumbnail();
		}
		
		/// <summary>
		/// Build a selectable list of IE tabs when we enter the menu item
		/// </summary>
		void CaptureIEMenuDropDownOpening(object sender, EventArgs e) {
			if (!_conf.IECapture) {
				return;
			}
			try {
				List<KeyValuePair<WindowDetails, string>> tabs = IECaptureHelper.GetBrowserTabs();
				contextmenu_captureiefromlist.DropDownItems.Clear();
				if (tabs.Count > 0) {
					contextmenu_captureie.Enabled = true;
					contextmenu_captureiefromlist.Enabled = true;
					Dictionary<WindowDetails, int> counter = new Dictionary<WindowDetails, int>();
					
					foreach(KeyValuePair<WindowDetails, string> tabData in tabs) {
						string title = tabData.Value;
						if (title == null) {
							continue;
						}
						if (title.Length > _conf.MaxMenuItemLength) {
							title = title.Substring(0, Math.Min(title.Length, _conf.MaxMenuItemLength));
						}
						ToolStripItem captureIETabItem = contextmenu_captureiefromlist.DropDownItems.Add(title);
						int index = counter.ContainsKey(tabData.Key) ? counter[tabData.Key] : 0;
						captureIETabItem.Image = tabData.Key.DisplayIcon;
						captureIETabItem.Tag = new KeyValuePair<WindowDetails, int>(tabData.Key, index++);
						captureIETabItem.Click += Contextmenu_captureiefromlist_Click;
						contextmenu_captureiefromlist.DropDownItems.Add(captureIETabItem);
						if (counter.ContainsKey(tabData.Key)) {
							counter[tabData.Key] = index;
						} else {
							counter.Add(tabData.Key, index);
						}
					}
				} else {
					contextmenu_captureie.Enabled = false;
					contextmenu_captureiefromlist.Enabled = false;
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

				captureScreenItem = new ToolStripMenuItem(Language.GetString(LangKey.contextmenu_capturefullscreen_all));
				captureScreenItem.Click += delegate {
					BeginInvoke((MethodInvoker)delegate {
						CaptureHelper.CaptureFullscreen(false, ScreenCaptureMode.FullScreen);
					});
				};
				captureScreenMenuItem.DropDownItems.Add(captureScreenItem);
				foreach (Screen screen in Screen.AllScreens) {
					Screen screenToCapture = screen;
					string deviceAlignment = "";
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
                    captureScreenItem = new ToolStripMenuItem(deviceAlignment);
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
			CleanupThumbnail();
		}
		
		private void ShowThumbnailOnEnter(object sender, EventArgs e) {
			ToolStripMenuItem captureWindowItem = sender as ToolStripMenuItem;
			if (captureWindowItem != null) {
				WindowDetails window = captureWindowItem.Tag as WindowDetails;
				if (_thumbnailForm == null) {
					_thumbnailForm = new ThumbnailForm();
				}
				_thumbnailForm.ShowThumbnail(window, captureWindowItem.GetCurrentParent().TopLevelControl);
			}
		}

		private void HideThumbnailOnLeave(object sender, EventArgs e) {
			if (_thumbnailForm != null) {
				_thumbnailForm.Hide();
			}
		}
		
		private void CleanupThumbnail() {
			if (_thumbnailForm != null) {
				_thumbnailForm.Close();
				_thumbnailForm = null;
			}
		}

		public void AddCaptureWindowMenuItems(ToolStripMenuItem menuItem, EventHandler eventHandler) {
			menuItem.DropDownItems.Clear();
			// check if thumbnailPreview is enabled and DWM is enabled
			bool thumbnailPreview = _conf.ThumnailPreview && DWM.isDWMEnabled();

			List<WindowDetails> windows = WindowDetails.GetTopLevelWindows();
			foreach(WindowDetails window in windows) {

				string title = window.Text;
				if (title != null) {
					if (title.Length > _conf.MaxMenuItemLength) {
						title = title.Substring(0, Math.Min(title.Length, _conf.MaxMenuItemLength));
					}
					ToolStripItem captureWindowItem = menuItem.DropDownItems.Add(title);
					captureWindowItem.Tag = window;
					captureWindowItem.Image = window.DisplayIcon;
					captureWindowItem.Click += eventHandler;
					// Only show preview when enabled
					if (thumbnailPreview) {
						captureWindowItem.MouseEnter += ShowThumbnailOnEnter;
						captureWindowItem.MouseLeave += HideThumbnailOnLeave;
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
				CaptureHelper.CaptureFullscreen(false, _conf.ScreenCaptureMode);
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
			if (!_conf.IECapture) {
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
			if (_settingsForm != null) {
				WindowDetails.ToForeground(_settingsForm.Handle);
			} else {
				try {
					using (_settingsForm = new SettingsForm()) {
						if (_settingsForm.ShowDialog() == DialogResult.OK) {
							InitializeQuickSettingsMenu();
						}
					}
				} finally {
					_settingsForm = null;
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
			if (_aboutForm != null) {
				WindowDetails.ToForeground(_aboutForm.Handle);
			} else {
				try {
					using (_aboutForm = new AboutForm()) {
						_aboutForm.ShowDialog(this);
					}
				} finally {
					_aboutForm = null;
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

		private void CheckStateChangedHandler(object sender, EventArgs e) {
			ToolStripMenuSelectListItem captureMouseItem = sender as ToolStripMenuSelectListItem;
			if (captureMouseItem != null) {
				_conf.CaptureMousepointer = captureMouseItem.Checked;
			}
		}
		
		/// <summary>
		/// This needs to be called to initialize the quick settings menu entries
		/// </summary>
		private void InitializeQuickSettingsMenu() {
			contextmenu_quicksettings.DropDownItems.Clear();

			if (_conf.DisableQuickSettings) {
				return;
			}

			// Only add if the value is not fixed
			if (!_conf.Values["CaptureMousepointer"].IsFixed) {
				// For the capture mousecursor option
				ToolStripMenuSelectListItem captureMouseItem = new ToolStripMenuSelectListItem();
				captureMouseItem.Text = Language.GetString("settings_capture_mousepointer");
				captureMouseItem.Checked = _conf.CaptureMousepointer;
				captureMouseItem.CheckOnClick = true;
				captureMouseItem.CheckStateChanged += CheckStateChangedHandler;

				contextmenu_quicksettings.DropDownItems.Add(captureMouseItem);
			}
			ToolStripMenuSelectList selectList;
			if (!_conf.Values["Destinations"].IsFixed) {
				// screenshot destination
				selectList = new ToolStripMenuSelectList("destinations", true);
				selectList.Text = Language.GetString(LangKey.settings_destination);
				// Working with IDestination:
				foreach (IDestination destination in DestinationHelper.GetAllDestinations()) {
					selectList.AddItem(destination.Description, destination, _conf.OutputDestinations.Contains(destination.Designation));
				}
				selectList.CheckedChanged += QuickSettingDestinationChanged;
				contextmenu_quicksettings.DropDownItems.Add(selectList);
			}

			if (!_conf.Values["WindowCaptureMode"].IsFixed) {
				// Capture Modes
				selectList = new ToolStripMenuSelectList("capturemodes", false);
				selectList.Text = Language.GetString(LangKey.settings_window_capture_mode);
				string enumTypeName = typeof(WindowCaptureMode).Name;
				foreach (WindowCaptureMode captureMode in Enum.GetValues(typeof(WindowCaptureMode))) {
					selectList.AddItem(Language.GetString(enumTypeName + "." + captureMode.ToString()), captureMode, _conf.WindowCaptureMode == captureMode);
				}
				selectList.CheckedChanged += QuickSettingCaptureModeChanged;
				contextmenu_quicksettings.DropDownItems.Add(selectList);
			}

			// print options
			selectList = new ToolStripMenuSelectList("printoptions",true);
			selectList.Text = Language.GetString(LangKey.settings_printoptions);
			
			IniValue iniValue;
			foreach(string propertyName in _conf.Values.Keys) {
				if (propertyName.StartsWith("OutputPrint")) {
					iniValue = _conf.Values[propertyName];
					if (iniValue.Attributes.LanguageKey != null && !iniValue.IsFixed) {
						selectList.AddItem(Language.GetString(iniValue.Attributes.LanguageKey), iniValue, (bool)iniValue.Value);
					}
				}
			}
			if (selectList.DropDownItems.Count > 0) {
				selectList.CheckedChanged += QuickSettingBoolItemChanged;
				contextmenu_quicksettings.DropDownItems.Add(selectList);
			}

			// effects
			selectList = new ToolStripMenuSelectList("effects",true);
			selectList.Text = Language.GetString(LangKey.settings_visualization);

			iniValue = _conf.Values["PlayCameraSound"];
			if (!iniValue.IsFixed) {
				selectList.AddItem(Language.GetString(iniValue.Attributes.LanguageKey), iniValue, (bool)iniValue.Value);
			}
			iniValue = _conf.Values["ShowTrayNotification"];
			if (!iniValue.IsFixed) {
				selectList.AddItem(Language.GetString(iniValue.Attributes.LanguageKey), iniValue, (bool)iniValue.Value);
			}
			if (selectList.DropDownItems.Count > 0) {
				selectList.CheckedChanged += QuickSettingBoolItemChanged;
				contextmenu_quicksettings.DropDownItems.Add(selectList);
			}
		}
		
		void QuickSettingCaptureModeChanged(object sender, EventArgs e) {
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs)e).Item;
			WindowCaptureMode windowsCaptureMode = (WindowCaptureMode)item.Data;
			if (item.Checked) {
				_conf.WindowCaptureMode = windowsCaptureMode;
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
					_conf.OutputDestinations.Clear();
				} else {
					// If the item is not the destination picker, remove the picker
					_conf.OutputDestinations.Remove(PickerDestination.DESIGNATION);
				}
				// Checked an item, add if the destination is not yet selected
				if (!_conf.OutputDestinations.Contains(selectedDestination.Designation)) {
					_conf.OutputDestinations.Add(selectedDestination.Designation);
				}
			} else {
				// deselected a destination, only remove if it was selected
				if (_conf.OutputDestinations.Contains(selectedDestination.Designation)) {
					_conf.OutputDestinations.Remove(selectedDestination.Designation);
				}
			}
			// Check if something was selected, if not make the picker the default
			if (_conf.OutputDestinations == null || _conf.OutputDestinations.Count == 0) {
				_conf.OutputDestinations.Add(PickerDestination.DESIGNATION);
			}
			IniConfig.Save();

			// Rebuild the quick settings menu with the new settings.
			InitializeQuickSettingsMenu();
		}
		#endregion
		
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			Exception exceptionToLog = e.ExceptionObject as Exception;
			string exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
			LOG.Error("Exception caught in the UnhandledException handler.");
			LOG.Error(exceptionText);
			if (exceptionText != null && exceptionText.Contains("InputLanguageChangedEventArgs"))
            {
				// Ignore for BUG-1809
				return;
			}
			new BugReportForm(exceptionText).ShowDialog();
		}
		
		private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
			Exception exceptionToLog = e.Exception;
			string exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
			LOG.Error("Exception caught in the ThreadException handler.");
            LOG.Error(exceptionText);
			if (exceptionText != null && exceptionText.Contains("InputLanguageChangedEventArgs"))
			{
				// Ignore for BUG-1809
				return;
			}

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
			if (_conf.DoubleClickAction == ClickActions.DO_NOTHING) {
				// As there isn't a double-click we can start the Left click
				NotifyIconClick(_conf.LeftClickAction);
				// ready with the test
				return;
			}
			// If the timer is enabled we are waiting for a double click...
			if (_doubleClickTimer.Enabled) {
				// User clicked a second time before the timer tick: Double-click!
				_doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
				_doubleClickTimer.Stop();
				NotifyIconClick(_conf.DoubleClickAction);
			} else {
				// User clicked without a timer, set the timer and if it ticks it was a single click
				// Create timer, if it ticks before the NotifyIconClickTest is called again we have a single click
				_doubleClickTimer.Elapsed += NotifyIconSingleClickTest;
				_doubleClickTimer.Interval = SystemInformation.DoubleClickTime;
				_doubleClickTimer.Start();
			}
		}

		/// <summary>
		/// Called by the doubleClickTimer, this means a single click was used on the tray icon
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NotifyIconSingleClickTest(object sender, EventArgs e) {
			_doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
			_doubleClickTimer.Stop();
			BeginInvoke((MethodInvoker)delegate {
				NotifyIconClick(_conf.LeftClickAction);
			});
		}

		/// <summary>
		/// Handle the notify icon click
		/// </summary>
		private void NotifyIconClick(ClickActions clickAction) {
			switch (clickAction) {
				case ClickActions.OPEN_LAST_IN_EXPLORER:
					string path = null;
                    if (!string.IsNullOrEmpty(_conf.OutputFileAsFullpath)) {
                        string lastFilePath = Path.GetDirectoryName(_conf.OutputFileAsFullpath);
                        if (!string.IsNullOrEmpty(lastFilePath) && Directory.Exists(lastFilePath)) {
                            path = lastFilePath;
					    }
                    }
                    if (path == null) {
                        string configPath = FilenameHelper.FillVariables(_conf.OutputFilePath, false);
                        if (Directory.Exists(configPath)) {
                            path = configPath;
                        }
                    }

					if (path != null) {
						try {
							using (Process.Start(path)) {}
						} catch (Exception ex) {
							// Make sure we show what we tried to open in the exception
							ex.Data.Add("path", path);
							throw;
						}
					}
					break;
				case ClickActions.OPEN_LAST_IN_EDITOR:
					if (File.Exists(_conf.OutputFileAsFullpath)) {
						CaptureHelper.CaptureFile(_conf.OutputFileAsFullpath, DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
					}
					break;
				case ClickActions.OPEN_SETTINGS:
					ShowSetting();
					break;
				case ClickActions.SHOW_CONTEXT_MENU:
					MethodInfo oMethodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
					oMethodInfo.Invoke(notifyIcon, null);
					break;
			}
		}

		/// <summary>
		/// The Contextmenu_OpenRecent currently opens the last know save location
		/// </summary>
		private void Contextmenu_OpenRecent(object sender, EventArgs eventArgs) {
			string path = FilenameHelper.FillVariables(_conf.OutputFilePath, false);
			// Fix for #1470, problems with a drive which is no longer available
			try {
				string lastFilePath = Path.GetDirectoryName(_conf.OutputFileAsFullpath);

				if (lastFilePath != null && Directory.Exists(lastFilePath)) {
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
				Process.Start(path);
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
				if (form.Handle != Handle && !form.GetType().Equals(typeof(ImageEditorForm))) {
					formsToClose.Add(form);
				}
			}
			foreach(Form form in formsToClose) {
				try {
					LOG.InfoFormat("Closing form: {0}", form.Name);
					Form formCapturedVariable = form;
					Invoke((MethodInvoker)delegate { formCapturedVariable.Close(); });
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
			if (_conf.MinimizeWorkingSetSize) {
				PsAPI.EmptyWorkingSet();
			}
			if (UpdateHelper.IsUpdateCheckNeeded()) {
				LOG.Debug("BackgroundWorkerTimerTick checking for update");
				// Start update check in the background
				Thread backgroundTask = new Thread (UpdateHelper.CheckAndAskForUpdate);
				backgroundTask.Name = "Update check";
				backgroundTask.IsBackground = true;
				backgroundTask.Start();
			}
		}
	}
}