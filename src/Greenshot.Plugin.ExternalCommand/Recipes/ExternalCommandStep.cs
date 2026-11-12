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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Expressions;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Plugin.ExternalCommand.Destinations;
using Greenshot.Plugin.ExternalCommand.Running;
using log4net;

namespace Greenshot.Plugin.ExternalCommand.Recipes
{
    /// <summary>
    /// Capture recipe step that runs an external program: what it gets (arguments, a file, standard input) and what it gives back
    /// (exit code, standard output, standard error) each have one parameter, the results land in var.&lt;ResultName&gt;.*
    /// </summary>
    [StepInfo("ExternalCommand", "External Command", "Runs a program with the capture: as a file, on standard input, or not at all; its output, exit code and errors are the results of the step.", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepParameter("Command", ContractDataType.String, Description = "Name of an external command of the settings; its program, arguments and settings are the defaults of the other parameters")]
    [StepParameter("Program", ContractDataType.FilePath, Purpose = TemplatePurpose.Command, RenderedByStep = true, Description = "The program to run, e.g. ${user.localappdata}\\Programs\\tool.exe (only trusted values)")]
    [StepParameter("Arguments", ContractDataType.String, Purpose = TemplatePurpose.Argument, RenderedByStep = true, Description = "Arguments, ${export.file} is the file (quote it yourself: \"${export.file}\"); default \"${export.file}\"")]
    [StepParameter("WorkingDirectory", ContractDataType.DirectoryPath, Purpose = TemplatePurpose.Folder, RenderedByStep = true, Description = "Working directory of the command; default the folder of the file")]
    [StepParameter("File", ContractDataType.Enum, DefaultValue = FileNew, AllowedValues = new[] { FileNew, FileLastExport, FileNone }, SupportsExpressions = false,
        Description = "The file handed to the command (${export.file}): New writes one in Format, LastExport is the file of the export before, None: no file")]
    [StepParameter("Format", ContractDataType.Enum, Description = "Format of the new file and of the image on standard input", AllowedValuesProvider = typeof(SaveableFileFormatIds))]
    [StepParameter("JpegQuality", ContractDataType.Integer, Description = "JPEG quality (1-100) when the format is JPEG")]
    [StepParameter("StandardInput", ContractDataType.Enum, DefaultValue = InputNone, AllowedValues = new[] { InputNone, InputImage, InputText }, SupportsExpressions = false,
        Description = "What the command gets on standard input: None, Image (the image in Format) or Text (StandardInputText)")]
    [StepParameter("StandardInputText", ContractDataType.String, Purpose = TemplatePurpose.Text, RenderedByStep = true,
        Description = "The text on standard input (StandardInput Text); everything may be used here, e.g. ${payload.ocr.text}")]
    [StepParameter("Output", ContractDataType.Enum, DefaultValue = nameof(CommandOutput.Text), AllowedValues = new[] { nameof(CommandOutput.Text), nameof(CommandOutput.Image), nameof(CommandOutput.File), nameof(CommandOutput.None) }, SupportsExpressions = false,
        Description = "What the standard output is: Text (the output, a URL in it is the link), Image (the new image), File (the command changed the file, it is read again) or None")]
    [StepParameter("ResultName", ContractDataType.String, DefaultValue = DefaultResultName, SupportsExpressions = false, Description = "The results are var.<ResultName>.exitCode, .output, .url, .error and .file; default command")]
    [StepParameter("CopyToClipboard", ContractDataType.Enum, DefaultValue = ClipboardNone, AllowedValues = new[] { ClipboardNone, ClipboardOutput, ClipboardUrl }, SupportsExpressions = false,
        Description = "Copy the output, or the URL found in it, to the clipboard (Output Text)")]
    [StepParameter("SuccessExitCodes", ContractDataType.String, DefaultValue = "0", SupportsExpressions = false, Description = "The exit codes which mean success, e.g. 0,98,99; default 0")]
    [StepParameter("Timeout", ContractDataType.Integer, DefaultValue = 0, Description = "Seconds after which the command is stopped; 0: no limit")]
    [StepParameter("Encoding", ContractDataType.String, DefaultValue = "utf-8", SupportsExpressions = false, Description = "Encoding of the text on standard input, output and error: utf-8 (default), oem (the console code page) or another encoding name")]
    [StepParameter("RunInBackground", ContractDataType.Boolean, Description = "Start the command without waiting for it: Output must be None, there are no results")]
    [StepOutputVariable("{Parameter:ResultName|command}.file", ContractDataType.FilePath, "The file handed to the command", Conditional = true)]
    [StepOutputVariable("{Parameter:ResultName|command}.exitCode", ContractDataType.Integer, "Exit code of the command", Conditional = true)]
    [StepOutputVariable("{Parameter:ResultName|command}.output", ContractDataType.String, "Standard output of the command (Output Text)", Conditional = true)]
    [StepOutputVariable("{Parameter:ResultName|command}.url", ContractDataType.String, "First URL in the output", Conditional = true)]
    [StepOutputVariable("{Parameter:ResultName|command}.error", ContractDataType.String, "Standard error of the command", Conditional = true)]
    public class ExternalCommandStep : ICaptureStep, IRequiresRecipeAuthorization, IUsesDestinations
    {
        public const string FileNew = CommandDefinitions.FileNew;
        public const string FileLastExport = CommandDefinitions.FileLastExport;
        public const string FileNone = CommandDefinitions.FileNone;
        public const string InputNone = CommandDefinitions.InputNone;
        public const string InputImage = CommandDefinitions.InputImage;
        public const string InputText = CommandDefinitions.InputText;
        public const string ClipboardNone = CommandDefinitions.ClipboardNone;
        public const string ClipboardOutput = CommandDefinitions.ClipboardOutput;
        public const string ClipboardUrl = CommandDefinitions.ClipboardUrl;
        public const string DefaultResultName = "command";

        private static readonly ILog Log = LogManager.GetLogger(typeof(ExternalCommandStep));

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        /// <summary>
        /// The destination of the command of the settings; a command given in the recipe itself is none
        /// </summary>
        public IEnumerable<string> UsedDestinations
        {
            get
            {
                string command = Parameter("Command");
                return string.IsNullOrWhiteSpace(command) ? Array.Empty<string>() : new[] { ExternalCommandDestination.DesignationOf(command.Trim()) };
            }
        }

        public ExternalCommandStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ExternalCommandStep";
        }

        private string Parameter(string name, string fallback = null)
        {
            string value = NodeConfig.GetParameter<object>(name)?.ToString();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        /// <summary>
        /// The command of the step: the definition named by Command (a copy), each parameter which is set wins. Without Command the
        /// parameters alone, the output is Text by default.
        /// </summary>
        /// <exception cref="ArgumentException">a choice has a value it doesn't know</exception>
        public CommandDefinition ResolveCommand()
        {
            string commandName = Parameter("Command");
            if (CommandDefinitions.IsTurnedOff(commandName))
            {
                throw new ArgumentException(string.Format(Texts.Get<IExternalCommandLanguage>().ErrorTurnedOff, commandName));
            }

            var named = CommandDefinitions.Find(commandName);
            if (commandName != null && named == null)
            {
                Log.WarnFormat("ExternalCommandStep: there is no external command '{0}' in the settings.", commandName);
            }

            var command = named?.Clone() ?? new CommandDefinition { Name = commandName, Output = nameof(CommandOutput.Text) };
            command.Program = Parameter("Program") ?? command.Program;
            command.Arguments = Parameter("Arguments") ?? command.Arguments ?? CommandDefinition.DefaultArguments;
            command.WorkingDirectory = Parameter("WorkingDirectory") ?? command.WorkingDirectory;
            command.File = Choice("File", command.File ?? FileNew, CommandKind.FileValues);
            command.Format = Parameter("Format") ?? command.Format;
            command.StandardInput = Choice("StandardInput", command.StandardInput ?? InputNone, CommandKind.StandardInputValues);
            command.StandardInputText = Parameter("StandardInputText") ?? command.StandardInputText;
            command.Output = Choice("Output", command.Output ?? nameof(CommandOutput.Text), CommandKind.OutputValues);
            command.CopyToClipboard = Choice("CopyToClipboard", command.CopyToClipboard ?? ClipboardNone, CommandKind.ClipboardValues);
            command.SuccessExitCodes = Parameter("SuccessExitCodes") ?? command.SuccessExitCodes;
            command.Timeout = NodeConfig.GetParameter<int?>("Timeout") ?? command.Timeout;
            command.Encoding = Parameter("Encoding") ?? command.Encoding;
            command.RunInBackground = NodeConfig.GetParameter<bool?>("RunInBackground") ?? command.RunInBackground;
            return command;
        }

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            string target;
            try
            {
                // The approval shows the program with its arguments, what else the command gets and whether it replaces the image
                target = CommandKind.GateTarget(ResolveCommand());
            }
            catch (ArgumentException)
            {
                // A value the validator reports
                target = Parameter("Program") ?? Parameter("Command") ?? string.Empty;
            }

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

            // 1. The parameters, a command of the settings gives the defaults
            var command = ResolveCommand();
            if (string.IsNullOrWhiteSpace(command.Program))
            {
                string msg = $"ExternalCommandStep: No program for command '{command.Name ?? NodeConfig.Id}'.";
                context.LogStep(msg);
                Log.Warn(msg);
                return;
            }

            string file = command.File;
            string input = command.StandardInput;
            string resultName = Parameter("ResultName", DefaultResultName);
            bool runInBackground = command.RunInBackground;

            var formatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            string formatStr = command.Format;
            if (!string.IsNullOrWhiteSpace(formatStr) && formatRegistry != null && !formatRegistry.TryGet(formatStr, out _))
            {
                Log.WarnFormat("Unknown output file format '{0}' for external command; using PNG.", formatStr);
            }
            string outputFormat = formatRegistry.ResolveFormatId(formatStr, WellKnownFileFormats.Png);
            int jpegQuality = NodeConfig.GetParameter<int?>("JpegQuality") ?? 90;
            var outputSettings = new SurfaceOutputSettings(outputFormat, jpegQuality, false);

            // 2. The file handed to the command
            string fullPath = null;
            if (file == FileLastExport)
            {
                fullPath = LastExportedFile(context)
                    ?? throw new InvalidOperationException("ExternalCommand: File is LastExport, but no export before this step wrote a file.");
            }
            else if (file == FileNew)
            {
                var source = await context.GetExportSourceAsync(cancellationToken).ConfigureAwait(false);
                fullPath = await ExportFiles.SaveNamedTmpFileAsync(source, captureDetails, outputSettings, cancellationToken).ConfigureAwait(false);
            }
            if (fullPath != null)
            {
                context.Properties[resultName + ".file"] = fullPath;
            }

            // 3. Program, arguments, working folder and standard input
            var request = CommandDefinitions.CreateRequest(command, fullPath, VariableScope.ForFlow(context));
            var output = request.Output;
            if (input == InputImage)
            {
                var source = await context.GetExportSourceAsync(cancellationToken).ConfigureAwait(false);
                var encoded = await source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);
                request.StandardInput = encoded.ToArray();
            }

            // The standard input isn't logged, it can be the text of the capture
            context.LogStep($"Executing external command: {request.Program} {request.Arguments}");
            Log.InfoFormat("ExternalCommandStep: Executing '{0}' with args '{1}'", request.Program, request.Arguments);

            // 4. Execution
            if (runInBackground)
            {
                // The flow doesn't wait for the command, the task is observed (logged) and outlives the flow
                CommandRunner.RunAsync(request, CancellationToken.None)
                    .FireAndLog($"External command background execution: {request.Program}", Log);
                context.LogStep($"External command '{command.Name ?? request.Program}' launched in background.");
                return;
            }

            var result = await CommandRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);

