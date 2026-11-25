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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Plugin.ExternalCommand.Recipes;
using Greenshot.Plugin.ExternalCommand.Running;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// What a command gets (arguments, standard input) and gives back (exit code, output, error), with the programs of Windows
    /// </summary>
    public class CommandRunnerTests
    {
        private static string Cmd => Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\cmd.exe");

        private static void SkipWithoutWindows() =>
            Assert.SkipWhen(Environment.OSVersion.Platform != PlatformID.Win32NT, "Runs the programs of Windows");

        [Fact]
        public async Task Output_ExitCodeAndError_AreTheResult()
        {
            SkipWithoutWindows();
            var result = await CommandRunner.RunAsync(new CommandRequest
            {
                Program = Cmd,
                Arguments = "/c echo out https://example.com/x& echo problem 1>&2& exit 3"
            }, TestContext.Current.CancellationToken);

            Assert.Equal(3, result.ExitCode);
            Assert.False(result.Succeeded);
            Assert.Contains("out https://example.com/x", result.Output);
            Assert.Equal("https://example.com/x", result.Url);
            Assert.Contains("problem", result.Error);
            Assert.Equal("problem", result.FailureMessage);
        }

        [Fact]
        public async Task SuccessExitCodes_DecideWhatSucceeds()
        {
            SkipWithoutWindows();
            var result = await CommandRunner.RunAsync(new CommandRequest
            {
                Program = Cmd,
                Arguments = "/c exit 98",
                SuccessExitCodes = CommandRunner.ParseExitCodes("0, 98,99")
            }, TestContext.Current.CancellationToken);

            Assert.Equal(98, result.ExitCode);
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task StandardInput_ReachesTheCommand()
        {
            SkipWithoutWindows();
            // findstr with a pattern which matches every line writes its input to its output
            var result = await CommandRunner.RunAsync(new CommandRequest
            {
                Program = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\findstr.exe"),
                Arguments = "\"^\"",
                StandardInput = Encoding.ASCII.GetBytes("a \" & | text\r\n")
            }, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded, result.FailureMessage);
            Assert.Equal("a \" & | text", result.Output.Trim());
        }

        [Fact]
        public async Task Output_None_IsNotRead()
        {
            SkipWithoutWindows();
            var result = await CommandRunner.RunAsync(new CommandRequest
            {
                Program = Cmd,
                Arguments = "/c echo nobody reads this",
                Output = CommandOutput.None,
                ReadError = false
            }, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Null(result.Output);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task Timeout_StopsTheCommand()
        {
            SkipWithoutWindows();
            var watch = Stopwatch.StartNew();
            var result = await CommandRunner.RunAsync(new CommandRequest
            {
                Program = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\ping.exe"),
                Arguments = "-n 60 127.0.0.1",
                Timeout = TimeSpan.FromSeconds(1)
            }, TestContext.Current.CancellationToken);

            Assert.True(result.TimedOut);
            Assert.False(result.Succeeded);
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30), $"took {watch.Elapsed}");
        }

        [Fact]
        public async Task Cancel_StopsTheCommand()
        {
            SkipWithoutWindows();
            using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var watch = Stopwatch.StartNew();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CommandRunner.RunAsync(new CommandRequest
            {
                Program = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\ping.exe"),
                Arguments = "-n 60 127.0.0.1"
            }, cancel.Token));
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30), $"took {watch.Elapsed}");
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, "the output https://example.com")]
        [InlineData(false, true, "https://example.com")]
        [InlineData(true, true, "https://example.com")]
        public void Clipboard_GetsTheUrlBeforeTheOutput(bool copyOutput, bool copyUrl, string expected)
        {
            var result = new CommandResult { Output = "the output https://example.com", Url = "https://example.com" };
            Assert.Equal(expected, CommandRunner.ClipboardTextOf(result, copyOutput, copyUrl));
        }

        [Fact]
        public void Clipboard_CopyUrl_WithoutUrl_CopiesTheOutputOnlyWhenWanted()
        {
            var result = new CommandResult { Output = "no link" };
            Assert.Null(CommandRunner.ClipboardTextOf(result, false, true));
            Assert.Equal("no link", CommandRunner.ClipboardTextOf(result, true, true));
        }

        [Fact]
        public void ExitCodes_AreParsed()
        {
            Assert.Equal(new[] { 0 }, CommandRunner.ParseExitCodes(null));
            Assert.Equal(new[] { 0, 98, -1 }, CommandRunner.ParseExitCodes("0, 98;-1"));
            Assert.Throws<FormatException>(() => CommandRunner.ParseExitCodes("0,x"));
        }

        [Fact]
        public void Encoding_Utf8_HasNoByteOrderMark()
        {
            Assert.Empty(ExternalCommandStep.EncodingOf(null).GetPreamble());
            Assert.Empty(ExternalCommandStep.EncodingOf("UTF-8").GetPreamble());
            Assert.Equal(Encoding.GetEncoding(1252).CodePage, ExternalCommandStep.EncodingOf("windows-1252").CodePage);
        }

        [Fact]
        public void ResultNames_FollowTheParameter()
        {
            var contract = new VariableContract("{Parameter:ResultName|command}.exitCode");
            Assert.Equal(new[] { "command.exitCode" }, contract.ResolveNames(new RecipeNodeConfig("cmd", "ExternalCommand")));
            var named = new RecipeNodeConfig("cmd", "ExternalCommand") { Parameters = new Dictionary<string, object> { { "ResultName", "ocr" } } };
            Assert.Equal(new[] { "ocr.exitCode" }, contract.ResolveNames(named));
        }
    }
}
