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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Imaging;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Native;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// The file format handler for everything the Windows Imaging Component (WIC) of this PC can decode, including the codecs from the Store
    /// (HEIF/AVIF, WebP, Raw, JPEG XL), and JPEG XR for saving
    /// </summary>
    public class WicFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WicFileFormatHandler));

        // WIC lists only .wdp and .jxr for JPEG XR, .hdp and .wmp are older names of the same format
        private static readonly string[] JpegXrExtensions = { ".jxr", ".hdp", ".wdp", ".wmp" };

        // Stable format ids and names for the WIC decoders which are known, other decoders get an id from their MIME type
        private static readonly Dictionary<Guid, (string Id, string DisplayName)> KnownDecoders = new()
        {
            [WicContainerFormats.Raw] = ("raw", "Camera RAW"),
            [WicContainerFormats.Adng] = ("dng", "Digital Negative"),
            [WicContainerFormats.JpegXl] = ("jxl", "JPEG XL"),
            [WicContainerFormats.Heif] = ("heif", "High Efficiency Image File Format"),
            [WicContainerFormats.Webp] = ("webp", "WebP")
        };

        // The formats WIC saves: faster than ImageSharp for JPEG with the same file, TIFF compressed (ImageSharp writes it uncompressed).
        // PNG stays with the PngOptimizer, which ImageSharp runs.
        private static readonly Dictionary<string, Guid> SaveContainers = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jxr"] = WicContainerFormats.Wmp,
            [".jpg"] = WicContainerFormats.Jpeg,
            [".jpeg"] = WicContainerFormats.Jpeg,
            [".bmp"] = WicContainerFormats.Bmp,
            [".gif"] = WicContainerFormats.Gif,
            [".tif"] = WicContainerFormats.Tiff,
            [".tiff"] = WicContainerFormats.Tiff
        };

        // WICTiffCompressionOption: LZW, which every program reads (ZIP is smaller, but not every program reads it)
        private const byte TiffCompressionLzw = 4;

        // EXIF tags WicMetadataPaths has no constant for
        private const ushort ExifTagCopyright = 33432;
        private const ushort ExifTagOffsetTimeOriginal = 36881;
        private const ushort ExifTagXpTitle = 0x9C9B;
        private const ushort ExifTagXpComment = 0x9C9C;

        private readonly IReadOnlyCollection<WicDecoder> _wicDecoders;
        private IReadOnlyCollection<string> LoadExtensions { get; } = JpegXrExtensions.Concat(new[] { ".dds" }).ToArray();
        private IReadOnlyCollection<string> SaveExtensions { get; } = SaveContainers.Keys.ToArray();

        public WicFileFormatHandler()
        {
            _wicDecoders = RetrieveDecoders();
            LoadExtensions = LoadExtensions.Concat(_wicDecoders.SelectMany(decoder => decoder.Extensions)).OrderBy(e => e).Distinct().ToArray();

            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = LoadExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = LoadExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = SaveExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToFile] = SaveExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromFile] = LoadExtensions;
        }

        /// <summary>
        /// JPEG XR and DDS, and every format of a WIC decoder which no other format has registered yet (the built-in decoders of Windows overlap with the core formats).
        /// The decoders are those WIC has on this PC, a decoder which can't be registered is logged and skipped.
        /// </summary>
        public override void RegisterFileFormats(IFileFormatRegistry registry)
        {
            RegisterFileFormat(registry, WellKnownFileFormats.Jxr, JpegXrExtensions, new[] { ".jxr" }, "jxr", "image/vnd.ms-photo", null, "JPEG XR");
            RegisterFileFormat(registry, "dds", new[] { ".dds" }, Array.Empty<string>(), "dds", "image/vnd.ms-dds", null, "DirectDraw Surface");

            foreach (var decoder in _wicDecoders)
            {
                var extensions = decoder.Extensions.Where(extension => registry.GetByExtension(extension) == null).ToList();
                var mimeTypes = decoder.MimeTypes.Where(mimeType => registry.GetByMimeType(mimeType) == null).ToList();
                if (extensions.Count == 0 || mimeTypes.Count == 0)
                {
                    continue;
                }

                var (id, displayName) = KnownDecoders.TryGetValue(decoder.ContainerFormat, out var known) ? known : (IdFromMimeType(mimeTypes[0]), decoder.Name);
                if (registry.TryGet(id, out _))
                {
                    continue;
                }

                try
                {
                    RegisterFileFormat(registry, id, extensions, Array.Empty<string>(), extensions[0], mimeTypes[0], mimeTypes.Skip(1), string.IsNullOrWhiteSpace(displayName) ? id : displayName);
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
                {
                    Log.WarnFormat("Couldn't register the formats of the WIC decoder {0} ({1}): {2}", decoder.Name, decoder.ContainerFormat, ex.Message);
                }
            }
        }

        /// <summary>
        /// image/x-portable-anymap gives portable-anymap, image/vnd.ms-dds gives ms-dds
        /// </summary>
        private static string IdFromMimeType(string mimeType)
        {
            string subtype = mimeType.Substring(mimeType.IndexOf('/') + 1);
            foreach (var prefix in new[] { "x-", "vnd." })
            {
                if (subtype.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    subtype = subtype.Substring(prefix.Length);
                }
            }
            return subtype.ToLowerInvariant();
        }

        /// <summary>
        /// A WIC decoder of this PC, with the extensions and MIME types in lower case
        /// </summary>
        private sealed class WicDecoder
        {
            public WicDecoder(WicCodecInfo codec)
            {
                ContainerFormat = codec.ContainerFormat;
                Name = codec.FriendlyName;
                MimeTypes = Normalize(codec.MimeTypes);
                Extensions = Normalize(codec.FileExtensions).Where(extension => extension.StartsWith(".")).ToArray();
            }

            public Guid ContainerFormat { get; }
            public string Name { get; }
            public IReadOnlyList<string> MimeTypes { get; }
            public IReadOnlyList<string> Extensions { get; }

            private static string[] Normalize(IEnumerable<string> values) =>
                (values ?? Enumerable.Empty<string>()).Select(value => value.Trim().ToLowerInvariant()).Where(value => value.Length > 0).Distinct().ToArray();
        }

        /// <summary>
        /// The decoders WIC has on this PC, the registry doesn't list all of them (e.g. not the built-in ones on Windows 11 26300)
        /// </summary>
        private static IReadOnlyCollection<WicDecoder> RetrieveDecoders()
        {
            try
            {
                return WicCodecs.GetDecoders().Select(codec => new WicDecoder(codec)).ToList();
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't enumerate the WIC decoders.", ex);
                return Array.Empty<WicDecoder>();
            }
        }

        /// <summary>
        /// On Windows WIC saves its formats first, ImageSharp is the fallback when WIC can't
        /// </summary>
        public override int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension) =>
            fileFormatHandlerAction is FileFormatHandlerActions.SaveToStream or FileFormatHandlerActions.SaveToFile ? PreferredSavePriority : 0;

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            surfaceOutputSettings ??= new SurfaceOutputSettings();
            if (extension == null || !SaveContainers.TryGetValue(extension, out var container))
            {
                return false;
            }
            // A bitmap with a palette (reduced colors, a GIF) is written with its palette, JPEG has none
            bool keepPalette = container != WicContainerFormats.Jpeg && container != WicContainerFormats.Wmp;
            Guid pixelFormat = bitmap.PixelFormat switch
            {
                PixelFormat.Format32bppArgb => WicPixelFormats.Format32bppBgra,
                PixelFormat.Format32bppPArgb => WicPixelFormats.Format32bppPbgra,
                PixelFormat.Format32bppRgb => WicPixelFormats.Format32bppBgr,
                PixelFormat.Format24bppRgb => WicPixelFormats.Format24bppBgr,
                PixelFormat.Format8bppIndexed when keepPalette => WicPixelFormats.Format8bppIndexed,
                PixelFormat.Format4bppIndexed when keepPalette => WicPixelFormats.Format4bppIndexed,
                PixelFormat.Format1bppIndexed when keepPalette => WicPixelFormats.Format1bppIndexed,
                _ => Guid.Empty
            };
            // Other formats are written as 32 bit
            using var converted = pixelFormat == Guid.Empty ? ImageHelper.Clone(bitmap, PixelFormat.Format32bppArgb) as Bitmap : null;
            var source = converted ?? bitmap;
            if (converted != null)
            {
                pixelFormat = WicPixelFormats.Format32bppBgra;
            }

            var options = new WicEncodeOptions
            {
                ContainerFormat = container,
                DpiX = source.HorizontalResolution,
                DpiY = source.VerticalResolution
            };
            if ((source.PixelFormat & PixelFormat.Indexed) != 0)
            {
                // The indexes of the pixels refer to this palette, a color with alpha 0 is the transparent one (GIF)
                options.Palette = source.Palette.Entries.Select(color => unchecked((uint)color.ToArgb())).ToArray();
            }
            if (container == WicContainerFormats.Jpeg || container == WicContainerFormats.Wmp)
            {
                options.ImageQuality = surfaceOutputSettings.JPGQuality / 100f;
            }
            if (container == WicContainerFormats.Tiff)
            {
                options.EncoderOptions["TiffCompressionMethod"] = TiffCompressionLzw;
            }
            if (container != WicContainerFormats.Bmp)
            {
                // Like the other handlers: the file says it was written by Greenshot, plus the details the user wants; what an encoder doesn't take is skipped
                var metadata = ExportImageMetadata.ForExport(surface?.CaptureDetails, surfaceOutputSettings.IncludePrivateMetadata);
                options.Metadata = writer => WriteMetadata(writer, container, metadata);
            }

            // Encoded into a buffer first: when WIC fails, the next handler writes to an untouched destination
            using var buffer = new MemoryStream();
            var bitmapData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
            try
            {
                WicImage.Encode(buffer, options, bitmapData.Scan0, source.Width, source.Height, bitmapData.Stride, pixelFormat);
            }
            catch (COMException ex)
            {
                Log.Warn($"Couldn't save the {extension} image with WIC: {ex.Message}");
                return false;
            }
            finally
            {
                source.UnlockBits(bitmapData);
            }

            buffer.WriteTo(destination);
            return true;
        }

        /// <summary>
        /// Write the details as EXIF tags, which Windows and other programs read from JPEG, TIFF and JPEG XR, and as XMP packet with what EXIF has no tag for (web address, barcodes).
        /// A path the container doesn't have is null and skipped.
        /// </summary>
        internal static void WriteMetadata(WicMetadataWriter writer, Guid container, ExportImageMetadata metadata)
        {
            SetAnsi(writer, WicMetadataPaths.Software(container), metadata.Software);
            if (metadata.CaptureTime.HasValue)
            {
                SetAnsi(writer, WicMetadataPaths.DateTimeOriginal(container), metadata.ExifCaptureTime);
                SetAnsi(writer, WicMetadataPaths.ExifSubTag(container, ExifTagOffsetTimeOriginal), metadata.ExifCaptureTimeOffset);
            }
            SetAnsi(writer, WicMetadataPaths.ExifTag(container, WicMetadataPaths.TagArtist), metadata.Author);
            SetAnsi(writer, WicMetadataPaths.ExifTag(container, ExifTagCopyright), metadata.Copyright);
            SetUcs2(writer, WicMetadataPaths.ExifTag(container, ExifTagXpTitle), metadata.Title);
            SetUcs2(writer, WicMetadataPaths.ExifTag(container, ExifTagXpComment), metadata.Description);
            string xmpPath = metadata.HasDetails ? WicMetadataPaths.Xmp(container) : null;
            if (!string.IsNullOrEmpty(xmpPath))
            {
                writer.AddRawBlock(new WicMetadataRawBlock(xmpPath, WicMetadataFormats.Xmp, metadata.ToXmpPacket()));
            }
        }

        /// <summary>
        /// The Windows EXIF tags (XPTitle, XPComment) are UTF-16 text as vector of bytes
        /// </summary>
        private static void SetUcs2(WicMetadataWriter writer, string path, string value)
        {
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(value))
            {
                writer.Set(path, WicMetadataValue.ByteVector(ExportImageMetadata.ToUcs2(value)));
            }
        }

        /// <summary>
        /// EXIF ASCII tags need a VT_LPSTR value
        /// </summary>
        private static void SetAnsi(WicMetadataWriter writer, string path, string value)
        {
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(value))
            {
                writer.Set(path, WicMetadataValue.AnsiString(value));
            }
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            // An HDR file (half or float pixels) is tone mapped like an HDR capture: WIC's conversion to 8 bit would clip everything above 80 nits
            var hdrDecoder = SimpleServiceProvider.Current.GetInstance<IHdrImageDecoder>(true);
            if (hdrDecoder != null && hdrDecoder.TryDecode(stream, out var hdrImage))
            {
                using (hdrImage)
                {
                    bitmap = HdrImageToneMapping.ToBitmap(hdrImage);
                    return true;
                }
            }

            Bitmap result = null;
            BitmapData bitmapData = null;
            try
            {
                // WIC writes the pixels straight into the bitmap, converted to 32 bit BGRA (the layout of Format32bppArgb) and rotated by the EXIF orientation
                var frameInfo = WicImage.Decode(stream, WicPixelFormats.Format32bppBgra, (WicFrameInfo info, out int stride) =>
                {
                    result = new Bitmap(info.Width, info.Height, PixelFormat.Format32bppArgb);
                    bitmapData = result.LockBits(new Rectangle(0, 0, info.Width, info.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    stride = bitmapData.Stride;
                    return bitmapData.Scan0;
                }, ApplyExifOrientation);
                result.UnlockBits(bitmapData);
                bitmapData = null;
                // Only a resolution the file stores: without one WIC reports the default of the decoder (72 for WebP, 96 for PNG)
                if (frameInfo.HasResolution && frameInfo.DpiX > 0 && frameInfo.DpiY > 0)
                {
                    result.SetResolution((float)frameInfo.DpiX, (float)frameInfo.DpiY);
                }
                else
                {
                    result.SetResolution(96, 96);
                }

                bitmap = result;
                return true;
            }
            catch (COMException ex)
            {
                // Also WicCodecNotFoundException: no codec for the stream, or a codec which can't be initialized
                Log.Warn($"Couldn't load the {extension} image: {ex.Message}");
            }
            finally
            {
                if (bitmapData != null)
                {
                    result.UnlockBits(bitmapData);
                }
            }

            result?.Dispose();
            bitmap = null;
            return false;
        }
    }
}
