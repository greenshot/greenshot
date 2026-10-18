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
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Xunit;

namespace Greenshot.Tests.Http
{
    public class ImageDownloaderTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

        public ImageDownloaderTests()
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
        public async Task DownloadImageAsync_Png_ReturnsTheBitmap()
        {
            byte[] png = CreatePng(3, 2);
            var service = new HttpService(new FakeHttpHandler(_ => FakeHttpHandler.Bytes(png)), TestTimeout);

            using var bitmap = await ImageDownloader.DownloadImageAsync(service, "https://example.com/image.png", CancellationToken.None);

            Assert.NotNull(bitmap);
            Assert.Equal(new Size(3, 2), bitmap.Size);
        }

        [Fact]
        public async Task DownloadImageAsync_PageWithAnImageUrlInTheFirstLine_DownloadsThatImage()
        {
            byte[] png = CreatePng(4, 4);
            var handler = new FakeHttpHandler(request => request.RequestUri.AbsolutePath == "/real.png"
                ? FakeHttpHandler.Bytes(png)
                : FakeHttpHandler.Text(HttpStatusCode.OK, "https://example.com/real.png\nmore text"));
            var service = new HttpService(handler, TestTimeout);

            using var bitmap = await ImageDownloader.DownloadImageAsync(service, "https://example.com/page", CancellationToken.None);

            Assert.NotNull(bitmap);
            Assert.Equal(new Size(4, 4), bitmap.Size);
            Assert.Equal(2, handler.Requests.Count);
        }

        [Fact]
        public async Task DownloadImageAsync_HttpErrorOrNoImage_ReturnsNull()
        {
            var notFound = new HttpService(new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.NotFound, string.Empty)), TestTimeout);
            var noImage = new HttpService(new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.OK, "just text")), TestTimeout);

            Assert.Null(await ImageDownloader.DownloadImageAsync(notFound, "https://example.com/image.png", CancellationToken.None));
            Assert.Null(await ImageDownloader.DownloadImageAsync(noImage, "https://example.com/page", CancellationToken.None));
        }
    }
}
