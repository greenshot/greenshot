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
using Greenshot.Plugin.CustomUploader;
using Greenshot.Plugin.CustomUploader.Sxcu;
using Xunit;

namespace Greenshot.Tests.CustomUploader
{
    public class UploaderStoreTests
    {
        private readonly Dictionary<string, string> _uploaders = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _uploads = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _icons = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _reminders = new Dictionary<string, string>();
        private int _changes;

        public UploaderStoreTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private UploaderStore CreateStore() => new UploaderStore(_uploaders, _uploads, () => _changes++, icons: _icons, reminders: _reminders);

        private static UploaderDefinition Uploader(string name, string host = "example.com") =>
            UploaderFile.Read(UploaderFileTests.File($@"""Name"": ""{name}"", ""DestinationType"": ""ImageUploader"", ""RequestURL"": ""https://{host}/upload"", ""FileFormName"": ""file"", ""Headers"": {{ ""Authorization"": ""Bearer s3cret"" }}"));

        [Fact]
        public void Add_StoresTheUploaderEncrypted()
        {
            var store = CreateStore();

            string id = store.Add(Uploader("Mine"));

            Assert.Equal(1, _changes);
            Assert.DoesNotContain("s3cret", _uploaders[id]);
            Assert.DoesNotContain("example.com", _uploaders[id]);
            var uploader = Assert.Single(CreateStore().GetUploaders());
            Assert.Equal("Mine", uploader.Name);
            Assert.Equal("Bearer s3cret", uploader.Definition.Headers["Authorization"]);
        }

        [Fact]
        public void Add_SameName_ReplacesTheUploader()
        {
            var store = CreateStore();
            string id = store.Add(Uploader("Mine"));

            string again = store.Add(Uploader("mine", "other.example.com"));

            Assert.Equal(id, again);
            Assert.Equal(new Uri("https://other.example.com"), UploaderFile.GetServer(Assert.Single(store.GetUploaders()).Definition));
        }

        [Fact]
        public void SetIcon_IsKeptPerUploader()
        {
            var store = CreateStore();
            string id = store.Add(Uploader("Mine"));
            byte[] png = { 0x89, (byte)'P', (byte)'N', (byte)'G', 1, 2, 3 };

            store.SetIcon(id, png);

            Assert.Equal(png, CreateStore().GetIcon(id));
            Assert.Null(store.GetIcon("other"));
            _icons[id] = "not base64!";
            Assert.Null(store.GetIcon(id));
        }

        [Fact]
        public void Icon_GoesWithTheUploader_AndIsFetchedAgainOnAReimport()
        {
            var store = CreateStore();
            string id = store.Add(Uploader("Mine"));
            store.SetIcon(id, new byte[] { 1 });

            store.Add(Uploader("Mine", "other.example.com"));
            Assert.Null(store.GetIcon(id));

            store.SetIcon(id, new byte[] { 2 });
            store.Remove(id);
            Assert.Empty(_icons);

            // The icon of an uploader removed while it was fetched isn't kept
            store.SetIcon(id, new byte[] { 3 });
            Assert.Empty(_icons);
        }

        [Fact]
        public void Reminder_IsChosenAtTheImport_AndCanBeStopped()
        {
            var store = CreateStore();
            string id = store.Add(Uploader("Mine"), remindBeforeUpload: true);
            Assert.True(CreateStore().RemindsBeforeUpload(id));

            store.StopReminding(id);
            Assert.False(store.RemindsBeforeUpload(id));

            store.Add(Uploader("Mine"), remindBeforeUpload: true);
            store.Add(Uploader("Mine"));
            Assert.False(store.RemindsBeforeUpload(id));

            store.Add(Uploader("Mine"), remindBeforeUpload: true);
            store.Remove(id);
            Assert.Empty(_reminders);
        }

        [Fact]
        public void GetUploaders_SortedByName_UnreadableLast()
        {
            var store = CreateStore();
            store.Add(Uploader("b"));
            store.Add(Uploader("a"));
            _uploaders["broken"] = "not encrypted";

            var uploaders = store.GetUploaders();

            Assert.Equal(new[] { "a", "b", null }, uploaders.Select(u => u.Name));
            Assert.Null(uploaders[2].Definition);
            Assert.Null(store.Find("broken"));
        }

        [Fact]
        public void Remove_RemovesTheUploader()
        {
            var store = CreateStore();
            string id = store.Add(Uploader("Mine"));

            store.Remove(id);
            store.Remove("unknown");

            Assert.Empty(store.GetUploaders());
            Assert.Equal(2, _changes);
        }

        [Fact]
        public void GetUploaders_ChangedToAnUnsafeUploader_IsNotReadable()
        {
            // Checked again when read: what is sent stays what the user approved when importing
            var store = new UploaderStore(_uploaders, _uploads, null, text => text, text => text);
            _uploaders["changed"] = UploaderFileTests.File(@"""DestinationType"": ""ImageUploader"", ""RequestURL"": ""http://example.com/upload"", ""FileFormName"": ""file""");

            Assert.Null(Assert.Single(store.GetUploaders()).Definition);
        }

        [Fact]
        public void AddUpload_KeepsTheNewestUploadsEncrypted()
        {
            var store = new UploaderStore(_uploaders, _uploads, null);
            var start = DateTimeOffset.Now;
            for (int i = 0; i < UploaderStore.MaximumUploads + 5; i++)
            {
                store.AddUpload(new UploadRecord { Uploader = "Mine", Url = $"https://i.example.com/{i}.png", DeletionUrl = $"https://example.com/delete/{i}", Uploaded = start.AddMinutes(i) });
            }

            var uploads = store.GetUploads();

            Assert.Equal(UploaderStore.MaximumUploads, uploads.Count);
            Assert.Equal("https://i.example.com/104.png", uploads[0].Value.Url);
            Assert.Equal("https://i.example.com/5.png", uploads.Last().Value.Url);
            Assert.DoesNotContain(_uploads.Values, value => value.Contains("delete"));

            store.ForgetUpload(uploads[0].Key);
            Assert.Equal("https://i.example.com/103.png", store.GetUploads()[0].Value.Url);
        }

        [Fact]
        public void Add_NonAsciiText_SurvivesTheEncryption()
        {
            // StringExtensions.Encrypt keeps ASCII only, the stored JSON escapes the rest
            var store = CreateStore();
            store.Add(Uploader("Grün – 画像"));
            store.AddUpload(new UploadRecord { Uploader = "Grün – 画像", Url = "https://i.example.com/ü.png", Uploaded = DateTimeOffset.Now });

            Assert.Equal("Grün – 画像", Assert.Single(CreateStore().GetUploaders()).Name);
            Assert.Equal("https://i.example.com/ü.png", Assert.Single(CreateStore().GetUploads()).Value.Url);
        }

        [Fact]
        public void Read_ValueWhichCantBeDecrypted_IsUnreadable()
        {
            var store = CreateStore();
            string id = store.Add(Uploader("Mine"));
            // Decrypt gives back what it can't decrypt: base64 of other bytes
            _uploaders[id] = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
            _uploads["broken"] = "not encrypted";

            Assert.Null(Assert.Single(store.GetUploaders()).Definition);
            Assert.Empty(store.GetUploads());
        }
    }
}
