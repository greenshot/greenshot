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

    /// <summary>
    /// Open an image or .greenshot file in the editor
    /// </summary>
    Open,

    /// <summary>
    /// Open the image on the clipboard in the editor
    /// </summary>
    OpenClipboard,

    /// <summary>
    /// Show the settings window
    /// </summary>
    Settings,
    Exit,
    Help,
    Version,

    /// <summary>
    /// Run a recipe, with the values of its arguments
    /// </summary>
    Recipe,
}

/// <summary>
/// What the command line asks for. The same text goes over the socket to a running Greenshot,
/// so "greenshot capture region" bound to a key starts a capture in the running instance.
/// </summary>
public sealed record AppCommand(AppCommandKind Kind, CaptureMode? Mode = null, string? Path = null, string? RecipeId = null,
    IReadOnlyDictionary<string, string>? Parameters = null)
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
          open <file>           Open an image or .greenshot file in the editor, also: greenshot <file>
          clipboard             Open the image on the clipboard in the editor
          recipe <id> [key=value ...]
                                Run a recipe, with values of its arguments (also: --recipe <id> --key value)
          settings              Show the settings
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
            case "open" when args.Count >= 2:
                return new AppCommand(AppCommandKind.Open, Path: ToAbsolute(string.Join(' ', args.Skip(1))));
            case "clipboard":
                return new AppCommand(AppCommandKind.OpenClipboard);
            case "settings" or "preferences":
                return new AppCommand(AppCommandKind.Settings);
            case "recipe" or "r" when args.Count >= 2:
                return ParseRecipe(args);
            default:
                // Like Greenshot.exe: a file on the command line is opened in the editor
                return File.Exists(args[0]) || args[0].StartsWith("file://", StringComparison.Ordinal)
                    ? new AppCommand(AppCommandKind.Open, Path: ToAbsolute(args[0]))
                    : null;
        }
    }

    /// <summary>
    /// The arguments of a recipe as key=value, --key=value or --key value, like greenshot-cli --recipe of Windows
    /// </summary>
    private static AppCommand? ParseRecipe(IReadOnlyList<string> args)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 2; i < args.Count; i++)
        {
            string arg = args[i];
            if (arg.StartsWith('-'))
            {
                string name = arg.TrimStart('-');
                int equals = name.IndexOf('=');
                if (equals > 0)
                {
                    parameters[Uri.UnescapeDataString(name[..equals])] = Uri.UnescapeDataString(name[(equals + 1)..]);
                }
                else if (i + 1 < args.Count && name.Length > 0)
                {
                    parameters[Uri.UnescapeDataString(name)] = Uri.UnescapeDataString(args[++i]);
                }
                else
                {
                    return null;
                }
            }
            else if (arg.IndexOf('=') is > 0 and var separator)
            {
                parameters[Uri.UnescapeDataString(arg[..separator])] = Uri.UnescapeDataString(arg[(separator + 1)..]);
            }
            else
            {
                return null;
            }
        }

        return new AppCommand(AppCommandKind.Recipe, RecipeId: args[1], Parameters: parameters);
    }

    /// <summary>
    /// Read a line sent over the socket; the path of "open" may contain spaces
    /// </summary>
    public static AppCommand? ParseWire(string line) =>
        line.StartsWith("open ", StringComparison.Ordinal)
            ? new AppCommand(AppCommandKind.Open, Path: line[5..])
            : Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// The running Greenshot has another working directory, paths are sent absolute; file:// URIs from file managers too
    /// </summary>
    private static string ToAbsolute(string path) =>
        path.StartsWith("file://", StringComparison.Ordinal) ? new Uri(path).LocalPath : System.IO.Path.GetFullPath(path);

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
        AppCommandKind.Open => "open " + Path,
        AppCommandKind.OpenClipboard => "clipboard",
        AppCommandKind.Settings => "settings",
        // The values are escaped, they can contain spaces
        AppCommandKind.Recipe => "recipe " + RecipeId + string.Concat((Parameters ?? new Dictionary<string, string>())
            .Select(p => $" {Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}")),
        AppCommandKind.Exit => "exit",
        _ => "start",
    };
}
