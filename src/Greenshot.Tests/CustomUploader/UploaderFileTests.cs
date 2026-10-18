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
using Greenshot.Plugin.CustomUploader.Sxcu;
using Xunit;

namespace Greenshot.Tests.CustomUploader
{
    public class UploaderFileTests
    {
        internal const string Discord = @"{
  ""Version"": ""14.1.0"",
  ""Name"": ""Discord webhook"",
  ""DestinationType"": ""ImageUploader, TextUploader, FileUploader"",
  ""RequestMethod"": ""POST"",
  ""RequestURL"": ""https://discord.com/api/webhooks/1234/s3cret"",
  ""Body"": ""MultipartFormData"",
  ""FileFormName"": ""file"",
  ""URL"": ""{json:attachments[0].url}""
}";

        public UploaderFileTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// An uploader file with the given properties (JSON, without the braces) instead of the Discord ones
        /// </summary>
        internal static string File(string properties) => "{" + properties + "}";

        private static string Uploader(string requestUrl = "https://example.com/upload", string body = "MultipartFormData", string extra = null) =>
            File($@"""Version"": ""15.0.0"", ""DestinationType"": ""ImageUploader"", ""RequestURL"": ""{requestUrl}"", ""Body"": ""{body}"", ""FileFormName"": ""file""" + (extra == null ? "" : ", " + extra));

        [Fact]
        public void Read_DiscordWebhook_IsAMultipartImageUploader()
        {
            var definition = UploaderFile.Read(Discord);

            Assert.Equal("Discord webhook", definition.Name);
            Assert.Equal("POST", definition.RequestMethod);
            Assert.Equal("MultipartFormData", definition.Body);
            Assert.Equal("file", definition.FileFormName);
            Assert.Equal(new Uri("https://discord.com"), UploaderFile.GetServer(definition));
            Assert.Equal("discord.com", UploaderFile.DescribeServer(definition));
        }

        [Fact]
        public void Read_WithoutName_IsNamedAfterTheServer()
        {
            Assert.Equal("example.com:8443", UploaderFile.Read(Uploader("https://example.com:8443/upload")).Name);
        }

        [Fact]
        public void Read_BinaryBodyAndPut_AreSupported()
        {
            var definition = UploaderFile.Read(Uploader(body: "binary", extra: @"""RequestMethod"": ""put"""));

            Assert.Equal("Binary", definition.Body);
            Assert.Equal("PUT", definition.RequestMethod);
        }

        [Fact]
        public void Read_OldFileWithRequestTypeAndWithoutBody_IsAForm()
        {
            var definition = UploaderFile.Read(File(@"""DestinationType"": ""FileUploader"", ""RequestType"": ""POST"", ""RequestURL"": ""https://example.com/upload"", ""FileFormName"": ""f"""));

            Assert.Equal("MultipartFormData", definition.Body);
            Assert.Equal("POST", definition.RequestMethod);
        }

        [Fact]
        public void Read_HttpToThisComputer_IsAllowed()
        {
            Assert.Equal("localhost:8080", UploaderFile.DescribeServer(UploaderFile.Read(Uploader("http://localhost:8080/upload"))));
            Assert.NotNull(UploaderFile.Read(Uploader("http://127.0.0.1/upload")));
        }

