/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Interop;
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
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Help;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Threading;
using Greenshot.Configuration;
using Greenshot.Controls;
using Greenshot.Destinations;
using Greenshot.Editor;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using Greenshot.Forms.Wpf;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Helpers;
using Greenshot.Helpers.Ipc;
using Greenshot.Pipeline;
using Greenshot.Plugin.Win10;
using Greenshot.Processors;
using Greenshot.Recipes;
using Greenshot.Triggers;
using Greenshot.UI;
using log4net;

using Timer = System.Timers.Timer;
using Greenshot.Base.Native;

namespace Greenshot.Forms
{
    /// <summary>
    /// This is the MainForm, the shell of Greenshot
    /// </summary>
    public partial class MainForm : GreenshotForm, IGreenshotMainForm, ICaptureHelper, IProvideDeviceDpi
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainForm));
        private static ResourceMutex _applicationMutex;
        private static ICoreConfiguration _conf => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());

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

                // A command (e.g. a file, --recipe, --reload, --exit) is handled exactly like one from greenshot.com:
                // the unparsed arguments are sent as a CLI request and parsed by the running Greenshot
                IpcEnvelope startupCommand = null;
                if (options.CommandArguments.Length > 0)
                {
                    var parsed = CliCommandParser.Parse(options.CommandArguments, IpcSources.Cli, Environment.CurrentDirectory);
                    if (!parsed.Success)
                    {
                        Log.Warn($"Invalid command line: {parsed.Error}");
                        GreenshotCommandLine.ReportError(parsed.Error);
                        FreeMutex();
                        return;
                    }

                    startupCommand = IpcEnvelope.CreateCli(options.CommandArguments, IpcSources.Cli, Environment.CurrentDirectory);
                    if (isAlreadyRunning)
                    {
                        Log.Info($"Sending the command '{parsed.Envelope.Command}' to the running Greenshot.");
                        NamedPipeClient.SendMessage(startupCommand);
                        FreeMutex();
                        return;
                    }

                    // Nothing to exit or to reload when Greenshot is not running
                    if (parsed.Envelope.Command is "EXIT" or "RELOAD_CONFIG")
                    {
                        FreeMutex();
                        return;
                    }
                    // Otherwise Greenshot starts and runs the command itself, see the MainForm constructor
                }

                if (options.NoRun)
                {
                    // Make an exit possible
                    FreeMutex();
                    return;
                }

                if (isAlreadyRunning)
                {
                    var instances = new List<RunningInstanceItem>();
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
                            string path = Kernel32Api.GetProcessPath(greenshotProcess.Id);
                            instances.Add(new RunningInstanceItem
                            {
                                Index = index++,
                                ProcessId = greenshotProcess.Id,
                                Path = path
                            });
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
                        instances.Add(new RunningInstanceItem
                        {
                            Index = index,
                            ProcessId = currentProcess.Id,
                            Path = Kernel32Api.GetProcessPath(currentProcess.Id)
                        });
                    }

                    var instanceWindow = new InstanceRunningWindow(instances);
                    instanceWindow.ShowDialog();

                    FreeMutex();
                    Application.Exit();
                    return;
                }

                // This is the Greenshot instance which runs: read greenshot.ini now, before anything (the language, the plugins,
                // the main form) uses the configuration. The plugins add their sections later, they are filled from the loaded content.
                IniConfigRegistry.Get().Load();

                // Apply the command line language before the language is used the first time
                if (options.Language != null)
                {
                    IniConfigRegistry.GetSection<ICoreConfiguration>().Language = options.Language;
                }

                // Make sure we handle END Session correctly
                RestartManagerHelper.RegisterForRestart(IniConfigRegistry.Get().OverrideDirectory);

                // Make sure we can use forms
                WindowsFormsHost.EnableWindowsFormsInterop();

                // BUG-1809: Add message filter, to filter out all the InputLangChanged messages which go to a target control with a handle > 32 bit.
                Application.AddMessageFilter(new WmInputLangChangeRequestFilter());

                // From here on we continue starting Greenshot
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.ApplicationExit += Application_ApplicationExit;

                Application.Run(new MainForm(options, startupCommand));
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

        private readonly NamedPipeServer _namedPipeServer;

        /// <summary>
        /// The dispatcher for the UI thread
        /// </summary>
        internal WinFormsUiDispatcher UiDispatcher { get; }

        /// <summary>
        /// The STA workers for COM servers (Office, MAPI)
        /// </summary>
        internal StaWorkerFactory StaWorkers { get; } = new StaWorkerFactory();

        private readonly UiStallWatchdog _uiStallWatchdog;

        private readonly CaptureFlowRunner _flowRunner;

        // Thumbnail preview
        private ThumbnailForm _thumbnailForm;

        // Make sure we have only one settings window
        private SettingsWindow _settingsWindow;

        // Make sure we have only one about window
        private AboutWindow _aboutWindow;

        // Timer for the double click test
        private readonly Timer _doubleClickTimer = new Timer();
        private UpdateService _updateService;

        public MainForm(CommandLineOptions options, IpcEnvelope startupCommand = null)
        {

            // The one UI thread: everything else reaches it through the IUiDispatcher
            UiDispatcher = WinFormsUiDispatcher.CreateForCurrentThread();
            SimpleServiceProvider.Current.AddService<IUiDispatcher>(UiDispatcher);
            SimpleServiceProvider.Current.AddService<IStaWorkerFactory>(StaWorkers);
            SimpleServiceProvider.Current.AddService<IClipboardService>(new ClipboardService(UiDispatcher));
            // The destinations talk to the user only through IUserInteraction, the dialogs are the registered views
            var userInteraction = new InteractiveUserInteraction(UiDispatcher);
            userInteraction.Register<PrintRequest, bool>(PrintRequest.Print);
            userInteraction.Register<ShareRequest, string>(SharingForm.Show);
            SimpleServiceProvider.Current.AddService<IUserInteraction>(userInteraction);
            SimpleServiceProvider.Current.AddService<IDialogViewRegistry>(userInteraction);
#if DEBUG
            _uiStallWatchdog = new UiStallWatchdog(UiDispatcher.Context);
#else
            if (_conf.EnableUiStallWatchdog)
            {
                _uiStallWatchdog = new UiStallWatchdog(UiDispatcher.Context);
            }
#endif
            // The UI thread is reached through IUiDispatcher (UiDispatcher.Current), the SynchronizationContext and TaskScheduler aren't registered

            if (_conf.UseWindowsGraphicsCapture)
            {
                // Creating the Direct3D device costs ~200 ms, do it now in the background instead of in the first capture
                WindowsGraphicsCaptureInterop.PrewarmAsync().FireAndLog("Prewarm the Windows Graphics Capture", Log);
            }

            // Register the RecyclableMemoryStreamManager to minimise Large Object Heap usage.
            SimpleServiceProvider.Current.AddService(RecyclableMemoryStreamFactory.Manager);
 
            // The most important form is this
            SimpleServiceProvider.Current.AddService<Form>(this);
            // Also as itself
            SimpleServiceProvider.Current.AddService(this);
            SimpleServiceProvider.Current.AddService<IGreenshotMainForm>(this);
            SimpleServiceProvider.Current.AddService<ICaptureHelper>(this);
            SimpleServiceProvider.Current.AddService<ITriggerManager>(TriggerManager.Instance);
            SimpleServiceProvider.Current.AddService<IRecipeManager>(RecipeManager.Instance);
            SimpleServiceProvider.Current.AddService<IStepRegistry>(StepRegistry.Instance);
            SimpleServiceProvider.Current.AddService<ICapturePipeline>(CapturePipeline.Instance);
            // Every flow is started, tracked and cancelled through the flow runner
            _flowRunner = new CaptureFlowRunner(CapturePipeline.Instance, UiDispatcher, CapturePipeline.Instance.Selector);
            SimpleServiceProvider.Current.AddService<ICaptureFlowRunner>(_flowRunner);

            // Windows specific services
            SimpleServiceProvider.Current.AddService<INotificationService>(ToastNotificationService.Create());
            // Set this as IOcrProvider
            SimpleServiceProvider.Current.AddService<IOcrProvider>(new Win10OcrProvider());

            // Factory for surface objects
            ISurface SurfaceFactory() => new Surface();

            SimpleServiceProvider.Current.AddService((Func<ISurface>) SurfaceFactory);

            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            try
            {
                InitializeComponent();
                ApplyImages();
                InitializeLanguage();
            }
            catch (ArgumentException ex)
            {
                // Added for Bug #1420, this doesn't solve the issue but maybe the user can do something with it.
                ex.Data.Add("more information here", "https://support.microsoft.com/kb/943140");
                throw;
            }

            // Make the main menu available
            SimpleServiceProvider.Current.AddService(contextMenu);

            var supportedFileFormatRegistry = new FileFormatRegistry();
            SimpleServiceProvider.Current.AddService<IFileFormatRegistry>(supportedFileFormatRegistry);
            CoreFileFormats.RegisterCoreFileFormats(supportedFileFormatRegistry);

            notifyIcon.Icon = GreenshotResources.GetGreenshotIcon();
            // Make the notify icon available
            SimpleServiceProvider.Current.AddService(notifyIcon);

            // Load all the plugins, their configuration sections are filled from the already loaded greenshot.ini
            // The plugins start in parallel, the main window doesn't wait for them. Greenshot Light has no plugins.
#if !GREENSHOT_LIGHT
            PluginHelper.Instance.LoadPluginsAsync().FireAndLog("Start the plugins", Log);
#endif

            EditorInitialize.Initialize();
            // JIT-compiling the editor and loading the emoji font takes seconds, do it in the background instead of when the first editor opens
            EditorPrewarm.PrewarmAsync(TimeSpan.FromSeconds(5)).FireAndLog("Prepare the editor", Log);

            // This forces the registration of all destinations inside Greenshot itself.
            RegisterInternalDestinations();
            // This forces the registration of all processors inside Greenshot itself.
            RegisterInternalProcessors();

            // The recipe settings (disabled recipes, recipe files) belong to the Recipe Editor plugin and recipe files can use steps
            // of other plugins, both are only available now that the plugins registered themselves.
            RecipeManager.Instance.ReloadRecipes();

            RecipeManager.Instance.RecipesChanged += (s, e) =>
            {
                // Raised from file watchers and flows: always marshal to the UI thread
                UiDispatcher.InvokeAsync(UpdateRecipesMenu).FireAndLog("Update recipes menu", Log);
                // Recipes with options in the quick settings can come or go
                UiDispatcher.InvokeAsync(InitializeQuickSettingsMenu).FireAndLog("Update quick settings", Log);
            };

            // The command line language was already applied in Start, right after greenshot.ini was read
            // if language is not set, show language dialog
            if (string.IsNullOrEmpty(_conf.Language))
            {
                var languageWindow = new Greenshot.Forms.Wpf.LanguageWindow();
                languageWindow.ShowDialog(this);
                _conf.Language = languageWindow.SelectedLanguage;
                Language.CurrentLanguage = languageWindow.SelectedLanguage;
            }
            else if (Language.CurrentLanguage != _conf.Language)
            {
                Language.CurrentLanguage = _conf.Language;
            }

            // Disable access to the settings, for feature #3521446
            contextmenu_settings.Visible = !_conf.DisableSettings;

            // No longer needed when the recipes are loaded from the configuration, but keep it for now to be sure
            //HotkeyHelper.RegisterHotkeys();

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

            // Start named pipe server for session-isolated IPC
            _namedPipeServer = new NamedPipeServer();
            _namedPipeServer.RequestReceived += OnNamedPipeRequestReceivedAsync;
            _namedPipeServer.Start();
            RestartManagerHelper.ShutdownNotifier = reason => _namedPipeServer.NotifyShutdownAsync(reason);
#if !GREENSHOT_LIGHT
            // greenshot-mcp updates its tools right away when the recipes or the AI tools switch change
            RecipeManager.Instance.RecipesChanged += (sender, args) => NotifyToolsChanged();
            coreConfiguration.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ICoreConfiguration.AiToolsEnabled))
                {
                    NotifyToolsChanged();
                }
            };
