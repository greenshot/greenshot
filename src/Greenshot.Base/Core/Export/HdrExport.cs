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
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Core.Export
{
    /// <summary>
    /// Writes the HDR file of a capture: its HDR image (see HdrImage) with what is drawn on it
    /// (the elements and the mouse cursor, see HdrOverlay). Used for a JPEG XR file of a capture with HDR content
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

            // The edits of the editor (e.g. a resize) are done on the thread pool, not on the UI thread
            // Once taken, the preparation holds a share of the HDR image which only running it releases: not cancelled while it is taken
            cancellationToken.ThrowIfCancellationRequested();
            var preparation = await source.UseSurfaceAsync(surface => surface.HdrState == HdrState.Available ? surface.HdrImagePreparation : null, CancellationToken.None)
                .ConfigureAwait(false);
            if (preparation != null)
            {
                await ThreadPoolSwitch.SwitchToThreadPoolAsync();
                preparation();
            }

            // On the UI thread, which owns the surface: what is needed of it is taken (a share of the HDR image, the elements as an overlay), so the
            // HDR file is put together and written on the thread pool while the editor goes on
            var (surfaceHdrImage, overlay, captureDetails, reasonText) = await source.UseSurfaceAsync(surface =>
            {
                var state = surface.HdrState;
                if (state == HdrState.None)
                {
                    return ((HdrImage)null, (HdrOverlay)null, surface.CaptureDetails, Texts.Core.HdrNotSavedNoHdr);
                }
                if (state == HdrState.Lost)
                {
                    return (null, null, surface.CaptureDetails, Texts.Core.HdrNotSavedChanged);
                }
                // Made now, with the edits of the editor (e.g. a crop): a crop may have left no HDR content
                var hdrImageNow = surface.HdrImage;
                if (hdrImageNow == null || !hdrImageNow.HasHdrContent)
                {
                    return (null, null, surface.CaptureDetails, Texts.Core.HdrNotSavedNoHdr);
                }
                var shared = hdrImageNow.Share();
                try
                {
                    return (shared, surface.CreateHdrOverlay(), surface.CaptureDetails, (string)null);
                }
                catch
                {
                    shared.Dispose();
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
            if (surfaceHdrImage == null)
            {
                Log.Info($"Not writing an HDR file: {(reasonText == Texts.Core.HdrNotSavedNoHdr ? "the capture has no HDR content" : "the capture's image was changed")}.");
                return (null, reasonText);
            }

            HdrImage hdrImage;
            using (surfaceHdrImage)
            using (overlay)
            {
                await ThreadPoolSwitch.SwitchToThreadPoolAsync();
                hdrImage = surfaceHdrImage.Compose(overlay);
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
