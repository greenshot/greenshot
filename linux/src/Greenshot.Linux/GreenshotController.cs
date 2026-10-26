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
using Avalonia.Styling;

namespace Greenshot.Linux;

/// <summary>
/// The running Greenshot: the tray icon, the commands from the command line and the hotkeys, and the capture flow
/// from taking the picture to the destinations. The MainForm of the Windows version without a form.
/// </summary>
internal sealed class GreenshotController : ISettingsHost, IDisposable
{
    private const string ShortcutRegion = "capture-region";
    private const string ShortcutWindow = "capture-window";
    private const string ShortcutFullscreen = "capture-fullscreen";
    private const string ShortcutLastRegion = "capture-last-region";

    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly CoreSettings _settings = CoreSettings.Load();
    private readonly ICompositor _compositor = CompositorDetector.Detect();
    private readonly List<IDestination> _destinations = new();
    private readonly EditorSettings _editorSettings;
    private FileChooserPortal? _fileChooser;
    private DesktopPortal? _portal;
    private DesktopNotifications? _notifications;
    private GlobalShortcutsPortal? _shortcuts;
    private ScreenCaptureService? _captureService;
    private InstanceChannel? _channel;
    private TrayIcon? _trayIcon;
    private Window? _screensHelper;
    private bool _capturing;

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

    public void ShowSettings()
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(this);
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

    public EditorSettings EditorSettings => _editorSettings;

    public FileChooserPortal? FileChooser => _fileChooser;

    public IReadOnlyList<IDestination> ExportDestinations =>
        _destinations.Where(d => d.Designation != WellKnownDestinations.Editor).OrderBy(d => d.Priority).ToList();

