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
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Ipc;
using Greenshot.Ipc.Cli;
using Greenshot.Recipes;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    /// <summary>
    /// Arguments passed to a recipe (CLI, greenshot: URL, extension) are only accepted when the recipe declares them,
    /// and are validated / converted according to their declared type.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class CommandlineArgumentBinderTests
    {
        private static readonly string Cwd = Path.GetTempPath();

        private static ArgumentBindingResult Bind(IEnumerable<CommandlineArgument> declared, Dictionary<string, string> supplied, string source = IpcSources.Cli)
        {
            return CommandlineArgumentBinder.Bind(declared.ToList(), supplied, "test-recipe", Cwd, source);
        }

        [Fact]
        public void Bind_UndeclaredArgument_IsRejected()
        {
            var result = Bind(new[] { new CommandlineArgument { Name = "file", Type = ContractDataType.FilePath } },
                new Dictionary<string, string> { ["file"] = "a.png", ["OverrideDestinations"] = "Clipboard" });

            Assert.False(result.Success);
            Assert.Contains("unknown argument 'OverrideDestinations'", result.Error);
            Assert.Contains("--file", result.Error);
        }

        [Fact]
        public void Bind_RecipeWithoutDeclaredArguments_AcceptsNone()
        {
            var result = Bind(new CommandlineArgument[0], new Dictionary<string, string> { ["CaptureDelay"] = "5000" });

            Assert.False(result.Success);
            Assert.Contains("does not accept arguments", result.Error);
        }

        [Fact]
        public void Bind_StoresValueOnlyUnderTheDeclaredVariable()
        {
            var result = Bind(new[] { new CommandlineArgument { Name = "lang", Variable = "OcrLanguage" } },
                new Dictionary<string, string> { ["LANG"] = "deu" });

            Assert.True(result.Success, result.Error);
            Assert.Equal("deu", result.Variables["OcrLanguage"]);
            Assert.False(result.Variables.ContainsKey("lang"));
        }

        [Fact]
        public void Bind_RequiredAndDefault_AreApplied()
        {
            var declared = new[]
            {
                new CommandlineArgument { Name = "file", Required = true, Type = ContractDataType.FilePath },
                new CommandlineArgument { Name = "format", DefaultValue = "png" }
            };

            var missing = Bind(declared, new Dictionary<string, string>());
            Assert.False(missing.Success);
            Assert.Contains("missing required argument '--file'", missing.Error);

            var ok = Bind(declared, new Dictionary<string, string> { ["file"] = "a.png" });
            Assert.True(ok.Success, ok.Error);
            Assert.Equal("png", ok.Variables["format"]);
        }

        [Fact]
        public void Bind_FilePath_IsResolvedAgainstTheWorkingDirectory()
        {
            var result = Bind(new[] { new CommandlineArgument { Name = "file", Variable = "Filename", Type = ContractDataType.FilePath } },
                new Dictionary<string, string> { ["file"] = "capture.png" });

            Assert.True(result.Success, result.Error);
            Assert.Equal(Path.GetFullPath(Path.Combine(Cwd, "capture.png")), result.Variables["Filename"]);
        }

        [Theory]
        [InlineData(@"C:\temp\image.png:hidden")]
        [InlineData("CON")]
        public void Bind_FilePath_RejectsUnsafePaths_WhateverTheArgumentIsCalled(string path)
        {
            var result = Bind(new[] { new CommandlineArgument { Name = "input", Variable = "InputFile", Type = ContractDataType.FilePath } },
                new Dictionary<string, string> { ["input"] = path });

            Assert.False(result.Success);
            Assert.Contains("[SECURITY]", result.Error);
        }

        [Fact]
        public void Bind_FilePath_RejectsNetworkPathsFromTheBrowser()
        {
            var declared = new[] { new CommandlineArgument { Name = "input", Type = ContractDataType.FilePath } };
            var supplied = new Dictionary<string, string> { ["input"] = @"\\attacker.example.com\share\x.png" };

            Assert.False(Bind(declared, supplied, IpcSources.UrlScheme).Success);
            Assert.False(Bind(declared, supplied, IpcSources.NativeMessaging).Success);
        }

        [Fact]
        public void Bind_StringValue_IsNotTurnedIntoAPath()
        {
            // Before, any string matching a file in the working directory was silently replaced by its full path
            string existing = Path.GetFileName(Path.GetTempFileName());
            var result = Bind(new[] { new CommandlineArgument { Name = "title" } },
                new Dictionary<string, string> { ["title"] = existing });

            Assert.True(result.Success, result.Error);
            Assert.Equal(existing, result.Variables["title"]);
        }

        [Fact]
        public void Bind_TypedValues_AreConvertedOrRejected()
        {
            var declared = new[]
            {
                new CommandlineArgument { Name = "count", Type = ContractDataType.Integer },
                new CommandlineArgument { Name = "scale", Type = ContractDataType.Decimal },
                new CommandlineArgument { Name = "notify", Type = ContractDataType.Boolean },
                new CommandlineArgument { Name = "format", Type = ContractDataType.Enum, AllowedValues = new List<string> { "png", "jpg" } }
            };

            var ok = Bind(declared, new Dictionary<string, string> { ["count"] = "3", ["scale"] = "1.5", ["notify"] = "yes", ["format"] = "JPG" });
            Assert.True(ok.Success, ok.Error);
            Assert.Equal(3, ok.Variables["count"]);
            Assert.Equal(1.5, ok.Variables["scale"]);
            Assert.Equal(true, ok.Variables["notify"]);
            Assert.Equal("jpg", ok.Variables["format"]);

            Assert.Contains("whole number", Bind(declared, new Dictionary<string, string> { ["count"] = "three" }).Error);
            Assert.Contains("expects a number", Bind(declared, new Dictionary<string, string> { ["scale"] = "big" }).Error);
            Assert.Contains("true or false", Bind(declared, new Dictionary<string, string> { ["notify"] = "maybe" }).Error);
            Assert.Contains("Allowed values: png, jpg", Bind(declared, new Dictionary<string, string> { ["format"] = "gif" }).Error);
        }

        [Fact]
        public async Task RunRecipe_UndeclaredArgument_IsRejectedWithUsageExitCode()
        {
            var recipe = new CaptureRecipe("recipe_binder_run", "Binder Run")
                .AddNode(new RecipeNodeConfig { Id = "s1", StepType = WellKnownStepTypes.Source, Parameters = new Dictionary<string, object> { ["SourceType"] = CaptureSourceType.Clipboard } })
                .AddTrigger(TriggerConfig.CreateCommandline("binder-run", arguments: new[] { new CommandlineArgument { Name = "file", Type = ContractDataType.FilePath } }));
            RecipeManager.Instance.RegisterRecipe(recipe);

            var envelope = new IpcEnvelope { Source = IpcSources.Cli, Command = "RUN_RECIPE", Recipe = "binder-run", Cwd = Cwd };
            envelope.Parameters["CaptureDelay"] = "5000";

            using (var ms = new MemoryStream())
            {
                await IpcSecurityDispatcher.DispatchAsync(new IpcRequestContext(envelope, ms), null, () => { }, () => { }, () => { }, f => { });

                ms.Position = 0;
                var lengthBytes = new byte[4];
                ms.Read(lengthBytes, 0, 4);
                var payload = new byte[BitConverter.ToUInt32(lengthBytes, 0)];
                ms.Read(payload, 0, payload.Length);
                var reply = JObject.Parse(Encoding.UTF8.GetString(payload));

                Assert.Equal("error", reply.Value<string>("status"));
                Assert.Equal(CliCommandParser.UsageExitCode, reply.Value<int>("exit_code"));
                Assert.Contains("unknown argument 'CaptureDelay'", reply.Value<string>("stderr"));
            }
        }
    }
}
