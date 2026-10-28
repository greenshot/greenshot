/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using Greenshot.Linux.Capturing;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Compositor;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.DBus;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Ipc;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Destinations;
using Greenshot.Linux.Editing;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Serialization;
using SkiaSharp;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Settings;
using Greenshot.Linux.Core.Plugins;
using Greenshot.Linux.Plugins.Atlassian;
using Greenshot.Linux.Plugins.Cloud;
using Greenshot.Linux.Plugins.CustomUploader;
using Greenshot.Linux.Plugins.ExternalCommand;
using Greenshot.Linux.Plugins.Pdf;
using Avalonia.Styling;
using Greenshot.Linux.Plugins.Recipes;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.RecipeUi;
using Greenshot.Linux.RecipeEditing;
using Greenshot.Linux.RecipeEditor;
using Greenshot.Linux.Recipes.Approval;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Steps;
using Greenshot.Linux.Recipes.Triggers;
using Greenshot.Linux.Recipes.Ipc;
using Greenshot.Linux.Ai;
using Greenshot.Linux.Recipes.Ai;
using CaptureMode = Greenshot.Linux.Core.Capturing.CaptureMode;

namespace Greenshot.Linux;

/// <summary>
/// The running Greenshot: the tray icon, the commands from the command line and the hotkeys, and the capture flow
/// from taking the picture to the destinations. The MainForm of the Windows version without a form.
/// </summary>
internal sealed class GreenshotController : ISettingsHost, IHotkeyService, IDisposable
{
    private const string ShortcutRegion = "capture-region";
    private const string ShortcutWindow = "capture-window";
    private const string ShortcutFullscreen = "capture-fullscreen";
    private const string ShortcutLastRegion = "capture-last-region";

    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly CoreSettings _settings = CoreSettings.Load();
    private readonly ICompositor _compositor = CompositorDetector.Detect();
    private readonly List<IDestination> _destinations = new();
    private readonly List<IGreenshotPlugin> _plugins = new();
    private readonly ISecretStore _secrets = SecretStores.Create();
    private PluginPolicy? _policy;
    private readonly EditorSettings _editorSettings;
    private FileChooserPortal? _fileChooser;
    private DesktopPortal? _portal;
    private DesktopNotifications? _notifications;
    private GlobalShortcutsPortal? _shortcuts;
    private ScreenCaptureService? _captureService;
    private InstanceChannel? _channel;
    private IpcDispatcher? _dispatcher;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completes when the recipes are loaded: requests which come in before wait for it
    /// </summary>
    internal Task Ready => _ready.Task;
    private TrayIcon? _trayIcon;
    private Window? _screensHelper;
    private AppRecipeHost? _recipeHost;
    private IReadOnlyList<string> _heldBackRecipeFiles = [];

    /// <summary>
    /// The hotkeys of recipe triggers, bound with the capture hotkeys (GlobalShortcuts portal)
    /// </summary>
    private readonly Dictionary<string, (string Description, string Hotkey, Action Pressed)> _recipeHotkeys = new();
    private DispatcherTimer? _rebindTimer;

    public GreenshotController(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _desktop = desktop;
        _editorSettings = new EditorSettings(_settings);
        Texts.Initialize(_settings.Language);
        ApplyTheme();
    }

    public bool HasGlobalShortcuts => _shortcuts != null;

    /// <summary>
    /// The settings window saved: the language, the theme and the tray menu follow at once
    /// </summary>
    public void ApplySettings()
    {
        Texts.SetLanguage(_settings.Language);
        ApplyTheme();
        if (_trayIcon != null)
        {
            _trayIcon.Menu = CreateTrayMenu();
        }
    }