            context.Properties[resultName + ".exitCode"] = result.ExitCode;
            context.Properties[resultName + ".error"] = result.Error ?? string.Empty;
            if (output == CommandOutput.Text)
            {
                context.Properties[resultName + ".output"] = result.Output ?? string.Empty;
                if (!string.IsNullOrEmpty(result.Url))
                {
                    context.Properties[resultName + ".url"] = result.Url;
                    surface.UploadUrl = result.Url;
                }
            }
            if (result.Succeeded)
            {
                string clipboardText = CommandRunner.ClipboardTextOf(result, command.CopyToClipboard == ClipboardOutput, command.CopyToClipboard == ClipboardUrl);
                if (clipboardText != null)
                {
                    await ClipboardService.For(context.Ui).SetTextAsync(clipboardText, cancellationToken).ConfigureAwait(false);
                    context.LogStep("Copied the output of the external command to the clipboard.");
                }
            }

            // The same result as the external command destination, before a new image replaces the capture: the Notification step shows it
            var exportSource = await context.GetExportSourceAsync(cancellationToken).ConfigureAwait(false);
            var exportResult = result.Succeeded
                ? ExportResult.Succeeded(fullPath, Uri.TryCreate(result.Url, UriKind.Absolute, out var uri) ? uri : null, clearsModified: false)
                : ExportResult.Failed(result.FailureMessage);
            var named = command.Clone();
            named.Name ??= Path.GetFileNameWithoutExtension(request.Program);
            var destination = new ExternalCommandDestination(named);
            await ExportResultHandler.ApplyAsync(destination, exportResult, exportSource, cancellationToken).ConfigureAwait(false);
            context.RecordExport(destination, exportResult, captureDetails, exportSource);

