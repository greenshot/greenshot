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
using Greenshot.Base.Core;
using Greenshot.Base.Expressions;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Recipes.Pipeline
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
        private readonly Func<string, StepContract> _contractLookup;

        /// <param name="stepFactory">Creates the step of a node.</param>
        /// <param name="contractLookup">The contract of a step type (IStepRegistry.GetContract); without it, contracts are not checked.</param>
        public DagExecutionEngine(Func<RecipeNodeConfig, ICaptureStep> stepFactory, Func<string, StepContract> contractLookup = null)
        {
            _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
            _contractLookup = contractLookup;
        }

        /// <summary>
        /// Receives contract violations found after a step ran: a variable the step wrote without declaring it,
        /// a declared (non-conditional) output it did not set, or a missing image it should have created.
        /// Debug builds log them as warnings; release builds do not check (null). Tests can collect them.
        /// </summary>
        public Action<string> ContractViolation { get; set; } =
#if DEBUG
            message => Log.Warn("[CONTRACT] " + message);
#else
            null;
#endif

        private static string GetBranchProperty(object branch, string name)
        {
            switch (branch)
            {
                case Newtonsoft.Json.Linq.JObject jObject:
                    return jObject.GetValue(name, StringComparison.OrdinalIgnoreCase)?.ToString();
                case System.Collections.IDictionary dictionary:
                    foreach (System.Collections.DictionaryEntry entry in dictionary)
                    {
                        if (string.Equals(entry.Key?.ToString(), name, StringComparison.OrdinalIgnoreCase)) return entry.Value?.ToString();
                    }
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Whether the node runs: a disabled node, or one whose enabled expression is false, is passed through to its next nodes
        /// </summary>
        private static bool IsNodeRunning(RecipeNodeConfig node, CaptureFlowContext context)
        {
            try
            {
                if (node.ShouldRun(context)) return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"The enabled expression '{node.EnabledExpression}' of node '{node.Id}' failed, the node is skipped.", ex);
            }

            return false;
        }

        /// <summary>
        /// Checks what the step's contract requires before the step runs: required parameters (set, or with a default)
        /// and required input variables. A missing one fails the node with a clear message.
        /// </summary>
        private static void ValidateBeforeStep(RecipeNodeConfig node, StepContract contract, CaptureFlowContext context)
        {
            if (contract == null) return;
            var parameters = node.Parameters ?? new Dictionary<string, object>();
            foreach (var parameter in contract.Parameters.Where(p => p.Required && p.DefaultValue == null))
            {
                bool isSet = parameters.Any(kvp => parameter.Matches(kvp.Key) && kvp.Value != null && !(kvp.Value is string text && string.IsNullOrWhiteSpace(text)));
                if (!isSet)
                {
                    throw new InvalidOperationException($"Node '{node.Id}' ({node.StepType}) is missing required parameter '{parameter.Name}'.");
                }
            }
            foreach (var input in contract.InputVariables.Where(v => v.Required))
            {
                foreach (var name in input.ResolveNames(node))
                {
                    if (!context.Properties.ContainsKey(name))
                    {
                        throw new InvalidOperationException($"Node '{node.Id}' ({node.StepType}) needs variable '{name}', which is not set.");
                    }
                }
            }
        }

        /// <summary>
        /// Compares what the step did with its contract: variables written without being declared, declared outputs
        /// that were not set, an image that should have been created. Reported through <see cref="ContractViolation"/>.
        /// </summary>
        private void CheckAfterStep(RecipeNodeConfig node, StepContract contract, CaptureFlowContext context, Dictionary<string, object> before)
        {
            var report = ContractViolation;
            if (report == null || context.IsAborted || context.State == CaptureFlowState.Failed) return;

            var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var output in contract.OutputVariables)
            {
                foreach (var name in output.ResolveNames(node))
                {
                    declared.Add(name);
                    if (output.IsGuaranteedFor(node) && !context.Properties.ContainsKey(name))
                    {
                        report($"Node '{node.Id}' ({node.StepType}) did not set its declared output '{name}'.");
                    }
                }
            }

            foreach (var kvp in context.Properties.ToList())
            {
                bool changed = !before.TryGetValue(kvp.Key, out var previous) || !Equals(previous, kvp.Value);
                if (changed && !declared.Contains(kvp.Key))
                {
                    report($"Node '{node.Id}' ({node.StepType}) set variable '{kvp.Key}', which its contract does not declare.");
                }
            }

            if (contract.PayloadContract.RawCapture == PayloadRequirement.Created && context.Payload?.RawCapture == null)
            {
                report($"Node '{node.Id}' ({node.StepType}) should have created the image, but there is none.");
            }
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

            // Nodes started next to the running path (joins downstream of a failed node), awaited before the flow ends
            var detachedRuns = new List<Task>();
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

                // Check node enabled state, also its enabled expression (e.g. switched by a recipe option)
                if (IsNodeRunning(nodeConfig, nodeContext))
                {
                    try
                    {
                        // Expressions in node parameters are evaluated exactly once: here, or by the step itself when it
                        // implements IEvaluatesOwnParameters (it then receives the raw parameters). See IEvaluatesOwnParameters.
                        var step = _stepFactory(nodeConfig.Clone());
                        if (step != null && !(step is IEvaluatesOwnParameters))
                        {
                            var resolvedConfig = nodeConfig.Clone();
                            resolvedConfig.Parameters = ExpressionEvaluator.Instance.ResolveParameters(nodeConfig.Parameters, nodeContext);
                            step = _stepFactory(resolvedConfig);
                        }
                        if (step == null)
                        {
                            Log.WarnFormat("Could not resolve executable step for node '{0}' [{1}]", nodeConfig.Id, nodeConfig.StepType);
                            nodeContext.LogStep($"Warning: unresolved step factory for node '{nodeConfig.Id}' [{nodeConfig.StepType}]");
                        }
                        else
                        {
                            var contract = _contractLookup?.Invoke(nodeConfig.StepType);
                            // Fails the node (error transitions apply) when a required parameter or input is missing
                            ValidateBeforeStep(nodeConfig, contract, nodeContext);
                            var before = contract != null && ContractViolation != null ? new Dictionary<string, object>(nodeContext.Properties, StringComparer.OrdinalIgnoreCase) : null;

                            nodeContext.LogStep($"Executing node: [{nodeConfig.Id}] {step.Name}");
                            Log.InfoFormat("Executing DAG node: [{0}] '{1}' [{2}]", nodeConfig.Id, step.Name, nodeConfig.StepType);
                            // Every step runs on the thread pool (roadmap section 2)
                            ThreadAssert.NotUi($"Step '{step.Name}' [{nodeConfig.StepType}]");
                            WinFormsContextGuard.ProtectPoolThread(nodeContext.Ui);
                            using (FlowDiagnostics.EnterStep(nodeContext.ExecutionId, $"{step.Name} [{nodeConfig.StepType}]"))
                            {
                                await step.ExecuteAsync(nodeContext, cancellationToken).ConfigureAwait(false);
                            }
                            ThreadAssert.NotUi($"After step '{step.Name}' [{nodeConfig.StepType}]");
                            WinFormsContextGuard.ProtectPoolThread(nodeContext.Ui);
                            Log.InfoFormat("Finished DAG node: [{0}] '{1}'", nodeConfig.Id, step.Name);

                            if (before != null)
                            {
                                CheckAfterStep(nodeConfig, contract, nodeContext, before);
                            }
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

                        // Check flow error transitions or node-level error configuration
                        var errorTransition = flow.GetErrorTransition(nodeId, ex);
                        string errorTargetNodeId = errorTransition?.To
                            ?? (!string.IsNullOrEmpty(nodeConfig.OnErrorNodeId) ? nodeConfig.OnErrorNodeId : null);
                        string errorTargetRecipeId = errorTransition?.TargetRecipeId
                            ?? (!string.IsNullOrEmpty(nodeConfig.OnErrorRecipeId) ? nodeConfig.OnErrorRecipeId : null);

                        if (!string.IsNullOrEmpty(errorTargetNodeId) || !string.IsNullOrEmpty(errorTargetRecipeId))
                        {
                            nodeContext.Properties["LastError"] = ex.Message;
                            nodeContext.Properties["LastErrorType"] = ex.GetType().Name;
                            nodeContext.Properties["FailedNodeId"] = nodeId;
                            nodeContext.LogStep($"Node '{nodeConfig.Id}' failed: {ex.Message}. Routing to error handler: '{(string.IsNullOrEmpty(errorTargetNodeId) ? errorTargetRecipeId : errorTargetNodeId)}'");

                            completedNodes[nodeId] = true;

                            // Bypass standard downstream nodes of the failed node
                            var toLaunchOnBypass = new List<string>();
                            lock (syncLock)
                            {
                                BypassNodeLocked(nodeId, toLaunchOnBypass);
                            }

                            foreach (var nextId in toLaunchOnBypass)
                            {
                                var childContext = nodeContext.CreateBranchContext();
                                // PARALLEL: the join downstream of the failed node runs next to the error handler; awaited before the flow ends
#pragma warning disable RS0030 // R10: documented parallel branch
                                var detachedRun = Task.Run(async () =>
#pragma warning restore RS0030
                                {
                                    try
                                    {
                                        await RunNodeAsync(nextId, childContext).ConfigureAwait(false);
                                    }
                                    catch (Exception childEx)
                                    {
                                        Log.Error($"Error executing bypassed-join node '{nextId}'", childEx);
                                    }
                                }, cancellationToken);
                                lock (detachedRuns)
                                {
                                    detachedRuns.Add(detachedRun);
                                }
                            }

                            if (!string.IsNullOrEmpty(errorTargetRecipeId))
                            {
                                var recipeManager = SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true);
                                var targetRecipe = recipeManager?.GetRecipeById(errorTargetRecipeId);
                                if (targetRecipe != null)
                                {
                                    var pipeline = SimpleServiceProvider.Current?.GetInstance<ICapturePipeline>(isOptional: true);
                                    if (pipeline != null)
                                    {
                                        var errorRecipeContext = await pipeline.ExecuteAsync(targetRecipe, null, ctx =>
                                        {
                                            ctx.Payload = nodeContext.Payload;
                                            // Keep streaming to the original caller (e.g. a StderrStep in the recovery recipe)
                                            ctx.StdoutWriter = nodeContext.StdoutWriter;
                                            ctx.StderrWriter = nodeContext.StderrWriter;
                                            foreach (var kvp in nodeContext.Properties)
                                            {
                                                ctx.Properties[kvp.Key] = kvp.Value;
                                            }
                                        }, cancellationToken).ConfigureAwait(false);
                                        MergeOutcome(errorRecipeContext, nodeContext);
                                    }
                                }
                                else
                                {
                                    Log.WarnFormat("Target error recipe '{0}' not found for node '{1}'.", errorTargetRecipeId, nodeId);
                                    nodeContext.Fail($"Target error recipe '{errorTargetRecipeId}' not found.", ex);
                                }
                                return;
                            }
                            else if (!string.IsNullOrEmpty(errorTargetNodeId))
                            {
                                lock (syncLock)
                                {
                                    activatedNodes.Add(errorTargetNodeId);
                                    launchedNodes.Add(errorTargetNodeId);
                                }
                                await RunNodeAsync(errorTargetNodeId, nodeContext).ConfigureAwait(false);
                                return;
                            }
                        }

                        nodeContext.Fail($"Node '{nodeConfig.Id}' failed: {ex.Message}", ex);
                        return;
                    }
                }
                else
                {
                    nodeContext.LogStep(string.IsNullOrWhiteSpace(nodeConfig.EnabledExpression)
                        ? $"Skipping disabled node: [{nodeConfig.Id}] {nodeConfig.Name}"
                        : $"Skipping node: [{nodeConfig.Id}] {nodeConfig.Name}, '{nodeConfig.EnabledExpression}' is not true");
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
                    var branchesParam = nodeConfig.GetParameter<object>("Branches");
                    if (branchesParam != null)
                    {
                        var branchList = new List<(string Key, string Expression)>();
                        if (branchesParam is System.Collections.IEnumerable enumerable && !(branchesParam is string))
                        {
                            foreach (var item in enumerable)
                            {
                                // Branch properties are case-insensitive, like parameters (JSON recipes use "key", code "Key")
                                string k = GetBranchProperty(item, "Key");
                                string exp = GetBranchProperty(item, "Expression");
                                if (!string.IsNullOrEmpty(k)) branchList.Add((k, exp));
                            }
                        }

                        foreach (var b in branchList)
                        {
                            string exp = b.Expression?.Trim();
                            if (string.IsNullOrEmpty(exp))
                            {
                                continue;
                            }
                            if (string.Equals(exp, "else", StringComparison.OrdinalIgnoreCase))
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
                else if (string.Equals(nodeConfig.StepType, WellKnownStepTypes.UserPrompt, StringComparison.OrdinalIgnoreCase))
                {
                    if (nodeContext.Properties.TryGetValue("UserChoice." + nodeId, out var choiceObj) && choiceObj != null)
                    {
                        matchedBranchKey = choiceObj.ToString();
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
                        var clusterContexts = new List<CaptureFlowContext>();
                        for (int i = 0; i < clusters.Count; i++)
                        {
                            var cluster = clusters[i];
                            var clusterContext = nodeContext.CreateBranchContext();
                            clusterContexts.Add(clusterContext);
                            Log.InfoFormat("Branch split detected without merge downstream for node(s) [{0}]. Created isolated cloned payload and context.",
                                string.Join(", ", cluster));
                            clusterContext.LogStep($"Branch split without merge: created isolated cloned payload for branch entry [{string.Join(", ", cluster)}]");

                            // PARALLEL: independent branches (no common merge node) run concurrently on their own payload copy
#pragma warning disable RS0030 // R10: documented parallel branch
                            childTasks.Add(Task.Run(async () =>
#pragma warning restore RS0030
                            {
                                foreach (var childNodeId in cluster)
                                {
                                    await RunNodeAsync(childNodeId, clusterContext).ConfigureAwait(false);
                                }
                            }, cancellationToken));
                        }

                        await Task.WhenAll(childTasks).ConfigureAwait(false);

                        foreach (var clusterCtx in clusterContexts)
                        {
                            foreach (var kvp in clusterCtx.Properties)
                            {
                                nodeContext.Properties[kvp.Key] = kvp.Value;
                            }

                            MergeOutcome(clusterCtx, nodeContext);
                        }
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
                    var initialContexts = new List<CaptureFlowContext>();
                    for (int i = 0; i < startClusters.Count; i++)
                    {
                        var cluster = startClusters[i];
                        var clusterContext = context.CreateBranchContext();
                        initialContexts.Add(clusterContext);
                        Log.InfoFormat("Multiple independent start nodes detected. Created isolated cloned context for entry [{0}].",
                            string.Join(", ", cluster));

                        // PARALLEL: independent start clusters run concurrently on their own context copy
#pragma warning disable RS0030 // R10: documented parallel branch
                        initialTasks.Add(Task.Run(async () =>
#pragma warning restore RS0030
                        {
                            foreach (var startNodeId in cluster)
                            {
                                await RunNodeAsync(startNodeId, clusterContext).ConfigureAwait(false);
                            }
                        }, cancellationToken));
                    }

                    await Task.WhenAll(initialTasks).ConfigureAwait(false);

                    foreach (var initCtx in initialContexts)
                    {
                        foreach (var kvp in initCtx.Properties)
                        {
                            context.Properties[kvp.Key] = kvp.Value;
                        }

                        MergeOutcome(initCtx, context);
                    }
                }
            }

            // Nothing may outlive the flow: wait for the detached runs (they can start more of them while we wait)
            while (true)
            {
                Task[] pending;
                lock (detachedRuns)
                {
                    pending = detachedRuns.Where(t => !t.IsCompleted).ToArray();
                }

                if (pending.Length == 0)
                {
                    break;
                }

                try
                {
                    await Task.WhenAll(pending).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // The flow is cancelled, the detached runs were cancelled with it
                }
            }
        }

        /// <summary>
        /// Propagates the outcome of an isolated child flow (parallel branch, independent start cluster or error-recovery recipe)
        /// back into its parent: failure/cancellation state and the first non-zero exit code (e.g. set by a StderrStep).
        /// Properties are merged by the caller.
        /// </summary>
        private static void MergeOutcome(CaptureFlowContext child, CaptureFlowContext parent)
        {
            if (child == null || parent == null)
            {
                return;
            }

            if (child.ExitCode != 0 && parent.ExitCode == 0)
            {
                parent.ExitCode = child.ExitCode;
            }

            if (child.State == CaptureFlowState.Failed && parent.State != CaptureFlowState.Failed)
            {
                parent.Fail(child.AbortReason, child.Error);
            }
            else if (child.State == CaptureFlowState.Cancelled && !parent.IsAborted)
            {
                parent.Abort(child.AbortReason);
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

