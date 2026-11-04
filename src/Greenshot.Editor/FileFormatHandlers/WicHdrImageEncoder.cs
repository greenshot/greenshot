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

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Imaging;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Hdr;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// Writes HDR image with the WIC encoder of Windows: JPEG XR (WMPhoto, part of Windows).
    /// It takes the half-float pixels of Windows.Graphics.Capture as they are and writes them lossless.
    /// </summary>
    public sealed class WicHdrImageEncoder : IHdrImageEncoder
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WicHdrImageEncoder));

        /// <inheritdoc />
        public bool CanEncode(string formatId)
        {
            var container = ContainerFor(formatId);
            if (container == Guid.Empty)
            {
                return false;
            }
            try
            {
                return WicCodecs.GetEncoders().Any(codec => codec.ContainerFormat == container);
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't list the WIC encoders: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryEncode(HdrImage hdrImage, string formatId, ICaptureDetails captureDetails, bool includePrivateMetadata, out EncodedImage encoded, out string error)
        {
            encoded = null;
            error = null;
            if (hdrImage == null) throw new ArgumentNullException(nameof(hdrImage));
            var container = ContainerFor(formatId);
            if (container == Guid.Empty)
            {
                error = $"'{formatId}' is no HDR format.";
                return false;
            }

            var metadata = ExportImageMetadata.ForExport(captureDetails, includePrivateMetadata);
            // So the file is tone mapped the same way when it is opened again (see WicHdrImageDecoder)
            metadata.HdrSdrWhiteLevelInNits = hdrImage.SdrWhiteLevelInNits;
            metadata.HdrDisplayPeakInNits = hdrImage.DisplayPeakInNits > 0 ? hdrImage.DisplayPeakInNits : (float?)null;
            var options = new WicEncodeOptions
            {
                ContainerFormat = container,
                // A screenshot: every pixel matters (text, edges)
                Lossless = true,
                DpiX = captureDetails?.DpiX > 0 ? captureDetails.DpiX : 96,
                DpiY = captureDetails?.DpiY > 0 ? captureDetails.DpiY : 96,
                Metadata = writer => WicFileFormatHandler.WriteMetadata(writer, container, metadata)
            };

            // The encoded image is the buffer of the stream, not a copy of it
            var buffer = new MemoryStream();
            Guid writtenFormat = Guid.Empty;
            try
            {
                // HalfVector4 is 64bppRGBAHalf bit for bit
                hdrImage.UsePixelMemory(first =>
                    writtenFormat = WicImage.Encode(buffer, options, first, hdrImage.Width, hdrImage.Height, hdrImage.Stride, WicPixelFormats.Format64bppRgbaHalf).PixelFormat);
            }
            catch (COMException ex)
            {
                error = $"Couldn't write the {formatId} file with WIC: {ex.Message}";
                return false;
            }

            // WIC converts to what an encoder takes: an 8-bit file would silently lose the HDR highlights
            if (writtenFormat != WicPixelFormats.Format64bppRgbaHalf && writtenFormat != WicPixelFormats.Format128bppRgbaFloat)
            {
                error = $"The {formatId} encoder doesn't take HDR pixels (it wrote {writtenFormat}).";
                return false;
            }

            Log.Info($"Wrote an HDR {formatId} file of {hdrImage.Width}x{hdrImage.Height} (SDR white {hdrImage.SdrWhiteLevelInNits} nits), {buffer.Length:N0} bytes.");
            encoded = new EncodedImage(buffer.GetBuffer(), (int)buffer.Length, formatId);
            return true;
        }

        private static Guid ContainerFor(string formatId)
        {
            if (string.Equals(formatId, WellKnownFileFormats.Jxr, StringComparison.OrdinalIgnoreCase))
            {
                return WicContainerFormats.Wmp;
            }
            return Guid.Empty;
        }
    }
}
