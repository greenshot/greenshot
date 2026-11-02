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

        private readonly IReadOnlyCollection<WicDecoder> _wicDecoders;
        private IReadOnlyCollection<string> LoadExtensions { get; } = JpegXrExtensions.Concat(new[] { ".dds" }).ToArray();
        private IReadOnlyCollection<string> SaveExtensions { get; } = new[] { ".jxr" };

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
            RegisterFileFormat(registry, WellKnownFileFormats.Jxr, JpegXrExtensions, SaveExtensions, "jxr", "image/vnd.ms-photo", null, "JPEG XR");
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

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            surfaceOutputSettings ??= new SurfaceOutputSettings();
            Guid pixelFormat = bitmap.PixelFormat switch
            {
                PixelFormat.Format32bppArgb => WicPixelFormats.Format32bppBgra,
                PixelFormat.Format32bppPArgb => WicPixelFormats.Format32bppPbgra,
                PixelFormat.Format32bppRgb => WicPixelFormats.Format32bppBgr,
                PixelFormat.Format24bppRgb => WicPixelFormats.Format24bppBgr,
                _ => Guid.Empty
            };
            // Indexed and other formats are written as 32 bit
            using var converted = pixelFormat == Guid.Empty ? ImageHelper.Clone(bitmap, PixelFormat.Format32bppArgb) as Bitmap : null;
            var source = converted ?? bitmap;
            if (converted != null)
            {
                pixelFormat = WicPixelFormats.Format32bppBgra;
            }

            var options = new WicEncodeOptions
            {
                ContainerFormat = WicContainerFormats.Wmp,
                ImageQuality = surfaceOutputSettings.JPGQuality / 100f,
                DpiX = source.HorizontalResolution,
                DpiY = source.VerticalResolution
            };
            var bitmapData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
            try
            {
                WicImage.Encode(destination, options, bitmapData.Scan0, source.Width, source.Height, bitmapData.Stride, pixelFormat);
                return true;
            }
            catch (COMException ex)
            {
                Log.Error("Couldn't save image as JPEG XR: ", ex);
                return false;
            }
            finally
            {
                source.UnlockBits(bitmapData);
            }
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
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
                if (frameInfo.DpiX > 0 && frameInfo.DpiY > 0)
                {
                    result.SetResolution((float)frameInfo.DpiX, (float)frameInfo.DpiY);
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
