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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Dapplo.Windows.Clipboard;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// Loads and saves a DIB (Device Independent Bitmap): CF_DIB / CF_DIBV5 data from the clipboard or a drop, or a .dib file.
    /// Decoding is done by Dapplo's DibImage.TryDecode, which validates the header against the data and only works on managed arrays.
    /// </summary>
    public class DibFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DibFileFormatHandler));

        /// <summary>
        /// DIB data larger than this isn't read (a 32 bpp 16384 x 16384 image, plus header and palette)
        /// </summary>
        private const long MaxDibSize = 16384L * 16384L * 4L + 4096;

        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".dib", ".format17", ".deviceindependentbitmap" };

        public DibFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
        }

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            if (bitmap == null || destination == null)
            {
                return false;
            }

            var pixels = ClipboardBitmapConverter.ToBgra32(bitmap);
            var dibBytes = DibImage.CreateDib(pixels.Pixels, pixels.Width, pixels.Height, pixels.Stride, pixels.PremultipliedAlpha);
            destination.Write(dibBytes, 0, dibBytes.Length);
            return true;
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            bitmap = null;
            if (stream == null)
            {
                return false;
            }

            try
            {
                if (!TryReadAll(stream, out var dib))
                {
                    return false;
                }

                if (ClipboardBitmapConverter.TryDecodeDib(dib, out bitmap))
                {
                    return true;
                }

                Log.WarnFormat("Couldn't decode the DIB data ({0} bytes)", dib.Length);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("Problem reading the DIB data.", ex);
                bitmap?.Dispose();
                bitmap = null;
                return false;
            }
        }

        private static bool TryReadAll(Stream stream, out byte[] bytes)
        {
            bytes = null;
            if (stream.CanSeek)
            {
                long remaining = stream.Length - stream.Position;
                if (remaining <= 0 || remaining > MaxDibSize)
                {
                    Log.WarnFormat("DIB data has an unsupported size: {0} bytes", remaining);
                    return false;
                }
            }

            using var memoryStream = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                memoryStream.Write(buffer, 0, read);
                if (memoryStream.Length > MaxDibSize)
                {
                    Log.Warn("DIB data is too large.");
                    return false;
                }
            }

            if (memoryStream.Length == 0)
            {
                return false;
            }
            bytes = memoryStream.ToArray();
            return true;
        }
    }
}