#endif

            if (options.Restore)
            {
                RestartManagerHelper.RestoreState();
            }
            // Check if it's the first time launch?
            if (_conf.IsFirstLaunch)
            {
                ApplicationStartupHelper.FirstLaunch();
            }

            if (startupCommand != null)
            {
                // The command Greenshot was started with takes the same way as one from greenshot.com, now that the pipe server listens
                AsyncCommand.RunInBackground(() =>
                {
                    NamedPipeClient.SendMessage(startupCommand);
                    return Task.CompletedTask;
                }, "Send the startup command to the pipe server");
            }

            // Start the update check in the background
            _updateService = new UpdateService();
            _updateService.Startup();
            SimpleServiceProvider.Current.AddService(_updateService);

            // Make Greenshot use less memory after startup
            if (_conf.MinimizeWorkingSetSize)
            {
                PsApi.EmptyWorkingSet();
            }
        }

        /// <summary>
        /// The images of the controls, embedded as plain files (see EmbeddedResources). They are assigned here and not in the
        /// designer: the designer would put them into the .resx as binary data, which needs System.Resources.Extensions.
        /// Never set an Image in the designer, add the file to Resources and a line here.
        /// </summary>
        private void ApplyImages()
        {
            contextmenu_capturearea.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_capturearea.Image");
            contextmenu_capturelastregion.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_capturelastregion.Image");
            contextmenu_capturewindow.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_capturewindow.Image");
            contextmenu_capturefullscreen.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_capturefullscreen.Image");
            contextmenu_captureclipboard.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_captureclipboard.Image");
            contextmenu_openfile.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_openfile.Image");
            contextmenu_settings.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_settings.Image");
            contextmenu_help.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_help.Image");
            contextmenu_donate.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_donate.Image");
            contextmenu_exit.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_exit.Image");
        }

        protected override void InitializeLanguage()
        {
            this.contextmenu_quicksettings.Size = new System.Drawing.Size(170, coreConfiguration.IconSize.Height + 8);
            Text = Language.GetString("application_title");

            contextmenu_capturearea.Text = Language.GetString("contextmenu_capturearea");
            contextmenu_capturelastregion.Text = Language.GetString("contextmenu_capturelastregion");
            contextmenu_capturewindow.Text = Language.GetString("contextmenu_capturewindow");
            contextmenu_capturefullscreen.Text = Language.GetString("contextmenu_capturefullscreen");
            contextmenu_capturewindowfromlist.Text = Language.GetString("contextmenu_capturewindowfromlist");
            contextmenu_captureclipboard.Text = Language.GetString("contextmenu_captureclipboard");
            contextmenu_openfile.Text = Language.GetString("contextmenu_openfile");
            contextmenu_openrecentcapture.Text = Language.GetString("contextmenu_openrecentcapture");
            contextmenu_quicksettings.Text = Language.GetString("contextmenu_quicksettings");
            contextmenu_settings.Text = Language.GetString("contextmenu_settings");
            contextmenu_help.Text = Language.GetString("contextmenu_help");
            contextmenu_donate.Text = Language.GetString("contextmenu_donate");
            contextmenu_about.Text = Language.GetString("contextmenu_about");
            contextmenu_exit.Text = Language.GetString("contextmenu_exit");
            // With the edition, e.g. "Greenshot Light - ..."
            string applicationTitle = Language.GetString("application_title");
            if (applicationTitle.StartsWith("Greenshot", StringComparison.Ordinal))
            {
                applicationTitle = GreenshotEdition.ProductName + applicationTitle.Substring("Greenshot".Length);
            }
            notifyIcon.Text = NotifyIconTextHelper.ToNotifyIconText(applicationTitle);
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
                if (internalDestination.IsAvailableFor(null))
                {
                    SimpleServiceProvider.Current.AddService(internalDestination);
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
        /// Handles incoming IPC requests via the security dispatcher.
        /// </summary>
        private async Task OnNamedPipeRequestReceivedAsync(IpcRequestContext context)
        {
            await IpcSecurityDispatcher.DispatchAsync(
                context,
                this,
                Exit,
                ApplicationStartupHelper.ReloadConfig,
                ApplicationStartupHelper.FirstLaunch,
                ApplicationStartupHelper.OpenFile).ConfigureAwait(false);
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
            InitializeLanguage();

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
            if (Volatile.Read(ref _shutdownState) == 2)
            {
                // The shutdown is done
                return;
            }

            if (e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing)
            {
                // No time to wait for flows, clean up what is essential: the plugins stop synchronously as far as they can
                if (Interlocked.CompareExchange(ref _shutdownState, 1, 0) == 0)
                {
                    ShutdownUi();
#if !GREENSHOT_LIGHT
                    PluginHelper.Instance.ShutdownAsync(TimeSpan.FromSeconds(1)).FireAndLog("Stop the plugins", Log);
#endif
                }

                ShutdownCleanup(false);
                return;
            }

            // Close after the async shutdown
            e.Cancel = true;
            Exit();
        }

        private void MainFormActivated(object sender, EventArgs e)
        {
            Hide();
            ShowInTaskbar = false;
        }

        private void CaptureFile(IDestination destination = null)
        {
            var fileFormatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            var extensions = fileFormatRegistry.GetLoadableFileFormats()
                .SelectMany(format => format.LoadableExtensions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
                .Select(extension => $"*.{extension}")
                .ToList();

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


        /// <summary>
        /// Phase 2 of the clipboard check for the context menu: continues on the UI thread
        /// </summary>
        private async Task EnableCaptureClipboardAsync()
        {
            if (await ClipboardHelper.ContainsImageAsync())
            {
                contextmenu_captureclipboard.Enabled = true;
            }
        }

        private void ContextMenuOpening(object sender, CancelEventArgs e)
        {
            var factor = DeviceDpi / 96f;
            contextMenu.Scale(new SizeF(factor, factor));
            // Phase 1 only checks the formats; when a file list, virtual files or HTML could contain an image, phase 2 checks them in the background
            bool? clipboardImage = ClipboardHelper.ContainsImageQuick();
            contextmenu_captureclipboard.Enabled = clipboardImage == true;
            if (clipboardImage == null)
            {
                EnableCaptureClipboardAsync().FireAndLog("Check the clipboard for an image", Log);
            }
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
                contextmenu_donate.Image = EmbeddedResources.GetImage(typeof(MainForm), "contextmenu_present.Image");
            }

            UpdateRecipesMenu();
            PluginUtils.UpdatePluginSeparatorsVisibility(contextMenu);
        }

        private ToolStripMenuItem _recipesMenuItem;

        private void UpdateRecipesMenu()
        {
            if (!RecipeConfigHelper.IsRecipeFeatureEnabled())
            {
                if (_recipesMenuItem != null && contextMenu.Items.Contains(_recipesMenuItem))
                {
                    contextMenu.Items.Remove(_recipesMenuItem);
                }
                return;
            }

            if (_recipesMenuItem == null)
            {
                _recipesMenuItem = new ToolStripMenuItem(Language.GetString("contextmenu_recipes") ?? "Recipes")
                {
                    Name = "contextmenu_recipes"
                };
                int insertIdx = contextMenu.Items.IndexOf(toolStripOtherSourcesSeparator);
                if (insertIdx >= 0)
                {
                    contextMenu.Items.Insert(insertIdx + 1, _recipesMenuItem);
                }
                else
                {
                    contextMenu.Items.Add(_recipesMenuItem);
                }
            }

            _recipesMenuItem.DropDownItems.Clear();

            var triggerManager = SimpleServiceProvider.Current.GetInstance<Greenshot.Base.Triggers.ITriggerManager>(isOptional: true) as Triggers.TriggerManager ?? Triggers.TriggerManager.Instance;
            var recipeManager = SimpleServiceProvider.Current.GetInstance<Greenshot.Base.Recipes.IRecipeManager>(isOptional: true) ?? Recipes.RecipeManager.Instance;

            var menuTriggers = triggerManager.GetContextMenuTriggers();
            int recipeItemCount = 0;

            foreach (var trigger in menuTriggers.OrderBy(t => t.Order))
            {
                var recipe = recipeManager.GetRecipeById(trigger.TargetRecipeId);
                if (recipe == null || !recipe.ShowInContextMenu || !recipe.IsEnabled) continue;

                var item = new ToolStripMenuItem(trigger.MenuItemText ?? recipe.Name);

                var hotkeyTrigger = triggerManager.FindHotkeyTriggerForRecipe(recipe.Id);
                if (hotkeyTrigger != null && !string.IsNullOrWhiteSpace(hotkeyTrigger.HotkeyString))
                {
                    item.ShortcutKeyDisplayString = hotkeyTrigger.HotkeyString;
                }

                item.Click += (s, ev) =>
                {
                    RunLater(() =>
                    {
                        trigger.Fire();
                    });
                };

                _recipesMenuItem.DropDownItems.Add(item);
                recipeItemCount++;
            }

            if (recipeItemCount > 0)
            {
                _recipesMenuItem.DropDownItems.Add(new ToolStripSeparator());
            }

            var importItem = new ToolStripMenuItem(Language.GetString("contextmenu_importrecipe") ?? "Import Recipe...");
            importItem.Click += (s, ev) =>
            {
                OnImportRecipeClicked();
            };
            _recipesMenuItem.DropDownItems.Add(importItem);

            var reloadItem = new ToolStripMenuItem(Language.GetString("contextmenu_reloadrecipes") ?? "Reload Recipes");
            reloadItem.Click += (s, ev) =>
            {
                recipeManager.ReloadRecipes();
            };
            _recipesMenuItem.DropDownItems.Add(reloadItem);

            var editorService = SimpleServiceProvider.Current.GetInstance<IRecipeEditorService>(isOptional: true);
            if (editorService != null)
            {
                var managerItem = new ToolStripMenuItem(Language.GetString("contextmenu_managerecipes") ?? "Recipe Manager...");
                managerItem.Click += (s, ev) =>
                {
                    editorService.OpenRecipeManager();
                };
                _recipesMenuItem.DropDownItems.Add(managerItem);

                var editorItem = new ToolStripMenuItem(Language.GetString("contextmenu_recipeeditor") ?? "Recipe Editor...");
                editorItem.Click += (s, ev) =>
                {
                    OnOpenRecipeEditorClicked();
                };
                _recipesMenuItem.DropDownItems.Add(editorItem);
            }
        }

        private void OnOpenRecipeEditorClicked()
        {
            try
            {
                var editorService = SimpleServiceProvider.Current.GetInstance<IRecipeEditorService>(isOptional: true);
                editorService?.OpenEditor();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to open recipe editor window.", ex);
            }
        }

        private void OnImportRecipeClicked()
        {
            using (var ofd = new OpenFileDialog
            {
                Title = Language.GetString("recipe_import_title") ?? "Import Capture Recipe",
                Filter = Greenshot.Base.Recipes.RecipeSerializer.RecipeFileFilter,
                Multiselect = false
            })
            {
                if (ofd.ShowDialog(this) == DialogResult.OK && File.Exists(ofd.FileName))
                {
                    string recipePath = Path.GetFullPath(ofd.FileName);
                    var result = Recipes.RecipeManager.Instance.LoadRecipeFromFile(recipePath, interactiveApproval: true, forceApprovalPrompt: true);
                    if (result.IsValid)
                    {
                        var recipeConfig = RecipeConfigHelper.TryGetRecipeConfiguration();
                        string existing = recipeConfig?.RecipeFiles ?? "";
                        var configuredPaths = new List<string>();
                        var currentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (string configuredPath in existing.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            try
                            {
                                string normalizedPath = Path.GetFullPath(configuredPath.Trim());
                                if (currentPaths.Add(normalizedPath))
                                {
                                    configuredPaths.Add(normalizedPath);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warn($"Could not normalize configured recipe path '{configuredPath}'.", ex);
                            }
                        }

                        if (currentPaths.Add(recipePath))
                        {
                            configuredPaths.Add(recipePath);
                            if (recipeConfig != null)
                            {
                                recipeConfig.RecipeFiles = string.Join(";", configuredPaths);
                            }
                            IniConfigRegistry.Get()?.Save();
                        }
                    }
                }
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
                RunLater(() =>
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
                    RunLater(()=>
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

        /// <summary>
        /// Run the action after the current UI event (e.g. when the context menu closed), exceptions are logged
        /// </summary>
        private void RunLater(Action action)
        {
            UiDispatcher.InvokeAsync(action).FireAndLog("Menu action", Log);
        }

        private void CaptureAreaToolStripMenuItemClick(object sender, EventArgs e)
        {
            RunLater(() =>
            {
                CaptureHelper.CaptureRegion(false);
            });
        }

        private void CaptureClipboardToolStripMenuItemClick(object sender, EventArgs e)
        {
            RunLater(() =>
            {
                CaptureHelper.CaptureClipboard();
            });
        }

        private void OpenFileToolStripMenuItemClick(object sender, EventArgs e)
        {
            RunLater(() =>
            {
                CaptureFile();
            });
        }

        private void CaptureFullScreenToolStripMenuItemClick(object sender, EventArgs e)
        {
            RunLater(() =>
            {
                CaptureHelper.CaptureFullscreen(false, _conf.ScreenCaptureMode);
            });
        }

        private void Contextmenu_CaptureLastRegionClick(object sender, EventArgs e)
        {
            RunLater(() =>
            {
                CaptureHelper.CaptureLastRegion(false);
            });
        }

        private void Contextmenu_CaptureWindow_Click(object sender, EventArgs e)
        {
            RunLater(() =>
            {
                CaptureHelper.CaptureWindowInteractive(false);
            });
        }

        private void Contextmenu_CaptureWindowFromList_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem) sender;
            RunLater(() =>
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
            RunLater(() =>
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
            RunLater(() =>
            {
                ShowSetting();
            });
        }

        public void ShowSetting(string pluginName = null) => ShowSetting(pluginName, null);

        public void ShowSetting(string pluginName, string tabName)
        {
            // Use WPF Settings Window
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                if (!string.IsNullOrEmpty(tabName))
                {
                    _settingsWindow.SelectTab(tabName);
                }
                if (!string.IsNullOrEmpty(pluginName))
                {
                    _settingsWindow.SelectPlugin(pluginName);
                }
                _settingsWindow.Activate();
            }
            else
            {
                try
                {
                    _settingsWindow = new SettingsWindow(pluginName, tabName);
                    
                    // Show the WPF window as a dialog
                    if (_settingsWindow.ShowDialog() == true)
                    {
                        InitializeQuickSettingsMenu();
                    }
                }
                finally
                {
                    _settingsWindow = null;
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
            if (_aboutWindow != null && _aboutWindow.IsLoaded)
            {
                _aboutWindow.Activate();
                WindowDetails.ToForeground(new System.Windows.Interop.WindowInteropHelper(_aboutWindow).Handle);
            }
            else
            {
                try
                {
                    _aboutWindow = new AboutWindow();
                    var helper = new System.Windows.Interop.WindowInteropHelper(_aboutWindow)
                    {
                        Owner = this.Handle
                    };
                    _aboutWindow.ShowDialog();
                }
                finally
                {
                    _aboutWindow = null;
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
            AsyncCommand.Run(HelpFileLoader.LoadHelpAsync, "Load the help");
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
                    selectList.AddItem(destination.Descriptor?.DisplayName ?? destination.Designation, destination, _conf.OutputDestinations.Contains(destination.Designation));
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

            AddRecipeQuickSettings();
        }

        /// <summary>
        /// At most this many recipe options in the "Automatic steps" block of the quick settings, the others are in Settings > Recipes ("More…")
        /// </summary>
        private const int MaxRecipeQuickSettings = 6;

        /// <summary>
        /// The options recipes offer in the quick settings ("quickSettings": true), in one "Automatic steps" block: a switch
        /// is a checked item, a choice a submenu with one item per value, at most <see cref="MaxRecipeQuickSettings"/> of them,
        /// and "More…" opens Settings > Recipes. The value is stored right away.
        /// </summary>
        private void AddRecipeQuickSettings()
        {
            List<(CaptureRecipe Recipe, RecipeOption Option)> options;
            try
            {
                options = RecipeManager.Instance.GetAllRecipes()
                    .Where(r => r?.Options != null)
                    .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
                    .SelectMany(r => r.Options
                        .Where(o => o != null && o.QuickSettings && (o.Type == ContractDataType.Boolean || (o.Type == ContractDataType.Enum && o.Choices != null)))
                        .Select(o => (Recipe: r, Option: o)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't add the options of the recipes to the quick settings.", ex);
                return;
            }

            if (options.Count == 0)
            {
                return;
            }

            contextmenu_quicksettings.DropDownItems.Add(new ToolStripSeparator());
            contextmenu_quicksettings.DropDownItems.Add(new ToolStripMenuItem(Language.GetString("quicksettings_automaticsteps"))
            {
                Enabled = false
            });

            // The same label of two recipes gets the recipe name in front
            var duplicateLabels = new HashSet<string>(options.GroupBy(o => o.Option.DisplayLabel, StringComparer.CurrentCultureIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key), StringComparer.CurrentCultureIgnoreCase);
            if (options.Count > MaxRecipeQuickSettings)
            {
                Log.DebugFormat("{0} recipe options are marked for the quick settings, showing {1}.", options.Count, MaxRecipeQuickSettings);
            }

            foreach (var (recipe, option) in options.Take(MaxRecipeQuickSettings))
            {
                string label = duplicateLabels.Contains(option.DisplayLabel) ? $"{recipe.Name ?? recipe.Id}: {option.DisplayLabel}" : option.DisplayLabel;
                var value = RecipeOptionStore.GetValue(recipe, option);
                if (option.Type == ContractDataType.Boolean)
                {
                    var switchItem = new ToolStripMenuSelectListItem
                    {
                        Text = label,
                        Checked = value is true,
                        CheckOnClick = true,
                        ToolTipText = option.Description
                    };
                    switchItem.CheckedChanged += (sender, args) => RecipeOptionStore.SetValue(recipe.Id, option, switchItem.Checked);
                    contextmenu_quicksettings.DropDownItems.Add(switchItem);
                    continue;
                }

                var choiceList = new ToolStripMenuSelectList($"recipe:{recipe.Id}:{option.Key}", false, this)
                {
                    Text = label
                };
                foreach (var choice in option.Choices.Where(c => c != null))
                {
                    bool isCurrent = string.Equals(choice.Value, value as string, StringComparison.OrdinalIgnoreCase);
                    choiceList.AddItem(choice.DisplayLabel, (Action)(() => RecipeOptionStore.SetValue(recipe.Id, option, choice.Value)), isCurrent);
                }
                choiceList.CheckedChanged += QuickSettingRecipeChoiceChanged;
                contextmenu_quicksettings.DropDownItems.Add(choiceList);
            }

            var moreItem = new ToolStripMenuItem(Language.GetString("quicksettings_automaticsteps_more"));
            moreItem.Click += (sender, args) => ShowSetting(null, "recipes");
            contextmenu_quicksettings.DropDownItems.Add(moreItem);
        }
        private static void QuickSettingRecipeChoiceChanged(object sender, EventArgs e)
        {
            var item = ((ItemCheckedChangedEventArgs) e).Item;
            if (item.Checked && item.Data is Action select)
            {
                select();
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
            BeginInvoke(() =>
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
        /// How long the shutdown waits for the running flows and for each plugin
        /// </summary>
        private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 0: running, 1: the async shutdown started, 2: the cleanup is done and the application may close
        /// </summary>
        private int _shutdownState;

        /// <summary>
        /// Start the shutdown from a (sync) UI event
        /// </summary>
        public void Exit()
        {
            AsyncCommand.Run(ExitAsync, "Exit Greenshot");
        }

        /// <summary>
        /// Shutdown / cleanup: wait (bounded) for the running flows, stop the plugins and the STA workers, then exit.
        /// </summary>
        public async Task ExitAsync()
        {
            if (Interlocked.CompareExchange(ref _shutdownState, 1, 0) != 0)
            {
                return;
            }

            Log.Info("Exit: " + EnvironmentInfo.EnvironmentToString(false));
            ShutdownUi();

            // The clients learn that Greenshot exits on purpose: greenshot-mcp waits for the next start instead of starting it again
            if (_namedPipeServer != null)
            {
                try
                {
                    await _namedPipeServer.NotifyShutdownAsync(NamedPipeServer.ShutdownReasonExit).WaitAsync(TimeSpan.FromSeconds(1));
                }
                catch (Exception ex)
                {
                    Log.Debug("Could not tell the named pipe clients that Greenshot exits.", ex);
                }
            }

            // Running flows are cancelled, the shutdown waits a bounded time for them
            using (var timeoutSource = new CancellationTokenSource(ShutdownTimeout))
            {
                try
                {
                    await _flowRunner.ShutdownAsync(timeoutSource.Token);
                }
                catch (Exception ex)
                {
                    Log.Warn("Error stopping the running flows", ex);
                }
            }

#if !GREENSHOT_LIGHT
            // Inform all registered plugins
            try
            {
                await PluginHelper.Instance.ShutdownAsync(ShutdownTimeout);
            }
            catch (Exception e)
            {
                Log.Error("Error shutting down plugins!", e);
            }
#endif

            try
            {
                // A COM call which hangs keeps its worker busy, don't wait for it forever
                await StaWorkers.DisposeAsync().AsTask().WaitAsync(ShutdownTimeout);
            }
            catch (Exception e)
            {
                Log.Error("Error stopping the STA workers!", e);
            }

            ShutdownCleanup(true);
        }

#if !GREENSHOT_LIGHT
        private void NotifyToolsChanged()
        {
            _namedPipeServer?.NotifyWatchersAsync(new { @event = "tools_changed" }).FireAndLog("Tell the watchers that the tools changed", Log);
        }
#endif

        /// <summary>
        /// The first, synchronous part of the shutdown: configuration, other forms, hotkeys, sound.
        /// </summary>
        private void ShutdownUi()
        {
            try
            {
                IniConfigRegistry.Get()?.Save();
            }
            catch (Exception ex)
            {
                Log.Warn("Error saving configuration on exit!", ex);
            }

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
                    form.Close();
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
        }

        /// <summary>
        /// The last part of the shutdown (runs once), closes the application.
        /// </summary>
        /// <param name="exitApplication">false when the application is already closing (FormClosing)</param>
        private void ShutdownCleanup(bool exitApplication)
        {
            if (Interlocked.Exchange(ref _shutdownState, 2) == 2)
            {
                return;
            }

            ImageIO.RemoveTmpFiles();

            _uiStallWatchdog?.Dispose();

            // Remove the application mutex
            FreeMutex();

            // make the icon invisible otherwise it stays even after exit!!
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            if (!exitApplication)
            {
                return;
            }

            // Graceful shutdown, the message loop ends
            try
            {
                Application.Exit();
            }
            catch (Exception e)
            {
                Log.Error("Error closing application!", e);
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
        public Task<ICapture> CaptureWindowAsync(WindowDetails windowToCapture, ICapture capture, WindowCaptureMode coreConfigurationWindowCaptureMode, CancellationToken cancellationToken = default)
        {
            return WindowCaptureHelper.CaptureWindowAsync(windowToCapture, capture, coreConfigurationWindowCaptureMode, UiDispatcher, cancellationToken);
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
