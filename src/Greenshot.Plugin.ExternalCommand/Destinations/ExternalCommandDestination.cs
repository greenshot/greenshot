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
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Definitions;
using Greenshot.Base.Expressions;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Greenshot.Plugin.ExternalCommand.Running;

namespace Greenshot.Plugin.ExternalCommand.Destinations;

/// <summary>
/// Icons of the external commands: key "extcmd:commandName", the icon of the program of the definition.
/// </summary>
public sealed class ExternalCommandIconProvider : IIconProvider
{
    private const string Prefix = "extcmd:";

    public static string KeyFor(string command) => Prefix + command;

    public bool CanProvide(string iconKey) => iconKey != null && iconKey.StartsWith(Prefix, StringComparison.Ordinal);

    public async Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
    {
        string name = iconKey.Substring(Prefix.Length);
        var icon = await IconCache.IconForCommandAsync(CommandDefinitions.Find(name)?.Program, name, cancellationToken).ConfigureAwait(false);
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
/// Calls the external command of a definition with the capture. One destination per definition, "External name" like in
/// Greenshot 1.3, so OutputDestinations and recipes keep working.
/// </summary>
public class ExternalCommandDestination : DefinitionItemDestination<CommandDefinition>
{
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExternalCommandDestination));

    private readonly bool _readsSettings;

    /// <param name="definition">the command</param>
    /// <param name="readsSettings">true: the command is read from the settings for each export (a change applies at once, a removed
    /// command fails), false: the definition is used as it is (a recipe step)</param>
    public ExternalCommandDestination(CommandDefinition definition, bool readsSettings = false) : base(CommandKind.Instance, definition)
    {
        _readsSettings = readsSettings;
    }

    public override string Designation => DesignationOf(Definition.Name);

    public override DestinationDescriptor Descriptor => new DestinationDescriptor(Definition.Name, iconKey: ExternalCommandIconProvider.KeyFor(Definition.Name));

    /// <summary>
    /// The designation of the destination of a command: "External name", as in Greenshot 1.3
    /// </summary>
    public static string DesignationOf(string name) => "External " + (name ?? string.Empty).Replace(',', '_');

    /// <summary>
    /// The command gets the file (in a destination LastExport is the file the capture was saved to, when it was), standard input and
    /// its text output goes to the clipboard as the definition says. An image or file as output is only read by the recipe step.
    /// </summary>
    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var definition = _readsSettings ? CommandDefinitions.Find(Definition.Name) : Definition;
        if (definition == null)
        {
            string error = $"Unknown external command '{Definition.Name}'";
            LOG.WarnFormat("Error calling external command: {0} ", error);
            return ExportResult.Failed(error);
        }

        var formatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
        if (definition.Format != null && formatRegistry != null && !formatRegistry.TryGet(definition.Format, out _))
        {
            LOG.WarnFormat("Unknown output file format '{0}' for external command '{1}'; using PNG.", definition.Format, definition.Name);
        }

        var outputSettings = new SurfaceOutputSettings(formatRegistry.ResolveFormatId(definition.Format, WellKnownFileFormats.Png));
        string fullPath = null;
        if (!CommandKind.Is(definition.File, CommandDefinitions.FileNone))
        {
            fullPath = (CommandKind.Is(definition.File, CommandDefinitions.FileLastExport) ? request.Metadata?.Filename : null)
                       ?? await ExportFiles.SaveNamedTmpFileAsync(request.Source, request.Metadata, outputSettings, cancellationToken).ConfigureAwait(false);
        }

        CommandRequest commandRequest;
        try
        {
            commandRequest = CommandDefinitions.CreateRequest(definition, fullPath, VariableScope.ForCapture(request.Metadata));
            if (CommandKind.Is(definition.StandardInput, CommandDefinitions.InputImage))
            {
                var encoded = await request.Source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);
                commandRequest.StandardInput = encoded.ToArray();
            }
        }
        catch (ArgumentException ex)
        {
            LOG.WarnFormat("Error calling external command {0}: {1}", definition.Name, ex.Message);
            return ExportResult.Failed(ex.Message, ex);
        }

        if (commandRequest.Output is CommandOutput.Image or CommandOutput.File)
        {
            LOG.InfoFormat("The output {0} of the external command {1} is only read in a recipe step", commandRequest.Output, definition.Name);
            commandRequest.Output = CommandOutput.None;
        }

        if (definition.RunInBackground)
        {
            // The command is started here, the export doesn't wait for it to finish. What fails before the command runs (a missing
            // program) completes the run right away: that is the result of the export, the notification of the flow shows it.
            // A failure of the running command is reported by a notification of its own (the task is observed).
            var run = RunAsync(definition, commandRequest, fullPath, CancellationToken.None);
            if (run.IsCompleted)
            {
                return await run.ConfigureAwait(false);
            }

            ReportFailureAsync(definition.Name, run, request.Ui).FireAndLog("Running " + definition.Name, LOG);
            return ExportResult.Succeeded(fullPath);
        }

        return await RunAsync(definition, commandRequest, fullPath, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReportFailureAsync(string name, Task<ExportResult> run, IUserInteraction userInteraction)
    {
        var result = await run.ConfigureAwait(false);
        if (result.Status == ExportStatus.Failed)
        {
            await userInteraction.NotifyAsync(new Notification(NotificationKind.Error, $"{name}: {result.Error}")).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Run the command, place the output or the URL in it on the clipboard as the definition says
    /// </summary>
    private static async Task<ExportResult> RunAsync(CommandDefinition definition, CommandRequest request, string fullPath, CancellationToken cancellationToken)
    {
        try
        {
            var result = await CommandRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                LOG.WarnFormat("Error calling external command {0}: {1}", definition.Name, result.FailureMessage);
                return ExportResult.Failed(result.FailureMessage);
            }

            string clipboardText = CommandRunner.ClipboardTextOf(result, CommandKind.Is(definition.CopyToClipboard, CommandDefinitions.ClipboardOutput),
                CommandKind.Is(definition.CopyToClipboard, CommandDefinitions.ClipboardUrl));
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
            LOG.WarnFormat("Error calling external command {0}: {1}", definition.Name, ex.Message);
            return ExportResult.Failed(ex.Message, ex);
        }
    }
}
