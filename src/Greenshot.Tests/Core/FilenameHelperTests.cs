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

using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Text from outside (window titles, web pages) made safe for file names
    /// </summary>
    public class FilenameHelperTests
    {
        [Fact]
        public void ASafeTextIsTheSameInstance()
        {
            string title = "Greenshot \u2013 Mozilla Firefox \U0001F468\u200D\U0001F469\u200D\U0001F467";
            Assert.Same(title, FilenameHelper.ToSafeNamePart(title));
        }

        [Theory]
        [InlineData("a<b>c:d\"e/f\\g|h?i*j", "a_b_c_d_e_f_g_h_i_j")]
        [InlineData("line\r\nbreak\tand\u2028separator", "line__break_and_separator")]
        [InlineData("\u0085next line", "_next line")]
        public void CharactersWindowsDoesNotAllowBecomeUnderscores(string value, string expected)
        {
            Assert.Equal(expected, FilenameHelper.ToSafeNamePart(value));
        }

        [Theory]
        // BUG-2164: Edge puts direction marks in its titles
        [InlineData("\u200EGreenshot\u200B tab\uFEFF", "Greenshot tab")]
        // The right-to-left override would show "invoicefdp.exe" as "invoiceexe.pdf"
        [InlineData("invoice\u202Efdp.exe", "invoicefdp.exe")]
        [InlineData("a\u2066b\u2069c\u061Cd\u200Fe\u00ADf", "abcdef")]
        public void InvisibleFormatCharactersAreLeftOut(string value, string expected)
        {
            Assert.Equal(expected, FilenameHelper.ToSafeNamePart(value));
        }

        [Theory]
        // Family emoji: man, joiner, woman, joiner, girl
        [InlineData("\U0001F468\u200D\U0001F469\u200D\U0001F467")]
        // Rainbow flag: white flag, variation selector, joiner, rainbow
        [InlineData("\U0001F3F3\uFE0F\u200D\U0001F308")]
        // Persian "mikonam" (I do) needs the non-joiner
        [InlineData("\u0645\u06CC\u200C\u06A9\u0646\u0645")]
        // England flag: black flag, tags g b e n g, cancel tag
        [InlineData("\U0001F3F4\U000E0067\U000E0062\U000E0065\U000E006E\U000E0067\U000E007F")]
        public void JoinersAndFlagTagsWhichBuildACharacterAreKept(string value)
        {
            Assert.Equal(value, FilenameHelper.ToSafeNamePart(value));
        }

        [Theory]
        // A joiner can't hide inside a plain word
        [InlineData("c\u200Dmd", "cmd")]
        [InlineData("\u200D\U0001F468", "\U0001F468")]
        [InlineData("\U0001F468\u200D", "\U0001F468")]
        [InlineData("\U0001F468\u200D\u200D\U0001F469", "\U0001F468\u200D\U0001F469")]
        [InlineData("\U0001F468\u200D \U0001F469", "\U0001F468 \U0001F469")]
        public void JoinersWhichJoinNothingAreLeftOut(string value, string expected)
        {
            Assert.Equal(expected, FilenameHelper.ToSafeNamePart(value));
        }

        [Theory]
        // Hidden text in tag characters ("ASCII smuggling")
        [InlineData("report\U000E0068\U000E0069", "report")]
        // Tags without the black flag or without the cancel tag
        [InlineData("a\U000E0067\U000E0062\U000E007F", "a")]
        [InlineData("\U0001F3F4\U000E0067\U000E0062", "\U0001F3F4")]
        public void TagCharactersOutsideAFlagAreLeftOut(string value, string expected)
        {
            Assert.Equal(expected, FilenameHelper.ToSafeNamePart(value));
        }

        [Fact]
        public void ALoneSurrogateBecomesAnUnderscore()
        {
            Assert.Equal("a_b", FilenameHelper.ToSafeNamePart("a\uD83Db"));
        }

        [Fact]
        public void TheTextIsNormalized()
        {
            // e with a combining acute accent becomes the single character \u00E9
            Assert.Equal("caf\u00E9", FilenameHelper.ToSafeNamePart("cafe\u0301"));
        }

        [Fact]
        public void MaxLengthNeverCutsASurrogatePairOrLeavesAJoiner()
        {
            Assert.Equal("ab", FilenameHelper.ToSafeNamePart("ab\U0001F468", 3));
            Assert.Equal("\u0645", FilenameHelper.ToSafeNamePart("\u0645\u200C\u06A9", 2));
        }

        [Fact]
        public void LongTextIsCleanedToo()
        {
            string value = new string('a', 300) + "\u202E" + new string('b', 300);
            Assert.Equal(new string('a', 300) + new string('b', 300), FilenameHelper.ToSafeNamePart(value));
        }

        [Theory]
        [InlineData("..", "__")]
        [InlineData("  spaces  ", "spaces")]
        [InlineData(null, "")]
        public void DotsOnlyAndSpacesAreHandled(string value, string expected)
        {
            Assert.Equal(expected, FilenameHelper.ToSafeNamePart(value));
        }

        [Theory]
        [InlineData("CON", "_CON")]
        [InlineData("nul.png", "_nul.png")]
        [InlineData("COM1.tar.gz", "_COM1.tar.gz")]
        [InlineData("LPT\u00B9.txt", "_LPT\u00B9.txt")]
        [InlineData("CONSOLE.png", "CONSOLE.png")]
        [InlineData("COM0.png", "COM0.png")]
        [InlineData("name. . ", "name")]
        public void AWholeFileNameHasNoReservedNameAndNoDotAtTheEnd(string value, string expected)
        {
            Assert.Equal(expected, FilenameHelper.MakeFilenameSafe(value));
        }
    }
}
