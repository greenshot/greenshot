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
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Greenshot.Ipc.BrowserExtension
{
    /// <summary>
    /// Validates and decodes the image of an IMPORT_CAPTURE request (a browser extension capture).
    /// Only PNG and JPEG are accepted (what the browser capture APIs produce), so GDI+ never parses metafiles, TIFF or icons
    /// from the browser. The dimensions are read from the image header before decoding: a small image claiming huge
    /// dimensions would otherwise make GDI+ allocate gigabytes of memory.
    /// </summary>
    public static class ImportCaptureDecoder
    {
        /// <summary>Maximum width or height, the canvas limit of the browsers (and the GDI+ coordinate limit)</summary>
        public const int MaxSide = 32767;

        /// <summary>Maximum number of pixels (100 megapixels, ~400 MB as 32bpp bitmap)</summary>
        public const long MaxPixels = 100_000_000;

        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };

        /// <summary>
        /// Decodes a base64 PNG or JPEG into a bitmap owned by the caller.
        /// </summary>
        /// <param name="base64Payload">The image, base64 encoded (without data: URL prefix)</param>
        /// <param name="bitmap">The decoded image, null on failure</param>
        /// <param name="error">Why the payload was rejected, null on success</param>
        public static bool TryDecode(string base64Payload, out Bitmap bitmap, out string error)
        {
            bitmap = null;
            if (string.IsNullOrWhiteSpace(base64Payload))
            {
                error = "the image payload is missing";
                return false;
            }

            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(base64Payload);
            }
            catch (FormatException)
            {
                error = "the image payload is not valid base64";
                return false;
            }

            if (!StartsWith(imageBytes, PngSignature) && !StartsWith(imageBytes, JpegSignature))
            {
                error = "only PNG and JPEG images are accepted";
                return false;
            }

            try
            {
                using (var stream = new MemoryStream(imageBytes, false))
                {
                    // Header only: validateImageData=false does not decode the pixels
                    using (var header = Image.FromStream(stream, false, false))
                    {
                        if (!IsAcceptableSize(header.Width, header.Height, out error))
                        {
                            return false;
                        }
                    }

                    stream.Position = 0;
                    using (var source = new Bitmap(stream))
                    {
                        // Copy, so the bitmap stays valid after the stream is disposed
                        bitmap = new Bitmap(source);
                    }
                }
                error = null;
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ExternalException || ex is OutOfMemoryException)
            {
                bitmap?.Dispose();
                bitmap = null;
                error = "the image could not be decoded";
                return false;
            }
        }

        /// <summary>
        /// Checks the dimensions against <see cref="MaxSide"/> and <see cref="MaxPixels"/>.
        /// </summary>
        public static bool IsAcceptableSize(int width, int height, out string error)
        {
            if (width <= 0 || height <= 0)
            {
                error = "the image has no size";
                return false;
            }
            if (width > MaxSide || height > MaxSide || (long)width * height > MaxPixels)
            {
                error = $"the image is too large ({width}x{height}, the maximum is {MaxSide} pixels per side and {MaxPixels / 1_000_000} megapixels)";
                return false;
            }
            error = null;
            return true;
        }

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            if (data == null || data.Length < prefix.Length)
            {
                return false;
            }
            for (int i = 0; i < prefix.Length; i++)
            {
                if (data[i] != prefix[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
