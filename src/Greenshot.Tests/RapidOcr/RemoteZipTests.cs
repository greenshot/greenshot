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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Plugin.RapidOcr.Files;
using Xunit;

namespace Greenshot.Tests.RapidOcr
{
    /// <summary>
    /// One file out of a zip on a server, with range requests only
    /// </summary>
    public class RemoteZipTests
    {
        [Fact]
        public async Task Entry_IsReadWithRanges()
        {
            var content = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("onnxruntime ", 5000)));
            var zip = CreateZip(("readme.txt", Encoding.UTF8.GetBytes("hello")), ("lib/onnxruntime.dll", content));
            var handler = new RangeHandler(zip);
            var http = new HttpService(handler, TimeSpan.FromSeconds(10));

            var read = await RemoteZip.ReadEntryAsync(http, "https://example.com/release.zip", "lib/onnxruntime.dll", TestContext.Current.CancellationToken);

            Assert.Equal(content, read);
            // Never the whole zip
            Assert.All(handler.Ranges, range => Assert.NotNull(range));
        }

        [Fact]
        public async Task MissingEntry_IsRefused()
        {
            var http = new HttpService(new RangeHandler(CreateZip(("readme.txt", new byte[] { 1 }))), TimeSpan.FromSeconds(10));
            await Assert.ThrowsAsync<InvalidDataException>(() => RemoteZip.ReadEntryAsync(http, "https://example.com/release.zip", "onnxruntime.dll", TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ServerWithoutRanges_IsRefused()
        {
            var http = new HttpService(new RangeHandler(CreateZip(("a.txt", new byte[] { 1 })), ignoreRanges: true), TimeSpan.FromSeconds(10));
            await Assert.ThrowsAsync<InvalidDataException>(() => RemoteZip.ReadEntryAsync(http, "https://example.com/release.zip", "a.txt", TestContext.Current.CancellationToken));
        }

        /// <summary>
        /// The real onnxruntime.dll out of Microsoft's release zip, only with GREENSHOT_RAPIDOCR_ONLINE set (it downloads 6 MB)
        /// </summary>
        [Fact]
        public async Task OnnxRuntime_FromTheRelease_HasTheExpectedHash()
        {
            Assert.SkipWhen(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GREENSHOT_RAPIDOCR_ONLINE")), "Set GREENSHOT_RAPIDOCR_ONLINE to download");
            string directory = Path.Combine(Path.GetTempPath(), "greenshot-rapidocr-" + Guid.NewGuid().ToString("N"));
            var files = new RapidOcrFiles(directory);
            var runtime = RapidOcrFiles.All.Where(file => file.ZipEntry != null).ToList();
            try
            {
                foreach (var file in runtime)
                {
                    var content = await RemoteZip.ReadEntryAsync(HttpService.Current, file.Url, file.ZipEntry, TestContext.Current.CancellationToken);
                    Directory.CreateDirectory(directory);
                    File.WriteAllBytes(files.PathOf(file.Name), content);
                    Assert.True(files.IsValid(file), file.Name);
                }
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static byte[] CreateZip(params (string Name, byte[] Content)[] entries)
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (var (name, data) in entries)
                {
                    using var entry = archive.CreateEntry(name, CompressionLevel.Optimal).Open();
                    entry.Write(data, 0, data.Length);
                }
            }
            return stream.ToArray();
        }

        /// <summary>Serves the bytes, honoring the Range header as a web server does</summary>
        private sealed class RangeHandler : HttpMessageHandler
        {
            private readonly byte[] _data;
            private readonly bool _ignoreRanges;

            public RangeHandler(byte[] data, bool ignoreRanges = false)
            {
                _data = data;
                _ignoreRanges = ignoreRanges;
            }

            public System.Collections.Generic.List<string> Ranges { get; } = new();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var range = request.Headers.Range?.Ranges.SingleOrDefault();
                Ranges.Add(range?.ToString());
                if (range == null || _ignoreRanges)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(_data) });
                }

                // Like GitHub's release downloads: no suffix ranges
                if (range.From == null) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
                long from = range.From.Value;
                long to = Math.Min(range.To ?? _data.Length - 1, _data.Length - 1);
                var part = _data.Skip((int)from).Take((int)(to - from + 1)).ToArray();
                var response = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(part) };
                response.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(from, to, _data.Length);
                return Task.FromResult(response);
            }
        }
    }
}
