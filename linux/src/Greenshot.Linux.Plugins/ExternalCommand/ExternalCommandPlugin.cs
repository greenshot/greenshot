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

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Core.Plugins;
using Capture = Greenshot.Linux.Core.Capturing.Capture;

namespace Greenshot.Linux.Plugins.ExternalCommand;

/// <summary>
/// One external command: the program, its arguments ({0} is the file), the file format and how it runs
/// </summary>
public sealed record ExternalCommandDefinition(
    string Name,
    string Commandline,
    string Argument = "\"{0}\"",
    string OutputFormat = "png",
    bool RunInBackground = true,
    bool? OutputToClipboard = null,
    bool? UriToClipboard = null);

/// <summary>
/// The [ExternalCommand] section of greenshot.ini, with the keys of the Windows plugin:
/// Commands=GIMP,Krita and per command Commandline.GIMP=, Argument.GIMP=, OutputFormat.GIMP=, RunInbackground.GIMP=
/// </summary>
public sealed class ExternalCommandSettings(CoreSettings settings)
{
    public const string SectionName = "ExternalCommand";

    /// <summary>
    /// The image editors the plugin offers when they are installed, like MS Paint and Paint.NET on Windows
    /// </summary>
    internal static readonly (string Name, string Program)[] BuiltIn =
    [
        ("GIMP", "gimp"),
        ("Krita", "krita"),
        ("Pinta", "pinta"),
        ("KolourPaint", "kolourpaint"),
    ];

    public IReadOnlyList<string> Commands
    {
        get => GetList("Commands");
        private set => settings.SetValue(SectionName, "Commands", string.Join(',', value));
    }

    public bool OutputToClipboard => GetBool("OutputToClipboard", false);

    public bool UriToClipboard => GetBool("UriToClipboard", true);

    public ExternalCommandDefinition? Get(string name)
    {
        if (!Commands.Contains(name))
        {
            return null;
        }

        return new ExternalCommandDefinition(
            name,
            settings.GetValue(SectionName, $"Commandline.{name}") ?? string.Empty,
            settings.GetValue(SectionName, $"Argument.{name}") ?? "\"{0}\"",
            settings.GetValue(SectionName, $"OutputFormat.{name}") ?? "png",
            GetBool($"RunInbackground.{name}", true),
            bool.TryParse(settings.GetValue(SectionName, $"OutputToClipboardCommand.{name}"), out var output) ? output : null,
            bool.TryParse(settings.GetValue(SectionName, $"UriToClipboardCommand.{name}"), out var uri) ? uri : null);
    }

    public void Save(ExternalCommandDefinition command, string? oldName = null)
    {
        if (oldName != null && oldName != command.Name)
        {
            Delete(oldName);
        }

        if (!Commands.Contains(command.Name))
        {
            Commands = [.. Commands, command.Name];
        }

        settings.SetValue(SectionName, $"Commandline.{command.Name}", command.Commandline);
        settings.SetValue(SectionName, $"Argument.{command.Name}", command.Argument);
        settings.SetValue(SectionName, $"OutputFormat.{command.Name}", command.OutputFormat);
        settings.SetValue(SectionName, $"RunInbackground.{command.Name}", command.RunInBackground ? "True" : "False");
    }

    /// <summary>
    /// Remove a command; a built-in one is remembered, so it isn't added again
    /// </summary>
    public void Delete(string name)
    {
        Commands = Commands.Where(c => c != name).ToList();
        foreach (var key in new[] { "Commandline", "Argument", "OutputFormat", "RunInbackground", "OutputToClipboardCommand", "UriToClipboardCommand" })
        {
            settings.RemoveValue(SectionName, $"{key}.{name}");
        }

        if (BuiltIn.Any(b => b.Name == name) && !GetList("DeletedBuildInCommands").Contains(name))
        {
            settings.SetValue(SectionName, "DeletedBuildInCommands", string.Join(',', GetList("DeletedBuildInCommands").Append(name)));
        }
    }

    /// <summary>
    /// Add the installed image editors which the user didn't delete before
    /// </summary>
    public void AddInstalledEditors(Func<string, bool>? isInstalled = null)
    {
        isInstalled ??= ProcessRunner.IsOnPath;
        var deleted = GetList("DeletedBuildInCommands");
        foreach (var (name, program) in BuiltIn)
        {
            if (!Commands.Contains(name) && !deleted.Contains(name) && isInstalled(program))
            {
                Save(new ExternalCommandDefinition(name, program));
            }
        }
    }

