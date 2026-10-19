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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Greenshot.Plugin.CustomUploader;
using Greenshot.Plugin.CustomUploader.Sxcu;
using Greenshot.Tests.Http;
using Xunit;

namespace Greenshot.Tests.CustomUploader
{
    public class UploaderRequestTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);
        private static readonly EncodedImage Png = new EncodedImage(Encoding.ASCII.GetBytes("PNGDATA"), "png");

        public UploaderRequestTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static UploaderDefinition Read(string properties) =>
            UploaderFile.Read(UploaderFileTests.File(@"""Version"": ""15.0.0"", ""DestinationType"": ""ImageUploader"", " + properties));

        [Fact]
        public async Task Create_Multipart_HasTheFieldsHeadersAndTheFile()
        {
            var definition = Read(@"""RequestURL"": ""https://example.com/upload"", ""Body"": ""MultipartFormData"", ""FileFormName"": ""image"",
                ""Arguments"": { ""key"": ""abc"", ""name"": ""{filename}"" }, ""Headers"": { ""Authorization"": ""Basic {base64:user:pw}"" }");

            using var request = UploaderRequest.Create(definition, Png, "capture.png", null);

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(new Uri("https://example.com/upload"), request.RequestUri);
            Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pw")), request.Headers.GetValues("Authorization").Single());
            Assert.Equal("multipart/form-data", request.Content.Headers.ContentType.MediaType);
            string body = await request.Content.ReadAsStringAsync();
            Assert.Contains("name=\"key\"", body);
            Assert.Contains("abc", body);
            Assert.Contains("name=\"name\"", body);
            Assert.Contains("name=\"image\"; filename=\"capture.png\"", body);
            Assert.Contains("PNGDATA", body);
        }

        [Fact]
        public async Task Create_Binary_SendsTheBytesWithTheContentTypeOfTheHeaders()
        {
            var definition = Read(@"""RequestMethod"": ""PUT"", ""RequestURL"": ""https://example.com/files/{filename}"", ""Body"": ""Binary"",
                ""Headers"": { ""Content-Type"": ""application/octet-stream"", ""X-Key"": ""k"" }");

            using var request = UploaderRequest.Create(definition, Png, "my capture.png", null);

            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("https://example.com/files/my%20capture.png", request.RequestUri.AbsoluteUri);
            Assert.Equal("application/octet-stream", request.Content.Headers.ContentType.MediaType);
            Assert.Equal("k", request.Headers.GetValues("X-Key").Single());
            Assert.Equal(Png.ToArray(), await request.Content.ReadAsByteArrayAsync());
        }

        [Fact]
        public void Create_Multipart_KeepsItsOwnContentType()
        {
            // The boundary is in the content type of a multipart body
            var definition = Read(@"""RequestURL"": ""https://example.com/upload"", ""FileFormName"": ""file"", ""Headers"": { ""Content-Type"": ""text/plain"" }");

            using var request = UploaderRequest.Create(definition, Png, "capture.png", null);

            Assert.Equal("multipart/form-data", request.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public void Create_Parameters_AreEscapedIntoTheQuery()
        {
            var definition = Read(@"""RequestURL"": ""https://example.com/upload?v=1"", ""FileFormName"": ""file"", ""Parameters"": { ""key"": ""a&b=c"", ""name"": ""{filename}"" }");

            using var request = UploaderRequest.Create(definition, Png, "my capture.png", null);

            Assert.Equal("https://example.com/upload?v=1&key=a%26b%3Dc&name=my%20capture.png", request.RequestUri.AbsoluteUri);
        }

        [Fact]
        public async Task SendAsync_DiscordWebhook_ReturnsTheLinkOfTheAttachment()
        {
            var definition = UploaderFile.Read(UploaderFileTests.Discord);
            var handler = new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.OK, @"{""id"":""1"",""attachments"":[{""url"":""https://cdn.discordapp.com/attachments/1/2/capture.png""}]}"));

            var response = await CustomUploaderPlugin.SendAsync(new HttpService(handler, TestTimeout), definition, Png, "capture.png", null, CancellationToken.None);

            Assert.True(response.IsSuccess);
            Assert.Equal(new Uri("https://cdn.discordapp.com/attachments/1/2/capture.png"), response.Url);
            var sent = Assert.Single(handler.Requests);
            Assert.Equal(new Uri("https://discord.com/api/webhooks/1234/s3cret"), sent.Uri);
            Assert.Contains("PNGDATA", Encoding.ASCII.GetString(sent.Body));
        }

        [Fact]
        public async Task SendAsync_HttpError_HasTheErrorMessageOfTheUploader()
        {
            var definition = Read(@"""RequestURL"": ""https://example.com/upload"", ""FileFormName"": ""file"", ""ErrorMessage"": ""{json:error.message}""");
            var handler = new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.BadRequest, @"{""error"":{""message"":""File too large""}}"));

            var response = await CustomUploaderPlugin.SendAsync(new HttpService(handler, TestTimeout), definition, Png, "capture.png", null, CancellationToken.None);

            Assert.False(response.IsSuccess);
            Assert.Null(response.Url);
            Assert.Equal("File too large", response.Error);
        }
    }

    public class UploaderResponseTests
    {
        public UploaderResponseTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static UploaderDefinition Read(string results) =>
            UploaderFile.Read(UploaderFileTests.File(@"""Version"": ""15.0.0"", ""DestinationType"": ""ImageUploader"", ""RequestURL"": ""https://example.com/upload"", ""FileFormName"": ""file""" +
                                                     (results == null ? "" : ", " + results)));

        private static HttpResult Result(string content, HttpStatusCode status = HttpStatusCode.OK, Dictionary<string, string> headers = null, string url = "https://example.com/upload") =>
            new HttpResult(status, status.ToString(), new Uri(url), headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), content);

        [Theory]
        [InlineData("{json:data.link}", @"{""data"":{""link"":""https://i.example.com/a.png""}}")]
        [InlineData("{json:$.data.link}", @"{""data"":{""link"":""https://i.example.com/a.png""}}")]
        [InlineData("{json:[0].link}", @"[{""link"":""https://i.example.com/a.png""}]")]
        [InlineData("https://i.example.com/{json:id}.png", @"{""id"":""a""}")]
        [InlineData("{xml:/upload/link}", "<upload><link>https://i.example.com/a.png</link></upload>")]
        [InlineData("{regex:href=\"([^\"]+)\"|1}", "<a href=\"https://i.example.com/a.png\">")]
        [InlineData("{regex:https://\\S+}", "Uploaded: https://i.example.com/a.png")]
        [InlineData("{response}", "https://i.example.com/a.png")]
        public void From_TheUrlOfTheUploader_ReadsTheLink(string url, string content)
        {
            var response = UploaderResponse.From(Read($@"""URL"": ""{url.Replace("\\", "\\\\").Replace("\"", "\\\"")}"""), Result(content), "a.png");

            Assert.True(response.IsSuccess);
            Assert.Equal(new Uri("https://i.example.com/a.png"), response.Url);
        }

        [Fact]
        public void From_WithoutUrl_TheResponseIsTheLink()
        {
            Assert.Equal(new Uri("https://i.example.com/a.png"), UploaderResponse.From(Read(null), Result(" https://i.example.com/a.png\n"), "a.png").Url);
        }

        [Fact]
        public void From_HeaderAndResponseUrl_AreRead()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "Location", "https://i.example.com/a.png" } };
            var response = UploaderResponse.From(Read(@"""URL"": ""{header:location}"", ""DeletionURL"": ""{responseurl}/delete"""), Result("", headers: headers, url: "https://example.com/u/1"), "a.png");

            Assert.Equal(new Uri("https://i.example.com/a.png"), response.Url);
            Assert.Equal(new Uri("https://example.com/u/1/delete"), response.DeletionUrl);
        }

        [Fact]
        public void From_DeletionAndThumbnailUrl_AreRead()
        {
            var response = UploaderResponse.From(Read(@"""URL"": ""{json:link}"", ""ThumbnailURL"": ""{json:thumb}"", ""DeletionURL"": ""https://example.com/delete/{json:key}"""),
                Result(@"{""link"":""https://i.example.com/a.png"",""thumb"":""https://i.example.com/t.png"",""key"":""k1""}"), "a.png");

            Assert.Equal(new Uri("https://i.example.com/t.png"), response.ThumbnailUrl);
            Assert.Equal(new Uri("https://example.com/delete/k1"), response.DeletionUrl);
        }

        [Theory]
        // Not JSON: an error page
        [InlineData("<html>Bad gateway</html>")]
        // Not a web link: it would be opened and put on the clipboard
        [InlineData(@"{""link"":""file:///C:/Windows/System32/calc.exe""}")]
        [InlineData(@"{""link"":""javascript:alert(1)""}")]
        [InlineData(@"{""link"":""not a link""}")]
        [InlineData(@"{""other"":1}")]
        public void From_NoWebLink_HasNoUrl(string content)
        {
            var response = UploaderResponse.From(Read(@"""URL"": ""{json:link}"""), Result(content), "a.png");

            Assert.True(response.IsSuccess);
            Assert.Null(response.Url);
        }

        [Fact]
        public void From_XmlWithDtd_IsNotRead()
        {
            string xml = "<?xml version=\"1.0\"?><!DOCTYPE a [<!ENTITY e SYSTEM \"file:///C:/Windows/win.ini\">]><a><link>&e;</link></a>";

            Assert.Null(UploaderResponse.From(Read(@"""URL"": ""{xml:/a/link}"""), Result(xml), "a.png").Url);
        }

        [Fact]
        public void From_HttpErrorWithoutErrorMessage_HasTheStatusAndTheTextAnswer()
        {
            var response = UploaderResponse.From(Read(null), Result("Error: file too large\n", HttpStatusCode.InternalServerError), "a.png");

            Assert.False(response.IsSuccess);
            Assert.Equal("500 InternalServerError: Error: file too large", response.Error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("<html><body><h1>500 Internal Server Error</h1></body></html>")]
        public void From_HttpErrorWithoutErrorMessage_EmptyOrHtmlAnswer_HasTheStatus(string content)
        {
            var response = UploaderResponse.From(Read(null), Result(content, HttpStatusCode.Forbidden), "a.png");

            Assert.Equal("403 Forbidden", response.Error);
        }
    }
}
