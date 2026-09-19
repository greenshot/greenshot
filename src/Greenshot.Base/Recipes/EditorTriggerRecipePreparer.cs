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
using System.Linq;
using Greenshot.Base.Core.Enums;
using log4net;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Prepares recipes for execution from an image editor trigger.
    /// </summary>
    public static class EditorTriggerRecipePreparer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EditorTriggerRecipePreparer));
        public static CaptureRecipe Prepare(CaptureRecipe recipe)
        {
            recipe = EnsureRequiredStartSource(recipe);
            recipe = EnsureRequiredEditorDestination(recipe);
            return recipe;
        }

        /// <summary>
        /// Ensures that the given recipe has a required start source node.
        /// If the recipe does not have at least one start node with the step type of "Source",
        /// a new source node with SourceType of "CurrentEditor" will be added to all start nodes of the given recipe.
        /// </summary>
        /// <param name="recipe">The recipe to inspect.</param>
        /// <returns>The modified recipe with the required start source node added, or the original recipe if no changes were needed.</returns>
        public static CaptureRecipe EnsureRequiredStartSource(CaptureRecipe recipe)
        {
            if (recipe == null || recipe.HasSourceStep()) return recipe;

            var startNodeIds = recipe.Flow?.GetEffectiveStartNodes() ?? new List<string>();
            if (startNodeIds.Count == 0 && recipe.Nodes?.Count > 0)
            {
                startNodeIds.Add(recipe.Nodes[0].Id);
            }

            var recipeToExecute = recipe.Clone();
            string sourceNodeId = $"editor_source_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            int sourceNodeSuffix = 1;
            while (recipeToExecute.FindNode(sourceNodeId) != null)
            {
                sourceNodeId = $"{sourceNodeId}_{sourceNodeSuffix++}";
            }

            recipeToExecute.Nodes.Insert(0, RecipeStepConfig.CreateSource(sourceNodeId, CaptureSourceType.CurrentEditor));
            recipeToExecute.Flow.StartNodes = new List<string> { sourceNodeId };
            recipeToExecute.Flow.AddTransitions(sourceNodeId, startNodeIds);

            Log.Info($"Added required start source node '{sourceNodeId}' with CaptureSourceType 'CurrentEditor' to recipe '{recipe.Name}', because it did not have a source step.");

            return recipeToExecute;
        }

        /// <summary>
        /// Ensures that the given recipe has an editor destination after every end node.
        /// If the recipe has no destination step, a new editor destination with TargetEditor set to CurrentEditor
        /// is added to every node without outgoing transitions.
        /// </summary>
        /// <param name="recipe">The recipe to inspect.</param>
        /// <returns>The modified recipe, or the original recipe if no changes were needed.</returns>
        public static CaptureRecipe EnsureRequiredEditorDestination(CaptureRecipe recipe)
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
                string editorNodeId = $"editor_destination_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                int editorNodeSuffix = 1;
                while (recipeToExecute.FindNode(editorNodeId) != null)
                {
                    editorNodeId = $"{editorNodeId}_{editorNodeSuffix++}";
                }

                var editorNode = RecipeStepConfig.CreateEditor(editorNodeId);
                editorNode.Set("TargetEditor", TargetEditor.CurrentEditor.ToString());
                recipeToExecute.Nodes.Add(editorNode);
                recipeToExecute.Flow.AddTransition(endNodeId, editorNodeId);
            }

            Log.Info($"Added required editor destination nodes with TargetEditor.CurrentEditor to recipe '{recipe.Name}', because it did not have a destination step.");
            return recipeToExecute;
        }
    }
}