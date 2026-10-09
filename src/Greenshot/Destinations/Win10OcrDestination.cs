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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Greenshot.Base.Core.FileFormat;

namespace Greenshot.Destinations
{
    /// <summary>
    /// This uses the Windows OcrEngine to perform OCR on the captured image, the text is placed on the clipboard.
    /// </summary>
    public class Win10OcrDestination : DestinationBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10OcrDestination));

        public override string Designation { get; } = "Windows10OCR";

        /// <summary>
        /// Icon for the OCR function, the icon was found via: https://help4windows.com/windows_8_imageres_dll.shtml
        /// </summary>
        public override DestinationDescriptor Descriptor { get; } =
            new DestinationDescriptor("Windows OCR", 3, DestinationIcons.Exe(FilenameHelper.FillCmdVariables(@"%windir%\system32\imageres.dll"), 97));

        /// <summary>
        /// Run the Windows OCR engine to process the text on the captured image
        /// </summary>
        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var captureDetails = request.Metadata;
            // Background processing (OCR started with the capture) is awaited, not blocked on
            if (captureDetails?.ProcessingTask != null)
            {
                try
                {
                    await captureDetails.ProcessingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (System.OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (System.Exception ex)
                {
                    Log.Error("Error waiting for background OCR processing in destination", ex);
                }
            }

            List<IOcrLineFeature> ocrFeatures = new List<IOcrLineFeature>();
            if (captureDetails != null)
            {
                lock (captureDetails.Features)
                {
                    ocrFeatures = captureDetails.Features.OfType<IOcrLineFeature>().ToList();
                }
            }

            if (!ocrFeatures.Any())
            {
                var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
                if (ocrProvider != null)
                {
                    using var lease = await request.Source.RenderAsync(new SurfaceOutputSettings(WellKnownFileFormats.Png, 100, false) { DisableReduceColors = true }, cancellationToken).ConfigureAwait(false);
                    var ocrLines = await ocrProvider.DoOcrAsync(lease.Image).ConfigureAwait(false);
                    if (ocrLines != null && ocrLines.Any())
                    {
                        if (captureDetails != null)
                        {
                            lock (captureDetails.Features)
                            {
                                captureDetails.Features.AddRange(ocrLines);
                            }
                        }

                        ocrFeatures = ocrLines;
                    }
                }
            }

            // Check if we found text
            if (ocrFeatures.Any())
            {
                var fullText = string.Join(System.Environment.NewLine, ocrFeatures.Select(line => line.Text));
                if (!string.IsNullOrWhiteSpace(fullText))
                {
                    // Place the OCR text on the Clipboard
                    await ClipboardService.Current.SetTextAsync(fullText, cancellationToken).ConfigureAwait(false);
                }
            }

            return ExportResult.Succeeded();
        }
    }
}
