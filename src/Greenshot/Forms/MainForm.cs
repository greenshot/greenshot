/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026  Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Greenshot.Base;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Help;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Configuration;
using Greenshot.Destinations;
using Greenshot.Editor;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using Greenshot.Helpers;
using Greenshot.Plugin.Win10;
using Greenshot.Processors;
using log4net;
using Timer = System.Timers.Timer;

namespace Greenshot.Forms
{
    /// <summary>
    /// This is the MainForm, the shell of Greenshot
    /// </summary>
    public partial class MainForm : BaseForm, IGreenshotMainForm, ICaptureHelper, IProvideDeviceDpi
    {
        private static ILog LOG;
        private static ResourceMutex _applicationMutex;
        private static CoreConfiguration _conf;
        public static string LogFileLocation;

        public static void Start(string[] arguments)
        {
            // Set the Thread name, is better than "1"
            Thread.CurrentThread.Name = Application.ProductName;

            // Init Log4NET
            LogFileLocation = LogHelper.InitializeLog4Net();
            // Get logger
            LOG = LogManager.GetLogger(typeof(MainForm));

            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            TaskScheduler.UnobservedTaskException += Task_UnhandledException;

            // Initialize the IniConfig
            IniConfig.Init();

            // Log the startup
            LOG.Info("Starting: " + EnvironmentInfo.EnvironmentToString(false));

            // Read configuration
            _conf = IniConfig.GetIniSection<CoreConfiguration>();
            try
            {
                // Fix for Bug 2495900, Multi-user Environment
                // check whether there's an local instance running already
                _applicationMutex = ResourceMutex.Create("F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08", "Greenshot", false);

                var isAlreadyRunning = !_applicationMutex.IsLocked;

                if (arguments.Length > 0 && LOG.IsDebugEnabled)
                {
                    var argumentString = new StringBuilder();
                    foreach (string argument in arguments)
                    {
                        argumentString.Append("[").Append(argument).Append("] ");
                    }

                    LOG.Debug("Greenshot arguments: " + argumentString);
                }

                // Parse command line arguments using System.CommandLine.
                // Returns null when --help was shown or a parse error occurred (application should exit).
                var options = GreenshotCommandLine.Parse(arguments);
                if (options == null)
                {
                    FreeMutex();
                    return;
                }

                if (options.Exit)
                {
                    // un-register application on uninstall (allow uninstall)
                    try
                    {
                        LOG.Info("Sending all instances the exit command.");
                        // Pass Exit to running instance, if any
                        SendData(new CopyDataTransport(CommandEnum.Exit));
                    }
                    catch (Exception e)
                    {
                        LOG.Warn("Exception by exit.", e);
                    }

                    FreeMutex();
                    return;
                }

                if (options.Reload)
                {
                    // Modify configuration
                    LOG.Info("Reloading configuration!");
                    // Update running instances
                    SendData(new CopyDataTransport(CommandEnum.ReloadConfig));
                    FreeMutex();
                    return;
                }

                if (options.NoRun)
                {
                    // Make an exit possible
                    FreeMutex();
                    return;
                }

                if (options.Language != null)
                {
                    _conf.Language = options.Language;
                    IniConfig.Save();
                }

                if (options.IniDirectory != null)
                {
                    IniConfig.IniDirectory = options.IniDirectory;
                }

                var filesToOpen = new List<string>(options.Files);

                // Finished parsing the command line arguments, see if we need to do anything
                CopyDataTransport transport = new CopyDataTransport();
                if (filesToOpen.Count > 0)
                {
                    foreach (string fileToOpen in filesToOpen)
                    {
                        transport.AddCommand(CommandEnum.OpenFile, fileToOpen);
                    }
                }

                if (isAlreadyRunning)
                {
                    // We didn't initialize the language yet, do it here just for the message box
                    if (filesToOpen.Count > 0)
                    {
                        SendData(transport);
                    }
                    else
                    {
                        StringBuilder instanceInfo = new StringBuilder();
                        bool matchedThisProcess = false;
                        int index = 1;
                        int currentProcessId;
                        using (Process currentProcess = Process.GetCurrentProcess())
                        {
                            currentProcessId = currentProcess.Id;
                        }

                        foreach (Process greenshotProcess in Process.GetProcessesByName("greenshot"))
                        {
                            try
                            {
                                instanceInfo.Append(index++ + ": ").AppendLine(Kernel32Api.GetProcessPath(greenshotProcess.Id));
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
                            using Process currentProcess = Process.GetCurrentProcess();
                            instanceInfo.Append(index + ": ").AppendLine(Kernel32Api.GetProcessPath(currentProcess.Id));
                        }

                        // A dirty fix to make sure the message box is visible as a Greenshot window on the taskbar
                        using Form dummyForm = new Form
                        {
                            Icon = GreenshotResources.GetGreenshotIcon(),
                            ShowInTaskbar = true,
                            FormBorderStyle = FormBorderStyle.None,
                            Location = new Point(int.MinValue, int.MinValue)
                        };
                        dummyForm.Load += delegate { dummyForm.Size = Size.Empty; };
                        dummyForm.Show();
                        MessageBox.Show(dummyForm, Language.GetString(LangKey.error_multipleinstances) + "\r\n" + instanceInfo, Language.GetString(LangKey.error),
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }

                    FreeMutex();
                    Application.Exit();
                    return;
                }

                // Make sure we can use forms
                WindowsFormsHost.EnableWindowsFormsInterop();

                // BUG-1809: Add message filter, to filter out all the InputLangChanged messages which go to a target control with a handle > 32 bit.
                Application.AddMessageFilter(new WmInputLangChangeRequestFilter());

                // From here on we continue starting Greenshot
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // if language is not set, show language dialog
                if (string.IsNullOrEmpty(_conf.Language))
                {
                    LanguageDialog languageDialog = LanguageDialog.GetInstance();
                    languageDialog.ShowDialog();
                    _conf.Language = languageDialog.SelectedLanguage;
                }

                // Check if it's the first time launch?
                if (_conf.IsFirstLaunch)
                {
                    _conf.IsFirstLaunch = false;
                    transport.AddCommand(CommandEnum.FirstLaunch);
                }

                // Should fix BUG-1633
                Application.DoEvents();
                _instance = new MainForm(transport);

                // force saving ini on every start because some init functions could change/fix the configuration. i.e. loading plugins
                IniConfig.Save();
                Application.Run();
            }
            catch (Exception ex)
            {
                LOG.Error("Exception in startup.", ex);
                Application_ThreadException(ActiveForm, new ThreadExceptionEventArgs(ex));
            }
        }

        /// <summary>
        /// Send DataTransport Object via Window-messages
        /// </summary>
        /// <param name="dataTransport">DataTransport with data for a running instance</param>
        private static void SendData(CopyDataTransport dataTransport)
        {
            string appName = Application.ProductName;
            CopyData copyData = new CopyData();
            copyData.Channels.Add(appName);
            copyData.Channels[appName].Send(dataTransport);
        }

        private static void FreeMutex()
        {
            // Remove the application mutex
            if (_applicationMutex == null)
            {
                return;
            }
            try
            {
                _applicationMutex.Dispose();
                _applicationMutex = null;
            }
            catch (Exception ex)
            {
                LOG.Error("Error releasing Mutex!", ex);
            }
        }

        private static MainForm _instance;

        private readonly CopyData _copyData;

        // Thumbnail preview
        private ThumbnailForm _thumbnailForm;

        // Make sure we have only one settings form
        private SettingsForm _settingsForm;

        // Make sure we have only one about form
        private AboutForm _aboutForm;

        // Timer for the double click test
        private readonly Timer _doubleClickTimer = new Timer();

        public MainForm(CopyDataTransport dataTransport)
        {
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            SimpleServiceProvider.Current.AddService(uiContext);
 
            // The most important form is this
            SimpleServiceProvider.Current.AddService<Form>(this);
            // Also as itself
            SimpleServiceProvider.Current.AddService(this);
            SimpleServiceProvider.Current.AddService<IGreenshotMainForm>(this);
            SimpleServiceProvider.Current.AddService<ICaptureHelper>(this);

            // Windows specific services
            SimpleServiceProvider.Current.AddService<INotificationService>(ToastNotificationService.Create());
            // Set this as IOcrProvider
            SimpleServiceProvider.Current.AddService<IOcrProvider>(new Win10OcrProvider());

            _instance = this;

            EditorInitialize.Initialize();

            // Factory for surface objects
            ISurface SurfaceFactory() => new Surface();

            SimpleServiceProvider.Current.AddService((Func<ISurface>) SurfaceFactory);

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
                ex.Data.Add("more information here", "https://support.microsoft.com/kb/943140");
                throw;
            }

            // Make the main menu available
            SimpleServiceProvider.Current.AddService(contextMenu);

            notifyIcon.Icon = GreenshotResources.GetGreenshotIcon();
            // Make the notify icon available
            SimpleServiceProvider.Current.AddService(notifyIcon);

            // Disable access to the settings, for feature #3521446
            contextmenu_settings.Visible = !_conf.DisableSettings;

            // Make sure all hot-keys pass this window!
            HotkeyControl.RegisterHotkeyHwnd(Handle);
            RegisterHotkeys();

            new ToolTip();

            UpdateUi();

            // This forces the registration of all destinations inside Greenshot itself.
            RegisterInternalDestinations();
            // This forces the registration of all processors inside Greenshot itself.
            RegisterInternalProcessors();

            // Load all the plugins
            PluginHelper.Instance.LoadPlugins();

            // Check to see if there is already another INotificationService
            if (!SimpleServiceProvider.Current.GetAllInstances<INotificationService>().Any())
            {
                // If not we add the internal NotifyIcon notification service
                SimpleServiceProvider.Current.AddService<INotificationService>(new NotifyIconNotificationService());
            }

            // Check destinations, remove all that don't exist
            foreach (string destination in _conf.OutputDestinations.ToArray())
            {
                if (DestinationHelper.GetDestination(destination) == null)
                {
                    _conf.OutputDestinations.Remove(destination);
                }
            }

            // we should have at least one!
            if (_conf.OutputDestinations.Count == 0)
            {
                _conf.OutputDestinations.Add(EditorDestination.DESIGNATION);
            }

            if (_conf.DisableQuickSettings)
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

            if (dataTransport != null)
            {
                HandleDataTransport(dataTransport);
            }

            // Start the update check in the background
            var updateService = new UpdateService();
            updateService.Startup();
            SimpleServiceProvider.Current.AddService(updateService);

            // Make Greenshot use less memory after startup
            if (_conf.MinimizeWorkingSetSize)
            {
                PsApi.EmptyWorkingSet();
            }
        }

        /// <summary>
        /// Create all the internal destinations
        /// </summary>
        private void RegisterInternalDestinations()
        {
            var internalDestinations = new List<IDestination>
            {
                new FileDestination(),
                new FileWithDialogDestination(),
                new ClipboardDestination(),
                new PrinterDestination(),
                new EmailDestination(),
                new PickerDestination(),
                new Win10ShareDestination(),
                new Win10OcrDestination()
            };
            
            bool useEditor = false;
            if (WindowsVersion.IsWindows10OrLater)
            {
                int len = 250;
                var stringBuilder = new StringBuilder(len);
                using var proc = Process.GetCurrentProcess();
                var err = Kernel32Api.GetPackageFullName(proc.Handle, ref len, stringBuilder);
                if (err != 0)
                {
                    useEditor = true;
                }
            } else
            {
                useEditor = true;
            }

            if (useEditor)
            {
                internalDestinations.Add(new EditorDestination());
            }

            foreach (var internalDestination in internalDestinations)
            {
                if (internalDestination.IsActive)
                {
                    SimpleServiceProvider.Current.AddService(internalDestination);
                }
                else
                {
                    internalDestination.Dispose();
                }
            }
        }

        private void RegisterInternalProcessors()
        {
            var internalProcessors = new List<IProcessor>
            {
                new TitleFixProcessor(),
                new Win10OcrProcessor()
            };

            foreach (var internalProcessor in internalProcessors)
            {
                if (internalProcessor.isActive)
                {
                    SimpleServiceProvider.Current.AddService(internalProcessor);
                }
                else
                {
                    internalProcessor.Dispose();
                }
            }
        }

        /// <summary>
        /// DataReceivedEventHandler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="copyDataReceivedEventArgs"></param>
        private void CopyDataDataReceived(object sender, CopyDataReceivedEventArgs copyDataReceivedEventArgs)
        {
            // Cast the data to the type of object we sent:
            var dataTransport = (CopyDataTransport) copyDataReceivedEventArgs.Data;
            HandleDataTransport(dataTransport);
        }

        private void HandleDataTransport(CopyDataTransport dataTransport)
        {
            foreach (KeyValuePair<CommandEnum, string> command in dataTransport.Commands)
            {
                LOG.Debug("Data received, Command = " + command.Key + ", Data: " + command.Value);
                switch (command.Key)
                {
                    case CommandEnum.Exit:
                        LOG.Info("Exit requested");
                        Exit();
                        break;
                    case CommandEnum.FirstLaunch:
                        LOG.Info("FirstLaunch: Created new configuration, showing balloon.");
                        var notifyIconClassicMessageHandler = SimpleServiceProvider.Current.GetInstance<INotificationService>();
                        notifyIconClassicMessageHandler.ShowInfoMessage(
                            Language.GetFormattedString(LangKey.tooltip_firststart, HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.RegionHotkey)), TimeSpan.FromMinutes(10),
                            ShowSetting);
                        break;
                    case CommandEnum.ReloadConfig:
                        LOG.Info("Reload requested");
                        try
                        {
                            IniConfig.Reload();
                            Invoke((MethodInvoker) delegate
                            {
                                // Even update language when needed
                                UpdateUi();
                                // Update the hotkey
                                // Make sure the current hotkeys are disabled
                                HotkeyControl.UnregisterHotkeys();
                                RegisterHotkeys();
                            });
                        }
                        catch (Exception ex)
                        {
                            LOG.Warn("Exception while reloading configuration: ", ex);
                        }

                        break;
                    case CommandEnum.OpenFile:
                        string filename = command.Value;
                        LOG.InfoFormat("Open file requested: {0}", filename);
                        if (File.Exists(filename))
                        {
                            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureFile(filename); });
                        }
                        else
                        {
                            LOG.Warn("No such file: " + filename);
                        }

                        break;
                    default:
                        LOG.Error("Unknown command!");
                        break;
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (HotkeyControl.HandleMessages(ref m))
            {
                return;
            }

            // BUG-1809 prevention, filter the InputLangChange messages
            if (WmInputLangChangeRequestFilter.PreFilterMessageExternal(ref m))
            {
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Helper method to cleanly register a hotkey
        /// </summary>
        /// <param name="failedKeys">StringBuilder</param>
        /// <param name="functionName">string</param>
        /// <param name="hotkeyString">string</param>
        /// <param name="handler">HotKeyHandler</param>
        /// <returns>bool</returns>
        private static bool RegisterHotkey(StringBuilder failedKeys, string functionName, string hotkeyString, HotKeyHandler handler)
        {
            Keys modifierKeyCode = HotkeyControl.HotkeyModifiersFromString(hotkeyString);
            Keys virtualKeyCode = HotkeyControl.HotkeyFromString(hotkeyString);
            if (!Keys.None.Equals(virtualKeyCode))
            {
                if (HotkeyControl.RegisterHotKey(modifierKeyCode, virtualKeyCode, handler) < 0)
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

        private static bool RegisterWrapper(StringBuilder failedKeys, string functionName, string configurationKey, HotKeyHandler handler, bool ignoreFailedRegistration)
        {
            IniValue hotkeyValue = _conf.Values[configurationKey];
            var hotkeyStringValue = hotkeyValue.Value?.ToString();
            if (string.IsNullOrEmpty(hotkeyStringValue))
            {
                return true;
            }

            try
            {
                bool success = RegisterHotkey(failedKeys, functionName, hotkeyStringValue, handler);
                if (!success && ignoreFailedRegistration)
                {
                    LOG.DebugFormat("Ignoring failed hotkey registration for {0}, with value '{1}', resetting to 'None'.", functionName, hotkeyStringValue);
                    _conf.Values[configurationKey].Value = Keys.None.ToString();
                    _conf.IsDirty = true;
                }

                return success;
            }
            catch (Exception ex)
            {
                LOG.Warn(ex);
                LOG.WarnFormat("Restoring default hotkey for {0}, stored under {1} from '{2}' to '{3}'", functionName, configurationKey, hotkeyStringValue,
                    hotkeyValue.Attributes.DefaultValue);
                // when getting an exception the key wasn't found: reset the hotkey value
                hotkeyValue.UseValueOrDefault(null);
                hotkeyValue.ContainingIniSection.IsDirty = true;
                return RegisterHotkey(failedKeys, functionName, hotkeyStringValue, handler);
            }
        }

        /// <summary>
        /// Fix icon reference
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private void OnIconSizeChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IconSize")
            {
                return;
            }

            DpiChangedHandler(96, DeviceDpi);
        }

        /// <summary>
        /// Modify the DPI settings depending in the current value
        /// </summary>
        protected override void DpiChangedHandler(int oldDpi, int newDpi)
        {
            var newSize = DpiCalculator.ScaleWithDpi(coreConfiguration.IconSize, newDpi);
            contextMenu.ImageScalingSize = newSize;
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
            StringBuilder failedKeys = new StringBuilder();

            if (!RegisterWrapper(failedKeys, "CaptureRegion", "RegionHotkey", _instance.CaptureRegion, ignoreFailedRegistration))
            {
                success = false;
            }

            if (!RegisterWrapper(failedKeys, "CaptureWindow", "WindowHotkey", _instance.CaptureWindow, ignoreFailedRegistration))
            {
                success = false;
            }

            if (!RegisterWrapper(failedKeys, "CaptureFullScreen", "FullscreenHotkey", _instance.CaptureFullScreen, ignoreFailedRegistration))
            {
                success = false;
            }

            if (!RegisterWrapper(failedKeys, "CaptureLastRegion", "LastregionHotkey", _instance.CaptureLastRegion, ignoreFailedRegistration))
            {
                success = false;
            }

            if (!RegisterWrapper(failedKeys, "CaptureClipboard", "ClipboardHotkey", _instance.CaptureClipboard, true))
            {
                success = false;
            }

            if (!success)
            {
                if (!ignoreFailedRegistration)
                {
                    success = HandleFailedHotkeyRegistration(failedKeys.ToString());
                }
                else
                {
                    // if failures have been ignored, the config has probably been updated
                    if (_conf.IsDirty)
                    {
                        IniConfig.Save();
                    }
                }
            }

            return success || ignoreFailedRegistration;
        }

        /// <summary>
        /// Check if OneDrive is blocking hotkeys
        /// </summary>
        /// <returns>true if one-drive has hotkeys turned on</returns>
        private static bool IsOneDriveBlockingHotkey()
        {
            if (!WindowsVersion.IsWindows10OrLater)
            {
                return false;
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var oneDriveSettingsPath = Path.Combine(localAppData, @"Microsoft\OneDrive\settings\Personal");
            if (!Directory.Exists(oneDriveSettingsPath))
            {
                return false;
            }

            var oneDriveSettingsFile = Directory.GetFiles(oneDriveSettingsPath, "*_screenshot.dat").FirstOrDefault();
            if (!File.Exists(oneDriveSettingsFile))
            {
                return false;
            }

            var screenshotSetting = File.ReadAllLines(oneDriveSettingsFile).Skip(1).Take(1).First();
            return "2".Equals(screenshotSetting);
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
            var warningTitle = Language.GetString(LangKey.warning);
            var message = string.Format(Language.GetString(LangKey.warning_hotkeys), failedKeys, IsOneDriveBlockingHotkey() ? " (OneDrive)" : "");
            var mainForm = SimpleServiceProvider.Current.GetInstance<MainForm>();
            DialogResult dr = MessageBox.Show(mainForm, message, warningTitle, MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Exclamation);
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

        public void UpdateUi()
        {
            // As the form is never loaded, call ApplyLanguage ourselves
            ApplyLanguage();

            // Show hotkeys in Contextmenu
            contextmenu_capturearea.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.RegionHotkey);
            contextmenu_capturelastregion.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.LastregionHotkey);
            contextmenu_capturewindow.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.WindowHotkey);
            contextmenu_capturefullscreen.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.FullscreenHotkey);
            var clipboardHotkey = HotkeyControl.GetLocalizedHotkeyStringFromString(_conf.ClipboardHotkey);
            if (!string.IsNullOrEmpty(clipboardHotkey) && !"None".Equals(clipboardHotkey))
            {
                contextmenu_captureclipboard.ShortcutKeyDisplayString = clipboardHotkey;
            }
        }


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
            
            // TODO: Do we really need this?
            //using var loProcess = Process.GetCurrentProcess();
            //loProcess.MaxWorkingSet = (IntPtr)750000;
            //loProcess.MinWorkingSet = (IntPtr)300000;
        }

