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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Helpers.Ipc;
using Greenshot.Recipes;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    /// <summary>
    /// Command line parsing (moved from the native proxy into Greenshot) and the text frame protocol for greenshot-cli.exe.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class CliProtocolTests
    {
        private static CliParseResult ParseCli(params string[] argv) => CliCommandParser.Parse(argv, IpcSources.Cli, @"C:\work");

        [Fact]
        public void Parse_RunRecipe_AcceptsAllArgumentForms()
        {
            var result = ParseCli("--recipe", "qr", "--file=a@b.png", "--offset", "-5", "lang=eng", "--json", "--query", "${Barcode.Text}", "--async", "--", "mode=--raw");

            Assert.True(result.Success, result.Error);
            var envelope = result.Envelope;
            Assert.Equal("RUN_RECIPE", envelope.Command);
            Assert.Equal("qr", envelope.Recipe);
            Assert.Equal(@"C:\work", envelope.Cwd);
            Assert.Equal("a@b.png", envelope.Parameters["file"]);
            Assert.Equal("-5", envelope.Parameters["offset"]);
            Assert.Equal("eng", envelope.Parameters["lang"]);
            Assert.Equal("--raw", envelope.Parameters["mode"]);
            Assert.True(envelope.Json);
            Assert.True(envelope.Async);
            Assert.Equal("${Barcode.Text}", envelope.Query);
            Assert.False(envelope.Parameters.ContainsKey("json"));
            Assert.False(envelope.Parameters.ContainsKey("query"));
        }

        [Fact]
        public void Parse_RunRecipe_LastDuplicateArgumentWins_CaseInsensitive()
        {
            var result = ParseCli("-r", "qr", "File=one.png", "--file", "two.png");
            Assert.True(result.Success, result.Error);
            Assert.Equal("two.png", result.Envelope.Parameters["file"]);
            Assert.Single(result.Envelope.Parameters);
        }

        [Theory]
        [InlineData("unexpected argument 'stray'", "--recipe", "qr", "stray")]
        [InlineData("missing value for argument '--file'", "--recipe", "qr", "--file")]
        [InlineData("missing recipe identifier", "--recipe")]
        [InlineData("requires an expression", "--recipe", "qr", "--query")]
        [InlineData("unexpected argument 'now'", "--reload", "now")]
        [InlineData("unrecognized option '--unknown'", "--unknown")]
        [InlineData("unexpected argument 'extra'", "--list-recipes", "extra")]
        [InlineData("invalid argument '--=x'", "--recipe", "qr", "--=x")]
        public void Parse_InvalidCommandLines_ReportUsageErrors(string expectedMessage, params string[] argv)
        {
            var result = ParseCli(argv);
            Assert.False(result.Success);
            Assert.StartsWith("Error: ", result.Error);
            Assert.Contains(expectedMessage, result.Error);
            Assert.Contains("greenshot-cli --help", result.Error);
        }

        [Fact]
        public void Parse_InvalidCommandLineWithJson_IsReportedAsJson()
        {
            var result = ParseCli("--recipe", "qr", "stray", "--json");
            Assert.False(result.Success);
            Assert.True(result.Json);
        }

        [Theory]
        [InlineData("LIST_RECIPES", "--list-recipes")]
        [InlineData("DESCRIBE_RECIPE", "--info", "qr")]
        [InlineData("OPEN_FILE", "--file", "a.png", "b.png")]
        [InlineData("OPEN_FILE", "a.png")]
        [InlineData("RELOAD_CONFIG", "--reload")]
        [InlineData("EXIT", "--exit")]
        [InlineData("URL_SCHEME", "greenshot://settings")]
        public void Parse_CliCommands_MapToDispatcherCommands(string expectedCommand, params string[] argv)
        {
            var result = ParseCli(argv);
            Assert.True(result.Success, result.Error);
            Assert.Equal(expectedCommand, result.Envelope.Command);
        }

        [Fact]
        public void Parse_UrlSchemeSource_AcceptsOnlyASingleUrl()
        {
            var ok = CliCommandParser.Parse(new[] { "greenshot://recipe/qr" }, IpcSources.UrlScheme, null);
            Assert.True(ok.Success);
            Assert.Equal("URL_SCHEME", ok.Envelope.Command);
            Assert.Equal("greenshot://recipe/qr", ok.Envelope.RawInput);

            Assert.False(CliCommandParser.Parse(new[] { "--exit" }, IpcSources.UrlScheme, null).Success);
            Assert.False(CliCommandParser.Parse(new[] { "greenshot://a", "greenshot://b" }, IpcSources.UrlScheme, null).Success);
        }

        [Fact]
        public void Parse_OpenWithSource_AcceptsOnlyFiles()
        {
            var ok = CliCommandParser.Parse(new[] { "--file", @"C:\a b\c.png" }, IpcSources.OpenWith, null);
            Assert.True(ok.Success);
            Assert.Equal("OPEN_FILE", ok.Envelope.Command);
            Assert.Equal(new[] { @"C:\a b\c.png" }, ok.Envelope.Files);

            // Everything is taken as a file name, so no other command can be smuggled in
            var files = CliCommandParser.Parse(new[] { "--exit" }, IpcSources.OpenWith, null);
            Assert.True(files.Success);
            Assert.Equal("OPEN_FILE", files.Envelope.Command);
        }

        [Fact]
        public void Parse_NativeMessagingSource_IsRejected()
        {
            Assert.False(CliCommandParser.Parse(new[] { "--exit" }, IpcSources.NativeMessaging, null).Success);
        }

        private static List<(char Type, string Text, int ExitCode)> ReadTextFrames(MemoryStream ms)
        {
            var frames = new List<(char, string, int)>();
            ms.Position = 0;
            var lengthBytes = new byte[4];
            while (ms.Read(lengthBytes, 0, 4) == 4)
            {
                var payload = new byte[BitConverter.ToUInt32(lengthBytes, 0)];
                ms.Read(payload, 0, payload.Length);
                char type = (char)payload[0];
                if (type == 'X')
                {
                    frames.Add((type, null, BitConverter.ToInt32(payload, 1)));
                }
                else
                {
                    frames.Add((type, Encoding.UTF8.GetString(payload, 1, payload.Length - 1), 0));
                }
            }
            return frames;
        }

        [Fact]
        public async Task TextFrames_StreamsAndFinalReply_AreTranslated()
        {
            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(new IpcEnvelope { Command = "RUN_RECIPE" }, ms) { UsesTextFrames = true };
                await context.ReplyAsync(new { stream = "stdout", text = "line \ud83d\ude80" });
                await context.ReplyAsync(new { stream = "stderr", text = "warning\n" });
                await context.ReplyAsync(new { status = "error", exit_code = 0, stdout = (string)null, stderr = "failed" });
                // Only one final reply is sent
                await context.ReplyAsync(new { status = "ok", exit_code = 0, stdout = "ignored" });
                await context.CompleteAsync(5);

                var frames = ReadTextFrames(ms);
                Assert.Equal(4, frames.Count);
                Assert.Equal(('O', "line \ud83d\ude80\n", 0), frames[0]);
                Assert.Equal(('E', "warning\n", 0), frames[1]);
                Assert.Equal(('E', "failed\n", 0), frames[2]);
                Assert.Equal('X', frames[3].Type);
                Assert.Equal(1, frames[3].ExitCode); // status "error" never exits with 0
            }
        }

        [Fact]
        public async Task TextFrames_JsonRequest_PrintsTheReplyDocument()
        {
            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(new IpcEnvelope { Command = "RUN_RECIPE", Json = true }, ms) { UsesTextFrames = true };
                await context.ReplyAsync(new { status = "ok", exit_code = 3, stdout = "42" });

                var frames = ReadTextFrames(ms);
                Assert.Equal(2, frames.Count);
                Assert.Equal('O', frames[0].Type);
                var document = JObject.Parse(frames[0].Text);
                Assert.Equal("42", document.Value<string>("stdout"));
                Assert.Equal(('X', (string)null, 3), frames[1]);
            }
        }

        [Fact]
        public async Task TextFrames_CompleteWithoutReply_SendsExitFrameOnce()
        {
            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(new IpcEnvelope { Command = "TAB_CHANGED" }, ms) { UsesTextFrames = true };
                await context.CompleteAsync();
                await context.CompleteAsync();
                Assert.Equal(new[] { ('X', (string)null, 0) }, ReadTextFrames(ms));
            }
        }

        private static async Task<List<(char Type, string Text, int ExitCode)>> DispatchCliAsync(string source, params string[] argv)
        {
            using (var ms = new MemoryStream())
            {
                var envelope = new IpcEnvelope { Command = "CLI", Source = source, Argv = argv.ToList(), Cwd = Path.GetTempPath() };
                var context = new IpcRequestContext(envelope, ms) { UsesTextFrames = true };
                await IpcSecurityDispatcher.DispatchAsync(context, null, () => { }, () => { }, () => { }, f => { });
                await context.CompleteAsync();
                return ReadTextFrames(ms);
            }
        }

        [Fact]
        public async Task Dispatch_Cli_ListRecipes_PrintsTextTable()
        {
            var recipe = new CaptureRecipe("recipe_cli_text_list", "CLI Text List")
                .AddNode(new RecipeNodeConfig { Id = "s1", StepType = WellKnownStepTypes.Source, Parameters = new Dictionary<string, object> { ["SourceType"] = CaptureSourceType.Clipboard } })
                .AddTrigger(TriggerConfig.CreateCommandline("cli-text-list", "Lists things", arguments: new[]
                {
                    new CommandlineArgument { Name = "file", Variable = "Filename", Required = true, Description = "Image" }
                }));
            RecipeManager.Instance.RegisterRecipe(recipe);

            var frames = await DispatchCliAsync(IpcSources.Cli, "--list-recipes");

            Assert.Equal('O', frames[0].Type);
            Assert.Contains("Available recipes with CommandlineTrigger", frames[0].Text);
            Assert.Contains("cli-text-list", frames[0].Text);
            Assert.Contains("--file", frames[0].Text);
            Assert.Contains("${Filename}", frames[0].Text);
            Assert.Equal(('X', (string)null, 0), frames.Last());
        }

        [Fact]
        public async Task Dispatch_Cli_UsageError_ExitsWithCode2()
        {
            var frames = await DispatchCliAsync(IpcSources.Cli, "--recipe", "qr", "stray");

            Assert.Equal('E', frames[0].Type);
            Assert.StartsWith("Error: unexpected argument 'stray'", frames[0].Text);
            Assert.Equal(('X', (string)null, 2), frames.Last());
        }

        [Fact]
        public async Task Dispatch_Cli_FromUrlScheme_CannotReachExit()
        {
            bool exitCalled = false;
            using (var ms = new MemoryStream())
            {
                var envelope = new IpcEnvelope { Command = "CLI", Source = IpcSources.UrlScheme, Argv = new List<string> { "greenshot://exit" } };
                var context = new IpcRequestContext(envelope, ms) { UsesTextFrames = true };
                await IpcSecurityDispatcher.DispatchAsync(context, null, () => exitCalled = true, () => { }, () => { }, f => { });

                var frames = ReadTextFrames(ms);
                Assert.False(exitCalled);
                Assert.Contains(frames, f => f.Type == 'E' && f.Text.Contains("[SECURITY]"));
                Assert.Equal(1, frames.Last().ExitCode);
            }
        }
    }
}
