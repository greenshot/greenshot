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
using Greenshot.Linux.Destinations;
using Greenshot.Linux.Helpers;

namespace Greenshot.Linux;

/// <summary>
/// The running Greenshot: the tray icon, the commands from the command line and the hotkeys, and the capture flow
/// from taking the picture to the destinations. The MainForm of the Windows version without a form.
/// </summary>
internal sealed class GreenshotController : IDisposable
{
    private const string ShortcutRegion = "capture-region";
    private const string ShortcutWindow = "capture-window";
    private const string ShortcutFullscreen = "capture-fullscreen";
    private const string ShortcutLastRegion = "capture-last-region";

    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly CoreSettings _settings = CoreSettings.Load();
    private readonly ICompositor _compositor = CompositorDetector.Detect();
    private readonly List<IDestination> _destinations = new();
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
    }

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

        FileChooserPortal? fileChooser = _portal != null && await FileChooserPortal.IsAvailableAsync(_portal) ? new FileChooserPortal(_portal) : null;
        var saveDialog = new SaveDialog(fileChooser);
        _destinations.Add(new FileDestination(_settings, saveDialog.AskAsync));
        _destinations.Add(new FileWithDialogDestination(_settings, saveDialog));
        _destinations.Add(new ClipboardDestination());

        CreateTrayIcon();
        await RegisterShortcutsAsync();

        if (startCommand.Kind == AppCommandKind.Capture && startCommand.Mode is { } mode)
        {
            await CaptureAsync(mode);
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
        var command = AppCommand.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (command == null)
        {
            return Task.FromResult($"error: unknown command {line}");
        }

        Dispatcher.UIThread.Post(() =>
        {
            switch (command.Kind)
            {
                case AppCommandKind.Capture when command.Mode is { } mode:
                    _ = CaptureAsync(mode);
                    break;
                case AppCommandKind.Exit:
                    Exit();
                    break;
            }
        });
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

    private async Task ReportAsync(IDestination destination, ExportResult result)
    {
        if (result.Cancelled)
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
            await NotifyAsync("Capture saved", path, NotificationSeverity.Info, path, () => ProcessRunner.StartDetached("xdg-open", path));
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

    private void CreateTrayIcon()
    {
        var menu = new NativeMenu();
        AddItem(menu, "Capture region", () => _ = CaptureAsync(CaptureMode.Region), _settings.RegionHotkey);
        AddItem(menu, "Capture last region", () => _ = CaptureAsync(CaptureMode.LastRegion), _settings.LastregionHotkey);
        if (_compositor.KnowsWindows)
        {
            AddItem(menu, "Capture window", () => _ = CaptureAsync(CaptureMode.Window), null);
            AddItem(menu, "Capture active window", () => _ = CaptureAsync(CaptureMode.ActiveWindow), _settings.WindowHotkey);
        }

        AddItem(menu, "Capture full screen", () => _ = CaptureAsync(CaptureMode.FullScreen), _settings.FullscreenHotkey);
        menu.Items.Add(new NativeMenuItemSeparator());
        AddItem(menu, "Open the capture folder", OpenCaptureFolder, null);
        AddItem(menu, "Edit the settings file", () => ProcessRunner.StartDetached("xdg-open", _settings.FilePath), null);
        AddItem(menu, "Open the log", () => ProcessRunner.StartDetached("xdg-open", Log.FilePath), null);
        menu.Items.Add(new NativeMenuItemSeparator());
        AddItem(menu, $"About Greenshot {Program.Version}", () => ProcessRunner.StartDetached("xdg-open", "https://getgreenshot.org/"), null);
        AddItem(menu, "Exit", Exit, null);

        _trayIcon = new TrayIcon
        {
            Icon = AppIcons.Tray,
            ToolTipText = "Greenshot",
            Menu = menu,
            IsVisible = true,
        };
        // A click on the icon captures a region, like the default of the Windows version
        _trayIcon.Clicked += (_, _) => _ = CaptureAsync(CaptureMode.Region);
        TrayIcon.SetIcons(Application.Current!, [_trayIcon]);
    }

    private static void AddItem(NativeMenu menu, string header, Action action, string? hotkey)
    {
        var item = new NativeMenuItem(string.IsNullOrWhiteSpace(hotkey) ? header : $"{header}\t{hotkey}");
        item.Click += (_, _) => action();
        menu.Items.Add(item);
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
                    ShortcutWindow => _compositor.KnowsWindows ? CaptureMode.ActiveWindow : CaptureMode.Window,
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
