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

#nullable disable

using System.Diagnostics;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Barcodes;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Http;
using Greenshot.Linux.Core.Naming;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Plugins.Atlassian;
using Greenshot.Linux.Plugins.Cloud;
using Greenshot.Linux.Plugins.CustomUploader;
using Greenshot.Linux.Plugins.ExternalCommand;
using Greenshot.Linux.Plugins.Pdf;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;
using Capture = Greenshot.Linux.Core.Capturing.Capture;

namespace Greenshot.Linux.Plugins.Recipes
{
    /// <summary>
    /// What the steps of the plugins share: the image for an upload, the record of the export for the notification
    /// </summary>
    internal static class PluginStepHelper
    {
        public static async Task<Capture> SourceAsync(CaptureFlowContext context, CancellationToken cancellationToken) =>
            context.Payload?.RawCapture == null ? null : await context.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// The image in the format and quality of the step, or of the plugin's settings
        /// </summary>
        public static EncodedImage Encode(Capture capture, RecipeNodeConfig config, string defaultFormat, int defaultQuality)
        {
            string format = ImageFormats.Normalize(config.GetParameter<string>("Format") ?? defaultFormat);
            if (!ImageFormats.Supported.Contains(format))
            {
                format = "png";
            }

            return new EncodedImage(ImageFormats.Encode(capture.Image, format, config.GetParameter<int?>("JpegQuality") ?? defaultQuality), format);
        }

        /// <summary>
        /// ${...} of the filename pattern (date, title, environment) in a parameter
        /// </summary>
        public static string Fill(string text, CaptureDetails details) =>
            string.IsNullOrEmpty(text) || RecipeServices.Settings == null ? text : FilenamePattern.ForSettings(RecipeServices.Settings).Fill(text, details ?? new CaptureDetails(), filenameSafe: false);

        public static void Record(CaptureFlowContext context, IDestination destination, ExportResult result, Capture source) =>
            context.RecordExport(destination, result, source?.Details, source);
    }

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
    [StepParameter("OutputVariable", ContractDataType.String, Description = "Also store the command's output in this variable", SupportsExpressions = false)]
    [StepParameter("ExitCodeVariable", ContractDataType.String, Description = "Also store the command's exit code in this variable", SupportsExpressions = false)]
    [StepInputVariable("Destination.Filename", ContractDataType.FilePath, Description = "File saved by an earlier destination step, handed to the command instead of a new file")]
    [StepOutputVariable("ExternalCommand.TargetFile", ContractDataType.FilePath, "The file handed to the command")]
    [StepOutputVariable("ExternalCommand.ExitCode", ContractDataType.Integer, "Exit code of the command", Conditional = true)]
    [StepOutputVariable("ExternalCommand.Output", ContractDataType.String, "Standard output of the command", Conditional = true)]
    [StepOutputVariable("ExternalCommand.Error", ContractDataType.String, "Standard error of the command", Conditional = true)]
    [StepOutputVariable("ExternalCommand.Uri", ContractDataType.String, "First URI found in the output", Conditional = true)]
    [StepOutputVariable("{Parameter:OutputVariable}", ContractDataType.String, "The command's output", Conditional = true)]
    [StepOutputVariable("{Parameter:ExitCodeVariable}", ContractDataType.Integer, "The command's exit code", Conditional = true)]
    public partial class ExternalCommandStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private readonly ExternalCommandSettings _commands;

        public string Name { get; }

        public RecipeNodeConfig NodeConfig { get; }