    private IReadOnlyList<string> GetList(string key) =>
        (settings.GetValue(SectionName, key) ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private bool GetBool(string key, bool fallback) => bool.TryParse(settings.GetValue(SectionName, key), out var value) ? value : fallback;
}

/// <summary>
/// The ExternalCommand plugin: every command is a destination which starts a program with the capture
/// </summary>
public sealed class ExternalCommandPlugin : IGreenshotPlugin
{
    private readonly CoreSettings _settings;

    public ExternalCommandPlugin(CoreSettings settings)
    {
        _settings = settings;
        Commands = new ExternalCommandSettings(settings);
        Commands.AddInstalledEditors();
    }

    public ExternalCommandSettings Commands { get; }

    public string Name => "ExternalCommand";

    public string DisplayName => Texts.Linux("plugin_externalcommand");

    public IReadOnlyList<IDestination> Destinations =>
        Commands.Commands.Select(Commands.Get).OfType<ExternalCommandDefinition>().Select(c => new ExternalCommandDestination(_settings, Commands, c)).ToList();
}

/// <summary>
/// Saves the capture to a file and starts the command with it. The output can bring a link: it goes to the clipboard
/// and the notification opens it, like on Windows.
/// </summary>
public sealed partial class ExternalCommandDestination(CoreSettings settings, ExternalCommandSettings commands, ExternalCommandDefinition command) : IDestination
{
    [GeneratedRegex(@"https?://[^\s""'<>]+")]
    private static partial Regex UriRegex();

    public ExternalCommandDefinition Command => command;

    public string Designation => "External " + command.Name.Replace(',', '_');

    public string DisplayName => command.Name;

    public int Priority => 10;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(command.Commandline);

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        string path;
        try
        {
            path = await ExportFiles.WriteAsync(settings, capture, ImageFormats.Normalize(command.OutputFormat)).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            return ExportResult.Failed(ex.Message);
        }

        var arguments = BuildArguments(command.Argument, path);
        if (command.RunInBackground)
        {
            try
            {
                var startInfo = new ProcessStartInfo(command.Commandline) { UseShellExecute = false };
                foreach (var argument in arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }

                using var process = Process.Start(startInfo);
                Log.Info($"Started {command.Name}: {command.Commandline} {string.Join(' ', arguments)}");
                return ExportResult.Done(message: command.Name);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                Log.Error($"Starting {command.Name} failed", ex);
                return ExportResult.Failed($"{command.Name}: {ex.Message}");
            }
        }

        try
        {
            var result = await ProcessRunner.RunAsync(command.Commandline, arguments, timeout: TimeSpan.FromMinutes(10), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                Log.Warn($"{command.Name} ended with {result.ExitCode}: {result.Error.Trim()}");
                return ExportResult.Failed(string.IsNullOrWhiteSpace(result.Error) ? $"{command.Name}: exit code {result.ExitCode}" : result.Error.Trim());
            }

            var output = result.OutputText.Trim();
            if (command.OutputToClipboard ?? commands.OutputToClipboard)
            {
                await SystemClipboard.SetTextAsync(output).ConfigureAwait(false);
            }

            var uri = FindUri(output);
            if (uri != null && (command.UriToClipboard ?? commands.UriToClipboard))
            {
                await SystemClipboard.SetTextAsync(uri).ConfigureAwait(false);
            }

            return ExportResult.Done(message: uri ?? command.Name) with { Link = uri };
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or OperationCanceledException)
        {
            Log.Error($"Running {command.Name} failed", ex);
            return ExportResult.Failed($"{command.Name}: {ex.Message}");
        }
    }

    internal static string? FindUri(string output) => UriRegex().Match(output) is { Success: true } match ? match.Value.TrimEnd('.', ',', ')') : null;

    /// <summary>
    /// The arguments as list: the template is split like a shell would ("..." and '...' keep spaces), then {0} becomes the file.
    /// The file name is never split or interpreted, whatever characters it has.
    /// </summary>
    internal static IReadOnlyList<string> BuildArguments(string template, string path)
    {
        var arguments = new List<string>();
        var current = new StringBuilder();
        bool inArgument = false;
        char? quote = null;
        foreach (char c in template)
        {
            if (quote != null)
            {
                if (c == quote)
                {
                    quote = null;
                }
                else
                {
                    current.Append(c);
                }

                continue;
            }

            if (c is '"' or '\'')
            {
                quote = c;
                inArgument = true;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (inArgument)
                {
                    arguments.Add(current.ToString());
                    current.Clear();
                    inArgument = false;
                }
            }
            else
            {
                current.Append(c);
                inArgument = true;
            }
        }

        if (inArgument)
        {
            arguments.Add(current.ToString());
        }

        return arguments.Select(a => a.Replace("{0}", path, StringComparison.Ordinal)).ToList();
    }
}
