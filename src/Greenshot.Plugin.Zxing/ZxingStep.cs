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
using Greenshot.Base.Recipes;
using log4net;
using ZXing;

namespace Greenshot.Plugin.Zxing
{
    /// <summary>
    /// Capture recipe step that scans barcodes / QR codes on the screenshot surface
    /// and extracts decoded text into the pipeline context and clipboard.
    /// </summary>
    public class ZxingStep : ICaptureStep
    {
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

            string setVariable = NodeConfig.GetParameter<string>("SetVariable")
                ?? NodeConfig.GetParameter<string>("setVariable")
                ?? NodeConfig.GetParameter<string>("Variable")
                ?? NodeConfig.GetParameter<string>("variable");

            bool copyToClipboard = NodeConfig.GetParameter<bool?>("CopyToClipboard")
                ?? NodeConfig.GetParameter<bool?>("copyToClipboard")
                ?? false;

            context.LogStep("Scanning surface for QR codes / barcodes...");
            Log.Info("ZxingStep: Scanning surface for barcodes.");

            var decodedResults = new List<string>();

            // 1. Check existing features if already scanned
            lock (captureDetails.Features)
            {
                var barcodeFeatures = captureDetails.Features.OfType<IBarcodeFeature>().ToList();
                if (barcodeFeatures.Any())
                {
                    decodedResults.AddRange(barcodeFeatures.Select(b => b.RawText).Where(t => !string.IsNullOrEmpty(t)));
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
