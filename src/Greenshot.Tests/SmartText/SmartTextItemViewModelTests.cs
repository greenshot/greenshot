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

using Greenshot.Plugin.SmartText.ViewModels;
using Xunit;

namespace Greenshot.Tests.SmartText
{
    /// <summary>
    /// The text an item was found under is shown only when it isn't the value written differently
    /// </summary>
    public class SmartTextItemViewModelTests
    {
        [Theory]
        [InlineData("FAQ", "https://getgreenshot.org/faq/", "FAQ")]
        [InlineData(" Impressum ", "https://getgreenshot.org/impressum/", "Impressum")]
        [InlineData("getgreenshot.org", "https://getgreenshot.org/", null)]
        [InlineData("www.getgreenshot.org", "https://www.getgreenshot.org", null)]
        [InlineData("https://GitHub.com/greenshot", "https://github.com/greenshot", null)]
        [InlineData("getgreenshot@gmail.com", "mailto:getgreenshot@gmail.com", null)]
        [InlineData("+49 30 123456", "+4930123456", null)]
        [InlineData("Write to us", "mailto:getgreenshot@gmail.com", "Write to us")]
        [InlineData("", "https://getgreenshot.org/", null)]
        public void FoundUnder_OnlyWhenNotTheValue(string readText, string value, string expected)
        {
            Assert.Equal(expected, SmartTextItemViewModel.FoundUnderOf(readText, value));
        }
    }
}
