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
using Greenshot.Base.Interfaces.Drawing;
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
            StepRegistry.Instance.Register<ConditionalStep>(config => new ConditionalStep(config));
            StepRegistry.Instance.Register<UserPromptStep>(config => new UserPromptStep(config));
            StepRegistry.Instance.Register<AnnotationStep>(config => new AnnotationStep(config));
            StepRegistry.Instance.Register<EffectCaptureStep>(config => new EffectCaptureStep(config));
            StepRegistry.Instance.Register<SlotStep>(config => new SlotStep(config));
            StepRegistry.Instance.Register<RecordVideoRecipeStep>(config => new RecordVideoRecipeStep(config));
            StepRegistry.Instance.Register<Greenshot.Plugin.Zxing.Recipes.ZxingStep>(config => new Greenshot.Plugin.Zxing.Recipes.ZxingStep(config));
            StepRegistry.Instance.Register<Greenshot.Plugin.ExternalCommand.Recipes.ExternalCommandStep>(config => new Greenshot.Plugin.ExternalCommand.Recipes.ExternalCommandStep(config));
            StepRegistry.Instance.Register<Greenshot.Plugin.Pdf.Recipes.PdfStep>(config => new Greenshot.Plugin.Pdf.Recipes.PdfStep(config, null));
            // An annotation type of a plugin, with its properties
            RecipeDrawableRegistry.Instance.RegisterDrawableFactory("TestBadge", (surface, parameters, context) => null, ScaleOptions.Default,
                new[] { new ParameterContract("Label") });
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
            Assert.False(sink.ShowInContextMenu);
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
            var choices = prompt.GetParameter<List<PromptChoice>>("Choices");
            Assert.Equal("Yes", choices[0].Key);
            Assert.True(choices[0].IsDefault);
            Assert.True(choices[1].IsCancel);
            Assert.Equal(new[] { "big", "small" }, sink.FindNode("branch").GetParameter<List<ConditionalBranch>>("Branches").Select(b => b.Key));
            Assert.Equal("${option.border}", sink.FindNode("branch").When);
            Assert.False(sink.FindNode("off").Enabled);
            var variables = Assert.IsAssignableFrom<IDictionary<string, object>>(sink.FindNode("vars").Parameters["variables"]);
            Assert.Equal(3L, variables["count"]);
            Assert.Equal(1.25, variables["ratio"]);
            Assert.Equal(false, variables["FLAG"]);
            var annotations = Assert.IsType<List<object>>(sink.FindNode("draw").Parameters["annotations"]);
            Assert.Equal("Rectangle", ((IDictionary<string, object>)annotations[0])["Type"]);
            Assert.Equal(20.5, ((IDictionary<string, object>)annotations[1])["FontSize"]);
            Assert.True(RecipeSlots.Accepts(sink.FindNode("slot"), "caption"));
            Assert.False(RecipeSlots.Accepts(sink.FindNode("slot"), "dropshadow"));
            Assert.Contains(sink.Flow.ErrorTransitions, t => t.From == "fail" && t.Recipe == "other" && t.OnError == "*");

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
                    { ""id"": ""a"", ""stepType"": ""SetVariable"", ""enabled"": false, ""parameters"": { ""Variables"": { ""X"": null } } },
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
        [InlineData(@"""Variables"": { ""X"": null }", @"""Variable"": ""X"", ""Value"": null", "'Variable' is not a parameter")]
        [InlineData(@"{ ""from"": ""a"", ""to"": ""b"" }", @"{ ""from"": ""a"", ""to"": ""b"", ""recipe"": ""other"" }", "only for error transitions")]
        [InlineData(@"""onError"": ""*""", @"""onError"": ""*"", ""branch"": ""x""", "either for a branch or for an error")]
        public void Recipe_WithAWrongParameterOrTransition_IsNotValid(string part, string mistake, string message)
        {
            Assert.Contains(part, HandWrittenRecipe);

            var error = Assert.Throws<JsonException>(() => RecipeSerializer.Deserialize(HandWrittenRecipe.Replace(part, mistake)));

            Assert.Contains(message, error.Message);
        }

        /// <summary>
        /// Each setting has one way to be written (recipe format 2.0): the ways which were removed are reported, not ignored
        /// </summary>
        [Theory]
        [InlineData(@"""stepType"": ""SetVariable"", ""parameters"": { ""Variable"": ""X"", ""Value"": 1 }", "'Variable' is not a parameter")]
        [InlineData(@"""stepType"": ""UserPrompt"", ""parameters"": { ""DefaultChoice"": ""ok"" }", "'DefaultChoice' is not a parameter")]
        [InlineData(@"""stepType"": ""UserPrompt"", ""parameters"": { ""Choices"": [ { ""Key"": ""a"", ""IsDefault"": true }, { ""Key"": ""b"", ""IsDefault"": true } ] }", "only one choice can be 'IsDefault'")]
        [InlineData(@"""stepType"": ""BarcodeScan"", ""parameters"": { ""DecodedText"": ""Barcode"" }", "'DecodedText' is not a parameter")]
        [InlineData(@"""stepType"": ""BarcodeScan"", ""parameters"": { ""Margin"": 2 }", "'Margin' is not a parameter")]
        [InlineData(@"""stepType"": ""ExternalCommand"", ""parameters"": { ""SetOutputVariable"": ""Out"" }", "'SetOutputVariable' is not a parameter")]
        [InlineData(@"""stepType"": ""Effect"", ""parameters"": { ""Effect"": ""ResizeCanvas"", ""Margin"": 10 }", "'Margin' is not a parameter")]
        [InlineData(@"""stepType"": ""Effect"", ""parameters"": { ""Effect"": ""ResizeCanvas"", ""Left"": 10, ""Color"": ""White"" }", "'Color' is the border color")]
        [InlineData(@"""stepType"": ""Effect"", ""parameters"": { ""Effect"": ""RemoveTransparency"", ""Color"": ""White"" }", "'Color' is the border color")]
        [InlineData(@"""stepType"": ""Effect"", ""parameters"": { ""Effect"": ""Resize"", ""Percentage"": 50, ""Width"": 100 }", "use 'Percentage' or 'Width' and 'Height'")]
        [InlineData(@"""stepType"": ""Annotation"", ""parameters"": { ""Annotations"": [ { ""Type"": ""Svg"", ""Content"": ""<svg/>"" } ] }", "'Svg'")]
        [InlineData(@"""stepType"": ""Annotation"", ""parameters"": { ""Annotations"": [ { ""Type"": ""Image"", ""FilePath"": ""a.png"", ""ImageData"": ""AAAA"" } ] }", "not both")]
        [InlineData(@"""stepType"": ""Pdf"", ""parameters"": { ""MeasurementUnit"": ""Cm"" }", "'MeasurementUnit' is not a parameter")]
        [InlineData(@"""stepType"": ""RecordVideo"", ""parameters"": { ""OutputFilePath"": ""a.mp4"" }", "'OutputFilePath' is not a parameter")]
        [InlineData(@"""stepType"": ""RecordVideo"", ""parameters"": { ""WindowTitle"": ""Notepad"" }", "'WindowTitle' is not a parameter")]
        [InlineData(@"""stepType"": ""Source"", ""parameters"": { ""SourceType"": ""Window"", ""WindowTitle"": ""Notepad"" }", "'WindowTitle' is not a parameter")]
        [InlineData(@"""stepType"": ""Source"", ""parameters"": { ""SourceType"": ""Window"", ""WindowTitlePattern"": ""Report (draft"" }", "not a valid regular expression")]
        public void Recipe_WithARemovedWayOfWritingASetting_IsNotValid(string node, string message)
        {
            string recipe = @"{ ""id"": ""removed"", ""name"": ""Removed"", ""nodes"": [ { ""id"": ""a"", " + node + @" } ], ""flow"": { ""startNodes"": [""a""] } }";

            var error = Assert.Throws<JsonException>(() => RecipeSerializer.Deserialize(recipe));

            Assert.Contains(message, error.Message);
        }

        private const string ConditionalRecipe = @"
            {
                ""id"": ""decide"",
                ""name"": ""Decide"",
                ""nodes"": [
                    { ""id"": ""decide_size"", ""stepType"": ""Conditional"", ""parameters"": { ""Branches"": [
                        { ""Key"": ""Large"", ""Expression"": ""${payload.width > 800}"" },
                        { ""Key"": ""Small"", ""Expression"": ""else"" } ] } },
                    { ""id"": ""large"", ""stepType"": ""SetVariable"" },
                    { ""id"": ""small"", ""stepType"": ""SetVariable"" }
                ],
                ""flow"": {
                    ""startNodes"": [""decide_size""],
                    ""transitions"": [ { ""from"": ""decide_size"", ""branch"": ""Large"", ""to"": ""large"" }, { ""from"": ""decide_size"", ""branch"": ""Small"", ""to"": ""small"" } ]
                }
            }";

        /// <summary>
        /// A parameter which holds objects is read into its declared type, as strictly as the recipe itself
        /// </summary>
        [Fact]
        public void Recipe_ObjectParameter_IsReadIntoItsType()
        {
            var recipe = RecipeSerializer.Deserialize(ConditionalRecipe);

            var branches = Assert.IsType<List<ConditionalBranch>>(recipe.FindNode("decide_size").Parameters["Branches"]);
            Assert.Equal(new[] { "Large", "Small" }, branches.Select(b => b.Key));
            Assert.Equal("else", branches[1].Expression);
            Assert.Contains("\"Key\": \"Large\"", RecipeSerializer.Serialize(recipe));
        }

        [Theory]
        [InlineData(@"""Expression"": ""${payload", @"""Expresion"": ""${payload", "'Branches', item 1: 'Expresion' is not one of Key, Expression.")]
        [InlineData(@"""Key"": ""Small""", @"""key"": ""Small""", "'Branches', item 2: 'key' is written 'Key'.")]
        [InlineData(@", ""Expression"": ""else""", "", "'Branches', item 2: 'Expression' is missing.")]
        [InlineData(@"""Key"": ""Small""", @"""Key"": 2", "'Branches', item 2: the value doesn't fit, it must be a list of objects with Key, Expression.")]
        public void Recipe_ObjectParameterWithAMistake_IsNotValid(string part, string mistake, string message)
        {
            Assert.Contains(part, ConditionalRecipe);

            var error = Assert.Throws<JsonException>(() => RecipeSerializer.Deserialize(ConditionalRecipe.Replace(part, mistake)));

            Assert.Contains("Node 'decide_size': the parameter " + message, error.Message);
        }

        private const string TypedValuesRecipe = @"
            {
                ""id"": ""typed"",
                ""name"": ""Typed"",
                ""nodes"": [
                    { ""id"": ""capture"", ""stepType"": ""Source"", ""parameters"": { ""SourceType"": ""Window"", ""WindowHandle"": 4242 } },
                    { ""id"": ""torn"", ""stepType"": ""Effect"", ""parameters"": { ""Effect"": ""TornEdge"", ""Edges"": [ ""Top"", ""Bottom"" ] } },
                    { ""id"": ""slot"", ""stepType"": ""Slot"", ""parameters"": { ""Name"": ""BeforeExport"", ""Accept"": [ ""ext_border"" ] } },
                    { ""id"": ""draw"", ""stepType"": ""Annotation"", ""parameters"": { ""Annotations"": [
                        { ""Type"": ""Text"", ""Text"": ""Hello"", ""FontSize"": 12, ""Width"": ""${payload.width / 2}"", ""Bold"": true },
                        { ""Type"": ""TestBadge"", ""Label"": ""New"", ""OffsetX"": -5 } ] } }
                ],
                ""flow"": {
                    ""startNodes"": [""capture""],
                    ""transitions"": [ { ""from"": ""capture"", ""to"": ""torn"" }, { ""from"": ""torn"", ""to"": ""slot"" }, { ""from"": ""slot"", ""to"": ""draw"" } ]
                }
            }";

        /// <summary>
        /// Lists of names, window handles and the properties of annotations are read in their declared types
        /// </summary>
        [Fact]
        public void Recipe_TypedValues_AreReadIntoTheirTypes()
        {
            var recipe = RecipeSerializer.Deserialize(TypedValuesRecipe);

            Assert.Equal(4242, recipe.FindNode("capture").Parameters["WindowHandle"]);
            Assert.Equal(new[] { "Top", "Bottom" }, Assert.IsType<List<string>>(recipe.FindNode("torn").Parameters["Edges"]));
            Assert.True(RecipeSlots.Accepts(recipe.FindNode("slot"), "ext_border"));
            Assert.False(RecipeSlots.Accepts(recipe.FindNode("slot"), "ext_caption"));
            var annotations = Assert.IsType<List<Dictionary<string, object>>>(recipe.FindNode("draw").Parameters["Annotations"]);
            Assert.Equal(12.0, annotations[0]["FontSize"]);
            Assert.Equal(-5, annotations[1]["OffsetX"]);
        }

        [Theory]
        [InlineData(@"""WindowHandle"": 4242", @"""WindowHandle"": ""main""", "'WindowHandle' must be a window handle")]
        [InlineData(@"[ ""Top"", ""Bottom"" ]", @"[ ""top"", ""Bottom"" ]", "Node 'torn': the parameter 'Edges', 'top' is not one of Top, Right, Bottom, Left.")]
        [InlineData(@"""Accept"": [ ""ext_border"" ]", @"""Accept"": ""all""", "Node 'slot': the parameter 'Accept', the value doesn't fit, it must be a list of texts.")]
        [InlineData(@"""FontSize"": 12", @"""fontSize"": 12", "Node 'draw': annotation 1 (Text): 'fontSize' is written 'FontSize'.")]
        [InlineData(@"""FontSize"": 12", @"""FontSize"": ""big""", "Node 'draw': annotation 1 (Text): 'FontSize' must be a number, not a text.")]
        [InlineData(@"""Bold"": true", @"""Bold"": true, ""TailX"": 3", "Node 'draw': annotation 1 (Text): 'TailX' is not a property of a Text annotation.")]
        [InlineData(@"""Label"": ""New""", @"""Text"": ""New""", "Node 'draw': annotation 2 (TestBadge): 'Text' is not a property of a TestBadge annotation.")]
        [InlineData(@"{ ""Type"": ""TestBadge"", ", "{ ", "Node 'draw': annotation 2: 'Type' is missing.")]
        public void Recipe_TypedValueWithAMistake_IsNotValid(string part, string mistake, string message)
        {
            Assert.Contains(part, TypedValuesRecipe);

            var error = Assert.Throws<JsonException>(() => RecipeSerializer.Deserialize(TypedValuesRecipe.Replace(part, mistake)));

            Assert.Contains(message, error.Message);
        }

        [Fact]
        public void Step_WithAValueTypeOfNoJsonContext_IsNotRegistered()
        {
            var contract = new StepContract("UnknownValueType", parameters: new[] { new ParameterContract("Target", ContractDataType.Object) { ValueType = typeof(Uri) } });

            var error = Assert.Throws<InvalidOperationException>(() => StepRegistry.Instance.Register(contract, config => null));

            Assert.Contains("'Target'", error.Message);
            Assert.False(StepRegistry.Instance.IsRegistered("UnknownValueType"));
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
