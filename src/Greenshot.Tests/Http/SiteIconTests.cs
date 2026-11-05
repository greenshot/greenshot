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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Xunit;

namespace Greenshot.Tests.Http
{
    public class SiteIconTests
    {
        public SiteIconTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static byte[] CreatePng(int width, int height)
        {
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        [Fact]
        public async Task FetchAsync_SiteWithAPath_AsksThereAndNotTheParentDomain()
        {
            // A Jira at https://jira.example.com/jira: its favicon is below the path
            var handler = new FakeHttpHandler(request => request.RequestUri.AbsolutePath == "/jira/favicon.ico"
                ? FakeHttpHandler.Bytes(CreatePng(16, 16))
                : FakeHttpHandler.Text(HttpStatusCode.NotFound, "not here"));

            var png = await SiteIcon.FetchAsync(new HttpService(handler, TimeSpan.FromSeconds(30)), new Uri("https://jira.example.com/jira"), false, CancellationToken.None);

            Assert.NotNull(png);
            Assert.Equal(new Uri("https://jira.example.com/jira/favicon.ico"), Assert.Single(handler.Requests).Uri);
        }

        [Fact]
        public async Task FetchAsync_WithoutParentDomain_StaysOnTheSite()
        {
            var handler = new FakeHttpHandler(request => FakeHttpHandler.Text(HttpStatusCode.NotFound, "not here"));

            Assert.Null(await SiteIcon.FetchAsync(new HttpService(handler, TimeSpan.FromSeconds(30)), new Uri("https://api.example.com"), false, CancellationToken.None));
            Assert.All(handler.Requests, request => Assert.Equal("api.example.com", request.Uri.Host));
        }

        [Fact]
        public void ToPng_TooLargeOrNoImage_IsNull()
        {
            var tooLarge = new byte[SiteIcon.MaximumLength + 1];
            CreatePng(1, 1).CopyTo(tooLarge, 0);

            Assert.Null(SiteIcon.ToPng(tooLarge));
            Assert.Null(SiteIcon.ToPng(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0, 0, 0, 0, 0 }));
            Assert.Null(SiteIcon.ToPng(null));
        }

        [Theory]
        [InlineData("https://api.upload.example.com", "https://api.upload.example.com/|https://upload.example.com/")]
        [InlineData("https://example.com", "https://example.com/")]
        [InlineData("http://localhost:8080", "http://localhost:8080/")]
        [InlineData("https://192.168.1.10", "https://192.168.1.10/")]
        [InlineData("https://example.com/jira?x=1", "https://example.com/jira/")]
        public void GetSites_ServerThenParentDomain(string server, string expected)
        {
            Assert.Equal(expected, string.Join("|", SiteIcon.GetSites(new Uri(server), true).Select(site => site.AbsoluteUri)));
        }
    }
}