        private void CaptureRegion()
        {
            CaptureHelper.CaptureRegion(true);
        }

        private void CaptureFile(IDestination destination = null)
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            var extensions = fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.LoadFromFile).Select(e => $"*{e}").ToList();

            var openFileDialog = new OpenFileDialog
            {
                Filter = @$"Image files ({string.Join(", ", extensions)})|{string.Join("; ", extensions)}"
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            if (File.Exists(openFileDialog.FileName))
            {
                CaptureHelper.CaptureFile(openFileDialog.FileName, destination);
            }
        }

        private void CaptureFullScreen()
        {
            CaptureHelper.CaptureFullscreen(true, _conf.ScreenCaptureMode);
        }

        private void CaptureLastRegion()
        {
            CaptureHelper.CaptureLastRegion(true);
        }

        /// <summary>
        /// This is used by the hotkey trigger
        /// </summary>
        private void CaptureClipboard()
        {
            CaptureHelper.CaptureClipboard(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
        }

        private void CaptureWindow()
        {
            if (_conf.CaptureWindowsInteractive)
            {
                CaptureHelper.CaptureWindowInteractive(true);
            }
            else
            {
                CaptureHelper.CaptureWindow(true);
            }
        }


        private void ContextMenuOpening(object sender, CancelEventArgs e)
        {
            var factor = DeviceDpi / 96f;
            contextMenu.Scale(new SizeF(factor, factor));
            contextmenu_captureclipboard.Enabled = ClipboardHelper.ContainsImage();
            contextmenu_capturelastregion.Enabled = coreConfiguration.LastCapturedRegion != NativeRect.Empty;

            // Multi-Screen captures
            contextmenu_capturefullscreen.Click -= CaptureFullScreenToolStripMenuItemClick;
            contextmenu_capturefullscreen.DropDownOpening -= MultiScreenDropDownOpening;
            contextmenu_capturefullscreen.DropDownClosed -= MultiScreenDropDownClosing;
            if (Screen.AllScreens.Length > 1)
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
                var resources = new ComponentResourceManager(typeof(MainForm));
                contextmenu_donate.Image = (Image) resources.GetObject("contextmenu_present.Image");
            }
        }

