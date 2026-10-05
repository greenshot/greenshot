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
using System.IO;
using System.Windows.Forms;
using Greenshot.Helpers;
using Xunit;
using System.Linq;

namespace Greenshot.Tests.Core
{
    public class NotifyIconTextHelperTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\r\n\t")]
        public void ToNotifyIconText_NullOrWhitespace_ReturnsFallback(string text)
        {
            Assert.Equal("Greenshot", NotifyIconTextHelper.ToNotifyIconText(text));
        }

        [Theory]
        [InlineData(62)]
        [InlineData(63)]
        public void ToNotifyIconText_FitsLimit_ReturnsUnchanged(int length)
        {
            string text = new string('a', length);
            Assert.Equal(text, NotifyIconTextHelper.ToNotifyIconText(text));
        }

        [Theory]
        [InlineData(64)]
        [InlineData(80)]
        public void ToNotifyIconText_TooLongWithoutSpaces_IsCutWithEllipsis(int length)
        {
            string result = NotifyIconTextHelper.ToNotifyIconText(new string('a', length));
            Assert.Equal(NotifyIconTextHelper.MaxLength, result.Length);
            Assert.EndsWith("…", result);
        }

        [Fact]
        public void ToNotifyIconText_TooLong_CutsAtWordBoundary()
        {
            string result = NotifyIconTextHelper.ToNotifyIconText("Greenshot - Uno straordinario strumento per copiare immagini dallo schermo");
            Assert.Equal("Greenshot - Uno straordinario strumento per copiare immagini…", result);
        }

        [Fact]
        public void ToNotifyIconText_CollapsesWhitespaceAndNewlines()
        {
            Assert.Equal("Greenshot - the tool", NotifyIconTextHelper.ToNotifyIconText("\r\n\t\tGreenshot -\r\n  the   tool\r\n"));
        }

        [Fact]
        public void ToNotifyIconText_DoesNotSplitSurrogatePair()
        {
            // 61 characters followed by an emoji (surrogate pair) which would be split at the cut position
            string text = new string('a', 61) + "\U0001F600" + new string('b', 10);
            string result = NotifyIconTextHelper.ToNotifyIconText(text);
            Assert.True(result.Length <= NotifyIconTextHelper.MaxLength);
            Assert.False(char.IsHighSurrogate(result[result.Length - 2]));
        }

        [Fact]
        public void ToNotifyIconText_AllApplicationTitles_AreAcceptedByNotifyIcon()
        {
            var languageDirectory = FindLanguageDirectory();
            var languageFiles = Directory.GetFiles(languageDirectory, "greenshot.*.ini");
            Assert.NotEmpty(languageFiles);

            using var notifyIcon = new NotifyIcon();
            foreach (var languageFile in languageFiles)
            {
                string titleLine = File.ReadAllLines(languageFile).FirstOrDefault(l => l.StartsWith("application_title=", StringComparison.Ordinal));
                if (titleLine == null)
                {
                    continue;
                }

                string result = NotifyIconTextHelper.ToNotifyIconText(titleLine.Substring("application_title=".Length).Trim());
                Assert.True(result.Length <= NotifyIconTextHelper.MaxLength, $"{Path.GetFileName(languageFile)}: '{result}' is {result.Length} characters");
                // Throws an ArgumentOutOfRangeException when the text is too long
                notifyIcon.Text = result;
            }
        }

        private static string FindLanguageDirectory()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Greenshot", "Languages");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
                candidate = Path.Combine(directory.FullName, "src", "Greenshot", "Languages");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("Could not find the Greenshot Languages directory from " + AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
