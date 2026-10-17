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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Xunit;

namespace Greenshot.Tests.Http
{
    public class HttpContentsTests
    {
        public HttpContentsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public async Task Upload_HasTheBytesMimeTypeAndLength_AndReportsTheProgress()
        {
            // More than one chunk of 64 KB
            byte[] bytes = Enumerable.Range(0, 200 * 1024).Select(i => unchecked((byte)i)).ToArray();
            var progress = new RecordingProgress();

            var content = HttpContents.Upload(new EncodedImage(bytes, "png"), progress);

            Assert.Equal("image/png", content.Headers.ContentType.MediaType);
            Assert.Equal((long?)bytes.Length, content.Headers.ContentLength);
            Assert.Equal(bytes, await content.ReadAsByteArrayAsync());
            Assert.True(progress.Reports.Count > 1);
            Assert.Equal(100.0, progress.Reports.Last());
        }

        [Fact]
        public async Task Multipart_HasTheFieldsAndTheFileWithQuotedNames()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("PNGDATA");

            var content = HttpContents.Multipart("file", new EncodedImage(bytes, "png"), "capture 1.png", new Dictionary<string, string>
            {
                { "parent_id", "42" }
            });

            Assert.Equal("multipart/form-data", content.Headers.ContentType.MediaType);
            string body = await content.ReadAsStringAsync();
            Assert.Contains("name=\"parent_id\"", body);
            Assert.Contains("42", body);
            Assert.Contains("name=\"file\"; filename=\"capture 1.png\"", body);
            Assert.Contains("Content-Type: image/png", body);
            Assert.Contains("PNGDATA", body);
            // The fields come before the file, some servers need the parent before the content
            Assert.True(body.IndexOf("parent_id", StringComparison.Ordinal) < body.IndexOf("PNGDATA", StringComparison.Ordinal));
        }

        private sealed class RecordingProgress : IProgress<ProgressInfo>
        {
            public List<double> Reports { get; } = new List<double>();

            public void Report(ProgressInfo value) => Reports.Add(value.Percentage ?? -1);
        }
    }
}