        private void ContextMenuClosing(object sender, EventArgs e)
        {
            contextmenu_capturewindowfromlist.DropDownItems.Clear();
            CleanupThumbnail();
        }

        
        /// <summary>
        /// MultiScreenDropDownOpening is called when mouse hovers over the Capture-Screen context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MultiScreenDropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem captureScreenMenuItem = (ToolStripMenuItem) sender;
            captureScreenMenuItem.DropDownItems.Clear();
            if (DisplayInfo.AllDisplayInfos.Length <= 1) return;

            var allScreensBounds = DisplayInfo.ScreenBounds;

            var captureScreenItem = new ToolStripMenuItem(Language.GetString(LangKey.contextmenu_capturefullscreen_all));
            captureScreenItem.Click += delegate {
                BeginInvoke((MethodInvoker) delegate {
                    CaptureHelper.CaptureFullscreen(false, ScreenCaptureMode.FullScreen);
                });
            };

            captureScreenMenuItem.DropDownItems.Add(captureScreenItem);
            foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
            {
                var displayToCapture = displayInfo;
                string deviceAlignment = displayToCapture.DeviceName;
                    
                if (displayInfo.Bounds.Top == allScreensBounds.Top && displayInfo.Bounds.Bottom != allScreensBounds.Bottom)
                {
                    deviceAlignment += " " + Language.GetString(LangKey.contextmenu_capturefullscreen_top);
                }
                else if (displayInfo.Bounds.Top != allScreensBounds.Top && displayInfo.Bounds.Bottom == allScreensBounds.Bottom)
                {
                    deviceAlignment += " " + Language.GetString(LangKey.contextmenu_capturefullscreen_bottom);
                }

                if (displayInfo.Bounds.Left == allScreensBounds.Left && displayInfo.Bounds.Right != allScreensBounds.Right)
                {
                    deviceAlignment += " " + Language.GetString(LangKey.contextmenu_capturefullscreen_left);
                }
                else if (displayInfo.Bounds.Left != allScreensBounds.Left && displayInfo.Bounds.Right == allScreensBounds.Right)
                {
                    deviceAlignment += " " + Language.GetString(LangKey.contextmenu_capturefullscreen_right);
                }

                captureScreenItem = new ToolStripMenuItem(deviceAlignment);
                captureScreenItem.Click += delegate
                {
                    BeginInvoke((MethodInvoker) delegate
                    {
                        CaptureHelper.CaptureRegion(false, displayToCapture.Bounds);
                    });
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
            AddCaptureWindowMenuItems(captureWindowFromListMenuItem, Contextmenu_CaptureWindowFromList_Click);
        }

        private void CaptureWindowFromListMenuDropDownClosed(object sender, EventArgs e)
        {
            CleanupThumbnail();
        }

        private void ShowThumbnailOnEnter(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem captureWindowItem) return;
            var window = captureWindowItem.Tag as WindowDetails;
            if (_thumbnailForm == null)
            {
                _thumbnailForm = new ThumbnailForm();
            }

            _thumbnailForm.ShowThumbnail(window, captureWindowItem.GetCurrentParent().TopLevelControl);
        }

