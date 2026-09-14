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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Plugin.ExternalCommand
{
    /// <summary>
    /// Capture recipe step that executes an external tool or command line utility
    /// against the current screenshot surface/file.
    /// </summary>
    public class ExternalCommandStep : ICaptureStep, IRequiresExternalCommandAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExternalCommandStep));
        private static readonly Regex UriRegex = new Regex(
            @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)",
            RegexOptions.Compiled);

        private static IExternalCommandConfiguration Config => IniConfigRegistry.GetSection<IExternalCommandConfiguration>();

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public ExternalCommandStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ExternalCommandStep";
        }

        public IEnumerable<string> GetExternalCommands()
        {
            string commandName = NodeConfig.GetFirstParameter<string>("Command", "CommandName");
            if (string.IsNullOrEmpty(commandName) && NodeConfig.StepType.StartsWith("ExternalCommand.", StringComparison.OrdinalIgnoreCase))
            {
                commandName = NodeConfig.StepType.Substring("ExternalCommand.".Length);
            }

            string commandLine = NodeConfig.GetFirstParameter<string>("CommandLine", "Executable", "Path");

            var extConfig = Config;
            if (string.IsNullOrEmpty(commandLine) && !string.IsNullOrEmpty(commandName) && extConfig?.Commandline != null && extConfig.Commandline.ContainsKey(commandName))
            {
                commandLine = extConfig.Commandline[commandName];
            }

            string target = !string.IsNullOrEmpty(commandLine)
                ? (!string.IsNullOrEmpty(commandName) ? $"{commandName} ({commandLine})" : commandLine)
                : (commandName ?? NodeConfig.StepType);

            yield return target;
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
            string commandName = NodeConfig.GetFirstParameter<string>("Command", "CommandName");
            if (string.IsNullOrEmpty(commandName) && NodeConfig.StepType.StartsWith("ExternalCommand.", StringComparison.OrdinalIgnoreCase))
            {
                commandName = NodeConfig.StepType.Substring("ExternalCommand.".Length);
            }

            string commandLine = NodeConfig.GetFirstParameter<string>("CommandLine", "Executable", "Path");
            string arguments = NodeConfig.GetFirstParameter<string>("Arguments", "Argument", "Args");
            bool? runInBackgroundParam = NodeConfig.GetFirstParameter<bool?>("RunInBackground", "Async");
            string formatStr = NodeConfig.GetFirstParameter<string>("OutputFormat", "Format");
            bool? outputToClipboardParam = NodeConfig.GetFirstParameter<bool?>("OutputToClipboard");
            bool? uriToClipboardParam = NodeConfig.GetFirstParameter<bool?>("UriToClipboard");
            bool reloadAfterExecution = NodeConfig.GetFirstParameter<bool?>("ReloadAfterExecution", "UpdatePayload") ?? false;
            string workingDirectory = NodeConfig.GetFirstParameter<string>("WorkingDirectory", "WorkingDir");
            string verb = NodeConfig.GetFirstParameter<string>("Verb");
            string setOutputVariable = NodeConfig.GetFirstParameter<string>("SetOutputVariable");
            string setExitCodeVariable = NodeConfig.GetFirstParameter<string>("SetExitCodeVariable");

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
                    formatStr = extConfig.OutputFormat[commandName].ToString();
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

            OutputFormat outputFormat = OutputFormat.png;
            if (!string.IsNullOrWhiteSpace(formatStr) && Enum.TryParse<OutputFormat>(formatStr, true, out var parsedFormat))
            {
                outputFormat = parsedFormat;
            }

            int jpegQuality = NodeConfig.GetParameter<int?>("JpegQuality") ?? 90;
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(outputFormat, jpegQuality, false);

            // 2. Prepare file on disk for the external command
            string fullPath = captureDetails.Filename
                ?? (context.Properties.TryGetValue("Destination.Filename", out var df) && df is string dfs ? dfs : null);

            bool isTempFile = false;
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                fullPath = ImageIO.SaveNamedTmpFile(surface, captureDetails, outputSettings);
                isTempFile = true;
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
                _ = Task.Run(() =>
                {
                    try
                    {
                        ExecuteProcess(resolvedCommandLine, resolvedArguments, resolvedWorkingDir, verb, out _, out _);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"External command background execution failed: {resolvedCommandLine}", ex);
                    }
                }, cancellationToken);

                context.LogStep($"External command '{commandName ?? resolvedCommandLine}' launched in background.");
                return;
            }

            string output = null;
            string error = null;
            int exitCode = -1;

            await Task.Run(() =>
            {
                exitCode = ExecuteProcess(resolvedCommandLine, resolvedArguments, resolvedWorkingDir, verb, out output, out error);
            }, cancellationToken).ConfigureAwait(false);

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

                if (outputToClipboard)
                {
                    ClipboardHelper.SetClipboardData(output);
                    context.LogStep("Copied external command output to clipboard.");
                }

                if (uriToClipboard && !string.IsNullOrEmpty(matchedUri))
                {
                    ClipboardHelper.SetClipboardData(matchedUri);
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
                        context.Payload.SharedRenderedBitmap?.Dispose();
                        context.Payload.SharedRenderedBitmap = null;
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

        private static int ExecuteProcess(string commandLine, string arguments, string workingDirectory, string verb, out string output, out string error)
        {
            output = null;
            error = null;

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
                    process.Start();
                }
                catch (Win32Exception)
                {
                    // Fall back to runas if access denied
                    process.StartInfo.Verb = "runas";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                    process.Start();
                    process.WaitForExit();
                    return process.ExitCode;
                }

                if (process.StartInfo.RedirectStandardOutput)
                {
                    output = process.StandardOutput.ReadToEnd();
                }

                if (process.StartInfo.RedirectStandardError)
                {
                    error = process.StandardError.ReadToEnd();
                }

                process.WaitForExit();
                return process.ExitCode;
            }
        }
    }
}
