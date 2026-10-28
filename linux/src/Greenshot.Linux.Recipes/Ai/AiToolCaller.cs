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

using System.Globalization;

namespace Greenshot.Linux.Recipes.Ai;

/// <summary>
/// The program which uses Greenshot through greenshot-mcp (e.g. Claude Code, an editor), identified by Greenshot itself
/// </summary>
public sealed class AiToolClient
{
    /// <summary>
    /// What the user allows (AiToolsAllowedClients): the executable, and for an interpreter (node, python) also its script,
    /// so allowing one Node.js program doesn't allow every one
    /// </summary>
    public required string Identity { get; init; }

    /// <summary>
    /// The executable of the program
    /// </summary>
    public required string ExePath { get; init; }

    /// <summary>
    /// The name to show the user
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The process of greenshot-mcp: one MCP session of the AI tool, the window ids of list_windows belong to it
    /// </summary>
    public int ServerProcessId { get; init; }

    public override string ToString() => $"{DisplayName} ({Identity})";
}

/// <summary>
/// Who is on the other end of a connection with source "mcp", instead of believing its HELLO (the AiToolCaller of
/// Windows): the process must be greenshot-mcp from Greenshot's own folder, and the AI tool is the program which started
/// it (shells and launchers in between are skipped). Everything is read from /proc, which the kernel fills.
/// </summary>
public static class AiToolCaller
{
    public const string McpServerFileName = "greenshot-mcp";

    /// <summary>
    /// Programs which only start the AI tool's command: the AI tool is their parent
    /// </summary>
    private static readonly HashSet<string> Launchers = new(StringComparer.Ordinal)
    {
        "sh", "bash", "dash", "zsh", "fish", "ksh", "env", "setsid", "timeout", "nohup", "stdbuf", "dotnet",
    };

    /// <summary>
    /// Interpreters: the program is the script they run
    /// </summary>
    private static readonly string[] Interpreters = ["node", "nodejs", "bun", "deno", "python", "python3", "ruby", "perl", "java"];

    private const int MaxLauncherDepth = 6;

    /// <summary>
    /// The folder of /proc, replaceable for tests
    /// </summary>
    internal static string ProcRoot { get; set; } = "/proc";

    public static bool TryIdentify(int serverProcessId, string greenshotDirectory, IEnumerable<string>? additionalServerPaths, out AiToolClient? client, out string? error)
    {
        client = null;
        if (serverProcessId <= 0)
        {
            error = "The process of the connection is unknown.";
            return false;
        }

        if (!IsTrustedMcpServer(serverProcessId, greenshotDirectory, additionalServerPaths, out string serverPath))
        {
            error = $"Only {McpServerFileName} from Greenshot's folder may connect as an AI tool, not '{serverPath}'.";
            return false;
        }

        int child = serverProcessId;
        for (int depth = 0; depth <= MaxLauncherDepth; depth++)
        {
            int parent = GetParent(child);
            if (parent <= 1 || !StartedBefore(parent, child))
            {
                break;
            }

            string? exe = GetExecutable(parent);
            if (exe == null)
            {
                break;
            }

            if (!Launchers.Contains(Path.GetFileName(exe)))
            {
                client = Describe(parent, exe, serverProcessId);
                error = null;
                return true;
            }

            child = parent;
        }

        error = $"Could not identify the program which started {McpServerFileName}.";
        return false;
    }

