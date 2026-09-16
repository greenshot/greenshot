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
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Destinations;
using Greenshot.Editor.Destinations;
using Greenshot.Helpers;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step that resolves and dispatches captures to destinations.
    /// Supports individual destination steps (File, Clipboard, Editor, Printer, Email, Custom)
    /// as well as custom storage directories, filename patterns, and clipboard format selections.
    /// </summary>
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
                    bool? reuse = Config.GetParameter<bool?>("ReuseEditor");
                    bool? matchSize = Config.GetParameter<bool?>("MatchSizeToCapture");
                    dest = new EditorDestination(reuse, matchSize);
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

                if (dest != null && dest.IsActive)
                {
                    destinations.Add(dest);
                }
                else if (dest != null && !dest.IsActive)
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
                string fmt = CoreConfig.OutputFileFormat;
                string fmtStr = Config.GetFirstParameter<string>("Format", "ImageFormat");
                if (!string.IsNullOrWhiteSpace(fmtStr))
                {
                    fmt = fmtStr;
                }
                var sos = new SurfaceOutputSettings(fmt, jpegQuality ?? CoreConfig.OutputFileJpegQuality, reduceColors ?? CoreConfig.OutputFileReduceColors);
                context.Properties["Destination.SurfaceOutputSettings"] = sos;
            }

            if (!string.IsNullOrWhiteSpace(customDir))
            {
                string expandedDir = FilenameHelper.FillVariables(customDir, false);
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

                string outputFormat = CoreConfig.OutputFileFormat;
                string formatStr = Config.GetFirstParameter<string>("Format", "ImageFormat");
                if (!string.IsNullOrWhiteSpace(formatStr))
                {
                    outputFormat = formatStr;
                }

                var captureDetails = context.Payload?.RawCapture?.CaptureDetails;
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
            if (string.Equals(Config.StepType, WellKnownStepTypes.SaveFile, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Config.StepType, "SaveToFile", StringComparison.OrdinalIgnoreCase))
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
                string customDest = Config.GetParameter<string>("Destination") ?? Config.GetParameter<string>("CustomDestinationId");
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

            // Priority 3: Explicit step parameter configuration ("Destinations")
            var stepDests = Config.GetParameter<List<string>>("Destinations");
            if (stepDests != null && stepDests.Count > 0)
            {
                return stepDests;
            }

            string singleDest = Config.GetParameter<string>("Destinations");
            if (!string.IsNullOrWhiteSpace(singleDest))
            {
                return singleDest.Contains(",")
                    ? singleDest.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                    : new List<string> { singleDest.Trim() };
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

            string textToCopy = null;
            if (isTextOnly || isImageAndText)
            {
                textToCopy = await ExtractOrGetOcrTextAsync(context).ConfigureAwait(false);
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
                    ClipboardHelper.SetClipboardData(textToCopy);
                    context.LogStep($"Copied {textToCopy.Length} character(s) of OCR text to clipboard.");
                }
                else
                {
                    ClipboardHelper.SetClipboardData("");
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

            var surface = context.Payload?.EnsureSurface();
            if (surface != null)
            {
                ClipboardHelper.SetClipboardData(surface, formats, text: isImageAndText ? textToCopy : null);
                context.LogStep($"Copied capture to clipboard with {formats.Count} format(s)" + (string.IsNullOrEmpty(textToCopy) ? "." : " (including OCR text)."));
            }
        }

        private async Task<string> ExtractOrGetOcrTextAsync(CaptureFlowContext context)
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
                    captureDetails.ProcessingTask.Wait();
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

            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>();
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
