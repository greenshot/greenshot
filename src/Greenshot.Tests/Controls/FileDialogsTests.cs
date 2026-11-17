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

using Greenshot.Base.Controls;
using Xunit;

namespace Greenshot.Tests.Controls
{
    /// <summary>
    /// The file types of the open and save dialogs, from the filter texts of the language packs
    /// </summary>
    public class FileDialogsTests
    {
        [Fact]
        public void ParseFilter_ReadsNameAndPatternPairs()
        {
            var filters = FileDialogs.ParseFilter("Greenshot Recipe (*.gsrecipe.json)|*.gsrecipe.json|JSON Recipe (*.json)|*.json|All files (*.*)|*.*");

            Assert.Equal(3, filters.Count);
            Assert.Equal(("Greenshot Recipe (*.gsrecipe.json)", "*.gsrecipe.json"), filters[0]);
            Assert.Equal(("JSON Recipe (*.json)", "*.json"), filters[1]);
            Assert.Equal(("All files (*.*)", "*.*"), filters[2]);
        }

        [Fact]
        public void ParseFilter_KeepsSeveralPatternsOfOneType()
        {
            var filters = FileDialogs.ParseFilter("Programs (*.exe, *.bat)|*.exe;*.bat");

            Assert.Equal(("Programs (*.exe, *.bat)", "*.exe;*.bat"), Assert.Single(filters));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Only a name")]
        [InlineData("A name without patterns|")]
        public void ParseFilter_WithoutPatterns_IsEmpty(string filter)
        {
            Assert.Empty(FileDialogs.ParseFilter(filter));
        }

        [Fact]
        public void ParseFilter_WithoutName_UsesThePattern()
        {
            Assert.Equal(("*.gst", "*.gst"), Assert.Single(FileDialogs.ParseFilter("|*.gst")));
        }

        [Theory]
        [InlineData("*.gst", "gst")]
        [InlineData("*.gsrecipe.json;*.json", "gsrecipe.json")]
        [InlineData(" *.png ; *.jpg", "png")]
        [InlineData("*.*", null)]
        [InlineData("*", null)]
        [InlineData("report.txt", null)]
        [InlineData("*.j?g", null)]
        [InlineData(null, null)]
        public void DefaultExtensionOf_IsTheExtensionOfTheFirstPattern(string pattern, string expected)
        {
            Assert.Equal(expected, FileDialogs.DefaultExtensionOf(pattern));
        }
    }
}
