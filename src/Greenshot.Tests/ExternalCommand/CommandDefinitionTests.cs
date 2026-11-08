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
using System.IO;
using System.Linq;
using Greenshot.Base.Definitions;
using Greenshot.Base.Recipes;
using Greenshot.Base.Trust;
using Greenshot.Plugin.ExternalCommand;
using Greenshot.Plugin.ExternalCommand.Destinations;
using Greenshot.Plugin.ExternalCommand.Recipes;
using Greenshot.Plugin.ExternalCommand.Running;
using Xunit;

namespace Greenshot.Tests.ExternalCommand
{
    /// <summary>
    /// External commands as definitions: the file format, the checks, the conversion of the commands of Greenshot 1.3, the built-in
    /// ones and how the recipe step uses a definition
    /// </summary>
    public class CommandDefinitionTests
    {
        public CommandDefinitionTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static CommandStore NewStore(params CommandDefinition[] builtIns) =>
            new CommandStore(new Dictionary<string, string>(), null, builtIns);

        /// <summary>
        /// The approvals of a test, in memory
        /// </summary>
        private sealed class TestApprovals : IFileApprovals
        {
            private readonly Dictionary<string, string> _approved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public bool IsApproved(string filePath, string sha256) => _approved.TryGetValue(Path.GetFullPath(filePath), out var hash) && hash == sha256;

            public bool IsKnown(string filePath) => _approved.ContainsKey(Path.GetFullPath(filePath));

            public void Approve(string filePath, string sha256) => _approved[Path.GetFullPath(filePath)] = sha256;

            public void Revoke(string filePath) => _approved.Remove(Path.GetFullPath(filePath));
        }

        private static CommandStore FolderStore(string directory, IFileApprovals approvals) =>
            new CommandStore(new DefinitionFolder(directory, CommandKind.FileExtension, CommandKind.MaximumLength, approvals), null);

        private static string NewDirectory() => Path.Combine(Path.GetTempPath(), "greenshot-commands-" + Guid.NewGuid().ToString("N"));

