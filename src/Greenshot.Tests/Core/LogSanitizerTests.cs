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
using System.IO;
using Greenshot.Base.Core;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Window titles, file names or clipboard text in the log can't hide or reorder text, and one entry can't fill the log.
    /// The invisible characters are built from their numbers, so this file doesn't contain them.
    /// </summary>
    public class LogSanitizerTests
    {
        private const string Pattern = "%date{yyyy-MM-dd HH:mm:ss,fff} [%thread] %-5level - [%logger] %m%n%exception";

        [Fact]
        public void Escape_PlainText_IsTheSameString()
        {
            string text = "Capturing window 'Untitled - Notepad' (1190x795)\r\n\tnext line";
            Assert.Same(text, LogSanitizer.EscapeUnsafeCharacters(text));
        }

        [Theory]
        [InlineData(0x202E)] // right-to-left override
        [InlineData(0x202A)] // left-to-right embedding
        [InlineData(0x2066)] // left-to-right isolate
        [InlineData(0x2069)] // pop directional isolate
        [InlineData(0x200B)] // zero width space
        [InlineData(0x200F)] // right-to-left mark
        [InlineData(0x061C)] // Arabic letter mark
        [InlineData(0xFEFF)] // zero width no-break space
        [InlineData(0x2028)] // line separator
        [InlineData(0x0085)] // next line
        [InlineData(0x0000)] // NUL
        [InlineData(0x001B)] // escape (terminal colors)
        [InlineData(0x007F)] // delete
        public void Escape_UnsafeCharacter_IsWrittenAsItsNumber(int codePoint)
        {
            string text = "a" + (char) codePoint + "b";

            Assert.Equal($"a\\u{codePoint:X4}b", LogSanitizer.EscapeUnsafeCharacters(text));
        }

        [Fact]
        public void Escape_KeepsNormalUnicode()
        {
            string text = "Größe: 100 % Привет 你好 \U0001F600";
            Assert.Same(text, LogSanitizer.EscapeUnsafeCharacters(text));
        }

        [Fact]
        public void Limit_LongText_IsCutWithANote()
        {
            string result = LogSanitizer.Limit(new string('x', 120), 100);

            Assert.Equal(new string('x', 100) + " ... (20 more characters)", result);
        }

        [Fact]
        public void Limit_KeepsSurrogatePairsWhole()
        {
            // An emoji (two chars) at the limit is left out as a whole
            string text = new string('x', 99) + "\U0001F600" + "rest";

            Assert.Equal(new string('x', 99) + " ... (6 more characters)", LogSanitizer.Limit(text, 100));
        }

        [Fact]
        public void EntryLayout_TitleWithRightToLeftOverride_IsEscaped()
        {
            var layout = CreateEntryLayout(Pattern);
            // Shown as "...gpj.exe" reversed: a file which looks like a picture
            string title = "photo" + (char) 0x202E + "gpj.exe";

            string written = Render(layout, $"Capturing window '{title}'");

            Assert.Contains("photo\\u202Egpj.exe", written);
            // By character: a culture-aware string search ignores invisible characters and always finds them
            Assert.Equal(-1, written.IndexOf((char) 0x202E));
        }

        [Fact]
        public void EntryLayout_VeryLongMessage_IsCutAndEndsTheLine()
        {
            var layout = CreateEntryLayout(Pattern);

            string written = Render(layout, new string('x', LogSanitizer.MaxEntryLength * 2));

            Assert.True(written.Length < LogSanitizer.MaxEntryLength + 100);
            Assert.EndsWith("more characters)" + Environment.NewLine, written);
        }

        [Fact]
        public void EntryLayout_LayoutWithoutException_WritesTheExceptionIndented()
        {
            // A log4net.xml without %exception: the exception is still indented, it can't fake an entry
            var layout = CreateEntryLayout("%date{yyyy-MM-dd HH:mm:ss,fff} %m%n");
            var exception = new InvalidOperationException("Texture\n2026-10-16 09:00:00,000 [1] ERROR - fake");

            string written = Render(layout, "Failed", exception);

            Assert.False(layout.IgnoresException);
            Assert.Contains("\n" + EntryLayout.Indent + "2026-10-16 09:00:00,000 [1] ERROR - fake", written);
            Assert.Contains(EntryLayout.Indent + "System.InvalidOperationException: Texture", written);
        }

        private static EntryLayout CreateEntryLayout(string pattern)
        {
            var patternLayout = new PatternLayout(pattern);
            patternLayout.ActivateOptions();
            return new EntryLayout(patternLayout);
        }

        private static string Render(ILayout layout, string message, Exception exception = null)
        {
            var loggingEvent = new LoggingEvent(typeof(LogSanitizerTests), new Hierarchy(), "Test", Level.Info, message, exception);
            using var writer = new StringWriter();
            layout.Format(writer, loggingEvent);
            return writer.ToString();
        }
    }
}
