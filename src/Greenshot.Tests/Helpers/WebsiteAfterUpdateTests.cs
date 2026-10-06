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


using Greenshot.Helpers;
using Xunit;

namespace Greenshot.Tests.Helpers
{
    public class WebsiteAfterUpdateTests
    {
        [Theory]
        [InlineData(null, "1.4.290", true)] // new install, or an update from a version before this existed
        [InlineData("", "1.4.290", true)]
        [InlineData("1.4.289", "1.4.290", true)] // update
        [InlineData("1.3.300", "1.4.1", true)]
        [InlineData("1.4.290", "1.4.290", false)] // shown already
        [InlineData("1.4.291", "1.4.290", false)] // downgrade
        [InlineData("garbage", "1.4.290", true)]
        [InlineData("1.4.289", "unknown", false)]
        public void ShouldShow(string shownForVersion, string currentVersion, bool expected)
        {
            Assert.Equal(expected, WebsiteAfterUpdate.ShouldShow(shownForVersion, currentVersion));
        }

        [Theory]
        [InlineData("en-US", "en")]
        [InlineData("de-DE", "de")]
        [InlineData("pt-BR", "ptBR")]
        [InlineData("zh-CN", "cn")]
        [InlineData("nn-NO", "nn")]
        [InlineData(null, "en")]
        public void WebsiteLanguage_IsTheInstallerName(string ietf, string expected)
        {
            Assert.Equal(expected, WebsiteAfterUpdate.WebsiteLanguage(ietf));
        }

        [Fact]
        public void BuildUri_FullEdition_HasNoEdition()
        {
            var uri = WebsiteAfterUpdate.BuildUri("1.4.289", "de-DE", null);
            Assert.Equal("https://getgreenshot.org/thank-you/?language=de&version=1.4.289", uri.AbsoluteUri);
        }

        [Fact]
        public void BuildUri_LightEdition_HasTheEdition()
        {
            var uri = WebsiteAfterUpdate.BuildUri("1.4.289", "en-US", "Light");
            Assert.Equal("https://getgreenshot.org/thank-you/?language=en&version=1.4.289&edition=light", uri.AbsoluteUri);
        }
    }
}
