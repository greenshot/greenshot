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

using Greenshot.Linux.Core.Ipc;

namespace Greenshot.Linux.Recipes.Ipc;

/// <summary>
/// What each source may ask Greenshot to do, and greenshot: links (the IpcSecurityDispatcher of Windows). The terminal may
/// do everything, a link (a web page) only show windows and start recipes which allow it, a file manager only open files.
/// </summary>
public static class IpcPolicy
{
    public static readonly IReadOnlySet<string> AllowedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        IpcSources.CliCommand, CliCommandParser.AppCommandName, "LIST_RECIPES", "DESCRIBE_RECIPE", "RUN_RECIPE", "OPEN_FILE", "VERSION", "SETTINGS",
        "ABOUT", "LOG_VIEWER", "RECIPE_EDITOR", "RECIPE_MANAGER", "EXIT", "RELOAD_CONFIG", "URL_SCHEME",
    };

    private static readonly Dictionary<string, HashSet<string>> SourceCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        [IpcSources.UrlScheme] = new(StringComparer.OrdinalIgnoreCase)
        {
            IpcSources.CliCommand, "URL_SCHEME", "RUN_RECIPE", "SETTINGS", "ABOUT", "LOG_VIEWER", "RECIPE_EDITOR", "RECIPE_MANAGER",
        },
        [IpcSources.OpenWith] = new(StringComparer.OrdinalIgnoreCase) { IpcSources.CliCommand, "OPEN_FILE" },
        // The commands of AI tools are added by them
        [IpcSources.Mcp] = new(StringComparer.OrdinalIgnoreCase),
    };

    /// <summary>
    /// Commands of other parts (the AI tools), allowed only for their source
    /// </summary>
    public static void AddSourceCommands(string source, IEnumerable<string> commands)
    {
        if (!SourceCommands.TryGetValue(source, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SourceCommands[source] = set;
        }

        foreach (var command in commands)
        {
            set.Add(command);
            // A command the terminal has too (VERSION) stays the terminal's
            if (!AllowedCommands.Contains(command))
            {
                ExtraCommands.Add(command);
            }
        }
    }

    private static readonly HashSet<string> ExtraCommands = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsCommandAllowedForSource(string? command, string? source)
    {
        if (string.IsNullOrEmpty(command))
        {
            return false;
        }

        bool extra = ExtraCommands.Contains(command);
        if (!AllowedCommands.Contains(command) && !extra)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(source) && SourceCommands.TryGetValue(source, out var allowed))
        {
            return allowed.Contains(command);
        }

        // The terminal: everything but the commands of other sources
        return !extra;
    }

    /// <summary>
    /// The command of a greenshot: link and its parameters (greenshot://recipe/id?key=value, greenshot://settings, ...),
    /// null when it is no command
    /// </summary>
    public static string? ParseUrlSchemeCommand(string? rawUrl, out Dictionary<string, string> parameters)
    {
        parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return null;
        }

        string text = rawUrl.Trim();
        if (text.StartsWith("greenshot://", StringComparison.OrdinalIgnoreCase))
        {
            text = text[12..];
        }
        else if (text.StartsWith("greenshot:", StringComparison.OrdinalIgnoreCase))
        {
            text = text[10..];
        }
        else
        {
            return null;
        }

        string pathAndAction = text;
        int question = text.IndexOf('?');
        if (question >= 0)
        {
            pathAndAction = text[..question];
            foreach (var pair in text[(question + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = pair.Split('=', 2);
                try
                {
                    parameters[Uri.UnescapeDataString(keyValue[0]).Trim()] = keyValue.Length > 1 ? Uri.UnescapeDataString(keyValue[1]) : string.Empty;
                }
                catch (UriFormatException)
                {
                    return null;
                }
            }
        }

        pathAndAction = pathAndAction.Trim('/');
        if (pathAndAction.StartsWith("recipe/", StringComparison.OrdinalIgnoreCase))
        {
            parameters["recipe"] = Uri.UnescapeDataString(pathAndAction[7..]);
            return "RUN_RECIPE";
        }

        return pathAndAction.ToLowerInvariant() switch
        {
            "settings" => "SETTINGS",
            "about" => "ABOUT",
            "log" => "LOG_VIEWER",
            "recipe-editor" => "RECIPE_EDITOR",
            "recipe-manager" => "RECIPE_MANAGER",
            // The commands which carry a link are never its target
            "cli" or "url_scheme" or "" => null,
            // Others are checked against the whitelist of the source: greenshot://exit is refused there
            _ => pathAndAction.ToUpperInvariant(),
        };
    }
}
