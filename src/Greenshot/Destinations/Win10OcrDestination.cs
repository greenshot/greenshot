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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Destinations
{
    /// <summary>
    /// This uses the Windows OcrEngine to perform OCR on the captured image, the text is placed on the clipboard.
    /// </summary>
    public class Win10OcrDestination : DestinationBase
    {
        public override string Designation { get; } = "Windows10OCR";

        /// <summary>
        /// Icon for the OCR function, the icon was found via: https://help4windows.com/windows_8_imageres_dll.shtml
        /// </summary>
        public override DestinationDescriptor Descriptor { get; } =
            new DestinationDescriptor("Windows OCR", 3, DestinationIcons.Exe(System.IO.Path.Combine(System.Environment.SystemDirectory, "imageres.dll"), 97));

        /// <summary>
        /// Run the Windows OCR engine to process the text on the captured image
        /// </summary>
        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            // The lines found with the capture, else the OCR runs now on the rendered capture
            var ocrFeatures = await OcrLines.GetAsync(request.Metadata, request.Source, null, request.Ui, cancellationToken).ConfigureAwait(false);

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
