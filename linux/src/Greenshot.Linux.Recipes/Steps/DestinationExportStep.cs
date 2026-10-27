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

using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.Destinations, "Export Destinations", "Exports the capture to one or more destinations (file, clipboard, editor, printer, email, plugins).", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required, ExtractedText = PayloadRequirement.Optional)]
    [StepParameter("DestinationDesignations", ContractDataType.Object, ValueType = typeof(List<string>), Description = "List of destination designations (default: settings)")]
    [StepParameter("SaveDirectory", ContractDataType.DirectoryPath, Description = "Directory to save to")]
    [StepParameter("FilenamePattern", ContractDataType.String, Description = "File name pattern (default: settings)")]
    [StepParameter("Format", ContractDataType.Enum, Description = "Image format (default: settings)", AllowedValuesProvider = typeof(SaveableFileFormatIds))]
    [StepParameter("JpegQuality", ContractDataType.Integer, Description = "JPEG quality (1-100)")]
    [StepParameter("ReduceColors", ContractDataType.Boolean, Description = "Reduce the image to 256 colors")]
    [StepParameter("PromptQuality", ContractDataType.Boolean, Description = "Ask for the JPEG quality")]
    [StepParameter("AllowOverwrite", ContractDataType.Boolean, Description = "Overwrite an existing file")]
    [StepParameter("CopyPathToClipboard", ContractDataType.Boolean, Description = "Copy the saved file's path to the clipboard")]
    [StepParameter("TargetEditor", ContractDataType.Enum, Description = "Editor to use (Editor)", AllowedValues = new[] { "NewEditor", "AvailableEditor", "CurrentEditor" })]
    [StepParameter("MatchSizeToCapture", ContractDataType.Boolean, Description = "Size the editor to the capture (Editor)")]
    [StepParameter("PrinterName", ContractDataType.String, Description = "Printer to use (Printer)")]
    [StepParameter("ShowPrintDialog", ContractDataType.Boolean, Description = "Show the print options (Printer)")]
    [StepParameter("AllowRotate", ContractDataType.Boolean, Description = "Rotate to fit the page (Printer)")]
    [StepParameter("AllowEnlarge", ContractDataType.Boolean, Description = "Enlarge to fit the page (Printer)")]
    [StepParameter("AllowShrink", ContractDataType.Boolean, Description = "Shrink to fit the page (Printer)")]
    [StepParameter("Center", ContractDataType.Boolean, Description = "Center on the page (Printer)")]
    [StepParameter("ColorMode", ContractDataType.Enum, Description = "Print colors (Printer)", AllowedValues = new[] { "Color", "Grayscale", "Monochrome" })]
    [StepParameter("PrintFooter", ContractDataType.Boolean, Description = "Print a footer (Printer)")]
    [StepParameter("FooterPattern", ContractDataType.String, Description = "Footer text pattern (Printer)")]
    [StepParameter("ClipboardMode", ContractDataType.Enum, DefaultValue = "ImageOnly", Description = "What to copy (Clipboard); TextOnly copies the extracted or OCR text", AllowedValues = new[] { "ImageOnly", "TextOnly", "ImageAndText" })]
    [StepParameter("ClipboardCustomText", ContractDataType.String, Description = "Text to copy instead of the OCR text; ${ocr_text} is the OCR text (Clipboard)")]
    [StepParameter("ClipboardFormatPNG", ContractDataType.Boolean, DefaultValue = true, Description = "Copy as PNG (Clipboard)")]
    [StepParameter("ClipboardFormatDIB", ContractDataType.Boolean, DefaultValue = true, Description = "Copy as DIB (Clipboard)")]
    [StepParameter("ClipboardFormatDIBV5", ContractDataType.Boolean, Description = "Copy as DIBV5 (Clipboard)")]
    [StepParameter("ClipboardFormatBitmap", ContractDataType.Boolean, Description = "Copy as bitmap (Clipboard)")]
    [StepParameter("ClipboardFormatHTML", ContractDataType.Boolean, Description = "Copy as HTML (Clipboard)")]
    [StepParameter("ClipboardFormatHTMLDataUrl", ContractDataType.Boolean, Description = "Copy as HTML with a data URL (Clipboard)")]
    [StepParameter("ClipboardFormatText", ContractDataType.Boolean, Description = "Also copy the text (Clipboard)")]
    [StepParameter("CustomDestinationId", ContractDataType.String, Description = "Destination designation (CustomDestination)")]
    [StepInputVariable("OverrideDestinations", ContractDataType.Object, Description = "Destinations to use instead of the configured ones (set by the caller)")]
    [StepInputVariable("EditorForm", ContractDataType.Object, Description = "The editor for TargetEditor CurrentEditor (set by the editor trigger)")]
    [StepOutputVariable("Destination.Filename", ContractDataType.FilePath, "Path of the file to save (with SaveDirectory)", Conditional = true, WhenParameter = "SaveDirectory")]
    [StepOutputVariable("Destination.SaveDirectory", ContractDataType.DirectoryPath, "The expanded SaveDirectory", Conditional = true, WhenParameter = "SaveDirectory")]
    [StepOutputVariable("Destination.SurfaceOutputSettings", ContractDataType.Object, "Output settings for the destinations (with JpegQuality or ReduceColors)", Conditional = true)]
    [StepOutputVariable("Destination.PromptQuality", ContractDataType.Boolean, "The PromptQuality parameter, for the destinations", Conditional = true, WhenParameter = "PromptQuality")]
    [StepOutputVariable("Destination.AllowOverwrite", ContractDataType.Boolean, "The AllowOverwrite parameter, for the destinations", Conditional = true, WhenParameter = "AllowOverwrite")]
    [StepOutputVariable("Destination.CopyPathToClipboard", ContractDataType.Boolean, "The CopyPathToClipboard parameter, for the destinations", Conditional = true, WhenParameter = "CopyPathToClipboard")]
    [StepOutputVariable("DestinationExportErrors", ContractDataType.String, "Errors of destinations that failed", Conditional = true)]
    public class DestinationExportStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationExportStep));

        private readonly IDestinationDispatcher _dispatcher;

        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public DestinationExportStep(RecipeNodeConfig config, IDestinationDispatcher dispatcher = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "DestinationExportStep";
            _dispatcher = dispatcher;
        }

        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            string saveDirectory = Config.GetParameter<string>("SaveDirectory");
            if (!string.IsNullOrWhiteSpace(saveDirectory))
            {
                yield return new RecipeGatedAction(RecipeGateType.FileSystemAccess, $"Writes files to {saveDirectory}");
            }

            var known = RecipeServices.Host?.Destinations ?? Array.Empty<IDestination>();
            foreach (var designation in ResolveDestinationDesignations(null, out _))
            {
                if (string.IsNullOrWhiteSpace(designation)) continue;
                var destination = known.FirstOrDefault(d => Matches(d, designation)) ?? RecipeServices.AuthorizationDestinations?.Invoke(designation);
                if (destination is IRequiresRecipeAuthorization authorization)
                {
                    foreach (var action in authorization.GetGatedActions())
                    {
                        yield return action;
                    }
                }
            }
        }

        /// <summary>
        /// "CustomUploader" means the uploaders of the plugin, else the designation must match
        /// </summary>
        public static bool Matches(IDestination destination, string designation) =>
            destination.Designation.Equals(designation, StringComparison.OrdinalIgnoreCase)
            || destination.Designation.StartsWith(designation + ":", StringComparison.OrdinalIgnoreCase);

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            context.State = CaptureFlowState.Exporting;
            if (string.Equals(Config.StepType, WellKnownStepTypes.Clipboard, StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteClipboardExportAsync(context, cancellationToken).ConfigureAwait(false);
                return;
            }

            ApplyCustomFileSettings(context);
            var designations = ResolveDestinationDesignations(context, out bool fromSettings).ToList();
            if (fromSettings && context.Payload?.Details?.Mode == Greenshot.Linux.Core.Capturing.CaptureMode.Text)
            {
                // Text chosen on the capture screen (T): its text goes to the clipboard, not the image to the destinations
                designations = [WellKnownDestinations.Ocr];
                fromSettings = false;
            }
            var available = RecipeServices.Host?.Destinations ?? Array.Empty<IDestination>();
            var destinations = new List<IDestination>();
            foreach (var designation in designations.Select(WellKnownDestinations.Normalize))
            {
                if (string.IsNullOrWhiteSpace(designation)) continue;
                var matching = available.Where(d => Matches(d, designation)).ToList();
                if (matching.Count == 0)
                {
                    Log.WarnFormat("Destination '{0}' is not available (switched off, or not on Linux).", designation);
                }

                destinations.AddRange(matching.Where(d => d.IsAvailable && !destinations.Contains(d)));
            }

            // The settings keep destinations which aren't there right now: without any other the capture opens in the editor
            if (destinations.Count == 0 && fromSettings)
            {
                destinations.AddRange(available.Where(d => d.Designation == WellKnownDestinations.Editor));
            }

            if (destinations.Count > 0)
            {
                context.LogStep($"Exporting capture to {destinations.Count} destination(s): {string.Join(", ", destinations.Select(d => d.Designation))}");
            }
            else
            {
                context.LogStep($"Warning: No active destinations resolved for designations: {string.Join(", ", designations)}");
            }

            await (_dispatcher ?? new DestinationDispatcher()).DispatchAsync(context, destinations, cancellationToken).ConfigureAwait(false);
        }

        private void ApplyCustomFileSettings(CaptureFlowContext context)
        {
            if (Config.GetParameter<bool?>("AllowOverwrite") is { } allowOverwrite) context.Properties[RecipeFileDestination.AllowOverwriteProperty] = allowOverwrite;
            if (Config.GetParameter<bool?>("CopyPathToClipboard") is { } copyPath) context.Properties[RecipeFileDestination.CopyPathProperty] = copyPath;
            if (Config.GetParameter<int?>("JpegQuality") is { } quality) context.Properties[RecipeFileDestination.JpegQualityProperty] = Math.Clamp(quality, 1, 100);
            if (Config.GetParameter<bool?>("PromptQuality") is { } promptQuality) context.Properties["Destination.PromptQuality"] = promptQuality;

            string format = Config.GetParameter<string>("Format");
            if (!string.IsNullOrWhiteSpace(format))
            {
                string normalized = ImageFormats.Normalize(format);
                if (!ImageFormats.Supported.Contains(normalized))
                {
                    Log.WarnFormat("Unknown output file format '{0}' in recipe; using png.", format);
                    normalized = "png";
                }

                context.Properties[RecipeFileDestination.FormatProperty] = normalized;
            }

            string directory = Config.GetParameter<string>("SaveDirectory");
            var details = context.Payload?.Details;
            if (!string.IsNullOrWhiteSpace(directory) && details != null && RecipeServices.Settings != null)
            {
                string pattern = Config.GetParameter<string>("FilenamePattern") ?? RecipeServices.Settings.OutputFileFilenamePattern;
                string fileFormat = context.Properties.TryGetValue(RecipeFileDestination.FormatProperty, out var f) ? (string)f : ImageFormats.Normalize(RecipeServices.Settings.OutputFileFormat);
                string path = RecipeFileDestination.BuildPath(directory, pattern, fileFormat, details);
                context.Properties["Destination.SaveDirectory"] = Path.GetDirectoryName(path);
                context.Properties[RecipeFileDestination.FilenameProperty] = path;
                context.Payload.Metadata["Filename"] = path;
                Log.InfoFormat("Custom save location configured: '{0}'", path);
            }
        }

        private IEnumerable<string> ResolveDestinationDesignations(CaptureFlowContext context, out bool fromSettings)
        {
            fromSettings = false;
            string stepType = Config.StepType;
            if (string.Equals(stepType, WellKnownStepTypes.SaveFile, StringComparison.OrdinalIgnoreCase)) return [WellKnownDestinations.FileDefault];
            if (string.Equals(stepType, WellKnownStepTypes.Clipboard, StringComparison.OrdinalIgnoreCase)) return [WellKnownDestinations.Clipboard];
            if (string.Equals(stepType, WellKnownStepTypes.Editor, StringComparison.OrdinalIgnoreCase)) return [WellKnownDestinations.Editor];
            if (string.Equals(stepType, WellKnownStepTypes.Printer, StringComparison.OrdinalIgnoreCase)) return [WellKnownDestinations.Printer];
            if (string.Equals(stepType, WellKnownStepTypes.Email, StringComparison.OrdinalIgnoreCase)) return [WellKnownDestinations.Email];
            if (string.Equals(stepType, WellKnownStepTypes.CustomDestination, StringComparison.OrdinalIgnoreCase)
                && Config.GetParameter<string>("CustomDestinationId") is { } custom && !string.IsNullOrWhiteSpace(custom))
            {
                return [custom.Trim()];
            }

            if (context != null && context.Properties.TryGetValue("OverrideDestinations", out var overrides) && overrides != null)
            {
                if (overrides is IEnumerable<string> list) return list;
                if (overrides is string single && !string.IsNullOrWhiteSpace(single)) return [single];
            }

            var stepDestinations = Config.GetParameter<List<string>>("DestinationDesignations");
            if (stepDestinations is { Count: > 0 })
            {
                return stepDestinations;
            }

            fromSettings = true;
            return (IEnumerable<string>)RecipeServices.Settings?.OutputDestinations ?? Array.Empty<string>();
        }

        private async Task ExecuteClipboardExportAsync(CaptureFlowContext context, CancellationToken cancellationToken)
        {
            string mode = Config.GetParameter<string>("ClipboardMode") ?? "ImageOnly";
            bool textOnly = string.Equals(mode, "TextOnly", StringComparison.OrdinalIgnoreCase);
            bool withText = string.Equals(mode, "ImageAndText", StringComparison.OrdinalIgnoreCase) || (Config.GetParameter<bool?>("ClipboardFormatText") ?? false);
            string text = null;
            if (textOnly || withText)
            {
                text = !string.IsNullOrWhiteSpace(context.Payload?.ExtractedText)
                    ? context.Payload.ExtractedText
                    : await OcrStep.ExtractTextAsync(context, cancellationToken).ConfigureAwait(false);
                string customPattern = Config.GetParameter<string>("ClipboardCustomText");
                if (!string.IsNullOrWhiteSpace(customPattern))
                {
                    text = customPattern.Replace("${ocr_text}", text ?? "").Replace("${payload.text}", text ?? "");
                }
            }

            if (textOnly)
            {
                try
                {
                    await SystemClipboard.SetTextAsync(text ?? string.Empty).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
                {
                    throw new ClipboardException($"Copying to the clipboard failed: {ex.Message}", null, ex);
                }

                context.LogStep(string.IsNullOrWhiteSpace(text) ? "Warning: No OCR text detected to place on clipboard." : $"Copied {text.Length} character(s) of OCR text to clipboard.");
                return;
            }

            if (context.Payload?.RawCapture == null)
            {
                return;
            }

            // Extensions on the BeforeDestination slot (border, ...) change a copy for the clipboard
            var payload = context.Payload;
            var chains = DestinationDispatcher.ChainsFor(context, WellKnownDestinations.Clipboard);
            if (chains.Count > 0)
            {
                var copyContext = await DestinationDispatcher.RunChainsOnCopyAsync(context, chains, "the clipboard", cancellationToken).ConfigureAwait(false);
                payload = copyContext?.Payload ?? payload;
            }

            var source = await payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
            // The Wayland clipboard offers one type per copy: the image (PNG); the text only when no image is wanted
            try
            {
                await SystemClipboard.SetImageAsync(ImageFormats.Encode(source.Image, "png"), "image/png").ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
            {
                throw new ClipboardException($"Copying to the clipboard failed: {ex.Message}", null, ex);
            }
            if (withText && !string.IsNullOrEmpty(text))
            {
                Log.Info("The clipboard holds the image, the text isn't copied with it on Linux");
            }

            var clipboard = (RecipeServices.Host?.Destinations ?? Array.Empty<IDestination>()).FirstOrDefault(d => d.Designation == WellKnownDestinations.Clipboard);
            if (clipboard != null)
            {
                context.RecordExport(clipboard, ExportResult.Done(), source.Details, source);
            }

            context.LogStep("Copied capture to clipboard.");
        }
    }
}
