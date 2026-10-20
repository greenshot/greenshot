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

        private IReadOnlyCollection<string> LoadExtensions { get; } = new []{ ".jxr", ".dds", ".hdp", ".wdp", ".wmp"};
        private IReadOnlyCollection<string> SaveExtensions { get; } = new[] { ".jxr" };

        /// <summary>
        /// The decoders WIC has, with their extensions and media types
        /// </summary>
        private readonly IReadOnlyList<(string[] Extensions, string[] MimeTypes)> _codecs;

        public WpfFileFormatHandler()
        {
            _codecs = RetrieveCodecs().ToList();
            LoadExtensions = LoadExtensions.ToList().Concat(_codecs.SelectMany(codec => codec.Extensions)).OrderBy(e => e).Distinct().ToArray();

            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = LoadExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = LoadExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = SaveExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToFile] = SaveExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromFile] = LoadExtensions;
        }

        /// <summary>
        /// Register the formats of the WIC decoders which no other handler registered, e.g. HEIC, so images in them can be kept as they are
        /// </summary>
        public override void RegisterFileFormats(IFileFormatRegistry registry)
        {
            foreach (var codec in _codecs)
            {
                var extensions = codec.Extensions.Where(extension => registry.GetByExtension(extension) == null).ToArray();
                var mimeTypes = codec.MimeTypes.Where(mimeType => registry.GetByMimeType(mimeType) == null).ToArray();
                if (extensions.Length == 0 || mimeTypes.Length == 0 || registry.TryGet(extensions[0].TrimStart('.'), out _))
                {
                    continue;
                }

                var id = extensions[0].TrimStart('.');
                RegisterFileFormat(registry, id, extensions, [], extensions[0], mimeTypes[0], mimeTypes.Skip(1), id.ToUpperInvariant());
            }
        }

        /// <summary>
        /// Detect all the formats WIC supports
        /// </summary>
        /// <returns>IEnumerable with the extensions and media types of each decoder</returns>
        private static IEnumerable<(string[] Extensions, string[] MimeTypes)> RetrieveCodecs()
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

                yield return (Split(codecKey.GetValue("FileExtensions")), Split(codecKey.GetValue("MimeTypes")));
            }
            var heifDecoderPath = Path.Combine(baseKeyPath, HeifDecoder);

            using RegistryKey heifKey = Registry.ClassesRoot.OpenSubKey(heifDecoderPath, false);
            if (heifKey == null) yield break;

            yield return (new[] { ".heic", ".heif" }, new[] { "image/heic", "image/heif" });
        }

        private static string[] Split(object value) => Convert.ToString(value ?? "").ToLowerInvariant().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).ToArray();

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