        [Fact]
        public void Store_InAFolder_OneReadableFilePerCommand()
        {
            string directory = NewDirectory();
            try
            {
                var approvals = new TestApprovals();
                var store = FolderStore(directory, approvals);
                string id = store.Add(new CommandDefinition { Name = "Viewer/big", Program = "viewer.exe" });

                Assert.Equal("Viewer_big.gscommand.json", id);
                string content = File.ReadAllText(Path.Combine(directory, id));
                Assert.Contains("\"program\": \"viewer.exe\"", content);
                Assert.Contains("\"arguments\": \"\\\"${export.file}\\\"\"", content);

                // What Greenshot wrote is approved, also after a restart
                Assert.Equal("viewer.exe", FolderStore(directory, approvals).Find("Viewer/big")?.Definition.Program);

                Assert.True(store.Remove(id));
                Assert.False(File.Exists(Path.Combine(directory, id)));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void ApprovalAtStartup_AsksOnlyAboutItsOwnChangedFiles_AndApprovesTheConfirmedOnes()
        {
            string directory = NewDirectory();
            try
            {
                var approvals = new TestApprovals();
                var written = FolderStore(directory, approvals);
                written.Add(new CommandDefinition { Name = "Mine", Program = "mine.exe" });
                string yes = written.Add(new CommandDefinition { Name = "Yes", Program = "yes.exe" });
                string no = written.Add(new CommandDefinition { Name = "No", Program = "no.exe" });
                string broken = written.Add(new CommandDefinition { Name = "Broken", Program = "broken.exe" });

                // Changed outside Greenshot, and one somebody put into the folder
                File.WriteAllText(Path.Combine(directory, yes), "{\"kind\":\"externalCommand\",\"name\":\"Yes\",\"program\":\"yes2.exe\"}");
                File.WriteAllText(Path.Combine(directory, no), "{\"kind\":\"externalCommand\",\"name\":\"No\",\"program\":\"no2.exe\"}");
                File.WriteAllText(Path.Combine(directory, broken), "{\"kind\":\"externalCommand\",\"name\":\"Broken\"}");
                File.WriteAllText(Path.Combine(directory, "stranger.gscommand.json"), "{\"kind\":\"externalCommand\",\"name\":\"Stranger\",\"program\":\"stranger.exe\"}");

                // The start: only the changed files which can be read are asked about
                var store = FolderStore(directory, approvals);
                var asked = new List<string>();
                var approved = new List<string>();
                int count = DefinitionApprovalAtStartup.Ask(store, command =>
                {
                    asked.Add(command.Name);
                    return command.Name == "Yes";
                }, command => approved.Add(command.Name));

                Assert.Equal(new[] { "No", "Yes" }, asked);
                Assert.Equal(new[] { "Yes" }, approved);
                Assert.Equal(1, count);
                Assert.Equal("yes2.exe", store.Find("Yes")?.Definition.Program);
                Assert.Null(store.Find("No"));
                Assert.Null(store.Find("Stranger"));

                // The next start asks about the one which wasn't approved again
                Assert.Equal(new[] { "No" }, DefinitionApprovalAtStartup.NotApproved(FolderStore(directory, approvals)).Select(stored => stored.Name));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void Store_InAFolder_FileCopiedIn_IsIgnored_AChangedFileNeedsTheApproval()
        {
            string directory = NewDirectory();
            try
            {
                var approvals = new TestApprovals();
                var store = FolderStore(directory, approvals);
                Assert.Empty(store.GetAll());

                // Copied in while Greenshot runs: no effect
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "stranger.gscommand.json"), "{\"kind\":\"externalCommand\",\"name\":\"Stranger\",\"program\":\"stranger.exe\"}");
                Assert.Empty(store.GetAll());

                // Not after a restart either: Greenshot didn't write or import it
                store = FolderStore(directory, approvals);
                Assert.Empty(store.GetAll());
                Assert.Null(store.Find("Stranger"));

                // Written by Greenshot, it is used, also after a restart
                string otherId = store.Add(new CommandDefinition { Name = "Other", Program = "other.exe" });
                string otherPath = Path.Combine(directory, otherId);
                Assert.Equal("other.exe", FolderStore(directory, approvals).Find("Other")?.Definition.Program);

                // Changed outside Greenshot: the running Greenshot keeps the approved content, after a restart it is listed, not approved, and not used
                File.WriteAllText(otherPath, "{\"kind\":\"externalCommand\",\"name\":\"Other\",\"program\":\"evil.exe\"}");
                Assert.Equal("other.exe", store.Find("Other")?.Definition.Program);
                store = FolderStore(directory, approvals);
                var other = Assert.Single(store.GetAll());
                Assert.False(other.IsApproved);
                Assert.Null(store.Find("Other"));
                Assert.Empty(store.GetReadable());

                // Approved, it is used at once and after a restart
                Assert.True(store.Approve(other.Id));
                Assert.Equal("evil.exe", store.Find("Other")?.Definition.Program);
                Assert.Equal("evil.exe", FolderStore(directory, approvals).Find("Other")?.Definition.Program);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void Store_InAFolder_NewCommand_DoesNotOverwriteAFileCopiedInLater()
        {
            string directory = NewDirectory();
            try
            {
                var store = FolderStore(directory, new TestApprovals());
                Assert.Empty(store.GetAll());
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "Viewer.gscommand.json"), "{}");

                string id = store.Add(new CommandDefinition { Name = "Viewer", Program = "viewer.exe" });

                Assert.Equal("Viewer (2).gscommand.json", id);
                Assert.Equal("{}", File.ReadAllText(Path.Combine(directory, "Viewer.gscommand.json")));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void Parse_WrittenDefinition_ReadsTheSame()
        {
            var definition = new CommandDefinition
            {
                Name = "Optimize",
                Program = @"${user.localappdata}\Programs\optimizer.exe",
                Arguments = "-o \"${export.file}\"",
                Output = "text",
                CopyToClipboard = "url",
                SuccessExitCodes = "0, 98",
                Timeout = 30
            };

            var read = CommandKind.Parse(CommandKind.Instance.Write(definition));

            Assert.Equal("Optimize", read.Name);
            Assert.Equal(definition.Program, read.Program);
            Assert.Equal(definition.Arguments, read.Arguments);
            // The choices are normalized
            Assert.Equal("Text", read.Output);
            Assert.Equal("Url", read.CopyToClipboard);
            Assert.Equal("0,98", read.SuccessExitCodes);
            Assert.Equal(30, read.Timeout);
            Assert.Equal("New", read.File);
        }

        [Theory]
        [InlineData("{\"kind\":\"other\",\"name\":\"A\",\"program\":\"a.exe\"}")]
        [InlineData("{\"kind\":\"externalCommand\",\"version\":2,\"name\":\"A\",\"program\":\"a.exe\"}")]
        [InlineData("{\"kind\":\"externalCommand\",\"name\":\"\",\"program\":\"a.exe\"}")]
        [InlineData("{\"kind\":\"externalCommand\",\"name\":\"A\"}")]
        [InlineData("{\"kind\":\"externalCommand\",\"name\":\"A\",\"program\":\"a.exe\",\"output\":\"Image\",\"runInBackground\":true}")]
        [InlineData("{\"kind\":\"externalCommand\",\"name\":\"A\",\"program\":\"a.exe\",\"output\":\"None\",\"copyToClipboard\":\"Output\"}")]
        [InlineData("{\"kind\":\"externalCommand\",\"name\":\"A\",\"program\":\"a.exe\",\"output\":\"File\",\"file\":\"None\"}")]
        [InlineData("{\"kind\":\"externalCommand\",\"name\":\"A\",\"program\":\"a.exe\",\"file\":\"Somewhere\"}")]
        [InlineData("{\"kind\":\"externalCommand\",\"name\":\"A\",\"program\":\"a.exe\",\"successExitCodes\":\"zero\"}")]
        [InlineData("{\"kind\":\"externalCommand\",\"name\":\"A\",\"program\":\"a.exe\",\"timeout\":-1}")]
        [InlineData("not json")]
        public void Parse_DefinitionWhichCantBeUsed_Throws(string json)
        {
            Assert.Throws<DefinitionFileException>(() => CommandKind.Parse(json));
        }

        [Fact]
        public void Check_TextOnlyWithTextInput()
        {
            var definition = new CommandDefinition { Name = "A", Program = "a.exe", StandardInputText = "secret" };
            CommandKind.Check(definition);
            Assert.Null(definition.StandardInputText);
        }

        [Fact]
        public void GateTarget_ShowsProgramArgumentsAndText()
        {
            var definition = new CommandDefinition { Name = "Ask", Program = "ask.exe", Arguments = "-q", StandardInput = "Text", StandardInputText = "${payload.ocr.text}", Output = "Image" };
            string target = CommandKind.GateTarget(definition);
            Assert.Equal("Ask (ask.exe -q), standard input: ${payload.ocr.text}, replaces the image", target);
        }

        [Fact]
        public void FromLegacy_CommandOf13_KeepsWhatItDid()
        {
            var config = new ExternalCommandConfigurationImpl();
            config.OnAfterLoad();
            config.Commands.Add("Upload");
            config.Commandline["Upload"] = @"C:\Tools\upload.exe";
            config.Argument["Upload"] = "--file \"{0}\"";
            config.RunInbackground["Upload"] = false;
            config.RedirectStandardOutputCommand["Upload"] = true;
            config.ParseOutputForUriCommand["Upload"] = true;
            config.UriToClipboardCommand["Upload"] = true;
            config.OutputFormat["Upload"] = "jpg";

            var definition = CommandDefinitions.FromLegacy(config, "Upload");

            Assert.Equal(@"C:\Tools\upload.exe", definition.Program);
            Assert.Equal("--file \"${export.file}\"", definition.Arguments);
            Assert.False(definition.RunInBackground);
            Assert.Equal("Text", definition.Output);
            Assert.Equal("Url", definition.CopyToClipboard);
            Assert.Equal("jpg", definition.Format);
            // 1.3 handed over the file the capture was saved to
            Assert.Equal("LastExport", definition.File);
        }

        [Fact]
        public void ConvertOnce_ConvertsTheCommandsOnce_SkipsAnUnchangedBuiltIn()
        {
            var config = new ExternalCommandConfigurationImpl();
            config.OnAfterLoad();
            config.Commands.Add("Viewer");
            config.CommandlineTemplate["Viewer"] = "viewer.exe";
            config.Commands.Add("Paint");
            config.CommandlineTemplate["Paint"] = "paint.exe";
            config.ArgumentTemplate["Paint"] = CommandDefinition.DefaultArguments;
            var builtIn = new CommandDefinition { Name = "Paint", Program = "paint.exe", RunInBackground = true };
            var store = new CommandStore(new Dictionary<string, string>(), null, new[] { builtIn });

            CommandDefinitions.ConvertOnce(config, store);
            CommandDefinitions.ConvertOnce(config, store);

            Assert.True(config.DefinitionsConverted);
            var all = store.GetAll();
            Assert.Equal(2, all.Count);
            Assert.Contains(all, stored => stored.Name == "Viewer" && !stored.IsBuiltIn);
            Assert.Contains(all, stored => stored.Name == "Paint" && stored.IsBuiltIn);
            // The old keys stay for Greenshot 1.3
            Assert.Contains("Viewer", config.Commands);
        }

        [Fact]
        public void ConvertOnce_ChangedBuiltIn_IsKeptNextToIt()
        {
            var config = new ExternalCommandConfigurationImpl();
            config.OnAfterLoad();
            config.Commands.Add("Paint");
            config.CommandlineTemplate["Paint"] = @"D:\Portable\paint.exe";
            var store = new CommandStore(new Dictionary<string, string>(), null, new[] { new CommandDefinition { Name = "Paint", Program = "paint.exe" } });

            CommandDefinitions.ConvertOnce(config, store);

            Assert.Equal(@"D:\Portable\paint.exe", store.Find("Paint (1.3)")?.Definition.Program);
        }

        [Fact]
        public void Store_BuiltIn_CantBeReplaced_UnlessAStoredOneHasItsName()
        {
            var store = NewStore(new CommandDefinition { Name = "Paint", Program = "paint.exe" });
            Assert.True(store.IsBuiltInName("paint"));
            Assert.Throws<InvalidOperationException>(() => store.Add(new CommandDefinition { Name = "Paint", Program = "other.exe" }));

            // Stored before the program was installed: the stored one takes the place of the built-in one
            var definitions = new Dictionary<string, string> { ["id"] = CommandKind.Instance.Write(new CommandDefinition { Name = "Paint", Program = "mine.exe" }) };
            var withStored = new CommandStore(definitions, null, new[] { new CommandDefinition { Name = "Paint", Program = "paint.exe" } });
            Assert.Single(withStored.GetAll());
            Assert.Equal("mine.exe", withStored.Find("Paint").Definition.Program);
            Assert.False(withStored.IsBuiltInName("Paint"));
        }

        [Fact]
        public void Destination_HasTheDesignationOf13()
        {
            var destination = new ExternalCommandDestination(new CommandDefinition { Name = "Paint, the program", Program = "paint.exe" });
            Assert.Equal("External Paint_ the program", destination.Designation);
            Assert.Equal(ExternalCommandIconProvider.KeyFor("Paint, the program"), destination.Descriptor.IconKey);
        }

        [Fact]
        public void Step_WithoutCommand_UsesTheParameters()
        {
            var step = new ExternalCommandStep(new RecipeNodeConfig
            {
                Id = "cmd",
                StepType = "ExternalCommand",
                Parameters = new Dictionary<string, object> { ["Program"] = "tool.exe", ["StandardInput"] = "text", ["StandardInputText"] = "hello" }
            });

            var command = step.ResolveCommand();

            Assert.Equal("tool.exe", command.Program);
            Assert.Equal(CommandDefinition.DefaultArguments, command.Arguments);
            Assert.Equal("Text", command.StandardInput);
            // The step reads the output by default
            Assert.Equal("Text", command.Output);
            Assert.Contains("standard input: hello", step.GetGatedActions().Single().Target);
        }

        [Fact]
        public void CreateRequest_InTheBackground_ReadsNoOutput()
        {
            string file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "a.png");
            var request = CommandDefinitions.CreateRequest(new CommandDefinition { Name = "A", Program = "a.exe", Output = "Text", RunInBackground = true }, file, null);
            Assert.Equal(CommandOutput.None, request.Output);
            Assert.Equal($"\"{file}\"", request.Arguments);
            Assert.Equal(System.IO.Path.GetDirectoryName(file), request.WorkingDirectory);
        }
    }
}
