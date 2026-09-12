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
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Result of validating a recipe against the formal written contract and schema.
    /// </summary>
    public class RecipeValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public bool HasExternalCommands { get; set; }
        public List<string> ExternalCommands { get; } = new List<string>();

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);

        public override string ToString()
        {
            if (IsValid)
            {
                return Warnings.Count > 0
                    ? $"Valid with {Warnings.Count} warning(s): {string.Join("; ", Warnings)}"
                    : "Valid";
            }
            return $"Invalid ({Errors.Count} error(s)): {string.Join("; ", Errors)}";
        }
    }

    /// <summary>
    /// Validates CaptureRecipe DAG structures against the schema contract.
    /// Enforces unique node IDs, valid entry points, reachable targets, and strictly prevents loops/cycles.
    /// </summary>
    public static class RecipeValidator
    {
        private static readonly HashSet<string> KnownStepTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            WellKnownStepTypes.Source,
            WellKnownStepTypes.InteractiveSelection,
            WellKnownStepTypes.Border,
            WellKnownStepTypes.Effect,
            WellKnownStepTypes.ImmediateFeedback,
            WellKnownStepTypes.Processors,
            WellKnownStepTypes.Destinations,
            WellKnownStepTypes.Notification,
            WellKnownStepTypes.Conditional,
            WellKnownStepTypes.TextEffect,
            WellKnownStepTypes.Drawable,
            WellKnownStepTypes.SetVariable,
            "ObfuscateText",
            "ExternalCommand"
        };

        private static readonly HashSet<string> KnownTriggerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Greenshot.Base.Triggers.TriggerConfig.TypeHotkey,
            Greenshot.Base.Triggers.TriggerConfig.TypeContextMenu,
            Greenshot.Base.Triggers.TriggerConfig.TypeSystray,
            Greenshot.Base.Triggers.TriggerConfig.TypeClipboard,
            Greenshot.Base.Triggers.TriggerConfig.TypeManual,
            Greenshot.Base.Triggers.TriggerConfig.TypeSchedule
        };

        /// <summary>
        /// Validates a CaptureRecipe DAG against the formal schema contract.
        /// </summary>
        public static RecipeValidationResult Validate(CaptureRecipe recipe)
        {
            var result = new RecipeValidationResult();

            if (recipe == null)
            {
                result.AddError("Recipe cannot be null.");
                return result;
            }

            // Validate Version
            if (string.IsNullOrWhiteSpace(recipe.Version))
            {
                result.AddWarning("Recipe 'version' is missing; defaulting to '1.0'.");
            }
            else
            {
                string major = recipe.Version.Split('.')[0].Trim();
                if (major != "1")
                {
                    result.AddError($"Unsupported recipe version '{recipe.Version}'. This version of Greenshot supports version 1.x recipes.");
                }
            }

            if (string.IsNullOrWhiteSpace(recipe.Id))
            {
                result.AddError("Recipe 'id' is required and cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(recipe.Name))
            {
                result.AddError("Recipe 'name' is required and cannot be empty.");
            }

            // Validate Triggers (optional)
            if (recipe.Triggers != null)
            {
                for (int i = 0; i < recipe.Triggers.Count; i++)
                {
                    var trigger = recipe.Triggers[i];
                    ValidateTrigger(trigger, i, result);
                }
            }

            // Validate Nodes
            if (recipe.Nodes == null || recipe.Nodes.Count == 0)
            {
                result.AddError("Recipe must contain at least one node in 'nodes'.");
                return result;
            }

            var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < recipe.Nodes.Count; i++)
            {
                var node = recipe.Nodes[i];
                ValidateNode(node, i, nodeIds, result);
            }

            // Validate Flow Definition & DAG acyclicity
            ValidateFlowAndDetectCycles(recipe, nodeIds, result);

            return result;
        }

        private static void ValidateTrigger(Greenshot.Base.Triggers.TriggerConfig trigger, int index, RecipeValidationResult result)
        {
            if (trigger == null)
            {
                result.AddError($"Trigger at index {index} cannot be null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(trigger.TriggerType))
            {
                result.AddError($"Trigger at index {index} is missing required 'triggerType'.");
                return;
            }

            if (!KnownTriggerTypes.Contains(trigger.TriggerType))
            {
                result.AddWarning($"Trigger at index {index} has unrecognized triggerType '{trigger.TriggerType}'.");
            }

            if (string.Equals(trigger.TriggerType, Greenshot.Base.Triggers.TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase))
            {
                string hotkey = trigger.GetParameter<string>("Hotkey");
                if (string.IsNullOrWhiteSpace(hotkey))
                {
                    result.AddError($"Hotkey trigger '{trigger.Name}' at index {index} is missing required 'Hotkey' parameter.");
                }
            }
        }

        private static void ValidateNode(RecipeNodeConfig node, int index, HashSet<string> seenIds, RecipeValidationResult result)
        {
            if (node == null)
            {
                result.AddError($"Node at index {index} cannot be null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(node.Id))
            {
                result.AddError($"Node at index {index} is missing required 'id'.");
            }
            else
            {
                if (seenIds.Contains(node.Id))
                {
                    result.AddError($"Duplicate node id '{node.Id}' found in recipe.");
                }
                else
                {
                    seenIds.Add(node.Id);
                }
            }

            if (string.IsNullOrWhiteSpace(node.StepType))
            {
                result.AddError($"Node '{node.Id ?? index.ToString()}' is missing required 'stepType'.");
                return;
            }

            if (!KnownStepTypes.Contains(node.StepType))
            {
                result.AddWarning($"Node '{node.Id}' has unrecognized stepType '{node.StepType}'. Ensure a matching plugin step factory is registered.");
            }

            // Node-specific parameter validations
            if (string.Equals(node.StepType, WellKnownStepTypes.Border, StringComparison.OrdinalIgnoreCase))
            {
                if (node.Parameters != null && node.Parameters.TryGetValue("Width", out var w) && w != null)
                {
                    if (int.TryParse(w.ToString(), out int width) && width < 1)
                    {
                        result.AddError($"Node '{node.Id}' [Border]: 'Width' must be greater than or equal to 1 (got {width}).");
                    }
                }
            }
            else if (string.Equals(node.StepType, WellKnownStepTypes.Source, StringComparison.OrdinalIgnoreCase))
            {
                if (node.Parameters != null && node.Parameters.TryGetValue("SourceType", out var st) && st is string stStr)
                {
                    if (!Enum.TryParse<CaptureSourceType>(stStr, true, out _))
                    {
                        result.AddError($"Node '{node.Id}' [Source]: Unknown SourceType '{stStr}'.");
                    }
                }
            }
            else if (string.Equals(node.StepType, WellKnownStepTypes.InteractiveSelection, StringComparison.OrdinalIgnoreCase))
            {
                if (node.Parameters != null && node.Parameters.TryGetValue("SelectionMode", out var sm) && sm is string smStr)
                {
                    if (!Enum.TryParse<CaptureMode>(smStr, true, out _))
                    {
                        result.AddError($"Node '{node.Id}' [InteractiveSelection]: Unknown SelectionMode '{smStr}'.");
                    }
                }
            }
            else if (string.Equals(node.StepType, "ExternalCommand", StringComparison.OrdinalIgnoreCase) ||
                     (node.Parameters != null && (node.Parameters.ContainsKey("Command") || node.Parameters.ContainsKey("Executable"))))
            {
                result.HasExternalCommands = true;
                string cmd = node.GetParameter<string>("Command") ?? node.GetParameter<string>("Executable") ?? node.Name;
                result.ExternalCommands.Add(cmd);
            }
        }

        private static void ValidateFlowAndDetectCycles(CaptureRecipe recipe, HashSet<string> validNodeIds, RecipeValidationResult result)
        {
            var flow = recipe.Flow;
            if (flow == null)
            {
                result.AddError("Recipe 'flow' definition is required.");
                return;
            }

            var startNodes = flow.GetEffectiveStartNodes();
            if (startNodes.Count == 0)
            {
                // If only 1 node in recipe, default to it
                if (recipe.Nodes.Count == 1)
                {
                    startNodes.Add(recipe.Nodes[0].Id);
                    flow.StartNode = recipe.Nodes[0].Id;
                }
                else
                {
                    result.AddError("Flow definition must specify at least one entry node in 'startNode' or 'startNodes'.");
                }
            }

            foreach (var startId in startNodes)
            {
                if (!validNodeIds.Contains(startId))
                {
                    result.AddError($"Flow start node '{startId}' does not match any defined node id.");
                }
            }

            var transitions = flow.GetUnifiedTransitions();

            // Validate that all source and target transition IDs exist in Nodes
            foreach (var kvp in transitions)
            {
                string fromNode = kvp.Key;
                if (!validNodeIds.Contains(fromNode))
                {
                    result.AddError($"Flow transition source node '{fromNode}' does not exist in 'nodes'.");
                }

                foreach (var toNode in kvp.Value)
                {
                    if (!validNodeIds.Contains(toNode))
                    {
                        result.AddError($"Flow transition target node '{toNode}' (from '{fromNode}') does not exist in 'nodes'.");
                    }
                }
            }

            // Detect cycles/loops using Depth-First Search with 3-color marking
            // 0 = Unvisited (White), 1 = Visiting / in current stack (Gray), 2 = Visited / complete (Black)
            var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var parentPath = new List<string>();

            foreach (var nodeId in validNodeIds)
            {
                state[nodeId] = 0;
            }

            foreach (var startNode in startNodes)
            {
                if (validNodeIds.Contains(startNode) && state[startNode] == 0)
                {
                    if (DetectCycleDfs(startNode, transitions, state, parentPath, out var cyclePath))
                    {
                        result.AddError($"Cycle/loop detected in recipe flow: {string.Join(" -> ", cyclePath)}. Loops are strictly disallowed in DAG flows.");
                        return;
                    }
                }
            }

            // Check any disconnected components for cycles as well
            foreach (var nodeId in validNodeIds)
            {
                if (state[nodeId] == 0)
                {
                    if (DetectCycleDfs(nodeId, transitions, state, parentPath, out var cyclePath))
                    {
                        result.AddError($"Cycle/loop detected in recipe flow: {string.Join(" -> ", cyclePath)}. Loops are strictly disallowed in DAG flows.");
                        return;
                    }
                }
            }
        }

        private static bool DetectCycleDfs(
            string current,
            Dictionary<string, List<string>> transitions,
            Dictionary<string, int> state,
            List<string> path,
            out List<string> cyclePath)
        {
            state[current] = 1; // Visiting (Gray)
            path.Add(current);

            if (transitions.TryGetValue(current, out var nextNodes) && nextNodes != null)
            {
                foreach (var next in nextNodes)
                {
                    if (!state.TryGetValue(next, out int nextState)) continue;

                    if (nextState == 1) // Cycle detected (Back edge to ancestor in current DFS stack)
                    {
                        int cycleStart = path.IndexOf(next);
                        cyclePath = path.Skip(cycleStart >= 0 ? cycleStart : 0).ToList();
                        cyclePath.Add(next);
                        return true;
                    }

                    if (nextState == 0) // Unvisited
                    {
                        if (DetectCycleDfs(next, transitions, state, path, out cyclePath))
                        {
                            return true;
                        }
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            state[current] = 2; // Visited (Black)
            cyclePath = null;
            return false;
        }
    }
}