    private void ApplyTheme()
    {
        if (Application.Current is { } application)
        {
            application.RequestedThemeVariant = _settings.Theme.ToLowerInvariant() switch
            {
                "light" => ThemeVariant.Light,
                "dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }
    }

    private SettingsWindow? _settingsWindow;

    private readonly RecipeEditorService _recipeEditor = new();

    /// <summary>
    /// The recipe manager and the recipe editor
    /// </summary>
    public IRecipeEditorService RecipeEditor => _recipeEditor;

    public void ShowSettings() => ShowSettings(null);

    /// <summary>
    /// The settings window, on the given page ("recipes") when there is one
    /// </summary>
    public void ShowSettings(string? page)
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.ShowPage(page);
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(this);
        _settingsWindow.ShowPage(page);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    /// <summary>
    /// The welcome window, once at the first start
    /// </summary>
    private async Task ShowWelcomeAsync()
    {
        if (_settings.WelcomeShown)
        {
            return;
        }

        _settings.WelcomeShown = true;
        _settings.Save();
        var checks = new List<SetupCheck>
        {
            _captureService != null
                ? new SetupCheck(true, Texts.Linux("setup_capture_ok", _captureService.Grabber.Name))
                : new SetupCheck(false, Texts.Linux("setup_capture_missing")),
            SystemClipboard.IsAvailable
                ? new SetupCheck(true, Texts.Linux("setup_clipboard_ok"))
                : new SetupCheck(false, Texts.Linux("setup_clipboard_missing")),
        };
        bool tray = _portal != null && await _portal.NameHasOwnerAsync("org.kde.StatusNotifierWatcher");
        checks.Add(new SetupCheck(tray, Texts.Linux(tray ? "setup_tray_ok" : "setup_tray_missing")));
        bool notifications = _portal != null && await _portal.NameHasOwnerAsync("org.freedesktop.Notifications");
        checks.Add(new SetupCheck(notifications, Texts.Linux(notifications ? "setup_notifications_ok" : "setup_notifications_missing")));
        checks.Add(new SetupCheck(HasGlobalShortcuts, Texts.Linux(HasGlobalShortcuts ? "setup_hotkeys_portal" : "setup_hotkeys_manual")));

        var welcome = new WelcomeWindow(checks, ShowSettings);
        welcome.Show();
    }

    public CoreSettings Settings => _settings;

    public ScreenCaptureService? CaptureService => _captureService;

    public IReadOnlyList<IEditorTrigger> EditorRecipeTriggers =>
        RecipeServices.Manager == null ? [] : TriggerManager.Instance.GetEditorTriggers();

    public ICompositor Compositor => _compositor;

    public IReadOnlyList<Screen> PlatformScreens => GetPlatformScreens();

    /// <summary>
    /// The destinations of recipes: all which are switched on, the editor and the picker included
    /// </summary>
    public IReadOnlyList<IDestination> RecipeDestinations => [.. AllDestinations(), new PickerPlaceholder()];

    /// <summary>
    /// The picker as destination of a recipe: the dispatcher shows the picker for it
    /// </summary>
    private sealed class PickerPlaceholder : IDestination
    {
        public string Designation => WellKnownDestinations.Picker;

        public string DisplayName => Texts.Settings("destination_picker");

        public int Priority => -2;

        public bool IsAvailable => true;

        public Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default) => Task.FromResult(ExportResult.Aborted);
    }

    public EditorSettings EditorSettings => _editorSettings;

    public FileChooserPortal? FileChooser => _fileChooser;

    public IReadOnlyList<IDestination> ExportDestinations =>
        AllDestinations().Where(d => d.Designation != WellKnownDestinations.Editor).ToList();

    public IReadOnlyList<IGreenshotPlugin> Plugins => _plugins;

    public PluginPolicy Policy => _policy ??= new PluginPolicy(_settings);

    public ISecretStore Secrets => _secrets;

    /// <summary>
    /// The built-in destinations and those of the switched on plugins, without the ones switched off, ordered for the menus
    /// </summary>
    private IReadOnlyList<IDestination> AllDestinations() => _destinations
        .Concat(_plugins.Where(p => Policy.IsPluginEnabled(p.Name)).SelectMany(p => p.Destinations))
        .Where(d => Policy.IsDestinationEnabled(d.Designation))
        .OrderBy(d => d.Priority)
        .ToList();

    /// <summary>
    /// "CustomUploader" in the settings means the uploaders of the plugin, else the designation must match
    /// </summary>
    private static bool Matches(IDestination destination, string designation) =>
        destination.Designation.Equals(designation, StringComparison.OrdinalIgnoreCase)
        || destination.Designation.StartsWith(designation + ":", StringComparison.OrdinalIgnoreCase);

    public async Task StartAsync(AppCommand startCommand)
    {
        _dispatcher = new IpcDispatcher(new IpcApp(this));
        AiToolsIpc.Register(_dispatcher);
        _channel = InstanceChannel.TryListen(_dispatcher.HandleAsync);
        if (_channel == null)
        {
            // Another Greenshot started at the same moment and won
            await using (var client = await InstanceClient.ConnectAsync(IpcSources.Cli, IpcSources.ReplyFormatJson))
            {
                if (client != null)
                {
                    await client.SendAsync(new IpcEnvelope { Command = "APP", RawInput = startCommand.ToWire() });
                    await client.ReadFrameAsync();
                }
            }

            _desktop.Shutdown();
            return;
        }

        Log.Info($"Compositor: {_compositor.Name}, settings: {_settings.FilePath}");
        _settings.Save();
        await ConnectPortalAsync();

        try
        {
            var grabber = await ScreenCaptureService.CreateGrabberAsync(_settings, _portal);
            _captureService = new ScreenCaptureService(_settings, _compositor, grabber, GetPlatformLayout);
            Log.Info($"Capturing with {grabber.Name}");
        }
        catch (CaptureException ex)
        {
            Log.Error(ex.Message);
            await NotifyAsync("Greenshot can't capture", ex.Message, NotificationSeverity.Error);
        }

        _fileChooser = _portal != null && await FileChooserPortal.IsAvailableAsync(_portal) ? new FileChooserPortal(_portal) : null;
        var saveDialog = new SaveDialog(_fileChooser);
        _destinations.Add(new EditorDestination(OpenEditor));
        _destinations.Add(new FileDestination(_settings, saveDialog.AskAsync));
        _destinations.Add(new FileWithDialogDestination(_settings, saveDialog));
        _destinations.Add(new ClipboardDestination());
        var printPortal = _portal != null && await PrintPortal.IsAvailableAsync(_portal) ? new PrintPortal(_portal) : null;
        var emailPortal = _portal != null && await EmailPortal.IsAvailableAsync(_portal) ? new EmailPortal(_portal) : null;
        _destinations.Add(new PrinterDestination(_settings, printPortal, options => PrintOptionsDialog.AskAsync(_settings, options)));
        _destinations.Add(new EmailDestination(_settings, emailPortal));
        _destinations.Add(new OcrDestination(_settings));
        _destinations.Add(new QrCodeDestination());
        await CreatePluginsAsync(saveDialog);
        await InitializeRecipesAsync();
        _ready.TrySetResult();

        CreateTrayIcon();
        await RegisterShortcutsAsync();
        await ShowWelcomeAsync();
        if (_heldBackRecipeFiles.Count > 0)
        {
            // The recipe files which need the user (approval of a changed file, errors), now that Greenshot runs
            RecipeManager.Instance.LoadRecipeFilesInteractively(_heldBackRecipeFiles);
        }

        await RunAsync(startCommand);
    }

    internal async Task RunAsync(AppCommand command)
    {
        switch (command.Kind)
        {
            case AppCommandKind.Capture when command.Mode is { } mode:
                await CaptureAsync(mode);
                break;
            case AppCommandKind.Open when command.Path != null && command.Path.EndsWith(".gsrecipe.json", StringComparison.OrdinalIgnoreCase):
                ImportRecipe(command.Path);
                break;
            case AppCommandKind.Open when command.Path != null:
                await OpenFileAsync(command.Path);
                break;
            case AppCommandKind.Recipe when command.RecipeId != null:
                StartRecipe(command.RecipeId, new ManualTrigger("Command line", command.RecipeId),
                    command.Parameters?.ToDictionary(p => p.Key, p => (object)p.Value));
                break;
            case AppCommandKind.OpenClipboard:
                await OpenClipboardAsync();
                break;
            case AppCommandKind.Settings:
                ShowSettings();
                break;
            case AppCommandKind.RecipeEditor:
                _recipeEditor.OpenEditor(command.RecipeId);
                break;
            case AppCommandKind.RecipeManager:
                _recipeEditor.OpenRecipeManager();
                break;
            case AppCommandKind.Exit:
                Exit();
                break;
        }
    }

    /// <summary>
    /// The plugins of Greenshot, built in; the settings decide which are switched on
    /// </summary>
    private async Task CreatePluginsAsync(SaveDialog saveDialog)
    {
        _plugins.Add(new ExternalCommandPlugin(_settings));
        _plugins.Add(new PdfPlugin(_settings, path => _fileChooser != null
            ? _fileChooser.SaveFileAsync(Texts.Get("Pdf", "destination"), path, [(Texts.Get("Pdf", "label_pdf_document"), "*.pdf")])
            : Task.FromResult<string?>(path)));
        var uploader = new CustomUploaderPlugin(_settings, _secrets, UploadReminderDialog.AskAsync);
        _plugins.Add(uploader);
        _plugins.Add(new BoxPlugin(_settings, _secrets));
        _plugins.Add(new DropboxPlugin(_settings, _secrets));
        var atlassian = new AtlassianPlugin(_settings, _secrets, new AtlassianDialogs());
        _plugins.Add(atlassian);
        if (_compositor.KnowsWindows && Policy.IsPluginEnabled(atlassian.Name))
        {
            // Issue keys in window titles become destinations, like the title monitor of Windows
            _ = atlassian.Jira.WatchWindowTitlesAsync(async () => (await _compositor.GetVisibleWindowsAsync()).Select(w => w.Title).ToList(),
                TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        try
        {
            await uploader.Store.LoadAsync();
        }
        catch (Exception ex)
        {
            Log.Warn("The custom uploaders can't be read", ex);
        }

        _settings.Save();
        Log.Info("Plugins: " + string.Join(", ", _plugins.Select(p => $"{p.Name}{(Policy.IsPluginEnabled(p.Name) ? string.Empty : " (off)")}")));
    }

    private async Task ConnectPortalAsync()
    {
        try
        {
            _portal = await DesktopPortal.ConnectAsync();
            _notifications = new DesktopNotifications(_portal.Connection);
            await _notifications.StartAsync();
        }
        catch (Exception ex)
        {
            Log.Warn("No connection to the D-Bus session bus, portals and notifications are not available", ex);
        }
    }

    /// <summary>
    /// What the requests over the socket do in the app
    /// </summary>
    private sealed class IpcApp(GreenshotController controller) : IIpcApp
    {
        public Task Ready => controller.Ready;

        public string Version => Program.Version;

        public void Run(AppCommand command) => Dispatcher.UIThread.Post(() => _ = controller.RunAsync(command));

        public Task ShowSettingsAsync() => Dispatcher.UIThread.InvokeAsync(controller.ShowSettings).GetTask();

        public Task ShowAboutAsync() => Dispatcher.UIThread.InvokeAsync(() => ProcessRunner.StartDetached("xdg-open", "https://getgreenshot.org/")).GetTask();

        public Task ShowLogAsync() => Dispatcher.UIThread.InvokeAsync(() => ProcessRunner.StartDetached("xdg-open", Log.FilePath)).GetTask();

        public Task OpenRecipeEditorAsync(string? recipeId) => Dispatcher.UIThread.InvokeAsync(() => controller.RecipeEditor.OpenEditor(recipeId)).GetTask();

        public Task OpenRecipeManagerAsync() => Dispatcher.UIThread.InvokeAsync(controller.RecipeEditor.OpenRecipeManager).GetTask();

        public Task ReloadConfigurationAsync() => Dispatcher.UIThread.InvokeAsync(controller.ReloadConfiguration).GetTask();

        public void Exit() => Dispatcher.UIThread.Post(controller.Exit);
    }

    /// <summary>
    /// greenshot.ini was changed outside Greenshot (greenshot --reload): read it and the recipes again
    /// </summary>
    internal void ReloadConfiguration()
    {
        _settings.Reload();
        ApplySettings();
        RecipeManager.Instance.ReloadRecipes();
    }

    /// <summary>
    /// Take a capture and send it to the destinations: the built-in recipe of the mode, like Windows, so the automatic steps
    /// (border, caption, ...) and recipe files which replace a built-in recipe apply
    /// </summary>
    public Task CaptureAsync(CaptureMode mode)
    {
        if (_captureService == null)
        {
            return NotifyAsync("Greenshot can't capture", "No way to capture the screen was found, see the log.", NotificationSeverity.Error);
        }

        string recipeId = mode switch
        {
            CaptureMode.Window => RecipeManager.RecipeIdWindow,
            CaptureMode.ActiveWindow => RecipeManager.RecipeIdActiveWindow,
            CaptureMode.FullScreen => RecipeManager.RecipeIdFullScreen,
            CaptureMode.LastRegion => RecipeManager.RecipeIdLastRegion,
            CaptureMode.Text => RecipeManager.RecipeIdOcr,
            _ => RecipeManager.RecipeIdRegion,
        };
        StartRecipe(recipeId, new ManualTrigger(mode.ToString(), recipeId), null);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Run a recipe in the flow runner, with the values for its variables
    /// </summary>
    public void StartRecipe(string recipeId, ITrigger trigger, IDictionary<string, object>? parameters)
    {
        var recipe = RecipeManager.Instance.GetRecipeById(recipeId);
        if (recipe == null || !recipe.IsEnabled)
        {
            Log.Warn($"The recipe {recipeId} doesn't exist or is switched off");
            _ = NotifyAsync("Greenshot", Texts.Linux("recipe_unknown", recipeId), NotificationSeverity.Warning);
            return;
        }

        var prepared = TriggerRecipePreparer.Prepare(recipe, trigger);
        RecipeServices.Runner.Start(prepared, FlowTriggerContext.Empty(trigger), context =>
        {
            foreach (var parameter in parameters ?? new Dictionary<string, object>())
            {
                context.Properties[parameter.Key] = parameter.Value;
            }
        });
    }

    /// <summary>
    /// The recipe engine: the services for the steps, the steps of the plugins, the approvals, the recipes and their triggers
    /// </summary>
    private async Task InitializeRecipesAsync()
    {
        RecipeServices.Settings = _settings;
        RecipeServices.Ui = new AvaloniaUiDispatcher();
        RecipeServices.Compositor = _compositor;
        RecipeServices.BuiltInRecipeProviders = [new AiToolRecipes()];
        AiToolAccess.ConsentPrompt = AiConsentDialog.AskAsync;
        AiRecipeIpcHandler.RecipeEditorService = _recipeEditor;
        _recipeHost = new AppRecipeHost(this);
        RecipeServices.Host = _recipeHost;
        RecipeServices.UserInteraction = _recipeHost;
        RecipeServices.ApprovalUi = new RecipeApprovalUi();
        var editorUi = new AppRecipeEditorUi(this);
        editorUi.Attach();
        RecipeEditorUi.Current = editorUi;
        RecipeServices.Hotkeys = this;
        RecipeServices.StepProviders = [.. _plugins.Where(p => Policy.IsPluginEnabled(p.Name)).OfType<IRecipeStepProvider>(), new BuiltInPluginSteps()];
        RecipeServices.AuthorizationDestinations = designation =>
            _destinations.Concat(_plugins.SelectMany(p => p.Destinations)).FirstOrDefault(d => DestinationExportStep.Matches(d, designation))!;
        RecipeValidator.ExtensionAvailabilityCheck = IsRequirementAvailable;
        RecipeTrustStore.ProtectionKey = await LoadTrustKeyAsync();

        var pipeline = CapturePipeline.Instance;
        RecipeServices.Pipeline = pipeline;
        RecipeServices.Runner = new CaptureFlowRunner(pipeline, RecipeServices.Ui);
        var manager = RecipeManager.Instance;
        RecipeServices.Manager = manager;
        _heldBackRecipeFiles = manager.LoadRecipesAtStartup();
        manager.RecipesChanged += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            // greenshot-mcp offers the recipes with an AI tool trigger as tools
            _ = _channel?.NotifyWatchersAsync(new { @event = "tools_changed" });
            if (_trayIcon != null)
            {
                _trayIcon.Menu = CreateTrayMenu();
            }
        });
        TriggerManager.Instance.SyncRecipeTriggers(manager.GetAllRecipes());
        TriggerManager.Instance.StartAll();
        Log.Info($"Recipes: {manager.GetAllRecipes().Count}, automatic steps: {manager.GetAllExtensions().Count}");
    }

    /// <summary>
    /// A recipe's "requires": the plugins of Windows are built in here (ZXing, the recipe editor) or plugins which can be switched off
    /// </summary>
    private (bool, string) IsRequirementAvailable(RecipeRequirement requirement)
    {
        string version = typeof(GreenshotController).Assembly.GetName().Version?.ToString(3) ?? "1.4.0";
        string id = requirement.Id.Replace("Greenshot.Plugin.", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (id.Equals("Zxing", StringComparison.OrdinalIgnoreCase) || id.Equals("RecipeEditor", StringComparison.OrdinalIgnoreCase))
        {
            return (true, version);
        }

        var plugin = _plugins.FirstOrDefault(p => p.Name.Equals(id, StringComparison.OrdinalIgnoreCase));
        return (plugin != null && Policy.IsPluginEnabled(plugin.Name), version);
    }

    /// <summary>
    /// The key of the trust store of approved recipe files, kept in the keyring (Windows uses DPAPI)
    /// </summary>
    private async Task<byte[]?> LoadTrustKeyAsync()
    {
        const string key = "recipes/trust-key";
        try
        {
            if (await _secrets.GetAsync(key) is { Length: > 0 } stored)
            {
                return Convert.FromBase64String(stored.Trim());
            }

            var created = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
            await _secrets.SetAsync(key, "Recipe approvals", Convert.ToBase64String(created));
            return created;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or IOException)
        {
            Log.Warn("No key for the recipe approvals in the keyring, using one derived from the machine", ex);
            return null;
        }
    }

    /// <summary>
    /// Import a recipe file: the approval window, then it is in RecipeFiles
    /// </summary>
    private void ImportRecipe(string path)
    {
        var result = RecipeManager.Instance.LoadRecipeFromFile(path);
        if (!result.IsValid)
        {
            Log.Warn($"Importing {path} failed: {string.Join("; ", result.Errors)}");
        }
    }

    private async Task AskImportRecipeAsync()
    {
        if (_fileChooser == null)
        {
            await NotifyAsync("Greenshot", "There is no file dialog (FileChooser portal), use: greenshot open <file.gsrecipe.json>", NotificationSeverity.Warning);
            return;
        }

        var path = await _fileChooser.OpenFileAsync(Texts.Get("Recipe", "import_title"), [("Greenshot recipes", "*.gsrecipe.json;*.json")]);
        if (path != null)
        {
            ImportRecipe(path);
        }
    }

    /// <summary>
    /// One notification for the exports of a recipe run, with the error of the flow, like the toast of Windows
    /// </summary>
    public async Task ReportRecipeAsync(IReadOnlyList<ExportOutcome> outcomes, string? error)
    {
        var shown = outcomes.Where(o => !o.Result.Cancelled && !(o.Result.Success && o.Destination.Designation == WellKnownDestinations.Editor)).ToList();
        if (shown.Count == 1 && string.IsNullOrEmpty(error))
        {
            await ReportAsync(shown[0].Destination, shown[0].Result);
            return;
        }

        if (shown.Count == 0 && string.IsNullOrEmpty(error))
        {
            return;
        }

        var lines = shown.Select(o => o.Result.Success
            ? o.Result.FilePath != null ? Texts.Editor("imagesaved", o.Result.FilePath) : o.Result.Message ?? o.Result.Link ?? o.Target
            : $"{Texts.Core("exported_to_error", o.Target)} {o.Result.Message}").ToList();
        if (!string.IsNullOrEmpty(error))
        {
            lines.Add(error);
        }

        bool failed = !string.IsNullOrEmpty(error) || shown.Any(o => !o.Result.Success);
        var last = shown.LastOrDefault(o => o.Result.Success && (o.Result.Link != null || o.Result.FilePath != null))?.Result;
        string? open = last?.Link ?? last?.FilePath;
        await NotifyAsync(shown.LastOrDefault()?.CaptureDetails?.Title ?? "Greenshot", string.Join("\n", lines),
            failed ? NotificationSeverity.Error : NotificationSeverity.Info, last?.FilePath,
            open == null ? null : () => ProcessRunner.StartDetached("xdg-open", open));
    }

    public Task NotifyRecipeAsync(NotificationKind kind, string message, string? link) =>
        NotifyAsync("Greenshot", message, kind switch
        {
            NotificationKind.Error => NotificationSeverity.Error,
            NotificationKind.Warning => NotificationSeverity.Warning,
            _ => NotificationSeverity.Info,
        }, onClick: link == null ? null : () => ProcessRunner.StartDetached("xdg-open", link));

    #region Recipe hotkeys

    public void Register(string id, string description, string hotkey, Action pressed)
    {
        _recipeHotkeys[id] = (description, hotkey, () => Dispatcher.UIThread.Post(pressed));
        ScheduleRebind();
    }

    public void Unregister(string id)
    {
        if (_recipeHotkeys.Remove(id))
        {
            ScheduleRebind();
        }
    }

    /// <summary>
    /// The recipes changed: the portal gets all hotkeys again, once when several change
    /// </summary>
    private void ScheduleRebind() => Dispatcher.UIThread.Post(() =>
    {
        if (_shortcuts == null)
        {
            return;
        }

        _rebindTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _rebindTimer.Stop();
        _rebindTimer.Tick -= OnRebind;
        _rebindTimer.Tick += OnRebind;
        _rebindTimer.Start();
    });

    private void OnRebind(object? sender, EventArgs e)
    {
        _rebindTimer?.Stop();
        _ = BindShortcutsAsync();
    }

    #endregion

    public async Task ReportAsync(IDestination destination, ExportResult result)
    {
        if (result.Cancelled || (result.Success && destination.Designation == WellKnownDestinations.Editor))
        {
            return;
        }

        if (!result.Success)
        {
            await NotifyAsync(Texts.Core("exported_to_error", destination.DisplayName), result.Message ?? Log.FilePath, NotificationSeverity.Error);
            return;
        }

        if (result.FilePath is { } path)
        {
            await NotifyAsync("Greenshot", Texts.Editor("imagesaved", path), NotificationSeverity.Info, path, () => ProcessRunner.StartDetached("xdg-open", path));
        }
        else if (result.Link is { } link)
        {
            await NotifyAsync("Greenshot", result.Message ?? link, NotificationSeverity.Info, onClick: () => ProcessRunner.StartDetached("xdg-open", link));
        }
        else
        {
            await NotifyAsync("Greenshot", result.Message ?? destination.DisplayName, NotificationSeverity.Info);
        }
    }

    private Task NotifyAsync(string summary, string body, NotificationSeverity severity, string? imagePath = null, Action? onClick = null)
    {
        if (_notifications == null || (severity == NotificationSeverity.Info && !_settings.ShowTrayNotification))
        {
            return Task.CompletedTask;
        }

        return _notifications.ShowAsync(summary, body, severity, imagePath, onClick);
    }

    /// <summary>
    /// The menu of the tray icon, with the texts of the Windows context menu
    /// </summary>
    private NativeMenu CreateTrayMenu()
    {
        var menu = new NativeMenu();
        AddItem(menu, Texts.Core("contextmenu_capturearea"), () => _ = CaptureAsync(CaptureMode.Region), _settings.RegionHotkey);
        AddItem(menu, Texts.Core("contextmenu_capturelastregion"), () => _ = CaptureAsync(CaptureMode.LastRegion), _settings.LastregionHotkey);
        if (_compositor.KnowsWindows)
        {
            AddItem(menu, Texts.Core("contextmenu_capturewindow"), () => _ = CaptureAsync(CaptureMode.Window), null);
            AddItem(menu, Texts.Linux("contextmenu_captureactivewindow"), () => _ = CaptureAsync(CaptureMode.ActiveWindow), _settings.WindowHotkey);
        }

        AddItem(menu, Texts.Core("contextmenu_capturefullscreen"), () => _ = CaptureAsync(CaptureMode.FullScreen), _settings.FullscreenHotkey);
        menu.Items.Add(new NativeMenuItemSeparator());
        AddItem(menu, Texts.Core("contextmenu_openfile"), () => _ = AskOpenFileAsync(), null);
        AddItem(menu, Texts.Core("contextmenu_captureclipboard"), () => _ = OpenClipboardAsync(), null);
        AddItem(menu, Texts.Core("contextmenu_openrecentcapture"), OpenCaptureFolder, null);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(CreateQuickSettingsMenu());
        menu.Items.Add(CreateRecipesMenu());
        AddItem(menu, Texts.Core("contextmenu_settings"), ShowSettings, null);
        AddItem(menu, Texts.Linux("open_log"), () => ProcessRunner.StartDetached("xdg-open", Log.FilePath), null);
        menu.Items.Add(new NativeMenuItemSeparator());
        AddItem(menu, $"{Texts.Core("contextmenu_about")} ({Program.Version})", () => ProcessRunner.StartDetached("xdg-open", "https://getgreenshot.org/"), null);
        AddItem(menu, Texts.Core("contextmenu_exit"), Exit, null);
        return menu;
    }

    /// <summary>
    /// At most this many recipe options in the "Automatic steps" block of the quick settings, the others are in Settings > Recipes ("More…")
    /// </summary>
    private const int MaxRecipeQuickSettings = 6;

    /// <summary>
    /// The quick settings of Windows: the settings which are changed often, stored right away
    /// </summary>
    private NativeMenuItem CreateQuickSettingsMenu()
    {
        var quickSettings = new NativeMenu();
        AddCheckItem(quickSettings, Texts.Settings("capture_mousepointer"), _settings.CaptureMousepointer, v => _settings.CaptureMousepointer = v);

        var destinations = new NativeMenu();
        var configured = _settings.OutputDestinations;
        foreach (var destination in RecipeDestinations.OrderBy(d => d.Priority))
        {
            var selected = destination;
            AddCheckItem(destinations, destination.DisplayName, configured.Contains(destination.Designation, StringComparer.OrdinalIgnoreCase),
                isChecked => QuickSettingDestinationChanged(selected.Designation, isChecked));
        }
        quickSettings.Items.Add(new NativeMenuItem(Texts.Settings("destination")) { Menu = destinations });

        var printOptions = new NativeMenu();
        AddCheckItem(printOptions, Texts.Settings("alwaysshowprintoptionsdialog"), _settings.OutputPrintPromptOptions, v => _settings.OutputPrintPromptOptions = v);
        AddCheckItem(printOptions, Texts.Core("printoptions_allowrotate"), _settings.OutputPrintAllowRotate, v => _settings.OutputPrintAllowRotate = v);
        AddCheckItem(printOptions, Texts.Core("printoptions_allowenlarge"), _settings.OutputPrintAllowEnlarge, v => _settings.OutputPrintAllowEnlarge = v);
        AddCheckItem(printOptions, Texts.Core("printoptions_allowshrink"), _settings.OutputPrintAllowShrink, v => _settings.OutputPrintAllowShrink = v);
        AddCheckItem(printOptions, Texts.Core("printoptions_allowcenter"), _settings.OutputPrintCenter, v => _settings.OutputPrintCenter = v);
        AddCheckItem(printOptions, Texts.Core("printoptions_inverted"), _settings.OutputPrintInverted, v => _settings.OutputPrintInverted = v);
        AddCheckItem(printOptions, Texts.Core("printoptions_printgrayscale"), _settings.OutputPrintGrayscale, v => _settings.OutputPrintGrayscale = v);
        AddCheckItem(printOptions, Texts.Core("printoptions_printmonochrome"), _settings.OutputPrintMonochrome, v => _settings.OutputPrintMonochrome = v);
        AddCheckItem(printOptions, Texts.Core("printoptions_timestamp"), _settings.OutputPrintFooter, v => _settings.OutputPrintFooter = v);
        quickSettings.Items.Add(new NativeMenuItem(Texts.Settings("printoptions")) { Menu = printOptions });

        var effects = new NativeMenu();
        AddCheckItem(effects, Texts.Settings("playsound"), _settings.PlayCameraSound, v => _settings.PlayCameraSound = v);
        AddCheckItem(effects, Texts.Settings("shownotify"), _settings.ShowTrayNotification, v => _settings.ShowTrayNotification = v);
        quickSettings.Items.Add(new NativeMenuItem(Texts.Settings("visualization")) { Menu = effects });

        AddRecipeQuickSettings(quickSettings);
        return new NativeMenuItem(Texts.Core("contextmenu_quicksettings")) { Menu = quickSettings };
    }

    /// <summary>
    /// The picker excludes the others, nothing selected means the picker
    /// </summary>
    private void QuickSettingDestinationChanged(string designation, bool isChecked)
    {
        var destinations = _settings.OutputDestinations.ToList();
        if (isChecked)
        {
            if (designation == WellKnownDestinations.Picker)
            {
                destinations.Clear();
            }
            else
            {
                destinations.RemoveAll(d => d == WellKnownDestinations.Picker);
            }

            if (!destinations.Contains(designation, StringComparer.OrdinalIgnoreCase))
            {
                destinations.Add(designation);
            }
        }
        else
        {
            destinations.RemoveAll(d => d.Equals(designation, StringComparison.OrdinalIgnoreCase));
        }

        _settings.OutputDestinations = destinations.Count > 0 ? destinations : [WellKnownDestinations.Picker];
    }

    /// <summary>
    /// The options recipes offer in the quick settings ("quickSettings": true), in one "Automatic steps" block: a switch
    /// is a checked item, a choice a submenu with one item per value, at most <see cref="MaxRecipeQuickSettings"/> of them,
    /// and "More…" opens Settings > Recipes. The value is stored right away.
    /// </summary>
    private void AddRecipeQuickSettings(NativeMenu quickSettings)
    {
        if (RecipeServices.Manager == null)
        {
            return;
        }

        List<(FlowDefinition Recipe, RecipeOption Option)> options;
        try
        {
            // The extensions (border, drop shadow, caption, ...) first, then the recipes
            var manager = RecipeManager.Instance;
            options = manager.GetAllExtensions()
                .OrderBy(e => e.Extends?.Order ?? 0).ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                .Cast<FlowDefinition>()
                .Concat(manager.GetAllRecipes().OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase))
                .Where(r => r?.Options != null)
                .SelectMany(r => r.Options
                    .Where(o => o != null && o.QuickSettings && (o.Type == ContractDataType.Boolean || (o.Type == ContractDataType.Enum && o.Choices != null)))
                    .Select(o => (Recipe: r, Option: o)))
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Warn("Couldn't add the options of the recipes to the quick settings", ex);
            return;
        }

        if (options.Count == 0)
        {
            return;
        }

        quickSettings.Items.Add(new NativeMenuItemSeparator());
        quickSettings.Items.Add(new NativeMenuItem(Texts.Core("quicksettings_automaticsteps")) { IsEnabled = false });

        // The same label of two recipes gets the recipe name in front
        var duplicateLabels = new HashSet<string>(options.GroupBy(o => o.Option.DisplayLabel, StringComparer.CurrentCultureIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key), StringComparer.CurrentCultureIgnoreCase);
        foreach (var (recipe, option) in options.Take(MaxRecipeQuickSettings))
        {
            var label = duplicateLabels.Contains(option.DisplayLabel) ? $"{RecipeText.Translate(recipe.Name ?? recipe.Id)}: {option.DisplayLabel}" : option.DisplayLabel;
            var value = RecipeOptionStore.GetValue(recipe, option);
            if (option.Type == ContractDataType.Boolean)
            {
                var switchItem = AddCheckItem(quickSettings, label, value is true, isChecked => RecipeOptionStore.SetValue(recipe.Id, option, isChecked));
                if (!string.IsNullOrEmpty(option.DisplayDescription))
                {
                    switchItem.ToolTip = option.DisplayDescription;
                }
                continue;
            }

            var choiceList = new NativeMenu();
            foreach (var choice in option.Choices.Where(c => c != null))
            {
                var selectedChoice = choice;
                var isCurrent = string.Equals(choice.Value, value as string, StringComparison.OrdinalIgnoreCase);
                // Like a radio button: clicking the current value keeps it
                AddCheckItem(choiceList, choice.DisplayLabel, isCurrent, _ => RecipeOptionStore.SetValue(recipe.Id, option, selectedChoice.Value), MenuItemToggleType.Radio);
            }
            quickSettings.Items.Add(new NativeMenuItem(label) { Menu = choiceList });
        }

        AddItem(quickSettings, Texts.Core("quicksettings_automaticsteps_more"), () => ShowSettings("recipes"), null);
    }

    /// <summary>
    /// A checked item: the value is stored right away and the menu is built again, so it shows the new state
    /// </summary>
    private NativeMenuItem AddCheckItem(NativeMenu menu, string header, bool isChecked, Action<bool> setter,
        MenuItemToggleType toggleType = MenuItemToggleType.CheckBox)
    {
        var item = new NativeMenuItem(Texts.WithoutAccessKey(header)) { ToggleType = toggleType, IsChecked = isChecked };
        item.Click += (_, _) =>
        {
            setter(!isChecked);
            _settings.Save();
            Dispatcher.UIThread.Post(() =>
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Menu = CreateTrayMenu();
                }
            });
        };
        menu.Items.Add(item);
        return item;
    }

