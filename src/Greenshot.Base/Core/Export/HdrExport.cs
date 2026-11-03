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

using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using log4net;
using Bitmap = System.Drawing.Bitmap;

namespace Greenshot.Base.Core.Export
{
    /// <summary>
    /// Writes the HDR file of a capture: its HDR image (only the HDR displays, see HdrImage) with the SDR displays and what is drawn on it
    /// (the elements and the mouse cursor, see HdrOverlay) put in at the SDR white level. Used for a JPEG XR file of a capture with HDR content
    /// (the editor's Save, Save as, the file destinations) and for the HdrFormat of SaveFile.
    /// </summary>
    public static class HdrExport
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HdrExport));

        /// <summary>
        /// True when an HDR file can be written of the surface: it has HDR content (HdrImage.HasHdrContent) and its image was changed only in ways
        /// which can be done on the HDR image too (ISurface.HdrState, e.g. a crop, an effect but monochrome). What is drawn on it doesn't matter,
        /// it goes into the HDR file. The HDR image isn't made for it. On the UI thread.
        /// </summary>
        public static bool CanWriteHdr(ISurface surface) => surface?.HdrState == HdrState.Available;

        /// <summary>
        /// Encode the HDR file of the export source. No file when it can't be written: the text (with {0} for the format of the normal file) tells why.
        /// </summary>
        /// <param name="source">The capture</param>
        /// <param name="formatId">The HDR format, see HdrOutput.ResolveFormatId</param>
        /// <param name="includePrivateMetadata">Also the details which are only for the user (a file on this computer)</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public static async Task<(EncodedImage Encoded, string ReasonText)> EncodeAsync(IExportSource source, string formatId, bool includePrivateMetadata,
            CancellationToken cancellationToken)
        {
            var encoder = SimpleServiceProvider.Current.GetInstance<IHdrImageEncoder>(true);
            if (encoder == null || formatId == null || !encoder.CanEncode(formatId))
            {
                Log.Warn($"No encoder can write the HDR format {formatId} on this system.");
                return (null, Texts.Core.HdrNotSavedFailed);
            }

            // On the UI thread, which owns the surface: the SDR displays and the elements come from its image
            var (hdrImage, captureDetails, reasonText) = await source.UseSurfaceAsync(surface =>
            {
                var state = surface.HdrState;
                if (state == HdrState.None)
                {
                    return ((HdrImage)null, surface.CaptureDetails, Texts.Core.HdrNotSavedNoHdr);
                }
                if (state == HdrState.Lost || !(surface.Image is Bitmap sdrImage))
                {
                    return (null, surface.CaptureDetails, Texts.Core.HdrNotSavedChanged);
                }
                // Made now, with the edits of the editor (e.g. a crop): a crop may have left no HDR content
                var surfaceHdrImage = surface.HdrImage;
                if (surfaceHdrImage == null || !surfaceHdrImage.HasHdrContent)
                {
                    return (null, surface.CaptureDetails, Texts.Core.HdrNotSavedNoHdr);
                }
                using var overlay = surface.CreateHdrOverlay();
                return (surfaceHdrImage.Compose(sdrImage, overlay), surface.CaptureDetails, (string)null);
            }, cancellationToken).ConfigureAwait(false);
            if (hdrImage == null)
            {
                Log.Info($"Not writing an HDR file: {(reasonText == Texts.Core.HdrNotSavedNoHdr ? "the capture has no HDR content" : "the capture's image was changed")}.");
                return (null, reasonText);
            }

            using (hdrImage)
            {
                if (!encoder.TryEncode(hdrImage, formatId, captureDetails, includePrivateMetadata, out var encoded, out string error))
                {
                    Log.Warn($"Couldn't write the HDR file: {error}");
                    return (null, Texts.Core.HdrNotSavedFailed);
                }
                return (encoded, null);
            }
        }
    }
}
