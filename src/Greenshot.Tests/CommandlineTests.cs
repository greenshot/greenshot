// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using CommandLine;
using Greenshot.Configuration;
using Xunit;

namespace Greenshot.Tests
{
    public class CommandlineTests
    {
        [Fact]
        public void TestLanguageShortOption()
        {
            var arguments = new []{"-l", "de-DE"};

            CommandlineCaptureOptions captureOptions = null;
            CommandlineOptions commandlineOptions = null;

            Parser.Default.ParseArguments<CommandlineOptions>(arguments)
                .WithParsed(opts => commandlineOptions = opts)
                .WithNotParsed(errs => throw new Exception(string.Join(",", errs.Select(e => e.Tag))));

            Assert.Null(captureOptions);
            Assert.NotNull(commandlineOptions);
            Assert.Equal("de-DE", commandlineOptions.Language);
        }

        [Fact]
        public void TestLanguageOption()
        {
            var arguments = new[] { "--language", "de-DE" };

            CommandlineCaptureOptions captureOptions = null;
            CommandlineOptions commandlineOptions = null;

            Parser.Default.ParseArguments<CommandlineOptions>(arguments)
                .WithParsed(opts => commandlineOptions = opts)
                .WithNotParsed(errs => throw new Exception(string.Join(",", errs.Select(e => e.Tag))));

            Assert.Null(captureOptions);
            Assert.NotNull(commandlineOptions);
            Assert.Equal("de-DE", commandlineOptions.Language);
        }

        [Fact]
        public void TestCaptureParser()
        {
            var arguments = new[] { "capture", "-s", "fullscreen", "-d", "Clipboard" };

            CommandlineCaptureOptions captureOptions = null;
            CommandlineOptions commandlineOptions = null;

            Parser.Default.ParseArguments<CommandlineOptions, CommandlineCaptureOptions>(arguments)
                .WithParsed<CommandlineCaptureOptions>(opts => captureOptions = opts)
                .WithParsed<CommandlineOptions>(opts => commandlineOptions = opts)
                .WithNotParsed(errs => throw new Exception(string.Join(",", errs.Select(e => e.Tag))));

            Assert.NotNull(captureOptions);
            Assert.Null(commandlineOptions);

            Assert.NotEmpty(captureOptions.Destinations);
            Assert.NotEmpty(captureOptions.Source);

            Assert.Equal("fullscreen", captureOptions.Source);
            Assert.Contains("Clipboard", captureOptions.Destinations);
        }
    }
}
