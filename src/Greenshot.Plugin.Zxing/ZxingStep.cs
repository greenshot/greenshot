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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using log4net;
using ZXing;

namespace Greenshot.Plugin.Zxing
{
    /// <summary>
    /// Capture recipe step that scans barcodes / QR codes on the screenshot surface
    /// and extracts decoded text into the pipeline context and clipboard.
    /// </summary>
    [StepInfo(ZxingStep.StepType, "Barcode Scanner (ZXing)", "Scans and decodes 1D/2D barcodes (such as QR codes) from the capture bitmap.", "Analysis")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required, ExtractedText = PayloadRequirement.Created)]
    [StepParameter("SetVariable", ContractDataType.String, Description = "Also store the decoded text in this variable", SupportsExpressions = false)]
    [StepParameter("CopyToClipboard", ContractDataType.Boolean, DefaultValue = false, Description = "Copy the decoded text to the clipboard")]
    [StepOutputVariable("Barcode.Text", ContractDataType.String, "Decoded text of the detected barcode(s), one per line", Conditional = true)]
    [StepOutputVariable("Barcode.Format", ContractDataType.String, "Format of the detected barcode(s), e.g. QR_CODE, one per line", Conditional = true)]
    [StepOutputVariable("Zxing.DecodedText", ContractDataType.String, "Same as Barcode.Text", Conditional = true)]
    [StepOutputVariable("{Parameter:SetVariable}", ContractDataType.String, "The decoded text, when SetVariable is set", Conditional = true)]
    public class ZxingStep : ICaptureStep
    {
        /// <summary>The step type recipes use for this step.</summary>
        public const string StepType = "BarcodeScan";

        private static readonly ILog Log = LogManager.GetLogger(typeof(ZxingStep));

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public ZxingStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ZxingBarcodeStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var surface = context.Payload?.EnsureSurface();
            if (surface == null)
            {
                context.LogStep("ZxingStep: No surface available to scan.");
                Log.Warn("ZxingStep: Surface is null in context payload.");
                return Task.CompletedTask;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            string setVariable = NodeConfig.GetParameter<string>("SetVariable");
            bool copyToClipboard = NodeConfig.GetParameter<bool?>("CopyToClipboard") ?? false;

            context.LogStep("Scanning surface for QR codes / barcodes...");
            Log.Info("ZxingStep: Scanning surface for barcodes.");

            var decodedResults = new List<string>();
            var decodedFormats = new List<string>();

            // 1. Check existing features if already scanned
            lock (captureDetails.Features)
            {
                foreach (var barcode in captureDetails.Features.OfType<IBarcodeFeature>().Where(b => !string.IsNullOrEmpty(b.RawText)))
                {
                    decodedResults.Add(barcode.RawText);
                    decodedFormats.Add(barcode.Format ?? string.Empty);
                }
            }

            // 2. Scan surface image directly with ZXing BarcodeReader
            if (decodedResults.Count == 0)
            {
                try
                {
                    using (var image = surface.GetImageForExport())
                    using (var bmp = image is Bitmap b ? (Bitmap)b.Clone() : new Bitmap(image))
                    {
                        var reader = new BarcodeReader
                        {
                            AutoRotate = true,
                            Options = new ZXing.Common.DecodingOptions
                            {
                                TryHarder = true,
                                TryInverted = true
                            }
                        };

                        var results = reader.DecodeMultiple(bmp);
                        if (results != null)
                        {
                            foreach (var res in results)
                            {
                                if (!string.IsNullOrEmpty(res?.Text))
                                {
                                    decodedResults.Add(res.Text);
                                    decodedFormats.Add(res.BarcodeFormat.ToString());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("ZxingStep: Error scanning barcode from bitmap", ex);
                }
            }

            if (decodedResults.Count > 0)
            {
                string combinedText = string.Join(Environment.NewLine, decodedResults);
                context.Payload.ExtractedText = combinedText;
                context.Properties["Zxing.DecodedText"] = combinedText;
                context.Properties["Barcode.Text"] = combinedText;
                context.Properties["Barcode.Format"] = string.Join(Environment.NewLine, decodedFormats);

                if (!string.IsNullOrEmpty(setVariable))
                {
                    context.Properties[setVariable] = combinedText;
                }

                if (copyToClipboard)
                {
                    ClipboardHelper.SetClipboardData(combinedText);
                    context.LogStep($"Copied {decodedResults.Count} decoded barcode(s) to clipboard.");
                }

                context.LogStep($"Detected {decodedResults.Count} barcode(s)/QR code(s): {combinedText}");
                Log.InfoFormat("ZxingStep: Detected {0} barcode(s).", decodedResults.Count);
            }
            else
            {
                context.LogStep("ZxingStep: No QR codes or barcodes found on surface.");
                Log.Info("ZxingStep: No barcodes found.");
            }

            return Task.CompletedTask;
        }
    }
}
