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

using Greenshot.Linux.Core.Capturing;

namespace Greenshot.Linux.Core.Ipc;

public enum AppCommandKind
{
    /// <summary>
    /// Start Greenshot, or do nothing when it already runs
    /// </summary>
    Start,
    Capture,
    Exit,
    Help,
    Version,
}

/// <summary>
/// What the command line asks for. The same text goes over the socket to a running Greenshot,
/// so "greenshot capture region" bound to a key starts a capture in the running instance.
/// </summary>
public sealed record AppCommand(AppCommandKind Kind, CaptureMode? Mode = null)
{
    public const string Usage = """
        Usage: greenshot [command]

        Without a command Greenshot starts in the tray, or does nothing when it already runs.

        Commands:
          capture region        Select a region and capture it (PrintScreen)
          capture window        Select a window and capture it (Sway and Hyprland)
          capture activewindow  Capture the active window (Alt + PrintScreen, Sway and Hyprland)
          capture fullscreen    Capture all screens (Ctrl + PrintScreen)
          capture last          Capture the last region again (Shift + PrintScreen)
          exit                  Stop the running Greenshot
          --help                Show this text
          --version             Show the version

        Wayland doesn't let applications grab keys. Desktops with the GlobalShortcuts portal (KDE, GNOME 48+, Hyprland)
        ask you to confirm Greenshot's keys at the first start. Elsewhere bind the commands in the compositor, e.g. for Sway:
          bindsym Print exec greenshot capture region
        """;

    /// <summary>
    /// Parse the command line, null when it is not understood
    /// </summary>
    public static AppCommand? Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return new AppCommand(AppCommandKind.Start);
        }

        var verb = args[0].TrimStart('-', '/').ToLowerInvariant();
        switch (verb)
        {
            case "help" or "h" or "?":
                return new AppCommand(AppCommandKind.Help);
            case "version":
                return new AppCommand(AppCommandKind.Version);
            case "exit":
                return new AppCommand(AppCommandKind.Exit);
            case "capture" when args.Count >= 2:
                var mode = ParseMode(args[1]);
                return mode == null ? null : new AppCommand(AppCommandKind.Capture, mode);
            default:
                return null;
        }
    }

    private static CaptureMode? ParseMode(string text) => text.ToLowerInvariant() switch
    {
        "region" => CaptureMode.Region,
        "window" => CaptureMode.Window,
        "activewindow" or "active-window" or "active" => CaptureMode.ActiveWindow,
        "fullscreen" or "full" or "screen" => CaptureMode.FullScreen,
        "last" or "lastregion" or "last-region" => CaptureMode.LastRegion,
        _ => null,
    };

    /// <summary>
    /// The text sent over the socket, Parse reads it back
    /// </summary>
    public string ToWire() => Kind switch
    {
        AppCommandKind.Capture => "capture " + Mode switch
        {
            CaptureMode.Window => "window",
            CaptureMode.ActiveWindow => "activewindow",
            CaptureMode.FullScreen => "fullscreen",
            CaptureMode.LastRegion => "last",
            _ => "region",
        },
        AppCommandKind.Exit => "exit",
        _ => "start",
    };
}
