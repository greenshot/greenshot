/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015  Thomas Braun, Jens Klingen, Robin Krom
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

using Dapplo.Addons.Implementation;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Config.Support;
using Dapplo.Windows.Native;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.Windows;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Extensions;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace Greenshot.Forms
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : BaseForm
	{
		private static ILog LOG;
		private const string ApplicationName = "Greenshot";
		private const string MutexId = @"Local\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08";
		private static Mutex _applicationMutex;
		public static string LogFileLocation = null;
		private static readonly ApplicationBootstrapper ApplicationBootstrapper = new ApplicationBootstrapper(ApplicationName);


		public static void Start(string[] args)
		{
			// Set the Thread name, is better than "1"
			Thread.CurrentThread.Name = Application.ProductName;

			// Read arguments
			var arguments = new Arguments(args);
			// Don't continue if the Help was requested
			if (arguments.IsHelp)
			{
				return;
			}

			Application.ThreadException += Application_ThreadException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			// Setting the INI-directory
			string iniDirectory = null;
			// Specified the ini directory directly
			if (!string.IsNullOrWhiteSpace(arguments.IniDirectory))
			{
				iniDirectory = arguments.IniDirectory;
			}
			// Portable mode
			if (PortableHelper.IsPortable)
			{
				iniDirectory = PortableHelper.PortableIniFileLocation;
			}

			// Initialize the string encryption, TODO: Move "credentials" to build server / yaml
			Dapplo.Config.Converters.StringEncryptionTypeConverter.RgbIv = "dlgjowejgogkklwj";
			Dapplo.Config.Converters.StringEncryptionTypeConverter.RgbKey = "lsjvkwhvwujkagfauguwcsjgu2wueuff";
			iniConfig = new IniConfig(ApplicationName, ApplicationName, iniDirectory);
			// Register method to fix some values
			iniConfig.AfterLoad<ICoreConfiguration>(CoreConfigurationChecker.AfterLoad);

			// Init Log4NET
			LogFileLocation = LogHelper.InitializeLog4NET();
			// Get logger
			LOG = LogManager.GetLogger(typeof (MainForm));

			Task.Run(async () =>
			{
				coreConfiguration = await iniConfig.RegisterAndGetAsync<ICoreConfiguration>();
				var languageLoader = new LanguageLoader(ApplicationName, coreConfiguration.Language ?? "en-US");
				ApplicationBootstrapper.LanguageLoader = languageLoader;
				ApplicationBootstrapper.IniConfig = iniConfig;
				// Read configuration & languages
				languageLoader.CorrectMissingTranslations();
				language = await LanguageLoader.Current.RegisterAndGetAsync<IGreenshotLanguage>();
				await iniConfig.RegisterAndGetAsync<INetworkConfiguration>();
			}).Wait();

			// Log the startup
			LOG.Info("Starting: " + EnvironmentInfo.EnvironmentToString(false));

			try
			{
				// Fix for Bug 2495900, Multi-user Environment
				// check whether there's an local instance running already
				if (!LockAppMutex())
				{
					// Other instance is running, call a Greenshot client or exit etc

					if (arguments.FilesToOpen.Count > 0)
					{
						GreenshotClient.OpenFiles(arguments.FilesToOpen);
					}

					if (arguments.IsExit)
					{
						GreenshotClient.Exit();
					}

					if (arguments.IsReload)
					{
						GreenshotClient.ReloadConfig();
					}

					ShowOtherInstances();
					FreeMutex();
					return;
				}

				if (!string.IsNullOrWhiteSpace(arguments.Language))
				{
					// Set language
					coreConfiguration.Language = arguments.Language;
				}

				// From here on we continue starting Greenshot
				Application.EnableVisualStyles();
				// BUG-1809: Add message filter, to filter out all the InputLangChanged messages which go to a target control with a handle > 32 bit.
				Application.AddMessageFilter(new WmInputLangChangeRequestFilter());
				Application.SetCompatibleTextRenderingDefault(false);

				// if language is not set, show language dialog
				if (string.IsNullOrEmpty(coreConfiguration.Language))
				{
					LanguageDialog languageDialog = LanguageDialog.GetInstance();
					languageDialog.ShowDialog();
					coreConfiguration.Language = languageDialog.SelectedLanguage;
				}

				// Should fix BUG-1633
				Application.DoEvents();
				_instance = new MainForm(arguments);
				Application.Run();
			}
			catch (Exception ex)
			{
				LOG.Error("Exception in startup.", ex);
				Application_ThreadException(ActiveForm, new ThreadExceptionEventArgs(ex));
			}
		}

		/// <summary>
		/// Helper method to show the other running instances
		/// </summary>
		private static void ShowOtherInstances()
		{
			var instanceInfo = new StringBuilder();
			bool matchedThisProcess = false;
			int index = 1;
			int currentProcessId;
			using (var currentProcess = Process.GetCurrentProcess())
			{
				currentProcessId = currentProcess.Id;
			}
			foreach (var greenshotProcess in Process.GetProcessesByName(ApplicationName))
			{
				try
				{
					instanceInfo.Append(index++ + ": ").AppendLine(Kernel32.GetProcessPath(greenshotProcess.Id));
					if (currentProcessId == greenshotProcess.Id)
					{
						matchedThisProcess = true;
					}
				}
				catch (Exception ex)
				{
					LOG.Debug(ex);
				}
				greenshotProcess.Dispose();
			}
			if (!matchedThisProcess)
			{
				using (Process currentProcess = Process.GetCurrentProcess())
				{
					instanceInfo.Append(index + ": ").AppendLine(Kernel32.GetProcessPath(currentProcess.Id));
				}
			}

			// A dirty fix to make sure the messagebox is visible as a Greenshot window on the taskbar
			using (var dummyForm = new Form())
			{
				dummyForm.Icon = GreenshotResources.GetGreenshotIcon();
				dummyForm.ShowInTaskbar = true;
				dummyForm.FormBorderStyle = FormBorderStyle.None;
				dummyForm.Location = new Point(int.MinValue, int.MinValue);
				dummyForm.Load += (sender, eventArgs) => dummyForm.Size = Size.Empty;
				dummyForm.Show();
				// Make sure the language files are loaded, so we can show the error message "Greenshot is already running" in the right language.

				MessageBox.Show(dummyForm, language.TranslationOrDefault(t => t.ErrorMultipleinstances) + "\r\n" + instanceInfo, language.TranslationOrDefault(t => t.Error), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		/// <summary>
		/// This tries to get the AppMutex, which takes care of having multiple Greenshot instances running
		/// </summary>
		/// <returns>true if it worked, false if another instance is already running</returns>
		private static bool LockAppMutex()
		{
			bool lockSuccess = true;
			// check whether there's an local instance running already, but use local so this works in a multi-user environment
			try
			{
				// Added Mutex Security, hopefully this prevents the UnauthorizedAccessException more gracefully
				// See an example in Bug #3131534
				var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
				var mutexsecurity = new MutexSecurity();
				mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
				mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.ChangePermissions, AccessControlType.Deny));
				mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.Delete, AccessControlType.Deny));

				bool created;
				// 1) Create Mutex
				_applicationMutex = new Mutex(false, MutexId, out created, mutexsecurity);
				// 2) Get the right to it, this returns false if it's already locked
				if (!_applicationMutex.WaitOne(0, false))
				{
					LOG.Debug($"{ApplicationName} seems already to be running!");
					lockSuccess = false;
					// Clean up
					_applicationMutex.Close();
					_applicationMutex = null;
				}
			}
			catch (AbandonedMutexException e)
			{
				// Another Greenshot instance didn't cleanup correctly!
				// we can ignore the exception, it happend on the "waitone" but still the mutex belongs to us
				LOG.Warn($"{ApplicationName} didn't cleanup correctly!", e);
			}
			catch (UnauthorizedAccessException e)
			{
				LOG.Warn($"{ApplicationName} is most likely already running for a different user in the same session, can't create mutex due to error: ", e);
				lockSuccess = false;
			}
			catch (Exception ex)
			{
				LOG.Warn("Problem obtaining the Mutex, assuming it was already taken!", ex);
				lockSuccess = false;
			}
			return lockSuccess;
		}

		/// <summary>
		/// Free the application mutex
		/// </summary>
		private static void FreeMutex()
		{
			if (_applicationMutex != null)
			{
				try
				{
					_applicationMutex.ReleaseMutex();
					_applicationMutex = null;
				}
				catch (Exception ex)
				{
					LOG.Error("Error releasing Mutex!", ex);
				}
			}
		}

		private static MainForm _instance;

		public static MainForm Instance
		{
			get
			{
				return _instance;
			}
		}

		// Thumbnail preview
		private ThumbnailForm _thumbnailForm;
		// Make sure we have only one settings form
		private SettingsForm _settingsForm;
		// Timer for the background update check (and more?)
		private readonly System.Threading.Timer _backgroundWorkerTimer;
		// Timer for the double click test
		private readonly Timer _doubleClickTimer = new Timer();
		private GreenshotServer server;

		/// <summary>
		/// Instance of the NotifyIcon, needed to open balloon-tips
		/// </summary>
		public NotifyIcon NotifyIcon
		{
			get
			{
				return notifyIcon;
			}
		}

		public MainForm(Arguments arguments)
		{
			_instance = this;

			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			try
			{
				InitializeComponent();
			}
			catch (ArgumentException ex)
			{
				// Added for Bug #1420, this doesn't solve the issue but maybe the user can do something with it.
				ex.Data.Add("more information here", "http://support.microsoft.com/kb/943140");
				throw;
			}
			notifyIcon.Icon = GreenshotResources.GetGreenshotIcon();

			// Disable access to the settings, for feature #3521446
			contextmenu_settings.Visible = !coreConfiguration.DisableSettings;

			// Make sure all hotkeys pass this window!
			HotkeyControl.RegisterHotkeyHWND(Handle);
			RegisterHotkeys();

			new ToolTip();

			UpdateUi();

            if (PortableHelper.IsPortable)
			{
				var pafPath = Path.Combine(Application.StartupPath, $@"App\{ApplicationName}");
				ApplicationBootstrapper.Add(pafPath, "*.gsp");
			}
			else
			{
				var pluginPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
				ApplicationBootstrapper.Add(pluginPath, "*.gsp");

				var applicationPath = Path.GetDirectoryName(Application.ExecutablePath);
				ApplicationBootstrapper.Add(applicationPath, "*.gsp");
			}
			// The GreenshotPlugin assembly needs to be added manually!
			ApplicationBootstrapper.Add(typeof(ICoreConfiguration).Assembly);
			// Initialize the bootstrapper, so we can export
			ApplicationBootstrapper.Initialize();
			// Run!
			ApplicationBootstrapper.Run();

			// Load all the plugins
			Task.Run(async () =>
			{
				await ApplicationBootstrapper.StartupAsync();
			}).Wait();

			// Check destinations, remove all that don't exist
			foreach (string destination in coreConfiguration.OutputDestinations.ToArray())
			{
				if (DestinationHelper.GetDestination(destination) == null)
				{
					coreConfiguration.OutputDestinations.Remove(destination);
				}
			}

			// we should have at least one!
			if (coreConfiguration.OutputDestinations.Count == 0)
			{
				coreConfiguration.OutputDestinations.Add(BuildInDestinationEnum.Editor.ToString());
			}

			if (coreConfiguration.DisableQuickSettings)
			{
				contextmenu_quicksettings.Visible = false;
			}
			else
			{
				// Do after all plugins & finding the destination, otherwise they are missing!
				InitializeQuickSettingsMenu();
			}

			SoundHelper.Initialize();

			coreConfiguration.PropertyChanged += OnIconSizeChanged;
			OnIconSizeChanged(this, new PropertyChangedEventArgs("IconSize"));

			// Set the Greenshot icon visibility depending on the configuration. (Added for feature #3521446)
			// Setting it to true this late prevents Problems with the context menu
			notifyIcon.Visible = !coreConfiguration.HideTrayicon;

			// Check if it's the first time launch?
			if (coreConfiguration.IsFirstLaunch)
			{
				coreConfiguration.IsFirstLaunch = false;
				LOG.Info("FirstLaunch: Created new configuration, showing balloon.");
				try
				{
					notifyIcon.BalloonTipClicked += BalloonTipClicked;
					notifyIcon.BalloonTipClosed += BalloonTipClosed;
					notifyIcon.ShowBalloonTip(2000, ApplicationName, string.Format(language.TooltipFirststart, HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.RegionHotkey)), ToolTipIcon.Info);
				}
				catch (Exception ex)
				{
					LOG.Warn("Exception while showing first launch: ", ex);
				}
			}

			// Make sure we never capture the mainform
			WindowDetails.RegisterIgnoreHandle(Handle);

			// Make Greenshot use less memory after startup
			if (coreConfiguration.MinimizeWorkingSetSize)
			{
				PsAPI.EmptyWorkingSet();
			}
			// Checking for updates etc in the background
			_backgroundWorkerTimer = new System.Threading.Timer(async _ => await BackgroundWorkerTimerTick().ConfigureAwait(false), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5));

			Load += async (sender, eventArguments) =>
			{
				server = new GreenshotServer();
				await server.StartAsync();
				// Use the client to connect to myself, maybe a bit overdone but it saves code
				GreenshotClient.OpenFiles(arguments.FilesToOpen);
			};
		}

		/// <summary>
		/// Handler for the BalloonTip clicked event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BalloonTipClicked(object sender, EventArgs e)
		{
			try
			{
				ShowSetting();
			}
			finally
			{
				BalloonTipClosed(sender, e);
			}
		}

		/// <summary>
		/// Handler for the BalloonTip closed event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BalloonTipClosed(object sender, EventArgs e)
		{
			notifyIcon.BalloonTipClicked -= BalloonTipClicked;
			notifyIcon.BalloonTipClosed -= BalloonTipClosed;
		}

		/// <summary>
		/// Get the ContextMenuStrip
		/// </summary>
		public ContextMenuStrip MainMenu
		{
			get
			{
				return contextMenu;
			}
		}

		#region hotkeys

		protected override void WndProc(ref Message m)
		{
			if (HotkeyControl.HandleMessages(ref m))
			{
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
		/// <param name="hotkeyAction"></param>
		/// <returns></returns>
		private static bool RegisterHotkey(StringBuilder failedKeys, string functionName, string hotkeyString, Action hotkeyAction)
		{
			Keys modifierKeyCode = HotkeyControl.HotkeyModifiersFromString(hotkeyString);
			Keys virtualKeyCode = HotkeyControl.HotkeyFromString(hotkeyString);
			if (!Keys.None.Equals(virtualKeyCode))
			{
				if (HotkeyControl.RegisterHotKey(modifierKeyCode, virtualKeyCode, hotkeyAction) < 0)
				{
					LOG.DebugFormat("Failed to register {0} to hotkey: {1}", functionName, hotkeyString);
					if (failedKeys.Length > 0)
					{
						failedKeys.Append(", ");
					}
					failedKeys.Append(hotkeyString);
					return false;
				}
				LOG.DebugFormat("Registered {0} to hotkey: {1}", functionName, hotkeyString);
			}
			else
			{
				LOG.InfoFormat("Skipping hotkey registration for {0}, no hotkey set!", functionName);
			}
			return true;
		}

		private static bool RegisterWrapper(StringBuilder failedKeys, string functionName, string configurationKey, Action hotkeyAction, bool ignoreFailedRegistration)
		{
			IniValue hotkeyValue = coreConfiguration.GetIniValue(configurationKey);
			try
			{
				bool success = RegisterHotkey(failedKeys, functionName, hotkeyValue.Value.ToString(), hotkeyAction);
				if (!success && ignoreFailedRegistration)
				{
					LOG.DebugFormat("Ignoring failed hotkey registration for {0}, with value '{1}', resetting to 'None'.", functionName, hotkeyValue.Value);
					hotkeyValue.Value = Keys.None.ToString();
				}
				return success;
			}
			catch (Exception ex)
			{
				LOG.Warn(ex);
				LOG.WarnFormat("Restoring default hotkey for {0}, stored under {1} from '{2}' to '{3}'", functionName, configurationKey, hotkeyValue.Value, hotkeyValue.DefaultValue);
				// when getting an exception the key wasn't found: reset the hotkey value
				hotkeyValue.ResetToDefault();
				return RegisterHotkey(failedKeys, functionName, hotkeyValue.Value.ToString(), hotkeyAction);
			}
		}

		/// <summary>
		/// Fix icon reference
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnIconSizeChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IconSize")
			{
				contextMenu.ImageScalingSize = coreConfiguration.IconSize;
				string ieExePath = PluginUtils.GetExePath("iexplore.exe");
				if (!string.IsNullOrEmpty(ieExePath))
				{
					contextmenu_captureie.Image = PluginUtils.GetCachedExeIcon(ieExePath, 0);
				}
			}
		}

		/// <summary>
		/// Registers all hotkeys as configured, displaying a dialog in case of hotkey conflicts with other tools.
		/// </summary>
		/// <returns>Whether the hotkeys could be registered to the users content. This also applies if conflicts arise and the user decides to ignore these (i.e. not to register the conflicting hotkey).</returns>
		public static bool RegisterHotkeys()
		{
			return RegisterHotkeys(false);
		}

		/// <summary>
		/// Registers all hotkeys as configured, displaying a dialog in case of hotkey conflicts with other tools.
		/// </summary>
		/// <param name="ignoreFailedRegistration">if true, a failed hotkey registration will not be reported to the user - the hotkey will simply not be registered</param>
		/// <returns>Whether the hotkeys could be registered to the users content. This also applies if conflicts arise and the user decides to ignore these (i.e. not to register the conflicting hotkey).</returns>
		private static bool RegisterHotkeys(bool ignoreFailedRegistration)
		{
			if (_instance == null)
			{
				return false;
			}
			bool success = true;
			var failedKeys = new StringBuilder();

			if (!RegisterWrapper(failedKeys, "CaptureRegion", "RegionHotkey", () => _instance.CaptureRegion(), ignoreFailedRegistration))
			{
				success = false;
			}
			if (!RegisterWrapper(failedKeys, "CaptureWindow", "WindowHotkey", () => _instance.CaptureWindow(), ignoreFailedRegistration))
			{
				success = false;
			}
			if (!RegisterWrapper(failedKeys, "CaptureFullScreen", "FullscreenHotkey", () => _instance.CaptureFullScreen(), ignoreFailedRegistration))
			{
				success = false;
			}
			if (!RegisterWrapper(failedKeys, "CaptureLastRegion", "LastregionHotkey", () => _instance.CaptureLastRegion(), ignoreFailedRegistration))
			{
				success = false;
			}
			if (coreConfiguration.IECapture)
			{
				if (!RegisterWrapper(failedKeys, "CaptureIE", "IEHotkey", () => _instance.CaptureIE(), ignoreFailedRegistration))
				{
					success = false;
				}
			}

			if (!success && !ignoreFailedRegistration)
			{
				success = HandleFailedHotkeyRegistration(failedKeys.ToString());
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
		private static bool HandleFailedHotkeyRegistration(string failedKeys)
		{
			bool success = false;
			DialogResult dr = MessageBox.Show(Instance, string.Format(language.WarningHotkeys, failedKeys), language.Warning, MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Exclamation);
			if (dr == DialogResult.Retry)
			{
				LOG.DebugFormat("Re-trying to register hotkeys");
				HotkeyControl.UnregisterHotkeys();
				success = RegisterHotkeys(false);
			}
			else if (dr == DialogResult.Ignore)
			{
				LOG.DebugFormat("Ignoring failed hotkey registration");
				HotkeyControl.UnregisterHotkeys();
				success = RegisterHotkeys(true);
			}
			return success;
		}

		#endregion

		/// <summary>
		/// Helper method to reload the configuration
		/// </summary>
		public async Task ReloadConfig()
		{
			await iniConfig.ReloadAsync();
			// Even update language when needed
			UpdateUi();
			// Update the hotkey
			// Make sure the current hotkeys are disabled
			HotkeyControl.UnregisterHotkeys();
			RegisterHotkeys();
		}

		public void UpdateUi()
		{
			// As the form is never loaded, call ApplyLanguage ourselves
			ApplyLanguage();

			// Show hotkeys in Contextmenu
			contextmenu_capturearea.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.RegionHotkey);
			contextmenu_capturelastregion.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.LastregionHotkey);
			contextmenu_capturewindow.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.WindowHotkey);
			contextmenu_capturefullscreen.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.FullscreenHotkey);
			contextmenu_captureie.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.IEHotkey);
		}

		#region mainform events

		private void MainFormFormClosing(object sender, FormClosingEventArgs e)
		{
			LOG.DebugFormat("Mainform closing, reason: {0}", e.CloseReason);
			_instance = null;
			Exit();
		}

		private void MainFormActivated(object sender, EventArgs e)
		{
			Hide();
			ShowInTaskbar = false;
		}

		#endregion

		#region key handlers

		private void CaptureRegion(CancellationToken token = default(CancellationToken))
		{
			TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			var task = Task.Factory.StartNew(async () =>
			{
				await CaptureHelper.CaptureRegionAsync(true, token);
			}, token, TaskCreationOptions.None, scheduler);
		}

		private void CaptureFile(CancellationToken token = default(CancellationToken))
		{
			var openFileDialog = new OpenFileDialog
			{
				Filter = "Image files (*.greenshot, *.png, *.jpg, *.gif, *.bmp, *.ico, *.tiff, *.wmf)|*.greenshot; *.png; *.jpg; *.jpeg; *.gif; *.bmp; *.ico; *.tiff; *.tif; *.wmf"
			};
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				if (File.Exists(openFileDialog.FileName))
				{
					TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
					var task = Task.Factory.StartNew(async () =>
					{
						await CaptureHelper.CaptureFileAsync(openFileDialog.FileName, token);
					}, token, TaskCreationOptions.None, scheduler);
				}
			}
		}

		private void CaptureFullScreen(CancellationToken token = default(CancellationToken))
		{
			TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			var task = Task.Factory.StartNew(async () =>
			{
				await CaptureHelper.CaptureFullscreenAsync(true, coreConfiguration.ScreenCaptureMode, token);
			}, token, TaskCreationOptions.None, scheduler);
		}

		private void CaptureLastRegion(CancellationToken token = default(CancellationToken))
		{
			TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			var task = Task.Factory.StartNew(async () =>
			{
				await CaptureHelper.CaptureLastRegionAsync(true, token);
			}, token, TaskCreationOptions.None, scheduler);
		}

		private void CaptureIE(CancellationToken token = default(CancellationToken))
		{
			if (coreConfiguration.IECapture)
			{
				TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
				var task = Task.Factory.StartNew(async () =>
				{
					await CaptureHelper.CaptureIEAsync(true, null, token);
				}, token, TaskCreationOptions.None, scheduler);
			}
		}

		private void CaptureWindow(CancellationToken token = default(CancellationToken))
		{
			TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			var task = Task.Factory.StartNew(async () =>
			{
				if (coreConfiguration.CaptureWindowsInteractive)
				{
					await CaptureHelper.CaptureWindowInteractiveAsync(true, token);
				}
				else
				{
					await CaptureHelper.CaptureWindowAsync(true, token);
				}
			}, token, TaskCreationOptions.None, scheduler);
		}

		#endregion

		#region contextmenu

		private void ContextMenuOpened(object sender, EventArgs e)
		{
			contextmenu_captureclipboard.Enabled = ClipboardHelper.ContainsImage();
		}

		private void ContextMenuOpening(object sender, CancelEventArgs e)
		{
			contextmenu_capturelastregion.Enabled = coreConfiguration.LastCapturedRegion != Rectangle.Empty;

			// IE context menu code
			try
			{
				if (coreConfiguration.IECapture && IECaptureHelper.IsIERunning())
				{
					contextmenu_captureie.Enabled = true;
					contextmenu_captureiefromlist.Enabled = true;
				}
				else
				{
					contextmenu_captureie.Enabled = false;
					contextmenu_captureiefromlist.Enabled = false;
				}
			}
			catch (Exception ex)
			{
				LOG.WarnFormat("Problem accessing IE information: {0}", ex.Message);
			}

			// Multi-Screen captures
			contextmenu_capturefullscreen.Click -= CaptureFullScreenToolStripMenuItemClick;
			contextmenu_capturefullscreen.DropDownOpening -= MultiScreenDropDownOpening;
			contextmenu_capturefullscreen.DropDownClosed -= MultiScreenDropDownClosing;
			if (User32.AllDisplays().Count > 1)
			{
				contextmenu_capturefullscreen.DropDownOpening += MultiScreenDropDownOpening;
				contextmenu_capturefullscreen.DropDownClosed += MultiScreenDropDownClosing;
			}
			else
			{
				contextmenu_capturefullscreen.Click += CaptureFullScreenToolStripMenuItemClick;
			}

			var now = DateTime.Now;
			if ((now.Month == 12 && now.Day > 19 && now.Day < 27) || // christmas
				(now.Month == 3 && now.Day > 13 && now.Day < 21))
			{
				// birthday
				var resources = new ComponentResourceManager(typeof (MainForm));
				contextmenu_donate.Image = (Image) resources.GetObject("contextmenu_present.Image");
			}
		}

		private void ContextMenuClosing(object sender, EventArgs e)
		{
			contextmenu_captureiefromlist.DropDownItems.Clear();
			contextmenu_capturewindowfromlist.DropDownItems.Clear();
			CleanupThumbnail();
		}

		/// <summary>
		/// Build a selectable list of IE tabs when we enter the menu item
		/// </summary>
		private void CaptureIeMenuDropDownOpening(object sender, EventArgs e)
		{
			if (!coreConfiguration.IECapture)
			{
				return;
			}
			try
			{
				var tabs = IECaptureHelper.GetBrowserTabs();
				contextmenu_captureiefromlist.DropDownItems.Clear();
				if (tabs.Count > 0)
				{
					contextmenu_captureie.Enabled = true;
					contextmenu_captureiefromlist.Enabled = true;
					var counter = new Dictionary<WindowDetails, int>();

					foreach (KeyValuePair<WindowDetails, string> tabData in tabs)
					{
						string title = tabData.Value;
						if (title == null)
						{
							continue;
						}
						if (title.Length > coreConfiguration.MaxMenuItemLength)
						{
							title = title.Substring(0, Math.Min(title.Length, coreConfiguration.MaxMenuItemLength));
						}
						ToolStripItem captureIeTabItem = contextmenu_captureiefromlist.DropDownItems.Add(title);
						int value;
						int index = counter.TryGetValue(tabData.Key, out value) ? value : 0;
						captureIeTabItem.Image = tabData.Key.DisplayIcon;
						captureIeTabItem.Tag = new KeyValuePair<WindowDetails, int>(tabData.Key, index++);
						captureIeTabItem.Click += Contextmenu_captureiefromlist_Click;
						contextmenu_captureiefromlist.DropDownItems.Add(captureIeTabItem);
						if (counter.ContainsKey(tabData.Key))
						{
							counter[tabData.Key] = index;
						}
						else
						{
							counter.Add(tabData.Key, index);
						}
					}
				}
				else
				{
					contextmenu_captureie.Enabled = false;
					contextmenu_captureiefromlist.Enabled = false;
				}
			}
			catch (Exception ex)
			{
				LOG.WarnFormat("Problem accessing IE information: {0}", ex.Message);
			}
		}

		/// <summary>
		/// MultiScreenDropDownOpening is called when mouse hovers over the Capture-Screen context menu 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MultiScreenDropDownOpening(object sender, EventArgs e)
		{
			var captureScreenMenuItem = (ToolStripMenuItem) sender;
			captureScreenMenuItem.DropDownItems.Clear();
			if (User32.AllDisplays().Count <= 1)
			{
				return;
			}
			ToolStripMenuItem captureScreenItem;
			var allScreensBounds = WindowCapture.GetScreenBounds();

			captureScreenItem = new ToolStripMenuItem(language.ContextmenuCapturefullscreenAll);
			captureScreenItem.Click += (item, eventArgs) =>
			{
				this.AsyncInvoke(async () => await CaptureHelper.CaptureFullscreenAsync(false, ScreenCaptureMode.FullScreen));
			};
			captureScreenMenuItem.DropDownItems.Add(captureScreenItem);
			foreach (var display in User32.AllDisplays())
			{
				var screenToCapture = display; // Capture loop variable
				string deviceAlignment = "";
				if (display.Bounds.Top == allScreensBounds.Top && display.Bounds.Bottom != allScreensBounds.Bottom)
				{
					deviceAlignment += " " + language.ContextmenuCapturefullscreenTop;
				}
				else if (display.Bounds.Top != allScreensBounds.Top && display.Bounds.Bottom == allScreensBounds.Bottom)
				{
					deviceAlignment += " " + language.ContextmenuCapturefullscreenBottom;
				}
				if (display.Bounds.Left == allScreensBounds.Left && display.Bounds.Right != allScreensBounds.Right)
				{
					deviceAlignment += " " + language.ContextmenuCapturefullscreenLeft;
				}
				else if (display.Bounds.Left != allScreensBounds.Left && display.Bounds.Right == allScreensBounds.Right)
				{
					deviceAlignment += " " + language.ContextmenuCapturefullscreenRight;
				}
				captureScreenItem = new ToolStripMenuItem(deviceAlignment);
				captureScreenItem.Click += (item, eventArgs) =>
				{
					this.AsyncInvoke(async () => await CaptureHelper.CaptureRegionAsync(false, screenToCapture.BoundsRectangle));
				};
				captureScreenMenuItem.DropDownItems.Add(captureScreenItem);
			}
		}

		/// <summary>
		/// MultiScreenDropDownOpening is called when mouse leaves the context menu 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MultiScreenDropDownClosing(object sender, EventArgs e)
		{
			ToolStripMenuItem captureScreenMenuItem = (ToolStripMenuItem) sender;
			captureScreenMenuItem.DropDownItems.Clear();
		}

		/// <summary>
		/// Build a selectable list of windows when we enter the menu item
		/// </summary>
		private void CaptureWindowFromListMenuDropDownOpening(object sender, EventArgs e)
		{
			// The Capture window context menu item used to go to the following code:
			// captureForm.MakeCapture(CaptureMode.Window, false);
			// Now we check which windows are there to capture
			ToolStripMenuItem captureWindowFromListMenuItem = (ToolStripMenuItem) sender;
			AddCaptureWindowMenuItems(captureWindowFromListMenuItem, Contextmenu_capturewindowfromlist_Click);
		}

		private void CaptureWindowFromListMenuDropDownClosed(object sender, EventArgs e)
		{
			CleanupThumbnail();
		}

		private void ShowThumbnailOnEnter(object sender, EventArgs e)
		{
			ToolStripMenuItem captureWindowItem = sender as ToolStripMenuItem;
			if (captureWindowItem != null)
			{
				WindowDetails window = captureWindowItem.Tag as WindowDetails;
				if (_thumbnailForm == null)
				{
					_thumbnailForm = new ThumbnailForm();
				}
				_thumbnailForm.ShowThumbnail(window, captureWindowItem.GetCurrentParent().TopLevelControl);
			}
		}

		private void HideThumbnailOnLeave(object sender, EventArgs e)
		{
			if (_thumbnailForm != null)
			{
				_thumbnailForm.Hide();
			}
		}

		private void CleanupThumbnail()
		{
			if (_thumbnailForm != null)
			{
				_thumbnailForm.Close();
				_thumbnailForm = null;
			}
		}

		public void AddCaptureWindowMenuItems(ToolStripMenuItem menuItem, EventHandler eventHandler)
		{
			menuItem.DropDownItems.Clear();
			// check if thumbnailPreview is enabled and DWM is enabled
			bool thumbnailPreview = coreConfiguration.ThumnailPreview && Dwm.IsDwmEnabled;

			foreach (var window in WindowDetails.GetTopLevelWindows())
			{
				string title = window.Text;
				if (title == null)
				{
					continue;
				}
				if (title.Length > coreConfiguration.MaxMenuItemLength)
				{
					title = title.Substring(0, Math.Min(title.Length, coreConfiguration.MaxMenuItemLength));
				}
				var captureWindowItem = menuItem.DropDownItems.Add(title);
				captureWindowItem.Tag = window;
				captureWindowItem.Image = window.DisplayIcon;
				captureWindowItem.Click += eventHandler;
				// Only show preview when enabled
				if (thumbnailPreview)
				{
					captureWindowItem.MouseEnter += ShowThumbnailOnEnter;
					captureWindowItem.MouseLeave += HideThumbnailOnLeave;
				}
			}
		}

		private void CaptureAreaToolStripMenuItemClick(object sender, EventArgs e)
		{
			this.AsyncInvoke(async () => await CaptureHelper.CaptureRegionAsync(false));
		}

		private async void CaptureClipboardToolStripMenuItemClick(object sender, EventArgs e)
		{
			await CaptureHelper.CaptureClipboardAsync();
		}

		private void OpenFileToolStripMenuItemClick(object sender, EventArgs e)
		{
			this.AsyncInvoke(() => CaptureFile());
		}

		private void CaptureFullScreenToolStripMenuItemClick(object sender, EventArgs e)
		{
			this.AsyncInvoke(async () => await CaptureHelper.CaptureFullscreenAsync(false, coreConfiguration.ScreenCaptureMode));
		}

		private void Contextmenu_capturelastregionClick(object sender, EventArgs e)
		{
			this.AsyncInvoke(async () => await CaptureHelper.CaptureLastRegionAsync(false));
		}

		private void Contextmenu_capturewindow_Click(object sender, EventArgs e)
		{
			this.AsyncInvoke(async () => await CaptureHelper.CaptureWindowInteractiveAsync(false));
		}

		private void Contextmenu_capturewindowfromlist_Click(object sender, EventArgs e)
		{
			var clickedItem = (ToolStripMenuItem) sender;
			this.AsyncInvoke(async () =>
			{
				try
				{
					var windowToCapture = (WindowDetails) clickedItem.Tag;
					await CaptureHelper.CaptureWindowAsync(windowToCapture);
				}
				catch (Exception exception)
				{
					LOG.Error(exception);
				}
			});
		}

		private void Contextmenu_captureie_Click(object sender, EventArgs e)
		{
			CaptureIE();
		}

		private void Contextmenu_captureiefromlist_Click(object sender, EventArgs e)
		{
			if (!coreConfiguration.IECapture)
			{
				LOG.InfoFormat("IE Capture is disabled.");
				return;
			}
			var clickedItem = (ToolStripMenuItem) sender;
			var tabData = (KeyValuePair<WindowDetails, int>) clickedItem.Tag;
			BeginInvoke(new Action(async () =>
			{
				var ieWindowToCapture = tabData.Key;
				if (ieWindowToCapture != null && (!ieWindowToCapture.Visible || ieWindowToCapture.Iconic))
				{
					ieWindowToCapture.Restore();
				}
				try
				{
					IECaptureHelper.ActivateIETab(ieWindowToCapture, tabData.Value);
				}
				catch (Exception exception)
				{
					LOG.Error(exception);
				}
				try
				{
					await CaptureHelper.CaptureIEAsync(false, ieWindowToCapture);
				}
				catch (Exception exception)
				{
					LOG.Error(exception);
				}
			}));
		}

		/// <summary>
		/// Context menu entry "Support Greenshot"
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Contextmenu_donateClick(object sender, EventArgs e)
		{
			this.AsyncInvoke(() => Process.Start("http://getgreenshot.org/support/"));
		}

		/// <summary>
		/// Context menu entry "Preferences"
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Contextmenu_settingsClick(object sender, EventArgs e)
		{
			this.AsyncInvoke(() => ShowSetting());
		}

		/// <summary>
		/// This is called indirectly from the context menu "Preferences"
		/// </summary>
		public void ShowSetting()
		{
			if (_settingsForm != null)
			{
				WindowDetails.ToForeground(_settingsForm.Handle);
			}
			else
			{
				try
				{
					using (_settingsForm = new SettingsForm())
					{
						if (_settingsForm.ShowDialog() == DialogResult.OK)
						{
							InitializeQuickSettingsMenu();
						}
					}
				}
				finally
				{
					_settingsForm = null;
				}
			}
		}

		/// <summary>
		/// The "About Greenshot" entry is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Contextmenu_aboutClick(object sender, EventArgs e)
		{
			ShowAbout();
		}

		/// <summary>
		/// Show the about
		/// </summary>
		public void ShowAbout()
		{
			AboutWindow.Create();
		}

		/// <summary>
		/// The "Help" entry is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Contextmenu_helpClick(object sender, EventArgs e)
		{
			var ignoreTask = HelpFileLoader.LoadHelpAsync();
		}

		/// <summary>
		/// The "Exit" entry is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Contextmenu_exitClick(object sender, EventArgs e)
		{
			Exit();
		}

		private void CheckStateChangedHandler(object sender, EventArgs e)
		{
			ToolStripMenuSelectListItem captureMouseItem = sender as ToolStripMenuSelectListItem;
			if (captureMouseItem != null)
			{
				coreConfiguration.CaptureMousepointer = captureMouseItem.Checked;
			}
		}

		/// <summary>
		/// This needs to be called to initialize the quick settings menu entries
		/// </summary>
		private void InitializeQuickSettingsMenu()
		{
			contextmenu_quicksettings.DropDownItems.Clear();

			if (coreConfiguration.DisableQuickSettings)
			{
				return;
			}

			// Only add if the value is not fixed
			if (!coreConfiguration.IsWriteProtected(x => x.CaptureMousepointer))
			{
				// For the capture mousecursor option
				ToolStripMenuSelectListItem captureMouseItem = new ToolStripMenuSelectListItem();
				captureMouseItem.Text = language.SettingsCaptureMousepointer;
				captureMouseItem.Checked = coreConfiguration.CaptureMousepointer;
				captureMouseItem.CheckOnClick = true;
				captureMouseItem.CheckStateChanged += CheckStateChangedHandler;

				contextmenu_quicksettings.DropDownItems.Add(captureMouseItem);
			}
			ToolStripMenuSelectList selectList;
			if (!coreConfiguration.IsWriteProtected(x => x.OutputDestinations))
			{
				// screenshot destination
				selectList = new ToolStripMenuSelectList("destinations", true);
				selectList.Text = language.SettingsDestination;
				// Working with IDestination:
				foreach (var destination in DestinationHelper.GetAllDestinations())
				{
					selectList.AddItem(destination.Description, destination, coreConfiguration.OutputDestinations.Contains(destination.Designation));
				}
				selectList.CheckedChanged += QuickSettingDestinationChanged;
				contextmenu_quicksettings.DropDownItems.Add(selectList);
			}

			if (!coreConfiguration.IsWriteProtected(x => x.WindowCaptureMode))
			{
				// Capture Modes
				selectList = new ToolStripMenuSelectList("capturemodes", false);
				selectList.Text = language.SettingsWindowCaptureMode;
				string enumTypeName = typeof (WindowCaptureMode).Name;
				foreach (WindowCaptureMode captureMode in Enum.GetValues(typeof (WindowCaptureMode)))
				{
					var languageKey = string.Format("{0}.{1}", enumTypeName, captureMode);
					var translation = language[languageKey];
					selectList.AddItem(translation, captureMode, coreConfiguration.WindowCaptureMode == captureMode);
				}
				selectList.CheckedChanged += QuickSettingCaptureModeChanged;
				contextmenu_quicksettings.DropDownItems.Add(selectList);
			}

			// print options
			selectList = new ToolStripMenuSelectList("printoptions", true);
			selectList.Text = language.SettingsPrintoptions;

			var outputPrintValues = from iniValue in coreConfiguration.GetIniValues().Values
				where !coreConfiguration.IsWriteProtected(iniValue.PropertyName) && iniValue.PropertyName.StartsWith("OutputPrint")
				select iniValue;

			foreach (var iniValue in outputPrintValues)
			{
				var languageKey = coreConfiguration.GetTagValue(iniValue.IniPropertyName, ConfigTags.LanguageKey) as string;
				if (languageKey != null)
				{
					selectList.AddItem(language[languageKey], iniValue, (bool) iniValue.Value);
				}
			}
			if (selectList.DropDownItems.Count > 0)
			{
				selectList.CheckedChanged += QuickSettingBoolItemChanged;
				contextmenu_quicksettings.DropDownItems.Add(selectList);
			}

			// effects
			selectList = new ToolStripMenuSelectList("effects", true);
			selectList.Text = language.SettingsVisualization;

			IniValue currentIniValue;
			if (coreConfiguration.TryGetIniValue(x => x.PlayCameraSound, out currentIniValue))
			{
				if (!coreConfiguration.IsWriteProtected(x => x.PlayCameraSound))
				{
					var languageKey = coreConfiguration.GetTagValue(x => x.PlayCameraSound, ConfigTags.LanguageKey) as string;
					if (languageKey != null)
					{
						selectList.AddItem(language[languageKey], currentIniValue, (bool) currentIniValue.Value);
					}
				}
			}
			if (coreConfiguration.TryGetIniValue(x => x.ShowTrayNotification, out currentIniValue))
			{
				if (!coreConfiguration.IsWriteProtected(x => x.ShowTrayNotification))
				{
					var languageKey = coreConfiguration.GetTagValue(x => x.ShowTrayNotification, ConfigTags.LanguageKey) as string;
					if (languageKey != null)
					{
						selectList.AddItem(language[languageKey], currentIniValue, (bool) currentIniValue.Value);
					}
				}
			}
			if (selectList.DropDownItems.Count > 0)
			{
				selectList.CheckedChanged += QuickSettingBoolItemChanged;
				contextmenu_quicksettings.DropDownItems.Add(selectList);
			}
		}

		private void QuickSettingCaptureModeChanged(object sender, EventArgs e)
		{
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs) e).Item;
			WindowCaptureMode windowsCaptureMode = (WindowCaptureMode) item.Data;
			if (item.Checked)
			{
				coreConfiguration.WindowCaptureMode = windowsCaptureMode;
			}
		}

		private void QuickSettingBoolItemChanged(object sender, EventArgs e)
		{
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs) e).Item;
			IniValue iniValue = item.Data as IniValue;
			if (iniValue != null)
			{
				iniValue.Value = item.Checked;
			}
		}

		private void QuickSettingDestinationChanged(object sender, EventArgs e)
		{
			ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs) e).Item;
			ILegacyDestination selectedDestination = (ILegacyDestination) item.Data;
			if (item.Checked)
			{
				if (selectedDestination.Designation.Equals(BuildInDestinationEnum.Picker.ToString()))
				{
					// If the item is the destination picker, remove all others
					coreConfiguration.OutputDestinations.Clear();
				}
				else
				{
					// If the item is not the destination picker, remove the picker
					coreConfiguration.OutputDestinations.Remove(BuildInDestinationEnum.Picker.ToString());
				}
				// Checked an item, add if the destination is not yet selected
				if (!coreConfiguration.OutputDestinations.Contains(selectedDestination.Designation))
				{
					coreConfiguration.OutputDestinations.Add(selectedDestination.Designation);
				}
			}
			else
			{
				// deselected a destination, only remove if it was selected
				if (coreConfiguration.OutputDestinations.Contains(selectedDestination.Designation))
				{
					coreConfiguration.OutputDestinations.Remove(selectedDestination.Designation);
				}
			}
			// Check if something was selected, if not make the picker the default
			if (coreConfiguration.OutputDestinations == null || coreConfiguration.OutputDestinations.Count == 0)
			{
				coreConfiguration.OutputDestinations.Add(BuildInDestinationEnum.Picker.ToString());
			}

			// Rebuild the quick settings menu with the new settings.
			InitializeQuickSettingsMenu();
		}

		#endregion

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception exceptionToLog = e.ExceptionObject as Exception;
			string exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
			LOG.Error(EnvironmentInfo.ExceptionToString(exceptionToLog));
			new BugReportForm(exceptionText).ShowDialog();
		}

		private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
		{
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
		private async void NotifyIconClickTest(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
			{
				return;
			}
			// The right button will automatically be handled with the context menu, here we only check the left.
			if (coreConfiguration.DoubleClickAction == ClickActions.DoNothing)
			{
				// As there isn't a double-click we can start the Left click
				await NotifyIconClickAsync(coreConfiguration.LeftClickAction);
				// ready with the test
				return;
			}
			// If the timer is enabled we are waiting for a double click...
			if (_doubleClickTimer.Enabled)
			{
				// User clicked a second time before the timer tick: Double-click!
				_doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
				_doubleClickTimer.Stop();
				await NotifyIconClickAsync(coreConfiguration.DoubleClickAction);
			}
			else
			{
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
		private async void NotifyIconSingleClickTest(object sender, EventArgs e)
		{
			_doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
			_doubleClickTimer.Stop();
			await NotifyIconClickAsync(coreConfiguration.LeftClickAction);
		}

		/// <summary>
		/// Handle the notify icon click
		/// </summary>
		private async Task NotifyIconClickAsync(ClickActions clickAction, CancellationToken token = default(CancellationToken))
		{
			switch (clickAction)
			{
				case ClickActions.OpenLastInExplorer:
					string path = null;
					if (!string.IsNullOrEmpty(coreConfiguration.OutputFileAsFullpath))
					{
						string lastFilePath = Path.GetDirectoryName(coreConfiguration.OutputFileAsFullpath);
						if (!string.IsNullOrEmpty(lastFilePath) && Directory.Exists(lastFilePath))
						{
							path = lastFilePath;
						}
					}
					if (path == null)
					{
						string configPath = FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false);
						if (Directory.Exists(configPath))
						{
							path = configPath;
						}
					}

					if (path != null)
					{
						try
						{
							using (Process.Start(path))
							{
							}
						}
						catch (Exception ex)
						{
							// Make sure we show what we tried to open in the exception
							ex.Data.Add("path", path);
							throw;
						}
					}
					break;
				case ClickActions.OpenLastInEditor:
					if (File.Exists(coreConfiguration.OutputFileAsFullpath))
					{
						await CaptureHelper.CaptureFileAsync(coreConfiguration.OutputFileAsFullpath, DestinationHelper.GetDestination(BuildInDestinationEnum.Editor.ToString()), token);
					}
					break;
				case ClickActions.OpenSettings:
					ShowSetting();
					break;
				case ClickActions.ShowContextMenu:
					this.AsyncInvoke(() =>
					{
						MethodInfo oMethodInfo = typeof (NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
						oMethodInfo.Invoke(notifyIcon, null);
					});
					break;
				case ClickActions.CaptureRegion:
					CaptureRegion(token);
					break;
				case ClickActions.CaptureScreen:
					CaptureFullScreen(token);
					break;
				case ClickActions.CaptureWindow:
					CaptureWindow(token);
					break;
				case ClickActions.CaptureLastRegion:
					CaptureLastRegion(token);
					break;
			}
		}

		/// <summary>
		/// The Contextmenu_OpenRecent currently opens the last know save location
		/// </summary>
		private void Contextmenu_OpenRecent(object sender, EventArgs eventArgs)
		{
			string path = FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false);
			// Fix for #1470, problems with a drive which is no longer available
			try
			{
				string lastFilePath = Path.GetDirectoryName(coreConfiguration.OutputFileAsFullpath);

				if (lastFilePath != null && Directory.Exists(lastFilePath))
				{
					path = lastFilePath;
				}
				else if (!Directory.Exists(path))
				{
					// What do I open when nothing can be found? Right, nothing...
					return;
				}
			}
			catch (Exception ex)
			{
				LOG.Warn("Couldn't open the path to the last exported file, taking default.", ex);
			}
			LOG.Debug("DoubleClick was called! Starting: " + path);
			try
			{
				Process.Start(path);
			}
			catch (Exception ex)
			{
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
		public void Exit()
		{
			LOG.InfoFormat("Exit: {0}", EnvironmentInfo.EnvironmentToString(false));

			// Close all open forms (except this), use a separate List to make sure we don't get a "InvalidOperationException: Collection was modified"
			var formsToClose = new List<Form>();

			foreach (Form form in Application.OpenForms)
			{
				if (form.Handle != Handle)
				{
					formsToClose.Add(form);
				}
			}

			foreach (var formLV in formsToClose)
			{
				var form = formLV; // Capture the loop variable for the lambda
				LOG.InfoFormat("Closing form: {0}", form.Name);
				this.AsyncInvoke(() => form.Close());
			}
			// Make sure any "save" actions are shown and handled!
			Application.DoEvents();

			// Make sure hotkeys are disabled
			try
			{
				HotkeyControl.UnregisterHotkeys();
			}
			catch (Exception e)
			{
				LOG.Error("Error unregistering hotkeys!", e);
			}

			// Now the sound isn't needed anymore
			try
			{
				SoundHelper.Deinitialize();
			}
			catch (Exception e)
			{
				LOG.Error("Error deinitializing sound!", e);
			}

			// Inform all registed plugins
			try
			{
				Task.Run(async () =>
				{
					await ApplicationBootstrapper.ShutdownAsync();
				}).Wait();
				
			}
			catch (Exception e)
			{
				LOG.Error("Error shutting down plugins!", e);
			}

			// Gracefull shutdown
			try
			{
				Application.DoEvents();
				Application.Exit();
			}
			catch (Exception e)
			{
				LOG.Error("Error closing application!", e);
			}

			ImageOutput.RemoveTmpFiles();

			// Store any open configuration changes
			try
			{
				Task.Run(async () => await iniConfig.WriteAsync()).Wait();
			}
			catch (Exception e)
			{
				LOG.Error("Error storing configuration!", e);
			}

			// Remove the application mutex
			FreeMutex();

			// make the icon invisible otherwise it stays even after exit!!
			if (notifyIcon != null)
			{
				notifyIcon.Visible = false;
				notifyIcon.Dispose();
				notifyIcon = null;
			}
		}


		/// <summary>
		/// Do work in the background
		/// </summary>
		private async Task BackgroundWorkerTimerTick()
		{
			if (coreConfiguration.MinimizeWorkingSetSize)
			{
				PsAPI.EmptyWorkingSet();
			}
			if (await UpdateHelper.IsUpdateCheckNeeded().ConfigureAwait(false))
			{
				LOG.Debug("BackgroundWorkerTimerTick checking for update");
				await UpdateHelper.CheckAndAskForUpdate().ConfigureAwait(false);
			}
		}
	}
}