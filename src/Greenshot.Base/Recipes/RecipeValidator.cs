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
using System.Text.RegularExpressions;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            WellKnownStepTypes.DynamicDestination,
            WellKnownStepTypes.RecordVideo,
            WellKnownStepTypes.UserPrompt,
            WellKnownStepTypes.Stdout,
            WellKnownStepTypes.Stderr,
            WellKnownStepTypes.Slot
        };

        private static readonly HashSet<string> KnownTriggerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeHotkey,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeContextMenu,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeSystray,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeClipboard,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeEditor,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeManual,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeSchedule,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeCommandline,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeOpenFile,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeExtension,
            Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeAiTool
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

            ValidateHeader(recipe, "Recipe", result);

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

            // Validate the options and where the nodes use them
            ValidateOptions(recipe, result);

            // Validate Flow Definition & DAG acyclicity
            ValidateFlowAndDetectCycles(recipe, nodeIds, result);

            // Check the recipe against the step contracts along the graph: required parameters, allowed values,
            // variables used before they are set on every path, nodes that never run, ...
            if (result.IsValid)
            {
                AddContractWarnings(recipe, result);
            }

            return result;
        }

        /// <summary>
        /// Version, id, name and requirements, the same for recipes and extensions
        /// </summary>
        private static void ValidateHeader(FlowDefinition definition, string what, RecipeValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(definition.Version))
            {
                result.AddWarning($"{what} 'version' is missing; defaulting to '1.0'.");
            }
            else
            {
                string major = definition.Version.Split('.')[0].Trim();
                if (major != "1")
                {
                    result.AddError($"Unsupported {what.ToLowerInvariant()} version '{definition.Version}'. This version of Greenshot supports version 1.x recipes.");
                }
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                result.AddError($"{what} 'id' is required and cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                result.AddError($"{what} 'name' is required and cannot be empty.");
            }

            // Validate Requires (extension dependencies)
            if (definition.Requires != null)
            {
                foreach (var req in definition.Requires)
                {
                    ValidateRequirement(req, result);
                }
            }
        }

        /// <summary>
        /// Validates a recipe extension: the common checks of a flow, plus where it goes and what it may contain.
        /// An extension is a small flow between In (its startNodes) and Out (a transition target): no triggers, no Source step,
        /// destinations only at AfterExport, no slots of its own, and every branch of a decision must lead somewhere.
        /// </summary>
        public static RecipeValidationResult Validate(RecipeExtension extension)
        {
            var result = new RecipeValidationResult();
            if (extension == null)
            {
                result.AddError("Extension cannot be null.");
                return result;
            }

            ValidateHeader(extension, "Extension", result);
            if (!string.IsNullOrWhiteSpace(extension.Id) && !RecipeOption.KeyPattern.IsMatch(extension.Id))
            {
                result.AddError($"Extension id '{extension.Id}' is invalid: use letters, digits and underscores, starting with a letter.");
            }

            string slotName = extension.SlotName;
            if (extension.Extends == null)
            {
                result.AddError("Extension 'extends' is required: which recipes, at which slot.");
            }
            else
            {
                if (slotName == null)
                {
                    result.AddError($"Extension slot '{extension.Extends.Slot}' is unknown, use {string.Join(", ", RecipeSlots.All)}.");
                }
                if (extension.Extends.Recipes == null || extension.Extends.Recipes.Count == 0 || extension.Extends.Recipes.Any(string.IsNullOrWhiteSpace))
                {
                    result.AddError($"Extension 'extends.recipes' needs recipe ids, \"{RecipeExtension.TargetAll}\" or \"{RecipeExtension.TargetCaptures}\".");
                }
                else if (extension.Extends.Recipes.Any(r => string.Equals(r.Trim(), extension.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    result.AddError("An extension can't extend itself.");
                }
            }

            if (!string.IsNullOrWhiteSpace(extension.When) && extension.When.IndexOf("${", StringComparison.Ordinal) < 0)
            {
                result.AddError("Extension 'when' must be an expression like \"${payload.width > 800}\".");
            }

            if (extension.Nodes == null || extension.Nodes.Count == 0)
            {
                result.AddError("Extension must contain at least one node in 'nodes'.");
                return result;
            }

            var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < extension.Nodes.Count; i++)
            {
                var node = extension.Nodes[i];
                ValidateNode(node, i, nodeIds, result);
                if (node == null) continue;

                if (RecipeExtension.ReservedNodeIds.Contains(node.Id, StringComparer.OrdinalIgnoreCase))
                {
                    result.AddError($"Node id '{node.Id}' is reserved in extensions (In and Out are where the extension starts and ends).");
                }
                if (string.Equals(node.StepType, WellKnownStepTypes.Source, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(node.StepType, WellKnownStepTypes.RecordVideo, StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError($"Node '{node.Id}': an extension can't capture, it works on the capture of the recipe it extends.");
                }
                if (string.Equals(node.StepType, WellKnownStepTypes.Slot, StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError($"Node '{node.Id}': an extension can't have slots, an extension can't extend another extension.");
                }
                if (WellKnownStepTypes.IsDestination(node.StepType) && slotName != null && slotName != RecipeSlots.AfterExport)
                {
                    result.AddError($"Node '{node.Id}' exports the capture ({node.StepType}); an extension can only use destinations at the {RecipeSlots.AfterExport} slot.");
                }
            }

            ValidateOptions(extension, result);
            foreach (var key in FindOptionReferences(extension.When).Where(k => extension.FindOption(k) == null))
            {
                result.AddError($"Extension 'when' uses the option '{key}', which the extension doesn't declare in 'options'.");
            }

            ValidateFlowAndDetectCycles(extension, nodeIds, result, allowOutTarget: true);
            ValidateExtensionFlow(extension, result);
            return result;
        }

        /// <summary>
        /// Every branch of a decision in an extension leads to a node or to Out, there is an "else" branch, and error
        /// transitions only start at the extension's own nodes: otherwise the recipe it extends would end in the extension.
        /// </summary>
        private static void ValidateExtensionFlow(RecipeExtension extension, RecipeValidationResult result)
        {
            var flow = extension.Flow ?? new RecipeFlowConfig();
            if (flow.GetEffectiveStartNodes().Any(s => string.Equals(s, RecipeExtension.OutNode, StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError("An extension can't start at Out.");
            }

            foreach (var node in extension.Nodes.Where(n => n != null && string.Equals(n.StepType, WellKnownStepTypes.Conditional, StringComparison.OrdinalIgnoreCase)))
            {
                var branches = ReadBranches(node);
                if (!branches.Any(b => string.Equals(b.Expression?.Trim(), "else", StringComparison.OrdinalIgnoreCase)))
                {
                    result.AddError($"Node '{node.Id}' [Conditional]: in an extension a decision needs an \"else\" branch, so the recipe always goes on.");
                }
                foreach (var branch in branches.Where(b => !string.IsNullOrEmpty(b.Key)))
                {
                    bool routed = flow.ConditionalTransitions?.Any(ct => string.Equals(ct?.From, node.Id, StringComparison.OrdinalIgnoreCase) &&
                                                                         string.Equals(ct.Branch, branch.Key, StringComparison.OrdinalIgnoreCase) &&
                                                                         !string.IsNullOrWhiteSpace(ct.To)) ?? false;
                    if (!routed)
                    {
                        result.AddError($"Node '{node.Id}' [Conditional]: branch '{branch.Key}' leads nowhere; route it to a node or to \"{RecipeExtension.OutNode}\".");
                    }
                }
            }

            foreach (var et in flow.ErrorTransitions ?? new List<RecipeErrorTransitionConfig>())
            {
                if (et == null) continue;
                if (extension.FindNode(et.From) == null)
                {
                    result.AddError($"Error transition from '{et.From}': in an extension error transitions must start at one of its nodes.");
                }
                if (!string.IsNullOrWhiteSpace(et.To) && extension.FindNode(et.To) == null)
                {
                    result.AddError($"Error transition to '{et.To}' doesn't lead to a node of the extension.");
                }
            }
        }

        private static List<(string Key, string Expression)> ReadBranches(RecipeNodeConfig node)
        {
            var list = new List<(string Key, string Expression)>();
            var branches = node.GetParameter<object>("Branches");
            if (branches == null) return list;
            var token = branches as JToken ?? JToken.FromObject(branches);
            foreach (var item in token.OfType<JObject>())
            {
                string key = item.GetValue("key", StringComparison.OrdinalIgnoreCase)?.ToString();
                string expression = item.GetValue("expression", StringComparison.OrdinalIgnoreCase)?.ToString();
                list.Add((key, expression));
            }
            return list;
        }

        private static void AddContractWarnings(CaptureRecipe recipe, RecipeValidationResult result)
        {
            try
            {
                var contract = RecipeContract.Analyze(recipe);
                foreach (var warning in contract?.ValidationWarnings ?? Array.Empty<string>())
                {
                    // Unknown step types are already reported as errors above (or are built-ins not registered yet)
                    if (warning.IndexOf("has no registered contract", StringComparison.Ordinal) >= 0) continue;
                    result.AddWarning(warning);
                }
            }
            catch (Exception ex)
            {
                result.AddWarning($"The recipe could not be checked against the step contracts: {ex.Message}");
            }
        }

        private static void ValidateTrigger(Greenshot.Base.Recipes.Triggers.TriggerConfig trigger, int index, RecipeValidationResult result)
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

            if (string.Equals(trigger.TriggerType, Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase))
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

            if (string.Equals(trigger.TriggerType, Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeAiTool, StringComparison.OrdinalIgnoreCase))
            {
                string toolName = trigger.GetParameter<string>("ToolName");
                if (!Greenshot.Base.Recipes.Triggers.AiToolTrigger.IsValidToolName(toolName))
                {
                    result.AddError($"AI tool trigger '{trigger.Name}' at index {index} needs a 'ToolName' of 1 to 64 letters, digits, '_' or '-'{(string.IsNullOrEmpty(toolName) ? string.Empty : $", not '{toolName}'")}.");
                }
                if (string.IsNullOrWhiteSpace(trigger.GetParameter<string>("Description")))
                {
                    result.AddWarning($"AI tool trigger '{trigger.Name}' at index {index} has no 'Description', the AI won't know what the tool does.");
                }
            }

            if (string.Equals(trigger.TriggerType, Greenshot.Base.Recipes.Triggers.TriggerConfig.TypeOpenFile, StringComparison.OrdinalIgnoreCase))
            {
                // Filter: extensions separated by ';', e.g. ".png;.jpg"
                string filter = trigger.GetParameter<string>("Filter");
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    foreach (string extension in filter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()))
                    {
                        if (extension.Length < 2 || extension[0] != '.' || extension.IndexOfAny(new[] { '.', '*', '?', ',', '|', ' ' }, 1) >= 0)
                        {
                            result.AddError($"OpenFile trigger '{trigger.Name}' at index {index} has the invalid extension '{extension}' in its Filter; use extensions separated by ';', e.g. \".png;.jpg\".");
                        }
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
            if (string.Equals(node.StepType, WellKnownStepTypes.Effect, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(node.GetParameter("Effect", "Border"), "Border", StringComparison.OrdinalIgnoreCase))
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
                var branches = node.GetParameter<object>("Branches");
                if (branches == null)
                {
                    result.AddError($"Node '{node.Id}' [Conditional]: Missing required 'branches' configuration list.");
                }
            }
            if (string.Equals(node.StepType, WellKnownStepTypes.Annotation, StringComparison.OrdinalIgnoreCase))
            {
                ValidateAnnotationNode(node, result);
            }
            if (string.Equals(node.StepType, WellKnownStepTypes.Slot, StringComparison.OrdinalIgnoreCase))
            {
                if (RecipeSlots.GetSlotName(node) == null)
                {
                    result.AddError($"Node '{node.Id}' [Slot]: 'name' must be one of {string.Join(", ", RecipeSlots.All)}.");
                }
                if (node.Parameters.TryGetValue("Accept", out var accept) && accept != null)
                {
                    var acceptValue = accept is JValue jAccept ? jAccept.Value : accept;
                    bool valid = acceptValue is string || (acceptValue is System.Collections.IEnumerable && !(acceptValue is string));
                    if (!valid)
                    {
                        result.AddError($"Node '{node.Id}' [Slot]: 'accept' must be \"{RecipeSlots.AcceptAll}\", \"{RecipeSlots.AcceptNone}\" or a list of extension ids.");
                    }
                }
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

        private static readonly Regex ExpressionPattern = new Regex(@"\$\{([^}]*)\}", RegexOptions.Compiled);
        private static readonly Regex OptionReferencePattern = new Regex(@"(?<![A-Za-z0-9_.])option\.([A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// The keys of the options a text uses in its ${...} expressions ("key", or "extension.key" in a composed recipe)
        /// </summary>
        public static IReadOnlyCollection<string> FindOptionReferences(string text)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text)) return keys;
            foreach (Match expression in ExpressionPattern.Matches(text))
            {
                foreach (Match reference in OptionReferencePattern.Matches(expression.Groups[1].Value))
                {
                    keys.Add(reference.Groups[1].Value);
                }
            }
            return keys;
        }

        private static void ValidateOptions(FlowDefinition recipe, RecipeValidationResult result)
        {
            var options = new Dictionary<string, RecipeOption>(StringComparer.OrdinalIgnoreCase);
            foreach (var option in recipe.Options ?? new List<RecipeOption>())
            {
                if (option == null)
                {
                    result.AddError("An option cannot be null.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(option.Key) || !RecipeOption.KeyPattern.IsMatch(option.Key))
                {
                    result.AddError($"Option key '{option.Key}' is invalid: use letters, digits and underscores, starting with a letter.");
                    continue;
                }
                if (options.ContainsKey(option.Key))
                {
                    result.AddError($"Duplicate option key '{option.Key}'.");
                    continue;
                }
                if (recipe is RecipeExtension && RecipeExtension.ScopeOptions.Any(s => string.Equals(s.Key, option.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    result.AddError($"Option key '{option.Key}' is reserved for the settings every extension has (which recipes, which destinations).");
                    continue;
                }
                options[option.Key] = option;

                if (!RecipeOption.SupportedTypes.Contains(option.Type))
                {
                    result.AddError($"Option '{option.Key}' has type '{option.Type}', options can be {string.Join(", ", RecipeOption.SupportedTypes)}.");
                    continue;
                }
                if (option.Type == ContractDataType.Enum)
                {
                    var values = option.Choices?.Where(c => c != null).Select(c => c.Value).ToList() ?? new List<string>();
                    if (values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
                    {
                        result.AddError($"Option '{option.Key}' is an Enum and needs 'choices', each with a 'value'.");
                    }
                    else if (values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Count)
                    {
                        result.AddError($"Option '{option.Key}' has duplicate choices.");
                    }
                }
                else if (option.Choices != null && option.Choices.Count > 0)
                {
                    result.AddWarning($"Option '{option.Key}': 'choices' are only used for Enum options.");
                }
                bool isNumber = option.Type == ContractDataType.Integer || option.Type == ContractDataType.Decimal;
                if (!isNumber && (option.Min.HasValue || option.Max.HasValue))
                {
                    result.AddWarning($"Option '{option.Key}': 'min' and 'max' are only used for Integer and Decimal options.");
                }
                if (option.Min.HasValue && option.Max.HasValue && option.Min.Value > option.Max.Value)
                {
                    result.AddError($"Option '{option.Key}': 'min' ({option.Min}) is larger than 'max' ({option.Max}).");
                }
                if (option.DefaultValue != null && !option.TryConvert(option.DefaultValue, out _))
                {
                    result.AddError($"Option '{option.Key}': the default '{option.DefaultValue}' is not a valid {option.Type}.");
                }
                if (option.QuickSettings && !RecipeOption.QuickSettingsTypes.Contains(option.Type))
                {
                    result.AddError($"Option '{option.Key}' can't be shown in the quick settings, only Boolean and Enum options can.");
                }
            }

            foreach (var option in options.Values.Where(o => !string.IsNullOrWhiteSpace(o.EnabledWhen)))
            {
                if (string.Equals(option.EnabledWhen, option.Key, StringComparison.OrdinalIgnoreCase) ||
                    !options.TryGetValue(option.EnabledWhen, out var switchOption) || switchOption.Type != ContractDataType.Boolean)
                {
                    result.AddError($"Option '{option.Key}': 'enabledWhen' must be the key of another Boolean option of the recipe.");
                }
            }

            foreach (var node in recipe.Nodes.Where(n => n != null))
            {
                if (!string.IsNullOrWhiteSpace(node.EnabledExpression) && node.EnabledExpression.IndexOf("${", StringComparison.Ordinal) < 0)
                {
                    result.AddError($"Node '{node.Id}': 'enabled' must be true, false or an expression like \"${{option.key}}\".");
                }

                string parameters = node.Parameters == null || node.Parameters.Count == 0 ? null : JsonConvert.SerializeObject(node.Parameters);
                var parameterReferences = FindOptionReferences(parameters);
                foreach (var key in parameterReferences.Concat(FindOptionReferences(node.EnabledExpression)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    // Options of the recipe, or (composed recipe) of the extension the node came from
                    if (!options.ContainsKey(key) && !RecipeOptionStore.TryResolve(recipe, key, out _, out _))
                    {
                        result.AddError($"Node '{node.Id}' uses the option '{key}', which the recipe doesn't declare in 'options'.");
                    }
                }

                // A text the user types must not end up in a command line, an upload address or a file path:
                // a step which needs an approval may only use options whose values the approved recipe limits.
                var textOptions = parameterReferences
                    .Where(key => RecipeOptionStore.TryResolve(recipe, key, out _, out var option) && option.Type == ContractDataType.String)
                    .ToList();
                if (textOptions.Count > 0 && IsGatedNode(node))
                {
                    result.AddError($"Node '{node.Id}' needs an approval and can't use the text option(s) {string.Join(", ", textOptions.Select(k => $"'{k}'"))}.");
                }
            }
        }

        private static bool IsGatedNode(RecipeNodeConfig node)
        {
            try
            {
                return StepRegistry.Instance.CreateStep(node) is IRequiresRecipeAuthorization authStep &&
                       (authStep.GetGatedActions()?.Any() ?? false);
            }
            catch
            {
                // Unresolvable step types are flagged by the schema check
                return false;
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

        private static void ValidateFlowAndDetectCycles(FlowDefinition recipe, HashSet<string> validNodeIds, RecipeValidationResult result, bool allowOutTarget = false)
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

                    if (!validNodeIds.Contains(ct.To) && !(allowOutTarget && IsOutNode(ct.To)))
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
                    if (!validNodeIds.Contains(toNode) && !(allowOutTarget && IsOutNode(toNode)))
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

        private static bool IsOutNode(string nodeId) => string.Equals(nodeId, RecipeExtension.OutNode, StringComparison.OrdinalIgnoreCase);

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
