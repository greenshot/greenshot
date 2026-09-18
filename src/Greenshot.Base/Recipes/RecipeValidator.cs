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
using Greenshot.Base.Core;
using Greenshot.Base.Drawing;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;

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
        public List<RecipeGatedAction> GatedActions { get; } = new List<RecipeGatedAction>();
        public bool HasGatedActions => GatedActions.Count > 0;
        public bool HasExternalCommands => HasGatedActions;
        public List<string> ExternalCommands => GatedActions.Select(g => g.Target).ToList();

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);

        public void AddGatedAction(RecipeGatedAction action)
        {
            if (action != null && !GatedActions.Contains(action))
            {
                GatedActions.Add(action);
            }
        }

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
            WellKnownStepTypes.Annotation,
            WellKnownStepTypes.SetVariable,
            WellKnownStepTypes.SaveFile,
            WellKnownStepTypes.Clipboard,
            WellKnownStepTypes.Editor,
            WellKnownStepTypes.Printer,
            WellKnownStepTypes.Email,
            WellKnownStepTypes.CustomDestination,
            "SaveToFile",
            "ObfuscateText",
            "ExternalCommand"
        };

        private static readonly HashSet<string> KnownTriggerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Greenshot.Base.Triggers.TriggerConfig.TypeHotkey,
            Greenshot.Base.Triggers.TriggerConfig.TypeContextMenu,
            Greenshot.Base.Triggers.TriggerConfig.TypeSystray,
            Greenshot.Base.Triggers.TriggerConfig.TypeClipboard,
            Greenshot.Base.Triggers.TriggerConfig.TypeEditor,
            Greenshot.Base.Triggers.TriggerConfig.TypeManual,
            Greenshot.Base.Triggers.TriggerConfig.TypeSchedule
        };

        /// <summary>
        /// Optional delegate to check whether an extension/plugin requirement is satisfied.
        /// Returns (isAvailable, installedVersion). If null, falls back to inspecting loaded plugins from SimpleServiceProvider.Current.
        /// </summary>
        public static Func<RecipeRequirement, (bool isAvailable, string installedVersion)> ExtensionAvailabilityCheck { get; set; }

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

            // Validate Requires (extension dependencies)
            if (recipe.Requires != null)
            {
                foreach (var req in recipe.Requires)
                {
                    ValidateRequirement(req, result);
                }
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
                else
                {
                    var seq = HotkeySequence.Parse(hotkey);
                    if (!seq.Validate(out string error))
                    {
                        result.AddError($"Hotkey trigger '{trigger.Name}' at index {index} has invalid hotkey '{hotkey}': {error}");
                    }
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

            if (!KnownStepTypes.Contains(node.StepType) && !StepRegistry.Instance.IsRegistered(node.StepType))
            {
                result.AddError($"Node '{node.Id}' uses stepType '{node.StepType}' which is not available because the required extension/plugin is not installed or active.");
            }

            // Node-specific parameter validations (independent checks)
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
            if (string.Equals(node.StepType, WellKnownStepTypes.Source, StringComparison.OrdinalIgnoreCase))
            {
                if (node.Parameters != null && node.Parameters.TryGetValue("SourceType", out var st) && st is string stStr)
                {
                    if (!Enum.TryParse<CaptureSourceType>(stStr, true, out _))
                    {
                        result.AddError($"Node '{node.Id}' [Source]: Unknown SourceType '{stStr}'.");
                    }
                }
            }
            if (string.Equals(node.StepType, WellKnownStepTypes.InteractiveSelection, StringComparison.OrdinalIgnoreCase))
            {
                if (node.Parameters != null && node.Parameters.TryGetValue("SelectionMode", out var sm) && sm is string smStr)
                {
                    if (!Enum.TryParse<CaptureMode>(smStr, true, out _))
                    {
                        result.AddError($"Node '{node.Id}' [InteractiveSelection]: Unknown SelectionMode '{smStr}'.");
                    }
                }
            }
            if (string.Equals(node.StepType, WellKnownStepTypes.Conditional, StringComparison.OrdinalIgnoreCase))
            {
                var branches = node.GetFirstParameter<object>("Branches");
                if (branches == null)
                {
                    result.AddError($"Node '{node.Id}' [Conditional]: Missing required 'branches' configuration list.");
                }
            }
            if (string.Equals(node.StepType, WellKnownStepTypes.Annotation, StringComparison.OrdinalIgnoreCase))
            {
                ValidateAnnotationNode(node, result);
            }

            // Programmatic step inspection for recipe authorization gates
            CheckAndDetectGatedActions(node, result);
        }

        private static readonly HashSet<string> BuiltInAnnotationTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Rectangle", "Ellipse", "Line", "Arrow", "Freehand", "Text", "Speechbubble", "StepLabel",
            "Image", "Icon", "Cursor", "Emoji", "Svg", "Blur", "Pixelize", "Highlight", "Magnify", "Crop"
        };

        private static void ValidateAnnotationNode(RecipeNodeConfig node, RecipeValidationResult result)
        {
            if (node.Parameters == null) return;

            // Single annotation in Type parameter
            if (node.Parameters.TryGetValue("Type", out var typeObj) && typeObj is string singleType && !string.IsNullOrWhiteSpace(singleType))
            {
                ValidateAnnotationType(singleType, node.Id, result);
            }

            // Multiple annotations in Annotations list
            if (node.Parameters.TryGetValue("Annotations", out var annotationsObj) && annotationsObj is System.Collections.IEnumerable list && !(annotationsObj is string))
            {
                foreach (var item in list)
                {
                    string aType = null;
                    if (item is Dictionary<string, object> dict && dict.TryGetValue("Type", out var tObj))
                    {
                        aType = tObj?.ToString();
                    }
                    else if (item is Newtonsoft.Json.Linq.JObject jobj && jobj.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out var jt))
                    {
                        aType = jt?.ToString();
                    }
                    if (!string.IsNullOrWhiteSpace(aType))
                    {
                        ValidateAnnotationType(aType, node.Id, result);
                    }
                }
            }
        }

        private static void ValidateAnnotationType(string annotationType, string nodeId, RecipeValidationResult result)
        {
            if (BuiltInAnnotationTypes.Contains(annotationType)) return;
            if (RecipeDrawableRegistry.Instance.IsRegistered(annotationType)) return;

            result.AddError($"Node '{nodeId}' uses custom annotation type '{annotationType}', which is not available because the required extension is not installed or active.");
        }

        private static void ValidateRequirement(RecipeRequirement req, RecipeValidationResult result)
        {
            if (req == null) return;
            if (string.IsNullOrWhiteSpace(req.Id))
            {
                result.AddError("Recipe requirement is missing required 'id'.");
                return;
            }

            if (ExtensionAvailabilityCheck != null)
            {
                var (isAvailable, installedVersion) = ExtensionAvailabilityCheck(req);
                if (!isAvailable)
                {
                    string extName = !string.IsNullOrWhiteSpace(req.Name) ? req.Name : req.Id;
                    string verSuffix = !string.IsNullOrWhiteSpace(req.MinVersion) ? $" (v{req.MinVersion}+)" : string.Empty;
                    string urlSuffix = !string.IsNullOrWhiteSpace(req.Url) ? $" Download/install from: {req.Url}" : string.Empty;
                    result.AddError($"This recipe requires the extension '{extName}'{verSuffix} (ID: {req.Id}), which is not installed or is disabled.{urlSuffix}");
                }
                else if (!string.IsNullOrWhiteSpace(req.MinVersion) && !string.IsNullOrWhiteSpace(installedVersion))
                {
                    if (Version.TryParse(req.MinVersion, out var minV) && Version.TryParse(installedVersion, out var curV))
                    {
                        if (curV < minV)
                        {
                            result.AddError($"This recipe requires extension '{req.Name ?? req.Id}' version {req.MinVersion} or newer, but version {installedVersion} is installed.");
                        }
                    }
                }
                return;
            }

            try
            {
                var plugins = SimpleServiceProvider.Current?.GetAllInstances<IGreenshotPlugin>()?.ToList();
                if (plugins != null && plugins.Count > 0)
                {
                    var match = plugins.FirstOrDefault(p =>
                        string.Equals(p.GetType().Assembly.GetName().Name, req.Id, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.GetType().FullName, req.Id, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.Name, req.Id, StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                    {
                        string extName = !string.IsNullOrWhiteSpace(req.Name) ? req.Name : req.Id;
                        string verSuffix = !string.IsNullOrWhiteSpace(req.MinVersion) ? $" (v{req.MinVersion}+)" : string.Empty;
                        string urlSuffix = !string.IsNullOrWhiteSpace(req.Url) ? $" Download/install from: {req.Url}" : string.Empty;
                        result.AddError($"This recipe requires the extension '{extName}'{verSuffix} (ID: {req.Id}), which is not installed or is disabled.{urlSuffix}");
                    }
                    else if (!string.IsNullOrWhiteSpace(req.MinVersion) && Version.TryParse(req.MinVersion, out var minV))
                    {
                        var curV = match.GetType().Assembly.GetName().Version;
                        if (curV != null && curV < minV)
                        {
                            result.AddError($"This recipe requires extension '{match.Name}' version {req.MinVersion} or newer, but version {curV} is installed.");
                        }
                    }
                }
            }
            catch
            {
                // DI not initialized; skip runtime check
            }
        }

        private static void CheckAndDetectGatedActions(RecipeNodeConfig node, RecipeValidationResult result)
        {
            try
            {
                var step = StepRegistry.Instance.CreateStep(node);
                if (step is IRequiresRecipeAuthorization authStep)
                {
                    var actions = authStep.GetGatedActions();
                    if (actions != null)
                    {
                        foreach (var action in actions)
                        {
                            result.AddGatedAction(action);
                        }
                    }
                }
            }
            catch
            {
                // Unresolvable step types are flagged by the schema check
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
                    if (flow.StartNodes == null) flow.StartNodes = new List<string>();
                    if (!flow.StartNodes.Contains(recipe.Nodes[0].Id, StringComparer.OrdinalIgnoreCase))
                    {
                        flow.StartNodes.Add(recipe.Nodes[0].Id);
                    }
                }
                else
                {
                    result.AddError("Flow definition must specify at least one entry node in 'startNodes'.");
                }
            }

            foreach (var startId in startNodes)
            {
                if (!validNodeIds.Contains(startId))
                {
                    result.AddError($"Flow start node '{startId}' does not match any defined node id.");
                }
            }

            // Validate ConditionalTransitions
            if (flow.ConditionalTransitions != null)
            {
                foreach (var ct in flow.ConditionalTransitions)
                {
                    if (string.IsNullOrWhiteSpace(ct?.From))
                    {
                        result.AddError("Conditional transition is missing source 'from' node ID.");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(ct.Branch))
                    {
                        result.AddError($"Conditional transition from '{ct.From}' is missing 'branch' key.");
                    }
                    if (string.IsNullOrWhiteSpace(ct.To))
                    {
                        result.AddError($"Conditional transition from '{ct.From}' is missing target 'to' node ID.");
                        continue;
                    }

                    if (!validNodeIds.Contains(ct.From))
                    {
                        result.AddError($"Conditional transition source node '{ct.From}' does not exist in 'nodes'.");
                    }
                    else
                    {
                        var fromNode = recipe.FindNode(ct.From);
                        if (fromNode != null && !string.Equals(fromNode.StepType, WellKnownStepTypes.Conditional, StringComparison.OrdinalIgnoreCase))
                        {
                            result.AddWarning($"Conditional transition source node '{ct.From}' has stepType '{fromNode.StepType}' rather than 'Conditional'.");
                        }
                    }

                    if (!validNodeIds.Contains(ct.To))
                    {
                        result.AddError($"Conditional transition target node '{ct.To}' (from '{ct.From}', branch '{ct.Branch}') does not exist in 'nodes'.");
                    }
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