        public ExternalCommandStep(RecipeNodeConfig config, ExternalCommandSettings commands = null)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            _commands = commands ?? (RecipeServices.Settings == null ? null : new ExternalCommandSettings(RecipeServices.Settings));
            Name = config.Name ?? "ExternalCommandStep";
        }

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            string commandName = NodeConfig.GetParameter<string>("Command");
            string commandLine = NodeConfig.GetParameter<string>("CommandLine");
            if (string.IsNullOrEmpty(commandLine) && !string.IsNullOrEmpty(commandName))
            {
                commandLine = _commands?.Get(commandName)?.Commandline;
            }

            string target = !string.IsNullOrEmpty(commandLine)
                ? (!string.IsNullOrEmpty(commandName) ? $"{commandName} ({commandLine})" : commandLine)
                : (commandName ?? string.Empty);
            yield return new RecipeGatedAction(RecipeGateType.ExternalCommand, target, "Recipe.gate_external_command");
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var source = await PluginStepHelper.SourceAsync(context, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                context.LogStep("ExternalCommand: No capture to pass to external command.");
                return;
            }

            string commandName = NodeConfig.GetParameter<string>("Command");
            var configured = string.IsNullOrEmpty(commandName) ? null : _commands?.Get(commandName);
            string commandLine = NodeConfig.GetParameter<string>("CommandLine") ?? configured?.Commandline;
            string arguments = NodeConfig.GetParameter<string>("Arguments") ?? configured?.Argument ?? "{0}";
            bool runInBackground = NodeConfig.GetParameter<bool?>("RunInBackground") ?? configured?.RunInBackground ?? false;
            string format = NodeConfig.GetParameter<string>("Format") ?? configured?.OutputFormat ?? "png";
            bool outputToClipboard = NodeConfig.GetParameter<bool?>("OutputToClipboard") ?? configured?.OutputToClipboard ?? _commands?.OutputToClipboard ?? false;
            bool uriToClipboard = NodeConfig.GetParameter<bool?>("UriToClipboard") ?? configured?.UriToClipboard ?? _commands?.UriToClipboard ?? false;
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                context.LogStep($"ExternalCommandStep: No command line or executable configured for command '{commandName ?? NodeConfig.Id}'.");
                return;
            }

            // The file of an earlier destination step, else a new one
            var details = source.Details;
            string path = context.Properties.TryGetValue("Destination.Filename", out var saved) && saved is string savedPath && File.Exists(savedPath) ? savedPath : null;
            if (path == null)
            {
                format = ImageFormats.Supported.Contains(ImageFormats.Normalize(format)) ? ImageFormats.Normalize(format) : "png";
                var capture = new Capture(source.Image, details);
                path = await ExportFiles.WriteAsync(RecipeServices.Settings, capture, format,
                    ImageFormats.Encode(capture.Image, format, NodeConfig.GetParameter<int?>("JpegQuality") ?? 90)).ConfigureAwait(false);
            }

            context.Properties["ExternalCommand.TargetFile"] = path;
            string program = PluginStepHelper.Fill(commandLine, details);
            var argumentList = ExternalCommandDestination.BuildArguments(PluginStepHelper.Fill(arguments, details), path);
            string workingDirectory = PluginStepHelper.Fill(NodeConfig.GetParameter<string>("WorkingDirectory"), details);
            if (string.Equals(NodeConfig.GetParameter<string>("Verb"), "open", StringComparison.OrdinalIgnoreCase))
            {
                // A shell verb of Windows: on Linux the desktop opens the file
                argumentList = [path];
                program = "xdg-open";
            }

            context.LogStep($"Executing external command: {program} {string.Join(' ', argumentList)}");
            if (runInBackground)
            {
                var startInfo = new ProcessStartInfo(program) { UseShellExecute = false };
                foreach (var argument in argumentList) startInfo.ArgumentList.Add(argument);
                if (!string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory)) startInfo.WorkingDirectory = workingDirectory;
                using (Process.Start(startInfo))
                {
                }

                context.LogStep($"External command '{commandName ?? program}' launched in background.");
                return;
            }

            var result = await ProcessRunner.RunAsync(program, argumentList, timeout: TimeSpan.FromMinutes(10), cancellationToken: cancellationToken,
                workingDirectory: string.IsNullOrWhiteSpace(workingDirectory) ? null : workingDirectory).ConfigureAwait(false);
            string output = result.OutputText;
            context.Properties["ExternalCommand.ExitCode"] = result.ExitCode;
            context.Properties["ExternalCommand.Output"] = output;
            context.Properties["ExternalCommand.Error"] = result.Error;
            if (NodeConfig.GetParameter<string>("OutputVariable") is { Length: > 0 } outputVariable) context.Properties[outputVariable] = output;
            if (NodeConfig.GetParameter<string>("ExitCodeVariable") is { Length: > 0 } exitCodeVariable) context.Properties[exitCodeVariable] = result.ExitCode;

            string uri = ExternalCommandDestination.FindUri(output);
            if (uri != null)
            {
                context.Properties["ExternalCommand.Uri"] = uri;
            }

            if (outputToClipboard && !string.IsNullOrEmpty(output))
            {
                await SystemClipboard.SetTextAsync(output).ConfigureAwait(false);
            }

            if (uriToClipboard && uri != null)
            {
                await SystemClipboard.SetTextAsync(uri).ConfigureAwait(false);
            }

            // The same result as the external command destination: the Notification step shows it
            var destination = new ExternalCommandDestination(RecipeServices.Settings, _commands,
                configured ?? new ExternalCommandDefinition(commandName ?? Path.GetFileNameWithoutExtension(program), program));
            var exportResult = result.ExitCode == 0
                ? ExportResult.Done(path, uri ?? destination.DisplayName) with { Link = uri }
                : ExportResult.Failed(string.IsNullOrEmpty(result.Error) ? $"Exit code {result.ExitCode}" : result.Error.Trim());
            PluginStepHelper.Record(context, destination, exportResult, source);

            // An optimizer changed the file: the flow goes on with it
            if (NodeConfig.GetParameter<bool?>("ReloadAfterExecution") == true && File.Exists(path))
            {
                var reloaded = SkiaSharp.SKBitmap.Decode(path);
                if (reloaded != null)
                {
                    context.Payload.RawCapture = new Capture(reloaded, context.Payload.Details ?? details);
                    context.Payload.Surface?.Dispose();
                    context.Payload.Surface = null;
                    context.Payload.InvalidateExportSource();
                    context.LogStep($"Reloaded transformed image from '{path}' ({reloaded.Width}x{reloaded.Height}).");
                }
            }

            context.LogStep($"External command finished with exit code {result.ExitCode}.");
        }
    }

    [StepInfo("Pdf", "Save as PDF", "Saves the capture as a PDF file.", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepParameter("PageSize", ContractDataType.Enum, Description = "PDF page size (default: PDF settings)", AllowedValues = new[] { "Image", "Custom", "A3", "A4", "A5", "A6", "Letter" })]
    [StepParameter("PageWidthMm", ContractDataType.Decimal, Description = "Custom page width in millimeters (default: PDF settings)")]
    [StepParameter("PageHeightMm", ContractDataType.Decimal, Description = "Custom page height in millimeters (default: PDF settings)")]
    [StepParameter("MarginTopMm", ContractDataType.Decimal, Description = "Top margin in millimeters (default: PDF settings)")]
    [StepParameter("MarginBottomMm", ContractDataType.Decimal, Description = "Bottom margin in millimeters (default: PDF settings)")]
    [StepParameter("MarginLeftMm", ContractDataType.Decimal, Description = "Left margin in millimeters (default: PDF settings)")]
    [StepParameter("MarginRightMm", ContractDataType.Decimal, Description = "Right margin in millimeters (default: PDF settings)")]
    [StepParameter("ScalingMode", ContractDataType.Enum, Description = "How to scale the image on a fixed page (default: PDF settings)", AllowedValues = new[] { "OnlyShrinkToFit", "FitToPage" })]
    [StepParameter("ShowSaveDialog", ContractDataType.Boolean, DefaultValue = true, Description = "Show the PDF save dialog")]
    [StepOutputVariable("Pdf.FilePath", ContractDataType.FilePath, "Path of the saved PDF file", Conditional = true)]
    public sealed class PdfStep(RecipeNodeConfig config, PdfPlugin plugin) : ICaptureStep
    {
        public string Name { get; } = config.Name ?? "PdfExportStep";

        public RecipeNodeConfig NodeConfig { get; } = config;

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var source = await PluginStepHelper.SourceAsync(context, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                context.LogStep("PdfStep: No surface available to export.");
                return;
            }

            // The settings of the plugin, changed by the parameters of the step
            var defaults = plugin.Settings.Options;
            string scaling = NodeConfig.GetParameter<string>("ScalingMode");
            var page = new PdfPageOptions(
                PdfSettings.PageSizes.FirstOrDefault(p => p.Equals(NodeConfig.GetParameter<string>("PageSize"), StringComparison.OrdinalIgnoreCase)) ?? defaults.PageSize,
                NodeConfig.GetParameter("PageWidthMm", defaults.PageWidthMm),
                NodeConfig.GetParameter("PageHeightMm", defaults.PageHeightMm),
                NodeConfig.GetParameter("MarginTopMm", defaults.MarginTopMm),
                NodeConfig.GetParameter("MarginBottomMm", defaults.MarginBottomMm),
                NodeConfig.GetParameter("MarginLeftMm", defaults.MarginLeftMm),
                NodeConfig.GetParameter("MarginRightMm", defaults.MarginRightMm),
                scaling == null ? defaults.FitToPage : scaling.Equals("FitToPage", StringComparison.OrdinalIgnoreCase));
            var destination = plugin.CreateDestination(page, NodeConfig.GetParameter("ShowSaveDialog", true));
            var result = await destination.ExportAsync(source, cancellationToken).ConfigureAwait(false);
            PluginStepHelper.Record(context, destination, result, source);
            if (result.Success)
            {
                context.Properties["Pdf.FilePath"] = result.FilePath;
                context.LogStep($"Saved capture as PDF: {result.FilePath}");
            }
            else if (result.Cancelled)
            {
                context.LogStep("PdfStep: PDF export was declined.");
            }
            else
            {
                context.LogStep($"PdfStep: Could not save capture as PDF: {result.Message}");
            }
        }
    }

    [StepInfo("Box", "Upload to Box", "Uploads the capture to Box (configured account).", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepOutputVariable("Box.UploadUrl", ContractDataType.String, "Link to the uploaded file", Conditional = true)]
    public class BoxStep(RecipeNodeConfig config, BoxPlugin plugin) : CloudStep(config, plugin.Destination, "Box (box.com)", "Box.UploadUrl");

    [StepInfo("Dropbox", "Upload to Dropbox", "Uploads the capture to Dropbox (configured account).", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepOutputVariable("Dropbox.UploadUrl", ContractDataType.String, "Link to the uploaded file", Conditional = true)]
    public class DropboxStep(RecipeNodeConfig config, DropboxPlugin plugin) : CloudStep(config, plugin.Destination, "Dropbox (dropbox.com)", "Dropbox.UploadUrl");

    /// <summary>
    /// Upload with the destination of a cloud plugin, the link goes into the variable
    /// </summary>
    public abstract class CloudStep(RecipeNodeConfig config, IDestination destination, string service, string urlVariable) : ICaptureStep, IRequiresRecipeAuthorization
    {
        public string Name { get; } = config.Name ?? config.StepType;

        public RecipeNodeConfig NodeConfig { get; } = config;

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            yield return new RecipeGatedAction(RecipeGateType.NetworkAccess, service);
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var source = await PluginStepHelper.SourceAsync(context, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                context.LogStep($"{Name}: No capture to upload.");
                return;
            }

            context.LogStep($"Uploading capture to {service}...");
            // The flow continues when it fails, the notification shows it
            var result = await destination.ExportAsync(source, cancellationToken).ConfigureAwait(false);
            if (result.Success && !string.IsNullOrEmpty(result.Link))
            {
                context.Properties[urlVariable] = result.Link;
            }

            PluginStepHelper.Record(context, destination, result, source);
        }
    }

    [StepInfo("Jira", "Attach to Jira", "Attaches the capture to a Jira issue.", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepParameter("Format", ContractDataType.Enum, Description = "Image format of the upload", AllowedValuesProvider = typeof(SaveableFileFormatIds))]
    [StepParameter("JpegQuality", ContractDataType.Integer, Description = "JPEG quality (1-100) when uploading as JPEG")]
    [StepParameter("ReduceColors", ContractDataType.Boolean, Description = "Reduce the image to 256 colors")]
    [StepParameter("IssueKey", ContractDataType.String, Description = "Issue to attach to (default: variable Jira.IssueKey)")]
    [StepInputVariable("Jira.IssueKey", ContractDataType.String, Description = "Issue to attach to, when the IssueKey parameter is not set")]
    [StepOutputVariable("Jira.UploadUrl", ContractDataType.String, "Link to the attachment", Conditional = true)]
    [StepOutputVariable("Jira.IssueKey", ContractDataType.String, "The issue the capture was attached to", Conditional = true)]
    public class JiraStep(RecipeNodeConfig config, JiraPlugin plugin) : ICaptureStep, IRequiresRecipeAuthorization
    {
        public string Name { get; } = config.Name ?? "JiraUploadStep";

        public RecipeNodeConfig NodeConfig { get; } = config;

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            yield return new RecipeGatedAction(RecipeGateType.NetworkAccess, "Jira");
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var source = await PluginStepHelper.SourceAsync(context, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                context.LogStep("JiraStep: No capture to upload.");
                return;
            }

            string issueKey = NodeConfig.GetParameter<string>("IssueKey") ?? (context.Properties.TryGetValue("Jira.IssueKey", out var key) ? key as string : null);
            issueKey = PluginStepHelper.Fill(issueKey, source.Details);
            var destination = plugin.Destinations.First();
            if (string.IsNullOrEmpty(issueKey))
            {
                // Without an issue: the Jira window, like the destination
                context.LogStep("JiraStep: No issue key specified; delegating to Jira destination.");
                var chosen = await destination.ExportAsync(source, cancellationToken).ConfigureAwait(false);
                PluginStepHelper.Record(context, destination, chosen, source);
                if (chosen.Link != null)
                {
                    context.Properties["Jira.UploadUrl"] = chosen.Link;
                    context.Properties["Jira.IssueKey"] = chosen.Link.Split('/').Last();
                }

                return;
            }

            context.LogStep($"Attaching capture to Jira issue '{issueKey}'...");
            try
            {
                var image = PluginStepHelper.Encode(source, NodeConfig, plugin.Settings.UploadFormat, plugin.Settings.UploadJpegQuality);
                var link = await plugin.Connector.UploadAsync(issueKey, image, plugin.Settings.Filename(source), null, cancellationToken).ConfigureAwait(false);
                context.Properties["Jira.UploadUrl"] = link.AbsoluteUri;
                context.Properties["Jira.IssueKey"] = issueKey;
                PluginStepHelper.Record(context, destination, ExportResult.Done(message: Greenshot.Linux.Core.Languages.Texts.Get("Jira", "upload_success")) with { Link = link.AbsoluteUri }, source);
            }
            catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException or System.Text.Json.JsonException or TaskCanceledException)
            {
                context.LogStep($"JiraStep: Failed to attach capture to '{issueKey}': {ex.Message}");
                PluginStepHelper.Record(context, destination, ExportResult.Failed(ex.Message), source);
            }
        }
    }

    [StepInfo("Confluence", "Upload to Confluence", "Uploads the capture as attachment to a Confluence page, as a new version when the page has a file with the name.", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepParameter("Format", ContractDataType.Enum, Description = "Image format of the upload", AllowedValuesProvider = typeof(SaveableFileFormatIds))]
    [StepParameter("JpegQuality", ContractDataType.Integer, Description = "JPEG quality (1-100) when uploading as JPEG")]
    [StepParameter("ReduceColors", ContractDataType.Boolean, Description = "Reduce the image to 256 colors")]
    [StepParameter("PageId", ContractDataType.String, Description = "Page to attach to")]
    [StepOutputVariable("Confluence.PageId", ContractDataType.String, "The page the capture was attached to", Conditional = true)]
    [StepOutputVariable("Confluence.UploadUrl", ContractDataType.String, "Link to the attachment", Conditional = true)]
    public class ConfluenceStep(RecipeNodeConfig config, ConfluencePlugin plugin) : ICaptureStep, IRequiresRecipeAuthorization
    {
        public string Name { get; } = config.Name ?? "ConfluenceUploadStep";

        public RecipeNodeConfig NodeConfig { get; } = config;

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            yield return new RecipeGatedAction(RecipeGateType.NetworkAccess, "Confluence");
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var source = await PluginStepHelper.SourceAsync(context, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                context.LogStep("ConfluenceStep: No capture to upload.");
                return;
            }

            var destination = plugin.Destinations.First();
            string pageId = PluginStepHelper.Fill(NodeConfig.GetParameter<string>("PageId"), source.Details);
            if (string.IsNullOrEmpty(pageId) || !long.TryParse(pageId, out long page))
            {
                context.LogStep("ConfluenceStep: Delegating to Confluence destination.");
                var chosen = await destination.ExportAsync(source, cancellationToken).ConfigureAwait(false);
                PluginStepHelper.Record(context, destination, chosen, source);
                if (chosen.Link != null)
                {
                    context.Properties["Confluence.UploadUrl"] = chosen.Link;
                }

                return;
            }

            context.LogStep($"Uploading capture to Confluence page '{page}'...");
            try
            {
                var image = PluginStepHelper.Encode(source, NodeConfig, plugin.Settings.UploadFormat, plugin.Settings.UploadJpegQuality);
                // Nobody to ask: a file name the page already has gets a new version of that attachment
                bool exists = await plugin.Connector.HasAttachmentAsync(page, plugin.Settings.Filename(source), cancellationToken).ConfigureAwait(false);
                await plugin.Connector.AttachAsync(page, image, plugin.Settings.Filename(source), exists, cancellationToken).ConfigureAwait(false);
                context.Properties["Confluence.PageId"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
                PluginStepHelper.Record(context, destination, ExportResult.Done(message: Greenshot.Linux.Core.Languages.Texts.Get("Confluence", "upload_success")), source);
            }
            catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException or System.Text.Json.JsonException or TaskCanceledException)
            {
                context.LogStep($"ConfluenceStep: Failed to upload to '{page}': {ex.Message}");
                PluginStepHelper.Record(context, destination, ExportResult.Failed(ex.Message), source);
            }
        }
    }

    /// <summary>
    /// The names of the imported uploaders, the values of the Uploader parameter
    /// </summary>
    public sealed class UploaderNames : IAllowedValuesProvider
    {
        public static Func<IReadOnlyList<string>> Names { get; set; } = () => Array.Empty<string>();

        public IReadOnlyList<string> GetAllowedValues() => Names();
    }

    [StepInfo("CustomUploader", "Upload with a custom uploader", "Uploads the capture with an uploader imported from an .sxcu file.", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepParameter("Uploader", ContractDataType.Enum, Description = "Name of the imported uploader, empty when there is only one", AllowedValuesProvider = typeof(UploaderNames))]
    [StepOutputVariable("CustomUploader.Url", ContractDataType.String, "Link to the upload", Conditional = true)]
    public class CustomUploaderStep(RecipeNodeConfig config, CustomUploaderPlugin plugin) : ICaptureStep, IRequiresRecipeAuthorization
    {
        public string Name { get; } = config.Name ?? "CustomUploaderStep";

        public RecipeNodeConfig NodeConfig { get; } = config;

        private CustomUploaderDestination FindUploader()
        {
            string name = NodeConfig.GetParameter<string>("Uploader");
            var uploaders = plugin.Destinations.OfType<CustomUploaderDestination>().ToList();
            return string.IsNullOrWhiteSpace(name)
                ? uploaders.Count == 1 ? uploaders[0] : null
                : uploaders.FirstOrDefault(u => string.Equals(u.Definition.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            var uploader = FindUploader();
            yield return uploader != null
                ? new RecipeGatedAction(RecipeGateType.NetworkAccess, $"{uploader.Definition.Name} ({Greenshot.Linux.Plugins.CustomUploader.Sxcu.UploaderFile.GetServer(uploader.Definition).Host})")
                : new RecipeGatedAction(RecipeGateType.NetworkAccess, NodeConfig.GetParameter<string>("Uploader") ?? CustomUploaderDestination.DesignationPrefix);
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var source = await PluginStepHelper.SourceAsync(context, cancellationToken).ConfigureAwait(false);
            var uploader = FindUploader();
            if (source == null || uploader == null)
            {
                context.LogStep($"CustomUploaderStep: No uploader '{NodeConfig.GetParameter<string>("Uploader")}', import it in the settings.");
                return;
            }

            context.LogStep($"Uploading capture with {uploader.Definition.Name}...");
            var result = await uploader.ExportAsync(source, cancellationToken).ConfigureAwait(false);
            if (result.Success && result.Link != null)
            {
                context.Properties["CustomUploader.Url"] = result.Link;
            }

            PluginStepHelper.Record(context, uploader, result, source);
        }
    }

    [StepInfo(ZxingStep.StepType, "Barcode Scanner (ZXing)", "Scans and decodes 1D/2D barcodes (such as QR codes) from the capture bitmap.", "Analysis")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required, ExtractedText = PayloadRequirement.Created)]
    [StepParameter("OutputVariable", ContractDataType.String, Description = "Also store the decoded text in this variable", SupportsExpressions = false)]
    [StepParameter("CopyToClipboard", ContractDataType.Boolean, DefaultValue = false, Description = "Copy the decoded text to the clipboard")]
    [StepOutputVariable("Barcode.Text", ContractDataType.String, "Decoded text of the detected barcode(s), one per line", Conditional = true)]
    [StepOutputVariable("Barcode.Format", ContractDataType.String, "Format of the detected barcode(s), e.g. QR_CODE, one per line", Conditional = true)]
    [StepOutputVariable("{Parameter:OutputVariable}", ContractDataType.String, "The decoded text, when OutputVariable is set", Conditional = true)]
    public class ZxingStep(RecipeNodeConfig config) : ICaptureStep
    {
        public const string StepType = "BarcodeScan";

        public string Name { get; } = config.Name ?? "ZxingBarcodeStep";

        public RecipeNodeConfig NodeConfig { get; } = config;

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var source = await PluginStepHelper.SourceAsync(context, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                context.LogStep("ZxingStep: No capture to scan.");
                return;
            }

            context.LogStep("Scanning surface for QR codes / barcodes...");
            var codes = Barcodes.Detect(source.Image);
            if (codes.Count == 0)
            {
                context.LogStep("ZxingStep: No QR codes or barcodes found on surface.");
                return;
            }

            string text = string.Join(Environment.NewLine, codes.Select(c => c.Text));
            context.Payload.ExtractedText = text;
            context.Properties["Barcode.Text"] = text;
            context.Properties["Barcode.Format"] = string.Join(Environment.NewLine, codes.Select(c => c.Format));
            if (NodeConfig.GetParameter<string>("OutputVariable") is { Length: > 0 } variable)
            {
                context.Properties[variable] = text;
            }

            if (NodeConfig.GetParameter<bool?>("CopyToClipboard") == true)
            {
                await SystemClipboard.SetTextAsync(text).ConfigureAwait(false);
                context.LogStep($"Copied {codes.Count} decoded barcode(s) to clipboard.");
            }

            context.LogStep($"Detected {codes.Count} barcode(s)/QR code(s): {text}");
        }
    }
}

namespace Greenshot.Linux.Plugins.Recipes
{
    /// <summary>
    /// The steps of the plugins which are built into Greenshot for Linux: BarcodeScan of the ZXing plugin
    /// </summary>
    public sealed class BuiltInPluginSteps : IRecipeStepProvider
    {
        public void RegisterSteps(IStepRegistry registry) => registry.Register<ZxingStep>(config => new ZxingStep(config));
    }
}
