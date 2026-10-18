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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Http;
using Greenshot.Plugin.CustomUploader;
using Greenshot.Plugin.CustomUploader.Sxcu;
using Greenshot.Tests.Http;
using Xunit;

namespace Greenshot.Tests.CustomUploader
{
    public class ServiceIconTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

        public ServiceIconTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static UploaderDefinition Uploader(string requestUrl) =>
            UploaderFile.Read(UploaderFileTests.File($@"""Name"": ""Mine"", ""DestinationType"": ""ImageUploader"", ""RequestURL"": ""{requestUrl}"", ""FileFormName"": ""file"""));

        private static byte[] CreatePng(int width, int height)
        {
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Red);
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        private static byte[] CreateIco()
        {
            using var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Blue);
            }

            IntPtr handle = bitmap.GetHicon();
            using var icon = Icon.FromHandle(handle);
            using var stream = new MemoryStream();
            icon.Save(stream);
            return stream.ToArray();
        }

        private static Size SizeOf(byte[] png)
        {
            using var stream = new MemoryStream(png);
            using var image = Image.FromStream(stream);
            Assert.Equal(ImageFormat.Png, image.RawFormat);
            return image.Size;
        }

        private static HttpService Service(FakeHttpHandler handler) => new HttpService(handler, TestTimeout);

        private static HttpResponseMessage NotFound() => FakeHttpHandler.Text(HttpStatusCode.NotFound, "not here");

        [Fact]
        public async Task FetchAsync_FaviconIco_IsStoredAsPng32()
        {
            byte[] ico = CreateIco();
            var handler = new FakeHttpHandler(request => request.RequestUri.AbsolutePath == "/favicon.ico" ? FakeHttpHandler.Bytes(ico) : NotFound());

            var png = await ServiceIcon.FetchAsync(Service(handler), Uploader("https://upload.example.com/api/{filename}"), CancellationToken.None);

            Assert.NotNull(png);
            Assert.Equal(new Size(ServiceIcon.Size, ServiceIcon.Size), SizeOf(png));
            Assert.Equal(new Uri("https://upload.example.com/favicon.ico"), Assert.Single(handler.Requests).Uri);
        }

        [Fact]
        public async Task FetchAsync_NoFavicon_UsesTheIconLinkOfTheStartPage()
        {
            byte[] image = CreatePng(64, 64);
            var handler = new FakeHttpHandler(request => request.RequestUri.AbsolutePath switch
            {
                "/" => FakeHttpHandler.Text(HttpStatusCode.OK, "<html><head><link rel=\"stylesheet\" href=\"/a.css\"><link rel=\"icon\" data-href=\"/wrong.png\"><link href='/static/icon.png?v=1&amp;x=2' rel='shortcut icon'></head></html>"),
                "/static/icon.png" => FakeHttpHandler.Bytes(image),
                _ => NotFound()
            });

            var png = await ServiceIcon.FetchAsync(Service(handler), Uploader("https://example.com/upload"), CancellationToken.None);

            Assert.NotNull(png);
            Assert.Equal(new Size(32, 32), SizeOf(png));
            Assert.Equal(new Uri("https://example.com/static/icon.png?v=1&x=2"), handler.Requests.Last().Uri);
        }

        [Fact]
        public async Task FetchAsync_NothingOnTheServer_TriesTheParentDomain()
        {
            byte[] image = CreatePng(16, 16);
            var handler = new FakeHttpHandler(request => request.RequestUri.Host == "example.com" && request.RequestUri.AbsolutePath == "/favicon.ico" ? FakeHttpHandler.Bytes(image) : NotFound());

            var png = await ServiceIcon.FetchAsync(Service(handler), Uploader("https://api.example.com/upload"), CancellationToken.None);

            Assert.NotNull(png);
            Assert.Equal(new[] { "https://api.example.com/favicon.ico", "https://api.example.com/", "https://example.com/favicon.ico" },
                handler.Requests.Select(request => request.Uri.AbsoluteUri));
        }

        [Fact]
        public async Task FetchAsync_NoImageAnywhere_IsNull()
        {
            // An HTML page as favicon, a link to another host, and the parent domain has nothing
            var handler = new FakeHttpHandler(request => request.RequestUri.AbsolutePath switch
            {
                "/favicon.ico" => FakeHttpHandler.Text(HttpStatusCode.OK, "<html>not an icon</html>"),
                "/" => FakeHttpHandler.Text(HttpStatusCode.OK, "<link rel=\"icon\" href=\"https://tracker.example.net/icon.png\">"),
                _ => NotFound()
            });

            var png = await ServiceIcon.FetchAsync(Service(handler), Uploader("https://upload.example.com/upload"), CancellationToken.None);

            Assert.Null(png);
            Assert.DoesNotContain(handler.Requests, request => request.Uri.Host == "tracker.example.net");
        }

        [Fact]
        public void ToPng_TooLargeOrNoImage_IsNull()
        {
            var tooLarge = new byte[ServiceIcon.MaximumLength + 1];
            CreatePng(1, 1).CopyTo(tooLarge, 0);

            Assert.Null(ServiceIcon.ToPng(tooLarge));
            Assert.Null(ServiceIcon.ToPng(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0, 0, 0, 0, 0 }));
            Assert.Null(ServiceIcon.ToPng(null));
        }

        [Theory]
        [InlineData("https://api.upload.example.com", "https://api.upload.example.com/|https://upload.example.com/")]
        [InlineData("https://example.com", "https://example.com/")]
        [InlineData("http://localhost:8080", "http://localhost:8080/")]
        [InlineData("https://192.168.1.10", "https://192.168.1.10/")]
        public void GetSites_ServerThenParentDomain(string server, string expected)
        {
            Assert.Equal(expected, string.Join("|", ServiceIcon.GetSites(new Uri(server)).Select(site => site.AbsoluteUri)));
        }

        [Fact]
        public void GenericIcon_IsEmbedded()
        {
            using var image = EmbeddedResources.GetImage(typeof(CustomUploaderPlugin), "Upload");

            Assert.NotNull(image);
            Assert.Equal(new Size(32, 32), image.Size);
        }
    }
}