    public async Task StartAsync(AppCommand startCommand)
    {
        _channel = InstanceChannel.TryListen(HandleCommandAsync);
        if (_channel == null)
        {
            // Another Greenshot started at the same moment and won
            await InstanceChannel.TrySendAsync(startCommand.ToWire());
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

        CreateTrayIcon();
        await RegisterShortcutsAsync();
        await ShowWelcomeAsync();

        await RunAsync(startCommand);
    }

    private async Task RunAsync(AppCommand command)
    {
        switch (command.Kind)
        {
            case AppCommandKind.Capture when command.Mode is { } mode:
                await CaptureAsync(mode);
                break;
            case AppCommandKind.Open when command.Path != null:
                await OpenFileAsync(command.Path);
                break;
            case AppCommandKind.OpenClipboard:
                await OpenClipboardAsync();
                break;
            case AppCommandKind.Settings:
                ShowSettings();
                break;
            case AppCommandKind.Exit:
                Exit();
                break;
        }
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

    private Task<string> HandleCommandAsync(string line)
    {
        var command = AppCommand.ParseWire(line);
        if (command == null)
        {
            return Task.FromResult($"error: unknown command {line}");
        }

        Dispatcher.UIThread.Post(() => _ = RunAsync(command));
        return Task.FromResult("ok");
    }

    /// <summary>
    /// Take a capture and send it to the destinations. A capture which is started while another one is running is ignored,
    /// like a second press of the hotkey on Windows.
    /// </summary>
    public async Task CaptureAsync(CaptureMode mode)
    {
        if (_captureService == null)
        {
            await NotifyAsync("Greenshot can't capture", "No way to capture the screen was found, see the log.", NotificationSeverity.Error);
            return;
        }

        if (_capturing)
        {
            Log.Debug($"A capture is running, ignoring {mode}");
            return;
        }

        _capturing = true;
        try
        {
            using var capture = await TakeCaptureAsync(_captureService, mode);
            if (capture == null)
            {
                return;
            }

            _settings.Save();
            if (_settings.PlayCameraSound)
            {
                ProcessRunner.StartDetached("canberra-gtk-play", "-i", "camera-shutter", "-d", "Greenshot");
            }

            await ExportAsync(capture);
        }
        catch (OperationCanceledException)
        {
            Log.Info("The capture was cancelled");
        }
        catch (Exception ex)
        {
            Log.Error($"Capturing {mode} failed", ex);
            await NotifyAsync("The capture failed", ex.Message, NotificationSeverity.Error);
        }
        finally
        {
            _capturing = false;
        }
    }

    private async Task<Capture?> TakeCaptureAsync(ScreenCaptureService service, CaptureMode mode)
    {
        // Ask before the picture is taken, the delay must not change which window is active
        var active = mode == CaptureMode.ActiveWindow && _compositor.KnowsWindows ? await _compositor.GetActiveWindowAsync() : null;
        if (mode == CaptureMode.ActiveWindow && active == null)
        {
            mode = CaptureMode.Window;
        }

        switch (mode)
        {
            case CaptureMode.FullScreen:
            {
                using var snapshot = await service.SnapshotAsync();
                return await service.CreateFullScreenCaptureAsync(snapshot);
            }
            case CaptureMode.LastRegion when !_settings.LastCapturedRegion.IsEmpty:
            {
                using var snapshot = await service.SnapshotAsync();
                return await service.CreateCaptureAsync(snapshot, snapshot.ToPixels(_settings.LastCapturedRegion), CaptureMode.LastRegion);
            }
            case CaptureMode.ActiveWindow when active != null:
            {
                using var snapshot = await service.SnapshotAsync();
                return await service.CreateCaptureAsync(snapshot, snapshot.ToPixels(active.Bounds), CaptureMode.ActiveWindow, active.Title);
            }
            default:
            {
                // Region and window, and the fallbacks: no last region yet, or no window information on this desktop
                var interactiveMode = mode == CaptureMode.Window ? CaptureMode.Window : CaptureMode.Region;
                var windows = _compositor.KnowsWindows ? await _compositor.GetVisibleWindowsAsync() : Array.Empty<WindowInfo>();
                using var snapshot = await service.SnapshotAsync();
                var selection = await CaptureOverlay.SelectAsync(snapshot, interactiveMode, windows, _compositor, GetPlatformScreens(), _settings);
                if (selection == null)
                {
                    return null;
                }

                return await service.CreateCaptureAsync(snapshot, selection.Pixels, selection.Mode, selection.Title);
            }
        }
    }

    /// <summary>
    /// Send the capture to the configured destinations, or let the user pick one
    /// </summary>
    private async Task ExportAsync(Capture capture)
    {
        var configured = _settings.OutputDestinations;
        List<IDestination> targets;
        if (configured.Contains(WellKnownDestinations.Picker, StringComparer.OrdinalIgnoreCase))
        {
            var picked = await PickerWindow.PickAsync(capture, _destinations.OrderBy(d => d.Priority).ToList());
            if (picked == null)
            {
                return;
            }

            targets = [picked];
        }
        else
        {
            targets = configured
                .Select(designation => _destinations.FirstOrDefault(d => d.Designation.Equals(designation, StringComparison.OrdinalIgnoreCase)))
                .OfType<IDestination>()
                .ToList();
            foreach (var missing in configured.Where(c => _destinations.All(d => !d.Designation.Equals(c, StringComparison.OrdinalIgnoreCase))))
            {
                Log.Warn($"The destination {missing} is not available on Linux yet");
            }

            if (targets.Count == 0)
            {
                targets = [_destinations.First(d => d.Designation == WellKnownDestinations.FileDefault)];
            }
        }

        foreach (var destination in targets)
        {
            var result = await destination.ExportAsync(capture);
            await ReportAsync(destination, result);
        }
    }

    public async Task ReportAsync(IDestination destination, ExportResult result)
    {
        if (result.Cancelled || (result.Success && destination.Designation == WellKnownDestinations.Editor))
        {
            return;
        }

        if (!result.Success)
        {
            await NotifyAsync($"{destination.DisplayName} failed", result.Message ?? "See the log for details", NotificationSeverity.Error);
            return;
        }

        if (result.FilePath is { } path)
        {
            await NotifyAsync("Greenshot", Texts.Editor("imagesaved", path), NotificationSeverity.Info, path, () => ProcessRunner.StartDetached("xdg-open", path));
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
        AddItem(menu, Texts.Core("contextmenu_settings"), ShowSettings, null);
        AddItem(menu, Texts.Linux("open_log"), () => ProcessRunner.StartDetached("xdg-open", Log.FilePath), null);
        menu.Items.Add(new NativeMenuItemSeparator());
        AddItem(menu, $"{Texts.Core("contextmenu_about")} ({Program.Version})", () => ProcessRunner.StartDetached("xdg-open", "https://getgreenshot.org/"), null);
        AddItem(menu, Texts.Core("contextmenu_exit"), Exit, null);
        return menu;
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

    private void ShowEditor(EditorSurface surface, CaptureDetails details, string? filePath)
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
            });
            var assigned = await _shortcuts.BindAsync(
            [
                new ShortcutRequest(ShortcutRegion, "Capture region", GlobalShortcutsPortal.ToXdgTrigger(_settings.RegionHotkey)),
                new ShortcutRequest(ShortcutWindow, "Capture window", GlobalShortcutsPortal.ToXdgTrigger(_settings.WindowHotkey)),
                new ShortcutRequest(ShortcutFullscreen, "Capture full screen", GlobalShortcutsPortal.ToXdgTrigger(_settings.FullscreenHotkey)),
                new ShortcutRequest(ShortcutLastRegion, "Capture last region", GlobalShortcutsPortal.ToXdgTrigger(_settings.LastregionHotkey)),
            ]);
            Log.Info("Global shortcuts: " + string.Join(", ", assigned.Select(a => $"{a.Key}={a.Value}")));
        }
        catch (Exception ex)
        {
            Log.Warn("Registering the global shortcuts failed", ex);
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

    private void Exit()
    {
        _settings.Save();
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
