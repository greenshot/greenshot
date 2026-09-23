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
using System.Linq;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Triggers;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class TriggerRecipePreparerTests
    {
        public TriggerRecipePreparerTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void EnsureRequiredStartSource_EditorTriggerAddsCurrentEditorSourceToRecipeWithoutSource()
        {
            var recipe = new CaptureRecipe("without_source", "Without Source")
                .AddNode(CreateEffectNode("effect_e095"));
            recipe.Flow = new RecipeFlowConfig("effect_e095");
            var trigger = new EditorTrigger("editor_trigger", "Editor Trigger", "Editor Trigger", recipe.Id);

            var preparedRecipe = TriggerRecipePreparer.EnsureRequiredStartSource(recipe, trigger);

            Assert.NotSame(recipe, preparedRecipe);
            var sourceNode = Assert.Single(preparedRecipe.Nodes, node => node.StepType == WellKnownStepTypes.Source);
            Assert.StartsWith("trigger_source_", sourceNode.Id);
            Assert.Equal(CaptureSourceType.CurrentEditor, sourceNode.GetParameter<CaptureSourceType>("SourceType"));
            Assert.Equal(new[] { sourceNode.Id }, preparedRecipe.Flow.StartNodes);
            Assert.Contains("effect_e095", preparedRecipe.Flow.Transitions[sourceNode.Id]);
            Assert.Single(recipe.Nodes);
        }

        [Fact]
        public void EnsureRequiredStartSource_ReturnsSameRecipeWhenSourceExists()
        {
            var recipe = new CaptureRecipe("with_source", "With Source")
                .AddNode(CreateSourceNode("source_f056"));
            recipe.Flow = new RecipeFlowConfig("source_f056");
            var trigger = new EditorTrigger("editor_trigger", "Editor Trigger", "Editor Trigger", recipe.Id);

            var preparedRecipe = TriggerRecipePreparer.EnsureRequiredStartSource(recipe, trigger);

            Assert.Same(recipe, preparedRecipe);
        }

        [Fact]
        public void EnsureRequiredDestination_EditorTriggerAddsCurrentEditorDestinationAfterEveryEndNode()
        {
            var recipe = new CaptureRecipe("without_destination", "Without Destination")
                .AddNode(CreateSourceNode("source_f056"))
                .AddNode(CreateEffectNode("effect_a"))
                .AddNode(CreateEffectNode("effect_b"));
            recipe.Flow = new RecipeFlowConfig("source_f056")
                .AddTransition("source_f056", "effect_a")
                .AddTransition("source_f056", "effect_b");
            var trigger = new EditorTrigger("editor_trigger", "Editor Trigger", "Editor Trigger", recipe.Id);

            var preparedRecipe = TriggerRecipePreparer.EnsureRequiredDestination(recipe, trigger);

            Assert.NotSame(recipe, preparedRecipe);
            var editorNodes = preparedRecipe.Nodes
                .Where(node => node.StepType == WellKnownStepTypes.Editor)
                .ToList();
            Assert.Equal(2, editorNodes.Count);
            Assert.All(editorNodes, node =>
            {
                Assert.StartsWith("editor_destination_", node.Id);
                Assert.Equal(nameof(TargetEditor.CurrentEditor), node.GetParameter<string>("TargetEditor"));
            });
            Assert.All(["effect_a", "effect_b"], endNodeId =>
            {
                Assert.Contains(endNodeId, preparedRecipe.Flow.Transitions.Keys);
                Assert.Single(preparedRecipe.Flow.Transitions[endNodeId]);
                Assert.Contains(preparedRecipe.Flow.Transitions[endNodeId][0], editorNodes.Select(node => node.Id));
            });
            Assert.Equal(3, recipe.Nodes.Count);
        }

        [Fact]
        public void EnsureRequiredDestination_ReturnsSameRecipeWhenDestinationExists()
        {
            var recipe = new CaptureRecipe("with_destination", "With Destination")
                .AddNode(CreateSourceNode("source_f056"))
                .AddNode(CreateEffectNode("effect_e095"))
                .AddNode(CreateEditorNode("editor_4933"));
            recipe.Flow = new RecipeFlowConfig("source_f056")
                .AddTransition("source_f056", "effect_e095")
                .AddTransition("effect_e095", "editor_4933");
            var trigger = new EditorTrigger("editor_trigger", "Editor Trigger", "Editor Trigger", recipe.Id);

            var preparedRecipe = TriggerRecipePreparer.EnsureRequiredDestination(recipe, trigger);

            Assert.Same(recipe, preparedRecipe);
        }

        [Fact]
        public void Prepare_EditorTriggerAddsSourceAndEditorDestinationWhenBothAreMissing()
        {
            var recipe = new CaptureRecipe("incomplete", "Incomplete")
                .AddNode(CreateEffectNode("effect_e095"));
            recipe.Flow = new RecipeFlowConfig("effect_e095");
            var trigger = new EditorTrigger("editor_trigger", "Editor Trigger", "Editor Trigger", recipe.Id);

            var preparedRecipe = TriggerRecipePreparer.Prepare(recipe, trigger);

            Assert.NotSame(recipe, preparedRecipe);
            var sourceNode = Assert.Single(preparedRecipe.Nodes, node => node.StepType == WellKnownStepTypes.Source);
            var editorNode = Assert.Single(preparedRecipe.Nodes, node => node.StepType == WellKnownStepTypes.Editor);
            Assert.Equal(CaptureSourceType.CurrentEditor, sourceNode.GetParameter<CaptureSourceType>("SourceType"));
            Assert.Equal(nameof(TargetEditor.CurrentEditor), editorNode.GetParameter<string>("TargetEditor"));
            Assert.Equal(new[] { sourceNode.Id }, preparedRecipe.Flow.StartNodes);
            Assert.Contains("effect_e095", preparedRecipe.Flow.Transitions[sourceNode.Id]);
            Assert.Contains(editorNode.Id, preparedRecipe.Flow.Transitions["effect_e095"]);
        }

        [Fact]
        public void Prepare_ReturnsSameRecipeWhenSourceAndDestinationExist()
        {
            var recipe = new CaptureRecipe("complete", "Complete")
                .AddNode(CreateSourceNode("source_f056"))
                .AddNode(CreateEffectNode("effect_e095"))
                .AddNode(CreateEditorNode("editor_4933"));
            recipe.Flow = new RecipeFlowConfig("source_f056")
                .AddTransition("source_f056", "effect_e095")
                .AddTransition("effect_e095", "editor_4933");
            var trigger = new EditorTrigger("editor_trigger", "Editor Trigger", "Editor Trigger", recipe.Id);

            var preparedRecipe = TriggerRecipePreparer.Prepare(recipe, trigger);

            Assert.Same(recipe, preparedRecipe);
        }

        [Fact]
        public void Prepare_ClipboardTriggerAddsClipboardSourceAndDestinationsWhenBothAreMissing()
        {
            var recipe = new CaptureRecipe("clipboard_recipe", "Clipboard Recipe")
                .AddNode(CreateEffectNode("effect_e095"));
            recipe.Flow = new RecipeFlowConfig("effect_e095");
            var trigger = new ClipboardTrigger("clipboard_trigger", "Clipboard Trigger", recipe.Id);

            var preparedRecipe = TriggerRecipePreparer.Prepare(recipe, trigger);

            Assert.NotSame(recipe, preparedRecipe);
            var sourceNode = Assert.Single(preparedRecipe.Nodes, node => node.StepType == WellKnownStepTypes.Source);
            var destinationNode = Assert.Single(preparedRecipe.Nodes, node => node.StepType == WellKnownStepTypes.Destinations);
            Assert.Equal(CaptureSourceType.Clipboard, sourceNode.GetParameter<CaptureSourceType>("SourceType"));
            Assert.Equal(new[] { sourceNode.Id }, preparedRecipe.Flow.StartNodes);
            Assert.Contains("effect_e095", preparedRecipe.Flow.Transitions[sourceNode.Id]);
            Assert.Contains(destinationNode.Id, preparedRecipe.Flow.Transitions["effect_e095"]);
        }

        [Fact]
        public void Prepare_VideoRecipe_DoesNotAddSourceOrDestination()
        {
            var recipe = new CaptureRecipe("video_recipe", "Video Recipe")
                .AddNode(CreateRecordVideoNode("record_1"));
            recipe.Flow = new RecipeFlowConfig("record_1");
            var trigger = new HotkeyTrigger("hotkey_trigger", "Hotkey Trigger", "Pause", recipe.Id);

            var preparedRecipe = TriggerRecipePreparer.Prepare(recipe, trigger);

            Assert.Same(recipe, preparedRecipe);
            Assert.Single(preparedRecipe.Nodes);
            Assert.Equal(WellKnownStepTypes.RecordVideo, preparedRecipe.Nodes[0].StepType);
        }

        [Fact]
        public void Prepare_VideoRecipeWithNotification_DoesNotAddSourceOrDestination()
        {
            var recipe = new CaptureRecipe("video_recipe", "Video Recipe")
                .AddNode(CreateRecordVideoNode("record_1"))
                .AddNode(new RecipeNodeConfig
                {
                    Id = "notify_1",
                    StepType = WellKnownStepTypes.Notification,
                    Name = "Notification",
                    Enabled = true
                });
            recipe.Flow = new RecipeFlowConfig("record_1")
                .AddTransition("record_1", "notify_1");
            var trigger = new HotkeyTrigger("hotkey_trigger", "Hotkey Trigger", "Pause", recipe.Id);

            var preparedRecipe = TriggerRecipePreparer.Prepare(recipe, trigger);

            Assert.Same(recipe, preparedRecipe);
            Assert.Equal(2, preparedRecipe.Nodes.Count);
            Assert.DoesNotContain(preparedRecipe.Nodes, n => n.StepType == WellKnownStepTypes.Destinations);
            Assert.DoesNotContain(preparedRecipe.Nodes, n => n.StepType == WellKnownStepTypes.Source);
        }

        private static RecipeNodeConfig CreateRecordVideoNode(string id)
        {
            return new RecipeNodeConfig
            {
                Id = id,
                StepType = WellKnownStepTypes.RecordVideo,
                Name = "Record Video",
                Enabled = true,
                Parameters = new Dictionary<string, object>
                {
                    ["targetType"] = "Window",
                    ["outputFilePath"] = "test.mp4"
                }
            };
        }

        private static RecipeNodeConfig CreateSourceNode(string id)
        {
            return new RecipeNodeConfig
            {
                Id = id,
                StepType = WellKnownStepTypes.Source,
                Name = "Source",
                Enabled = true,
                Parameters = new Dictionary<string, object>
                {
                    ["sourceType"] = "Clipboard",
                    ["captureMouseCursor"] = null,
                    ["delayMs"] = 0
                }
            };
        }

        private static RecipeNodeConfig CreateEditorNode(string id)
        {
            return new RecipeNodeConfig
            {
                Id = id,
                StepType = WellKnownStepTypes.Editor,
                Name = "Editor",
                Enabled = true,
                Parameters = new Dictionary<string, object>
                {
                    ["destination"] = "Editor",
                    ["targetEditor"] = "CurrentEditor"
                }
            };
        }

        private static RecipeNodeConfig CreateEffectNode(string id)
        {
            return new RecipeNodeConfig
            {
                Id = id,
                StepType = WellKnownStepTypes.Effect,
                Name = "Effect",
                Enabled = true,
                Parameters = new Dictionary<string, object>
                {
                    ["effect"] = "Border",
                    ["shadowSize"] = 10,
                    ["darkness"] = 0.6
                }
            };
        }
    }
}
