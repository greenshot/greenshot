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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Expressions;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Pipeline
{
    /// <summary>
    /// Executes Directed Acyclic Graph (DAG) capture recipes.
    /// Handles asynchronous node execution, concurrent branch splitting (fork),
    /// dependency join synchronization (merge), and dynamic expression resolution.
    /// </summary>
    public class DagExecutionEngine
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DagExecutionEngine));

        private readonly Func<RecipeNodeConfig, ICaptureStep> _stepFactory;

        public DagExecutionEngine(Func<RecipeNodeConfig, ICaptureStep> stepFactory)
        {
            _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        }

        /// <summary>
        /// Executes the DAG recipe to completion or until cancelled/aborted.
        /// </summary>
        public async Task ExecuteAsync(CaptureRecipe recipe, CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (recipe.Nodes == null || recipe.Nodes.Count == 0)
            {
                context.LogStep("DAG execution finished: recipe has no nodes.");
                return;
            }

            var nodesById = new Dictionary<string, RecipeNodeConfig>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in recipe.Nodes)
            {
                if (!string.IsNullOrWhiteSpace(node?.Id))
                {
                    nodesById[node.Id] = node;
                }
            }

            var flow = recipe.Flow ?? new RecipeFlowConfig();
            var transitions = flow.GetUnifiedTransitions();
            var startNodes = flow.GetEffectiveStartNodes();

            if (startNodes.Count == 0 && recipe.Nodes.Count > 0)
            {
                startNodes.Add(recipe.Nodes[0].Id);
            }

            // Compute incoming predecessors for each node
            var incomingMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var nodeId in nodesById.Keys)
            {
                incomingMap[nodeId] = new List<string>();
            }

            foreach (var kvp in transitions)
            {
                string from = kvp.Key;
                foreach (var to in kvp.Value)
                {
                    if (incomingMap.TryGetValue(to, out var predecessors))
                    {
                        if (!predecessors.Contains(from, StringComparer.OrdinalIgnoreCase))
                        {
                            predecessors.Add(from);
                        }
                    }
                }
            }

            // Track pending predecessor completion counts for join synchronization
            var pendingIncoming = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in incomingMap)
            {
                pendingIncoming[kvp.Key] = kvp.Value.Count;
            }

            var completedNodes = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var syncLock = new object();
            var runningTasks = new List<Task>();

            Log.InfoFormat("Starting DAG execution for recipe '{0}' ({1} node(s), {2} start node(s))",
                recipe.Name, nodesById.Count, startNodes.Count);

            context.LogStep($"DAG Execution started with {startNodes.Count} entry node(s)");

            // Asynchronous recursive node runner
            async Task RunNodeAsync(string nodeId)
            {
                if (context.IsAborted || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (!nodesById.TryGetValue(nodeId, out var nodeConfig))
                {
                    Log.WarnFormat("DAG node '{0}' not found in recipe.", nodeId);
                    context.LogStep($"Warning: DAG node '{nodeId}' not found.");
                    return;
                }

                // Check node enabled state
                if (nodeConfig.Enabled)
                {
                    try
                    {
                        // Dynamically resolve expressions in node parameters prior to execution
                        var resolvedConfig = nodeConfig.Clone();
                        resolvedConfig.Parameters = ExpressionEvaluator.Instance.ResolveParameters(nodeConfig.Parameters, context);

                        var step = _stepFactory(resolvedConfig);
                        if (step == null)
                        {
                            Log.WarnFormat("Could not resolve executable step for node '{0}' [{1}]", nodeConfig.Id, nodeConfig.StepType);
                            context.LogStep($"Warning: unresolved step factory for node '{nodeConfig.Id}' [{nodeConfig.StepType}]");
                        }
                        else
                        {
                            context.LogStep($"Executing node: [{nodeConfig.Id}] {step.Name}");
                            Log.InfoFormat("Executing DAG node: [{0}] '{1}' [{2}]", nodeConfig.Id, step.Name, nodeConfig.StepType);
                            await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                            Log.InfoFormat("Finished DAG node: [{0}] '{1}'", nodeConfig.Id, step.Name);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        context.Abort($"Node '{nodeConfig.Id}' cancelled.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Node '{nodeConfig.Id}' failed with exception", ex);
                        context.Fail($"Node '{nodeConfig.Id}' failed: {ex.Message}", ex);
                        return;
                    }
                }
                else
                {
                    context.LogStep($"Skipping disabled node: [{nodeConfig.Id}] {nodeConfig.Name}");
                }

                completedNodes[nodeId] = true;

                if (context.IsAborted || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Evaluate conditional branch selection if this is a Conditional node
                string matchedBranchKey = null;
                if (string.Equals(nodeConfig.StepType, WellKnownStepTypes.Conditional, StringComparison.OrdinalIgnoreCase))
                {
                    var branchesParam = nodeConfig.GetParameter<object>("Branches") ?? nodeConfig.GetParameter<object>("branches");
                    if (branchesParam != null)
                    {
                        var branchList = new List<(string Key, string Expression)>();
                        if (branchesParam is System.Collections.IEnumerable enumerable && !(branchesParam is string))
                        {
                            foreach (var item in enumerable)
                            {
                                if (item is System.Collections.IDictionary d)
                                {
                                    string k = d.Contains("Key") ? d["Key"]?.ToString() : (d.Contains("key") ? d["key"]?.ToString() : null);
                                    string exp = d.Contains("Expression") ? d["Expression"]?.ToString() : (d.Contains("expression") ? d["expression"]?.ToString() : null);
                                    if (!string.IsNullOrEmpty(k)) branchList.Add((k, exp ?? "${true}"));
                                }
                                else if (item is Newtonsoft.Json.Linq.JObject jobj)
                                {
                                    string k = jobj.Value<string>("Key") ?? jobj.Value<string>("key");
                                    string exp = jobj.Value<string>("Expression") ?? jobj.Value<string>("expression");
                                    if (!string.IsNullOrEmpty(k)) branchList.Add((k, exp ?? "${true}"));
                                }
                            }
                        }

                        foreach (var b in branchList)
                        {
                            string exp = b.Expression?.Trim();
                            if (string.Equals(exp, "else", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(exp, "default", StringComparison.OrdinalIgnoreCase) ||
                                string.IsNullOrEmpty(exp))
                            {
                                matchedBranchKey = b.Key;
                                break;
                            }

                            bool isMet = ExpressionEvaluator.Instance.Evaluate<bool>(exp, context, false);
                            if (isMet)
                            {
                                matchedBranchKey = b.Key;
                                break;
                            }
                        }

                        context.LogStep($"Conditional node [{nodeId}] evaluated branch -> '{matchedBranchKey ?? "None"}'");
                    }
                }

                // Determine active next nodes to launch vs bypassed nodes
                var activeNext = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var bypassedNext = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Standard transitions
                if (flow.Transitions != null && flow.Transitions.TryGetValue(nodeId, out var stdTargets) && stdTargets != null)
                {
                    foreach (var t in stdTargets)
                    {
                        if (!string.IsNullOrWhiteSpace(t)) activeNext.Add(t);
                    }
                }

                // 2. Conditional transitions
                if (flow.ConditionalTransitions != null)
                {
                    foreach (var ct in flow.ConditionalTransitions)
                    {
                        if (!string.Equals(ct.From, nodeId, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(ct.To)) continue;

                        if (matchedBranchKey != null && string.Equals(ct.Branch, matchedBranchKey, StringComparison.OrdinalIgnoreCase))
                        {
                            activeNext.Add(ct.To);
                        }
                        else
                        {
                            bypassedNext.Add(ct.To);
                        }
                    }
                }

                // Find next ready downstream nodes
                var nextToLaunch = new List<string>();
                lock (syncLock)
                {
                    // Decrement incoming counter for bypassed nodes so join synchronization doesn't stall
                    foreach (var nextId in bypassedNext)
                    {
                        if (!activeNext.Contains(nextId))
                        {
                            if (pendingIncoming.TryGetValue(nextId, out int remaining))
                            {
                                remaining--;
                                pendingIncoming[nextId] = remaining;
                            }
                        }
                    }

                    // Process active targets
                    foreach (var nextId in activeNext)
                    {
                        if (pendingIncoming.TryGetValue(nextId, out int remaining))
                        {
                            remaining--;
                            pendingIncoming[nextId] = remaining;
                            if (remaining <= 0 && !completedNodes.ContainsKey(nextId))
                            {
                                nextToLaunch.Add(nextId);
                            }
                        }
                    }
                }

                // Split / Fork to all ready downstream nodes
                if (nextToLaunch.Count > 0)
                {
                    var childTasks = nextToLaunch.Select(RunNodeAsync).ToArray();
                    await Task.WhenAll(childTasks).ConfigureAwait(false);
                }
            }

            // Launch all start nodes
            var initialTasks = startNodes.Where(id => nodesById.ContainsKey(id)).Select(RunNodeAsync).ToArray();
            if (initialTasks.Length > 0)
            {
                await Task.WhenAll(initialTasks).ConfigureAwait(false);
            }
        }
    }
}
