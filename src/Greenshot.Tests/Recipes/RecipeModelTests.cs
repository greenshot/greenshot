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

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Triggers;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeModelTests
    {
        public RecipeModelTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Recipe_CreationAndNodeManagement_WorksCorrectly()
        {
            var recipe = new CaptureRecipe("test_recipe", "Test Recipe", "Test Description")
                .AddNode(new RecipeNodeConfig
                {
                    Id = "node_1",
                    Name = "Step 1",
                    StepType = "Source",
                    Parameters = new Dictionary<string, object>
                    {
                        { "Threshold", 50 },
                        { "Prefix", "Screenshot_" }
                    }
                })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "node_2",
                    Name = "Step 2",
                    StepType = "Filter"
                });

            Assert.Equal("test_recipe", recipe.Id);
            Assert.Equal("Test Recipe", recipe.Name);
            Assert.Equal(2, recipe.Nodes.Count);

            var node1 = recipe.FindNode("node_1");
            Assert.NotNull(node1);
            Assert.Equal("Source", node1.StepType);
            Assert.Equal(50, node1.GetParameter<int>("Threshold"));
            Assert.Equal("Screenshot_", node1.GetParameter<string>("Prefix"));
            Assert.Equal(100, node1.GetParameter<int>("MissingParam", 100));

            var node2 = recipe.FindNode("node_2");
            Assert.NotNull(node2);
            Assert.Null(recipe.FindNode("non_existent"));
        }

        [Fact]
        public void Recipe_FlowTransitions_GetUnifiedTransitionsAndStartNodes()
        {
            var recipe = new CaptureRecipe("flow_test", "Flow Test")
                .AddNode(new RecipeNodeConfig { Id = "start_node", StepType = "Source" })
                .AddNode(new RecipeNodeConfig { Id = "branch_a", StepType = "BranchA" })
                .AddNode(new RecipeNodeConfig { Id = "branch_b", StepType = "BranchB" })
                .AddNode(new RecipeNodeConfig { Id = "join_node", StepType = "Join" });

            recipe.Flow = new RecipeFlowConfig("start_node")
                .AddTransition("start_node", "branch_a")
                .AddTransition("start_node", "branch_b")
                .AddTransition("branch_a", "join_node")
                .AddTransition("branch_b", "join_node");

            var startNodes = recipe.Flow.GetEffectiveStartNodes();
            Assert.Single(startNodes);
            Assert.Equal("start_node", startNodes[0]);

            var transitions = recipe.Flow.GetUnifiedTransitions();
            Assert.Equal(3, transitions.Count);
            Assert.Equal(2, transitions["start_node"].Count);
            Assert.Contains("branch_a", transitions["start_node"]);
            Assert.Contains("branch_b", transitions["start_node"]);
            Assert.Equal(new List<string> { "join_node" }, transitions["branch_a"]);
            Assert.Equal(new List<string> { "join_node" }, transitions["branch_b"]);
        }

        [Fact]
        public void Recipe_JsonSerializationRoundtrip_PreservesAllProperties()
        {
            var original = new CaptureRecipe("json_test", "JSON Test", "Serialization test")
            {
                Version = "2.0",
                IsBuiltIn = false
            };

            original.AddTrigger(TriggerConfig.CreateHotkey("Ctrl+Shift+R"));

            original.AddNode(new RecipeNodeConfig
            {
                Id = "node_source",
                Name = "Capture Region",
                StepType = "RegionCapture",
                Enabled = true,
                Parameters = new Dictionary<string, object>
                {
                    { "ShowMagnifier", true },
                    { "DelayMs", 250 }
                }
            });

            original.Flow = new RecipeFlowConfig("node_source")
                .AddTransition("node_source", "node_dest")
                .AddConditionalTransition("node_source", "ConditionAlt", "node_alt");

            string json = JsonSerializer.Serialize(original, RecipeSerializer.Options);
            var deserialized = JsonSerializer.Deserialize<CaptureRecipe>(json, RecipeSerializer.Options);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Id, deserialized.Id);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.Description, deserialized.Description);
            Assert.Single(deserialized.Triggers);
            Assert.Equal(TriggerConfig.TypeHotkey, deserialized.Triggers[0].TriggerType);
            Assert.Equal("Ctrl+Shift+R", deserialized.Triggers[0].GetParameter<string>("Hotkey"));
            Assert.Single(deserialized.Nodes);
            Assert.Equal("node_source", deserialized.Nodes[0].Id);
            Assert.Equal("RegionCapture", deserialized.Nodes[0].StepType);

            var unified = deserialized.Flow.GetUnifiedTransitions();
            Assert.Contains("node_dest", unified["node_source"]);
            Assert.Contains("node_alt", unified["node_source"]);
        }

        [Fact]
        public void RecipeStepConfig_CreateEditor_UsesTargetEditor()
        {
            var node = RecipeStepConfig.CreateEditor(targetEditor: TargetEditor.CurrentEditor);

            Assert.Equal("CurrentEditor", node.GetParameter<string>("TargetEditor"));
            // check old obsolete parameters 
            Assert.False(node.HasParameter("ReuseEditor"));
            Assert.False(node.HasParameter("ReuseCurrentEditor"));
        }

        [Fact]
        public void RecipeNodeConfig_Parameters_AreAlwaysCaseInsensitiveEvenAfterDeserialization()
        {
            string json = @"
            {
                ""id"": ""test_node"",
                ""stepType"": ""ExternalCommand"",
                ""parameters"": {
                    ""path"": ""C:\\tools\\test.exe"",
                    ""commandline"": ""test.exe --flag"",
                    ""RUNINBACKGROUND"": true
                }
            }";

            var node = JsonSerializer.Deserialize<RecipeNodeConfig>(json, RecipeSerializer.Options);
            Assert.NotNull(node);
            Assert.True(node.HasParameter("Path"));
            Assert.True(node.HasParameter("path"));
            Assert.True(node.HasParameter("PATH"));
            Assert.Equal("C:\\tools\\test.exe", node.GetParameter<string>("Path"));
            Assert.Equal("C:\\tools\\test.exe", node.GetParameter<string>("path"));
            Assert.Equal("test.exe --flag", node.GetParameter<string>("CommandLine"));
            Assert.True(node.GetParameter<bool>("RunInBackground"));
        }
        [Fact]
        public void Recipe_AuthorsCopyrightAndLicense_AreReadAndWritten()
        {
            const string json = @"{
                ""id"": ""credited"", ""name"": ""Credited"",
                ""authors"": [ { ""name"": ""Jane Doe"", ""github"": ""janedoe"" }, { ""name"": ""John Roe"" } ],
                ""copyright"": ""Copyright (c) 2026 Jane Doe"",
                ""license"": ""MIT"",
                ""nodes"": [ { ""id"": ""a"", ""stepType"": ""SetVariable"" } ],
                ""flow"": { ""startNodes"": [ ""a"" ] }
            }";

            var recipe = RecipeSerializer.Deserialize(json, validate: false);

            Assert.Equal(new[] { "Jane Doe", "John Roe" }, recipe.Authors.Select(a => a.Name));
            Assert.Equal("janedoe", recipe.Authors[0].GitHub);
            Assert.Null(recipe.Authors[1].GitHub);
            Assert.Equal("Copyright (c) 2026 Jane Doe", recipe.Copyright);
            Assert.Equal("MIT", recipe.License);

            var clone = recipe.Clone();
            Assert.Equal("John Roe", clone.Authors[1].Name);
            Assert.NotSame(recipe.Authors[0], clone.Authors[0]);
            Assert.Equal("MIT", clone.License);

            string written = RecipeSerializer.Serialize(recipe);
            Assert.Contains(@"""github"": ""janedoe""", written);
            Assert.DoesNotContain(@"""github"": null", written);
            Assert.Equal("Copyright (c) 2026 Jane Doe", RecipeSerializer.Deserialize(written, validate: false).Copyright);
        }

        [Fact]
        public void Recipe_WithoutAuthors_WritesNoAuthors()
        {
            string written = RecipeSerializer.Serialize(new CaptureRecipe("plain", "Plain"));

            Assert.DoesNotContain("authors", written);
            Assert.DoesNotContain("copyright", written);
            Assert.DoesNotContain("license", written);
        }

        [Theory]
        [InlineData(@"""authors"": [ { ""github"": ""janedoe"" } ]", "needs a 'name'")]
        [InlineData(@"""license"": "" """, "'license' is empty")]
        public void Recipe_InvalidCredits_AreErrors(string property, string expected)
        {
            string json = @"{ ""id"": ""credited"", ""name"": ""Credited"", " + property + @",
                ""nodes"": [ { ""id"": ""a"", ""stepType"": ""SetVariable"" } ], ""flow"": { ""startNodes"": [ ""a"" ] } }";

            var result = RecipeValidator.Validate(RecipeSerializer.Deserialize(json, validate: false));

            Assert.Contains(result.Errors, e => e.Contains(expected));
        }

        /// <summary>
        /// Greenshot sets what it knows about a loaded recipe itself: never written, and skipped in files an earlier beta saved
        /// </summary>
        [Fact]
        public void Recipe_RuntimeProperties_AreNotWrittenAndSkippedWhenRead()
        {
            var recipe = new CaptureRecipe("runtime", "Runtime")
            {
                IsBuiltIn = true,
                IsOverridden = true,
                IsEnabled = false,
                FilePath = @"C:\Users\someone\runtime.gsrecipe.json"
            };

            string written = RecipeSerializer.Serialize(recipe);
            Assert.DoesNotContain("isBuiltIn", written);
            Assert.DoesNotContain("isOverridden", written);
            Assert.DoesNotContain("isEnabled", written);
            Assert.DoesNotContain("filePath", written);
            Assert.DoesNotContain("someone", written);

            const string earlierBeta = @"{ ""id"": ""old"", ""name"": ""Old"", ""isBuiltIn"": true, ""isOverridden"": true, ""isEnabled"": false,
                ""filePath"": ""C:\\elsewhere.json"", ""nodes"": [ { ""id"": ""a"", ""stepType"": ""SetVariable"" } ], ""flow"": { ""startNodes"": [ ""a"" ] } }";
            var read = RecipeSerializer.Deserialize(earlierBeta, validate: false);
            Assert.False(read.IsBuiltIn);
            Assert.False(read.IsOverridden);
            Assert.True(read.IsEnabled);
            Assert.Null(read.FilePath);
            Assert.False(Assert.Single(RecipeSerializer.DeserializeList("[" + earlierBeta + "]", validate: false)).IsBuiltIn);

            var extension = RecipeSerializer.DeserializeExtension(@"{ ""kind"": ""extension"", ""id"": ""ext_old"", ""name"": ""Old"", ""isBuiltIn"": true,
                ""extends"": { ""recipes"": [ ""*"" ], ""slot"": ""BeforeExport"" }, ""nodes"": [ { ""id"": ""a"", ""stepType"": ""SetVariable"" } ],
                ""flow"": { ""startNodes"": [ ""a"" ] } }", validate: false);
            Assert.False(extension.IsBuiltIn);
            Assert.DoesNotContain("isBuiltIn", RecipeSerializer.Serialize(new RecipeExtension { Id = "ext_new", Name = "New", IsBuiltIn = true }));
        }
    }
}
