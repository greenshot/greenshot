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

using System.Collections;
using System.Globalization;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Naming;

namespace Greenshot.Linux.Tests.Naming;

public class FilenamePatternTests
{
    private static readonly CaptureDetails Details = new()
    {
        DateTime = new DateTime(2026, 3, 7, 9, 5, 4),
        Title = "Terminal: ~/src",
    };

    private static FilenamePattern Create(uint number = 41, IDictionary? environment = null) =>
        new(() => number + 1, environment ?? new Hashtable { ["GS_TEST"] = "env-value" }) { Culture = CultureInfo.InvariantCulture };

    [Theory]
    [InlineData("${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title}", "2026-03-07 09_05_04-Terminal: ~_src")]
    [InlineData("${YYYY}${MM}${DD}-${hh}${mm}${ss}", "20260307-090504")]
    [InlineData("${capturetime}", "2026-03-07 09-05-04")]
    [InlineData("${NUM}", "000042")]
    [InlineData("${NUM:p3,x}", "42x")]
    [InlineData("${title:s0,8}", "Terminal")]
    [InlineData("${title:s-3}", "src")]
    [InlineData("${title:r ,_}", "Terminal:_~_src")]
    [InlineData("${GS_TEST}", "env-value")]
    [InlineData("${unknown}", "")]
    public void Fill_matches_the_windows_variables(string pattern, string expected)
    {
        Assert.Equal(expected, Create().Fill(pattern, Details));
    }

    [Fact]
    public void Fill_without_filename_safety_keeps_slashes()
    {
        Assert.Equal("Terminal: ~/src", Create().Fill("${title}", Details, filenameSafe: false));
    }

    [Fact]
    public void Random_variable_has_its_length()
    {
        var value = Create().Fill("${RRRRRR}", Details);
        Assert.Equal(6, value.Length);
        Assert.All(value, c => Assert.True(char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void Missing_title_uses_Greenshot()
    {
        Assert.Equal("Greenshot", Create().Fill("${title}", new CaptureDetails()));
    }

    [Fact]
    public void Long_titles_are_cut_at_80_characters()
    {
        var details = new CaptureDetails { Title = new string('a', 100) };
        Assert.Equal(80, Create().Fill("${title}", details).Length);
    }

    [Fact]
    public void Empty_pattern_falls_back_like_windows()
    {
        Assert.Equal("greenshot 2026-03-07 09-05-04.png", Create().GetFilename("  ", "png", Details));
    }

    [Fact]
    public void Substring_outside_the_value_keeps_the_value()
    {
        Assert.Equal("Terminal: ~_src", Create().Fill("${title:s5,100}", Details));
    }
}