    /// <summary>
    /// The Recipes menu of Windows: the recipes of ContextMenu triggers (groups as submenus, Recipes first), Import and Reload
    /// </summary>
    private NativeMenuItem CreateRecipesMenu()
    {
        var recipes = new NativeMenu();
        var triggers = RecipeServices.Manager == null ? [] : TriggerManager.Instance.GetContextMenuTriggers();
        foreach (var group in triggers.GroupBy(t => t.Group ?? "Recipes")
                     .OrderBy(g => g.Key == "Recipes" ? 0 : 1).ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase))
        {
            var target = recipes;
            if (group.Key != "Recipes")
            {
                var sub = new NativeMenuItem(group.Key) { Menu = new NativeMenu() };
                recipes.Items.Add(sub);
                target = sub.Menu;
            }

            foreach (var trigger in group.OrderBy(t => t.Order).ThenBy(t => t.MenuItemText, StringComparer.CurrentCultureIgnoreCase))
            {
                var item = new NativeMenuItem(trigger.MenuItemText);
                item.Click += (_, _) => trigger.Fire();
                target.Items.Add(item);
            }
        }

        if (recipes.Items.Count > 0)
        {
            recipes.Items.Add(new NativeMenuItemSeparator());
        }

        AddItem(recipes, Texts.Linux("recipe_import"), () => _ = AskImportRecipeAsync(), null);
        AddItem(recipes, Texts.Linux("recipe_reload"), () => RecipeManager.Instance.ReloadRecipes(), null);
        AddItem(recipes, Texts.Core("contextmenu_managerecipes"), _recipeEditor.OpenRecipeManager, null);
        AddItem(recipes, Texts.Core("contextmenu_recipeeditor"), () => _recipeEditor.OpenEditor(), null);
        return new NativeMenuItem(Texts.Linux("recipes")) { Menu = recipes };
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new TrayIcon
        {
            Icon = AppIcons.Tray,
            ToolTipText = "Greenshot",
            Menu = CreateTrayMenu(),
            IsVisible = true,
        };
        // A click on the icon captures a region, like the default of the Windows version
        _trayIcon.Clicked += (_, _) => _ = CaptureAsync(CaptureMode.Region);
        TrayIcon.SetIcons(Application.Current!, [_trayIcon]);
    }

    private static void AddItem(NativeMenu menu, string header, Action action, string? hotkey)
    {
        header = Texts.WithoutAccessKey(header);
        var item = new NativeMenuItem(string.IsNullOrWhiteSpace(hotkey) ? header : $"{header}\t{hotkey}");
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    /// <summary>
    /// Open a capture in a new editor window; the editor gets its own copy, the capture is disposed by the caller
    /// </summary>
    private void OpenEditor(Capture capture)
    {
        var surface = new EditorSurface(capture.Image.Copy());
        ShowEditor(surface, capture.Details, null);
    }

    public void ShowEditor(EditorSurface surface, CaptureDetails details, string? filePath)
    {
        var window = new EditorWindow(this, surface, details, filePath);
        window.Show();
        window.Activate();
    }

    /// <summary>
    /// Open an image or a .greenshot file in the editor
    /// </summary>
    public async Task OpenFileAsync(string path)
    {
        try
        {
            EditorSurface surface;
            string? warning = null;
            if (Path.GetExtension(path).Equals(GreenshotFile.Extension, StringComparison.OrdinalIgnoreCase))
            {
                var content = GreenshotFile.Load(path);
                surface = new EditorSurface(content.Image);
                if (content.ElementsJson != null)
                {
                    surface.LoadElements(content.ElementsJson);
                }

                warning = content.Warning;
            }
            else
            {
                var image = SKBitmap.Decode(path) ?? throw new InvalidDataException("The file is not an image Greenshot can read");
                surface = new EditorSurface(image);
            }

            surface.IsModified = false;
            var details = new CaptureDetails { Title = Path.GetFileNameWithoutExtension(path), DateTime = File.GetLastWriteTime(path) };
            ShowEditor(surface, details, path);
            if (warning != null)
            {
                await NotifyAsync("Greenshot", warning, NotificationSeverity.Warning);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException or NotSupportedException)
        {
            Log.Error($"Opening {path} failed", ex);
            await NotifyAsync("Could not open the file", $"{path}: {ex.Message}", NotificationSeverity.Error);
        }
    }

    private async Task AskOpenFileAsync()
    {
        if (_fileChooser == null)
        {
            await NotifyAsync("Greenshot", "There is no file dialog (FileChooser portal), use: greenshot open <file>", NotificationSeverity.Warning);
            return;
        }

        var path = await _fileChooser.OpenFileAsync("Open an image", SaveDialog.OpenFilters);
        if (path != null)
        {
            await OpenFileAsync(path);
        }
    }

    private async Task OpenClipboardAsync()
    {
        var data = await SystemClipboard.GetImageAsync();
        var image = data != null ? SKBitmap.Decode(data) : null;
        if (image == null)
        {
            await NotifyAsync("Greenshot", "There is no image on the clipboard", NotificationSeverity.Warning);
            return;
        }

        var surface = new EditorSurface(image);
        surface.IsModified = false;
        ShowEditor(surface, new CaptureDetails { Title = "Clipboard" }, null);
    }

    private void OpenCaptureFolder()
    {
        var folder = Path.GetDirectoryName(FileDestination.GetDefaultPath(_settings, new CaptureDetails()))!;
        Directory.CreateDirectory(folder);
        ProcessRunner.StartDetached("xdg-open", folder);
    }

    /// <summary>
    /// Ask the GlobalShortcuts portal for the hotkeys. Where it doesn't exist (e.g. Sway) the user binds "greenshot capture ..."
    /// </summary>
    private async Task RegisterShortcutsAsync()
    {
        if (_portal == null || !await GlobalShortcutsPortal.IsAvailableAsync(_portal))
        {
            Log.Info("The desktop has no GlobalShortcuts portal, bind \"greenshot capture region\" etc. to keys in the compositor");
            return;
        }

        try
        {
            _shortcuts = new GlobalShortcutsPortal(_portal);
            _shortcuts.Activated += id => Dispatcher.UIThread.Post(() =>
            {
                CaptureMode? mode = id switch
                {
                    ShortcutRegion => CaptureMode.Region,
                    ShortcutWindow => _compositor.KnowsWindows && !_settings.CaptureWindowsInteractive ? CaptureMode.ActiveWindow : CaptureMode.Window,
                    ShortcutFullscreen => CaptureMode.FullScreen,
                    ShortcutLastRegion => CaptureMode.LastRegion,
                    _ => null,
                };
                if (mode != null)
                {
                    _ = CaptureAsync(mode.Value);
                }
                else if (_recipeHotkeys.TryGetValue(id, out var recipe))
                {
                    recipe.Pressed();
                }
            });
            await BindShortcutsAsync();
        }
        catch (Exception ex)
        {
            Log.Warn("Registering the global shortcuts failed", ex);
        }
    }

    /// <summary>
    /// The capture hotkeys and those of recipe triggers
    /// </summary>
    private async Task BindShortcutsAsync()
    {
        if (_shortcuts == null)
        {
            return;
        }

        try
        {
            var requests = new List<ShortcutRequest>
            {
                new(ShortcutRegion, "Capture region", GlobalShortcutsPortal.ToXdgTrigger(_settings.RegionHotkey)),
                new(ShortcutWindow, "Capture window", GlobalShortcutsPortal.ToXdgTrigger(_settings.WindowHotkey)),
                new(ShortcutFullscreen, "Capture full screen", GlobalShortcutsPortal.ToXdgTrigger(_settings.FullscreenHotkey)),
                new(ShortcutLastRegion, "Capture last region", GlobalShortcutsPortal.ToXdgTrigger(_settings.LastregionHotkey)),
            };
            requests.AddRange(_recipeHotkeys.Select(h => new ShortcutRequest(h.Key, h.Value.Description, GlobalShortcutsPortal.ToXdgTrigger(h.Value.Hotkey))));
            var assigned = await _shortcuts.BindAsync(requests);
            Log.Info("Global shortcuts: " + string.Join(", ", assigned.Select(a => $"{a.Key}={a.Value}")));
        }
        catch (Exception ex)
        {
            Log.Warn("Binding the global shortcuts failed", ex);
        }
    }

    /// <summary>
    /// The monitors as the toolkit sees them, for desktops whose compositor doesn't tell the layout
    /// </summary>
    private ScreenLayout GetPlatformLayout()
    {
        var screens = GetPlatformScreens()
            .Select((s, i) => new ScreenInfo(s.DisplayName ?? $"screen{i}", new LayoutRect(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height),
                s.Scaling, s.IsPrimary))
            .ToList();
        return new ScreenLayout(screens);
    }

    private IReadOnlyList<Screen> GetPlatformScreens()
    {
        // Screens are only available on a top level, a window which is never shown is enough
        _screensHelper ??= new Window();
        return _screensHelper.Screens.All;
    }

    internal void Exit()
    {
        _settings.Save();
        // greenshot-mcp waits for the next start instead of starting Greenshot again
        _channel?.NotifyShutdownAsync("exit").Wait(TimeSpan.FromSeconds(1));
        _desktop.Shutdown();
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _shortcuts?.Dispose();
        _notifications?.Dispose();
        _portal?.Dispose();
        _channel?.Dispose();
        _screensHelper?.Close();
    }
}