        [Fact]
        public void Read_PlaceholdersInThePath_AreAllowed()
        {
            var definition = UploaderFile.Read(Uploader("https://example.com/files/{filename}"));

            Assert.Equal(new Uri("https://example.com"), UploaderFile.GetServer(definition));
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("[1, 2]")]
        [InlineData("")]
        public void Read_NotAnUploaderFile_IsRefused(string json)
        {
            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(json));
        }

        [Theory]
        // Not encrypted
        [InlineData("http://example.com/upload")]
        // The server must be fixed: the user approves where the captures go
        [InlineData("https://{json:host}/upload")]
        [InlineData("https://example.{filename}/upload")]
        // A user name in the url
        [InlineData("https://user:password@example.com/upload")]
        [InlineData("ftp://example.com/upload")]
        [InlineData("/upload")]
        public void Read_UnsafeServer_IsRefused(string requestUrl)
        {
            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(Uploader(requestUrl)));
        }

        [Theory]
        [InlineData("FormURLEncoded")]
        [InlineData("JSON")]
        [InlineData("XML")]
        [InlineData("None")]
        public void Read_BodyWithoutTheFile_IsRefused(string body)
        {
            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(Uploader(body: body).Replace(@", ""FileFormName"": ""file""", "")));
        }

        [Fact]
        public void Read_MultipartWithoutFileFormName_IsRefused()
        {
            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(Uploader().Replace(@"""FileFormName"": ""file""", @"""FileFormName"": """"")));
        }

        [Theory]
        [InlineData("TextUploader")]
        [InlineData("URLShortener")]
        [InlineData("")]
        public void Read_NotAnImageOrFileUploader_IsRefused(string type)
        {
            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(Uploader().Replace(@"""ImageUploader""", $@"""{type}""")));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("DELETE")]
        public void Read_MethodWithoutBody_IsRefused(string method)
        {
            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(Uploader(extra: $@"""RequestMethod"": ""{method}""")));
        }

        [Theory]
        // Ask the user or pick at random: the user approves exactly what is sent
        [InlineData(@"""Arguments"": { ""title"": ""{prompt:Title}"" }")]
        [InlineData(@"""Arguments"": { ""title"": ""{inputbox:Title}"" }")]
        [InlineData(@"""Headers"": { ""X-Server"": ""{select:a|b}"" }")]
        [InlineData(@"""Parameters"": { ""n"": ""{random:1|2}"" }")]
        // The response isn't there yet
        [InlineData(@"""Parameters"": { ""n"": ""{json:id}"" }")]
        [InlineData(@"""URL"": ""{outputbox:x}""")]
        [InlineData(@"""URL"": ""{unknown}""")]
        // Not closed
        [InlineData(@"""URL"": ""{json:link""")]
        public void Read_UnsupportedPlaceholder_IsRefused(string extra)
        {
            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(Uploader(extra: extra)));
        }

        [Fact]
        public void Read_ResponsePlaceholders_AreAllowedInTheResults()
        {
            var definition = UploaderFile.Read(Uploader(extra: @"""URL"": ""{json:data.link}"", ""ThumbnailURL"": ""{xml:/a/b}"", ""DeletionURL"": ""https://example.com/delete/{regex:id=(\\w+)|1}"", ""ErrorMessage"": ""{header:X-Error} {response}"""));

            Assert.Equal("{json:data.link}", definition.URL);
        }

        [Theory]
        [InlineData(@"""Headers"": { ""Authorization"": ""Bearer a\r\nX-Evil: 1"" }")]
        [InlineData(@"""Headers"": { ""Bad Header"": ""1"" }")]
        [InlineData(@"""Headers"": { ""X-Evil:"": ""1"" }")]
        public void Read_InvalidHeader_IsRefused(string extra)
        {
            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(Uploader(extra: extra)));
        }

        [Fact]
        public void Read_OldSyntax_IsRefused()
        {
            string old = File(@"""Version"": ""13.1.0"", ""DestinationType"": ""ImageUploader"", ""RequestURL"": ""https://example.com/upload"", ""FileFormName"": ""file"", ""URL"": ""$json:data.link$""");

            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(old));
        }

        [Fact]
        public void Read_TooLargeFile_IsRefused()
        {
            string large = Uploader(extra: @"""Name"": """ + new string('a', UploaderFile.MaximumLength) + @"""");

            Assert.Throws<UploaderFileException>(() => UploaderFile.Read(large));
        }

        [Fact]
        public void Read_NameWithControlCharacters_IsCleaned()
        {
            var definition = UploaderFile.Read(Uploader(extra: @"""Name"": ""  My\r\nuploader\t """));

            Assert.Equal("Myuploader", definition.Name);
        }
    }
}
