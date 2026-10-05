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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.ExternalCommand.Destinations;
using log4net;
using Greenshot.Base.Threading;
using Greenshot.Base.Core.Export;

namespace Greenshot.Plugin.ExternalCommand.Recipes
{
    /// <summary>
    /// Capture recipe step that executes an external tool or command line utility
    /// against the current screenshot surface/file.
    /// </summary>
    [StepInfo("ExternalCommand", "External Command", "Saves the capture to a file and runs an external command with it: a command configured in the settings (Command) or an executable (CommandLine).", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepParameter("Command", ContractDataType.String, Description = "Name of an external command configured in the settings")]
    [StepParameter("CommandLine", ContractDataType.FilePath, Description = "Executable to run, instead of a configured command")]
    [StepParameter("Arguments", ContractDataType.String, Description = "Arguments, {0} is replaced by the file")]
    [StepParameter("WorkingDirectory", ContractDataType.DirectoryPath, Description = "Working directory of the command")]
    [StepParameter("Verb", ContractDataType.String, Description = "Shell verb to use instead of running the executable (e.g. open, print)")]
    [StepParameter("Format", ContractDataType.Enum, Description = "Format of the file handed to the command", AllowedValuesProvider = typeof(SaveableFileFormatIds))]
    [StepParameter("JpegQuality", ContractDataType.Integer, Description = "JPEG quality (1-100) when saving as JPEG")]
    [StepParameter("RunInBackground", ContractDataType.Boolean, Description = "Start the command without waiting for it (no output variables then)")]
    [StepParameter("OutputToClipboard", ContractDataType.Boolean, Description = "Copy the command's output to the clipboard")]
    [StepParameter("UriToClipboard", ContractDataType.Boolean, Description = "Copy a URI found in the command's output to the clipboard")]
    [StepParameter("ReloadAfterExecution", ContractDataType.Boolean, DefaultValue = false, Description = "Reload the image from the file after the command changed it")]
    [StepParameter("SetOutputVariable", ContractDataType.String, Description = "Also store the command's output in this variable", SupportsExpressions = false)]
    [StepParameter("SetExitCodeVariable", ContractDataType.String, Description = "Also store the command's exit code in this variable", SupportsExpressions = false)]
    [StepInputVariable("Destination.Filename", ContractDataType.FilePath, Description = "File saved by an earlier destination step, handed to the command instead of a new file")]
    [StepOutputVariable("ExternalCommand.TargetFile", ContractDataType.FilePath, "The file handed to the command")]
    [StepOutputVariable("ExternalCommand.ExitCode", ContractDataType.Integer, "Exit code of the command", Conditional = true)]
    [StepOutputVariable("ExternalCommand.Output", ContractDataType.String, "Standard output of the command", Conditional = true)]
    [StepOutputVariable("ExternalCommand.Error", ContractDataType.String, "Standard error of the command", Conditional = true)]
    [StepOutputVariable("ExternalCommand.Uri", ContractDataType.String, "First URI found in the output", Conditional = true)]
    [StepOutputVariable("{Parameter:SetOutputVariable}", ContractDataType.String, "The command's output", Conditional = true)]
    [StepOutputVariable("{Parameter:SetExitCodeVariable}", ContractDataType.Integer, "The command's exit code", Conditional = true)]
    public class ExternalCommandStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExternalCommandStep));
        private static readonly Regex UriRegex = new Regex(
            @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)",
            RegexOptions.Compiled);

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

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public ExternalCommandStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ExternalCommandStep";
        }

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            string commandName = NodeConfig.GetParameter<string>("Command");
            string commandLine = NodeConfig.GetParameter<string>("CommandLine");

            var extConfig = Config;
            if (string.IsNullOrEmpty(commandLine) && !string.IsNullOrEmpty(commandName) && extConfig?.Commandline != null && extConfig.Commandline.ContainsKey(commandName))
            {
                commandLine = extConfig.Commandline[commandName];
            }

            string target = !string.IsNullOrEmpty(commandLine)
                ? (!string.IsNullOrEmpty(commandName) ? $"{commandName} ({commandLine})" : commandLine)
                : (commandName ?? string.Empty);

            yield return new RecipeGatedAction(RecipeGateType.ExternalCommand, target, "Recipe.gate_external_command");
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var surface = context.Payload?.EnsureSurface();
            if (surface == null)
            {
                context.LogStep("ExternalCommand: No surface available to pass to external command.");
                Log.Warn("ExternalCommandStep: Surface is null in context payload.");
                return;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            // 1. Resolve Command Name & Settings
            string commandName = NodeConfig.GetParameter<string>("Command");

            string commandLine = NodeConfig.GetParameter<string>("CommandLine");
            string arguments = NodeConfig.GetParameter<string>("Arguments");
            bool? runInBackgroundParam = NodeConfig.GetParameter<bool?>("RunInBackground");
            string formatStr = NodeConfig.GetParameter<string>("Format");
            bool? outputToClipboardParam = NodeConfig.GetParameter<bool?>("OutputToClipboard");
            bool? uriToClipboardParam = NodeConfig.GetParameter<bool?>("UriToClipboard");
            bool reloadAfterExecution = NodeConfig.GetParameter<bool?>("ReloadAfterExecution") ?? false;
            string workingDirectory = NodeConfig.GetParameter<string>("WorkingDirectory");
            string verb = NodeConfig.GetParameter<string>("Verb");
            string setOutputVariable = NodeConfig.GetParameter<string>("SetOutputVariable");
            string setExitCodeVariable = NodeConfig.GetParameter<string>("SetExitCodeVariable");

            // Look up configured command if commandName is given or commandLine is not explicitly set
            var extConfig = Config;
            if (!string.IsNullOrEmpty(commandName) && extConfig != null)
            {
                if (string.IsNullOrEmpty(commandLine) && extConfig.Commandline != null && extConfig.Commandline.ContainsKey(commandName))
                {
                    commandLine = extConfig.Commandline[commandName];
                }

                if (string.IsNullOrEmpty(arguments) && extConfig.Argument != null && extConfig.Argument.ContainsKey(commandName))
                {
                    arguments = extConfig.Argument[commandName];
                }

                if (!runInBackgroundParam.HasValue && extConfig.RunInbackground != null && extConfig.RunInbackground.ContainsKey(commandName))
                {
                    runInBackgroundParam = extConfig.RunInbackground[commandName];
                }

                if (string.IsNullOrEmpty(formatStr) && extConfig.OutputFormat != null && extConfig.OutputFormat.ContainsKey(commandName))
                {
                    formatStr = extConfig.OutputFormat[commandName];
                }
            }

            if (string.IsNullOrWhiteSpace(commandLine))
            {
                string msg = $"ExternalCommandStep: No command line or executable configured for command '{commandName ?? NodeConfig.Id}'.";
                context.LogStep(msg);
                Log.Warn(msg);
                return;
            }

            // Defaults
            bool runInBackground = runInBackgroundParam ?? false;
            arguments = arguments ?? "{0}";
            bool outputToClipboard = outputToClipboardParam ?? (extConfig?.OutputToClipboard ?? false);
            bool uriToClipboard = uriToClipboardParam ?? (extConfig?.UriToClipboard ?? false);

            var formatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            if (!string.IsNullOrWhiteSpace(formatStr) && formatRegistry != null && !formatRegistry.TryGet(formatStr, out _))
            {
                Log.WarnFormat("Unknown output file format '{0}' for external command; using PNG.", formatStr);
            }

            string outputFormat = formatRegistry.ResolveFormatId(formatStr, WellKnownFileFormats.Png);

            int jpegQuality = NodeConfig.GetParameter<int?>("JpegQuality") ?? 90;
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(outputFormat, jpegQuality, false);

            // 2. Prepare file on disk for the external command
            string fullPath = captureDetails.Filename
                ?? (context.Properties.TryGetValue("Destination.Filename", out var df) && df is string dfs ? dfs : null);

            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                var source = await context.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
                fullPath = await ExportFiles.SaveNamedTmpFileAsync(source, captureDetails, outputSettings, cancellationToken).ConfigureAwait(false);
            }

            context.Properties["ExternalCommand.TargetFile"] = fullPath;

            // 3. Resolve command line and argument variables
            string resolvedCommandLine = FilenameHelper.FillVariables(commandLine, true);
            resolvedCommandLine = FilenameHelper.FillCmdVariables(resolvedCommandLine, true);

            string resolvedArguments = FilenameHelper.FillVariables(arguments, false);
            resolvedArguments = FilenameHelper.FillCmdVariables(resolvedArguments, false);
            resolvedArguments = ExternalCommandDestination.FormatArguments(resolvedArguments, fullPath);

            string resolvedWorkingDir = null;
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                resolvedWorkingDir = FilenameHelper.FillVariables(workingDirectory, true);
                resolvedWorkingDir = FilenameHelper.FillCmdVariables(resolvedWorkingDir, true);
            }

            context.LogStep($"Executing external command: {resolvedCommandLine} {resolvedArguments}");
            Log.InfoFormat("ExternalCommandStep: Executing '{0}' with args '{1}'", resolvedCommandLine, resolvedArguments);

            // 4. Execution
            if (runInBackground)
            {
                // The flow doesn't wait for the command, the task is observed (logged) and outlives the flow
                ExecuteProcessAsync(resolvedCommandLine, resolvedArguments, resolvedWorkingDir, verb, CancellationToken.None)
                    .FireAndLog($"External command background execution: {resolvedCommandLine}", Log);

                context.LogStep($"External command '{commandName ?? resolvedCommandLine}' launched in background.");
                return;
            }

            var (exitCode, output, error) = await ExecuteProcessAsync(resolvedCommandLine, resolvedArguments, resolvedWorkingDir, verb, cancellationToken).ConfigureAwait(false);

            context.Properties["ExternalCommand.ExitCode"] = exitCode;
            context.Properties["ExternalCommand.Output"] = output ?? "";
            context.Properties["ExternalCommand.Error"] = error ?? "";

            if (!string.IsNullOrWhiteSpace(setOutputVariable))
            {
                context.Properties[setOutputVariable] = output ?? "";
            }

            if (!string.IsNullOrWhiteSpace(setExitCodeVariable))
            {
                context.Properties[setExitCodeVariable] = exitCode;
            }

            // Extract URI if available
            string matchedUri = null;
            if (!string.IsNullOrEmpty(output))
            {
                var matches = UriRegex.Matches(output);
                if (matches.Count > 0)
                {
                    matchedUri = matches[0].Groups[1].Value;
                    context.Properties["ExternalCommand.Uri"] = matchedUri;
                    if (surface != null)
                    {
                        surface.UploadUrl = matchedUri;
                    }
                    Log.InfoFormat("ExternalCommandStep: Extracted URI '{0}' from output.", matchedUri);
                }

                var clipboard = ClipboardService.For(context.Ui);
                if (outputToClipboard)
                {
                    await clipboard.SetTextAsync(output, cancellationToken).ConfigureAwait(false);
                    context.LogStep("Copied external command output to clipboard.");
                }

                if (uriToClipboard && !string.IsNullOrEmpty(matchedUri))
                {
                    await clipboard.SetTextAsync(matchedUri, cancellationToken).ConfigureAwait(false);
                    context.LogStep($"Copied extracted URL '{matchedUri}' to clipboard.");
                }
            }

            // 5. Reload modified image if requested (e.g. for in-place optimizers like OptiPNG)
            if (reloadAfterExecution && File.Exists(fullPath))
            {
                try
                {
                    using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var img = Image.FromStream(fs))
                    {
                        var reloadedBitmap = (Bitmap)img.Clone();
                        context.Payload.RawCapture = new Greenshot.Base.Core.Capture(reloadedBitmap)
                        {
                            CaptureDetails = captureDetails
                        };
                        context.Payload.Surface = null;
                        context.Payload.InvalidateExportSource();
                        context.LogStep($"Reloaded transformed image from '{fullPath}' ({reloadedBitmap.Width}x{reloadedBitmap.Height}).");
                        Log.InfoFormat("ExternalCommandStep: Reloaded image payload from {0}", fullPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"ExternalCommandStep: Failed to reload image from '{fullPath}'", ex);
                }
            }

            context.LogStep($"External command finished with exit code {exitCode}.");
        }

        private static async Task<(int ExitCode, string Output, string Error)> ExecuteProcessAsync(string commandLine, string arguments, string workingDirectory, string verb,
            CancellationToken cancellationToken)
        {
            var extConfig = Config;
            using (var process = new Process())
            {
                process.StartInfo.FileName = commandLine;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                if (!string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory))
                {
                    process.StartInfo.WorkingDirectory = workingDirectory;
                }

                if (verb != null)
                {
                    process.StartInfo.Verb = verb;
                }

                process.StartInfo.RedirectStandardOutput = extConfig?.RedirectStandardOutput ?? true;
                process.StartInfo.RedirectStandardError = extConfig?.RedirectStandardError ?? true;

                try
                {
                    return await process.RunAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Win32Exception)
                {
                    // Fall back to runas if access denied
                    process.StartInfo.Verb = "runas";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                    return await process.RunAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
