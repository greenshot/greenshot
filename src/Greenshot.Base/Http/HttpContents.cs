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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Http
{
    /// <summary>
    /// HTTP content for uploading an encoded capture.
    /// </summary>
    public static class HttpContents
    {
        /// <summary>
        /// The encoded capture as content (without copying the bytes) with its MIME type, reports the upload progress.
        /// </summary>
        /// <param name="image">EncodedImage</param>
        /// <param name="progress">IProgress for the percentage of the upload, optional</param>
        public static HttpContent Upload(EncodedImage image, IProgress<ProgressInfo> progress = null)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            var content = new UploadContent(image.Bytes, progress);
            content.Headers.ContentType = new MediaTypeHeaderValue(image.MimeType);
            return content;
        }

        /// <summary>
        /// multipart/form-data with the text fields first and then the encoded capture as file.
        /// </summary>
        /// <param name="fileFieldName">name of the file field</param>
        /// <param name="image">EncodedImage</param>
        /// <param name="filename">the file name the server gets</param>
        /// <param name="fields">text fields, optional</param>
        /// <param name="progress">IProgress for the percentage of the upload, optional</param>
        public static MultipartFormDataContent Multipart(string fileFieldName, EncodedImage image, string filename, IEnumerable<KeyValuePair<string, string>> fields = null, IProgress<ProgressInfo> progress = null)
        {
            if (string.IsNullOrEmpty(fileFieldName)) throw new ArgumentNullException(nameof(fileFieldName));
            var multipart = new MultipartFormDataContent($"----------{Guid.NewGuid():N}");
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    multipart.Add(new StringContent(field.Value ?? string.Empty), Quote(field.Key));
                }
            }

            // Not Add(content, name, fileName): on .NET Framework that MIME encodes a non-ASCII name (filename="=?utf-8?B?...?=")
            // and writes filename* with the quotes encoded into it, either way a server (catbox.moe) loses the extension.
            var file = Upload(image, progress);
            file.Headers.ContentDisposition = CreateFileDisposition(fileFieldName, filename);
            multipart.Add(file);
            return multipart;
        }

        /// <summary>
        /// The Content-Disposition of the file part: the name in printable ASCII (others, quotes and backslashes replaced by _,
        /// the extension stays; .NET rejects a quote in it), plus the real name as filename* (RFC 5987) when it differs.
        /// </summary>
        internal static ContentDispositionHeaderValue CreateFileDisposition(string fileFieldName, string filename)
        {
            filename ??= string.Empty;
            var ascii = new StringBuilder(filename.Length);
            foreach (char c in filename)
            {
                ascii.Append(c >= ' ' && c < 127 && c != '"' && c != '\\' ? c : '_');
            }

            var disposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = Quote(fileFieldName),
                FileName = Quote(ascii.ToString())
            };
            if (ascii.ToString() != filename)
            {
                disposition.FileNameStar = filename;
            }

            return disposition;
        }

        /// <summary>
        /// Some servers (Box) only accept quoted names in the Content-Disposition, HttpClient doesn't quote them itself.
        /// </summary>
        private static string Quote(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\\\"")}\"";

        /// <summary>
        /// Writes the bytes in chunks and reports the percentage, the cancellation of the request stops it.
        /// </summary>
        private sealed class UploadContent : HttpContent
        {
            private const int ChunkSize = 64 * 1024;
            private readonly ReadOnlyMemory<byte> _bytes;
            private readonly IProgress<ProgressInfo> _progress;

            public UploadContent(ReadOnlyMemory<byte> bytes, IProgress<ProgressInfo> progress)
            {
                _bytes = bytes;
                _progress = progress;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                var segment = MemoryMarshal.TryGetArray(_bytes, out var arraySegment)
                    ? arraySegment
                    : new ArraySegment<byte>(_bytes.ToArray());
                int written = 0;
                while (written < segment.Count)
                {
                    int chunk = Math.Min(ChunkSize, segment.Count - written);
                    await stream.WriteAsync(segment.Array, segment.Offset + written, chunk).ConfigureAwait(false);
                    written += chunk;
                    _progress?.Report(new ProgressInfo(null, 100.0 * written / segment.Count));
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _bytes.Length;
                return true;
            }
        }
    }
}
