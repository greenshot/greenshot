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
using Greenshot.Plugin.CustomUploader.Sxcu;
using Xunit;

namespace Greenshot.Tests.CustomUploader
{
    public class PlaceholderSyntaxTests
    {
        private static string Call(string name, IReadOnlyList<string> parameters) => name switch
        {
            "filename" => "my capture.png",
            "upper" => parameters[0].ToUpperInvariant(),
            "join" => string.Join("+", parameters),
            _ => "?" + name
        };

        [Theory]
        [InlineData("https://example.com/upload", "https://example.com/upload")]
        [InlineData("name={filename}", "name=my capture.png")]
        [InlineData("{upper:abc}", "ABC")]
        [InlineData("{join:a|b|c}", "a+b+c")]
        [InlineData("{upper:{filename}}", "MY CAPTURE.PNG")]
        [InlineData(@"\{filename\}", "{filename}")]
        [InlineData(@"{join:a\|b|c}", "a|b+c")]
        [InlineData(@"{upper:\d+}", @"\D+")]
        [InlineData("{ FileName }", "my capture.png")]
        public void Replace_ReplacesThePlaceholders(string text, string expected)
        {
            Assert.Equal(expected, PlaceholderSyntax.Replace(text, Call));
        }

        [Fact]
        public void Replace_UrlEncode_EscapesOnlyTheValues()
        {
            Assert.Equal("https://example.com/files/my%20capture.png?a=b", PlaceholderSyntax.Replace("https://example.com/files/{filename}?a=b", Call, true));
        }

        [Fact]
        public void Replace_UrlEncode_EscapesANestedValueOnce()
        {
            // The inner value is a parameter, only the outer result is escaped
            Assert.Equal("MY%20CAPTURE.PNG", PlaceholderSyntax.Replace("{upper:{filename}}", Call, true));
        }

        [Theory]
        [InlineData("{json:data.link")]
        [InlineData("{json")]
        [InlineData("{join:a|{upper:b}")]
        public void Replace_UnclosedPlaceholder_Throws(string text)
        {
            Assert.Throws<FormatException>(() => PlaceholderSyntax.Replace(text, Call));
        }

        [Fact]
        public void FindNames_FindsNestedNames()
        {
            Assert.Equal(new[] { "json", "regex" }, PlaceholderSyntax.FindNames("{regex:{json:a}|1} and text"));
            Assert.Empty(PlaceholderSyntax.FindNames("no placeholders"));
            Assert.Empty(PlaceholderSyntax.FindNames(null));
        }
    }
}
