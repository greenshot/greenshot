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
using Greenshot.Destinations.ImageSearch;
using Xunit;

namespace Greenshot.Tests.Destinations
{
    public class ImageSearchTests
    {
        [Fact]
        public void FormPostPage_EncodesTheFields()
        {
            string page = FormPostPage.Create("https://www.bing.com/images/search?view=detailv2&iss=sbiupload",
                new Dictionary<string, string> { ["imageBin"] = "ab+/=\"<x>" });

            Assert.Contains("action=\"https://www.bing.com/images/search?view=detailv2&amp;iss=sbiupload\"", page);
            Assert.Contains("name=\"imageBin\" value=\"ab+/=&quot;&lt;x&gt;\"", page);
            Assert.Contains("enctype=\"multipart/form-data\"", page);
        }

        [Fact]
        public void FormPostPage_OnlyPostsToHttps()
        {
            Assert.Throws<ArgumentException>(() => FormPostPage.Create("http://www.bing.com/", new Dictionary<string, string>()));
            Assert.Throws<ArgumentException>(() => FormPostPage.Create("javascript:alert(1)", new Dictionary<string, string>()));
        }

        [Fact]
        public void Yandex_ResultUrl_FromTheUploadAnswer()
        {
            // Shortened answer of the upload, 2026-11-21
            const string answer = "{\"cnt\":\"pageview_candidate\",\"blocks\":[{\"name\":{\"block\":\"cbir-uploader__get-cbir-id\",\"mods\":{}}," +
                                  "\"params\":{\"originalImageUrl\":\"https://avatars.mds.yandex.net/get-images-cbir/45167/abc/orig\",\"cbirId\":\"45167/abc\",\"bundles\":[]},\"html\":\"\"}]}";

            Assert.True(YandexImageSearch.TryGetResultUrl(answer, out var url));
            Assert.Equal("https://yandex.com/images/search?rpt=imageview&cbir_id=45167%2Fabc&url=https%3A%2F%2Favatars.mds.yandex.net%2Fget-images-cbir%2F45167%2Fabc%2Forig", url);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("<html>captcha</html>")]
        [InlineData("{\"blocks\":[]}")]
        [InlineData("{\"blocks\":[{\"params\":{\"cbirId\":\"\"}}]}")]
        public void Yandex_NoResultUrl_WithoutId(string answer)
        {
            Assert.False(YandexImageSearch.TryGetResultUrl(answer, out _));
        }
    }
}
