/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
using Dapplo.Ini;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Messages;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Languages;
using Greenshot.Base.Native;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Base.Threading;
using Greenshot.Capturing;
using Greenshot.Configuration;
using Greenshot.Destinations;
using Greenshot.Editor;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using Greenshot.Helpers;
using Greenshot.Ipc;
using Greenshot.Plugin.Win10;
using Greenshot.Processors;
using Greenshot.Recipes;
using Greenshot.Recipes.Pipeline;
using Greenshot.Recipes.Triggers;
using Greenshot.Settings.Views;
using Greenshot.Views;
using log4net;
#if !GREENSHOT_LIGHT
using Greenshot.Plugins;
#endif
using WpfWindow = System.Windows.Window;
using WpfWindowStyle = System.Windows.WindowStyle;

namespace Greenshot.Shell
{
    /// <summary>
    /// The shell of Greenshot while it runs: composes the services, owns the tray icon, the settings and about windows, and the shutdown.
    /// The message loop of the UI thread is a WPF one (see <see cref="GreenshotApplication"/>), there is no main window.
    /// </summary>
    internal sealed class GreenshotShell : IGreenshotShell
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotShell));
        private static ICoreConfiguration _conf => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());

        /// <summary>
        /// How long the shutdown waits for the running flows and for each plugin
        /// </summary>
        private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

        private readonly NamedPipeServer _namedPipeServer;
        private readonly UiStallWatchdog _uiStallWatchdog;
        private readonly CaptureFlowRunner _flowRunner;
        private readonly UpdateService _updateService;
        private readonly WpfWindow _ownerWindow;
        private TrayIcon _trayIcon;

        // Make sure we have only one settings window
        private SettingsWindow _settingsWindow;

        // Make sure we have only one about window
        private AboutWindow _aboutWindow;

        /// <summary>
        /// 0: running, 1: the async shutdown started, 2: the cleanup is done and the application may close
        /// </summary>
        private int _shutdownState;

        /// <summary>
        /// The dispatcher for the UI thread
        /// </summary>
        internal WpfUiDispatcher UiDispatcher { get; }

        /// <summary>
        /// The STA workers for COM servers (Office, MAPI)
        /// </summary>
        internal StaWorkerFactory StaWorkers { get; } = new StaWorkerFactory();

        /// <inheritdoc />
        public IntPtr OwnerHandle { get; }

        public GreenshotShell(CommandLineOptions options, IpcEnvelope startupCommand = null)
        {
            // The one UI thread: everything else reaches it through the IUiDispatcher
            UiDispatcher = WpfUiDispatcher.CreateForCurrentThread();
            SimpleServiceProvider.Current.AddService<IUiDispatcher>(UiDispatcher);
            SimpleServiceProvider.Current.AddService<IStaWorkerFactory>(StaWorkers);
            SimpleServiceProvider.Current.AddService<IClipboardService>(new ClipboardService(UiDispatcher));
            // The destinations talk to the user only through IUserInteraction, the dialogs are the registered views
            var userInteraction = new InteractiveUserInteraction(UiDispatcher);
            userInteraction.Register<PrintRequest, bool>(PrintRequest.Print);
            userInteraction.Register<ShareRequest, string>(ShareHostWindow.Show);
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

            // The background work of the start, MinimizeWorkingSetSize trims the memory when it's done
            var startupWork = new List<Task>();
            void TrackStartupWork(Task work, string description)
            {
                work.FireAndLog(description, Log);
                startupWork.Add(work);
            }

            // Creating the Direct3D device costs ~200 ms, do it now in the background instead of in the first capture
            // (unless the UseGraphicsCapture or KeepGraphicsCaptureReady settings are off)
            TrackStartupWork(WindowsGraphicsCaptureInterop.PrewarmAsync(), "Prewarm the Windows Graphics Capture");

            // Register the RecyclableMemoryStreamManager to minimise Large Object Heap usage.
            SimpleServiceProvider.Current.AddService(RecyclableMemoryStreamFactory.Manager);

            // A hidden top level window, never shown: the owner of the windows which belong to no other window
            _ownerWindow = new WpfWindow
            {
                Title = "Greenshot",
                WindowStyle = WpfWindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                Width = 0,
                Height = 0
            };
            OwnerHandle = new WindowInteropHelper(_ownerWindow).EnsureHandle();

            SimpleServiceProvider.Current.AddService<IGreenshotShell>(this);
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

            var supportedFileFormatRegistry = new FileFormatRegistry();
            SimpleServiceProvider.Current.AddService<IFileFormatRegistry>(supportedFileFormatRegistry);
            CoreFileFormats.RegisterCoreFileFormats(supportedFileFormatRegistry);

            // Load all the plugins, their configuration sections are filled from the already loaded greenshot.ini
            // The plugins start in parallel, the shell doesn't wait for them. Greenshot Light has no plugins.
#if !GREENSHOT_LIGHT
            TrackStartupWork(PluginHelper.Instance.LoadPluginsAsync(), "Start the plugins");
#endif

            EditorInitialize.Initialize();
            // JIT-compiling the editor and loading the emoji font takes seconds, do it in the background instead of when the first editor opens
            // The same for the interactive capture, earlier: it is what a hotkey opens first. The settings can switch both off to save memory.
            if (_conf.PrewarmCapture)
            {
                TrackStartupWork(CapturePrewarm.PrewarmAsync(TimeSpan.FromSeconds(2)), "Prepare the interactive capture");
            }
            if (_conf.PrewarmEditor)
            {
                TrackStartupWork(EditorPrewarm.PrewarmAsync(TimeSpan.FromSeconds(5)), "Prepare the editor");
            }

            // This forces the registration of all destinations inside Greenshot itself.
            RegisterInternalDestinations();
            // This forces the registration of all processors inside Greenshot itself.
            RegisterInternalProcessors();

            // The recipe settings (disabled recipes, recipe files) belong to the Recipe Editor plugin and recipe files can use steps
            // of other plugins, both are only available now that the plugins registered themselves.
            RecipeManager.Instance.ReloadRecipes();

            // The command line language was already applied in Start, right after greenshot.ini was read
            // if language is not set, show language dialog
            if (string.IsNullOrEmpty(_conf.Language))
            {
                var languageWindow = new LanguageWindow();
                languageWindow.ShowDialog(OwnerHandle);
                Texts.SetLanguage(languageWindow.SelectedLanguage);
            }
            else
            {
                Texts.SetLanguage(_conf.Language);
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

            SoundHelper.Initialize();

            // The tray icon; the menu is built when it opens, so it always shows the current recipes, plugins and settings
            _trayIcon = new TrayIcon(this);
            // Set the Greenshot icon visibility depending on the configuration. (Added for feature #3521446)
            _trayIcon.Visible = !_conf.HideTrayicon;
            SimpleServiceProvider.Current.AddService<ITrayIcon>(_trayIcon);

            // Check to see if there is already another INotificationService
            if (!SimpleServiceProvider.Current.GetAllInstances<INotificationService>().Any())
            {
                // If not we add the balloons of the tray icon
                SimpleServiceProvider.Current.AddService<INotificationService>(new TrayNotificationService(_trayIcon));
            }

            // Make sure we never capture our own windows
            WindowDetails.RegisterIgnoreHandle(SharedMessageWindow.Handle);
            WindowDetails.RegisterIgnoreHandle(OwnerHandle);

            // The application ends without the shutdown when the session ends (see RestartManagerHelper).
            // Not Application.ApplicationExit: WinForms raises it when its first message loop ends, which is the one of an STA worker now.
            RestartManagerHelper.SessionEndShutdown = ShutdownForSessionEnd;

            // Start named pipe server for session-isolated IPC
            _namedPipeServer = new NamedPipeServer();
            _namedPipeServer.RequestReceived += OnNamedPipeRequestReceivedAsync;
            _namedPipeServer.Start();
            RestartManagerHelper.ShutdownNotifier = reason => _namedPipeServer.NotifyShutdownAsync(reason);
#if !GREENSHOT_LIGHT
            // greenshot-mcp updates its tools right away when the recipes or the AI tools switch change
            RecipeManager.Instance.RecipesChanged += (sender, args) => NotifyToolsChanged();
            _conf.PropertyChanged += (sender, args) =>
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
                // The command Greenshot was started with takes the same way as one from greenshot-cli.exe, now that the pipe server listens
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

            // Make Greenshot use less memory after startup: once the background work of the start is done, it would fill the memory again
            if (_conf.MinimizeWorkingSetSize)
            {
                TrimMemoryAfterAsync(startupWork).FireAndLog("Minimize the memory after the start", Log);
            }
        }

        private static async Task TrimMemoryAfterAsync(IEnumerable<Task> work)
        {
            try
            {
                await Task.WhenAll(work).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Already logged by FireAndLog
            }

            PsApi.EmptyWorkingSet();
        }

        /// <summary>
        /// Create all the internal destinations
        /// </summary>
        private static void RegisterInternalDestinations()
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
                new Win10OcrDestination(),
                new EditorDestination()
            };

            foreach (var internalDestination in internalDestinations)
            {
                if (internalDestination.IsAvailableFor(null))
                {
                    SimpleServiceProvider.Current.AddService(internalDestination);
                }
            }
        }

        private static void RegisterInternalProcessors()
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

        /// <inheritdoc />
        public void UpdateUi()
        {
            // The menu reads the language and the hotkeys when it opens, only the tooltip is set now
            _trayIcon?.UpdateToolTip();
        }

        /// <inheritdoc />
        public void ShowSetting(string pluginName = null) => ShowSetting(pluginName, null);

        /// <inheritdoc />
        public void ShowSetting(string pluginName, string tabName)
        {
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
                return;
            }

            try
            {
                _settingsWindow = new SettingsWindow(pluginName, tabName);
                _settingsWindow.ShowDialog();
            }
            finally
            {
                _settingsWindow = null;
            }
        }

        /// <inheritdoc />
        public void ShowAbout()
        {
            if (_aboutWindow != null && _aboutWindow.IsLoaded)
            {
                _aboutWindow.Activate();
                WindowDetails.ToForeground(new WindowInteropHelper(_aboutWindow).Handle);
                return;
            }

            try
            {
                _aboutWindow = new AboutWindow();
                _ = new WindowInteropHelper(_aboutWindow)
                {
                    Owner = OwnerHandle
                };
                _aboutWindow.ShowDialog();
            }
            finally
            {
                _aboutWindow = null;
            }
        }

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

            // The editors first, before anything is stopped: the user can still keep Greenshot running
            // (cancel in the save question, or in the save dialog)
            if (!await UiDispatcher.InvokeAsync(CloseEditors))
            {
                Log.Info("The exit was cancelled in an editor, Greenshot keeps running.");
                Volatile.Write(ref _shutdownState, 0);
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
        /// Close the editors like the user does, so each can ask to save its image (with cancel)
        /// </summary>
        /// <returns>false when an editor stays open: the user cancelled</returns>
        private static bool CloseEditors()
        {
            foreach (var editor in Application.OpenForms.OfType<ImageEditorForm>().ToList())
            {
                editor.Close();
                if (!editor.IsDisposed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The application ends without <see cref="ExitAsync"/>, e.g. at the end of the session: no time to wait for flows,
        /// clean up what is essential, the plugins stop synchronously as far as they can
        /// </summary>
        private void ShutdownForSessionEnd()
        {
            if (Volatile.Read(ref _shutdownState) == 2)
            {
                // The shutdown is done
                return;
            }

            if (Interlocked.CompareExchange(ref _shutdownState, 1, 0) == 0)
            {
                ShutdownUi();
#if !GREENSHOT_LIGHT
                PluginHelper.Instance.ShutdownAsync(TimeSpan.FromSeconds(1)).FireAndLog("Stop the plugins", Log);
#endif
            }

            ShutdownCleanup(false);
        }

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

            _trayIcon?.Menu.Close();

            // Close all open forms except the editors, use a separate List to make sure we don't get a "InvalidOperationException: Collection was modified"
            var formsToClose = Application.OpenForms.Cast<Form>().Where(form => form.GetType() != typeof(ImageEditorForm)).ToList();
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
        /// <param name="exitApplication">false when the application is already closing</param>
        private void ShutdownCleanup(bool exitApplication)
        {
            if (Interlocked.Exchange(ref _shutdownState, 2) == 2)
            {
                return;
            }

            ImageIO.RemoveTmpFiles();

            _uiStallWatchdog?.Dispose();

            // Remove the application mutex
            GreenshotApplication.FreeMutex();

            // Remove the icon, otherwise it stays in the notification area even after exit
            _trayIcon?.Dispose();
            _trayIcon = null;

            _ownerWindow.Close();

            if (!exitApplication)
            {
                return;
            }

            Log.Debug("Closing the forms.");

            // Graceful shutdown: the WinForms forms (the editors) close first, they can ask to save and cancel, then the message loop ends
            try
            {
                // The editors were closed (and could cancel) at the start of ExitAsync
                var cancelExit = new CancelEventArgs();
                Application.Exit(cancelExit);
                if (cancelExit.Cancel)
                {
                    // Everything is stopped already (tray icon, hotkeys, plugins): staying would leave an invisible Greenshot
                    Log.Warn("A form cancelled the exit, Greenshot ends anyway.");
                }
            }
            catch (Exception e)
            {
                Log.Error("Error closing application!", e);
            }

            Log.Debug("Ending the message loop.");
            GreenshotApplication.EndMessageLoop();
        }
    }
}
