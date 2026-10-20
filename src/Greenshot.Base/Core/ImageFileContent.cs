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
using System.Text;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The content of an image file in its own format, e.g. a virtual file of a drop or a downloaded image, for who keeps the file as it is
    /// </summary>
    public sealed class ImageFileContent
    {
        /// <param name="fileName">the name of the file, without folder</param>
        /// <param name="extension">the extension of the format, with the dot</param>
        /// <param name="content">the bytes of the file</param>
        public ImageFileContent(string fileName, string extension, byte[] content)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>
        /// The name of the file, without folder
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The extension of the format of the content, lower case with the dot
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// The bytes of the file
        /// </summary>
        public byte[] Content { get; }

        /// <summary>
        /// The extension of an image by its first bytes, not by its name or what a server says it is
        /// </summary>
        /// <returns>the extension with the dot, null when it's not a format this knows</returns>
        public static string GuessExtension(byte[] content)
        {
            if (content == null || content.Length < 8) return null;
            if (StartsWith(content, 0x89, (byte)'P', (byte)'N', (byte)'G')) return ".png";
            if (StartsWith(content, 0xFF, 0xD8, 0xFF)) return ".jpg";
            if (StartsWith(content, (byte)'G', (byte)'I', (byte)'F', (byte)'8')) return ".gif";
            if (StartsWith(content, (byte)'B', (byte)'M')) return ".bmp";
            if (StartsWith(content, 0, 0, 1, 0)) return ".ico";
            if (StartsWith(content, (byte)'I', (byte)'I', 0x2A, 0) || StartsWith(content, (byte)'M', (byte)'M', 0, 0x2A)) return ".tif";
            if (content.Length >= 12 && StartsWith(content, (byte)'R', (byte)'I', (byte)'F', (byte)'F') &&
                content[8] == 'W' && content[9] == 'E' && content[10] == 'B' && content[11] == 'P') return ".webp";
            return IsSvg(content) ? ".svg" : null;
        }

        private static bool StartsWith(byte[] content, params byte[] prefix) => content.Length >= prefix.Length && prefix.Select((value, index) => content[index] == value).All(equal => equal);

        /// <summary>
        /// XML with an svg element near the start, a page which only mentions svg further down isn't one
        /// </summary>
        private static bool IsSvg(byte[] content)
        {
            string start = Encoding.UTF8.GetString(content, 0, Math.Min(content.Length, 1024)).TrimStart('﻿', ' ', '\t', '\r', '\n');
            return start.StartsWith("<", StringComparison.Ordinal) && start.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   start.IndexOf("<html", StringComparison.OrdinalIgnoreCase) < 0;
        }

        /// <summary>
        /// The file name for the image of the url: its last path segment (or "image") with the extension of the image,
        /// an url like .../image.php gives image.png for a PNG
        /// </summary>
        public static string FileNameOf(Uri uri, string extension)
        {
            string name = null;
            if (uri != null && uri.IsAbsoluteUri)
            {
                name = Uri.UnescapeDataString(uri.Segments.LastOrDefault() ?? string.Empty).Trim('/');
            }

            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                name = "image";
            }

            return Path.GetFileNameWithoutExtension(name) + extension;
        }
    }
}
