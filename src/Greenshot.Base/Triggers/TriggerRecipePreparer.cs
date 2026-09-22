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
using System.Linq;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Base.Triggers
{
    /// <summary>
    /// Prepares recipes for execution by a trigger.
    /// </summary>
    public static class TriggerRecipePreparer
    {

        private static readonly ILog Log = LogManager.GetLogger(typeof(TriggerRecipePreparer));

        /// <summary>
        /// Prepares a recipe for execution by a trigger.
        /// </summary>
        /// <param name="recipe">The recipe to prepare.</param>
        /// <param name="trigger">The trigger that determines the source and destination configuration.</param>
        /// <returns>The prepared recipe.</returns>
        public static CaptureRecipe Prepare(CaptureRecipe recipe, ITrigger trigger)
        {
            recipe = EnsureRequiredStartSource(recipe, trigger);
            recipe = EnsureRequiredDestination(recipe, trigger);
            return recipe;
        }

        /// <summary>
        /// Prepares a recipe for execution in the recipe editor's test run, using a TestRunTrigger as the trigger with type ContextMenu.
        /// </summary>
        /// <param name="recipe">The recipe to prepare.</param>
        /// <returns>The prepared recipe. Creates a clone in every case.</returns>
        public static CaptureRecipe PrepareForTestRun(CaptureRecipe recipe)
        {
            return Prepare(recipe, new TestRunTrigger()).Clone();
        }
        private class TestRunTrigger : ITrigger
        {
            public string TriggerType => TriggerConfig.TypeContextMenu;

            public string Id => "test_run_trigger";
            string ITrigger.Name { get; set; }
            public string Name => "Test Run Trigger";
            public string TargetRecipeId => null;
            public bool IsEnabled { get; set; }
            public void Start() { }
            public void Stop() { }
            public event EventHandler<TriggerEventArgs> Triggered;
            string ITrigger.TargetRecipeId { get; set; }
            public void Dispose() { }
        }

        /// <summary>
        /// Ensures that the given recipe has at least one start source node.
        /// If the recipe does not have a source node, a trigger-specific source node is added
        /// and connected to the effective start nodes of the given recipe.
        /// It does not modify the recipe if it already has a source node.
        /// </summary>
        /// <param name="recipe">The recipe to inspect.</param>
        /// <param name="trigger">The trigger that determines the source type.</param>
        /// <returns>A clone of the recipe with the required start source node added, or the original recipe if no changes were needed.</returns>
        public static CaptureRecipe EnsureRequiredStartSource(CaptureRecipe recipe, ITrigger trigger)
        {
            if (recipe == null || recipe.HasSourceStep()) return recipe;

            var startNodeIds = recipe.Flow?.GetEffectiveStartNodes() ?? [];
            if (startNodeIds.Count == 0 && recipe.Nodes?.Count > 0)
            {
                startNodeIds.Add(recipe.Nodes[0].Id);
            }

            var recipeToExecute = recipe.Clone();
            var sourceNodeId = $"trigger_source_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            var sourceNodeSuffix = 1;
            while (recipeToExecute.FindNode(sourceNodeId) != null)
            {
                sourceNodeId = $"{sourceNodeId}_{sourceNodeSuffix++}";
            }
            var sourceNode = GetTriggerSpecificSourceNode(sourceNodeId, trigger);
            recipeToExecute.Nodes.Insert(0, sourceNode);
            recipeToExecute.Flow.StartNodes = [sourceNodeId];
            recipeToExecute.Flow.AddTransitions(sourceNodeId, startNodeIds);

            Log.Info($"Added required start source node '{sourceNodeId}' for trigger '{trigger.TriggerType}' to recipe '{recipe.Name}', because it did not have a source step.");

            return recipeToExecute;
        }

        /// <summary>
        /// Ensures that the given recipe has at least one destination.
        /// If the recipe has no destination node, a trigger-specific destination is added
        /// to every node without outgoing transitions.
        /// It does not modify the recipe if it already has a destination node.
        /// </summary>
        /// <param name="recipe">The recipe to inspect.</param>
        /// <param name="trigger">The trigger that determines the destination configuration.</param>
        /// <returns>A clone of the recipe with the required destination nodes added, or the original recipe if no changes were needed.</returns>
        public static CaptureRecipe EnsureRequiredDestination(CaptureRecipe recipe, ITrigger trigger)
        {
            if (recipe == null || recipe.HasDestinationStep()) return recipe;

            var transitions = recipe.Flow?.GetUnifiedTransitions();
            var endNodeIds = recipe.Nodes?
                .Where(node => node != null && !string.IsNullOrWhiteSpace(node.Id))
                .Where(node => transitions == null || !transitions.TryGetValue(node.Id, out var targets) || targets == null || targets.Count == 0)
                .Select(node => node.Id)
                .ToList();

            if (endNodeIds == null || endNodeIds.Count == 0)
            {
                return recipe;
            }

            var recipeToExecute = recipe.Clone();
            foreach (var endNodeId in endNodeIds)
            {
                var editorNodeId = $"editor_destination_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                var editorNodeSuffix = 1;
                while (recipeToExecute.FindNode(editorNodeId) != null)
                {
                    editorNodeId = $"{editorNodeId}_{editorNodeSuffix++}";
                }

                var editorNode = GetTriggerSpecificDestinationNode(editorNodeId, trigger);
                recipeToExecute.Nodes.Add(editorNode);
                recipeToExecute.Flow.AddTransition(endNodeId, editorNodeId);
            }

            Log.Info($"Added required editor destination nodes with TargetEditor.CurrentEditor to recipe '{recipe.Name}', because it did not have a destination step.");
            return recipeToExecute;
        }

        /// <summary>
        /// Gets a trigger-specific source node configuration based on the trigger type.
        /// </summary>
        /// <param name="sourceNodeId"></param>
        /// <param name="trigger"></param>
        /// <returns></returns>
        private static RecipeNodeConfig GetTriggerSpecificSourceNode(string sourceNodeId, ITrigger trigger)
        {
            var sourceType = trigger.TriggerType switch
            {
                TriggerConfig.TypeClipboard => CaptureSourceType.Clipboard,
                TriggerConfig.TypeEditor => CaptureSourceType.CurrentEditor,
                _ => CaptureSourceType.FullScreen
            };
            return RecipeStepConfig.CreateSource(sourceNodeId, sourceType);
        }

        /// <summary>
        /// Gets a trigger-specific destination node configuration based on the trigger type.
        /// </summary>
        /// <param name="editorNodeId"></param>
        /// <param name="trigger"></param>
        /// <returns></returns>
        private static RecipeNodeConfig GetTriggerSpecificDestinationNode(string editorNodeId, ITrigger trigger)
        {
            switch (trigger.TriggerType)
            {
                case TriggerConfig.TypeEditor:
                {
                    var editorNode = RecipeStepConfig.CreateEditor(editorNodeId);
                    editorNode.Set("TargetEditor", nameof(TargetEditor.CurrentEditor));
                    return editorNode;
                }
                default:
                {
                    var editorNode = RecipeStepConfig.CreateDestinations(editorNodeId);
                    return editorNode;
                }
            }
        }

    }
}