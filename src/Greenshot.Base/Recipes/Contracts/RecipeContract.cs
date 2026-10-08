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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;

namespace Greenshot.Base.Recipes.Contracts
{
    public class StepSummaryContract
    {
        public string NodeId { get; set; }
        public string StepType { get; set; }
        public string DisplayName { get; set; }
        public IReadOnlyList<string> RequiredInputs { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> ProducedOutputs { get; set; } = Array.Empty<string>();
        public PayloadRequirement RequiresRawCapture { get; set; }
        public PayloadRequirement ProducesRawCapture { get; set; }

        /// <summary>False when no path from a start node reaches this node.</summary>
        public bool Reachable { get; set; } = true;

        /// <summary>Variables that are set on every path to this node (by preceding nodes or the trigger).</summary>
        public IReadOnlyList<string> GuaranteedVariables { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Composite contract of a CaptureRecipe, computed from its triggers and the contracts of its steps along the graph.
    /// </summary>
    /// <remarks>
    /// The analysis follows the flow as the engine executes it: a node runs after all its predecessors finished or were
    /// bypassed, and shares their context; a Conditional or UserPrompt node follows only the transitions of the chosen
    /// branch; a failing node with an error transition continues there instead of with its normal successors.
    /// Every combination of branch choices and error outcomes is a scenario; a variable is guaranteed for a node when,
    /// in every scenario that runs the node, a node that ran before it (an ancestor in the graph) set it, or the trigger
    /// provided it. Conditional outputs (e.g. Barcode.Text, only set when a barcode was found) are never guaranteed, but
    /// using one is not reported as long as its step runs before the node on every path (the value is then empty when
    /// nothing was found); a variable that is missing because of the path taken is reported.
    /// </remarks>
    public class RecipeContract
    {
        /// <summary>Variables the engine sets when a node fails and the flow continues on its error transition.</summary>
        public static readonly IReadOnlyList<string> ErrorVariables = new[] { "LastError", "LastErrorType", "FailedNodeId" };

        /// <summary>Pseudo variable for "an image is available" in <see cref="StepSummaryContract.GuaranteedVariables"/>.</summary>
        public const string PayloadVariable = "Payload";

        private const int MaxScenarios = 4096;
        private static readonly Regex VariableReference = new Regex(@"\$\{\s*([A-Za-z_][\w.]*)", RegexOptions.Compiled);

        public string RecipeId { get; set; }
        public string RecipeName { get; set; }
        public string Description { get; set; }

        public IReadOnlyList<VariableContract> Inputs { get; set; } = Array.Empty<VariableContract>();
        public IReadOnlyList<VariableContract> Outputs { get; set; } = Array.Empty<VariableContract>();
        public IReadOnlyList<StepSummaryContract> Steps { get; set; } = Array.Empty<StepSummaryContract>();
        public IReadOnlyList<string> ValidationWarnings { get; set; } = Array.Empty<string>();

        public bool AcquiresImage { get; set; }
        public bool MutatesPixels { get; set; }
        public bool ExtractsText { get; set; }

        /// <summary>
        /// Analyzes the recipe with the contracts of <paramref name="registry"/> (default: <see cref="StepRegistry.Instance"/>).
        /// </summary>
        public static RecipeContract Analyze(CaptureRecipe recipe, IStepRegistry registry = null)
        {
            if (recipe == null) return null;
            registry ??= StepRegistry.Instance;
            return new Analyzer(recipe, registry).Run();
        }

        /// <summary>
        /// What the triggers of the recipe hand to the flow: command line arguments, the file for Open with,
        /// the browser capture for the extension, the editor for editor triggers.
        /// </summary>
        public static IReadOnlyList<VariableContract> GetTriggerInputs(CaptureRecipe recipe, out bool providesPayload)
        {
            providesPayload = false;
            var inputs = new Dictionary<string, VariableContract>(StringComparer.OrdinalIgnoreCase);
            foreach (var trigger in recipe?.Triggers?.Where(t => t != null && t.Enabled) ?? Enumerable.Empty<TriggerConfig>())
            {
                bool isAiTool = string.Equals(trigger.TriggerType, TriggerConfig.TypeAiTool, StringComparison.OrdinalIgnoreCase);
                if (isAiTool || string.Equals(trigger.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var arg in trigger.GetParameter<List<CommandlineArgument>>("Arguments") ?? new List<CommandlineArgument>())
                    {
                        if (string.IsNullOrWhiteSpace(arg?.Name)) continue;
                        string name = arg.EffectiveVariable;
                        inputs[name] = new VariableContract(name, arg.Type, arg.Required, arg.Description ?? (isAiTool ? $"AI tool argument {arg.Name}" : $"Command-line argument --{arg.Name}"), arg.DefaultValue)
                        {
                            // Only an argument with a value in every call is guaranteed
                            Conditional = !arg.Required && string.IsNullOrEmpty(arg.DefaultValue)
                        };
                    }
                }
                else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeOpenFile, StringComparison.OrdinalIgnoreCase))
                {
                    inputs["Filename"] = new VariableContract("Filename", ContractDataType.FilePath, true, "The file that was opened");
                }
                else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeExtension, StringComparison.OrdinalIgnoreCase))
                {
                    providesPayload = true;
                    inputs["Capture"] = new VariableContract("Capture", ContractDataType.Object, true, "The capture imported from the browser (also the payload)");
                    inputs["Browser"] = new VariableContract("Browser", ContractDataType.String, false, "The browser that sent the capture") { Conditional = true };
                }
                else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeEditor, StringComparison.OrdinalIgnoreCase))
                {
                    inputs["EditorForm"] = new VariableContract("EditorForm", ContractDataType.Object, true, "The editor that triggered the recipe");
                }
            }
            return inputs.Values.ToList();
        }

        private sealed class Analyzer
        {
            private readonly CaptureRecipe _recipe;
            private readonly IStepRegistry _registry;
            private readonly List<string> _warnings = new List<string>();
            private readonly Dictionary<string, RecipeNodeConfig> _nodes = new Dictionary<string, RecipeNodeConfig>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, StepContract> _contracts = new Dictionary<string, StepContract>(StringComparer.OrdinalIgnoreCase);

            // Edges: from -> (to, kind, branch)
            private readonly Dictionary<string, List<Edge>> _outgoing = new Dictionary<string, List<Edge>>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<Edge>> _incoming = new Dictionary<string, List<Edge>>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<string>> _branchKeys = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _hasErrorEdge = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, HashSet<string>> _ancestors = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            private enum EdgeKind { Standard, Conditional, Error }

            private sealed class Edge
            {
                public string From;
                public string To;
                public EdgeKind Kind;
                public string Branch;
            }

            public Analyzer(CaptureRecipe recipe, IStepRegistry registry)
            {
                _recipe = recipe;
                _registry = registry;
            }

            public RecipeContract Run()
            {
                var result = new RecipeContract
                {
                    RecipeId = _recipe.Id,
                    RecipeName = _recipe.Name,
                    Description = _recipe.Description ?? string.Empty
                };

                var triggerInputs = GetTriggerInputs(_recipe, out bool triggerProvidesPayload);
                // A recipe without a source node gets a trigger-specific one when a trigger runs it (TriggerRecipePreparer)
                triggerProvidesPayload |= !_recipe.HasSourceStep() && (_recipe.Triggers?.Any(t => t != null && t.Enabled) ?? false);
                var inputsMap = triggerInputs.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);
                var outputsMap = new Dictionary<string, VariableContract>(StringComparer.OrdinalIgnoreCase);

                foreach (var node in _recipe.Nodes ?? new List<RecipeNodeConfig>())
                {
                    if (node == null || string.IsNullOrWhiteSpace(node.Id) || _nodes.ContainsKey(node.Id)) continue;
                    _nodes[node.Id] = node;
                    var contract = string.IsNullOrWhiteSpace(node.StepType) ? null : _registry.GetContract(node.StepType);
                    if (contract != null)
                    {
                        _contracts[node.Id] = contract;
                    }
                    else
                    {
                        _warnings.Add($"Node '{node.Id}' uses step type '{node.StepType}', which has no registered contract.");
                    }
                }

                var summaries = new Dictionary<string, StepSummaryContract>(StringComparer.OrdinalIgnoreCase);
                foreach (var node in _nodes.Values)
                {
                    _contracts.TryGetValue(node.Id, out var contract);
                    var summary = new StepSummaryContract
                    {
                        NodeId = node.Id,
                        StepType = node.StepType,
                        DisplayName = contract?.DisplayName ?? node.StepType
                    };
                    if (contract != null)
                    {
                        var payload = contract.PayloadContract;
                        summary.RequiresRawCapture = payload.RawCapture;
                        summary.ProducesRawCapture = payload.RawCapture == PayloadRequirement.Created ? PayloadRequirement.Created : PayloadRequirement.None;
                        result.AcquiresImage |= payload.RawCapture == PayloadRequirement.Created;
                        result.MutatesPixels |= payload.VisualMutation == PayloadEffect.MutatesPixels;
                        result.ExtractsText |= payload.ExtractedText == PayloadRequirement.Created;
                        summary.RequiredInputs = contract.InputVariables.Where(v => v.Required).SelectMany(v => v.ResolveNames(node)).ToList();
                        var produced = new List<string>();
                        foreach (var output in contract.OutputVariables)
                        {
                            foreach (var name in output.ResolveNames(node))
                            {
                                produced.Add(name);
                                if (!outputsMap.TryGetValue(name, out var existing) || existing.Conditional)
                                {
                                    outputsMap[name] = new VariableContract(name, output.DataType, false, output.Description, output.ExampleValue)
                                    {
                                        Conditional = !output.IsGuaranteedFor(node) && (existing?.Conditional ?? true)
                                    };
                                }
                            }
                        }
                        summary.ProducedOutputs = produced;
                        CheckParameters(node, contract);
                    }
                    summaries[node.Id] = summary;
                }

                BuildEdges();
                var order = TopologicalOrder();
                ComputeAncestors(order);

                var starts = GetStartNodes();
                var (guaranteed, covered, possible, reachable, imageGuaranteed) = RunScenarios(order, starts, triggerInputs, triggerProvidesPayload);

                // Everything a node can produce anywhere in the recipe (or the trigger provides): references to other
                // variables (environment, configuration, capture details, ...) are not checked here.
                var producible = new HashSet<string>(outputsMap.Keys, StringComparer.OrdinalIgnoreCase);
                producible.UnionWith(inputsMap.Keys);
                producible.UnionWith(ErrorVariables);

                foreach (var node in _nodes.Values)
                {
                    var summary = summaries[node.Id];
                    if (guaranteed == null)
                    {
                        continue;
                    }
                    if (!order.Contains(node.Id, StringComparer.OrdinalIgnoreCase))
                    {
                        continue; // part of a cycle, reported above
                    }
                    if (!reachable.Contains(node.Id))
                    {
                        summary.Reachable = false;
                        _warnings.Add($"Node '{node.Id}' ({node.StepType}) is never executed: no path from a start node leads to it.");
                        continue;
                    }

                    var guaranteedHere = guaranteed[node.Id];
                    summary.GuaranteedVariables = guaranteedHere.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();

                    if (_contracts.TryGetValue(node.Id, out var contract))
                    {
                        if (contract.PayloadContract.RawCapture == PayloadRequirement.Required && !imageGuaranteed[node.Id])
                        {
                            _warnings.Add($"Node '{node.Id}' ({node.StepType}) needs an image, but on some path to it no preceding node acquires one.");
                        }
                        foreach (var input in contract.InputVariables.Where(v => v.Required))
                        {
                            foreach (var name in input.ResolveNames(node))
                            {
                                if (guaranteedHere.Contains(name)) continue;
                                if (possible[node.Id].Contains(name))
                                {
                                    _warnings.Add($"Node '{node.Id}' ({node.StepType}) needs variable '{name}', which is not set on every path to it.");
                                }
                                else if (!inputsMap.ContainsKey(name))
                                {
                                    // Not produced before this node: the caller has to provide it
                                    inputsMap[name] = input;
                                }
                            }
                        }
                    }

                    foreach (var name in GetReferencedVariables(node).Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        string produced = FindProducible(name, producible);
                        if (produced == null || covered[node.Id].Contains(produced)) continue;
                        _warnings.Add(possible[node.Id].Contains(produced)
                            ? $"Node '{node.Id}' ({node.StepType}) uses ${{{produced}}}, which is not set on every path to it (it is only set in some branches)."
                            : $"Node '{node.Id}' ({node.StepType}) uses ${{{produced}}}, which is only set by nodes that do not run before it.");
                    }
                }

                if (result.AcquiresImage || triggerProvidesPayload)
                {
                    outputsMap["Payload.Width"] = new VariableContract("Payload.Width", ContractDataType.Integer, false, "Image width in pixels");
                    outputsMap["Payload.Height"] = new VariableContract("Payload.Height", ContractDataType.Integer, false, "Image height in pixels");
                }
                if (result.ExtractsText)
                {
                    outputsMap["Payload.ExtractedText"] = new VariableContract("Payload.ExtractedText", ContractDataType.String, false, "Text extracted via OCR or barcode recognition") { Conditional = true };
                }

                result.Inputs = inputsMap.Values.ToList().AsReadOnly();
                result.Outputs = outputsMap.Values.ToList().AsReadOnly();
                result.Steps = (_recipe.Nodes ?? new List<RecipeNodeConfig>())
                    .Where(n => n != null && !string.IsNullOrWhiteSpace(n.Id) && summaries.ContainsKey(n.Id))
                    .Select(n => summaries[n.Id])
                    .Distinct()
                    .ToList()
                    .AsReadOnly();
                result.ValidationWarnings = _warnings.Distinct().ToList().AsReadOnly();
                return result;
            }

            private void CheckParameters(RecipeNodeConfig node, StepContract contract)
            {
                var parameters = node.Parameters ?? new Dictionary<string, object>();
                foreach (var parameter in contract.Parameters)
                {
                    var supplied = parameters.FirstOrDefault(p => parameter.Matches(p.Key));
                    bool isSet = supplied.Key != null && supplied.Value != null && !(supplied.Value is string text && string.IsNullOrWhiteSpace(text));
                    if (parameter.Required && !isSet && parameter.DefaultValue == null)
                    {
                        _warnings.Add($"Node '{node.Id}' ({node.StepType}) is missing required parameter '{parameter.Name}'.");
                    }
                    if (isSet && parameter.AllowedValues.Count > 0 && supplied.Value is string literal && literal.IndexOf("${", StringComparison.Ordinal) < 0 &&
                        !parameter.AllowedValues.Contains(literal, StringComparer.OrdinalIgnoreCase))
                    {
                        _warnings.Add($"Node '{node.Id}' ({node.StepType}): parameter '{parameter.Name}' has value '{literal}'; allowed are {string.Join(", ", parameter.AllowedValues)}.");
                    }
                }

                if (contract.AcceptsUndeclaredParameters) return;
                foreach (var name in parameters.Keys)
                {
                    if (contract.FindParameter(name) == null)
                    {
                        _warnings.Add($"Node '{node.Id}' ({node.StepType}) has parameter '{name}', which the step does not read.");
                    }
                }
            }

            private IEnumerable<string> GetStartNodes()
            {
                var starts = (_recipe.Flow ?? new RecipeFlowConfig()).GetEffectiveStartNodes()
                    .Where(id => _nodes.ContainsKey(id))
                    .ToList();
                if (starts.Count == 0 && _recipe.Nodes?.Count > 0 && _recipe.Nodes[0]?.Id != null)
                {
                    starts.Add(_recipe.Nodes[0].Id);
                }
                foreach (var start in (_recipe.Flow?.StartNodes ?? new List<string>()).Where(id => !string.IsNullOrWhiteSpace(id) && !_nodes.ContainsKey(id)))
                {
                    _warnings.Add($"Start node '{start}' does not exist.");
                }
                return starts;
            }

            private void AddEdge(string from, string to, EdgeKind kind, string branch = null)
            {
                if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)) return;
                if (!_nodes.ContainsKey(from) || !_nodes.ContainsKey(to))
                {
                    _warnings.Add($"Transition from '{from}' to '{to}' refers to a node that does not exist.");
                    return;
                }
                var edge = new Edge { From = from, To = to, Kind = kind, Branch = branch };
                if (!_outgoing.TryGetValue(from, out var outList)) _outgoing[from] = outList = new List<Edge>();
                if (!_incoming.TryGetValue(to, out var inList)) _incoming[to] = inList = new List<Edge>();
                outList.Add(edge);
                inList.Add(edge);
            }

            private void BuildEdges()
            {
                var flow = _recipe.Flow ?? new RecipeFlowConfig();
                var conditionalSources = new HashSet<string>(
                    (flow.ConditionalTransitions ?? new List<RecipeConditionalTransitionConfig>()).Where(ct => !string.IsNullOrWhiteSpace(ct?.From) && !string.IsNullOrWhiteSpace(ct.To)).Select(ct => ct.From),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in flow.Transitions ?? new Dictionary<string, List<string>>())
                {
                    if (conditionalSources.Contains(kvp.Key))
                    {
                        // The engine follows only the conditional transitions of a node that has them
                        if (kvp.Value?.Count > 0)
                        {
                            _warnings.Add($"Node '{kvp.Key}' has conditional transitions, so its normal transitions ({string.Join(", ", kvp.Value)}) are never followed.");
                        }
                        continue;
                    }
                    foreach (var to in kvp.Value ?? new List<string>())
                    {
                        AddEdge(kvp.Key, to, EdgeKind.Standard);
                    }
                }

                foreach (var ct in flow.ConditionalTransitions ?? new List<RecipeConditionalTransitionConfig>())
                {
                    if (string.IsNullOrWhiteSpace(ct?.From) || string.IsNullOrWhiteSpace(ct.To)) continue;
                    AddEdge(ct.From, ct.To, EdgeKind.Conditional, ct.Branch ?? string.Empty);
                    if (!_branchKeys.TryGetValue(ct.From, out var keys)) _branchKeys[ct.From] = keys = new List<string>();
                    if (!keys.Contains(ct.Branch ?? string.Empty, StringComparer.OrdinalIgnoreCase)) keys.Add(ct.Branch ?? string.Empty);
                }

                foreach (var et in flow.ErrorTransitions ?? new List<RecipeErrorTransitionConfig>())
                {
                    if (string.IsNullOrWhiteSpace(et?.From) || string.IsNullOrWhiteSpace(et.To)) continue;
                    AddEdge(et.From, et.To, EdgeKind.Error);
                    _hasErrorEdge.Add(et.From);
                }
                foreach (var node in _nodes.Values)
                {
                    string target = node.OnErrorNodeId;
                    if (!string.IsNullOrWhiteSpace(target) && !_hasErrorEdge.Contains(node.Id))
                    {
                        AddEdge(node.Id, target, EdgeKind.Error);
                        _hasErrorEdge.Add(node.Id);
                    }
                }
            }

            private List<string> TopologicalOrder()
            {
                var inDegree = _nodes.Keys.ToDictionary(id => id, id => 0, StringComparer.OrdinalIgnoreCase);
                foreach (var edge in _outgoing.Values.SelectMany(e => e))
                {
                    inDegree[edge.To]++;
                }
                var queue = new Queue<string>((_recipe.Nodes ?? new List<RecipeNodeConfig>()).Where(n => n?.Id != null && _nodes.ContainsKey(n.Id) && inDegree[n.Id] == 0).Select(n => n.Id).Distinct(StringComparer.OrdinalIgnoreCase));
                var order = new List<string>();
                while (queue.Count > 0)
                {
                    var id = queue.Dequeue();
                    order.Add(id);
                    foreach (var edge in _outgoing.TryGetValue(id, out var edges) ? edges : new List<Edge>())
                    {
                        if (--inDegree[edge.To] == 0) queue.Enqueue(edge.To);
                    }
                }
                var cyclic = _nodes.Keys.Where(id => !order.Contains(id, StringComparer.OrdinalIgnoreCase)).ToList();
                if (cyclic.Count > 0)
                {
                    _warnings.Add($"The flow contains a cycle through node(s) {string.Join(", ", cyclic)}; these nodes are not analyzed.");
                }
                return order;
            }

            private void ComputeAncestors(List<string> order)
            {
                foreach (var id in order)
                {
                    var ancestors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var edge in _incoming.TryGetValue(id, out var edges) ? edges : new List<Edge>())
                    {
                        ancestors.Add(edge.From);
                        if (_ancestors.TryGetValue(edge.From, out var fromAncestors)) ancestors.UnionWith(fromAncestors);
                    }
                    _ancestors[id] = ancestors;
                }
            }

            private (Dictionary<string, HashSet<string>> guaranteed, Dictionary<string, HashSet<string>> covered, Dictionary<string, HashSet<string>> possible,
                HashSet<string> reachable, Dictionary<string, bool> image)
                RunScenarios(List<string> order, IEnumerable<string> starts, IReadOnlyList<VariableContract> triggerInputs, bool triggerProvidesPayload)
            {
                var startSet = new HashSet<string>(starts, StringComparer.OrdinalIgnoreCase);

                // Scenario dimensions: the branch of every conditional node, success or failure of every node with an error transition
                var dimensions = new List<(string NodeId, List<string> Outcomes)>();
                foreach (var id in order)
                {
                    if (_branchKeys.TryGetValue(id, out var keys)) dimensions.Add((id, keys.Select(k => "branch:" + k).ToList()));
                    if (_hasErrorEdge.Contains(id)) dimensions.Add((id, new List<string> { "ok", "error" }));
                }
                long scenarioCount = dimensions.Aggregate(1L, (count, d) => Math.Min(count * Math.Max(1, d.Outcomes.Count), MaxScenarios + 1L));
                if (scenarioCount > MaxScenarios)
                {
                    _warnings.Add($"The flow has too many branch combinations ({scenarioCount}+) to check which variables are set on every path; variable checks are skipped.");
                    return (null, null, null, new HashSet<string>(_nodes.Keys, StringComparer.OrdinalIgnoreCase), null);
                }

                var guaranteedInputs = new HashSet<string>(triggerInputs.Where(v => !v.Conditional).Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
                var possibleInputs = new HashSet<string>(triggerInputs.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

                var guaranteed = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                var covered = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                var possible = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                var image = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var choice = new int[dimensions.Count];
                for (long scenario = 0; scenario < scenarioCount; scenario++)
                {
                    // Decode the scenario number into one outcome per dimension
                    long rest = scenario;
                    for (int d = 0; d < dimensions.Count; d++)
                    {
                        int outcomes = Math.Max(1, dimensions[d].Outcomes.Count);
                        choice[d] = (int)(rest % outcomes);
                        rest /= outcomes;
                    }
                    string BranchOf(string id)
                    {
                        for (int d = 0; d < dimensions.Count; d++)
                        {
                            if (string.Equals(dimensions[d].NodeId, id, StringComparison.OrdinalIgnoreCase) && dimensions[d].Outcomes[choice[d]].StartsWith("branch:", StringComparison.Ordinal))
                                return dimensions[d].Outcomes[choice[d]].Substring("branch:".Length);
                        }
                        return null;
                    }
                    bool Fails(string id)
                    {
                        for (int d = 0; d < dimensions.Count; d++)
                        {
                            if (string.Equals(dimensions[d].NodeId, id, StringComparison.OrdinalIgnoreCase) && dimensions[d].Outcomes[choice[d]] == "error")
                                return true;
                        }
                        return false;
                    }

                    var executed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var id in order)
                    {
                        bool runs = startSet.Contains(id) || (_incoming.TryGetValue(id, out var edges) && edges.Any(e =>
                            executed.Contains(e.From) && (
                                (e.Kind == EdgeKind.Standard && !Fails(e.From)) ||
                                (e.Kind == EdgeKind.Conditional && !Fails(e.From) && string.Equals(BranchOf(e.From), e.Branch, StringComparison.OrdinalIgnoreCase)) ||
                                (e.Kind == EdgeKind.Error && Fails(e.From)))));
                        if (!runs) continue;
                        executed.Add(id);
                        reachable.Add(id);

                        // What is set before this node in this scenario: the trigger, plus every executed ancestor
                        var sure = new HashSet<string>(guaranteedInputs, StringComparer.OrdinalIgnoreCase);
                        var maybe = new HashSet<string>(possibleInputs, StringComparer.OrdinalIgnoreCase);
                        // Set on this path when the producing step finds something (conditional outputs included)
                        var produced = new HashSet<string>(possibleInputs, StringComparer.OrdinalIgnoreCase);
                        bool hasImage = triggerProvidesPayload;
                        foreach (var ancestor in _ancestors.TryGetValue(id, out var ancestors) ? ancestors : new HashSet<string>())
                        {
                            if (!executed.Contains(ancestor)) continue;
                            if (Fails(ancestor))
                            {
                                sure.UnionWith(ErrorVariables);
                                maybe.UnionWith(ErrorVariables);
                                produced.UnionWith(ErrorVariables);
                                continue;
                            }
                            var node = _nodes[ancestor];
                            if (!node.Enabled || !_contracts.TryGetValue(ancestor, out var contract)) continue;
                            hasImage |= contract.PayloadContract.RawCapture == PayloadRequirement.Created;
                            foreach (var output in contract.OutputVariables)
                            {
                                foreach (var name in output.ResolveNames(node))
                                {
                                    maybe.Add(name);
                                    produced.Add(name);
                                    if (output.IsGuaranteedFor(node)) sure.Add(name);
                                }
                            }
                        }

                        if (guaranteed.TryGetValue(id, out var existing))
                        {
                            existing.IntersectWith(sure);
                            covered[id].IntersectWith(produced);
                            possible[id].UnionWith(maybe);
                            image[id] &= hasImage;
                        }
                        else
                        {
                            guaranteed[id] = sure;
                            covered[id] = produced;
                            possible[id] = maybe;
                            image[id] = hasImage;
                        }
                    }
                }
                return (guaranteed, covered, possible, reachable, image);
            }

            private static IEnumerable<string> GetReferencedVariables(RecipeNodeConfig node)
            {
                var names = new List<string>();
                void Collect(object value)
                {
                    switch (value)
                    {
                        case null:
                            return;
                        case string text:
                            foreach (Match match in VariableReference.Matches(text))
                            {
                                names.Add(match.Groups[1].Value.TrimEnd('.'));
                            }
                            return;
                        case Newtonsoft.Json.Linq.JValue jValue:
                            Collect(jValue.Value);
                            return;
                        case Newtonsoft.Json.Linq.JObject jObject:
                            foreach (var property in jObject.Properties()) Collect(property.Value);
                            return;
                        case IDictionary dictionary:
                            foreach (var item in dictionary.Values) Collect(item);
                            return;
                        case IEnumerable enumerable:
                            foreach (var item in enumerable) Collect(item);
                            return;
                    }
                }
                foreach (var value in (node.Parameters ?? new Dictionary<string, object>()).Values)
                {
                    Collect(value);
                }
                return names;
            }

            /// <summary>
            /// The producible variable a reference is about: the reference itself, or the longest producible prefix
            /// (e.g. ${UserChoice.ask.Length} is about "UserChoice.ask").
            /// </summary>
            private static string FindProducible(string reference, HashSet<string> producible)
            {
                string candidate = reference;
                while (!string.IsNullOrEmpty(candidate))
                {
                    if (producible.Contains(candidate)) return candidate;
                    int dot = candidate.LastIndexOf('.');
                    if (dot < 0) return null;
                    candidate = candidate.Substring(0, dot);
                }
                return null;
            }
        }
    }
}
