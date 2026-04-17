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
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Threading;
using Dapplo.Ini;
using Dapplo.Ini.Interfaces;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Messages;
using Dapplo.Windows.User32;
using Greenshot.Base;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Help;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Configuration;
using Greenshot.Controls;
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
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainForm));
        private static ResourceMutex _applicationMutex;
        private static ICoreConfiguration _conf = IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <summary>
        /// Application entry-point, called from <see cref="GreenshotMain"/> after the
        /// <see cref="IniConfigRegistry"/> has been set up and command-line arguments
        /// have been parsed.
        /// </summary>
        public static void Start(CommandLineOptions options)
        {
            try
            {
                // Fix for Bug 2495900, Multi-user Environment
                // check whether there's an local instance running already
                _applicationMutex = ResourceMutex.Create("F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08", "Greenshot", false);

                var isAlreadyRunning = !_applicationMutex.IsLocked;

                if (options.Exit)
                {
                    // un-register application on uninstall (allow uninstall)
                    try
                    {
                        Log.Info("Sending all instances the exit command.");
                        // Pass Exit to running instance, if any
                        SendData(new CopyDataTransport(CommandEnum.Exit));
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Exception by exit.", e);
                    }

                    FreeMutex();
                    return;
                }

                if (options.Reload)
                {
                    // Modify configuration
                    Log.Info("Reloading configuration!");
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
                }

                if (isAlreadyRunning)
                {
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
                    // We didn't initialize the language yet, do it here just for the message box
                    if (transport.Commands.Count > 0)
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
                                Log.Debug(ex);
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

                // Make sure we handle END Session correctly
                RestartManagerHelper.RegisterForRestart();

                // Make sure we can use forms
                WindowsFormsHost.EnableWindowsFormsInterop();

                // BUG-1809: Add message filter, to filter out all the InputLangChanged messages which go to a target control with a handle > 32 bit.
                Application.AddMessageFilter(new WmInputLangChangeRequestFilter());

                // From here on we continue starting Greenshot
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.ApplicationExit += Application_ApplicationExit;

                Application.Run(new MainForm(options));
            }
            catch (Exception ex)
            {
                Log.Error("Exception in startup.", ex);
                GreenshotMain.Application_ThreadException(ActiveForm, new ThreadExceptionEventArgs(ex));
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            FreeMutex();
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
                Log.Error("Error releasing Mutex!", ex);
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

        public MainForm(CommandLineOptions options)
        {

            SimpleServiceProvider.Current.AddService(SynchronizationContext.Current);
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            SimpleServiceProvider.Current.AddService(uiContext);

            // Register the RecyclableMemoryStreamManager to minimise Large Object Heap usage.
            SimpleServiceProvider.Current.AddService(RecyclableMemoryStreamFactory.Manager);
 
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

            // Load all the plugins, and while doing to load the configuration
            PluginHelper.Instance.LoadPlugins();

            // This forces the registration of all destinations inside Greenshot itself.
            RegisterInternalDestinations();
            // This forces the registration of all processors inside Greenshot itself.
            RegisterInternalProcessors();

            // if language is not set, show language dialog
            if (string.IsNullOrEmpty(_conf.Language))
            {
                LanguageDialog languageDialog = LanguageDialog.GetInstance();
                languageDialog.ShowDialog();
                _conf.Language = languageDialog.SelectedLanguage;
            }

            // Disable access to the settings, for feature #3521446
            contextmenu_settings.Visible = !_conf.DisableSettings;

            HotkeyHelper.RegisterHotkeys();

            new ToolTip();

            UpdateUi();

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
            WindowDetails.RegisterIgnoreHandle(SharedMessageWindow.Handle);

            // Create a new instance of the class: copyData = new CopyData();
            _copyData = new CopyData();

            // Assign the handle:
            _copyData.AssignHandle(SharedMessageWindow.Handle);
            // Create the channel to send on:
            _copyData.Channels.Add("Greenshot");
            // Hook up received event:
            _copyData.CopyDataReceived += CopyDataDataReceived;

            if (options.Restore)
            {
                RestartManagerHelper.RestoreState();
            }
            // Check if it's the first time launch?
            if (_conf.IsFirstLaunch)
            {
                ApplicationStartupHelper.FirstLaunch();
            }

            if (options.Files.Length > 0)
            {
                // Default behavior was to open only one file (which is not correct)
                ApplicationStartupHelper.OpenFile(options.Files.First());
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
                Log.Debug("Data received, Command = " + command.Key + ", Data: " + command.Value);
                switch (command.Key)
                {
                    case CommandEnum.Exit:
                        Log.Info("Exit requested");
                        Exit();
                        break;
                    case CommandEnum.FirstLaunch:
                        ApplicationStartupHelper.FirstLaunch();
                        break;
                    case CommandEnum.ReloadConfig:
                        ApplicationStartupHelper.ReloadConfig();
                        break;
                    case CommandEnum.OpenFile:
                        ApplicationStartupHelper.OpenFile(command.Value);
                        break;
                    default:
                        Log.Error("Unknown command!");
                        break;
                }
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

        public void UpdateUi()
        {
            // As the form is never loaded, call ApplyLanguage ourselves
            ApplyLanguage();

            // Show hotkeys in Contextmenu
            contextmenu_capturearea.ShortcutKeyDisplayString = HotkeyManager.GetLocalizedHotkeyStringFromString(_conf.RegionHotkey);
            contextmenu_capturelastregion.ShortcutKeyDisplayString = HotkeyManager.GetLocalizedHotkeyStringFromString(_conf.LastregionHotkey);
            contextmenu_capturewindow.ShortcutKeyDisplayString = HotkeyManager.GetLocalizedHotkeyStringFromString(_conf.WindowHotkey);
            contextmenu_capturefullscreen.ShortcutKeyDisplayString = HotkeyManager.GetLocalizedHotkeyStringFromString(_conf.FullscreenHotkey);
            var clipboardHotkey = HotkeyManager.GetLocalizedHotkeyStringFromString(_conf.ClipboardHotkey);
            if (!string.IsNullOrEmpty(clipboardHotkey) && !"None".Equals(clipboardHotkey))
            {
                contextmenu_captureclipboard.ShortcutKeyDisplayString = clipboardHotkey;
            }
        }


        private void MainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            Log.DebugFormat("Mainform closing, reason: {0}", e.CloseReason);
            Exit();
        }

        private void MainFormActivated(object sender, EventArgs e)
        {
            Hide();
            ShowInTaskbar = false;
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
                Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                {
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
                    Dispatcher.CurrentDispatcher.BeginInvoke(()=>
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
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(window.ToString());
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
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                CaptureHelper.CaptureRegion(false);
            });
        }

        private void CaptureClipboardToolStripMenuItemClick(object sender, EventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                CaptureHelper.CaptureClipboard();
            });
        }

        private void OpenFileToolStripMenuItemClick(object sender, EventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                CaptureFile();
            });
        }

        private void CaptureFullScreenToolStripMenuItemClick(object sender, EventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                CaptureHelper.CaptureFullscreen(false, _conf.ScreenCaptureMode);
            });
        }

        private void Contextmenu_CaptureLastRegionClick(object sender, EventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                CaptureHelper.CaptureLastRegion(false);
            });
        }

        private void Contextmenu_CaptureWindow_Click(object sender, EventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                CaptureHelper.CaptureWindowInteractive(false);
            });
        }

        private void Contextmenu_CaptureWindowFromList_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem) sender;
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                try
                {
                    WindowDetails windowToCapture = (WindowDetails)clickedItem.Tag;
                    CaptureHelper.CaptureWindow(windowToCapture);
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
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
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                Process.Start("https://getgreenshot.org/support/?version=" + EnvironmentInfo.GetGreenshotVersion(true));
            });
        }

        /// <summary>
        /// Context menu entry "Preferences"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contextmenu_SettingsClick(object sender, EventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                ShowSetting();
            });
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

            var coreSection = IniConfigRegistry.GetSection<ICoreConfiguration>();

            // Only add if the value is not fixed
            if (coreSection == null || !coreSection.IsConstant("CaptureMousepointer"))
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
            if (coreSection == null || !coreSection.IsConstant("Destinations"))
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

            if (coreSection == null || !coreSection.IsConstant("WindowCaptureMode"))
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

            AddBoolMenuItem(selectList, coreSection, "OutputPrintPromptOptions", "settings_alwaysshowprintoptionsdialog", v => _conf.OutputPrintPromptOptions = v, _conf.OutputPrintPromptOptions);
            AddBoolMenuItem(selectList, coreSection, "OutputPrintAllowRotate", "printoptions_allowrotate", v => _conf.OutputPrintAllowRotate = v, _conf.OutputPrintAllowRotate);
            AddBoolMenuItem(selectList, coreSection, "OutputPrintAllowEnlarge", "printoptions_allowenlarge", v => _conf.OutputPrintAllowEnlarge = v, _conf.OutputPrintAllowEnlarge);
            AddBoolMenuItem(selectList, coreSection, "OutputPrintAllowShrink", "printoptions_allowshrink", v => _conf.OutputPrintAllowShrink = v, _conf.OutputPrintAllowShrink);
            AddBoolMenuItem(selectList, coreSection, "OutputPrintCenter", "printoptions_allowcenter", v => _conf.OutputPrintCenter = v, _conf.OutputPrintCenter);
            AddBoolMenuItem(selectList, coreSection, "OutputPrintInverted", "printoptions_inverted", v => _conf.OutputPrintInverted = v, _conf.OutputPrintInverted);
            AddBoolMenuItem(selectList, coreSection, "OutputPrintGrayscale", "printoptions_printgrayscale", v => _conf.OutputPrintGrayscale = v, _conf.OutputPrintGrayscale);
            AddBoolMenuItem(selectList, coreSection, "OutputPrintMonochrome", "printoptions_printmonochrome", v => _conf.OutputPrintMonochrome = v, _conf.OutputPrintMonochrome);
            AddBoolMenuItem(selectList, coreSection, "OutputPrintFooter", "printoptions_timestamp", v => _conf.OutputPrintFooter = v, _conf.OutputPrintFooter);

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

            AddBoolMenuItem(selectList, coreSection, "PlayCameraSound", "settings_playsound", v => _conf.PlayCameraSound = v, _conf.PlayCameraSound);
            AddBoolMenuItem(selectList, coreSection, "ShowTrayNotification", "settings_shownotify", v => _conf.ShowTrayNotification = v, _conf.ShowTrayNotification);

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

        /// <summary>
        /// Adds a bool menu item to a <see cref="ToolStripMenuSelectList"/> for a config property,
        /// skipping it when the property is marked as constant (admin-enforced).
        /// </summary>
        private static void AddBoolMenuItem(
            ToolStripMenuSelectList list,
            IIniSection section,
            string propertyName,
            string langKey,
            Action<bool> setter,
            bool currentValue)
        {
            if (section != null && section.IsConstant(propertyName))
            {
                return;
            }

            list.AddItem(Language.GetString(langKey), setter, currentValue);
        }

        private void QuickSettingBoolItemChanged(object sender, EventArgs e)
        {
            ToolStripMenuSelectListItem item = ((ItemCheckedChangedEventArgs) e).Item;
            if (item.Data is Action<bool> setter)
            {
                setter(item.Checked);
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

            // Rebuild the quick settings menu with the new settings.
            InitializeQuickSettingsMenu();
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
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                NotifyIconClick(_conf.LeftClickAction);
            });
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
                    Log.Warn("Couldn't open the path to the last exported file, taking default.", ex);
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
                Log.Warn("Couldn't open the path to the last exported file", ex);
                // No reason to create a bug-form, we just display the error.
                MessageBox.Show(this, ex.Message, "Opening " + path, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shutdown / cleanup
        /// </summary>
        public void Exit()
        {
            Log.Info("Exit: " + EnvironmentInfo.EnvironmentToString(false));

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
                    Log.InfoFormat("Closing form: {0}", form.Name);
                    Form formCapturedVariable = form;
                    Invoke((MethodInvoker) delegate { formCapturedVariable.Close(); });
                }
                catch (Exception e)
                {
                    Log.Error("Error closing form!", e);
                }
            }

            // Make sure hotkeys are disabled
            try
            {
                HotkeyManager.UnregisterHotkeys();
            }
            catch (Exception e)
            {
                Log.Error("Error unregistering hotkeys!", e);
            }

            // Now the sound isn't needed anymore
            try
            {
                SoundHelper.Deinitialize();
            }
            catch (Exception e)
            {
                Log.Error("Error deinitializing sound!", e);
            }

            // Inform all registered plugins
            try
            {
                PluginHelper.Instance.Shutdown();
            }
            catch (Exception e)
            {
                Log.Error("Error shutting down plugins!", e);
            }

            // Graceful shutdown
            try
            {
                Application.DoEvents();
                Application.Exit();
            }
            catch (Exception e)
            {
                Log.Error("Error closing application!", e);
            }

            ImageIO.RemoveTmpFiles();

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

        protected override void WndProc(ref Message m)
        {
            if (!WndProcDefaults.TryHandleMessage(ref m))
            {
                base.WndProc(ref m);
            }
        }
    }
}