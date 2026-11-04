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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Imaging;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Hdr;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Native;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// Reads HDR image files with the WIC decoders of Windows (JPEG XR, and every other format with half or float pixels) as an HDR image,
    /// lossless as half-floats. The SDR white level is the one Greenshot wrote into the file (XMP), otherwise that of the current HDR display.
    /// </summary>
    public sealed class WicHdrImageDecoder : IHdrImageDecoder
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WicHdrImageDecoder));

        // The WIC pixel formats with half, float or fixed point (more than 8 bits, linear scRGB) samples, GUID_WICPixelFormat...
        private static readonly HashSet<Guid> HdrPixelFormats = new HashSet<Guid>
        {
            WicPixelFormats.Format64bppRgbaHalf,
            WicPixelFormats.Format128bppRgbaFloat,
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc942"), // 64bppRGBHalf
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc93b"), // 48bppRGBHalf
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc91a"), // 128bppPRGBAFloat
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc91b"), // 128bppRGBFloat
            new Guid("e3fed78f-e8db-4acf-84c1-e97f6136b327"), // 96bppRGBFloat
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc93d"), // 32bppRGBE
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc91d"), // 64bppRGBAFixedPoint
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc940"), // 64bppRGBFixedPoint
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc912"), // 48bppRGBFixedPoint
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc91e"), // 128bppRGBAFixedPoint
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc941"), // 128bppRGBFixedPoint
        };

        /// <inheritdoc />
        public bool TryDecode(Stream stream, out HdrImage hdrImage)
        {
            hdrImage = null;
            if (stream == null || !stream.CanSeek)
            {
                return false;
            }
            long start = stream.Position;
            try
            {
                var frameInfo = WicImage.GetFrameInfo(stream, true);
                if (!HdrPixelFormats.Contains(frameInfo.PixelFormat))
                {
                    return false;
                }

                var (sdrWhiteLevel, displayPeak) = ReadHdrLevels(frameInfo.Metadata);
                if (sdrWhiteLevel == null)
                {
                    // Not written by Greenshot: as it would look on this HDR display
                    var current = HdrImageToneMapping.CurrentHdrDisplayLevels();
                    sdrWhiteLevel = current.SdrWhiteLevelInNits;
                    displayPeak ??= current.DisplayPeakInNits;
                }

                var image = new HdrImage(frameInfo.Width, frameInfo.Height, sdrWhiteLevel.Value, displayPeak ?? 0);
                try
                {
                    stream.Position = start;
                    image.FillPixelMemory(first => WicImage.Decode(stream, WicPixelFormats.Format64bppRgbaHalf, first, image.Stride, true));
                }
                catch
                {
                    image.Dispose();
                    throw;
                }
                Log.Info($"Read an HDR image of {image.Width}x{image.Height} (SDR white {image.SdrWhiteLevelInNits} nits).");
                hdrImage = image;
                return true;
            }
            catch (Exception ex) when (ex is COMException || ex is IOException || ex is InvalidOperationException || ex is ArgumentException)
            {
                // Not readable as HDR image: the normal loading tries it
                Log.Warn($"Couldn't read the HDR image: {ex.Message}");
                stream.Position = start;
                return false;
            }
        }

        /// <summary>
        /// The SDR white level and peak Greenshot wrote into the XMP of the file
        /// </summary>
        private static (float? SdrWhiteLevelInNits, float? DisplayPeakInNits) ReadHdrLevels(WicMetadata metadata)
        {
            foreach (var block in metadata?.RawBlocks ?? Enumerable.Empty<WicMetadataRawBlock>())
            {
                if (block.Format != WicMetadataFormats.Xmp)
                {
                    continue;
                }
                var levels = ExportImageMetadata.ReadHdrLevels(block.Data);
                if (levels.SdrWhiteLevelInNits.HasValue)
                {
                    return levels;
                }
            }
            return (null, null);
        }
    }
}
