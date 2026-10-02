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
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.Export;
using Dapplo.Ini;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Threading;

namespace Greenshot.Plugin.ExternalCommand;

/// <summary>
/// Icons of the external commands: key "extcmd:commandName".
/// </summary>
public sealed class ExternalCommandIconProvider : IIconProvider
{
    private const string Prefix = "extcmd:";

    public static string KeyFor(string command) => Prefix + command;

    public bool CanProvide(string iconKey) => iconKey != null && iconKey.StartsWith(Prefix, StringComparison.Ordinal);

    public async Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
    {
        var icon = await IconCache.IconForCommandAsync(iconKey.Substring(Prefix.Length), cancellationToken).ConfigureAwait(false);
        if (icon == null)
        {
            return null;
        }

        // The icon cache owns its images, the caller gets a copy
        lock (icon)
        {
            return ImageHelper.Clone(icon);
        }
    }
}

/// <summary>
/// Calls an external command with the capture.
/// </summary>
public class ExternalCommandDestination : DestinationBase, IRequiresRecipeAuthorization
{
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExternalCommandDestination));

    private static readonly Regex URI_REGEXP =
        new Regex(
            @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)");

    private static IExternalCommandConfiguration Config
    {
        get
        {
            try
            {
                return IniConfigRegistry.GetSection<IExternalCommandConfiguration>();
            }
            catch
            {
                return null;
            }
        }
    }
    private readonly string _presetCommand;

    public ExternalCommandDestination(string commando)
    {
        _presetCommand = commando;
        Descriptor = new DestinationDescriptor(commando, iconKey: ExternalCommandIconProvider.KeyFor(commando));
    }

    public IEnumerable<RecipeGatedAction> GetGatedActions()
    {
        string cmd = _presetCommand;
        if (Config?.Commandline != null && Config.Commandline.ContainsKey(_presetCommand))
        {
            cmd = Config.Commandline[_presetCommand];
        }
        string target = !string.IsNullOrWhiteSpace(cmd) ? cmd : Designation;
        yield return new RecipeGatedAction(RecipeGateType.ExternalCommand, target, "recipe_gate_external_command");
    }

    public override string Designation => "External " + _presetCommand.Replace(',', '_');

    public override DestinationDescriptor Descriptor { get; }

    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        if (_presetCommand is null)
        {
            LOG.Warn("No external command configured");
            return ExportResult.Failed("No external command configured");
        }

        var config = Config;
        // check if the command is still configured
        if (config == null || !config.Commands.Contains(_presetCommand))
        {
            string error = $"Unknown external command '{_presetCommand}'";
            LOG.WarnFormat("Error calling external command: {0} ", error);
            return ExportResult.Failed(error);
        }

        // fallback to PNG / background if the configuration is incomplete (the plugin repairs it on startup), or the format is unknown
        string configuredFormat = config.OutputFormat.TryGetValue(_presetCommand, out var format) ? format : WellKnownFileFormats.Png;
        var formatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
        if (formatRegistry != null && !formatRegistry.TryGet(configuredFormat, out _))
        {
            LOG.WarnFormat("Unknown output file format '{0}' for external command '{1}'; using PNG.", configuredFormat, _presetCommand);
        }

        var outputSettings = new SurfaceOutputSettings(formatRegistry.ResolveFormatId(configuredFormat, WellKnownFileFormats.Png));
        bool runInBackground = !config.RunInbackground.TryGetValue(_presetCommand, out var background) || background;
        string fullPath = request.Metadata?.Filename
                          ?? await ExportFiles.SaveNamedTmpFileAsync(request.Source, request.Metadata, outputSettings, cancellationToken).ConfigureAwait(false);

        if (runInBackground)
        {
            // The export doesn't wait for the command, its outcome is reported by a notification (the task is observed)
            RunInBackgroundAsync(fullPath, request.Ui).FireAndLog("Running " + _presetCommand, LOG);
            return ExportResult.Succeeded(fullPath);
        }

        return await RunAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunInBackgroundAsync(string fullPath, IUserInteraction userInteraction)
    {
        var result = await RunAsync(fullPath, CancellationToken.None).ConfigureAwait(false);
        if (result.Status == ExportStatus.Failed)
        {
            await userInteraction.NotifyAsync(new Notification(NotificationKind.Error, $"{_presetCommand}: {result.Error}")).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Call the external command, parse the output for an URI and place it on the clipboard
    /// </summary>
    private async Task<ExportResult> RunAsync(string fullPath, CancellationToken cancellationToken)
    {
        try
        {
            var (exitCode, output, error) = await CallExternalCommandAsync(_presetCommand, fullPath, cancellationToken).ConfigureAwait(false);
            if (exitCode != 0)
            {
                LOG.WarnFormat("Error calling external command: {0} ", output);
                return ExportResult.Failed(string.IsNullOrEmpty(error) ? $"Exit code {exitCode}" : error);
            }

            Uri uri = null;
            if (!string.IsNullOrEmpty(output))
            {
                var clipboard = ClipboardService.Current;
                MatchCollection uriMatches = URI_REGEXP.Matches(output);
                // Place output on the clipboard before the URI, so if one is found this overwrites
                bool outputToClipboard = Config?.OutputToClipboardCommand != null && Config.OutputToClipboardCommand.TryGetValue(_presetCommand, out var otc)
                    ? otc
                    : Config?.OutputToClipboard ?? false;
                if (outputToClipboard)
                {
                    await clipboard.SetTextAsync(output, cancellationToken).ConfigureAwait(false);
                }

                if (uriMatches.Count > 0)
                {
                    string uriText = uriMatches[0].Groups[1].Value;
                    LOG.InfoFormat("Got URI : {0} ", uriText);
                    Uri.TryCreate(uriText, UriKind.RelativeOrAbsolute, out uri);
                    bool uriToClipboard = Config?.UriToClipboardCommand != null && Config.UriToClipboardCommand.TryGetValue(_presetCommand, out var utc)
                        ? utc
                        : Config?.UriToClipboard ?? true;
                    if (uriToClipboard)
                    {
                        await clipboard.SetTextAsync(uriText, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return ExportResult.Succeeded(fullPath, uri);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LOG.WarnFormat("Error calling external command: {0} ", ex.Message);
            return ExportResult.Failed(ex.Message, ex);
        }
    }

    /// <summary>
    /// Wrapper to retry with a runas
    /// </summary>
    private async Task<(int ExitCode, string Output, string Error)> CallExternalCommandAsync(string commando, string fullPath, CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                return await CallExternalCommandAsync(commando, fullPath, null, cancellationToken).ConfigureAwait(false);
            }
            catch (Win32Exception)
            {
                return await CallExternalCommandAsync(commando, fullPath, "runas", cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ex.Data["commandline"] = Config.Commandline[_presetCommand];
            ex.Data["arguments"] = Config.Argument[_presetCommand];
            throw;
        }
    }

    /// <summary>
    /// The actual executing code for the external command
    /// </summary>
    private async Task<(int ExitCode, string Output, string Error)> CallExternalCommandAsync(string commando, string fullPath, string verb, CancellationToken cancellationToken)
    {
        string commandline = Config.Commandline[commando];
        string arguments = Config.Argument[commando];
        if (string.IsNullOrEmpty(commandline))
        {
            return (-1, null, null);
        }

        using Process process = new Process();
        // Fix variables
        commandline = FilenameHelper.FillVariables(commandline, true);
        commandline = FilenameHelper.FillCmdVariables(commandline, true);

        arguments = FilenameHelper.FillVariables(arguments, false);
        arguments = FilenameHelper.FillCmdVariables(arguments, false);

        process.StartInfo.FileName = FilenameHelper.FillCmdVariables(commandline, true);
        process.StartInfo.Arguments = FormatArguments(arguments, fullPath);
        bool redirectOutput = Config?.RedirectStandardOutputCommand != null && Config.RedirectStandardOutputCommand.TryGetValue(commando, out var rso)
            ? rso
            : Config?.RedirectStandardOutput ?? true;
        bool redirectError = Config?.RedirectStandardErrorCommand != null && Config.RedirectStandardErrorCommand.TryGetValue(commando, out var rse)
            ? rse
            : Config?.RedirectStandardError ?? true;
        bool showInLog = Config?.ShowStandardOutputInLogCommand != null && Config.ShowStandardOutputInLogCommand.TryGetValue(commando, out var sil)
            ? sil
            : Config?.ShowStandardOutputInLog ?? false;

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = redirectOutput;
        process.StartInfo.RedirectStandardError = redirectError;

        if (verb != null)
        {
            process.StartInfo.Verb = verb;
        }

        LOG.InfoFormat("Starting : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
        var (exitCode, output, error) = await process.RunAsync(cancellationToken).ConfigureAwait(false);
        if (showInLog && output?.Trim().Length > 0)
        {
            LOG.InfoFormat("Output:\n{0}", output);
        }

        if (error?.Trim().Length > 0)
        {
            LOG.WarnFormat("Error:\n{0}", error);
        }

        LOG.InfoFormat("Finished : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
        return (exitCode, output, error);
    }

    public static string FormatArguments(string arguments, string fullpath)
    {
        // Validate filename doesn't contain shell metacharacters
        char[] dangerousChars = { '&', '|', ';', '$', '`', '(', ')', '<', '>', '\n', '\r', '"', '\'' };

        if (fullpath.IndexOfAny(dangerousChars) >= 0)
        {
            throw new ArgumentException(
                "Filename contains potentially dangerous characters. " +
                "For security reasons, filenames with shell metacharacters are not allowed."
            );
        }

        // Validate arguments template doesn't use shell interpreters
        if (arguments.Contains("cmd.exe") || arguments.Contains("powershell"))
        {
            LOG.Warn("ExternalCommand configured with shell interpreter - potential security risk");
        }

        // Additional: Ensure proper quoting
        string safePath = fullpath.Replace("\"", "\\\"");

        return string.Format(arguments, safePath);
    }
}