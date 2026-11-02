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
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces.Plugin;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This is the System.Windows.Media.Imaging (WPF) file format handler, which uses WIC
    /// </summary>
    public class WpfFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WpfFileFormatHandler));
        private const string HeifDecoder = "{E9A4A80A-44FE-4DE4-8971-7150B10A5199}";
        private const string WicDecoderCategory = "{7ED96837-96F0-4812-B211-F13C24117ED3}";

        private const string RawDecoder = "{41945702-8302-44A6-9445-AC98E8AFA086}";
        private const string JpegXlDecoder = "{FC6CEECE-AEF5-4A23-96EC-5984FFB486D9}";

        private static readonly string[] JpegXrExtensions = { ".jxr", ".hdp", ".wdp", ".wmp" };

        // Stable format ids and names for the WIC decoders which are known, other decoders get an id from their MIME type
        private static readonly Dictionary<string, (string Id, string DisplayName)> KnownDecoders = new(StringComparer.OrdinalIgnoreCase)
        {
            [RawDecoder] = ("raw", "Camera RAW"),
            [JpegXlDecoder] = ("jxl", "JPEG XL"),
            [HeifDecoder] = ("heif", "High Efficiency Image File Format")
        };

        private readonly IReadOnlyCollection<WicDecoder> _wicDecoders;
        private IReadOnlyCollection<string> LoadExtensions { get; } = JpegXrExtensions.Concat(new[] { ".dds" }).ToArray();
        private IReadOnlyCollection<string> SaveExtensions { get; } = new[] { ".jxr" };

        public WpfFileFormatHandler()
        {
            _wicDecoders = RetrieveDecoders().ToList();
            LoadExtensions = LoadExtensions.Concat(_wicDecoders.SelectMany(decoder => decoder.Extensions)).OrderBy(e => e).Distinct().ToArray();

            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = LoadExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = LoadExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = SaveExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToFile] = SaveExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromFile] = LoadExtensions;
        }

        /// <summary>
        /// JPEG XR and DDS, and every format of a WIC decoder which no other format has registered yet (the built-in decoders of Windows overlap with the core formats).
        /// The decoders come from the registry of this PC, a decoder which can't be registered is logged and skipped.
        /// </summary>
        public override void RegisterFileFormats(IFileFormatRegistry registry)
        {
            RegisterFileFormat(registry, WellKnownFileFormats.Jxr, JpegXrExtensions, SaveExtensions, "jxr", "image/vnd.ms-photo", null, "JPEG XR");
            RegisterFileFormat(registry, "dds", new[] { ".dds" }, Array.Empty<string>(), "dds", "image/vnd-ms.dds", null, "DirectDraw Surface");

            foreach (var decoder in _wicDecoders)
            {
                var extensions = decoder.Extensions.Where(extension => registry.GetByExtension(extension) == null).ToList();
                var mimeTypes = decoder.MimeTypes.Where(mimeType => registry.GetByMimeType(mimeType) == null).ToList();
                if (extensions.Count == 0 || mimeTypes.Count == 0)
                {
                    continue;
                }

                var (id, displayName) = KnownDecoders.TryGetValue(decoder.Clsid, out var known) ? known : (IdFromMimeType(mimeTypes[0]), decoder.Name);
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
                    Log.WarnFormat("Couldn't register the formats of the WIC decoder {0} ({1}): {2}", decoder.Name, decoder.Clsid, ex.Message);
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
        /// A WIC decoder as it is registered on this PC
        /// </summary>
        private sealed class WicDecoder
        {
            public WicDecoder(string clsid, string name, IEnumerable<string> mimeTypes, IEnumerable<string> extensions)
            {
                Clsid = clsid;
                Name = name;
                MimeTypes = SplitList(mimeTypes);
                Extensions = SplitList(extensions).Where(extension => extension.StartsWith(".")).ToArray();
            }

            public string Clsid { get; }
            public string Name { get; }
            public IReadOnlyList<string> MimeTypes { get; }
            public IReadOnlyList<string> Extensions { get; }

            private static string[] SplitList(IEnumerable<string> values) =>
                values.Select(value => value.Trim().ToLowerInvariant()).Where(value => value.Length > 0).Distinct().ToArray();
        }

        /// <summary>
        /// Detect all the decoders WIC has
        /// </summary>
        /// <returns>IEnumerable{WicDecoder}</returns>
        private static IEnumerable<WicDecoder> RetrieveDecoders()
        {
            string baseKeyPath;
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                baseKeyPath = "Wow6432Node\\CLSID";
            }
            else
            {
                baseKeyPath = "CLSID";
            }

            using RegistryKey baseKey = Registry.ClassesRoot.OpenSubKey(baseKeyPath, false);
            if (baseKey == null) yield break;

            var wicDecoderCategoryPath = Path.Combine(baseKeyPath, WicDecoderCategory, "instance");
            using RegistryKey categoryKey = Registry.ClassesRoot.OpenSubKey(wicDecoderCategoryPath, false);
            if (categoryKey == null)
            {
                yield break;
            }
            foreach (var codecGuid in categoryKey.GetSubKeyNames())
            {
                // Read the properties of the single registered decoder
                using var codecKey = baseKey.OpenSubKey(codecGuid);
                if (codecKey == null) continue;

                yield return new WicDecoder(codecGuid,
                    Convert.ToString(codecKey.GetValue("FriendlyName", "")),
                    Convert.ToString(codecKey.GetValue("MimeTypes", "")).Split(','),
                    Convert.ToString(codecKey.GetValue("FileExtensions", "")).Split(','));
            }
            var heifDecoderPath = Path.Combine(baseKeyPath, HeifDecoder);

            using RegistryKey heifKey = Registry.ClassesRoot.OpenSubKey(heifDecoderPath, false);
            if (heifKey == null) yield break;

            yield return new WicDecoder(HeifDecoder, "HEIF", new[] { "image/heif", "image/heic" }, new[] { ".heic", ".heif" });
        }

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            surfaceOutputSettings ??= new SurfaceOutputSettings();
            try
            {
                var bitmapSource = bitmap.ToBitmapSource();
                var bitmapFrame = BitmapFrame.Create(bitmapSource);
                var jpegXrEncoder = new WmpBitmapEncoder();
                jpegXrEncoder.Frames.Add(bitmapFrame);
                // TODO: Support supplying a quality
                jpegXrEncoder.ImageQualityLevel = surfaceOutputSettings.JPGQuality / 100f;
                jpegXrEncoder.Save(destination);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't save image as JPEG XR: ", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            try
            {
                var bitmapDecoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                var bitmapSource = bitmapDecoder.Frames[0];
                bitmap = bitmapSource.ToBitmap();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't load image: ", ex);
            }

            bitmap = null;
            return false;
        }
    }
}
