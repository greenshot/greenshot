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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The built-in title fixes take the browser name off the window title, also in files of an earlier version
    /// </summary>
    public class CoreConfigurationTitleFixTests
    {
        public CoreConfigurationTitleFixTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Defaults_HaveFirefoxChromeAndEdge()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();

            config.OnAfterLoad();

            Assert.Equal(new List<string> { "Firefox", "Chrome", "Edge" }, config.ActiveTitleFixes);
            Assert.True(config.TitleFixMatcher.ContainsKey("Edge"));
            Assert.Equal(string.Empty, config.TitleFixReplacer["Edge"]);
        }

        [Fact]
        public void FileOfAnEarlierVersion_GetsEdge_AndKeepsItsOwnFixes()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.ActiveTitleFixes = new List<string> { "Firefox", "Chrome", "Mine" };
            config.TitleFixMatcher = new Dictionary<string, string>
            {
                ["Firefox"] = " - Mozilla Firefox.*",
                ["Chrome"] = " - Google Chrome.*",
                ["Mine"] = "^Mine ",
            };
            config.TitleFixReplacer = new Dictionary<string, string>
            {
                ["Firefox"] = string.Empty,
                ["Chrome"] = string.Empty,
                ["Mine"] = "Yours ",
            };

            config.OnAfterLoad();

            Assert.Equal(new List<string> { "Firefox", "Chrome", "Mine", "Edge" }, config.ActiveTitleFixes);
            Assert.Equal("^Mine ", config.TitleFixMatcher["Mine"]);
            Assert.Equal("Yours ", config.TitleFixReplacer["Mine"]);
            Assert.True(config.TitleFixMatcher.ContainsKey("Edge"));
        }

        [Fact]
        public void EdgeSwitchedOff_StaysOff()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.OnAfterLoad();

            config.ActiveTitleFixes.Remove("Edge");
            config.OnAfterLoad();

            Assert.DoesNotContain("Edge", config.ActiveTitleFixes);
        }

        [Theory]
        // Edge puts a zero width space (U+200B) between "Microsoft" and " Edge"
        [InlineData("Report and 12 more pages - Personal - Microsoft​ Edge", "Report")]
        [InlineData("Report and 1 more page - Work - Microsoft​ Edge", "Report")]
        [InlineData("Report - Personal - Microsoft​ Edge", "Report")]
        [InlineData("Report - Microsoft​ Edge", "Report")]
        [InlineData("Report - Microsoft Edge", "Report")]
        [InlineData("Part A - Part B - Personal - Microsoft​ Edge", "Part A - Part B")]
        [InlineData("Report - Mozilla Firefox", "Report")]
        [InlineData("Report - Google Chrome", "Report")]
        [InlineData("Microsoft Edge release notes - Google Chrome", "Microsoft Edge release notes")]
        [InlineData("Notepad", "Notepad")]
        public void BuiltInFixes_TakeTheBrowserOff(string windowTitle, string expected)
        {
            string title = windowTitle;
            foreach (var (_, matcher, replacer) in CoreConfigurationImpl.BuiltInTitleFixes)
            {
                title = Regex.Replace(title, matcher, replacer);
            }
            Assert.Equal(expected, title);
        }
    }
}
