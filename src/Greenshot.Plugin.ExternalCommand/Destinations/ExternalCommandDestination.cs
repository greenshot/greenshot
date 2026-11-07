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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Dapplo.Ini;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Base.Expressions;
using Greenshot.Plugin.ExternalCommand.Running;

namespace Greenshot.Plugin.ExternalCommand.Destinations;

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

    /// <summary>
    /// What a file name handed to a command may not contain: no shell reads the arguments (the program is started directly), so only
    /// what breaks a quoted argument (a quote, a line break) or is expanded by cmd.exe (%) is refused.
    /// </summary>
    private static readonly char[] UnsafeFileNameChars = { '"', '%', '\n', '\r' };

    private readonly string _presetCommand;

    public ExternalCommandDestination(string commando)
    {
        _presetCommand = commando;
        Descriptor = new DestinationDescriptor(commando, iconKey: ExternalCommandIconProvider.KeyFor(commando));
    }

    public IEnumerable<RecipeGatedAction> GetGatedActions()
    {
        string cmd = ConfiguredCommand.Find(_presetCommand)?.Program;
        string target = !string.IsNullOrWhiteSpace(cmd) ? cmd : Designation;
        yield return new RecipeGatedAction(RecipeGateType.ExternalCommand, target, "Recipe.gate_external_command");
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

        // check if the command is still configured
        var command = ConfiguredCommand.Find(_presetCommand);
        if (command == null)
        {
            string error = $"Unknown external command '{_presetCommand}'";
            LOG.WarnFormat("Error calling external command: {0} ", error);
            return ExportResult.Failed(error);
        }

        // fallback to PNG / background if the configuration is incomplete (the plugin repairs it on startup), or the format is unknown
        string configuredFormat = command.Format ?? WellKnownFileFormats.Png;
        var formatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
        if (formatRegistry != null && !formatRegistry.TryGet(configuredFormat, out _))
        {
            LOG.WarnFormat("Unknown output file format '{0}' for external command '{1}'; using PNG.", configuredFormat, _presetCommand);
        }

        var outputSettings = new SurfaceOutputSettings(formatRegistry.ResolveFormatId(configuredFormat, WellKnownFileFormats.Png));
        bool runInBackground = command.RunInBackground ?? true;
        string fullPath = request.Metadata?.Filename
                          ?? await ExportFiles.SaveNamedTmpFileAsync(request.Source, request.Metadata, outputSettings, cancellationToken).ConfigureAwait(false);

        if (runInBackground)
        {
            // The command is started here, the export doesn't wait for it to finish. What fails before the command runs (a file name
            // it doesn't accept, a missing program) completes the run right away: that is the result of the export, the notification
            // of the flow shows it. A failure of the running command is reported by a notification of its own (the task is observed).
            var run = RunAsync(command, fullPath, CancellationToken.None);
            if (run.IsCompleted)
            {
                return await run.ConfigureAwait(false);
            }
            ReportFailureAsync(run, request.Ui).FireAndLog("Running " + _presetCommand, LOG);
            return ExportResult.Succeeded(fullPath);
        }

        return await RunAsync(command, fullPath, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReportFailureAsync(Task<ExportResult> run, IUserInteraction userInteraction)
    {
        var result = await run.ConfigureAwait(false);
        if (result.Status == ExportStatus.Failed)
        {
            await userInteraction.NotifyAsync(new Notification(NotificationKind.Error, $"{_presetCommand}: {result.Error}")).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Call the external command, parse the output for an URI and place it (or the output) on the clipboard
    /// </summary>
    private static async Task<ExportResult> RunAsync(ConfiguredCommand command, string fullPath, CancellationToken cancellationToken)
    {
        try
        {
            var request = new CommandRequest
            {
                Program = OutputTemplates.RenderCommand(command.Program, VariableScope.Empty, out string problem) ?? throw new ArgumentException(problem),
                Arguments = RenderArguments(command.Arguments, fullPath, VariableScope.Empty),
                Output = command.ReadOutput ? CommandOutput.Text : CommandOutput.None,
                ReadError = command.ReadError,
                FindUrl = command.FindUrl
            };
            var result = await CommandRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);
            if (command.ShowOutputInLog && result.Output?.Trim().Length > 0)
            {
                LOG.InfoFormat("Output:\n{0}", result.Output);
            }

            if (!result.Succeeded)
            {
                LOG.WarnFormat("Error calling external command {0}: {1}", command.Name, result.FailureMessage);
                return ExportResult.Failed(result.FailureMessage);
            }

            string clipboardText = CommandRunner.ClipboardTextOf(result, command.CopyOutput, command.CopyUrl);
            if (clipboardText != null)
            {
                await ClipboardService.Current.SetTextAsync(clipboardText, cancellationToken).ConfigureAwait(false);
            }

            return ExportResult.Succeeded(fullPath, Uri.TryCreate(result.Url, UriKind.Absolute, out var uri) ? uri : null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LOG.WarnFormat("Error calling external command {0}: {1}", command.Name, ex.Message);
            return ExportResult.Failed(ex.Message, ex);
        }
    }

    /// <summary>
    /// The arguments of a command: the template rendered for the file (${export.file}). Nothing of the capture can be part of it, the
    /// file name can't hold characters which break a quoted argument.
    /// </summary>
    /// <exception cref="ArgumentException">The file name has a character which can't be passed safely</exception>
    public static string RenderArguments(string arguments, string fullpath, VariableScope scope)
    {
        if (fullpath != null && fullpath.IndexOfAny(UnsafeFileNameChars) >= 0)
        {
            throw new ArgumentException($"The file name '{fullpath}' contains a character which can't be passed to a command safely (\", % or a line break).");
        }

        arguments ??= string.Empty;
        return VariableResolver.Instance.Render(arguments, TemplatePurpose.Argument, (scope ?? VariableScope.Empty).With(exportFile: fullpath));
    }
}