            // 5. The new image: from the standard output, or the file the command changed (e.g. an optimizer)
            if (result.Succeeded && output == CommandOutput.Image)
            {
                var image = result.OutputBytes?.Length > 0
                    ? ImageIO.FromStream(new MemoryStream(result.OutputBytes), FileFormatRegistry.GetPreferredExtensionWithDot(outputFormat))
                    : null;
                ReplaceImage(context, image, captureDetails, "the output of the command");
            }
            else if (result.Succeeded && output == CommandOutput.File && fullPath != null)
            {
                ReplaceImage(context, ImageIO.LoadImage(fullPath), captureDetails, fullPath);
            }

            context.LogStep($"External command finished with exit code {result.ExitCode}.");
        }

        /// <summary>
        /// One of the allowed values of a parameter (case is ignored), the default when it isn't set
        /// </summary>
        private string Choice(string name, string defaultValue, IReadOnlyList<string> allowed)
        {
            string value = Parameter(name) ?? defaultValue;
            return allowed.FirstOrDefault(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"ExternalCommand: '{value}' is not a value of {name}, use one of {string.Join(", ", allowed)}.");
        }

        /// <summary>
        /// The encoding of the text: utf-8 (no byte order mark), oem (the console code page of the system) or an encoding name
        /// </summary>
        public static Encoding EncodingOf(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || string.Equals(name, "utf-8", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "utf8", StringComparison.OrdinalIgnoreCase))
            {
                return CommandRunner.Utf8;
            }
            if (string.Equals(name, "oem", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            }
            return Encoding.GetEncoding(name);
        }

        /// <summary>
        /// The file of the last export of the flow which wrote one, null when there is none
        /// </summary>
        private static string LastExportedFile(CaptureFlowContext context) =>
            context.GetExportLog().ToList()
                .Where(o => o.Result?.Status == ExportStatus.Succeeded && !string.IsNullOrEmpty(o.Result.FilePath) && File.Exists(o.Result.FilePath))
                .Select(o => o.Result.FilePath)
                .LastOrDefault();

        private static void ReplaceImage(CaptureFlowContext context, Image image, ICaptureDetails captureDetails, string from)
        {
            if (image == null)
            {
                context.LogStep($"ExternalCommand: no image could be read from {from}, the image stays as it was.");
                Log.WarnFormat("ExternalCommandStep: no image could be read from {0}", from);
                return;
            }

            var bitmap = image as Bitmap ?? new Bitmap(image);
            context.Payload.RawCapture = new Greenshot.Base.Core.Capture(bitmap)
            {
                CaptureDetails = captureDetails
            };
            context.Payload.Surface = null;
            context.Payload.InvalidateExportSource();
            context.LogStep($"The image is now the one from {from} ({bitmap.Width}x{bitmap.Height}).");
            Log.InfoFormat("ExternalCommandStep: the image was read from {0}", from);
        }
    }
}
