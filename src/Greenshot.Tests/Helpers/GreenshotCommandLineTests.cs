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

using Greenshot.Helpers;
using Xunit;

namespace Greenshot.Tests.Helpers
{
    public class GreenshotCommandLineTests
    {
        [Fact]
        public void Parse_OpenFileOption_PutsPathInFiles()
        {
            // This is the form the installer registers for the .greenshot shell open command.
            CommandLineOptions options = GreenshotCommandLine.Parse(["--openfile", @"C:\x.greenshot"]);

            Assert.NotNull(options);
            Assert.Equal([@"C:\x.greenshot"], options.Files);
        }

        [Fact]
        public void Parse_PositionalFile_PutsPathInFiles()
        {
            CommandLineOptions options = GreenshotCommandLine.Parse([@"C:\x.greenshot"]);

            Assert.NotNull(options);
            Assert.Equal([@"C:\x.greenshot"], options.Files);
        }

        [Fact]
        public void Parse_OpenFileOptionAndPositionalFiles_CombinesThemInOrder()
        {
            CommandLineOptions options = GreenshotCommandLine.Parse(["--openfile", @"C:\a.greenshot", "--exit", @"C:\b.png"]);

            Assert.NotNull(options);
            Assert.True(options.Exit);
            Assert.Equal([@"C:\a.greenshot", @"C:\b.png"], options.Files);
        }
    }
}