        private void HideThumbnailOnLeave(object sender, EventArgs e)
        {
            _thumbnailForm?.Hide();
        }

        private void CleanupThumbnail()
        {
            if (_thumbnailForm == null)
            {
                return;
            }

            _thumbnailForm.Close();
            _thumbnailForm = null;
        }

        /// <summary>
        /// Create the "capture window from list" list
        /// </summary>
        /// <param name="menuItem">ToolStripMenuItem</param>
        /// <param name="eventHandler">EventHandler</param>
        public void AddCaptureWindowMenuItems(ToolStripMenuItem menuItem, EventHandler eventHandler)
        {
            menuItem.DropDownItems.Clear();
            // check if thumbnailPreview is enabled and DWM is enabled
            bool thumbnailPreview = _conf.ThumnailPreview && DwmApi.IsDwmEnabled;

            foreach (var window in WindowDetails.GetTopLevelWindows())
            {
                if (LOG.IsDebugEnabled)
                {
                    LOG.Debug(window.ToString());
                }

                string title = window.Text;
                if (string.IsNullOrEmpty(title))
                {
                    continue;
                }

                if (title.Length > _conf.MaxMenuItemLength)
                {
                    title = title.Substring(0, Math.Min(title.Length, _conf.MaxMenuItemLength));
                }

                ToolStripItem captureWindowItem = menuItem.DropDownItems.Add(title);
                captureWindowItem.Tag = window;
                captureWindowItem.Click += eventHandler;
                // Dispose the icon when the menu item is disposed to prevent memory leaks
                captureWindowItem.AssignAutoDisposingImage(window?.DisplayIcon, needsClone: false);
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
            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureRegion(false); });
        }

        private void CaptureClipboardToolStripMenuItemClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureClipboard(); });
        }

        private void OpenFileToolStripMenuItemClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { CaptureFile(); });
        }

        private void CaptureFullScreenToolStripMenuItemClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureFullscreen(false, _conf.ScreenCaptureMode); });
        }

        private void Contextmenu_CaptureLastRegionClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureLastRegion(false); });
        }

        private void Contextmenu_CaptureWindow_Click(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureWindowInteractive(false); });
        }

        private void Contextmenu_CaptureWindowFromList_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem) sender;
            BeginInvoke((MethodInvoker) delegate
            {
                try
                {
                    WindowDetails windowToCapture = (WindowDetails) clickedItem.Tag;
                    CaptureHelper.CaptureWindow(windowToCapture);
                }
                catch (Exception exception)
                {
                    LOG.Error(exception);
                }
            });
        }

        /// <summary>
        /// Context menu entry "Support Greenshot"
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        private void Contextmenu_DonateClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { Process.Start("https://getgreenshot.org/support/?version=" + EnvironmentInfo.GetGreenshotVersion(true)); });
        }

        /// <summary>
        /// Context menu entry "Preferences"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contextmenu_SettingsClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) ShowSetting);
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
        private void Contextmenu_AboutClick(object sender, EventArgs e)
        {
            ShowAbout();
        }

        public void ShowAbout()
        {
            if (_aboutForm != null)
            {
                WindowDetails.ToForeground(_aboutForm.Handle);
            }
            else
            {
                try
                {
                    using (_aboutForm = new AboutForm())
                    {
                        _aboutForm.ShowDialog(this);
                    }
                }
                finally
                {
                    _aboutForm = null;
                }
            }
        }

        /// <summary>
        /// The "Help" entry is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contextmenu_HelpClick(object sender, EventArgs e)
        {
            HelpFileLoader.LoadHelp();
        }

        /// <summary>
        /// The "Exit" entry is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contextmenu_ExitClick(object sender, EventArgs e)
        {
            Exit();
        }

        private void CheckStateChangedHandler(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuSelectListItem captureMouseItem)
            {
                _conf.CaptureMousepointer = captureMouseItem.Checked;
            }
        }

        /// <summary>
        /// This needs to be called to initialize the quick settings menu entries
        /// </summary>
        private void InitializeQuickSettingsMenu()
        {
            contextmenu_quicksettings.DropDownItems.Clear();

            if (_conf.DisableQuickSettings)
            {
                return;
            }

            // Only add if the value is not fixed
            if (!_conf.Values["CaptureMousepointer"].IsFixed)
            {
                // For the capture mouse-cursor option
                ToolStripMenuSelectListItem captureMouseItem = new ToolStripMenuSelectListItem
                {
                    Text = Language.GetString("settings_capture_mousepointer"),
                    Checked = _conf.CaptureMousepointer,
                    CheckOnClick = true
                };
                captureMouseItem.CheckStateChanged += CheckStateChangedHandler;

                contextmenu_quicksettings.DropDownItems.Add(captureMouseItem);
            }

            ToolStripMenuSelectList selectList;
            if (!_conf.Values["Destinations"].IsFixed)
            {
                // screenshot destination
                selectList = new ToolStripMenuSelectList("destinations", true, this)
                {
                    Text = Language.GetString(LangKey.settings_destination)
                };
                // Working with IDestination:
                foreach (var destination in DestinationHelper.GetAllDestinations())
                {
                    selectList.AddItem(destination.Description, destination, _conf.OutputDestinations.Contains(destination.Designation));
                }

                selectList.CheckedChanged += QuickSettingDestinationChanged;
                contextmenu_quicksettings.DropDownItems.Add(selectList);
            }

            if (!_conf.Values["WindowCaptureMode"].IsFixed)
            {
                // Capture Modes
                selectList = new ToolStripMenuSelectList("capturemodes", false, this)
                {
                    Text = Language.GetString(LangKey.settings_window_capture_mode)
                };
                string enumTypeName = typeof(WindowCaptureMode).Name;
                foreach (WindowCaptureMode captureMode in Enum.GetValues(typeof(WindowCaptureMode)))
                {
                    selectList.AddItem(Language.GetString(enumTypeName + "." + captureMode), captureMode, _conf.WindowCaptureMode == captureMode);
                }

                selectList.CheckedChanged += QuickSettingCaptureModeChanged;
                contextmenu_quicksettings.DropDownItems.Add(selectList);
            }

            // print options
            selectList = new ToolStripMenuSelectList("printoptions", true, this)
            {
                Text = Language.GetString(LangKey.settings_printoptions)
            };

            IniValue iniValue;
            foreach (string propertyName in _conf.Values.Keys)
            {
                if (propertyName.StartsWith("OutputPrint"))
                {
                    iniValue = _conf.Values[propertyName];
                    if (iniValue.Attributes.LanguageKey != null && !iniValue.IsFixed)
                    {
                        selectList.AddItem(Language.GetString(iniValue.Attributes.LanguageKey), iniValue, (bool) iniValue.Value);
                    }
                }
            }

            if (selectList.DropDownItems.Count > 0)
            {
                selectList.CheckedChanged += QuickSettingBoolItemChanged;
                contextmenu_quicksettings.DropDownItems.Add(selectList);
            }

            // effects
            selectList = new ToolStripMenuSelectList("effects", true, this)
            {
                Text = Language.GetString(LangKey.settings_visualization)
            };

            iniValue = _conf.Values["PlayCameraSound"];
            if (!iniValue.IsFixed)
            {
                selectList.AddItem(Language.GetString(iniValue.Attributes.LanguageKey), iniValue, (bool) iniValue.Value);
            }

            iniValue = _conf.Values["ShowTrayNotification"];
            if (!iniValue.IsFixed)
            {
                selectList.AddItem(Language.GetString(iniValue.Attributes.LanguageKey), iniValue, (bool) iniValue.Value);
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
                _conf.WindowCaptureMode = windowsCaptureMode;
            }
        }

        private void QuickSettingBoolItemChanged(object sender, EventArgs e)
        {
            ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs) e).Item;
            if (item.Data is IniValue iniValue)
            {
                iniValue.Value = item.Checked;
                IniConfig.Save();
            }
        }

        private void QuickSettingDestinationChanged(object sender, EventArgs e)
        {
            ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs) e).Item;
            IDestination selectedDestination = (IDestination) item.Data;
            if (item.Checked)
            {
                if (selectedDestination.Designation.Equals(nameof(WellKnownDestinations.Picker)))
                {
                    // If the item is the destination picker, remove all others
                    _conf.OutputDestinations.Clear();
                }
                else
                {
                    // If the item is not the destination picker, remove the picker
                    _conf.OutputDestinations.Remove(nameof(WellKnownDestinations.Picker));
                }

                // Checked an item, add if the destination is not yet selected
                if (!_conf.OutputDestinations.Contains(selectedDestination.Designation))
                {
                    _conf.OutputDestinations.Add(selectedDestination.Designation);
                }
            }
            else
            {
                // deselected a destination, only remove if it was selected
                if (_conf.OutputDestinations.Contains(selectedDestination.Designation))
                {
                    _conf.OutputDestinations.Remove(selectedDestination.Designation);
                }
            }

            // Check if something was selected, if not make the picker the default
            if (_conf.OutputDestinations == null || _conf.OutputDestinations.Count == 0)
            {
                _conf.OutputDestinations.Add(nameof(WellKnownDestinations.Picker));
            }

            IniConfig.Save();

            // Rebuild the quick settings menu with the new settings.
            InitializeQuickSettingsMenu();
        }

        private static void Task_UnhandledException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            try
            {
                Exception exceptionToLog = args.Exception;
                string exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
                LOG.Error("Exception caught in the UnobservedTaskException handler.");
                LOG.Error(exceptionText);
                new BugReportForm(exceptionText).ShowDialog();
            }
            finally
            {
                args.SetObserved();
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
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

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
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
        private void NotifyIconClickTest(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // The right button will automatically be handled with the context menu, here we only check the left.
            if (_conf.DoubleClickAction == ClickActions.DO_NOTHING)
            {
                // As there isn't a double-click we can start the Left click
                NotifyIconClick(_conf.LeftClickAction);
                // ready with the test
                return;
            }

            // If the timer is enabled we are waiting for a double click...
            if (_doubleClickTimer.Enabled)
            {
                // User clicked a second time before the timer tick: Double-click!
                _doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
                _doubleClickTimer.Stop();
                NotifyIconClick(_conf.DoubleClickAction);
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
        private void NotifyIconSingleClickTest(object sender, EventArgs e)
        {
            _doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
            _doubleClickTimer.Stop();
            BeginInvoke((MethodInvoker) delegate { NotifyIconClick(_conf.LeftClickAction); });
        }

        /// <summary>
        /// Handle the notify icon click
        /// </summary>
        private void NotifyIconClick(ClickActions clickAction)
        {
            switch (clickAction)
            {
                case ClickActions.OPEN_LAST_IN_EXPLORER:
                    Contextmenu_OpenRecent(this, null);
                    break;
                case ClickActions.OPEN_LAST_IN_EDITOR:
                    _conf.ValidateAndCorrectOutputFileAsFullpath();

                    if (File.Exists(_conf.OutputFileAsFullpath))
                    {
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
                case ClickActions.CAPTURE_CLIPBOARD:
                    CaptureHelper.CaptureClipboard();
                    break;
                case ClickActions.OPEN_CLIPBOARD_IN_EDITOR:
                    CaptureHelper.CaptureClipboard(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
                    break;
                case ClickActions.OPEN_FILE_IN_EDITOR:
                    CaptureFile(DestinationHelper.GetDestination(EditorDestination.DESIGNATION));
                    break;
                case ClickActions.CAPTURE_REGION:
                    CaptureHelper.CaptureRegion(false);
                    break;
                case ClickActions.CAPTURE_SCREEN:
                    CaptureHelper.CaptureFullscreen(false, ScreenCaptureMode.FullScreen);
                    break;
                case ClickActions.CAPTURE_WINDOW:
                    CaptureHelper.CaptureWindowInteractive(false);
                    break;
                case ClickActions.OPEN_EMPTY_EDITOR:
                    var imageEditor = new ImageEditorForm();
                    imageEditor.Show();
                    imageEditor.Activate();
                    break;
            }
        }

        /// <summary>
        /// The Contextmenu_OpenRecent currently opens the last know save location
        /// </summary>
        private void Contextmenu_OpenRecent(object sender, EventArgs eventArgs)
        {
            _conf.ValidateAndCorrectOutputFilePath();
            _conf.ValidateAndCorrectOutputFileAsFullpath();
            string path = _conf.OutputFileAsFullpath;
            if (!File.Exists(path))
            {
                path = FilenameHelper.FillVariables(_conf.OutputFilePath, false);
                // Fix for #1470, problems with a drive which is no longer available
                try
                {
                    string lastFilePath = Path.GetDirectoryName(_conf.OutputFileAsFullpath);

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
            }

            try
            {
                ExplorerHelper.OpenInExplorer(path);
            }
            catch (Exception ex)
            {
                // Make sure we show what we tried to open in the exception
                ex.Data["path"] = path;
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
            LOG.Info("Exit: " + EnvironmentInfo.EnvironmentToString(false));

            // Close all open forms (except this), use a separate List to make sure we don't get a "InvalidOperationException: Collection was modified"
            List<Form> formsToClose = new List<Form>();
            foreach (Form form in Application.OpenForms)
            {
                if (form.Handle != Handle && form.GetType() != typeof(ImageEditorForm))
                {
                    formsToClose.Add(form);
                }
            }

            foreach (Form form in formsToClose)
            {
                try
                {
                    LOG.InfoFormat("Closing form: {0}", form.Name);
                    Form formCapturedVariable = form;
                    Invoke((MethodInvoker) delegate { formCapturedVariable.Close(); });
                }
                catch (Exception e)
                {
                    LOG.Error("Error closing form!", e);
                }
            }

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
                PluginHelper.Instance.Shutdown();
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

            ImageIO.RemoveTmpFiles();

            // Store any open configuration changes
            try
            {
                IniConfig.Save();
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
        /// TODO: Delete when the ICaptureHelper can be solve someway else
        /// </summary>
        /// <param name="windowToCapture">WindowDetails</param>
        /// <returns>WindowDetails</returns>
        public WindowDetails SelectCaptureWindow(WindowDetails windowToCapture)
        {
            return CaptureHelper.SelectCaptureWindow(windowToCapture);
        }

        /// <summary>
        /// TODO: Delete when the ICaptureHelper can be solve someway else
        /// </summary>
        /// <param name="windowToCapture">WindowDetails</param>
        /// <param name="capture">ICapture</param>
        /// <param name="coreConfigurationWindowCaptureMode">WindowCaptureMode</param>
        /// <returns>ICapture</returns>
        public ICapture CaptureWindow(WindowDetails windowToCapture, ICapture capture, WindowCaptureMode coreConfigurationWindowCaptureMode)
        {
            return CaptureHelper.CaptureWindow(windowToCapture, capture, coreConfigurationWindowCaptureMode);
        }
    }
}