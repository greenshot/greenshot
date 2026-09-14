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

using System.Collections.Generic;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Newtonsoft.Json;
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
                Version = "1.0",
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

            string json = JsonConvert.SerializeObject(original, Formatting.Indented);
            var deserialized = JsonConvert.DeserializeObject<CaptureRecipe>(json);

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
    }
}
