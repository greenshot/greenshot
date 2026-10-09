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
        public void Parse_NoArguments_DefaultsWithoutCommand()
        {
            // Autostart: no startup options and no command
            foreach (string[] args in new[] { System.Array.Empty<string>(), null })
            {
                CommandLineOptions options = GreenshotCommandLine.Parse(args);

                Assert.NotNull(options);
                Assert.False(options.NoRun);
                Assert.False(options.Restore);
                Assert.Null(options.Language);
                Assert.Null(options.IniDirectory);
                Assert.Empty(options.CommandArguments);
            }
        }

        [Fact]
        public void Parse_FileArgument_IsTheCommand()
        {
            // The form the shell uses when Greenshot.exe was chosen via "Open with"
            CommandLineOptions options = GreenshotCommandLine.Parse([@"C:\x.greenshot"]);

            Assert.NotNull(options);
            Assert.Equal([@"C:\x.greenshot"], options.CommandArguments);
        }

        [Fact]
        public void Parse_StartupOptionsFirst_TheRestIsTheCommandUnchanged()
        {
            // --language after the command belongs to the command (e.g. a recipe argument), not to Greenshot.exe
            CommandLineOptions options = GreenshotCommandLine.Parse(["--language", "de-DE", "--no-run", "--recipe", "ocr", "--language", "en"]);

            Assert.NotNull(options);
            Assert.Equal("de-DE", options.Language);
            Assert.True(options.NoRun);
            Assert.Equal(["--recipe", "ocr", "--language", "en"], options.CommandArguments);
        }

        [Fact]
        public void Parse_OnlyStartupOptions_HasNoCommand()
        {
            CommandLineOptions options = GreenshotCommandLine.Parse(["--restore"]);

            Assert.NotNull(options);
            Assert.True(options.Restore);
            Assert.Empty(options.CommandArguments);
        }

        [Fact]
        public void Parse_ReloadAndExit_AreCommandsForTheRunningGreenshot()
        {
            Assert.Equal(["--exit"], GreenshotCommandLine.Parse(["--exit"]).CommandArguments);
            Assert.Equal(["--reload"], GreenshotCommandLine.Parse(["--reload"]).CommandArguments);
        }
    }
}
