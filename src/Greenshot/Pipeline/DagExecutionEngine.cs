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
            var activatedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var launchedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var bypassedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var syncLock = new object();

            foreach (var s in startNodes)
            {
                activatedNodes.Add(s);
                launchedNodes.Add(s);
            }

            // Compute reachability map for all nodes in the recipe
            var allTransitions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in nodesById.Keys)
            {
                allTransitions[id] = new List<string>();
            }
            foreach (var kvp in transitions)
            {
                if (allTransitions.TryGetValue(kvp.Key, out var list))
                {
                    list.AddRange(kvp.Value);
                }
                else
                {
                    allTransitions[kvp.Key] = new List<string>(kvp.Value);
                }
            }
            var descendantsMap = ComputeDescendantsMap(allTransitions);

            Log.InfoFormat("Starting DAG execution for recipe '{0}' ({1} node(s), {2} start node(s))",
                recipe.Name, nodesById.Count, startNodes.Count);

            context.LogStep($"DAG Execution started with {startNodes.Count} entry node(s)");

            // Dead-path elimination helper: recursively propagates bypass signals down the graph
            void BypassNodeLocked(string bypassedId, List<string> toLaunchList)
            {
                if (!bypassedNodes.Add(bypassedId))
                {
                    return;
                }

                Log.DebugFormat("Node '{0}' bypassed via dead-path elimination", bypassedId);

                // Collect all outgoing targets from bypassedId (both standard and conditional)
                var outgoing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (flow.Transitions != null && flow.Transitions.TryGetValue(bypassedId, out var stdTargets) && stdTargets != null)
                {
                    foreach (var t in stdTargets)
                    {
                        if (!string.IsNullOrWhiteSpace(t)) outgoing.Add(t);
                    }
                }
                if (flow.ConditionalTransitions != null)
                {
                    foreach (var ct in flow.ConditionalTransitions)
                    {
                        if (string.Equals(ct.From, bypassedId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ct.To))
                        {
                            outgoing.Add(ct.To);
                        }
                    }
                }

                foreach (var targetId in outgoing)
                {
                    if (pendingIncoming.TryGetValue(targetId, out int remaining))
                    {
                        remaining--;
                        pendingIncoming[targetId] = remaining;
                        if (remaining <= 0)
                        {
                            if (activatedNodes.Contains(targetId))
                            {
                                if (launchedNodes.Add(targetId))
                                {
                                    toLaunchList.Add(targetId);
                                }
                            }
                            else
                            {
                                BypassNodeLocked(targetId, toLaunchList);
                            }
                        }
                    }
                }
            }

            // Asynchronous recursive node runner
            async Task RunNodeAsync(string nodeId, CaptureFlowContext nodeContext)
            {
                if (nodeContext.IsAborted || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (!nodesById.TryGetValue(nodeId, out var nodeConfig))
                {
                    Log.WarnFormat("DAG node '{0}' not found in recipe.", nodeId);
                    nodeContext.LogStep($"Warning: DAG node '{nodeId}' not found.");
                    return;
                }

                // Check node enabled state
                if (nodeConfig.Enabled)
                {
                    try
                    {
                        // Dynamically resolve expressions in node parameters prior to execution
                        var resolvedConfig = nodeConfig.Clone();
                        resolvedConfig.Parameters = ExpressionEvaluator.Instance.ResolveParameters(nodeConfig.Parameters, nodeContext);

                        var step = _stepFactory(resolvedConfig);
                        if (step == null)
                        {
                            Log.WarnFormat("Could not resolve executable step for node '{0}' [{1}]", nodeConfig.Id, nodeConfig.StepType);
                            nodeContext.LogStep($"Warning: unresolved step factory for node '{nodeConfig.Id}' [{nodeConfig.StepType}]");
                        }
                        else
                        {
                            nodeContext.LogStep($"Executing node: [{nodeConfig.Id}] {step.Name}");
                            Log.InfoFormat("Executing DAG node: [{0}] '{1}' [{2}]", nodeConfig.Id, step.Name, nodeConfig.StepType);
                            await step.ExecuteAsync(nodeContext, cancellationToken).ConfigureAwait(false);
                            Log.InfoFormat("Finished DAG node: [{0}] '{1}'", nodeConfig.Id, step.Name);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        nodeContext.Abort($"Node '{nodeConfig.Id}' cancelled.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Node '{nodeConfig.Id}' failed with exception", ex);
                        nodeContext.Fail($"Node '{nodeConfig.Id}' failed: {ex.Message}", ex);
                        return;
                    }
                }
                else
                {
                    nodeContext.LogStep($"Skipping disabled node: [{nodeConfig.Id}] {nodeConfig.Name}");
                }

                completedNodes[nodeId] = true;

                if (nodeContext.IsAborted || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Evaluate conditional branch selection if this is a Conditional or UserPrompt node
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

                            bool isMet = ExpressionEvaluator.Instance.Evaluate<bool>(exp, nodeContext, false);
                            if (isMet)
                            {
                                matchedBranchKey = b.Key;
                                break;
                            }
                        }

                        nodeContext.LogStep($"Conditional node [{nodeId}] evaluated branch -> '{matchedBranchKey ?? "None"}'");
                    }
                }
                else if (string.Equals(nodeConfig.StepType, WellKnownStepTypes.UserPrompt, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(nodeConfig.StepType, "PromptChoice", StringComparison.OrdinalIgnoreCase))
                {
                    if (nodeContext.Properties.TryGetValue("UserPrompt.Choice." + nodeId, out var choiceObj) && choiceObj != null)
                    {
                        matchedBranchKey = choiceObj.ToString();
                    }
                    else if (nodeContext.Properties.TryGetValue("UserChoice." + nodeId, out var ucObj) && ucObj != null)
                    {
                        matchedBranchKey = ucObj.ToString();
                    }
                    else if (nodeContext.Properties.TryGetValue("LastUserChoice", out var lastChoice) && lastChoice != null)
                    {
                        matchedBranchKey = lastChoice.ToString();
                    }

                    nodeContext.LogStep($"UserPrompt node [{nodeId}] selected branch -> '{matchedBranchKey ?? "None"}'");
                }

                // Determine active next nodes to launch vs bypassed nodes
                var activeNext = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var bypassedNext = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                bool hasConditionalTransitions = flow.ConditionalTransitions != null &&
                                                 flow.ConditionalTransitions.Any(ct => string.Equals(ct.From, nodeId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ct.To));

                if (hasConditionalTransitions)
                {
                    // For conditional nodes (Conditional or UserPrompt), transitions are driven strictly by the matched branch
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
                else
                {
                    // Standard transitions (only when no conditional transitions exist from this node)
                    if (flow.Transitions != null && flow.Transitions.TryGetValue(nodeId, out var stdTargets) && stdTargets != null)
                    {
                        foreach (var t in stdTargets)
                        {
                            if (!string.IsNullOrWhiteSpace(t)) activeNext.Add(t);
                        }
                    }
                }

                // Find next ready downstream nodes
                var nextToLaunch = new List<string>();
                lock (syncLock)
                {
                    // First, mark all active targets in activatedNodes
                    foreach (var nextId in activeNext)
                    {
                        activatedNodes.Add(nextId);
                    }

                    // Decrement incoming counter for bypassed nodes and propagate dead paths
                    foreach (var nextId in bypassedNext)
                    {
                        if (!activeNext.Contains(nextId))
                        {
                            if (pendingIncoming.TryGetValue(nextId, out int remaining))
                            {
                                remaining--;
                                pendingIncoming[nextId] = remaining;
                                if (remaining <= 0)
                                {
                                    if (activatedNodes.Contains(nextId))
                                    {
                                        if (launchedNodes.Add(nextId))
                                        {
                                            nextToLaunch.Add(nextId);
                                        }
                                    }
                                    else
                                    {
                                        BypassNodeLocked(nextId, nextToLaunch);
                                    }
                                }
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
                            if (remaining <= 0)
                            {
                                if (activatedNodes.Contains(nextId))
                                {
                                    if (launchedNodes.Add(nextId))
                                    {
                                        nextToLaunch.Add(nextId);
                                    }
                                }
                                else
                                {
                                    BypassNodeLocked(nextId, nextToLaunch);
                                }
                            }
                        }
                    }
                }

                // Split / Fork to all ready downstream nodes
                if (nextToLaunch.Count > 0)
                {
                    // Partition into merge clusters: branches in the same cluster share a merge node downstream;
                    // independent branches (with no common merge descendant) receive isolated cloned contexts/payloads.
                    var clusters = PartitionIntoMergeClusters(nextToLaunch, descendantsMap);

                    if (clusters.Count == 1)
                    {
                        // Converging merge cluster: execute branches sequentially in depth-first declaration order
                        foreach (var childNodeId in clusters[0])
                        {
                            await RunNodeAsync(childNodeId, nodeContext).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // Multiple independent non-merging clusters: run clusters in parallel with isolated cloned contexts
                        var childTasks = new List<Task>();
                        for (int i = 0; i < clusters.Count; i++)
                        {
                            var cluster = clusters[i];
                            var clusterContext = nodeContext.CreateBranchContext();
                            Log.InfoFormat("Branch split detected without merge downstream for node(s) [{0}]. Created isolated cloned payload and context.",
                                string.Join(", ", cluster));
                            clusterContext.LogStep($"Branch split without merge: created isolated cloned payload for branch entry [{string.Join(", ", cluster)}]");

                            childTasks.Add(Task.Run(async () =>
                            {
                                foreach (var childNodeId in cluster)
                                {
                                    await RunNodeAsync(childNodeId, clusterContext).ConfigureAwait(false);
                                }
                            }));
                        }

                        await Task.WhenAll(childTasks).ConfigureAwait(false);
                    }
                }
            }

            // Launch all start nodes, partitioned into merge clusters
            var validStartNodes = startNodes.Where(id => nodesById.ContainsKey(id)).ToList();
            if (validStartNodes.Count > 0)
            {
                var startClusters = PartitionIntoMergeClusters(validStartNodes, descendantsMap);

                if (startClusters.Count == 1)
                {
                    // Converging merge cluster: execute entry nodes sequentially in depth-first order
                    foreach (var startNodeId in startClusters[0])
                    {
                        await RunNodeAsync(startNodeId, context).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Multiple independent start clusters: run clusters in parallel with isolated cloned contexts
                    var initialTasks = new List<Task>();
                    for (int i = 0; i < startClusters.Count; i++)
                    {
                        var cluster = startClusters[i];
                        var clusterContext = context.CreateBranchContext();
                        Log.InfoFormat("Multiple independent start nodes detected. Created isolated cloned context for entry [{0}].",
                            string.Join(", ", cluster));

                        initialTasks.Add(Task.Run(async () =>
                        {
                            foreach (var startNodeId in cluster)
                            {
                                await RunNodeAsync(startNodeId, clusterContext).ConfigureAwait(false);
                            }
                        }));
                    }

                    await Task.WhenAll(initialTasks).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Computes the set of all reachable descendant nodes for every node in the DAG.
        /// </summary>
        private static Dictionary<string, HashSet<string>> ComputeDescendantsMap(
            Dictionary<string, List<string>> transitions)
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var nodeId in transitions.Keys)
            {
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { nodeId };
                var queue = new Queue<string>();
                queue.Enqueue(nodeId);

                while (queue.Count > 0)
                {
                    var curr = queue.Dequeue();
                    if (transitions.TryGetValue(curr, out var nextList) && nextList != null)
                    {
                        foreach (var next in nextList)
                        {
                            if (!string.IsNullOrWhiteSpace(next) && visited.Add(next))
                            {
                                queue.Enqueue(next);
                            }
                        }
                    }
                }

                map[nodeId] = visited;
            }

            return map;
        }

        /// <summary>
        /// Partitions a list of sibling node IDs into merge clusters.
        /// Sibling nodes whose downstream reachable sets overlap (i.e. merge at a common descendant)
        /// are grouped into the same cluster. Sibling nodes with no common descendants form independent clusters.
        /// </summary>
        private static List<List<string>> PartitionIntoMergeClusters(
            IReadOnlyList<string> nodeIds,
            Dictionary<string, HashSet<string>> descendantsMap)
        {
            var clusters = new List<List<string>>();
            if (nodeIds == null || nodeIds.Count == 0) return clusters;

            foreach (var node in nodeIds)
            {
                var nodeDescendants = descendantsMap != null && descendantsMap.TryGetValue(node, out var d)
                    ? d
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { node };

                var matchingClusters = new List<List<string>>();

                foreach (var cluster in clusters)
                {
                    bool sharesMerge = false;
                    foreach (var member in cluster)
                    {
                        var memberDescendants = descendantsMap != null && descendantsMap.TryGetValue(member, out var md)
                            ? md
                            : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { member };

                        if (nodeDescendants.Overlaps(memberDescendants))
                        {
                            sharesMerge = true;
                            break;
                        }
                    }

                    if (sharesMerge)
                    {
                        matchingClusters.Add(cluster);
                    }
                }

                if (matchingClusters.Count == 0)
                {
                    clusters.Add(new List<string> { node });
                }
                else if (matchingClusters.Count == 1)
                {
                    matchingClusters[0].Add(node);
                }
                else
                {
                    // Sibling node connects multiple existing clusters (e.g. multi-way join)
                    var merged = matchingClusters[0];
                    merged.Add(node);
                    for (int i = 1; i < matchingClusters.Count; i++)
                    {
                        merged.AddRange(matchingClusters[i]);
                        clusters.Remove(matchingClusters[i]);
                    }
                }
            }

            return clusters;
        }
    }
}

