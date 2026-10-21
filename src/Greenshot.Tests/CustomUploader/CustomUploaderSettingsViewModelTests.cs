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
using System.Collections.Generic;
using System.Linq;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Plugin.CustomUploader;
using Greenshot.Plugin.CustomUploader.Sxcu;
using Greenshot.Plugin.CustomUploader.ViewModels;
using Xunit;

namespace Greenshot.Tests.CustomUploader
{
    public class CustomUploaderSettingsViewModelTests
    {
        public CustomUploaderSettingsViewModelTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static ICustomUploaderLanguage Language => Texts.Get<ICustomUploaderLanguage>();

        private static UploaderStore CreateStore() => new UploaderStore(new Dictionary<string, string>(), new Dictionary<string, string>(), null);

        private static UploaderDefinition Uploader(string deletionUrl = null) =>
            UploaderFile.Read(UploaderFileTests.File(@"""Name"": ""Mine"", ""DestinationType"": ""ImageUploader"", ""RequestURL"": ""https://example.com/upload"", ""FileFormName"": ""file"""
                + (deletionUrl == null ? "" : $@", ""DeletionURL"": ""{deletionUrl}""")));

        [Fact]
        public void DescribeImport_WithoutDeletionUrl_SaysUploadsCantBeDeleted()
        {
            var viewModel = new CustomUploaderSettingsViewModel(CreateStore());
            string noDeletion = string.Format(Language.ImportNoDeletion, "example.com");

            Assert.Contains(noDeletion, viewModel.DescribeImport(Uploader()));
            Assert.DoesNotContain(noDeletion, viewModel.DescribeImport(Uploader("{json:delete}")));
        }

        [Fact]
        public void DescribeImport_SaysTheServiceIsntGreenshots()
        {
            var viewModel = new CustomUploaderSettingsViewModel(CreateStore());

            Assert.Contains(string.Format(Language.ImportNotGreenshot, "Mine", "example.com"), viewModel.DescribeImport(Uploader()));
        }

        [Fact]
        public void DescribeFailure_HasTheNamesOfWhatWasSentButNoValues()
        {
            var definition = CustomUploaderSettingsViewModel.WithValues(Shared(), CustomUploaderSettingsViewModel.GetValues(Shared()).Select(value =>
            {
                if (value.Name == "key") value.Value = "my-secret-key";
                return value;
            }).ToList());

            string details = CustomUploaderPlugin.DescribeFailure(definition, "412 PreconditionFailed: No file!", new EncodedImage(new byte[] { 1, 2, 3 }, "png"),
                "capture.png", new DateTimeOffset(2026, 10, 19, 8, 39, 25, TimeSpan.FromHours(2)));

            Assert.Contains("Uploader: ImgBB", details);
            Assert.Contains("Server: https://api.imgbb.com/", details);
            Assert.Contains("fields expiration, file image (\"capture.png\"", details);
            Assert.Contains("3 bytes", details);
            Assert.Contains("Address parameters: key", details);
            Assert.Contains("Headers: X-Client", details);
            Assert.Contains("Answer: 412 PreconditionFailed: No file!", details);
            Assert.Contains("Time: 2026-10-19 08:39:25 +02:00", details);
            Assert.DoesNotContain("my-secret-key", details);
            Assert.DoesNotContain("600", details);
        }

        [Fact]
        public void NoDeletionHint_OnlyForAnUploadWithoutDeleteLink()
        {
            var store = CreateStore();
            store.AddUpload(new UploadRecord { Uploader = "Litterbox", Url = "https://litter.catbox.moe/a.png", Uploaded = DateTimeOffset.Now });
            store.AddUpload(new UploadRecord { Uploader = "Mine", Url = "https://example.com/b.png", DeletionUrl = "https://example.com/delete/b", Uploaded = DateTimeOffset.Now.AddMinutes(1) });
            var viewModel = new CustomUploaderSettingsViewModel(store);

            Assert.Null(viewModel.NoDeletionHint);
            viewModel.SelectedUpload = viewModel.Uploads[0];
            Assert.Null(viewModel.NoDeletionHint);
            viewModel.SelectedUpload = viewModel.Uploads[1];
            Assert.Equal(string.Format(Language.NoDeletionLink, "Litterbox"), viewModel.NoDeletionHint);
        }

        private static UploaderDefinition Shared() =>
            UploaderFile.Read(UploaderFileTests.File(@"""Name"": ""ImgBB"", ""DestinationType"": ""ImageUploader"", ""RequestURL"": ""https://api.imgbb.com/1/upload"", ""FileFormName"": ""image"", ""URL"": ""{json:data.url}"",
                ""Parameters"": { ""key"": ""<API KEY HERE (https://api.imgbb.com/)>"" }, ""Headers"": { ""X-Client"": ""Greenshot"" }, ""Arguments"": { ""expiration"": ""600"" }"));

        [Fact]
        public void GetValues_ListsTheAddressParametersHeadersAndFormFields()
        {
            var values = CustomUploaderSettingsViewModel.GetValues(Shared());

            Assert.Equal(new string[] { null, "key", "X-Client", "expiration" }, values.Select(value => value.Name));
            Assert.Equal("https://api.imgbb.com/1/upload", values[0].Value);
            Assert.Equal(string.Format(Language.ValuesParameter, "key"), values[1].Label);
            Assert.Equal(string.Format(Language.ValuesHeader, "X-Client"), values[2].Label);
        }

        [Fact]
        public void WithValues_TheKeyOfTheUser_ReplacesTheTextOfTheFile()
        {
            var definition = Shared();
            var values = CustomUploaderSettingsViewModel.GetValues(definition);
            values[1].Value = "my-key";

            var withKey = CustomUploaderSettingsViewModel.WithValues(definition, values);

            Assert.Equal("my-key", withKey.Parameters["key"]);
            Assert.Equal("Greenshot", withKey.Headers["X-Client"]);
            Assert.Equal("<API KEY HERE (https://api.imgbb.com/)>", definition.Parameters["key"]);
        }

        [Theory]
        [InlineData(1, "{prompt}")]
        [InlineData(2, "a\r\nX-Injected: 1")]
        [InlineData(0, "http://api.imgbb.com/1/upload")]
        public void WithValues_ValueTheImportRefuses_Throws(int index, string value)
        {
            var definition = Shared();
            var values = CustomUploaderSettingsViewModel.GetValues(definition);
            values[index].Value = value;

            Assert.Throws<UploaderFileException>(() => CustomUploaderSettingsViewModel.WithValues(definition, values));
        }
        [Fact]
        public void GetValues_KeysAndTokens_AreMasked()
        {
            var values = CustomUploaderSettingsViewModel.GetValues(Shared());

            Assert.Equal(new[] { false, true, false, false }, values.Select(value => value.IsSecret));
            Assert.True(values[1].IsMasked);
            values[1].IsRevealed = true;
            Assert.False(values[1].IsMasked);
        }

        [Theory]
        [InlineData("<API KEY HERE (https://api.imgbb.com/)>", true)]
        [InlineData("[insert key]", true)]
        [InlineData("YOUR_TOKEN", true)]
        [InlineData("Token YOUR-TOKEN", true)]
        [InlineData("Paste your key here", true)]
        [InlineData("my-key", false)]
        [InlineData("600", false)]
        [InlineData("", false)]
        public void LooksLikePlaceholder_TheTextOfSharedFiles(string value, bool expected)
        {
            var values = CustomUploaderSettingsViewModel.GetValues(Shared());
            values[1].Value = value;

            Assert.Equal(expected, values[1].LooksLikePlaceholder);
        }
    }
}
