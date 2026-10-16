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

using Greenshot.Base.Core;
using Greenshot.SelfService;
using log4net.Core;
using Xunit;

namespace Greenshot.Tests.SelfService
{
    public class LogSearchTests
    {
        private const string Log =
            "2026-10-16 09:00:00,000 [1] INFO  - [Startup] Greenshot started\n" +
            "2026-10-16 09:00:01,000 [1] WARN  - [Capture] Capture failed\n" +
            "System.InvalidOperationException: HDR texture lost\n" +
            "   at Greenshot.Capture()\n" +
            "2026-10-16 09:00:02,000 [1] INFO  - [Capture] HDR tone mapping 2560x1440\n" +
            "2026-10-16 09:00:03,000 [1] INFO  - [Startup] Ready";

        [Fact]
        public void Filter_KeepsWholeEntries_WithTheirExceptionLines()
        {
            string result = LogSearch.Filter(Log, "texture", out int matches, out int entries);

            Assert.Equal(1, matches);
            Assert.Equal(4, entries);
            Assert.Equal(
                "2026-10-16 09:00:01,000 [1] WARN  - [Capture] Capture failed\n" +
                "System.InvalidOperationException: HDR texture lost\n" +
                "   at Greenshot.Capture()\n", result);
        }

        [Fact]
        public void Filter_IgnoresCase_AndKeepsTheOrder()
        {
            string result = LogSearch.Filter(Log, "hdr", out int matches, out _);

            Assert.Equal(2, matches);
            Assert.True(result.IndexOf("Capture failed") < result.IndexOf("HDR tone mapping"));
            Assert.DoesNotContain("[Startup]", result);
        }

        [Fact]
        public void Filter_LastEntryWithoutNewline_EndsWithOne()
        {
            string result = LogSearch.Filter(Log, "Ready", out int matches, out _);

            Assert.Equal(1, matches);
            Assert.Equal("2026-10-16 09:00:03,000 [1] INFO  - [Startup] Ready" + System.Environment.NewLine, result);
        }

        [Fact]
        public void Filter_TextBeforeTheFirstEntry_IsAnEntry()
        {
            string result = LogSearch.Filter("leftover of a rolled file\n" + Log, "leftover", out int matches, out int entries);

            Assert.Equal(1, matches);
            Assert.Equal(5, entries);
            Assert.Equal("leftover of a rolled file\n", result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Filter_WithoutSearch_ReturnsTheLog(string search)
        {
            Assert.Equal(Log, LogSearch.Filter(Log, search, out _, out _));
        }

        [Fact]
        public void Filter_NoMatch_IsEmpty()
        {
            Assert.Equal(string.Empty, LogSearch.Filter(Log, "nothing like this", out int matches, out _));
            Assert.Equal(0, matches);
        }

        [Fact]
        public void LogViewerAppender_CollectsEntries_OnlyWhileAttached()
        {
            var logger = log4net.LogManager.GetLogger(typeof(LogSearchTests));
            var appender = LogViewerAppender.Attach();
            try
            {
                logger.Error("Log viewer appender test entry");
                string entries = appender.TakeEntries();
                Assert.Contains("Log viewer appender test entry", entries);
                Assert.Contains("ERROR", entries);
                Assert.Equal(string.Empty, appender.TakeEntries());
            }
            finally
            {
                appender.Detach();
            }

            logger.Error("Written after the log viewer closed");
            Assert.Equal(string.Empty, appender.TakeEntries());
        }

        [Fact]
        public void RootLevel_Change_AppliesToTheLoggers()
        {
            var original = LogHelper.RootLevel;
            try
            {
                LogHelper.RootLevel = Level.Debug;
                Assert.Equal(Level.Debug, LogHelper.RootLevel);
                Assert.True(log4net.LogManager.GetLogger(typeof(LogSearchTests)).IsDebugEnabled);

                LogHelper.RootLevel = Level.Error;
                Assert.False(log4net.LogManager.GetLogger(typeof(LogSearchTests)).IsInfoEnabled);
            }
            finally
            {
                LogHelper.RootLevel = original;
            }
        }
    }
}
