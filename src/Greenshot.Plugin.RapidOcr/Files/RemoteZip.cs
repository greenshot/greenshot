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
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;

namespace Greenshot.Plugin.RapidOcr.Files;

/// <summary>
/// Reads one file out of a zip on a web server with range requests, so only that file is downloaded
/// (e.g. onnxruntime.dll, 6 MB, out of the 80 MB release zip of ONNX Runtime)
/// </summary>
internal static class RemoteZip
{
    private const uint EndOfCentralDirectory = 0x06054b50;
    private const uint CentralDirectoryEntry = 0x02014b50;
    private const uint LocalFileHeader = 0x04034b50;
    // The end record is at most 22 bytes plus a comment of up to 64 KB
    private const int TailLength = 22 + 65535;

    /// <summary>
    /// The uncompressed content of the entry
    /// </summary>
    /// <exception cref="InvalidDataException">the zip or the entry isn't as expected</exception>
    public static async Task<byte[]> ReadEntryAsync(HttpService http, string url, string entryName, CancellationToken cancellationToken)
    {
        // The end of the zip: where the central directory is. Not every server knows suffix ranges, so the size comes first
        long zipSize = await GetSizeAsync(http, url, cancellationToken).ConfigureAwait(false);
        long tailLength = Math.Min(zipSize, TailLength);
        var tail = await GetRangeAsync(http, url, zipSize - tailLength, tailLength, cancellationToken).ConfigureAwait(false);
        int end = LastIndexOf(tail, EndOfCentralDirectory);
        if (end < 0) throw new InvalidDataException("No zip directory found");
        long directorySize = BitConverter.ToUInt32(tail, end + 12);
        long directoryOffset = BitConverter.ToUInt32(tail, end + 16);
        if (directoryOffset == uint.MaxValue || directorySize == uint.MaxValue) throw new InvalidDataException("Zip64 isn't supported");

        var directory = await GetRangeAsync(http, url, directoryOffset, directorySize, cancellationToken).ConfigureAwait(false);
        var (method, compressedSize, size, localOffset) = FindEntry(directory, entryName);

        // The local header has its own name and extra lengths
        var header = await GetRangeAsync(http, url, localOffset, 30, cancellationToken).ConfigureAwait(false);
        if (BitConverter.ToUInt32(header, 0) != LocalFileHeader) throw new InvalidDataException("Damaged zip entry");
        long dataOffset = localOffset + 30 + BitConverter.ToUInt16(header, 26) + BitConverter.ToUInt16(header, 28);
        var compressed = await GetRangeAsync(http, url, dataOffset, compressedSize, cancellationToken).ConfigureAwait(false);

        byte[] content;
        switch (method)
        {
            case 0:
                content = compressed;
                break;
            case 8:
                using (var input = new DeflateStream(new MemoryStream(compressed), CompressionMode.Decompress))
                using (var output = new MemoryStream((int)size))
                {
                    await input.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
                    content = output.ToArray();
                }
                break;
            default:
                throw new InvalidDataException($"Unsupported compression {method}");
        }

        if (content.Length != size) throw new InvalidDataException("The zip entry has the wrong size");
        return content;
    }

    /// <summary>
    /// The entry with the name in the central directory
    /// </summary>
    internal static (int Method, long CompressedSize, long Size, long Offset) FindEntry(byte[] directory, string entryName)
    {
        int position = 0;
        while (position + 46 <= directory.Length && BitConverter.ToUInt32(directory, position) == CentralDirectoryEntry)
        {
            int method = BitConverter.ToUInt16(directory, position + 10);
            long compressedSize = BitConverter.ToUInt32(directory, position + 20);
            long size = BitConverter.ToUInt32(directory, position + 24);
            int nameLength = BitConverter.ToUInt16(directory, position + 28);
            int extraLength = BitConverter.ToUInt16(directory, position + 30);
            int commentLength = BitConverter.ToUInt16(directory, position + 32);
            long offset = BitConverter.ToUInt32(directory, position + 42);
            string name = Encoding.UTF8.GetString(directory, position + 46, nameLength);
            if (string.Equals(name, entryName, StringComparison.Ordinal))
            {
                return (method, compressedSize, size, offset);
            }
            position += 46 + nameLength + extraLength + commentLength;
        }

        throw new InvalidDataException($"{entryName} isn't in the zip");
    }

    /// <summary>
    /// The size of the file, from the Content-Range of a request for its first byte
    /// </summary>
    private static async Task<long> GetSizeAsync(HttpService http, string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(0, 0);
        var result = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess();
        if (result.StatusCode != System.Net.HttpStatusCode.PartialContent
            || !result.Headers.TryGetValue("Content-Range", out string contentRange)
            || !ContentRangeHeaderValue.TryParse(contentRange, out var range) || range.Length is not > 0)
        {
            throw new InvalidDataException("The server doesn't send parts of the file");
        }
        return range.Length.Value;
    }

    /// <summary>
    /// The bytes from the offset; a server which ignores the range is refused
    /// </summary>
    private static async Task<byte[]> GetRangeAsync(HttpService http, string url, long offset, long length, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(offset, offset + length - 1);
        var bytes = await http.GetBytesAsync(request, cancellationToken).ConfigureAwait(false);
        if (bytes.Length != length)
        {
            throw new InvalidDataException("The server doesn't send parts of the file");
        }
        return bytes;
    }

    private static int LastIndexOf(byte[] data, uint signature)
    {
        for (int i = data.Length - 22; i >= 0; i--)
        {
            if (BitConverter.ToUInt32(data, i) == signature) return i;
        }
        return -1;
    }
}
