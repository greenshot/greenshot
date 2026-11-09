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
using Greenshot.Base.Expressions;
using Xunit;

namespace Greenshot.Tests.Expressions
{
    public class FolderTemplateTests
    {
        [Fact]
        public void FolderTemplateOf_APathInAKnownFolder_UsesItsValue()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Assert.SkipWhen(string.IsNullOrEmpty(desktop), "No desktop folder here");

            Assert.Equal("${user.desktop}", OutputTemplates.FolderTemplateOf(desktop));
            Assert.Equal("${user.desktop}\\Greenshots", OutputTemplates.FolderTemplateOf(desktop + "\\Greenshots"));
        }

        [Fact]
        public void FolderTemplateOf_ANameWhichOnlyStartsLikeAKnownFolder_IsNotInIt()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Assert.SkipWhen(string.IsNullOrEmpty(desktop), "No desktop folder here");

            // Next to the desktop (e.g. in the profile), not in it
            Assert.DoesNotContain("${user.desktop}", OutputTemplates.FolderTemplateOf(desktop + "Old"));
        }

        [Theory]
        [InlineData(@"\\server\share\Screenshots")]
        [InlineData("${user.pictures}\\Greenshots")]
        [InlineData("")]
        public void FolderTemplateOf_ATemplateOrAPathElsewhere_StaysAsItIs(string path)
        {
            Assert.Equal(path, OutputTemplates.FolderTemplateOf(path));
        }
    }
}
