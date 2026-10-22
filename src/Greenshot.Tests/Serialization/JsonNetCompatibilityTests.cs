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
using Greenshot.Recipes.Steps;
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
            // The validator checks the parameters with the contracts of the step types
            StepRegistry.Instance.Register<SourceAcquisitionStep>(config => new SourceAcquisitionStep(config));
            StepRegistry.Instance.Register<SetVariableStep>(config => new SetVariableStep(config));
        }

        private static string TestData(params string[] path) => File.ReadAllText(Path.Combine(new[] { AppContext.BaseDirectory, "TestData" }.Concat(path).ToArray()));

        private static void AssertSameJson(string expected, string actual)
        {
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)), $"Expected:\n{expected}\nActual:\n{actual}");
        }

        /// <summary>
        /// Recipes of the earlier beta (written by Json.NET, transitions per node) get an error which says what changed
        /// </summary>
        [Fact]
        public void Recipes_OfTheEarlierBeta_AreRejectedWithAClearError()
        {
            var error = Assert.Throws<JsonException>(() => RecipeSerializer.DeserializeList(TestData("JsonNet", "recipes.gsrecipe.json"), validate: false));

            Assert.Contains("earlier Greenshot beta", error.Message);
            Assert.Contains("\"transitions\" is now one list", error.Message);
        }

        [Theory]
        [InlineData("ext_border")]
        [InlineData("ext_caption")]
        [InlineData("ext_dropshadow")]
        public void Extensions_OfTheEarlierBeta_AreRejectedWithAClearError(string id)
        {
            var error = Assert.Throws<JsonException>(() => RecipeSerializer.DeserializeExtension(TestData("JsonNet", $"extension-{id}.gsrecipe.json"), validate: false));

            Assert.Contains("earlier Greenshot beta", error.Message);
        }

        /// <summary>
        /// The same recipes in the current format (TestData/Recipes) are read and written the same
        /// </summary>
        [Fact]
        public void Recipes_AreReadAndWrittenTheSame()
        {
            string json = TestData("Recipes", "recipes.gsrecipe.json");

            var recipes = RecipeSerializer.DeserializeList(json, validate: false);

            Assert.Equal(17, recipes.Count);
            var written = JsonNode.Parse(json).AsArray();
            for (int i = 0; i < recipes.Count; i++)
            {
                AssertSameJson(written[i].ToJsonString(), RecipeSerializer.Serialize(recipes[i]));
            }
        }

        [Theory]
        [InlineData("border")]
        [InlineData("caption")]
        [InlineData("dropshadow")]
        public void Extensions_AreReadAndWrittenTheSame(string name)
        {
            string json = TestData("Recipes", $"extension-{name}.gsrecipe.json");

            var extension = RecipeSerializer.DeserializeExtension(json, validate: false);

            Assert.Equal("ext_" + name, extension.Id);
            Assert.True(RecipeValidator.Validate(extension).IsValid, RecipeValidator.Validate(extension).ToString());
            AssertSameJson(json, RecipeSerializer.Serialize(extension));
        }

        [Fact]
        public void Recipe_HasTheValuesTheStepsRead()
        {
            var recipes = RecipeSerializer.DeserializeList(TestData("Recipes", "recipes.gsrecipe.json"), validate: false);
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
            Assert.Equal("${option.border}", sink.FindNode("branch").When);
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
            Assert.Equal("fallback_recipe", sink.Flow.GetErrorTransition("prompt", new InvalidOperationException()).Recipe);

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

        private const string HandWrittenRecipe = @"
            {
                // A comment
                ""kind"": ""recipe"",
                ""id"": ""hand_written"",
                ""name"": ""Hand written"",
                ""nodes"": [
                    { ""id"": ""a"", ""stepType"": ""SetVariable"", ""enabled"": false, ""parameters"": { ""Variable"": ""X"", ""Value"": null } },
                    { ""id"": ""b"", ""stepType"": ""SetVariable"", ""when"": ""${option.on}"" },
                    { ""id"": ""c"", ""stepType"": ""Source"", ""parameters"": { ""SourceType"": ""Region"", ""DelayMs"": 5 } },
                ],
                ""flow"": {
                    ""startNodes"": [""c""],
                    ""transitions"": [ { ""from"": ""c"", ""to"": ""a"" }, { ""from"": ""a"", ""to"": ""b"" }, { ""from"": ""*"", ""to"": ""b"", ""onError"": ""*"" }, ],
                },
                ""options"": [ { ""key"": ""on"", ""type"": ""Boolean"", ""default"": true } ]
            }";

        /// <summary>
        /// A hand written recipe: comments and trailing commas are fine, the values get the type the step declares
        /// </summary>
        [Fact]
        public void Recipe_HandWritten_IsRead()
        {
            var recipe = RecipeSerializer.Deserialize(HandWrittenRecipe);

            Assert.Equal("hand_written", recipe.Id);
            Assert.False(recipe.FindNode("a").Enabled);
            Assert.Equal("${option.on}", recipe.FindNode("b").When);
            Assert.IsType<int>(recipe.FindNode("c").Parameters["DelayMs"]);
            Assert.Equal(new[] { "a" }, recipe.Flow.GetTargets("c"));
            Assert.Equal("b", recipe.Flow.GetErrorTransition("c", new Exception()).To);
        }

        /// <summary>
        /// Recipes are read strictly: a mistake is an error, not a guess
        /// </summary>
        [Theory]
        [InlineData(@"""id"": ""hand_written""", @"""ID"": ""hand_written""", "ID")]
        [InlineData(@"""name"": ""Hand written"",", @"""name"": ""Hand written"", ""unknownProperty"": 1,", "unknownProperty")]
        [InlineData(@"""name"": ""Hand written"",", @"""name"": ""Hand written"", ""name"": ""Again"",", "name")]
        [InlineData(@"""enabled"": false", @"""enabled"": 0", "enabled")]
        [InlineData(@"""kind"": ""recipe""", @"""kind"": ""Recipe""", "Recipe")]
        [InlineData(@"""type"": ""Boolean""", @"""type"": 3", "type")]
        public void Recipe_WithAMistake_IsNotRead(string part, string mistake, string named)
        {
            Assert.Contains(part, HandWrittenRecipe);

            var error = Assert.Throws<JsonException>(() => RecipeSerializer.Deserialize(HandWrittenRecipe.Replace(part, mistake), validate: false));

            Assert.Contains(named, error.Message + error.Path);
        }

        [Theory]
        [InlineData(@"""SourceType"": ""Region""", @"""sourceType"": ""Region""", "is written 'SourceType'")]
        [InlineData(@"""DelayMs"": 5", @"""DelayMs"": ""5""", "'DelayMs' must be a whole number")]
        [InlineData(@"""DelayMs"": 5", @"""DelayMs"": 5.5", "'DelayMs' must be a whole number")]
        [InlineData(@"""Variable"": ""X""", @"""Variable"": 7", "'Variable' must be a text")]
        [InlineData(@"{ ""from"": ""a"", ""to"": ""b"" }", @"{ ""from"": ""a"", ""to"": ""b"", ""recipe"": ""other"" }", "only for error transitions")]
        [InlineData(@"""onError"": ""*""", @"""onError"": ""*"", ""branch"": ""x""", "either for a branch or for an error")]
        public void Recipe_WithAWrongParameterOrTransition_IsNotValid(string part, string mistake, string message)
        {
            Assert.Contains(part, HandWrittenRecipe);

            var error = Assert.Throws<JsonException>(() => RecipeSerializer.Deserialize(HandWrittenRecipe.Replace(part, mistake)));

            Assert.Contains(message, error.Message);
        }

        [Fact]
        public void Recipe_WithAConditionInEnabled_SaysWhereItGoes()
        {
            var error = Assert.Throws<JsonException>(() => RecipeSerializer.Deserialize(HandWrittenRecipe.Replace(@"""when"": ""${option.on}""", @"""enabled"": ""${option.on}"""), validate: false));

            Assert.Contains("goes into \"when\"", error.Message);
        }

        [Fact]
        public void Recipe_SavedAgain_KeepsTheKeysOfTheValuesAndEscapesNoText()
        {
            var recipe = new CaptureRecipe("keys", "Keys", "Grüße <b>")
                .AddNode(new RecipeNodeConfig("vars", WellKnownStepTypes.SetVariable)
                    .Set("Variables", new Dictionary<string, object> { { "MyVar", 1 } }));

            string json = RecipeSerializer.Serialize(recipe);

            // The parameter names as the step declares them, the variable names as the user wrote them
            Assert.Contains("\"Variables\": {", json);
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