    /// <summary>
    /// greenshot-mcp from Greenshot's folder: the published executable, or "dotnet greenshot-mcp.dll" of a development build
    /// </summary>
    internal static bool IsTrustedMcpServer(int processId, string greenshotDirectory, IEnumerable<string>? additionalPaths, out string serverPath)
    {
        string? exe = GetExecutable(processId);
        serverPath = exe ?? "unknown";
        if (exe == null)
        {
            return false;
        }

        if (Path.GetFileName(exe) == "dotnet")
        {
            // The program is the first argument which is a .dll
            serverPath = GetCommandLine(processId).Skip(1).FirstOrDefault(a => a.EndsWith(".dll", StringComparison.Ordinal)) ?? exe;
            if (!serverPath.EndsWith(McpServerFileName + ".dll", StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (Path.GetFileName(exe) != McpServerFileName)
        {
            return false;
        }

        string directory = Normalize(Path.GetDirectoryName(Path.GetFullPath(serverPath)));
        if (directory == Normalize(greenshotDirectory))
        {
            return true;
        }

        string server = serverPath;
        return (additionalPaths ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim().Trim('"'))
            .Any(p => p == server || Normalize(p) == directory);
    }

    private static string Normalize(string? directory) => (directory ?? string.Empty).TrimEnd('/');

    /// <summary>
    /// The program for the user: an interpreter with its script, else the executable; named after the script's package
    /// </summary>
    internal static AiToolClient Describe(int processId, string exe, int serverProcessId)
    {
        string name = Path.GetFileName(exe);
        string identity = exe;
        string displayName = name;
        if (Interpreters.Any(i => name == i || name.StartsWith(i + ".", StringComparison.Ordinal) || (name.StartsWith(i, StringComparison.Ordinal) && name[i.Length..].All(c => char.IsDigit(c) || c == '.'))))
        {
            var arguments = GetCommandLine(processId).Skip(1).ToList();
            string? script = arguments.FirstOrDefault(a => !a.StartsWith('-'));
            if (script != null)
            {
                string scriptPath = Path.IsPathRooted(script) ? script : Path.Combine(GetWorkingDirectory(processId) ?? "/", script);
                identity = exe + " " + Path.GetFullPath(scriptPath);
                displayName = $"{PackageName(scriptPath) ?? Path.GetFileNameWithoutExtension(scriptPath)} ({name})";
            }
        }

        return new AiToolClient { Identity = identity, ExePath = exe, DisplayName = displayName, ServerProcessId = serverProcessId };
    }

    /// <summary>
    /// The name of the npm package or folder a script belongs to: …/node_modules/@anthropic-ai/claude-code/cli.js → claude-code
    /// </summary>
    private static string? PackageName(string scriptPath)
    {
        var parts = scriptPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int modules = Array.LastIndexOf(parts, "node_modules");
        if (modules >= 0 && modules + 1 < parts.Length)
        {
            string package = parts[modules + 1];
            return package.StartsWith('@') && modules + 2 < parts.Length ? parts[modules + 2] : package;
        }

        return parts.Length >= 2 ? parts[^2] : null;
    }

    internal static string? GetExecutable(int processId)
    {
        try
        {
            var info = new FileInfo(Path.Combine(ProcRoot, processId.ToString(CultureInfo.InvariantCulture), "exe"));
            return info.ResolveLinkTarget(false)?.FullName;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? GetWorkingDirectory(int processId)
    {
        try
        {
            return new DirectoryInfo(Path.Combine(ProcRoot, processId.ToString(CultureInfo.InvariantCulture), "cwd")).ResolveLinkTarget(false)?.FullName;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal static IReadOnlyList<string> GetCommandLine(int processId)
    {
        try
        {
            return File.ReadAllText(Path.Combine(ProcRoot, processId.ToString(CultureInfo.InvariantCulture), "cmdline"))
                .Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>
    /// The fields of /proc/pid/stat after the process name (which can contain spaces and parentheses)
    /// </summary>
    private static string[]? GetStat(int processId)
    {
        try
        {
            string stat = File.ReadAllText(Path.Combine(ProcRoot, processId.ToString(CultureInfo.InvariantCulture), "stat"));
            int end = stat.LastIndexOf(')');
            return end < 0 ? null : stat[(end + 2)..].Split(' ');
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal static int GetParent(int processId) =>
        GetStat(processId) is { Length: > 1 } fields && int.TryParse(fields[1], out int parent) ? parent : 0;

    /// <summary>
    /// A parent id can be reused after the parent exited: it must have started before the child
    /// </summary>
    private static bool StartedBefore(int parent, int child)
    {
        // Field 22 of stat, the 20th after the name
        static long StartTime(int pid) => GetStat(pid) is { Length: > 19 } fields && long.TryParse(fields[19], out long start) ? start : long.MaxValue;
        return StartTime(parent) <= StartTime(child);
    }

    /// <summary>
    /// The name of a process, as the kernel knows it
    /// </summary>
    public static string GetProcessName(int processId)
    {
        try
        {
            return File.ReadAllText(Path.Combine(ProcRoot, processId.ToString(CultureInfo.InvariantCulture), "comm")).Trim();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }
}
