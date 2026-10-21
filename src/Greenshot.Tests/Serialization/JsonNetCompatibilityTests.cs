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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Base.Serialization;
using Greenshot.Ipc;
using Greenshot.Plugin.CustomUploader;
using Greenshot.Plugin.CustomUploader.Sxcu;
using Greenshot.Recipes.Approval;
using Xunit;

namespace Greenshot.Tests.Serialization
{
    /// <summary>
    /// What Greenshot wrote with Json.NET (TestData/JsonNet, written by the code before System.Text.Json) and what other
    /// programs send must still be read, and written again the same way.
    /// </summary>
    public class JsonNetCompatibilityTests
    {
        public JsonNetCompatibilityTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static string TestData(params string[] path) => File.ReadAllText(Path.Combine(new[] { AppContext.BaseDirectory, "TestData" }.Concat(path).ToArray()));

        private static void AssertSameJson(string expected, string actual)
        {
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)), $"Expected:\n{expected}\nActual:\n{actual}");
        }

        [Fact]
        public void Recipes_WrittenByJsonNet_AreReadAndWrittenTheSame()
        {
            string json = TestData("JsonNet", "recipes.gsrecipe.json");

            var recipes = RecipeSerializer.DeserializeList(json, validate: false);

            Assert.Equal(17, recipes.Count);
            var oldRecipes = JsonNode.Parse(json).AsArray();
            for (int i = 0; i < recipes.Count; i++)
            {
                AssertSameJson(oldRecipes[i].ToJsonString(), RecipeSerializer.Serialize(recipes[i]));
            }
        }

        [Theory]
        [InlineData("ext_border")]
        [InlineData("ext_caption")]
        [InlineData("ext_dropshadow")]
        public void Extensions_WrittenByJsonNet_AreReadAndWrittenTheSame(string id)
        {
            string json = TestData("JsonNet", $"extension-{id}.gsrecipe.json");

            var extension = RecipeSerializer.DeserializeExtension(json, validate: false);

            Assert.Equal(id, extension.Id);
            Assert.True(RecipeValidator.Validate(extension).IsValid, RecipeValidator.Validate(extension).ToString());
            AssertSameJson(json, RecipeSerializer.Serialize(extension));
        }

        [Fact]
        public void Recipe_WrittenByJsonNet_HasTheValuesTheStepsRead()
        {
            var recipes = RecipeSerializer.DeserializeList(TestData("JsonNet", "recipes.gsrecipe.json"), validate: false);
            var sink = recipes.Single(r => r.Id == "json_net_kitchen_sink");

            Assert.Equal("Ünïcödé, <html> & \"quotes\"", sink.Description);
            Assert.Equal(FlowConcurrency.ReplacePrevious, sink.Concurrency);
            Assert.False(sink.IsEnabled);
            Assert.Equal("1.4", sink.Requires.Single().MinVersion);

            // Options: the default values have the type of the option
            Assert.Equal(true, sink.FindOption("border").GetDefaultValue());
            Assert.Equal(4, sink.FindOption("width").GetDefaultValue());
            Assert.Equal(0.5, sink.FindOption("opacity").GetDefaultValue());
            Assert.Equal("#FF0000", sink.FindOption("color").GetDefaultValue());
            Assert.Equal(new[] { "fast", "good" }, sink.FindOption("mode").Choices.Select(c => c.Value));
            Assert.True(sink.FindOption("caption").IsTemplate);

            // Parameters: found in any casing, lists and dictionaries as plain values
            var prompt = sink.FindNode("prompt");
            var choices = Assert.IsType<List<object>>(prompt.Parameters["Choices"]).Cast<IDictionary<string, object>>().ToList();
            Assert.Equal("Yes", choices[0]["Key"]);
            Assert.Equal(true, choices[0]["IsDefault"]);
            Assert.Equal(true, choices[1]["isCancel"]);
            Assert.Equal(new[] { "big", "small" }, RecipeSerializer.ConvertValue<List<Dictionary<string, string>>>(sink.FindNode("branch").Parameters["branches"]).Select(b => b["key"]));
            Assert.Equal("${option.border}", sink.FindNode("branch").EnabledExpression);
            Assert.False(sink.FindNode("off").Enabled);
            var variables = Assert.IsAssignableFrom<IDictionary<string, object>>(sink.FindNode("vars").Parameters["variables"]);
            Assert.Equal(3L, variables["count"]);
            Assert.Equal(1.25, variables["ratio"]);
            Assert.Equal(false, variables["FLAG"]);
            var annotations = Assert.IsType<List<object>>(sink.FindNode("draw").Parameters["annotations"]);
            Assert.Equal("Rectangle", ((IDictionary<string, object>)annotations[0])["Type"]);
            Assert.Equal(20.5, ((IDictionary<string, object>)annotations[0])["y"]);
            Assert.True(RecipeSlots.Accepts(sink.FindNode("slot"), "caption"));
            Assert.False(RecipeSlots.Accepts(sink.FindNode("slot"), "dropshadow"));
            Assert.Equal("other", sink.FindNode("fail").OnErrorRecipeId);

            // Flow
            Assert.Contains("draw", sink.Flow.GetUnifiedTransitions()["branch"]);
            Assert.Equal("vars", sink.Flow.GetErrorTransition("fail", new IOException()).To);
            Assert.Equal("fallback_recipe", sink.Flow.GetErrorTransition("prompt", new InvalidOperationException()).TargetRecipeId);

            // Triggers
            var contextMenu = sink.Triggers.Single();
            Assert.Equal(5, contextMenu.GetParameter<int>("Order"));
            var all = recipes.Single(r => r.Id == "json_net_all_steps");
            var commandline = all.Triggers.Single(t => t.TriggerType == TriggerConfig.TypeCommandline && t.GetParameter<string>("Command") == "convert");
            var arguments = commandline.GetParameter<List<CommandlineArgument>>("Arguments");
            Assert.Equal(2, arguments.Count);
            Assert.Equal(ContractDataType.FilePath, arguments[0].Type);
            Assert.Equal("Input", arguments[0].EffectiveVariable);
            Assert.Equal(new[] { "png", "jpg" }, arguments[1].AllowedValues);
            Assert.False(all.Triggers.Single(t => t.GetParameter<string>("Hotkey") == "Ctrl+Shift+F9").Enabled);
            Assert.True(all.Triggers.Single(t => t.TriggerType == TriggerConfig.TypeAiTool && t.GetParameter<string>("ToolName") == "convert_file").GetParameter<bool>("ReadOnly"));
        }

        /// <summary>
        /// Hand written recipe files were read by Json.NET, which is lenient
        /// </summary>
        [Fact]
        public void Recipe_HandWritten_AsLenientAsJsonNet()
        {
            const string json = @"
            {
                // A comment
                ""Kind"": ""Recipe"",
                ""ID"": ""hand_written"",
                ""Name"": ""Hand written"",
                ""version"": 2,
                ""concurrency"": ""exclusive"",
                ""isEnabled"": ""false"",
                ""unknownProperty"": { ""a"": [1, 2] },
                ""triggers"": [
                    { ""triggerType"": ""Hotkey"", ""enabled"": 1, ""parameters"": { ""hotkey"": ""Ctrl+H"", ""Order"": ""7"" } },
                ],
                ""nodes"": [
                    { ""id"": ""a"", ""stepType"": ""SetVariable"", ""enabled"": ""false"", ""parameters"": { ""Variable"": ""X"", ""Value"": null } },
                    { ""id"": ""b"", ""stepType"": ""SetVariable"", ""enabled"": ""${option.on}"" },
                    { ""id"": ""c"", ""stepType"": ""SetVariable"", ""onErrorNodeId"": null },
                ],
                ""flow"": {
                    ""startNodes"": [""a""],
                    ""transitions"": { ""a"": ""b"", ""b"": [""c"", null, """"] },
                    ""errorTransitions"": [ { ""from"": ""*"", ""to"": ""c"" } ]
                },
                ""options"": [ { ""key"": ""on"", ""type"": 3, ""default"": ""true"" }, { ""key"": ""size"", ""type"": ""integer"", ""default"": ""12"", ""min"": ""1"" } ]
            }";

            var recipe = RecipeSerializer.Deserialize(json, validate: false);

            Assert.Equal("hand_written", recipe.Id);
            Assert.Equal("2", recipe.Version);
            Assert.Equal(FlowConcurrency.Exclusive, recipe.Concurrency);
            Assert.False(recipe.IsEnabled);
            var trigger = Assert.Single(recipe.Triggers);
            Assert.True(trigger.Enabled);
            Assert.Equal("Ctrl+H", trigger.GetParameter<string>("Hotkey"));
            Assert.Equal(7, trigger.GetParameter<int>("order"));
            Assert.False(recipe.FindNode("a").Enabled);
            Assert.Equal("${option.on}", recipe.FindNode("b").EnabledExpression);
            Assert.Equal(new[] { "b" }, recipe.Flow.Transitions["A"]);
            Assert.Equal(new[] { "c" }, recipe.Flow.Transitions["b"]);
            Assert.Equal("c", recipe.Flow.GetErrorTransition("b", new Exception()).To);
            Assert.Equal(ContractDataType.Boolean, recipe.FindOption("on").Type);
            Assert.Equal(true, recipe.FindOption("on").GetDefaultValue());
            Assert.Equal(12, recipe.FindOption("size").GetDefaultValue());
            Assert.Equal(1m, recipe.FindOption("size").Min);
        }

        [Fact]
        public void Recipe_SavedAgain_KeepsTheKeysOfTheValuesAndEscapesNoText()
        {
            var recipe = new CaptureRecipe("keys", "Keys", "Grüße <b>")
                .AddNode(new RecipeNodeConfig("vars", WellKnownStepTypes.SetVariable)
                    .Set("Variables", new Dictionary<string, object> { { "MyVar", 1 } }));

            string json = RecipeSerializer.Serialize(recipe);

            // The parameter names are camel-cased as Json.NET did, the variable names of the user are kept
            Assert.Contains("\"variables\": {", json);
            Assert.Contains("\"MyVar\": 1", json);
            Assert.Contains("Grüße <b>", json);
            Assert.StartsWith("{\r\n  \"kind\": \"recipe\"".Replace("\r\n", Environment.NewLine), json);
            Assert.Equal(1L, RecipeSerializer.Deserialize(json, validate: false).FindNode("vars").GetParameter<IDictionary<string, object>>("variables")["myvar"]);
        }

        [Fact]
        public void IpcFrames_WrittenByJsonNet_AreReadAndWrittenTheSame()
        {
            var lines = TestData("JsonNet", "ipc-frames.txt").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var envelopes = lines.Select(line => JsonSerializer.Deserialize<IpcEnvelope>(line, GreenshotJson.Options)).ToList();

            for (int i = 0; i < lines.Length; i++)
            {
                AssertSameJson(lines[i], JsonSerializer.Serialize(envelopes[i], GreenshotJson.Options));
            }
            Assert.True(envelopes[0].IsHello);
            Assert.Equal(@"C:\Users\Test\Pictures\Grüße.png", envelopes[1].Argv.Last());
            Assert.Equal("de", envelopes[2].Parameters["LANGUAGE"]);
            Assert.True(envelopes[2].Async);
            Assert.Equal(new[] { @"C:\a.png", @"D:\b c.greenshot" }, envelopes[3].Files);
            Assert.Equal("https://example.com/?a=1&b=<2>", envelopes[5].Metadata.Url);
            Assert.Equal(1760000000, envelopes[5].Metadata.Timestamp);
            Assert.Equal("Value", envelopes[5].Parsed.Parameters["key"]);
        }

        /// <summary>
        /// As greenshot-proxy.exe (HELLO, CLI) and the browser extension write the messages: unknown properties, numbers where texts are expected
        /// </summary>
        [Fact]
        public void IpcFrames_OfTheProxyAndTheBrowserExtension_AreRead()
        {
            var hello = JsonSerializer.Deserialize<IpcEnvelope>(@"{""version"":1,""command"":""HELLO"",""source"":""native_messaging"",""reply_format"":""json"",""client"":""greenshot-proxy"",""client_version"":""1.4.0"",""origin"":""chrome-extension://abc/""}", GreenshotJson.Options);
            Assert.True(hello.IsHello);
            Assert.Equal(IpcSources.ReplyFormatJson, hello.ReplyFormat);
            Assert.Equal("chrome-extension://abc/", hello.Origin);

            var cli = JsonSerializer.Deserialize<IpcEnvelope>(@"{""version"":1,""command"":""CLI"",""cwd"":""C:\\temp"",""argv"":[""-r"",""qr""]}", GreenshotJson.Options);
            Assert.Equal(new[] { "-r", "qr" }, cli.Argv);
            Assert.Empty(cli.Files);

            var import = JsonSerializer.Deserialize<IpcEnvelope>(@"{""source"":""native_messaging"",""command"":""IMPORT_CAPTURE"",""metadata"":{""title"":""Tab"",""url"":""https://example.com"",""timestamp"":1760000000},""data"":{""mime_type"":""image/png"",""encoding"":""base64"",""payload"":""AA==""},""parameters"":{""Delay"":5,""Flag"":true}}", GreenshotJson.Options);
            Assert.Equal("image/png", import.Data.MimeType);
            Assert.Equal("5", import.Parameters["delay"]);
            Assert.Equal("true", import.Parameters["flag"]);
        }

        [Fact]
        public void TrustStore_WrittenByJsonNet_IsRead()
        {
            var records = JsonSerializer.Deserialize<List<RecipeTrustRecord>>(TestData("JsonNet", "trust-store.json"), GreenshotJson.Options);

            Assert.Equal(2, records.Count);
            var record = records[0];
            Assert.Equal(new DateTime(2026, 10, 21, 20, 26, 30, 123, DateTimeKind.Utc), record.ApprovedAt.ToUniversalTime());
            Assert.True(record.AllowExternalCommands);
            Assert.True(record.IsAiCreated);
            Assert.Equal("{\"id\":\"a\"}", record.ApprovedContent);
            var approval = record.GetApproval("A");
            Assert.True(approval.IsTriggerApproved("hotkey:ctrl+a"));
            Assert.True(approval.IsGateAllowed(RecipeGateType.ExternalCommand));
            Assert.False(approval.IsGateAllowed(RecipeGateType.FileSystemAccess));
            Assert.True(approval.ReplacesBuiltIn);
            Assert.Null(records[1].Recipes);
            Assert.True(records[1].GetApproval("b").AllTriggers);

            // Written again, the gates stay numbers
            Assert.Contains("\"AllowedGates\": [", JsonSerializer.Serialize(records, GreenshotJson.IndentedOptions));
            AssertSameJson(TestData("JsonNet", "trust-store.json"), JsonSerializer.Serialize(records, GreenshotJson.IndentedOptions));
        }

        [Fact]
        public void UploaderStore_WrittenByJsonNet_IsRead()
        {
            var lines = TestData("JsonNet", "uploader-store.txt").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var uploaders = new Dictionary<string, string> { { "a", lines[0] }, { "b", lines[1] }, { "c", lines[2] } };
            var uploads = new Dictionary<string, string> { { "u", lines[3] } };
            var store = new UploaderStore(uploaders, uploads, null, text => text, text => text);

            var definition = store.GetUploaders().Single(u => u.Id == "c").Definition;
            Assert.Equal("Example ShareX export (Ünïcödé)", definition.Name);
            Assert.Equal("PUT", definition.RequestMethod);
            Assert.Equal("3600", definition.Parameters["expires"]);
            Assert.Equal("Bearer YOUR_TOKEN", definition.Headers["Authorization"]);
            Assert.Equal(3, store.GetUploaders().Count(u => u.Definition != null));
            var upload = store.GetUploads().Single().Value;
            Assert.Equal(new DateTimeOffset(2026, 10, 21, 22, 26, 30, TimeSpan.FromHours(2)), upload.Uploaded);
            Assert.Equal("https://upload.example.com/delete/k3y", upload.DeletionUrl);

            // Stored again: the text is ASCII only
            store.Add(definition);
            Assert.All(uploaders.Values, json => Assert.True(json.All(c => c < 128), json));
            AssertSameJson(lines[2], uploaders["c"]);
        }

        [Theory]
        [InlineData("discord-webhook.sxcu", "Discord webhook")]
        [InlineData("litterbox-1h.sxcu", "Litterbox (deleted after 1 hour)")]
        [InlineData("sharex-export.sxcu", "Example ShareX export (Ünïcödé)")]
        public void SxcuFiles_AreRead(string file, string name)
        {
            var definition = UploaderFile.Read(TestData("Sxcu", file));

            Assert.Equal(name, definition.Name);
            Assert.Equal(file == "sharex-export.sxcu" ? "PUT" : "POST", definition.RequestMethod);
        }

        [Fact]
        public void SxcuFile_ResponseOfTheService_GivesTheLinks()
        {
            var definition = UploaderFile.Read(TestData("Sxcu", "sharex-export.sxcu"));
            const string content = @"{""data"":{""files"":[{""direct link"":""https://i.example.com/a.png""}],""thumb"":""https://i.example.com/t.png"",""key"":""k3y""}}";
            var result = new Greenshot.Base.Http.HttpResult(System.Net.HttpStatusCode.OK, "OK", new Uri("https://upload.example.com/api/v1/upload"), new Dictionary<string, string>(), content);

            var response = UploaderResponse.From(definition, result, "a.png");

            Assert.Equal(new Uri("https://i.example.com/a.png"), response.Url);
            Assert.Equal(new Uri("https://i.example.com/t.png"), response.ThumbnailUrl);
            Assert.Equal(new Uri("https://upload.example.com/delete/k3y"), response.DeletionUrl);
        }

        [Theory]
        [InlineData("data.link", "https://i.example.com/a.png")]
        [InlineData("$.data['link']", "https://i.example.com/a.png")]
        [InlineData("data[\"with space\"]", "x")]
        [InlineData("data.list[1]", "b")]
        [InlineData("data.list[2]", null)]
        [InlineData("data.number", "42.5")]
        [InlineData("data.flag", "true")]
        [InlineData("data.none", "")]
        [InlineData("data.none.deeper", null)]
        [InlineData("data.missing", null)]
        [InlineData("data.list.link", null)]
        [InlineData("data.object", "{\"a\":\"ü<\"}")]
        [InlineData("$", "{\"data\":{\"link\":\"https://i.example.com/a.png\",\"with space\":\"x\",\"list\":[\"a\",\"b\"],\"number\":42.5,\"flag\":true,\"none\":null,\"object\":{\"a\":\"ü<\"}}}")]
        public void ResponseJsonPath_ReadsTheValue(string path, string expected)
        {
            const string content = "{\"data\":{\"link\":\"https://i.example.com/a.png\",\"with space\":\"x\",\"list\":[\"a\",\"b\"],\"number\":42.5,\"flag\":true,\"none\":null,\"object\":{\"a\":\"ü<\"}}}";

            Assert.Equal(expected, UploaderResponse.Json(content, path));
        }

        [Theory]
        [InlineData("data.list[*]")]
        [InlineData("data..link")]
        [InlineData("$..link")]
        [InlineData("data.list[?(@ == 'a')]")]
        public void ResponseJsonPath_Unsupported_Throws(string path)
        {
            Assert.Throws<InvalidOperationException>(() => UploaderResponse.Json("{\"data\":{\"list\":[\"a\"]}}", path));
        }
    }
}
