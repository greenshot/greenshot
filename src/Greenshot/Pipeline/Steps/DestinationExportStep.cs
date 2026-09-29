using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Contracts = Greenshot.Base.Pipeline.Contracts;

using Greenshot.Base.Recipes;
using Greenshot.Destinations;
using Greenshot.Editor.Destinations;
using Greenshot.Helpers;
using log4net;
using Greenshot.Base.Threading;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step that resolves and dispatches captures to destinations.
    /// Supports individual destination steps (File, Clipboard, Editor, Printer, Email, Custom)
    /// as well as custom storage directories, filename patterns, and clipboard format selections.
    /// </summary>
    [StepInfo(WellKnownStepTypes.Destinations, "Export Destinations", "Exports the capture to one or more destinations (file, clipboard, editor, printer, email, plugins).", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required, ExtractedText = PayloadRequirement.Optional)]
    [StepParameter("DestinationDesignations", ContractDataType.Object, Description = "List of destination designations (default: settings)")]
    [StepParameter("SaveDirectory", ContractDataType.DirectoryPath, Description = "Directory to save to")]
    [StepParameter("FilenamePattern", ContractDataType.String, Description = "File name pattern (default: settings)")]
    [StepParameter("Format", ContractDataType.Enum, Description = "Image format (default: settings)", AllowedValues = new[] { "png", "jpg", "bmp", "gif", "tiff", "greenshot" })]
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
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

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
            var designations = ResolveDestinationDesignations(null);
            if (designations != null)
            {
                foreach (var des in designations)
                {
                    if (string.IsNullOrWhiteSpace(des)) continue;

                    IDestination dest = DestinationHelper.GetDestination(des)
                        ?? SimpleServiceProvider.Current.GetAllInstances<IDestination>()
                            .FirstOrDefault(d => string.Equals(d.Designation, des, StringComparison.OrdinalIgnoreCase));

                    if (dest is IRequiresRecipeAuthorization authDest)
                    {
                        foreach (var action in authDest.GetGatedActions())
                        {
                            yield return action;
                        }
                    }
                }
            }
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            context.State = CaptureFlowState.Exporting;

            // Handle dedicated Clipboard step with format and OCR text customization
            if (string.Equals(Config.StepType, WellKnownStepTypes.Clipboard, StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteClipboardExportAsync(context, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Handle custom SaveDirectory & Filename overrides
            ApplyCustomFileSettings(context);

            var designations = ResolveDestinationDesignations(context).ToList();
            List<IDestination> destinations = new List<IDestination>();

            foreach (var designation in designations)
            {
                if (string.IsNullOrWhiteSpace(designation)) continue;

                IDestination dest = null;

                if (string.Equals(designation, EditorDestination.DESIGNATION, StringComparison.OrdinalIgnoreCase))
                {
                    TargetEditor? targetEditor = Config.GetParameter<TargetEditor?>("TargetEditor");

                    if (targetEditor == TargetEditor.CurrentEditor
                        && context.Properties.TryGetValue("EditorForm", out var editorObject)
                        && editorObject is IImageEditor editor)
                    {
                        dest = new EditorDestination(editor, true);
                    }
                    else
                    {
                        if (targetEditor == TargetEditor.CurrentEditor)
                        {
                            Log.Warn("Property 'EditorForm' contains no valid editor instance. Falling back to another editor.");
                        }

                        bool? reuseAvailable = targetEditor.HasValue ? targetEditor == TargetEditor.AvailableEditor : (bool?)null;
                        bool? matchSize = Config.GetParameter<bool?>("MatchSizeToCapture");
                        dest = new EditorDestination(reuseAvailable, matchSize);
                    }
                }
                else if (string.Equals(designation, nameof(WellKnownDestinations.Printer), StringComparison.OrdinalIgnoreCase))
                {
                    string printerName = Config.GetParameter<string>("PrinterName");
                    bool? promptOptions = Config.GetParameter<bool?>("ShowPrintDialog");
                    bool? allowRotate = Config.GetParameter<bool?>("AllowRotate");
                    bool? allowEnlarge = Config.GetParameter<bool?>("AllowEnlarge");
                    bool? allowShrink = Config.GetParameter<bool?>("AllowShrink");
                    bool? center = Config.GetParameter<bool?>("Center");
                    string colorMode = Config.GetParameter<string>("ColorMode");
                    bool? printFooter = Config.GetParameter<bool?>("PrintFooter");
                    string footerPattern = Config.GetParameter<string>("FooterPattern");

                    var printOptions = new PrintOptions
                    {
                        AllowRotate = allowRotate,
                        AllowEnlarge = allowEnlarge,
                        AllowShrink = allowShrink,
                        Center = center,
                        PromptOptions = promptOptions,
                        Footer = printFooter,
                        FooterPattern = footerPattern,
                        Grayscale = string.Equals(colorMode, "Grayscale", StringComparison.OrdinalIgnoreCase) ? true : (string.Equals(colorMode, "Color", StringComparison.OrdinalIgnoreCase) ? false : (bool?)null),
                        Monochrome = string.Equals(colorMode, "Monochrome", StringComparison.OrdinalIgnoreCase) ? true : (string.Equals(colorMode, "Color", StringComparison.OrdinalIgnoreCase) ? false : (bool?)null),
                    };
                    dest = new PrinterDestination(printerName, printOptions);
                }
                else
                {
                    // Case-insensitive destination matching
                    dest = DestinationHelper.GetAllDestinations()
                        .FirstOrDefault(d => string.Equals(d.Designation, designation, StringComparison.OrdinalIgnoreCase));

                    if (dest == null)
                    {
                        dest = SimpleServiceProvider.Current.GetAllInstances<IDestination>()
                            .FirstOrDefault(d => string.Equals(d.Designation, designation, StringComparison.OrdinalIgnoreCase));
                    }
                }

                if (dest != null && dest.IsAvailableFor(context.Payload?.RawCapture?.CaptureDetails))
                {
                    destinations.Add(dest);
                }
                else if (dest != null)
                {
                    Log.WarnFormat("Destination '{0}' was resolved but is marked inactive in configuration.", dest.Designation);
                }
            }

            if (destinations.Count > 0)
            {
                Log.InfoFormat("DestinationExportStep: Resolved {0} destination(s): [{1}]",
                    destinations.Count, string.Join(", ", destinations.Select(d => d.Designation)));
                context.LogStep($"Exporting capture to {destinations.Count} destination(s): {string.Join(", ", destinations.Select(d => d.Designation))}");
            }
            else
            {
                Log.WarnFormat("DestinationExportStep: No active destinations resolved for designations: [{0}]", string.Join(", ", designations));
                context.LogStep($"Warning: No active destinations resolved for designations: {string.Join(", ", designations)}");
            }

            var dispatcher = _dispatcher ?? new DestinationDispatcher();
            await dispatcher.DispatchAsync(context, destinations, cancellationToken).ConfigureAwait(false);
        }

        private void ApplyCustomFileSettings(CaptureFlowContext context)
        {
            string customDir = Config.GetParameter<string>("SaveDirectory");
            bool? promptQuality = Config.GetParameter<bool?>("PromptQuality");
            if (promptQuality.HasValue) context.Properties["Destination.PromptQuality"] = promptQuality.Value;

            bool? allowOverwrite = Config.GetParameter<bool?>("AllowOverwrite");
            if (allowOverwrite.HasValue) context.Properties["Destination.AllowOverwrite"] = allowOverwrite.Value;

            bool? copyPath = Config.GetParameter<bool?>("CopyPathToClipboard");
            if (copyPath.HasValue) context.Properties["Destination.CopyPathToClipboard"] = copyPath.Value;

            int? jpegQuality = Config.GetParameter<int?>("JpegQuality");
            bool? reduceColors = Config.GetParameter<bool?>("ReduceColors");
            if (jpegQuality.HasValue || reduceColors.HasValue)
            {
                OutputFormat fmt = CoreConfig.OutputFileFormat;
                string fmtStr = Config.GetParameter<string>("Format");
                if (!string.IsNullOrWhiteSpace(fmtStr) && Enum.TryParse<OutputFormat>(fmtStr, true, out var parsedFmt))
                {
                    fmt = parsedFmt;
                }
                var sos = new SurfaceOutputSettings(fmt, jpegQuality ?? CoreConfig.OutputFileJpegQuality, reduceColors ?? CoreConfig.OutputFileReduceColors);
                context.Properties["Destination.SurfaceOutputSettings"] = sos;
            }

            if (!string.IsNullOrWhiteSpace(customDir))
            {
                var captureDetails = context.Payload?.RawCapture?.CaptureDetails;
                string expandedDir = FilenameHelper.FillPattern(customDir, captureDetails, false);
                if (!Directory.Exists(expandedDir))
                {
                    try
                    {
                        Directory.CreateDirectory(expandedDir);
                    }
                    catch (Exception ex)
                    {
                        Log.WarnFormat("Could not create directory '{0}': {1}", expandedDir, ex.Message);
                    }
                }

                string pattern = Config.GetParameter<string>("FilenamePattern")
                    ?? CoreConfig.OutputFileFilenamePattern
                    ?? "greenshot ${capturetime}";

                OutputFormat outputFormat = CoreConfig.OutputFileFormat;
                string formatStr = Config.GetParameter<string>("Format");
                if (!string.IsNullOrWhiteSpace(formatStr) && Enum.TryParse<OutputFormat>(formatStr, true, out var parsedFmt))
                {
                    outputFormat = parsedFmt;
                }

                if (captureDetails == null)
                {
                    captureDetails = context.Payload?.RawCapture?.CaptureDetails;
                }
                if (captureDetails != null)
                {
                    string filename = FilenameHelper.GetFilenameFromPattern(pattern, outputFormat, captureDetails);
                    string fullPath = Path.Combine(expandedDir, filename);
                    captureDetails.Filename = fullPath;
                    context.Properties["Destination.SaveDirectory"] = expandedDir;
                    context.Properties["Destination.Filename"] = fullPath;
                    Log.InfoFormat("Custom save location configured: '{0}'", fullPath);
                }
            }
        }

        private IEnumerable<string> ResolveDestinationDesignations(CaptureFlowContext context)
        {
            // Priority 1: StepType direct mapping
            if (string.Equals(Config.StepType, WellKnownStepTypes.SaveFile, StringComparison.OrdinalIgnoreCase))
            {
                return new[] { nameof(WellKnownDestinations.FileNoDialog) };
            }
            if (string.Equals(Config.StepType, WellKnownStepTypes.Clipboard, StringComparison.OrdinalIgnoreCase))
            {
                return new[] { nameof(WellKnownDestinations.Clipboard) };
            }
            if (string.Equals(Config.StepType, WellKnownStepTypes.Editor, StringComparison.OrdinalIgnoreCase))
            {
                return new[] { EditorDestination.DESIGNATION };
            }
            if (string.Equals(Config.StepType, WellKnownStepTypes.Printer, StringComparison.OrdinalIgnoreCase))
            {
                return new[] { nameof(WellKnownDestinations.Printer) };
            }
            if (string.Equals(Config.StepType, WellKnownStepTypes.Email, StringComparison.OrdinalIgnoreCase))
            {
                return new[] { nameof(WellKnownDestinations.EMail) };
            }
            if (string.Equals(Config.StepType, WellKnownStepTypes.CustomDestination, StringComparison.OrdinalIgnoreCase))
            {
                string customDest = Config.GetParameter<string>("CustomDestinationId");
                if (!string.IsNullOrWhiteSpace(customDest))
                {
                    return new[] { customDest.Trim() };
                }
            }

            // Priority 2: Context property override (e.g. from trigger or caller)
            if (context != null && context.Properties.TryGetValue("OverrideDestinations", out var ctxVal) && ctxVal != null)
            {
                if (ctxVal is IEnumerable<string> ctxDests) return ctxDests;
                if (ctxVal is string ctxStr && !string.IsNullOrWhiteSpace(ctxStr)) return new[] { ctxStr };
            }

            // Priority 3: Explicit step parameter configuration
            var stepDests = Config.GetParameter<List<string>>("DestinationDesignations");
            if (stepDests != null && stepDests.Count > 0)
            {
                return stepDests;
            }

            // Priority 4: Dynamic user configuration evaluation
            return (IEnumerable<string>)CoreConfig?.OutputDestinations ?? Array.Empty<string>();
        }

        private async Task ExecuteClipboardExportAsync(CaptureFlowContext context, CancellationToken cancellationToken)
        {
            string mode = Config.GetParameter<string>("ClipboardMode") ?? "ImageOnly";
            bool formatText = Config.GetParameter<bool?>("ClipboardFormatText") ?? false;
            bool isTextOnly = string.Equals(mode, "TextOnly", StringComparison.OrdinalIgnoreCase);
            bool isImageAndText = string.Equals(mode, "ImageAndText", StringComparison.OrdinalIgnoreCase) || formatText;
            var clipboard = ClipboardService.For(context.Ui);

            string textToCopy = null;
            if (isTextOnly || isImageAndText)
            {
                textToCopy = await ExtractOrGetOcrTextAsync(context, cancellationToken).ConfigureAwait(false);
                string customPattern = Config.GetParameter<string>("ClipboardCustomText");
                if (!string.IsNullOrWhiteSpace(customPattern))
                {
                    string expandedPattern = customPattern
                        .Replace("${ocr_text}", textToCopy ?? "")
                        .Replace("${payload.text}", textToCopy ?? "");
                    textToCopy = FilenameHelper.FillVariables(expandedPattern, false);
                }
            }

            if (isTextOnly)
            {
                if (!string.IsNullOrWhiteSpace(textToCopy))
                {
                    await clipboard.SetTextAsync(textToCopy, cancellationToken).ConfigureAwait(false);
                    context.LogStep($"Copied {textToCopy.Length} character(s) of OCR text to clipboard.");
                }
                else
                {
                    await clipboard.SetTextAsync("", cancellationToken).ConfigureAwait(false);
                    context.LogStep("Warning: No OCR text detected to place on clipboard.");
                }
                return;
            }

            // Image formats list
            var formats = new List<ClipboardFormat>();
            if (Config.GetParameter<bool?>("ClipboardFormatPNG") ?? true) formats.Add(ClipboardFormat.PNG);
            if (Config.GetParameter<bool?>("ClipboardFormatDIB") ?? true) formats.Add(ClipboardFormat.DIB);
            if (Config.GetParameter<bool?>("ClipboardFormatDIBV5") ?? false) formats.Add(ClipboardFormat.DIBV5);
            if (Config.GetParameter<bool?>("ClipboardFormatBitmap") ?? false) formats.Add(ClipboardFormat.BITMAP);
            if (Config.GetParameter<bool?>("ClipboardFormatHTML") ?? true) formats.Add(ClipboardFormat.HTML);
            if (Config.GetParameter<bool?>("ClipboardFormatHTMLDataUrl") ?? false) formats.Add(ClipboardFormat.HTMLDATAURL);

            if (context.Payload?.EnsureSurface() != null)
            {
                // The export source renders the surface on the UI thread once, the lease is our own copy
                var source = await context.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
                using (var lease = await source.RenderAsync(new SurfaceOutputSettings(OutputFormat.png, 100, false), cancellationToken).ConfigureAwait(false))
                {
                    await clipboard.SetImageAsync(lease.Image, formats, isImageAndText ? textToCopy : null, cancellationToken).ConfigureAwait(false);
                }

                context.LogStep($"Copied capture to clipboard with {formats.Count} format(s)" + (string.IsNullOrEmpty(textToCopy) ? "." : " (including OCR text)."));
            }
        }

        private async Task<string> ExtractOrGetOcrTextAsync(CaptureFlowContext context, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(context.Payload?.ExtractedText))
            {
                return context.Payload.ExtractedText;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails;
            if (captureDetails?.ProcessingTask != null)
            {
                try
                {
                    await captureDetails.ProcessingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warn("Error waiting for background OCR processing in DestinationExportStep", ex);
                }
            }

            if (captureDetails != null)
            {
                lock (captureDetails.Features)
                {
                    var ocrLines = captureDetails.Features.OfType<IOcrLineFeature>().ToList();
                    if (ocrLines.Any())
                    {
                        string txt = string.Join(Environment.NewLine, ocrLines.Select(l => l.Text));
                        context.Payload.ExtractedText = txt;
                        return txt;
                    }
                }
            }

            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
            if (ocrProvider != null)
            {
                var surf = context.Payload?.EnsureSurface();
                if (surf != null)
                {
                    var ocrLines = await ocrProvider.DoOcrAsync(surf).ConfigureAwait(false);
                    if (ocrLines != null && ocrLines.Any())
                    {
                        string txt = string.Join(Environment.NewLine, ocrLines.Select(l => l.Text));
                        context.Payload.ExtractedText = txt;
                        return txt;
                    }
                }
            }

            return "";
        }
    }
}
